using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeManagement.Data;
using CafeManagement.DTOs;
using CafeManagement.Models;
 
namespace CafeManagement.Controllers
{
    // ================================================================
    // MENU CONTROLLER
    // ================================================================
 
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MenuController : ControllerBase
    {
        private readonly CafeDbContext _db;
        public MenuController(CafeDbContext db) => _db = db;
 
        [HttpGet("categories")]
        public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories()
        {
            var categories = await _db.Categories
                .Select(c => new CategoryDto(
                    c.Id, c.Name,
                    c.MenuItems.Count(m => !m.IsDeleted)))
                .ToListAsync();
 
            return Ok(new ApiResponse<List<CategoryDto>>(true, null, categories));
        }
 
        [HttpPost("categories")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            if (await _db.Categories.AnyAsync(c => c.Name == request.Name))
                return Conflict(new ApiResponse<CategoryDto>(false, "Danh mục đã tồn tại", null));
 
            var cat = new Category { Name = request.Name };
            _db.Categories.Add(cat);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCategories), new ApiResponse<CategoryDto>(true, null,
                new CategoryDto(cat.Id, cat.Name, 0)));
        }
 
        [HttpGet("items")]
        public async Task<ActionResult<ApiResponse<List<MenuItemDto>>>> GetMenuItems(
            [FromQuery] int? categoryId,
            [FromQuery] bool? isAvailable)
        {
            var query = _db.MenuItems.Include(m => m.Category).AsQueryable();
            if (categoryId.HasValue) query = query.Where(m => m.CategoryId == categoryId);
            if (isAvailable.HasValue) query = query.Where(m => m.IsAvailable == isAvailable);
 
            var items = await query
                .Select(m => new MenuItemDto(
                    m.Id, m.CategoryId, m.Category.Name,
                    m.Name, m.Price, m.ImageUrl, m.IsAvailable, m.Description))
                .ToListAsync();
 
            return Ok(new ApiResponse<List<MenuItemDto>>(true, null, items));
        }
 
        [HttpGet("items/{id:int}")]
        public async Task<ActionResult<ApiResponse<MenuItemDto>>> GetMenuItem(int id)
        {
            var m = await _db.MenuItems.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id);
            if (m == null) return NotFound(new ApiResponse<MenuItemDto>(false, "Không tìm thấy món", null));
            return Ok(new ApiResponse<MenuItemDto>(true, null,
                new MenuItemDto(m.Id, m.CategoryId, m.Category.Name, m.Name, m.Price, m.ImageUrl, m.IsAvailable, m.Description)));
        }
 
        [HttpPost("items")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<MenuItemDto>>> CreateMenuItem([FromBody] CreateMenuItemRequest request)
        {
            var item = new MenuItem
            {
                CategoryId = request.CategoryId,
                Name = request.Name,
                Price = request.Price,
                ImageUrl = request.ImageUrl,
                IsAvailable = request.IsAvailable,
                Description = request.Description
            };
            _db.MenuItems.Add(item);
            await _db.SaveChangesAsync();
 
            return CreatedAtAction(nameof(GetMenuItem), new { id = item.Id },
                new ApiResponse<MenuItemDto>(true, "Tạo món thành công", null));
        }
 
        [HttpPut("items/{id:int}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<MenuItemDto>>> UpdateMenuItem(int id, [FromBody] UpdateMenuItemRequest request)
        {
            var item = await _db.MenuItems.FindAsync(id);
            if (item == null) return NotFound(new ApiResponse<MenuItemDto>(false, "Không tìm thấy món", null));
 
            // Lưu lịch sử giá nếu thay đổi
            if (request.Price.HasValue && request.Price != item.Price)
            {
                _db.PriceHistories.Add(new PriceHistory
                {
                    MenuItemId = id,
                    OldPrice = item.Price,
                    NewPrice = request.Price.Value,
                    StartDate = DateTime.UtcNow
                });
                item.Price = request.Price.Value;
            }
 
            if (request.Name != null) item.Name = request.Name;
            if (request.ImageUrl != null) item.ImageUrl = request.ImageUrl;
            if (request.IsAvailable.HasValue) item.IsAvailable = request.IsAvailable.Value;
            if (request.Description != null) item.Description = request.Description;
            if (request.CategoryId.HasValue) item.CategoryId = request.CategoryId.Value;
            item.UpdatedAt = DateTime.UtcNow;
 
            await _db.SaveChangesAsync();
            return Ok(new ApiResponse<MenuItemDto>(true, "Cập nhật thành công", null));
        }
 
        [HttpDelete("items/{id:int}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteMenuItem(int id)
        {
            var item = await _db.MenuItems.FindAsync(id);
            if (item == null) return NotFound(new ApiResponse<object>(false, "Không tìm thấy món", null));
            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new ApiResponse<object>(true, "Đã xóa món", null));
        }
    }
 
    // ================================================================
    // CUSTOMERS CONTROLLER
    // ================================================================
 
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly CafeDbContext _db;
        public CustomersController(CafeDbContext db) => _db = db;
 
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<CustomerDto>>>> GetCustomers(
            [FromQuery] string? search,
            [FromQuery] string? tier,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _db.Customers.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.Phone.Contains(search) || c.FullName.Contains(search));
            if (!string.IsNullOrEmpty(tier))
                query = query.Where(c => c.MemberTier == tier.ToUpper());
 
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(c => c.Points)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(c => new CustomerDto(c.Id, c.FullName, c.Phone, c.Email, c.Birthday, c.Points, c.MemberTier))
                .ToListAsync();
 
            return Ok(new ApiResponse<PagedResult<CustomerDto>>(true, null,
                new PagedResult<CustomerDto>(items, total, page, pageSize)));
        }
 
        [HttpGet("lookup")]
        public async Task<ActionResult<ApiResponse<CustomerDto>>> LookupByPhone([FromQuery] string phone)
        {
            var c = await _db.Customers.FirstOrDefaultAsync(x => x.Phone == phone);
            if (c == null) return NotFound(new ApiResponse<CustomerDto>(false, "Không tìm thấy khách hàng", null));
            return Ok(new ApiResponse<CustomerDto>(true, null,
                new CustomerDto(c.Id, c.FullName, c.Phone, c.Email, c.Birthday, c.Points, c.MemberTier)));
        }
 
        [HttpPost]
        public async Task<ActionResult<ApiResponse<CustomerDto>>> CreateCustomer([FromBody] CreateCustomerRequest request)
        {
            if (await _db.Customers.AnyAsync(c => c.Phone == request.Phone))
                return Conflict(new ApiResponse<CustomerDto>(false, "Số điện thoại đã được đăng ký", null));
 
            var customer = new Customer
            {
                FullName = request.FullName,
                Phone = request.Phone,
                Email = request.Email,
                Birthday = request.Birthday
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();
 
            return CreatedAtAction(nameof(LookupByPhone), new { phone = customer.Phone },
                new ApiResponse<CustomerDto>(true, "Tạo khách hàng thành công",
                    new CustomerDto(customer.Id, customer.FullName, customer.Phone, customer.Email, customer.Birthday, 0, "BRONZE")));
        }
    }
 
    // ================================================================
    // PAYMENTS CONTROLLER
    // ================================================================
 
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly CafeDbContext _db;
        public PaymentsController(CafeDbContext db) => _db = db;
 
        [HttpPost]
        public async Task<ActionResult<ApiResponse<PaymentDto>>> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            var order = await _db.Orders.FindAsync(request.OrderId);
            if (order == null)
                return NotFound(new ApiResponse<PaymentDto>(false, "Order không tồn tại", null));
 
            var validMethods = new[] { "CASH", "BANK", "CARD" };
            if (!validMethods.Contains(request.Method.ToUpper()))
                return BadRequest(new ApiResponse<PaymentDto>(false, "Phương thức thanh toán không hợp lệ", null));
 
            var payment = new Payment
            {
                OrderId = request.OrderId,
                Amount = request.Amount,
                Method = request.Method.ToUpper(),
                Status = "SUCCESS",
                Reference = request.Reference,
                PaidAt = DateTime.UtcNow
            };
            _db.Payments.Add(payment);
 
            // Cộng điểm nếu có khách hàng
            if (order.CustomerId.HasValue)
            {
                await _db.Database.ExecuteSqlRawAsync(
                    "EXEC sp_AddCustomerPoints @p0, @p1", order.CustomerId.Value, order.FinalAmount);
            }
 
            await _db.SaveChangesAsync();
 
            return CreatedAtAction(nameof(CreatePayment), new ApiResponse<PaymentDto>(true, "Thanh toán thành công",
                new PaymentDto(payment.Id, payment.OrderId, payment.Amount, payment.Method, payment.Status, payment.PaidAt)));
        }
 
        [HttpPost("refunds")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<RefundDto>>> CreateRefund([FromBody] CreateRefundRequest request)
        {
            var order = await _db.Orders.FindAsync(request.OrderId);
            if (order == null)
                return NotFound(new ApiResponse<RefundDto>(false, "Order không tồn tại", null));
 
            var refund = new Refund
            {
                OrderId = request.OrderId,
                PaymentId = request.PaymentId,
                Amount = request.Amount,
                Reason = request.Reason,
                CreatedAt = DateTime.UtcNow
            };
            _db.Refunds.Add(refund);
            await _db.SaveChangesAsync();
 
            return CreatedAtAction(nameof(CreateRefund), new ApiResponse<RefundDto>(true, "Hoàn tiền thành công",
                new RefundDto(refund.Id, refund.OrderId, refund.Amount, refund.Reason, refund.CreatedAt)));
        }
    }
 
    // ================================================================
    // INVENTORY CONTROLLER
    // ================================================================
 
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InventoryController : ControllerBase
    {
        private readonly CafeDbContext _db;
        public InventoryController(CafeDbContext db) => _db = db;
 
        [HttpGet("stock/{branchId:int}")]
        public async Task<ActionResult<ApiResponse<List<BranchIngredientDto>>>> GetStock(int branchId)
        {
            var stock = await _db.BranchIngredients
                .Include(bi => bi.Ingredient)
                .Where(bi => bi.BranchId == branchId)
                .Select(bi => new BranchIngredientDto(
                    bi.BranchId, bi.IngredientId, bi.Ingredient.Name,
                    bi.Ingredient.Unit, bi.CurrentStock, bi.CostPerUnit, bi.MinStockThreshold,
                    bi.CurrentStock <= bi.MinStockThreshold))
                .ToListAsync();
 
            return Ok(new ApiResponse<List<BranchIngredientDto>>(true, null, stock));
        }
 
        [HttpGet("low-stock/{branchId:int}")]
        public async Task<ActionResult<ApiResponse<List<BranchIngredientDto>>>> GetLowStock(int branchId)
        {
            var low = await _db.BranchIngredients
                .Include(bi => bi.Ingredient)
                .Where(bi => bi.BranchId == branchId && bi.CurrentStock <= bi.MinStockThreshold)
                .Select(bi => new BranchIngredientDto(
                    bi.BranchId, bi.IngredientId, bi.Ingredient.Name,
                    bi.Ingredient.Unit, bi.CurrentStock, bi.CostPerUnit, bi.MinStockThreshold, true))
                .ToListAsync();
 
            return Ok(new ApiResponse<List<BranchIngredientDto>>(true, null, low));
        }
 
        [HttpPost("import-receipts")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<ImportReceiptDto>>> CreateImportReceipt([FromBody] CreateImportReceiptRequest request)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var receipt = new ImportReceipt
                {
                    BranchId = request.BranchId,
                    SupplierId = request.SupplierId,
                    EmployeeId = request.EmployeeId,
                    Note = request.Note,
                    Status = "COMPLETED"
                };
                _db.ImportReceipts.Add(receipt);
                await _db.SaveChangesAsync();
 
                decimal total = 0;
                foreach (var item in request.Items)
                {
                    var receiptItem = new ImportReceiptItem
                    {
                        ImportReceiptId = receipt.Id,
                        IngredientId = item.IngredientId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    _db.ImportReceiptItems.Add(receiptItem);
                    total += item.Quantity * item.UnitPrice;
 
                    // Cập nhật tồn kho
                    var branchIngredient = await _db.BranchIngredients
                        .FirstOrDefaultAsync(bi => bi.BranchId == request.BranchId && bi.IngredientId == item.IngredientId);
 
                    if (branchIngredient == null)
                    {
                        _db.BranchIngredients.Add(new BranchIngredient
                        {
                            BranchId = request.BranchId,
                            IngredientId = item.IngredientId,
                            CurrentStock = item.Quantity,
                            CostPerUnit = item.UnitPrice
                        });
                    }
                    else
                    {
                        branchIngredient.CurrentStock += item.Quantity;
                        branchIngredient.CostPerUnit = item.UnitPrice; // Cập nhật giá mới nhất
                    }
 
                    // Ghi transaction kho
                    _db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        BranchId = request.BranchId,
                        IngredientId = item.IngredientId,
                        EmployeeId = request.EmployeeId,
                        ImportReceiptId = receipt.Id,
                        Type = "IMPORT",
                        Quantity = item.Quantity,
                        Note = $"Nhập kho theo phiếu #{receipt.Id}"
                    });
                }
 
                receipt.TotalAmount = total;
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
 
                return CreatedAtAction(nameof(CreateImportReceipt),
                    new ApiResponse<ImportReceiptDto>(true, "Nhập kho thành công", null));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
 
    // ================================================================
    // TABLES CONTROLLER
    // ================================================================
 
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TablesController : ControllerBase
    {
        private readonly CafeDbContext _db;
        public TablesController(CafeDbContext db) => _db = db;
 
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<TableDto>>>> GetTables([FromQuery] int? branchId, [FromQuery] string? status)
        {
            var query = _db.Tables.Include(t => t.Branch).AsQueryable();
            if (branchId.HasValue) query = query.Where(t => t.BranchId == branchId);
            if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status.ToUpper());
 
            var tables = await query
                .Select(t => new TableDto(t.Id, t.BranchId, t.Branch.Name, t.Name, t.Capacity, t.Status))
                .ToListAsync();
 
            return Ok(new ApiResponse<List<TableDto>>(true, null, tables));
        }
 
        [HttpPatch("{id:int}/status")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(int id, [FromBody] UpdateTableStatusRequest request)
        {
            var validStatuses = new[] { "EMPTY", "OCCUPIED", "CLEANING" };
            if (!validStatuses.Contains(request.Status.ToUpper()))
                return BadRequest(new ApiResponse<object>(false, "Trạng thái không hợp lệ", null));
 
            var table = await _db.Tables.FindAsync(id);
            if (table == null) return NotFound(new ApiResponse<object>(false, "Bàn không tồn tại", null));
 
            table.Status = request.Status.ToUpper();
            table.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
 
            return Ok(new ApiResponse<object>(true, "Cập nhật trạng thái bàn thành công", null));
        }
 
        [HttpPost]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<TableDto>>> CreateTable([FromBody] CreateTableRequest request)
        {
            var table = new TableCafe
            {
                BranchId = request.BranchId,
                Name = request.Name,
                Capacity = request.Capacity
            };
            _db.Tables.Add(table);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTables),
                new ApiResponse<TableDto>(true, "Tạo bàn thành công", null));
        }
    }
 
    // ================================================================
    // WORK SHIFTS CONTROLLER
    // ================================================================
 
    [ApiController]
    [Route("api/shifts")]
    [Authorize]
    public class WorkShiftsController : ControllerBase
    {
        private readonly CafeDbContext _db;
        public WorkShiftsController(CafeDbContext db) => _db = db;
 
        [HttpGet("current/{branchId:int}")]
        public async Task<ActionResult<ApiResponse<WorkShiftDto>>> GetCurrentShift(int branchId)
        {
            var shift = await _db.WorkShifts
                .Include(ws => ws.Employee)
                .FirstOrDefaultAsync(ws => ws.BranchId == branchId && ws.Status == "OPEN");
 
            if (shift == null)
                return NotFound(new ApiResponse<WorkShiftDto>(false, "Không có ca đang mở", null));
 
            return Ok(new ApiResponse<WorkShiftDto>(true, null, MapToShiftDto(shift)));
        }
 
        [HttpPost("open")]
        public async Task<ActionResult<ApiResponse<WorkShiftDto>>> OpenShift([FromBody] OpenShiftRequest request)
        {
            var existingOpen = await _db.WorkShifts
                .AnyAsync(ws => ws.BranchId == request.BranchId && ws.Status == "OPEN");
 
            if (existingOpen)
                return Conflict(new ApiResponse<WorkShiftDto>(false, "Chi nhánh đang có ca mở, vui lòng đóng ca trước", null));
 
            var shift = new WorkShift
            {
                BranchId = request.BranchId,
                EmployeeId = request.EmployeeId,
                StartingCash = request.StartingCash,
                StartTime = DateTime.UtcNow,
                Status = "OPEN"
            };
            _db.WorkShifts.Add(shift);
            await _db.SaveChangesAsync();
 
            return CreatedAtAction(nameof(GetCurrentShift), new { branchId = request.BranchId },
                new ApiResponse<WorkShiftDto>(true, "Mở ca thành công", MapToShiftDto(shift)));
        }
 
        [HttpPost("{id:long}/close")]
        public async Task<ActionResult<ApiResponse<WorkShiftDto>>> CloseShift(long id, [FromBody] CloseShiftRequest request)
        {
            var shift = await _db.WorkShifts.Include(ws => ws.Employee).FirstOrDefaultAsync(ws => ws.Id == id);
            if (shift == null || shift.Status != "OPEN")
                return BadRequest(new ApiResponse<WorkShiftDto>(false, "Ca không tồn tại hoặc đã đóng", null));
 
            var revenue = await _db.Orders
                .Where(o => o.ShiftId == id && o.OrderStatusId == 4) // COMPLETED
                .SumAsync(o => (decimal?)o.FinalAmount) ?? 0;
 
            shift.EndTime = DateTime.UtcNow;
            shift.Status = "CLOSED";
            shift.ActualEndingCash = request.ActualEndingCash;
            shift.ExpectedCash = shift.StartingCash + revenue;
            shift.Difference = request.ActualEndingCash - shift.ExpectedCash;
            shift.Note = request.Note;
            await _db.SaveChangesAsync();
 
            return Ok(new ApiResponse<WorkShiftDto>(true, "Đóng ca thành công", MapToShiftDto(shift)));
        }
 
        private static WorkShiftDto MapToShiftDto(WorkShift ws) => new(
            ws.Id, ws.BranchId, ws.EmployeeId, ws.Employee?.FullName ?? "",
            ws.StartTime, ws.EndTime,
            ws.StartingCash, ws.ActualEndingCash,
            ws.ExpectedCash, ws.Difference, ws.Status);
    }
 
    // ================================================================
    // DASHBOARD CONTROLLER
    // ================================================================
 
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly CafeDbContext _db;
        public DashboardController(CafeDbContext db) => _db = db;
 
        [HttpGet("today/{branchId:int}")]
        public async Task<ActionResult<ApiResponse<DashboardTodayDto>>> GetToday(int branchId)
        {
            var today = DateTime.Today;
            var orders = await _db.Orders
                .Include(o => o.OrderStatus)
                .Where(o => o.BranchId == branchId && o.CreatedAt.Date == today)
                .ToListAsync();
 
            var dto = new DashboardTodayDto(
                branchId,
                orders.Count,
                orders.Where(o => o.OrderStatus.Name == "COMPLETED").Sum(o => o.FinalAmount),
                orders.Where(o => o.OrderStatus.Name == "COMPLETED").Sum(o => o.CostAmount),
                orders.Where(o => o.OrderStatus.Name == "COMPLETED").Sum(o => o.FinalAmount - o.CostAmount),
                orders.Count(o => o.OrderStatus.Name == "CANCELLED"),
                orders.Any() ? orders.Average(o => o.FinalAmount) : 0);
 
            return Ok(new ApiResponse<DashboardTodayDto>(true, null, dto));
        }
 
        [HttpGet("top-items/{branchId:int}")]
        public async Task<ActionResult<ApiResponse<List<TopMenuItemDto>>>> GetTopItems(
            int branchId, [FromQuery] int days = 30, [FromQuery] int top = 10)
        {
            var since = DateTime.UtcNow.AddDays(-days);
            var items = await _db.OrderItems
                .Include(oi => oi.Order).ThenInclude(o => o.OrderStatus)
                .Include(oi => oi.MenuItem).ThenInclude(m => m.Category)
                .Where(oi =>
                    oi.Order.BranchId == branchId &&
                    oi.Order.OrderStatus.Name == "COMPLETED" &&
                    oi.Order.CreatedAt >= since)
                .GroupBy(oi => new
                {
                    oi.MenuItemId,
                    ItemName = oi.MenuItem.Name,          // Đặt tên là ItemName
                    CategoryName = oi.MenuItem.Category.Name // Đặt tên là CategoryName
                })
                .Select(g => new
                {
                    g.Key.MenuItemId,
                    ItemName = g.Key.ItemName,     // Gọi theo tên mới đã đặt ở trên
                    CategoryName = g.Key.CategoryName,
                    TotalQty = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.TotalPrice)
                })
                .OrderByDescending(x => x.TotalQty)
                .Take(top)
                .ToListAsync();
 
            var result = items.Select((x, idx) => new TopMenuItemDto(
                x.MenuItemId, x.ItemName, x.CategoryName,
                x.TotalQty, x.TotalRevenue, idx + 1)).ToList();
 
            return Ok(new ApiResponse<List<TopMenuItemDto>>(true, null, result));
        }
 
        [HttpGet("revenue-chart/{branchId:int}")]
        public async Task<ActionResult<ApiResponse<List<object>>>> GetRevenueChart(
            int branchId, [FromQuery] int days = 7)
        {
            var since = DateTime.Today.AddDays(-days + 1);
            var data = await _db.Orders
                .Where(o => o.BranchId == branchId &&
                            o.OrderStatusId == 4 &&
                            o.CreatedAt.Date >= since)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.FinalAmount), Orders = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();
 
            return Ok(new ApiResponse<List<object>>(true, null, data.Cast<object>().ToList()));
        }
    }
 
    // ================================================================
    // PROMOTIONS CONTROLLER
    // ================================================================
 
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PromotionsController : ControllerBase
    {
        private readonly CafeDbContext _db;
        public PromotionsController(CafeDbContext db) => _db = db;
 
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<PromotionDto>>>> GetPromotions([FromQuery] bool? activeOnly)
        {
            var query = _db.Promotions.AsQueryable();
            if (activeOnly == true)
            {
                var now = DateTime.UtcNow;
                query = query.Where(p =>
                    (p.StartDate == null || p.StartDate <= now) &&
                    (p.EndDate == null || p.EndDate >= now) &&
                    (p.UsageLimit == null || p.UsedCount < p.UsageLimit));
            }
 
            var promos = await query
                .Select(p => new PromotionDto(
                    p.Id, p.Name, p.Code, p.DiscountType, p.Value,
                    p.MinOrderValue, p.MaxDiscountValue,
                    p.UsageLimit, p.UsedCount, p.StartDate, p.EndDate))
                .ToListAsync();
 
            return Ok(new ApiResponse<List<PromotionDto>>(true, null, promos));
        }
 
        [HttpPost]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<PromotionDto>>> CreatePromotion([FromBody] CreatePromotionRequest request)
        {
            if (request.Code != null && await _db.Promotions.AnyAsync(p => p.Code == request.Code))
                return Conflict(new ApiResponse<PromotionDto>(false, "Mã khuyến mãi đã tồn tại", null));
 
            var promo = new Promotion
            {
                Name = request.Name,
                Code = request.Code,
                DiscountType = request.DiscountType.ToUpper(),
                Value = request.Value,
                MinOrderValue = request.MinOrderValue,
                MaxDiscountValue = request.MaxDiscountValue,
                UsageLimit = request.UsageLimit,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };
            _db.Promotions.Add(promo);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPromotions),
                new ApiResponse<PromotionDto>(true, "Tạo khuyến mãi thành công", null));
        }
    }
}