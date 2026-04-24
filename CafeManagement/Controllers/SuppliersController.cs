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
    public class SuppliersController : ControllerBase
    {
        private readonly CafeDbContext _db;
        public SuppliersController(CafeDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<SupplierDto>>>> GetSuppliers([FromQuery] string? search)
        {
            var query = _db.Suppliers.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(s => s.Name.Contains(search) || (s.Phone != null && s.Phone.Contains(search)));

            var suppliers = await query
                .OrderBy(s => s.Name)
                .Select(s => new SupplierDto(s.Id, s.Name, s.Phone, s.Email, s.Address))
                .ToListAsync();

            return Ok(new ApiResponse<List<SupplierDto>>(true, null, suppliers));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<SupplierDto>>> GetSupplier(int id)
        {
            var s = await _db.Suppliers.FindAsync(id);
            if (s == null) return NotFound(new ApiResponse<SupplierDto>(false, "Không tìm thấy nhà cung cấp", null));
            return Ok(new ApiResponse<SupplierDto>(true, null, new SupplierDto(s.Id, s.Name, s.Phone, s.Email, s.Address)));
        }

        [HttpPost]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<SupplierDto>>> CreateSupplier([FromBody] CreateSupplierRequest request)
        {
            if (await _db.Suppliers.AnyAsync(s => s.Name == request.Name))
                return Conflict(new ApiResponse<SupplierDto>(false, "Nhà cung cấp đã tồn tại", null));

            var supplier = new Supplier
            {
                Name    = request.Name,
                Phone   = request.Phone ?? string.Empty,
                Email   = request.Email,
                Address = request.Address
            };
            _db.Suppliers.Add(supplier);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id },
                new ApiResponse<SupplierDto>(true, "Thêm nhà cung cấp thành công",
                    new SupplierDto(supplier.Id, supplier.Name, supplier.Phone, supplier.Email, supplier.Address)));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateSupplier(int id, [FromBody] UpdateSupplierRequest request)
        {
            var supplier = await _db.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound(new ApiResponse<object>(false, "Không tìm thấy nhà cung cấp", null));

            if (request.Name    != null) supplier.Name    = request.Name;
            if (request.Phone   != null) supplier.Phone   = request.Phone;
            if (request.Email   != null) supplier.Email   = request.Email;
            if (request.Address != null) supplier.Address = request.Address;

            await _db.SaveChangesAsync();
            return Ok(new ApiResponse<object>(true, "Cập nhật nhà cung cấp thành công", null));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteSupplier(int id)
        {
            var supplier = await _db.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound(new ApiResponse<object>(false, "Không tìm thấy nhà cung cấp", null));

            var hasReceipts = await _db.ImportReceipts.AnyAsync(r => r.SupplierId == id);
            if (hasReceipts)
                return Conflict(new ApiResponse<object>(false, "Không thể xóa NCC đang có phiếu nhập kho", null));

            _db.Suppliers.Remove(supplier);
            await _db.SaveChangesAsync();
            return Ok(new ApiResponse<object>(true, "Đã xóa nhà cung cấp", null));
        }
    }
}
