using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using CafeManagement.Data;
using CafeManagement.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Database ────────────────────────────────────────────────────────
builder.Services.AddDbContext<CafeDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(3)));

// ── Authentication: JWT ─────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── Controllers + Views (MVC) ────────────────────────────────────────
builder.Services.AddControllersWithViews()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ── Session (lưu JWT token phía server cho cookie-based flow) ────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
});

// ── HttpClient (dùng trong LoginController để gọi API nội bộ) ────────
builder.Services.AddHttpClient();

// ── CORS ─────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("CafePolicy", policy =>
    {
        policy
            .WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:3000", "http://localhost:5189", "http://localhost:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ── Swagger ─────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PBL3 Cafe Management API",
        Version = "v1",
        Description = "REST API cho hệ thống POS quản lý quán cà phê"
    });

    // JWT Support trong Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Nhập: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── Build ────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cafe API v1");
        c.RoutePrefix = "swagger"; // Swagger tại /swagger (không chiếm route gốc nữa)
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("CafePolicy");
app.UseSession();               // Session PHẢI trước Authentication
app.UseSessionAuth();           // ✅ Session guard: chặn truy cập URL trực tiếp nếu chưa login
app.UseAuthentication();
app.UseAuthorization();

// ── API Controllers ──────────────────────────────────────────────────
app.MapControllers();

// ── MVC (Razor Views) ────────────────────────────────────────────────
// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Named page routes (sidebar nav)
app.MapControllerRoute(name: "dashboard",   pattern: "Dashboard",   defaults: new { controller = "DashboardPage",  action = "Index" });
app.MapControllerRoute(name: "orders",      pattern: "Orders",      defaults: new { controller = "OrdersMvc",     action = "Index" });
app.MapControllerRoute(name: "menu",        pattern: "Menu",        defaults: new { controller = "MenuMvc",       action = "Index" });
app.MapControllerRoute(name: "tables",      pattern: "Tables",      defaults: new { controller = "TablesMvc",     action = "Index" });
app.MapControllerRoute(name: "shifts",      pattern: "Shifts",      defaults: new { controller = "ShiftsPage",    action = "Index" });
app.MapControllerRoute(name: "customers",   pattern: "Customers",   defaults: new { controller = "CustomersMvc",  action = "Index" });
app.MapControllerRoute(name: "promotions",  pattern: "Promotions",  defaults: new { controller = "PromotionsMvc", action = "Index" });
app.MapControllerRoute(name: "employees",   pattern: "Employees",   defaults: new { controller = "EmployeesMvc",  action = "Index" });
app.MapControllerRoute(name: "inventory",   pattern: "Inventory",   defaults: new { controller = "InventoryMvc",  action = "Index" });
app.MapControllerRoute(name: "profile",     pattern: "Profile",     defaults: new { controller = "ProfileMvc",    action = "Index" });
app.MapControllerRoute(name: "suppliers",   pattern: "Suppliers",   defaults: new { controller = "SuppliersPage", action = "Index" });
app.MapControllerRoute(name: "orderdetail", pattern: "Orders/{id:long}", defaults: new { controller = "OrderDetailMvc", action = "Index" });
app.MapControllerRoute(name: "kitchen",     pattern: "Kitchen",     defaults: new { controller = "KitchenMvc",    action = "Index" });

// Public menu (không cần đăng nhập — dành cho khách scan QR của bàn)
app.MapControllerRoute(name: "publicmenu",  pattern: "PublicMenu",  defaults: new { controller = "PublicMenu",   action = "Index" });

// Health check nhanh
app.MapGet("/health", () => Results.Ok(new { status = "healthy", time = DateTime.UtcNow }));

// ── DEV ONLY: Tạo BCrypt hash để setup password ban đầu ──────────────
// Truy cập: http://localhost:5189/dev/hash?password=admin123
if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/hash", (string password) =>
        Results.Ok(new { password, hash = BCrypt.Net.BCrypt.HashPassword(password) }));

    app.MapGet("/dev/db-check", async (CafeManagement.Data.CafeDbContext db) =>
    {
        try
        {
            var canConnect = await db.Database.CanConnectAsync();
            var branchCount = canConnect ? await db.Branches.CountAsync() : 0;
            var tableCount  = canConnect ? await db.Tables.CountAsync() : 0;
            var menuCount   = canConnect ? await db.MenuItems.CountAsync() : 0;
            var shiftOpen   = canConnect ? await db.WorkShifts.AnyAsync(s => s.Status == "OPEN") : false;
            return Results.Ok(new
            {
                connected   = canConnect,
                branches    = branchCount,
                tables      = tableCount,
                menuItems   = menuCount,
                hasOpenShift = shiftOpen,
                message     = canConnect ? "✅ Kết nối database thành công!" : "❌ Không kết nối được database"
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"❌ Lỗi: {ex.Message}");
        }
    });
}

app.Run();