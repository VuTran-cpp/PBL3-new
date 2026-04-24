using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeManagement.Data;
using CafeManagement.DTOs;
using CafeManagement.Models;

namespace CafeManagement.Controllers
{
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
                BranchId     = request.BranchId,
                EmployeeId   = request.EmployeeId,
                StartingCash = request.StartingCash,
                StartTime    = DateTime.UtcNow,
                Status       = "OPEN"
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

            // ✅ FIX: So sánh theo tên trạng thái, không dùng hardcode ID = 4
            var revenue = await _db.Orders
                .Include(o => o.OrderStatus)
                .Where(o => o.ShiftId == id && o.OrderStatus.Name == "COMPLETED")
                .SumAsync(o => (decimal?)o.FinalAmount) ?? 0;

            shift.EndTime          = DateTime.UtcNow;
            shift.Status           = "CLOSED";
            shift.ActualEndingCash = request.ActualEndingCash;
            shift.ExpectedCash     = shift.StartingCash + revenue;
            shift.Difference       = request.ActualEndingCash - shift.ExpectedCash;
            shift.Note             = request.Note;
            await _db.SaveChangesAsync();

            return Ok(new ApiResponse<WorkShiftDto>(true, "Đóng ca thành công", MapToShiftDto(shift)));
        }

        [HttpGet("history/{branchId:int}")]
        public async Task<ActionResult<ApiResponse<List<WorkShiftDto>>>> GetShiftHistory(
            int branchId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var shifts = await _db.WorkShifts
                .Include(ws => ws.Employee)
                .Where(ws => ws.BranchId == branchId)
                .OrderByDescending(ws => ws.StartTime)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            return Ok(new ApiResponse<List<WorkShiftDto>>(true, null,
                shifts.Select(MapToShiftDto).ToList()));
        }

        private static WorkShiftDto MapToShiftDto(WorkShift ws) => new(
            ws.Id, ws.BranchId, ws.EmployeeId, ws.Employee?.FullName ?? "",
            ws.StartTime, ws.EndTime,
            ws.StartingCash, ws.ActualEndingCash,
            ws.ExpectedCash, ws.Difference, ws.Status);
    }
}
