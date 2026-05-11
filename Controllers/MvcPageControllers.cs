using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using CafeManagement.Data;

namespace CafeManagement.Controllers
{
    /// <summary>
    /// MVC Controllers phục vụ Razor Views.
    /// Mỗi action: kiểm tra JWT session → inject ViewBag → trả về View.
    /// </summary>

    // ─── Base: xác thực session ──────────────────────────────────────────
    public abstract class AuthenticatedController : Controller
    {
        protected bool IsAuthenticated()
            => !string.IsNullOrEmpty(HttpContext.Session.GetString("JwtToken"));

        protected IActionResult RequireAuth()
            => Redirect("/Login");

        protected string? GetRole()
            => HttpContext.Session.GetString("Role");

        protected bool IsManager()
        {
            var role = GetRole()?.ToUpper();
            return role == "MANAGER" || role == "ADMIN";
        }

        /// <summary>Redirect nhân viên về POS nếu truy cập trang quản lý</summary>
        protected IActionResult? RequireManager()
            => IsManager() ? null : Redirect("/Home/Index");

        protected int GetBranchId()
        {
            if (int.TryParse(HttpContext.Session.GetString("BranchId"), out var id)) return id;
            return 1;
        }
    }

    // ─── Dashboard (Manager only) ─────────────────────────────────────────
    public class DashboardPageController : AuthenticatedController
    {
        public IActionResult Index()
        {
            if (!IsAuthenticated()) return RequireAuth();
            var mgr = RequireManager(); if (mgr != null) return mgr;
            ViewBag.ActivePage = "Dashboard";
            ViewBag.PageTitle  = "Dashboard";
            return View("~/Views/Dashboard/Index.cshtml");
        }
    }

    // ─── Orders ──────────────────────────────────────────────────────────
    public class OrdersMvcController : AuthenticatedController
    {
        public IActionResult Index()
        {
            if (!IsAuthenticated()) return RequireAuth();
            ViewBag.ActivePage = "Orders";
            ViewBag.PageTitle  = "Lịch sử Đơn hàng";
            return View("~/Views/Orders/Index.cshtml");
        }
    }

    // ─── Menu ────────────────────────────────────────────────────────────
    public class MenuMvcController : AuthenticatedController
    {
        public IActionResult Index()
        {
            if (!IsAuthenticated()) return RequireAuth();
            ViewBag.ActivePage = "Menu";
            ViewBag.PageTitle  = "Quản lý Thực đơn";
            return View("~/Views/Menu/Index.cshtml");
        }
    }

    // ─── Tables ──────────────────────────────────────────────────────────
    public class TablesMvcController : AuthenticatedController
    {
        public IActionResult Index()
        {
            if (!IsAuthenticated()) return RequireAuth();
            ViewBag.ActivePage = "Tables";
            ViewBag.PageTitle  = "Quản lý Bàn";
            return View("~/Views/Tables/Index.cshtml");
        }
    }

    // ─── Shifts (Manager only) ───────────────────────────────────────────
    public class ShiftsPageController : AuthenticatedController
    {
        public IActionResult Index()
        {
            if (!IsAuthenticated()) return RequireAuth();
            var mgr = RequireManager(); if (mgr != null) return mgr;
            ViewBag.ActivePage = "Shifts";
            ViewBag.PageTitle  = "Ca làm việc";
            return View("~/Views/Shifts/Index.cshtml");
        }
    }

    // ─── Customers (Manager only) ──────────────────────────────────────
    public class CustomersMvcController : AuthenticatedController
    {
        public IActionResult Index()
        {
            if (!IsAuthenticated()) return RequireAuth();
            var mgr = RequireManager(); if (mgr != null) return mgr;
            ViewBag.ActivePage = "Customers";
            ViewBag.PageTitle  = "Quản lý Khách hàng";
            return View("~/Views/Customers/Index.cshtml");
        }
    }

    // ─── Promotions (Manager only) ─────────────────────────────────────
    public class PromotionsMvcController : AuthenticatedController
    {
        public IActionResult Index()
        {
            if (!IsAuthenticated()) return RequireAuth();
            var mgr = RequireManager(); if (mgr != null) return mgr;
            ViewBag.ActivePage = "Promotions";
            ViewBag.PageTitle  = "Khuyến mãi";
            return View("~/Views/Promotions/Index.cshtml");
        }
    }

    // ─── Employees (Manager only) ─────────────────────────────────────
    public class EmployeesMvcController : AuthenticatedController
    {
        public IActionResult Index()
        {
            if (!IsAuthenticated()) return RequireAuth();
            var mgr = RequireManager(); if (mgr != null) return mgr;
            ViewBag.ActivePage = "Employees";
            ViewBag.PageTitle  = "Quản lý Nhân viên";
            return View("~/Views/Employees/Index.cshtml");
        }
    }

    // ─── Suppliers (Manager only) ──────────────────────────────────────
    public class SuppliersPageController : AuthenticatedController
    {
        public IActionResult Index()
        {
            if (!IsAuthenticated()) return RequireAuth();
            var mgr = RequireManager(); if (mgr != null) return mgr;
            ViewBag.ActivePage = "Suppliers";
            ViewBag.PageTitle  = "Nhà cung cấp";
            return View("~/Views/Suppliers/Index.cshtml");
        }
    }

    // ─── Profile ─────────────────────────────────────────────────────────
    public class ProfileMvcController : AuthenticatedController
    {
        public IActionResult Index()
        {
            if (!IsAuthenticated()) return RequireAuth();
            ViewBag.ActivePage = "Profile";
            ViewBag.PageTitle  = "Hồ sơ cá nhân";
            return View("~/Views/Profile/Index.cshtml");
        }
    }

    // ─── Order Detail ─────────────────────────────────
    public class OrderDetailMvcController : AuthenticatedController
    {
        public IActionResult Index(long id)
        {
            if (!IsAuthenticated()) return RequireAuth();
            ViewBag.OrderId   = id;
            ViewBag.ActivePage = "Orders";
            ViewBag.PageTitle  = $"Đơn Hàng #{id}";
            return View("~/Views/Orders/Detail.cshtml");
        }
    }

    // ─── Public Menu (không yêu cầu đăng nhập — dành cho khách scan QR) ──────
    public class PublicMenuController : Controller
    {
        /// <summary>
        /// Trang thực đơn công khai: khách scan QR có thể xem mà không cần tài khoản.
        /// API /api/menu/items được gọi trực tiếp từ trình duyệt khách hàng.
        /// </summary>
        public IActionResult Index(int? tableId)
        {
            ViewBag.TableId   = tableId;
            ViewBag.PageTitle = "Thực Đơn";
            return View("~/Views/PublicMenu/Index.cshtml");
        }
    }
    // ─── Kitchen Display (không yêu cầu đăng nhập — TV riêng của quầy bếp) ───────────
    public class KitchenMvcController : Controller
    {
        /// <summary>
        /// Màn hình bếp công khai: nhân viên bếp nhìn thấy ticket không cần login.
        /// Token và branchId truyền qua query string cho TV chia sẻ.
        /// </summary>
        public IActionResult Index()
        {
            ViewBag.PageTitle = "🍳 Màn hình Bếp";
            return View("~/Views/Kitchen/Index.cshtml");
        }
    }
}
