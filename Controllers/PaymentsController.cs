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
    public class PaymentsController : ControllerBase
    {
        private readonly CafeDbContext _db;
        public PaymentsController(CafeDbContext db) => _db = db;

        [HttpPost]
        public async Task<ActionResult<ApiResponse<PaymentDto>>> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            var order = await _db.Orders.FindAsync(request.OrderId);
            if (order == null)
                return NotFound(new ApiResponse<PaymentDto>(false, "Order không tồn tại", null));

            var validMethods = new[] { "CASH", "BANK", "CARD" };
            if (!validMethods.Contains(request.Method.ToUpper()))
                return BadRequest(new ApiResponse<PaymentDto>(false, "Phương thức thanh toán không hợp lệ", null));

            var payment = new Payment
            {
                OrderId   = request.OrderId,
                Amount    = request.Amount,
                Method    = request.Method.ToUpper(),
                Status    = "SUCCESS",
                Reference = request.Reference,
                PaidAt    = DateTime.UtcNow
            };
            _db.Payments.Add(payment);

            // Cộng điểm nếu có khách hàng
            if (order.CustomerId.HasValue)
            {
                var customer = await _db.Customers.FindAsync(order.CustomerId.Value);
                if (customer != null)
                {
                    var pointsEarned = (int)(order.FinalAmount / 10000); // 1 điểm / 10.000đ
                    customer.Points += pointsEarned;

                    // Cập nhật tier
                    customer.MemberTier = customer.Points switch
                    {
                        >= 5000 => "PLATINUM",
                        >= 2000 => "GOLD",
                        >= 500  => "SILVER",
                        _       => "BRONZE"
                    };
                    customer.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(CreatePayment), new ApiResponse<PaymentDto>(true, "Thanh toán thành công",
                new PaymentDto(payment.Id, payment.OrderId, payment.Amount, payment.Method, payment.Status, payment.PaidAt)));
        }

        [HttpPost("refunds")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<RefundDto>>> CreateRefund([FromBody] CreateRefundRequest request)
        {
            var order = await _db.Orders.FindAsync(request.OrderId);
            if (order == null)
                return NotFound(new ApiResponse<RefundDto>(false, "Order không tồn tại", null));

            var refund = new Refund
            {
                OrderId   = request.OrderId,
                PaymentId = request.PaymentId,
                Amount    = request.Amount,
                Reason    = request.Reason,
                CreatedAt = DateTime.UtcNow
            };
            _db.Refunds.Add(refund);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(CreateRefund), new ApiResponse<RefundDto>(true, "Hoàn tiền thành công",
                new RefundDto(refund.Id, refund.OrderId, refund.Amount, refund.Reason, refund.CreatedAt)));
        }
    }
}
