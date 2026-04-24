using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace CafeManagement.Controllers
{
    /// <summary>
    /// MVC Controller xử lý đăng nhập:
    /// - GET /Login  → Hiển thị form login
    /// - POST /Login → Gọi API /api/auth/login, lưu token vào Session, redirect về trang chủ
    /// </summary>
    public class LoginController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public LoginController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        // GET /Login
        [HttpGet]
        public IActionResult Index(string? returnUrl)
        {
            // Nếu đã đăng nhập rồi thì redirect về trang chủ
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("JwtToken")))
                return Redirect("/Home/Index");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST /Login
        [HttpPost]
        public async Task<IActionResult> Index(string username, string password, string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập tên đăng nhập và mật khẩu.";
                return View();
            }

            try
            {
                // Gọi API login nội bộ (cùng server)
                var client = _httpClientFactory.CreateClient();
                var baseUrl = $"{Request.Scheme}://{Request.Host}";

                var payload = new { username, password };
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync($"{baseUrl}/api/auth/login", content);
                var body = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<ApiResult>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Success == true && result.Data != null)
                {
                    // Lưu token và thông tin user vào Session
                    HttpContext.Session.SetString("JwtToken", result.Data.Token);
                    HttpContext.Session.SetString("Username", result.Data.Username);
                    HttpContext.Session.SetString("FullName", result.Data.FullName);
                    HttpContext.Session.SetString("Role", result.Data.Role);
                    HttpContext.Session.SetString("EmployeeId", result.Data.EmployeeId.ToString());

                    // Decode BranchId từ JWT payload
                    try
                    {
                        var parts = result.Data.Token.Split('.');
                        if (parts.Length == 3)
                        {
                            var payload64 = parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=');
                            var payloadJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload64));
                            using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
                            if (doc.RootElement.TryGetProperty("branch_id", out var branchEl))
                                HttpContext.Session.SetString("BranchId", branchEl.GetString() ?? "1");
                        }
                    }
                    catch { HttpContext.Session.SetString("BranchId", "1"); }

                    return Redirect(!string.IsNullOrEmpty(returnUrl) ? returnUrl : "/Home/Index");
                }

                ViewBag.Error = result?.Message ?? "Đăng nhập thất bại. Kiểm tra lại thông tin.";
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi kết nối server: {ex.Message}";
            }

            return View();
        }

        // GET /Login/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Redirect("/Login");
        }

        // ── DTOs nội bộ cho deserialize ───────────────────────────
        private record ApiResult(bool Success, string? Message, LoginData? Data);
        private record LoginData(string Token, string Username, string Role, int EmployeeId, string FullName);
    }
}
