namespace CafeManagement.Middleware
{
    /// <summary>
    /// Session guard middleware:
    /// Chặn tất cả MVC route (không phải /api/...) nếu chưa đăng nhập.
    /// Redirect về /Login thay vì trả lỗi 401, giúp UX tốt hơn.
    /// </summary>
    public class SessionAuthMiddleware
    {
        private readonly RequestDelegate _next;

        // Các path được phép truy cập mà không cần đăng nhập
        private static readonly string[] PublicPaths =
        [
            "/login",
            "/publicmenu",
            "/kitchen",        // KDS có thể chạy trên TV riêng
            "/api/",           // API dùng JWT riêng
            "/health",
            "/dev/",
            "/swagger",
            "/favicon",
            "/css/",
            "/js/",
            "/lib/",
            "/images/",
        ];

        public SessionAuthMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "/";

            // Cho qua nếu là public path
            if (IsPublicPath(path))
            {
                await _next(context);
                return;
            }

            // Kiểm tra session
            var token = context.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                // Lưu URL cũ để redirect về sau khi login
                var returnUrl = context.Request.Path + context.Request.QueryString;
                context.Response.Redirect($"/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
                return;
            }

            await _next(context);
        }

        private static bool IsPublicPath(string path)
        {
            foreach (var pub in PublicPaths)
            {
                if (path.StartsWith(pub, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }

    // Extension method để dễ đăng ký trong Program.cs
    public static class SessionAuthMiddlewareExtensions
    {
        public static IApplicationBuilder UseSessionAuth(this IApplicationBuilder builder)
            => builder.UseMiddleware<SessionAuthMiddleware>();
    }
}
