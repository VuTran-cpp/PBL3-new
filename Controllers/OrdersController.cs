using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CafeManagement.Data;
using CafeManagement.DTOs;
using CafeManagement.Models;

namespace CafeManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly CafeDbContext _db;

        public OrdersController(CafeDbContext db) => _db = db;

        // ── GET: Danh sách orders (có filter + phân trang) ────────

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<OrderSummaryDto>>>> GetOrders(
            [FromQuery] int? branchId,
            [FromQuery] string? status,
            [FromQuery] string? orderType,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] long? shiftId,
            [FromQuery] int? employeeId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _db.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.Table)
                .Include(o => o.Customer)
                .Where(o => !o.IsDeleted)
                .AsQueryable();

            if (branchId.HasValue) query = query.Where(o => o.BranchId == branchId);
            if (shiftId.HasValue)  query = query.Where(o => o.ShiftId == shiftId);
            if (employeeId.HasValue)
            {
                var employeeAccountIds = _db.Accounts
                    .Where(a => a.EmployeeId == employeeId.Value)
                    .Select(a => a.Id);

                query = query.Where(o => 
                    (o.WorkShift != null && o.WorkShift.EmployeeId == employeeId.Value) ||
                    o.StatusHistories.Any(h => h.ChangedBy.HasValue && employeeAccountIds.Contains(h.ChangedBy.Value))
                );
            }
            if (!string.IsNullOrEmpty(status))    query = query.Where(o => o.OrderStatus.Name == status.ToUpper());
            if (!string.IsNullOrEmpty(orderType)) query = query.Where(o => o.OrderType == orderType.ToUpper());
            if (fromDate.HasValue) query = query.Where(o => o.CreatedAt >= fromDate);
            if (toDate.HasValue)   query = query.Where(o => o.CreatedAt <= toDate);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderSummaryDto(
                    o.Id, o.OrderType, o.OrderStatus.Name,
                    o.TableId,
                    o.Table    != null ? o.Table.Name    : null,
                    o.Customer != null ? o.Customer.FullName : null,
                    o.FinalAmount, o.CreatedAt, o.Note))
                .ToListAsync();

            return Ok(new ApiResponse<PagedResult<OrderSummaryDto>>(
                true, null, new PagedResult<OrderSummaryDto>(items, total, page, pageSize)));
        }

        // ── GET: Chi tiết 1 order ─────────────────────────────────

        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrder(long id)
        {
            var order = await _db.Orders
                .Include(o => o.Branch)
                .Include(o => o.WorkShift)
                .Include(o => o.Customer)
                .Include(o => o.OrderStatus)
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Options)
                        .ThenInclude(oio => oio.Option)
                .Include(o => o.OrderPromotions)
                    .ThenInclude(op => op.Promotion)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound(new ApiResponse<OrderDto>(false, $"Không tìm thấy order #{id}", null));

            return Ok(new ApiResponse<OrderDto>(true, null, MapToOrderDto(order)));
        }

        // ── POST: Tạo order mới ───────────────────────────────────

        [HttpPost]
        public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrder([FromBody] CreateOrderRequest request)
        {
            // Kiểm tra shift đang mở
            var shift = await _db.WorkShifts
                .FirstOrDefaultAsync(ws => ws.Id == request.ShiftId && ws.Status == "OPEN");
            if (shift == null)
                return BadRequest(new ApiResponse<OrderDto>(false, "Ca làm việc không hợp lệ hoặc đã đóng", null));

            // ✅ FIX BUG-06: DINE_IN bắt buộc phải có TableId
            if (request.OrderType.ToUpper() == "DINE_IN" && !request.TableId.HasValue)
                return BadRequest(new ApiResponse<OrderDto>(false, "Đơn ăn tại chỗ (DINE_IN) phải chọn bàn", null));

            // Kiểm tra bàn (nếu DINE_IN)
            if (request.OrderType.ToUpper() == "DINE_IN" && request.TableId.HasValue)
            {
                var table = await _db.Tables.FindAsync(request.TableId.Value);
                if (table == null)
                    return BadRequest(new ApiResponse<OrderDto>(false, "Bàn không tồn tại", null));
                if (table.Status == "OCCUPIED")
                    return BadRequest(new ApiResponse<OrderDto>(false, $"Bàn {table.Name} đang được sử dụng", null));
            }

            // ✅ FIX: Tra cứu trạng thái PENDING từ DB thay vì hardcode ID = 1
            var pendingStatus = await _db.OrderStatuses.FirstOrDefaultAsync(s => s.Name == "PENDING");
            if (pendingStatus == null)
                return StatusCode(500, new ApiResponse<OrderDto>(false, "Cấu hình trạng thái đơn hàng bị thiếu", null));

            var order = new Order
            {
                BranchId      = request.BranchId,
                ShiftId       = request.ShiftId,
                CustomerId    = request.CustomerId,
                OrderStatusId = pendingStatus.Id,   // ✅ dynamic lookup
                TableId       = request.TableId,
                OrderType     = request.OrderType.ToUpper(),
                Note          = request.Note,
                CreatedAt     = DateTime.UtcNow,
                UpdatedAt     = DateTime.UtcNow
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // Ghi status history
            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId   = order.Id,
                StatusId  = pendingStatus.Id,   // ✅ dynamic lookup
                ChangedBy = GetAccountId(),
                ChangedAt = DateTime.UtcNow
            });

            // Cập nhật trạng thái bàn
            if (request.OrderType == "DINE_IN" && request.TableId.HasValue)
            {
                var table = await _db.Tables.FindAsync(request.TableId.Value);
                if (table != null) { table.Status = "OCCUPIED"; table.UpdatedAt = DateTime.UtcNow; }
            }

            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id },
                new ApiResponse<OrderDto>(true, "Tạo order thành công", await GetOrderDtoAsync(order.Id)));
        }

        // ── POST: Thêm món vào order ──────────────────────────────

        [HttpPost("{id:long}/items")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> AddItem(long id, [FromBody] AddOrderItemRequest request)
        {
            var order = await _db.Orders
                .Include(o => o.OrderStatus)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
                return NotFound(new ApiResponse<OrderDto>(false, "Order không tồn tại", null));

            // ✅ FIX: So sánh theo tên trạng thái, không dùng hardcode ID {1,2,3}
            var allowedStatuses = new[] { "PENDING", "COOKING", "SERVED" };
            if (!allowedStatuses.Contains(order.OrderStatus.Name))
                return BadRequest(new ApiResponse<OrderDto>(false, "Không thể thêm món ở trạng thái hiện tại", null));

            var menuItem = await _db.MenuItems.FindAsync(request.MenuItemId);
            if (menuItem == null || !menuItem.IsAvailable)
                return BadRequest(new ApiResponse<OrderDto>(false, "Món không tồn tại hoặc đã ngừng bán", null));

            var orderItem = new OrderItem
            {
                OrderId    = id,
                MenuItemId = request.MenuItemId,
                Quantity   = request.Quantity,
                UnitPrice  = menuItem.Price,
                Note       = request.Note    // ✅ Note per item
            };
            _db.OrderItems.Add(orderItem);
            await _db.SaveChangesAsync();

            // Thêm options nếu có
            if (request.OptionIds != null && request.OptionIds.Any())
            {
                foreach (var optionId in request.OptionIds)
                {
                    _db.OrderItemOptions.Add(new OrderItemOption
                    {
                        OrderItemId = orderItem.Id,
                        OptionId    = optionId
                    });
                }
            }

            await RecalculateOrderAsync(id);
            await _db.SaveChangesAsync();

            return Ok(new ApiResponse<OrderDto>(true, "Thêm món thành công", await GetOrderDtoAsync(id)));
        }

        // ── PATCH: Cập nhật ghi chú món ──────────────────────────

        [HttpPatch("{id:long}/items/{itemId:long}/note")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> UpdateItemNote(long id, long itemId, [FromBody] UpdateItemNoteRequest request)
        {
            var item = await _db.OrderItems.FirstOrDefaultAsync(oi => oi.Id == itemId && oi.OrderId == id);
            if (item == null)
                return NotFound(new ApiResponse<OrderDto>(false, "Không tìm thấy món", null));

            item.Note = request.Note;
            await _db.SaveChangesAsync();

            return Ok(new ApiResponse<OrderDto>(true, "Đã cập nhật ghi chú", await GetOrderDtoAsync(id)));
        }

        // ── DELETE: Xóa món khỏi order ───────────────────────────

        [HttpDelete("{id:long}/items/{itemId:long}")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> RemoveItem(long id, long itemId)
        {
            var item = await _db.OrderItems
                .Include(oi => oi.Options)
                .FirstOrDefaultAsync(oi => oi.Id == itemId && oi.OrderId == id);

            if (item == null)
                return NotFound(new ApiResponse<OrderDto>(false, "Không tìm thấy item", null));

            _db.OrderItemOptions.RemoveRange(item.Options);
            _db.OrderItems.Remove(item);
            await RecalculateOrderAsync(id);
            await _db.SaveChangesAsync();

            return Ok(new ApiResponse<OrderDto>(true, "Đã xóa món", await GetOrderDtoAsync(id)));
        }

        // ── POST: Áp dụng khuyến mãi ─────────────────────────────

        [HttpPost("{id:long}/promotions")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> ApplyPromotion(long id, [FromBody] ApplyPromotionRequest request)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new ApiResponse<OrderDto>(false, "Order không tồn tại", null));

            var now = DateTime.UtcNow;
            var promotion = await _db.Promotions.FirstOrDefaultAsync(p =>
                p.Code == request.PromotionCode &&
                (p.StartDate == null || p.StartDate <= now) &&
                (p.EndDate   == null || p.EndDate   >= now) &&
                (p.UsageLimit == null || p.UsedCount < p.UsageLimit));

            if (promotion == null)
                return BadRequest(new ApiResponse<OrderDto>(false, "Mã khuyến mãi không hợp lệ hoặc đã hết hạn", null));

            if (promotion.MinOrderValue.HasValue && order.SubTotal < promotion.MinOrderValue)
                return BadRequest(new ApiResponse<OrderDto>(false,
                    $"Đơn hàng cần tối thiểu {promotion.MinOrderValue:N0}đ để áp dụng mã này", null));

            decimal discountValue = promotion.DiscountType == "PERCENT"
                ? Math.Min(order.SubTotal * promotion.Value / 100,
                    promotion.MaxDiscountValue ?? decimal.MaxValue)
                : promotion.Value;

            var existing = await _db.OrderPromotions
                .FirstOrDefaultAsync(op => op.OrderId == id && op.PromotionId == promotion.Id);
            if (existing != null)
                return BadRequest(new ApiResponse<OrderDto>(false, "Mã khuyến mãi đã được áp dụng", null));

            _db.OrderPromotions.Add(new OrderPromotion
            {
                OrderId       = id,
                PromotionId   = promotion.Id,
                DiscountValue = discountValue
            });
            promotion.UsedCount++;

            await RecalculateOrderAsync(id);
            await _db.SaveChangesAsync();

            return Ok(new ApiResponse<OrderDto>(true, $"Áp dụng mã thành công, giảm {discountValue:N0}đ",
                await GetOrderDtoAsync(id)));
        }

        // ── PATCH: Cập nhật trạng thái order ─────────────────────

        [HttpPatch("{id:long}/status")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> UpdateStatus(long id, [FromBody] UpdateOrderStatusRequest request)
        {
            var validStatuses = new[] { "PENDING", "COOKING", "SERVED", "COMPLETED", "CANCELLED" };
            if (!validStatuses.Contains(request.Status.ToUpper()))
                return BadRequest(new ApiResponse<OrderDto>(false, "Trạng thái không hợp lệ", null));

            var order = await _db.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new ApiResponse<OrderDto>(false, "Order không tồn tại", null));

            var statusEntity = await _db.OrderStatuses
                .FirstOrDefaultAsync(s => s.Name == request.Status.ToUpper());
            if (statusEntity == null)
                return BadRequest(new ApiResponse<OrderDto>(false, "Trạng thái không tồn tại trong hệ thống", null));

            var accountId = GetAccountId();

            if (request.Status.ToUpper() == "COMPLETED")
            {
                // ✅ FIX: Thay thế stored procedure bằng EF trực tiếp (portable, không phụ thuộc SP)
                order.OrderStatusId = statusEntity.Id;
                order.UpdatedAt     = DateTime.UtcNow;

                _db.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId   = id,
                    StatusId  = statusEntity.Id,
                    ChangedBy = accountId,
                    Note      = request.Note ?? "Đơn hàng hoàn thành",
                    ChangedAt = DateTime.UtcNow
                });

                // ✅ NEW: Kết nối recipe → kho — tự động trừ nguyên liệu khi hoàn thành đơn
                // Resolve employeeId from accountId (InventoryTransaction.EmployeeId is FK to Employee, not Account)
                int? employeeId = null;
                if (accountId.HasValue)
                {
                    var acct = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId.Value);
                    employeeId = acct?.EmployeeId;
                }
                await DeductIngredientStockAsync(id, order.BranchId, employeeId);

                // Trả bàn về EMPTY khi COMPLETED
                if (order.TableId.HasValue)
                {
                    var table = await _db.Tables.FindAsync(order.TableId.Value);
                    if (table != null) { table.Status = "EMPTY"; table.UpdatedAt = DateTime.UtcNow; }
                }

                await _db.SaveChangesAsync();
            }
            else
            {
                order.OrderStatusId = statusEntity.Id;
                order.UpdatedAt     = DateTime.UtcNow;

                _db.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId   = id,
                    StatusId  = statusEntity.Id,
                    ChangedBy = accountId,
                    Note      = request.Note,
                    ChangedAt = DateTime.UtcNow
                });

                // Nếu CANCELLED: trả bàn về EMPTY
                if (request.Status.ToUpper() == "CANCELLED" && order.TableId.HasValue)
                {
                    var table = await _db.Tables.FindAsync(order.TableId.Value);
                    if (table != null) { table.Status = "EMPTY"; table.UpdatedAt = DateTime.UtcNow; }
                }

                await _db.SaveChangesAsync();
            }

            return Ok(new ApiResponse<OrderDto>(true, "Cập nhật trạng thái thành công", await GetOrderDtoAsync(id)));
        }

        // ── DELETE: Soft delete order ─────────────────────────────

        [HttpDelete("{id:long}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteOrder(long id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new ApiResponse<object>(false, "Order không tồn tại", null));

            order.IsDeleted = true;
            order.DeletedAt = DateTime.UtcNow;

            // ✅ FIX BUG-10: Trả bàn về EMPTY khi xóa order (tránh bàn bị khóa mãi)
            if (order.TableId.HasValue)
            {
                var table = await _db.Tables.FindAsync(order.TableId.Value);
                if (table != null) { table.Status = "EMPTY"; table.UpdatedAt = DateTime.UtcNow; }
            }

            await _db.SaveChangesAsync();

            return Ok(new ApiResponse<object>(true, "Đã xóa order", null));
        }

        // ── Private Helpers ────────────────────────────────────────

        /// <summary>
        /// Trừ nguyên liệu kho khi đơn hàng hoàn thành.
        /// Mỗi OrderItem → tra Recipe → trừ BranchIngredient.CurrentStock.
        /// </summary>
        private async Task DeductIngredientStockAsync(long orderId, int branchId, int? employeeId)
        {
            var orderItems = await _db.OrderItems
                .Include(oi => oi.MenuItem)
                    .ThenInclude(m => m.Recipes)
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();

            foreach (var item in orderItems)
            {
                foreach (var recipe in item.MenuItem.Recipes)
                {
                    var bi = await _db.BranchIngredients
                        .FirstOrDefaultAsync(x => x.BranchId == branchId && x.IngredientId == recipe.IngredientId);

                    if (bi == null) continue; // Chưa cấu hình kho cho nguyên liệu này

                    var qtyUsed = recipe.QuantityRequired * item.Quantity;
                    bi.CurrentStock = Math.Max(0, bi.CurrentStock - qtyUsed);

                    _db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        BranchId     = branchId,
                        IngredientId = recipe.IngredientId,
                        EmployeeId   = employeeId,
                        Type         = "USAGE",
                        Quantity     = qtyUsed,
                        Note         = $"Bán {item.Quantity}x {item.MenuItem.Name} (Order #{orderId})",
                        CreatedAt    = DateTime.UtcNow,
                        UpdatedAt    = DateTime.UtcNow
                    });
                }
            }
        }

        private async Task RecalculateOrderAsync(long orderId)
        {
            var subTotal = await _db.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .SumAsync(oi => (decimal?)oi.TotalPrice) ?? 0;

            var discount = await _db.OrderPromotions
                .Where(op => op.OrderId == orderId)
                .SumAsync(op => (decimal?)op.DiscountValue) ?? 0;

            var order = await _db.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.SubTotal       = subTotal;
                order.DiscountAmount = discount;
                order.FinalAmount    = Math.Max(0, subTotal - discount);
                order.UpdatedAt      = DateTime.UtcNow;
            }
        }

        private async Task<OrderDto> GetOrderDtoAsync(long orderId)
        {
            var order = await _db.Orders
                .Include(o => o.Branch)
                .Include(o => o.WorkShift)
                .Include(o => o.Customer)
                .Include(o => o.OrderStatus)
                .Include(o => o.Table)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Options).ThenInclude(oio => oio.Option)
                .Include(o => o.OrderPromotions).ThenInclude(op => op.Promotion)
                .FirstAsync(o => o.Id == orderId);

            return MapToOrderDto(order);
        }

        private static OrderDto MapToOrderDto(Order o) => new(
            o.Id, o.BranchId, o.Branch.Name,
            o.ShiftId, o.CustomerId, o.Customer?.FullName,
            o.OrderStatus.Name, o.TableId, o.Table?.Name,
            o.OrderType, o.SubTotal, o.DiscountAmount,
            o.FinalAmount, o.CostAmount, o.Note, o.CreatedAt,
            o.OrderItems.Select(oi => new OrderItemDto(
                oi.Id, oi.MenuItemId, oi.MenuItem.Name,
                oi.Quantity, oi.UnitPrice, oi.TotalPrice,
                oi.Note,
                oi.Options.Select(opt => opt.Option.Name).ToList()
            )).ToList(),
            o.OrderPromotions.Select(op => new OrderPromotionDto(
                op.PromotionId, op.Promotion.Name, op.DiscountValue
            )).ToList()
        );

        private int? GetAccountId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim) : null;
        }
    }
}