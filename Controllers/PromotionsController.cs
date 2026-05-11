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
                    (p.EndDate   == null || p.EndDate   >= now) &&
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
                Name             = request.Name,
                Code             = request.Code,
                DiscountType     = request.DiscountType.ToUpper(),
                Value            = request.Value,
                MinOrderValue    = request.MinOrderValue,
                MaxDiscountValue = request.MaxDiscountValue,
                UsageLimit       = request.UsageLimit,
                StartDate        = request.StartDate,
                EndDate          = request.EndDate
            };
            _db.Promotions.Add(promo);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPromotions),
                new ApiResponse<PromotionDto>(true, "Tạo khuyến mãi thành công", null));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> UpdatePromotion(int id, [FromBody] UpdatePromotionRequest request)
        {
            var promo = await _db.Promotions.FindAsync(id);
            if (promo == null)
                return NotFound(new ApiResponse<object>(false, "Không tìm thấy khuyến mãi", null));

            if (request.Name         != null) promo.Name             = request.Name;
            if (request.Code         != null) promo.Code             = request.Code;
            if (request.DiscountType != null) promo.DiscountType     = request.DiscountType.ToUpper();
            if (request.Value.HasValue)       promo.Value            = request.Value.Value;
            if (request.MinOrderValue.HasValue)    promo.MinOrderValue    = request.MinOrderValue;
            if (request.MaxDiscountValue.HasValue) promo.MaxDiscountValue = request.MaxDiscountValue;
            if (request.UsageLimit.HasValue)       promo.UsageLimit       = request.UsageLimit;
            if (request.StartDate.HasValue)        promo.StartDate        = request.StartDate;
            if (request.EndDate.HasValue)          promo.EndDate          = request.EndDate;

            if (request.IsActive.HasValue)
            {
                if (request.IsActive.Value)
                {
                    // Kích hoạt lại (xoá EndDate hoặc đặt tương lai xa)
                    promo.EndDate = null;
                }
                else
                {
                    // Dừng khuyến mãi bằng cách set EndDate về quá khứ
                    promo.EndDate = DateTime.UtcNow.AddMinutes(-1);
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new ApiResponse<object>(true, "Cập nhật khuyến mãi thành công", null));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> DeletePromotion(int id)
        {
            var promo = await _db.Promotions.FindAsync(id);
            if (promo == null)
                return NotFound(new ApiResponse<object>(false, "Không tìm thấy khuyến mãi", null));

            _db.Promotions.Remove(promo);
            await _db.SaveChangesAsync();
            return Ok(new ApiResponse<object>(true, "Đã xóa khuyến mãi", null));
        }
    }
}
