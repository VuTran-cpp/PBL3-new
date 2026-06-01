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

        // Helper: tính doanh thu của 1 ca
        private async Task<decimal> CalcShiftRevenue(long shiftId)
        {
            return await _db.Orders
                .Include(o => o.OrderStatus)
                .Where(o => o.ShiftId == shiftId && o.OrderStatus.Name == "COMPLETED")
                .SumAsync(o => (decimal?)o.FinalAmount) ?? 0;
        }

        [HttpGet("current/{branchId:int}")]
        public async Task<ActionResult<ApiResponse<WorkShiftDto>>> GetCurrentShift(int branchId)
        {
            var shift = await _db.WorkShifts
                .Include(ws => ws.Employee)
                .FirstOrDefaultAsync(ws => ws.BranchId == branchId && ws.Status == "OPEN");

            if (shift == null)
                return NotFound(new ApiResponse<WorkShiftDto>(false, "Không có ca đang mở", null));

            var revenue = await CalcShiftRevenue(shift.Id);
            return Ok(new ApiResponse<WorkShiftDto>(true, null, MapToShiftDto(shift, revenue)));
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
                new ApiResponse<WorkShiftDto>(true, "Mở ca thành công", MapToShiftDto(shift, 0)));
        }

        [HttpPost("{id:long}/close")]
        public async Task<ActionResult<ApiResponse<WorkShiftDto>>> CloseShift(long id, [FromBody] CloseShiftRequest request)
        {
            var shift = await _db.WorkShifts.Include(ws => ws.Employee).FirstOrDefaultAsync(ws => ws.Id == id);
            if (shift == null || shift.Status != "OPEN")
                return BadRequest(new ApiResponse<WorkShiftDto>(false, "Ca không tồn tại hoặc đã đóng", null));

            // Tính doanh thu ca dựa trên ShiftId
            var revenue = await CalcShiftRevenue(id);

            shift.EndTime          = DateTime.UtcNow;
            shift.Status           = "CLOSED";
            shift.ActualEndingCash = request.ActualEndingCash;
            shift.ExpectedCash     = shift.StartingCash + revenue;
            shift.Difference       = request.ActualEndingCash - shift.ExpectedCash;
            shift.Note             = request.Note;
            await _db.SaveChangesAsync();

            return Ok(new ApiResponse<WorkShiftDto>(true, "Đóng ca thành công", MapToShiftDto(shift, revenue)));
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

            // Tính revenue cho từng ca
            var shiftIds = shifts.Select(s => s.Id).ToList();
            var revenueMap = await _db.Orders
                .Include(o => o.OrderStatus)
                .Where(o => o.ShiftId != null && shiftIds.Contains(o.ShiftId.Value)
                         && o.OrderStatus.Name == "COMPLETED")
                .GroupBy(o => o.ShiftId!.Value)
                .Select(g => new { ShiftId = g.Key, Revenue = g.Sum(o => o.FinalAmount) })
                .ToDictionaryAsync(x => x.ShiftId, x => x.Revenue);

            var dtos = shifts.Select(s =>
                MapToShiftDto(s, revenueMap.GetValueOrDefault(s.Id, 0))
            ).ToList();

            return Ok(new ApiResponse<List<WorkShiftDto>>(true, null, dtos));
        }

        [HttpGet("daily-stats/{branchId:int}")]
        public async Task<ActionResult<ApiResponse<List<DailyShiftStatDto>>>> GetDailyShiftStats(
            int branchId, [FromQuery] int days = 30)
        {
            var cutoffDate = DateTime.UtcNow.Date.AddDays(-days);

            var shifts = await _db.WorkShifts
                .Where(ws => ws.BranchId == branchId && ws.StartTime >= cutoffDate)
                .OrderByDescending(ws => ws.StartTime)
                .ToListAsync();

            if (!shifts.Any())
                return Ok(new ApiResponse<List<DailyShiftStatDto>>(true, null, new List<DailyShiftStatDto>()));

            var shiftIds = shifts.Select(s => s.Id).ToList();

            var revenueMap = await _db.Orders
                .Include(o => o.OrderStatus)
                .Where(o => o.ShiftId != null && shiftIds.Contains(o.ShiftId.Value)
                         && o.OrderStatus.Name == "COMPLETED")
                .GroupBy(o => o.ShiftId!.Value)
                .Select(g => new { ShiftId = g.Key, Revenue = g.Sum(o => o.FinalAmount) })
                .ToDictionaryAsync(x => x.ShiftId, x => x.Revenue);

            var stats = shifts
                .GroupBy(s => s.StartTime.Date)
                .Select(g => {
                    var totalStarting = g.Sum(s => s.StartingCash);
                    var totalRevenue  = g.Sum(s => revenueMap.GetValueOrDefault(s.Id, 0));
                    var totalActual   = g.Sum(s => s.ActualEndingCash ?? 0);
                    var totalExpected = totalStarting + totalRevenue;
                    var totalDiff     = g.Sum(s => s.Difference ?? 0);

                    return new DailyShiftStatDto(
                        g.Key.ToString("yyyy-MM-dd"),
                        g.Count(),
                        totalStarting,
                        totalRevenue,
                        totalActual,
                        totalExpected,
                        totalDiff
                    );
                })
                .OrderByDescending(x => x.Date)
                .ToList();

            return Ok(new ApiResponse<List<DailyShiftStatDto>>(true, null, stats));
        }

        [HttpGet("{id:long}/employee-breakdown")]
        public async Task<ActionResult<ApiResponse<List<EmployeeRevenueBreakdownDto>>>> GetEmployeeBreakdown(long id)
        {
            var pendingStatus = await _db.OrderStatuses.FirstOrDefaultAsync(s => s.Name == "PENDING");
            var pendingStatusId = pendingStatus?.Id ?? 1;

            var completedOrders = await _db.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.Payments)
                .Include(o => o.StatusHistories)
                    .ThenInclude(h => h.Account)
                        .ThenInclude(a => a.Employee)
                .Where(o => o.ShiftId == id && o.OrderStatus.Name == "COMPLETED")
                .ToListAsync();

            var shift = await _db.WorkShifts.Include(ws => ws.Employee).FirstOrDefaultAsync(ws => ws.Id == id);
            var defaultEmployeeName = shift?.Employee?.FullName ?? "Nhân viên ca";
            var defaultEmployeeId = shift?.EmployeeId ?? 0;

            var breakdownList = completedOrders
                .Select(o => {
                    var creator = o.StatusHistories
                        .OrderBy(h => h.ChangedAt)
                        .FirstOrDefault();
                    
                    var empId = creator?.Account?.EmployeeId ?? defaultEmployeeId;
                    var empName = creator?.Account?.Employee?.FullName ?? defaultEmployeeName;

                    var payments = o.Payments.Where(p => p.Status.ToUpper() == "SUCCESS").ToList();
                    decimal cashAmt = 0;
                    decimal transAmt = 0;
                    if (payments.Count == 0)
                    {
                        cashAmt = o.FinalAmount;
                    }
                    else
                    {
                        cashAmt = payments.Where(p => p.Method.ToUpper() == "CASH").Sum(p => p.Amount);
                        transAmt = payments.Where(p => p.Method.ToUpper() == "BANK" || p.Method.ToUpper() == "CARD").Sum(p => p.Amount);
                    }

                    return new { 
                        EmployeeId = empId, 
                        EmployeeName = empName, 
                        FinalAmount = o.FinalAmount,
                        CashAmount = cashAmt,
                        TransferAmount = transAmt
                    };
                })
                .GroupBy(x => new { x.EmployeeId, x.EmployeeName })
                .Select(g => new EmployeeRevenueBreakdownDto(
                    g.Key.EmployeeId,
                    g.Key.EmployeeName,
                    g.Count(),
                    g.Sum(x => x.FinalAmount),
                    g.Sum(x => x.CashAmount),
                    g.Sum(x => x.TransferAmount)
                ))
                .ToList();

            return Ok(new ApiResponse<List<EmployeeRevenueBreakdownDto>>(true, null, breakdownList));
        }

        private static WorkShiftDto MapToShiftDto(WorkShift ws, decimal revenue) => new(
            ws.Id, ws.BranchId, ws.EmployeeId, ws.Employee?.FullName ?? "",
            ws.StartTime, ws.EndTime,
            ws.StartingCash, ws.ActualEndingCash,
            ws.ExpectedCash, ws.Difference, ws.Status, revenue);
    }
}

