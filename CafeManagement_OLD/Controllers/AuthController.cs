using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CafeManagement.Data;
using CafeManagement.DTOs;
using CafeManagement.Models;

namespace CafeManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly CafeDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public AuthController(CafeDbContext db, IConfiguration config, ILogger<AuthController> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        /// <summary>Đăng nhập — trả về JWT token</summary>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
        {
            var account = await _db.Accounts
                .Include(a => a.Employee)
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Username == request.Username);

            if (account == null || !BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
            {
                // Log thất bại
                if (account != null)
                    await LogLogin(account.Id, GetClientIp(), Request.Headers.UserAgent, false);

                return Unauthorized(new ApiResponse<LoginResponse>(false, "Tên đăng nhập hoặc mật khẩu không đúng", null));
            }

            // Ghi log đăng nhập thành công
            await LogLogin(account.Id, GetClientIp(), Request.Headers.UserAgent, true);

            var token = GenerateJwtToken(account);
            var response = new LoginResponse(
                token,
                account.Username,
                account.Role.Name,
                account.EmployeeId,
                account.Employee.FullName);

            return Ok(new ApiResponse<LoginResponse>(true, "Đăng nhập thành công", response));
        }

        /// <summary>Đổi mật khẩu</summary>
        [HttpPost("change-password")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var accountId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var account = await _db.Accounts.FindAsync(accountId);

            if (account == null || !BCrypt.Net.BCrypt.Verify(request.OldPassword, account.PasswordHash))
                return BadRequest(new ApiResponse<object>(false, "Mật khẩu cũ không đúng", null));

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            account.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new ApiResponse<object>(true, "Đổi mật khẩu thành công", null));
        }

        // ── Private Helpers ────────────────────────────────────────

        private string GenerateJwtToken(Account account)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new Claim(ClaimTypes.Name, account.Username),
                new Claim(ClaimTypes.Role, account.Role.Name),
                new Claim("employee_id", account.EmployeeId.ToString()),
                new Claim("branch_id", account.Employee.BranchId.ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task LogLogin(int accountId, string? ip, string? userAgent, bool success)
        {
            _db.LoginLogs.Add(new LoginLog
            {
                AccountId = accountId,
                IpAddress = ip,
                UserAgent = userAgent,
                IsSuccess = success,
                LoginAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        private string? GetClientIp() =>
            HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
