using CafeManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace CafeManagement.Data
{
    public class CafeDbContext : DbContext
    {
        public CafeDbContext(DbContextOptions<CafeDbContext> options) : base(options) { }

        // DbSets
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<LoginLog> LoginLogs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<BranchIngredient> BranchIngredients { get; set; }
        public DbSet<ImportReceipt> ImportReceipts { get; set; }
        public DbSet<ImportReceiptItem> ImportReceiptItems { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<PriceHistory> PriceHistories { get; set; }
        public DbSet<Option> Options { get; set; }
        public DbSet<TableCafe> Tables { get; set; }
        public DbSet<WorkShift> WorkShifts { get; set; }
        public DbSet<OrderStatus> OrderStatuses { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
        public DbSet<OrderPromotion> OrderPromotions { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderItemOption> OrderItemOptions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Refund> Refunds { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Composite Primary Keys ─────────────────────────────
            modelBuilder.Entity<BranchIngredient>()
                .HasKey(bi => new { bi.BranchId, bi.IngredientId });

            modelBuilder.Entity<Recipe>()
                .HasKey(r => new { r.MenuItemId, r.IngredientId });

            modelBuilder.Entity<OrderPromotion>()
                .HasKey(op => new { op.OrderId, op.PromotionId });

            modelBuilder.Entity<OrderItemOption>()
                .HasKey(oio => new { oio.OrderItemId, oio.OptionId });

            // ── Table Names ────────────────────────────────────────
            modelBuilder.Entity<TableCafe>().ToTable("Table_Cafe");
            modelBuilder.Entity<MenuItem>().ToTable("Menu_Item");
            modelBuilder.Entity<OrderStatus>().ToTable("Order_Status");
            modelBuilder.Entity<OrderStatusHistory>().ToTable("Order_Status_History");
            modelBuilder.Entity<OrderPromotion>().ToTable("Order_Promotion");
            modelBuilder.Entity<OrderItem>().ToTable("Order_Item");
            modelBuilder.Entity<OrderItemOption>().ToTable("Order_Item_Option");
            modelBuilder.Entity<ImportReceipt>().ToTable("Import_Receipt");
            modelBuilder.Entity<ImportReceiptItem>().ToTable("Import_Receipt_Item");
            modelBuilder.Entity<InventoryTransaction>().ToTable("Inventory_Transaction");
            modelBuilder.Entity<LoginLog>().ToTable("Login_Log");
            modelBuilder.Entity<AuditLog>().ToTable("Audit_Log");
            modelBuilder.Entity<PriceHistory>().ToTable("Price_History");
            modelBuilder.Entity<WorkShift>().ToTable("Work_Shift");
            modelBuilder.Entity<BranchIngredient>().ToTable("Branch_Ingredient");
            modelBuilder.Entity<Order>().ToTable("Order");
            modelBuilder.Entity<Option>().ToTable("Option");

            // ── Column Naming Conventions ──────────────────────────
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(ToSnakeCase(property.Name));
                }
            }

            // ── Computed Columns ────────────────────────────────────
            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.TotalPrice)
                .HasComputedColumnSql("([quantity]*[unit_price])");

            modelBuilder.Entity<ImportReceiptItem>()
                .Property(iri => iri.TotalPrice)
                .HasComputedColumnSql("([quantity]*[unit_price])");

            // ── Soft Delete Query Filters & Relationship Sync ──────
            // Sửa lỗi: Định nghĩa bộ lọc khớp nhau cho cả hai bên quan hệ

            // 1. Branch & BranchIngredient
            modelBuilder.Entity<Branch>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<BranchIngredient>().HasQueryFilter(bi => !bi.Branch.IsDeleted);

            // 2. Account & AuditLog & LoginLog
            modelBuilder.Entity<Account>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<AuditLog>().HasQueryFilter(al => !al.Account.IsDeleted);
            modelBuilder.Entity<LoginLog>().HasQueryFilter(ll => !ll.Account.IsDeleted);

            // 3. Employee
            modelBuilder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted);

            // Các bộ lọc đơn lẻ khác
            modelBuilder.Entity<Supplier>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Customer>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Promotion>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Ingredient>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ImportReceipt>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<MenuItem>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<TableCafe>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Order>().HasQueryFilter(e => !e.IsDeleted);

            // ── Unique Constraints ─────────────────────────────────
            modelBuilder.Entity<Employee>().HasIndex(e => e.Phone).IsUnique();
            modelBuilder.Entity<Account>().HasIndex(a => a.Username).IsUnique();
            modelBuilder.Entity<Account>().HasIndex(a => a.EmployeeId).IsUnique();
            modelBuilder.Entity<Customer>().HasIndex(c => c.Phone).IsUnique();
            modelBuilder.Entity<Role>().HasIndex(r => r.Name).IsUnique();
            modelBuilder.Entity<Category>().HasIndex(c => c.Name).IsUnique();
            modelBuilder.Entity<Option>().HasIndex(o => o.Name).IsUnique();
            modelBuilder.Entity<Promotion>().HasIndex(p => p.Code).IsUnique();

            // ── Relationships & Prevent Cascade Delete ──────────────
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Table)
                .WithMany(t => t.Orders)
                .HasForeignKey(o => o.TableId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.WorkShift)
                .WithMany(ws => ws.Orders)
                .HasForeignKey(o => o.ShiftId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ImportReceipt>()
                .HasOne(ir => ir.Employee)
                .WithMany()
                .HasForeignKey(ir => ir.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<InventoryTransaction>()
                .HasOne(it => it.Employee)
                .WithMany()
                .HasForeignKey(it => it.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PriceHistory>()
                .HasOne(ph => ph.Account)
                .WithMany()
                .HasForeignKey(ph => ph.ChangedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Refund>()
                .HasOne(r => r.Account)
                .WithMany()
                .HasForeignKey(r => r.ProcessedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<WorkShift>()
                .HasOne(ws => ws.Account)
                .WithMany()
                .HasForeignKey(ws => ws.AccountId)
                .OnDelete(DeleteBehavior.SetNull);
        }

        private static string ToSnakeCase(string name)
        {
            return System.Text.RegularExpressions.Regex
                .Replace(name, "([a-z0-9])([A-Z])", "$1_$2")
                .ToLower();
        }
    }
}