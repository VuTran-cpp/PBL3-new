using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace CafeManagement.Controllers
{
    /// <summary>
    /// MVC Controller xử lý đăng nhập:
    /// - GET /Login  → Hiển thị form login (chọn vai trò + nhập thông tin)
    /// - POST /Login → Gọi API /api/auth/login, kiểm tra role khớp, lưu token vào Session, redirect
    /// </summary>
    public class LoginController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;//dùng để gọi các API nội bộ
        private readonly IConfiguration _config;//dùng để đọc file cấu hình hệ thống

        public LoginController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        //Hành động sẽ xảy ra khi từ trình duyệt gửi 1 HTTP GET request
        //Hiển thị giao diện đăng nhập
        [HttpGet]
        public IActionResult Index(string? returnUrl)
        {
            // Nếu đã đăng nhập rồi thì chuyển hướng sang trang phù hợp
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("JwtToken")))
            {
                var role = HttpContext.Session.GetString("Role") ?? "";
                return Redirect(GetRedirectByRole(role));
            }
            //Lưu lại đường dẫn URL mà người dùng muốn vào 
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //Xử lí khi người dùng gửi 1 yêu cầu dữ liệu
        //Xử lí dữ liệu đầu vào
        [HttpPost]
        public async Task<IActionResult> Index(string username, string password, string? role, string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập tên đăng nhập và mật khẩu.";
                ViewBag.SelectedRole = role;
                return View();
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                ViewBag.Error = "Vui lòng chọn vai trò trước khi đăng nhập.";
                return View();
            }

            try
            {
                // Gọi API login nội bộ
                var client = _httpClientFactory.CreateClient();
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                //Đóng gói thông tin theo định dạng JSON 
                var payload = new { username, password };
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");
                //Thực hiện gửi lệnh POST, chờ API chạy xong và đọc chuỗi lệnh JSON, sau đó trả về kết quả(body)
                var response = await client.PostAsync($"{baseUrl}/api/auth/login", content);
                var body = await response.Content.ReadAsStringAsync();
                //Giải mã chuỗi body thành 1 đối tượng, bỏ qua sự khác biệt chữ hoa, chữ thường
                var result = JsonSerializer.Deserialize<ApiResult>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                //kiểm tra xem API có xác nhận là đăng nhập thành công hay không
                if (result?.Success == true && result.Data != null)
                {
                    // Kiểm tra role người dùng chọn có khớp với role trong DB không
                    var actualRole = result.Data.Role.ToUpper();
                    var chosenRole = role.ToUpper();
                    // ADMIN luôn có quyền đăng nhập cả 2 vai trò
                    bool roleMatch = actualRole == "ADMIN"
                        || actualRole == chosenRole
                        || (chosenRole == "MANAGER" && (actualRole == "MANAGER" || actualRole == "ADMIN"))
                        || (chosenRole == "STAFF" && (actualRole == "STAFF" || actualRole == "ADMIN" || actualRole == "MANAGER"));
                    //Nếu chọn sai vai trò
                    if (!roleMatch)
                    {
                        var roleLabel = chosenRole == "MANAGER" ? "Quản lý" : "Nhân viên";
                        ViewBag.Error = $"Tài khoản này không có quyền đăng nhập với vai trò \"{roleLabel}\". Vui lòng chọn đúng vai trò.";
                        ViewBag.SelectedRole = role;
                        return View();
                    }

                    // Lưu token và thông tin user vào Session 
                    // Lưu role theo lựa chọn (không phải role gốc) để sidebar hiển thị đúng
                    HttpContext.Session.SetString("JwtToken", result.Data.Token);//tạo 1 chuỗi kí tự riêng để đánh dấu mỗi lần đăng nhập của user đó
                    HttpContext.Session.SetString("Username", result.Data.Username);
                    HttpContext.Session.SetString("FullName", result.Data.FullName);
                    HttpContext.Session.SetString("Role", chosenRole);
                    HttpContext.Session.SetString("ActualRole", actualRole); // role gốc từ DB
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

                    //Đẩy user tới đường dẫn đã chọn
                    if (!string.IsNullOrEmpty(returnUrl))
                        return Redirect(returnUrl);
                    return Redirect(GetRedirectByRole(chosenRole));
                }
                //Nếu API báo đăng nhập sai
                ViewBag.Error = result?.Message ?? "Đăng nhập thất bại. Kiểm tra lại thông tin.";
                ViewBag.SelectedRole = role;
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi kết nối server: {ex.Message}";
                ViewBag.SelectedRole = role;
            }

            return View();
        }

        //Đăng xuất
        public IActionResult Logout()
        {
            //Xóa toàn bộ token và thông tin đang lưu
            HttpContext.Session.Clear();
            return Redirect("/Login");
        }

        //Trang đổi mật khẩu (truy cập từ trang Login, không cần đăng nhập)
        public IActionResult ChangePassword()
        {
            return View();
        }

        // Trả về giao diện đúng trang theo vai trò
        private static string GetRedirectByRole(string role)
        {
            return role.ToUpper() switch
            {
                "MANAGER" or "ADMIN" => "/Dashboard",
                _ => "/Home/Index" // STAFF → POS
            };
        }

        // ── DTOs nội bộ cho deserialize ───────────────────────────
        private record ApiResult(bool Success, string? Message, LoginData? Data);
        private record LoginData(string Token, string Username, string Role, int EmployeeId, string FullName);
    }
}
