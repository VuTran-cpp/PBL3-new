using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeManagement.Data;
using CafeManagement.DTOs;
using CafeManagement.Models;

namespace CafeManagement.Controllers
{
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
            var query = _db.Tables.Include(t => t.Branch)
                .Where(t => !t.IsDeleted)
                .AsQueryable();
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

            table.Status    = request.Status.ToUpper();
            table.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new ApiResponse<object>(true, "Cập nhật trạng thái bàn thành công", null));
        }

        [HttpPost]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<TableDto>>> CreateTable([FromBody] CreateTableRequest request)
        {
            var normalizedName = request.Name.Trim().ToLower();
            var exists = await _db.Tables.AnyAsync(t =>
                t.BranchId == request.BranchId &&
                !t.IsDeleted &&
                t.Name.ToLower() == normalizedName);
            if (exists)
            {
                return BadRequest(new ApiResponse<TableDto>(false, $"Bàn '{request.Name}' đã tồn tại ở chi nhánh này.", null));
            }

            var table = new TableCafe
            {
                BranchId = request.BranchId,
                Name     = request.Name.Trim(),
                Capacity = request.Capacity
            };
            _db.Tables.Add(table);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTables),
                new ApiResponse<TableDto>(true, "Tạo bàn thành công", null));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteTable(int id)
        {
            var table = await _db.Tables.FindAsync(id);
            if (table == null) return NotFound(new ApiResponse<object>(false, "Bàn không tồn tại", null));

            // Kiểm tra có order đang chạy không
            var hasActiveOrders = await _db.Orders
                .Include(o => o.OrderStatus)
                .AnyAsync(o => o.TableId == id &&
                               o.OrderStatus.Name != "COMPLETED" &&
                               o.OrderStatus.Name != "CANCELLED");
            if (hasActiveOrders)
                return Conflict(new ApiResponse<object>(false, "Bàn đang có đơn hàng, không thể xóa", null));

            table.IsDeleted = true;
            table.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new ApiResponse<object>(true, "Đã xóa bàn", null));
        }
    }
}
