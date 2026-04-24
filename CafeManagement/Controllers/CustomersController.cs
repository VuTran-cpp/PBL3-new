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
                Phone    = request.Phone,
                Email    = request.Email,
                Birthday = request.Birthday
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(LookupByPhone), new { phone = customer.Phone },
                new ApiResponse<CustomerDto>(true, "Tạo khách hàng thành công",
                    new CustomerDto(customer.Id, customer.FullName, customer.Phone, customer.Email, customer.Birthday, 0, "BRONZE")));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateCustomer(int id, [FromBody] UpdateCustomerRequest request)
        {
            var customer = await _db.Customers.FindAsync(id);
            if (customer == null)
                return NotFound(new ApiResponse<object>(false, "Không tìm thấy khách hàng", null));

            if (request.FullName != null) customer.FullName = request.FullName;
            if (request.Phone    != null) customer.Phone    = request.Phone;
            if (request.Email    != null) customer.Email    = request.Email;
            if (request.Birthday.HasValue) customer.Birthday = request.Birthday;
            customer.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new ApiResponse<object>(true, "Cập nhật khách hàng thành công", null));
        }
    }
}
