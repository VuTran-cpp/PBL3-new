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
    public class InventoryController : ControllerBase
    {
        private readonly CafeDbContext _db;
        public InventoryController(CafeDbContext db) => _db = db;

        [HttpGet("stock/{branchId:int}")]
        public async Task<ActionResult<ApiResponse<List<BranchIngredientDto>>>> GetStock(int branchId)
        {
            var stock = await _db.BranchIngredients
                .Include(bi => bi.Ingredient)
                .Where(bi => bi.BranchId == branchId)
                .Select(bi => new BranchIngredientDto(
                    bi.BranchId, bi.IngredientId, bi.Ingredient.Name,
                    bi.Ingredient.Unit, bi.CurrentStock, bi.CostPerUnit, bi.MinStockThreshold,
                    bi.CurrentStock <= bi.MinStockThreshold))
                .ToListAsync();

            return Ok(new ApiResponse<List<BranchIngredientDto>>(true, null, stock));
        }

        [HttpGet("low-stock/{branchId:int}")]
        public async Task<ActionResult<ApiResponse<List<BranchIngredientDto>>>> GetLowStock(int branchId)
        {
            var low = await _db.BranchIngredients
                .Include(bi => bi.Ingredient)
                .Where(bi => bi.BranchId == branchId && bi.CurrentStock <= bi.MinStockThreshold)
                .Select(bi => new BranchIngredientDto(
                    bi.BranchId, bi.IngredientId, bi.Ingredient.Name,
                    bi.Ingredient.Unit, bi.CurrentStock, bi.CostPerUnit, bi.MinStockThreshold, true))
                .ToListAsync();

            return Ok(new ApiResponse<List<BranchIngredientDto>>(true, null, low));
        }

        [HttpGet("ingredients")]
        public async Task<ActionResult<ApiResponse<List<IngredientDto>>>> GetIngredients([FromQuery] string? search)
        {
            var query = _db.Ingredients.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(i => i.Name.Contains(search));

            var items = await query
                .OrderBy(i => i.Name)
                .Select(i => new IngredientDto(i.Id, i.Name, i.Unit))
                .ToListAsync();

            return Ok(new ApiResponse<List<IngredientDto>>(true, null, items));
        }

        [HttpPost("ingredients")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<IngredientDto>>> CreateIngredient([FromBody] CreateIngredientRequest request)
        {
            if (await _db.Ingredients.AnyAsync(i => i.Name == request.Name))
                return Conflict(new ApiResponse<IngredientDto>(false, "Nguyên liệu đã tồn tại", null));

            var ingredient = new Ingredient { Name = request.Name, Unit = request.Unit };
            _db.Ingredients.Add(ingredient);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetIngredients),
                new ApiResponse<IngredientDto>(true, "Thêm nguyên liệu thành công",
                    new IngredientDto(ingredient.Id, ingredient.Name, ingredient.Unit)));
        }

        [HttpPost("import-receipts")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<ImportReceiptDto>>> CreateImportReceipt([FromBody] CreateImportReceiptRequest request)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var receipt = new ImportReceipt
                {
                    BranchId   = request.BranchId,
                    SupplierId = request.SupplierId,
                    EmployeeId = request.EmployeeId,
                    Note       = request.Note,
                    Status     = "COMPLETED"
                };
                _db.ImportReceipts.Add(receipt);
                await _db.SaveChangesAsync();

                decimal total = 0;
                foreach (var item in request.Items)
                {
                    var receiptItem = new ImportReceiptItem
                    {
                        ImportReceiptId = receipt.Id,
                        IngredientId    = item.IngredientId,
                        Quantity        = item.Quantity,
                        UnitPrice       = item.UnitPrice
                    };
                    _db.ImportReceiptItems.Add(receiptItem);
                    total += item.Quantity * item.UnitPrice;

                    // Cập nhật tồn kho
                    var branchIngredient = await _db.BranchIngredients
                        .FirstOrDefaultAsync(bi => bi.BranchId == request.BranchId && bi.IngredientId == item.IngredientId);

                    if (branchIngredient == null)
                    {
                        _db.BranchIngredients.Add(new BranchIngredient
                        {
                            BranchId     = request.BranchId,
                            IngredientId = item.IngredientId,
                            CurrentStock = item.Quantity,
                            CostPerUnit  = item.UnitPrice
                        });
                    }
                    else
                    {
                        branchIngredient.CurrentStock += item.Quantity;
                        branchIngredient.CostPerUnit   = item.UnitPrice;
                    }

                    _db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        BranchId        = request.BranchId,
                        IngredientId    = item.IngredientId,
                        EmployeeId      = request.EmployeeId,
                        ImportReceiptId = receipt.Id,
                        Type            = "IMPORT",
                        Quantity        = item.Quantity,
                        Note            = $"Nhập kho theo phiếu #{receipt.Id}"
                    });
                }

                receipt.TotalAmount = total;
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(CreateImportReceipt),
                    new ApiResponse<ImportReceiptDto>(true, "Nhập kho thành công", null));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpGet("import-receipts")]
        public async Task<ActionResult<ApiResponse<PagedResult<object>>>> GetImportReceipts(
            [FromQuery] int? branchId,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _db.ImportReceipts
                .Include(r => r.Supplier)
                .Include(r => r.Employee)
                .Where(r => !r.IsDeleted)
                .AsQueryable();

            if (branchId.HasValue) query = query.Where(r => r.BranchId == branchId);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(r => new {
                    r.Id, r.BranchId, r.SupplierId,
                    SupplierName = r.Supplier.Name,
                    EmployeeName = r.Employee != null ? r.Employee.FullName : null,
                    TotalValue   = r.TotalAmount,
                    r.Status, r.Note, r.CreatedAt
                })
                .ToListAsync();

            return Ok(new ApiResponse<PagedResult<object>>(true, null,
                new PagedResult<object>(items.Cast<object>().ToList(), total, page, pageSize)));
        }

        [HttpPost("adjust")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> AdjustStock([FromBody] AdjustStockRequest request)
        {
            var bi = await _db.BranchIngredients
                .FirstOrDefaultAsync(x => x.BranchId == request.BranchId && x.IngredientId == request.IngredientId);

            if (bi == null)
                return NotFound(new ApiResponse<object>(false, "Không tìm thấy nguyên liệu trong kho", null));

            var diff = request.NewQuantity - bi.CurrentStock;
            bi.CurrentStock = request.NewQuantity;

            _db.InventoryTransactions.Add(new InventoryTransaction
            {
                BranchId     = request.BranchId,
                IngredientId = request.IngredientId,
                Type         = diff >= 0 ? "ADJUST_IN" : "ADJUST_OUT",
                Quantity     = Math.Abs(diff),
                Note         = request.Note ?? "Điều chỉnh tồn kho thủ công"
            });

            await _db.SaveChangesAsync();
            return Ok(new ApiResponse<object>(true, "Điều chỉnh tồn kho thành công", null));
        }
    }
}
