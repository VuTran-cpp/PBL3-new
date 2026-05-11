using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeManagement.Data;
using CafeManagement.DTOs;

namespace CafeManagement.Controllers
{
    // ================================================================
    // DASHBOARD CONTROLLER
    // ✅ FIX: Dùng DateTime.UtcNow.Date thay vì DateTime.Today
    //         để đảm bảo nhất quán với dữ liệu lưu UTC trong DB.
    //         DateTime.Today trả về giờ local → miss đơn hàng 0h–7h (UTC+7).
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
            // ✅ FIX: Dùng UtcNow.Date để nhất quán với CreatedAt (lưu UTC trong DB)
            var todayUtc = DateTime.UtcNow.Date;
            var tomorrowUtc = todayUtc.AddDays(1);

            var orders = await _db.Orders
                .Include(o => o.OrderStatus)
                .Where(o => o.BranchId == branchId
                         && o.CreatedAt >= todayUtc
                         && o.CreatedAt < tomorrowUtc)
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
                    ItemName     = oi.MenuItem.Name,
                    CategoryName = oi.MenuItem.Category.Name
                })
                .Select(g => new
                {
                    g.Key.MenuItemId,
                    ItemName     = g.Key.ItemName,
                    CategoryName = g.Key.CategoryName,
                    TotalQty     = g.Sum(x => x.Quantity),
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
            // ✅ FIX: Đã sửa lỗi corrupted code; dùng UTC range thay vì .Date comparison
            var sinceUtc   = DateTime.UtcNow.Date.AddDays(-days + 1);
            var tomorrowUtc = DateTime.UtcNow.Date.AddDays(1);

            var data = await _db.Orders
                .Include(o => o.OrderStatus)
                .Where(o => o.BranchId == branchId &&
                            o.OrderStatus.Name == "COMPLETED" &&
                            o.CreatedAt >= sinceUtc &&
                            o.CreatedAt < tomorrowUtc)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date    = g.Key,
                    Revenue = g.Sum(o => o.FinalAmount),
                    Orders  = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return Ok(new ApiResponse<List<object>>(true, null, data.Cast<object>().ToList()));
        }

        [HttpGet("pending-orders/{branchId:int}")]
        public async Task<ActionResult<ApiResponse<int>>> GetPendingCount(int branchId)
        {
            var count = await _db.Orders
                .Include(o => o.OrderStatus)
                .CountAsync(o => o.BranchId == branchId &&
                                 (o.OrderStatus.Name == "PENDING" || o.OrderStatus.Name == "COOKING"));
            return Ok(new ApiResponse<int>(true, null, count));
        }
    }
}
