using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using CafeManagement.Data;

namespace CafeManagement.Controllers
{
    /// <summary>
    /// MVC Controller phục vụ trang POS chính (Home/Index).
    /// Xác thực JWT từ Session → truyền dữ liệu bàn xuống View.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly CafeDbContext _db;
        public HomeController(CafeDbContext db) => _db = db;

        // GET /  hoặc  GET /Home/Index
        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return Redirect("/Login");

            var branchId = int.TryParse(HttpContext.Session.GetString("BranchId"), out var bid) ? bid : 1;

            // Lấy danh sách bàn từ database theo chi nhánh
            var tables = await _db.Tables
                .Where(t => t.BranchId == branchId && !t.IsDeleted)
                .OrderBy(t => t.Name)
                .ToListAsync();

            ViewBag.BranchId  = branchId;
            ViewBag.JwtToken  = token;
            ViewBag.ActivePage = "POS";
            ViewBag.PageTitle  = "POS — Bán hàng";

            return View(tables);
        }
    }
}
