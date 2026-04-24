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
    public class EmployeesController : ControllerBase
    {
        private readonly CafeDbContext _db;
        public EmployeesController(CafeDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<EmployeeDto>>>> GetEmployees(
            [FromQuery] int? branchId,
            [FromQuery] string? search,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _db.Employees
                .Include(e => e.Branch)
                .Where(e => !e.IsDeleted)
                .AsQueryable();

            if (branchId.HasValue) query = query.Where(e => e.BranchId == branchId);
            if (!string.IsNullOrEmpty(search))
                query = query.Where(e => e.FullName.Contains(search) || e.Phone.Contains(search));

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(e => e.FullName)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(e => new EmployeeDto(
                    e.Id, e.BranchId, e.Branch.Name,
                    e.FullName, e.Phone, e.Email,
                    e.Position, e.Salary, e.HiredDate))
                .ToListAsync();

            return Ok(new ApiResponse<PagedResult<EmployeeDto>>(true, null,
                new PagedResult<EmployeeDto>(items, total, page, pageSize)));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetEmployee(int id)
        {
            var e = await _db.Employees.Include(x => x.Branch).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (e == null) return NotFound(new ApiResponse<EmployeeDto>(false, "Không tìm thấy nhân viên", null));
            return Ok(new ApiResponse<EmployeeDto>(true, null,
                new EmployeeDto(e.Id, e.BranchId, e.Branch.Name, e.FullName, e.Phone, e.Email, e.Position, e.Salary, e.HiredDate)));
        }

        [HttpPost]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<EmployeeDto>>> CreateEmployee([FromBody] CreateEmployeeRequest request)
        {
            var emp = new Employee
            {
                BranchId  = request.BranchId,
                FullName  = request.FullName,
                Phone     = request.Phone,
                Email     = request.Email,
                Position  = request.Position,
                Salary    = request.Salary,
                HiredDate = request.HiredDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Employees.Add(emp);
            await _db.SaveChangesAsync();

            // Tạo tài khoản đăng nhập nếu có cung cấp username và password
            if (!string.IsNullOrWhiteSpace(request.Username) && !string.IsNullOrWhiteSpace(request.Password))
            {
                if (!await _db.Accounts.AnyAsync(a => a.Username == request.Username))
                {
                    var roleName = request.Role?.ToUpper() ?? "STAFF";
                    var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                    if (role != null)
                    {
                        _db.Accounts.Add(new Account
                        {
                            EmployeeId   = emp.Id,
                            RoleId       = role.Id,
                            Username     = request.Username,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                            IsDeleted    = false,
                            CreatedAt    = DateTime.UtcNow,
                            UpdatedAt    = DateTime.UtcNow
                        });
                        await _db.SaveChangesAsync();
                    }
                }
            }

            var branch = await _db.Branches.FindAsync(emp.BranchId);
            return CreatedAtAction(nameof(GetEmployee), new { id = emp.Id },
                new ApiResponse<EmployeeDto>(true, "Thêm nhân viên thành công",
                    new EmployeeDto(emp.Id, emp.BranchId, branch?.Name ?? "", emp.FullName,
                        emp.Phone, emp.Email, emp.Position, emp.Salary, emp.HiredDate)));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest request)
        {
            var emp = await _db.Employees.FindAsync(id);
            if (emp == null || emp.IsDeleted)
                return NotFound(new ApiResponse<object>(false, "Không tìm thấy nhân viên", null));

            if (request.FullName  != null) emp.FullName  = request.FullName;
            if (request.Phone     != null) emp.Phone     = request.Phone;
            if (request.Email     != null) emp.Email     = request.Email;
            if (request.Position  != null) emp.Position  = request.Position;
            if (request.Salary.HasValue)   emp.Salary    = request.Salary.Value;
            if (request.HiredDate.HasValue) emp.HiredDate = request.HiredDate;
            if (request.BranchId.HasValue)  emp.BranchId  = request.BranchId.Value;
            emp.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new ApiResponse<object>(true, "Cập nhật nhân viên thành công", null));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteEmployee(int id)
        {
            var emp = await _db.Employees.FindAsync(id);
            if (emp == null || emp.IsDeleted)
                return NotFound(new ApiResponse<object>(false, "Không tìm thấy nhân viên", null));

            emp.IsDeleted = true;
            emp.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new ApiResponse<object>(true, "Đã xóa nhân viên", null));
        }
    }
}
