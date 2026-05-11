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

        /// <summary>Lấy danh sách orders theo branch, ngày, trạng thái</summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<OrderSummaryDto>>>> GetOrders(
            [FromQuery] int? branchId,
            [FromQuery] string? status,
            [FromQuery] string? orderType,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _db.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.Table)
                .Include(o => o.Customer)
                .AsQueryable();

            if (branchId.HasValue) query = query.Where(o => o.BranchId == branchId);
            if (!string.IsNullOrEmpty(status)) query = query.Where(o => o.OrderStatus.Name == status.ToUpper());
            if (!string.IsNullOrEmpty(orderType)) query = query.Where(o => o.OrderType == orderType.ToUpper());
            if (fromDate.HasValue) query = query.Where(o => o.CreatedAt >= fromDate);
            if (toDate.HasValue) query = query.Where(o => o.CreatedAt <= toDate);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderSummaryDto(
                    o.Id, o.OrderType, o.OrderStatus.Name,
                    o.Table != null ? o.Table.Name : null,
                    o.Customer != null ? o.Customer.FullName : null,
                    o.FinalAmount, o.CreatedAt))
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

            // Kiểm tra bàn (nếu DINE_IN)
            if (request.OrderType == "DINE_IN" && request.TableId.HasValue)
            {
                var table = await _db.Tables.FindAsync(request.TableId.Value);
                if (table == null)
                    return BadRequest(new ApiResponse<OrderDto>(false, "Bàn không tồn tại", null));
                if (table.Status == "OCCUPIED")
                    return BadRequest(new ApiResponse<OrderDto>(false, $"Bàn {table.Name} đang được sử dụng", null));
            }

            var order = new Order
            {
                BranchId = request.BranchId,
                ShiftId = request.ShiftId,
                CustomerId = request.CustomerId,
                OrderStatusId = 1, // PENDING
                TableId = request.TableId,
                OrderType = request.OrderType.ToUpper(),
                Note = request.Note,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // Ghi status history
            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                StatusId = 1,
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
            var order = await _db.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new ApiResponse<OrderDto>(false, "Order không tồn tại", null));

            if (!new[] { 1, 2 }.Contains(order.OrderStatusId)) // PENDING or IN_PROGRESS
                return BadRequest(new ApiResponse<OrderDto>(false, "Không thể thêm món ở trạng thái hiện tại", null));

            var menuItem = await _db.MenuItems.FindAsync(request.MenuItemId);
            if (menuItem == null || !menuItem.IsAvailable)
                return BadRequest(new ApiResponse<OrderDto>(false, "Món không tồn tại hoặc đã ngừng bán", null));

            var orderItem = new OrderItem
            {
                OrderId = id,
                MenuItemId = request.MenuItemId,
                Quantity = request.Quantity,
                UnitPrice = menuItem.Price,
                Note = request.Note
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
                        OptionId = optionId
                    });
                }
            }

            // Tính lại tổng tiền
            await RecalculateOrderAsync(id);
            await _db.SaveChangesAsync();

            return Ok(new ApiResponse<OrderDto>(true, "Thêm món thành công", await GetOrderDtoAsync(id)));
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
                (p.EndDate == null || p.EndDate >= now) &&
                (p.UsageLimit == null || p.UsedCount < p.UsageLimit));

            if (promotion == null)
                return BadRequest(new ApiResponse<OrderDto>(false, "Mã khuyến mãi không hợp lệ hoặc đã hết hạn", null));

            if (promotion.MinOrderValue.HasValue && order.SubTotal < promotion.MinOrderValue)
                return BadRequest(new ApiResponse<OrderDto>(false,
                    $"Đơn hàng cần tối thiểu {promotion.MinOrderValue:N0}đ để áp dụng mã này", null));

            // Tính giá trị giảm
            decimal discountValue = promotion.DiscountType == "PERCENTAGE"
                ? Math.Min(order.SubTotal * promotion.Value / 100,
                    promotion.MaxDiscountValue ?? decimal.MaxValue)
                : promotion.Value;

            // Kiểm tra đã áp dụng chưa
            var existing = await _db.OrderPromotions
                .FirstOrDefaultAsync(op => op.OrderId == id && op.PromotionId == promotion.Id);
            if (existing != null)
                return BadRequest(new ApiResponse<OrderDto>(false, "Mã khuyến mãi đã được áp dụng", null));

            _db.OrderPromotions.Add(new OrderPromotion
            {
                OrderId = id,
                PromotionId = promotion.Id,
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
            var validStatuses = new[] { "PENDING", "IN_PROGRESS", "READY", "COMPLETED", "CANCELLED" };
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

            // Nếu COMPLETED: dùng SP để trừ kho
            if (request.Status.ToUpper() == "COMPLETED")
            {
                await _db.Database.ExecuteSqlRawAsync(
                    "EXEC sp_CompleteOrder @p0, @p1", id, accountId ?? 0);
            }
            else
            {
                order.OrderStatusId = statusEntity.Id;
                order.UpdatedAt = DateTime.UtcNow;

                _db.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = id,
                    StatusId = statusEntity.Id,
                    ChangedBy = accountId,
                    Note = request.Note,
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
            await _db.SaveChangesAsync();

            return Ok(new ApiResponse<object>(true, "Đã xóa order", null));
        }

        // ── Private Helpers ────────────────────────────────────────

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
                order.SubTotal = subTotal;
                order.DiscountAmount = discount;
                order.FinalAmount = Math.Max(0, subTotal - discount);
                order.UpdatedAt = DateTime.UtcNow;
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