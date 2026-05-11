using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeManagement.Data;
using CafeManagement.DTOs;
using CafeManagement.Models;

namespace CafeManagement.Controllers
{
    // ================================================================
    // MENU CONTROLLER
    // ================================================================

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MenuController : ControllerBase
    {
        private readonly CafeDbContext _db;
        public MenuController(CafeDbContext db) => _db = db;

        // Khách scan QR có thể xem danh mục không cần đăng nhập
        [AllowAnonymous]
        [HttpGet("categories")]
        public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories()
        {
            var categories = await _db.Categories
                .Select(c => new CategoryDto(
                    c.Id, c.Name,
                    c.MenuItems.Count(m => !m.IsDeleted)))
                .ToListAsync();

            return Ok(new ApiResponse<List<CategoryDto>>(true, null, categories));
        }

        [HttpPost("categories")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            if (await _db.Categories.AnyAsync(c => c.Name == request.Name))
                return Conflict(new ApiResponse<CategoryDto>(false, "Danh mục đã tồn tại", null));

            var cat = new Category { Name = request.Name };
            _db.Categories.Add(cat);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCategories), new ApiResponse<CategoryDto>(true, null,
                new CategoryDto(cat.Id, cat.Name, 0)));
        }

        // Khách scan QR có thể xem thực đơn không cần đăng nhập
        [AllowAnonymous]
        [HttpGet("items")]
        public async Task<ActionResult<ApiResponse<List<MenuItemDto>>>> GetMenuItems(
            [FromQuery] int? categoryId,
            [FromQuery] bool? isAvailable)
        {
            var query = _db.MenuItems.Include(m => m.Category)
                .Where(m => !m.IsDeleted)
                .AsQueryable();
            if (categoryId.HasValue) query = query.Where(m => m.CategoryId == categoryId);
            if (isAvailable.HasValue) query = query.Where(m => m.IsAvailable == isAvailable);

            var items = await query
                .Select(m => new MenuItemDto(
                    m.Id, m.CategoryId, m.Category.Name,
                    m.Name, m.Price, m.ImageUrl, m.IsAvailable, m.Description))
                .ToListAsync();

            return Ok(new ApiResponse<List<MenuItemDto>>(true, null, items));
        }

        [HttpGet("items/{id:int}")]
        public async Task<ActionResult<ApiResponse<MenuItemDto>>> GetMenuItem(int id)
        {
            var m = await _db.MenuItems.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id);
            if (m == null) return NotFound(new ApiResponse<MenuItemDto>(false, "Không tìm thấy món", null));
            return Ok(new ApiResponse<MenuItemDto>(true, null,
                new MenuItemDto(m.Id, m.CategoryId, m.Category.Name, m.Name, m.Price, m.ImageUrl, m.IsAvailable, m.Description)));
        }

        [HttpPost("items")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<MenuItemDto>>> CreateMenuItem([FromBody] CreateMenuItemRequest request)
        {
            var item = new MenuItem
            {
                CategoryId  = request.CategoryId,
                Name        = request.Name,
                Price       = request.Price,
                ImageUrl    = request.ImageUrl,
                IsAvailable = request.IsAvailable,
                Description = request.Description
            };
            _db.MenuItems.Add(item);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMenuItem), new { id = item.Id },
                new ApiResponse<MenuItemDto>(true, "Tạo món thành công", null));
        }

        [HttpPut("items/{id:int}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<MenuItemDto>>> UpdateMenuItem(int id, [FromBody] UpdateMenuItemRequest request)
        {
            var item = await _db.MenuItems.FindAsync(id);
            if (item == null) return NotFound(new ApiResponse<MenuItemDto>(false, "Không tìm thấy món", null));

            if (request.Price.HasValue && request.Price != item.Price)
            {
                _db.PriceHistories.Add(new PriceHistory
                {
                    MenuItemId = id,
                    OldPrice   = item.Price,
                    NewPrice   = request.Price.Value,
                    StartDate  = DateTime.UtcNow
                });
                item.Price = request.Price.Value;
            }

            if (request.Name        != null) item.Name        = request.Name;
            if (request.ImageUrl    != null) item.ImageUrl    = request.ImageUrl;
            if (request.IsAvailable.HasValue) item.IsAvailable = request.IsAvailable.Value;
            if (request.Description != null) item.Description = request.Description;
            if (request.CategoryId.HasValue) item.CategoryId  = request.CategoryId.Value;
            item.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new ApiResponse<MenuItemDto>(true, "Cập nhật thành công", null));
        }

        [HttpDelete("items/{id:int}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteMenuItem(int id)
        {
            var item = await _db.MenuItems.FindAsync(id);
            if (item == null) return NotFound(new ApiResponse<object>(false, "Không tìm thấy món", null));
            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new ApiResponse<object>(true, "Đã xóa món", null));
        }

        // GET /api/menu/recipes/{menuItemId} — Xem công thức của 1 món
        [HttpGet("recipes/{menuItemId:int}")]
        public async Task<ActionResult<ApiResponse<List<object>>>> GetRecipes(int menuItemId)
        {
            var recipes = await _db.Recipes
                .Include(r => r.Ingredient)
                .Where(r => r.MenuItemId == menuItemId)
                .Select(r => new
                {
                    r.IngredientId,
                    IngredientName   = r.Ingredient.Name,
                    Unit             = r.Ingredient.Unit,
                    r.QuantityRequired
                })
                .ToListAsync();

            return Ok(new ApiResponse<List<object>>(true, null, recipes.Cast<object>().ToList()));
        }

        // POST /api/menu/recipes — Tạo/cập nhật công thức
        [HttpPost("recipes")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> UpsertRecipe([FromBody] UpsertRecipeRequest request)
        {
            var existing = await _db.Recipes
                .FirstOrDefaultAsync(r => r.MenuItemId == request.MenuItemId && r.IngredientId == request.IngredientId);

            if (existing != null)
            {
                existing.QuantityRequired = request.QuantityRequired;
            }
            else
            {
                _db.Recipes.Add(new Recipe
                {
                    MenuItemId        = request.MenuItemId,
                    IngredientId      = request.IngredientId,
                    QuantityRequired  = request.QuantityRequired
                });
            }

            await _db.SaveChangesAsync();
            return Ok(new ApiResponse<object>(true, "Lưu công thức thành công", null));
        }

        // DELETE /api/menu/recipes/{menuItemId}/{ingredientId}
        [HttpDelete("recipes/{menuItemId:int}/{ingredientId:int}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteRecipe(int menuItemId, int ingredientId)
        {
            var recipe = await _db.Recipes
                .FirstOrDefaultAsync(r => r.MenuItemId == menuItemId && r.IngredientId == ingredientId);
            if (recipe == null)
                return NotFound(new ApiResponse<object>(false, "Không tìm thấy công thức", null));

            _db.Recipes.Remove(recipe);
            await _db.SaveChangesAsync();
            return Ok(new ApiResponse<object>(true, "Đã xóa nguyên liệu khỏi công thức", null));
        }
    }
}
