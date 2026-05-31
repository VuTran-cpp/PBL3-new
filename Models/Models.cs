using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeManagement.Models
{
    // ============================================================
    // PHẦN 1: BRANCH & SUPPLIER
    // ============================================================

    [Table("Branch")]
    public class Branch
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
        [Required, MaxLength(255)] public string Address { get; set; } = string.Empty;
        [MaxLength(20)] public string? Phone { get; set; }
        [MaxLength(100)] public string? Email { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public ICollection<TableCafe> Tables { get; set; } = new List<TableCafe>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<BranchIngredient> BranchIngredients { get; set; } = new List<BranchIngredient>();
        public ICollection<WorkShift> WorkShifts { get; set; } = new List<WorkShift>();
        public ICollection<ImportReceipt> ImportReceipts { get; set; } = new List<ImportReceipt>();
    }

    [Table("Supplier")]
    public class Supplier
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
        [Required, MaxLength(20)] public string Phone { get; set; } = string.Empty;
        [MaxLength(255)] public string? Address { get; set; }
        [MaxLength(100)] public string? Email { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ImportReceipt> ImportReceipts { get; set; } = new List<ImportReceipt>();
    }

    // ============================================================
    // PHẦN 2: HR & SECURITY
    // ============================================================

    [Table("Employee")]
    public class Employee
    {
        [Key] public int Id { get; set; }
        public int BranchId { get; set; }
        [Required, MaxLength(100)] public string FullName { get; set; } = string.Empty;
        [Required, MaxLength(15)] public string Phone { get; set; } = string.Empty;
        [MaxLength(100)] public string? Email { get; set; }
        [MaxLength(50)] public string? Position { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal Salary { get; set; } = 0;
        public DateOnly? HiredDate { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("BranchId")] public Branch Branch { get; set; } = null!;
        public Account? Account { get; set; }
        public ICollection<WorkShift> WorkShifts { get; set; } = new List<WorkShift>();
    }

    [Table("Role")]
    public class Role
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(50)] public string Name { get; set; } = string.Empty;

        public ICollection<Account> Accounts { get; set; } = new List<Account>();
    }

    [Table("Account")]
    public class Account
    {
        [Key] public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int RoleId { get; set; }
        [Required, MaxLength(50)] public string Username { get; set; } = string.Empty;
        [Required, MaxLength(255)] public string PasswordHash { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("EmployeeId")] public Employee Employee { get; set; } = null!;
        [ForeignKey("RoleId")] public Role Role { get; set; } = null!;
        public ICollection<LoginLog> LoginLogs { get; set; } = new List<LoginLog>();
    }

    [Table("Login_Log")]
    public class LoginLog
    {
        [Key] public long Id { get; set; }
        public int AccountId { get; set; }
        [MaxLength(50)] public string? IpAddress { get; set; }
        [MaxLength(255)] public string? UserAgent { get; set; }
        public DateTime LoginAt { get; set; } = DateTime.UtcNow;
        public bool IsSuccess { get; set; } = true;

        [ForeignKey("AccountId")] public Account Account { get; set; } = null!;
    }

    [Table("Audit_Log")]
    public class AuditLog
    {
        [Key] public long Id { get; set; }
        public int AccountId { get; set; }
        [Required, MaxLength(20)] public string Action { get; set; } = string.Empty;
        [Required, MaxLength(50)] public string Entity { get; set; } = string.Empty;
        public long? EntityId { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("AccountId")] public Account Account { get; set; } = null!;
    }

    // ============================================================
    // PHẦN 3: CRM
    // ============================================================

    [Table("Customer")]
    public class Customer
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(100)] public string FullName { get; set; } = string.Empty;
        [Required, MaxLength(15)] public string Phone { get; set; } = string.Empty;
        [MaxLength(100)] public string? Email { get; set; }
        public DateOnly? Birthday { get; set; }
        public int Points { get; set; } = 0;
        [MaxLength(20)] public string MemberTier { get; set; } = "BRONZE";
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }

    [Table("Promotion")]
    public class Promotion
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
        [MaxLength(50)] public string? Code { get; set; }
        [Required, MaxLength(20)] public string DiscountType { get; set; } = string.Empty;
        [Column(TypeName = "decimal(12,2)")] public decimal Value { get; set; }
        [Column(TypeName = "decimal(12,2)")] public decimal? MinOrderValue { get; set; }
        [Column(TypeName = "decimal(12,2)")] public decimal? MaxDiscountValue { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; } = 0;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<OrderPromotion> OrderPromotions { get; set; } = new List<OrderPromotion>();
    }

    // ============================================================
    // PHẦN 4: INVENTORY
    // ============================================================

    [Table("Ingredient")]
    public class Ingredient
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
        [Required, MaxLength(20)] public string Unit { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<BranchIngredient> BranchIngredients { get; set; } = new List<BranchIngredient>();
        public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    }

    [Table("Branch_Ingredient")]
    public class BranchIngredient
    {
        public int BranchId { get; set; }
        public int IngredientId { get; set; }
        [Column(TypeName = "decimal(12,2)")] public decimal CurrentStock { get; set; } = 0;
        [Column(TypeName = "decimal(12,2)")] public decimal CostPerUnit { get; set; } = 0;
        [Column(TypeName = "decimal(12,2)")] public decimal MinStockThreshold { get; set; } = 0;

        [ForeignKey("BranchId")] public Branch Branch { get; set; } = null!;
        [ForeignKey("IngredientId")] public Ingredient Ingredient { get; set; } = null!;
    }

    [Table("Import_Receipt")]
    public class ImportReceipt
    {
        [Key] public long Id { get; set; }
        public int BranchId { get; set; }
        public int SupplierId { get; set; }
        public int? EmployeeId { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal TotalAmount { get; set; } = 0;
        [MaxLength(20)] public string Status { get; set; } = "COMPLETED";
        [MaxLength(500)] public string? Note { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("BranchId")] public Branch Branch { get; set; } = null!;
        [ForeignKey("SupplierId")] public Supplier Supplier { get; set; } = null!;
        [ForeignKey("EmployeeId")] public Employee? Employee { get; set; }
        public ICollection<ImportReceiptItem> Items { get; set; } = new List<ImportReceiptItem>();
    }

    [Table("Import_Receipt_Item")]
    public class ImportReceiptItem
    {
        [Key] public long Id { get; set; }
        public long ImportReceiptId { get; set; }
        public int IngredientId { get; set; }
        [Column(TypeName = "decimal(12,2)")] public decimal Quantity { get; set; }
        [Column(TypeName = "decimal(12,2)")] public decimal UnitPrice { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column(TypeName = "decimal(12,2)")] public decimal TotalPrice { get; set; }

        [ForeignKey("ImportReceiptId")] public ImportReceipt ImportReceipt { get; set; } = null!;
        [ForeignKey("IngredientId")] public Ingredient Ingredient { get; set; } = null!;
    }

    [Table("Inventory_Transaction")]
    public class InventoryTransaction
    {
        [Key] public long Id { get; set; }
        public int BranchId { get; set; }
        public int IngredientId { get; set; }
        public int? EmployeeId { get; set; }
        public long? ImportReceiptId { get; set; }
        [Required, MaxLength(20)] public string Type { get; set; } = string.Empty;
        [Column(TypeName = "decimal(12,2)")] public decimal Quantity { get; set; }
        [MaxLength(255)] public string? Note { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("BranchId")] public Branch Branch { get; set; } = null!;
        [ForeignKey("IngredientId")] public Ingredient Ingredient { get; set; } = null!;
        [ForeignKey("EmployeeId")] public Employee? Employee { get; set; }
        [ForeignKey("ImportReceiptId")] public ImportReceipt? ImportReceipt { get; set; }
    }

    // ============================================================
    // PHẦN 5: MENU
    // ============================================================

    [Table("Category")]
    public class Category
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;

        public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }

    [Table("Menu_Item")]
    public class MenuItem
    {
        [Key] public int Id { get; set; }
        public int CategoryId { get; set; }
        [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
        [Column(TypeName = "decimal(12,2)")] public decimal Price { get; set; }
        [MaxLength(500)] public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; } = true;
        [MaxLength(500)] public string? Description { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("CategoryId")] public Category Category { get; set; } = null!;
        public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
        public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    [Table("Recipe")]
    public class Recipe
    {
        public int MenuItemId { get; set; }
        public int IngredientId { get; set; }
        [Column(TypeName = "decimal(12,4)")] public decimal QuantityRequired { get; set; }

        [ForeignKey("MenuItemId")] public MenuItem MenuItem { get; set; } = null!;
        [ForeignKey("IngredientId")] public Ingredient Ingredient { get; set; } = null!;
    }

    [Table("Price_History")]
    public class PriceHistory
    {
        [Key] public long Id { get; set; }
        public int MenuItemId { get; set; }
        [Column(TypeName = "decimal(12,2)")] public decimal OldPrice { get; set; }
        [Column(TypeName = "decimal(12,2)")] public decimal NewPrice { get; set; }
        public int? ChangedBy { get; set; }
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }

        [ForeignKey("MenuItemId")] public MenuItem MenuItem { get; set; } = null!;
        [ForeignKey("ChangedBy")] public Account? Account { get; set; }
    }

    [Table("Option")]
    public class Option
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(50)] public string Name { get; set; } = string.Empty;

        public ICollection<OrderItemOption> OrderItemOptions { get; set; } = new List<OrderItemOption>();
    }

    // ============================================================
    // PHẦN 6: OPERATIONS
    // ============================================================

    [Table("Table_Cafe")]
    public class TableCafe
    {
        [Key] public int Id { get; set; }
        public int BranchId { get; set; }
        [Required, MaxLength(50)] public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; } = 4;
        [MaxLength(20)] public string Status { get; set; } = "EMPTY";
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("BranchId")] public Branch Branch { get; set; } = null!;
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }

    [Table("Work_Shift")]
    public class WorkShift
    {
        [Key] public long Id { get; set; }
        public int BranchId { get; set; }
        public int EmployeeId { get; set; }
        public int? AccountId { get; set; }
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal StartingCash { get; set; } = 0;
        [Column(TypeName = "decimal(15,2)")] public decimal? ActualEndingCash { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? ExpectedCash { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? Difference { get; set; }
        [MaxLength(20)] public string Status { get; set; } = "OPEN";
        [MaxLength(500)] public string? Note { get; set; }

        [ForeignKey("BranchId")] public Branch Branch { get; set; } = null!;
        [ForeignKey("EmployeeId")] public Employee Employee { get; set; } = null!;
        [ForeignKey("AccountId")] public Account? Account { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }

    [Table("Order_Status")]
    public class OrderStatus
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(50)] public string Name { get; set; } = string.Empty;

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }

    [Table("Order")]
    public class Order
    {
        [Key] public long Id { get; set; }
        public int BranchId { get; set; }
        public long? ShiftId { get; set; }
        public int? CustomerId { get; set; }
        public int OrderStatusId { get; set; }
        public int? TableId { get; set; }
        [Required, MaxLength(20)] public string OrderType { get; set; } = string.Empty;
        [Column(TypeName = "decimal(15,2)")] public decimal SubTotal { get; set; } = 0;
        [Column(TypeName = "decimal(15,2)")] public decimal DiscountAmount { get; set; } = 0;
        [Column(TypeName = "decimal(15,2)")] public decimal FinalAmount { get; set; } = 0;
        [Column(TypeName = "decimal(15,2)")] public decimal CostAmount { get; set; } = 0;
        [MaxLength(500)] public string? Note { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("BranchId")] public Branch Branch { get; set; } = null!;
        [ForeignKey("ShiftId")] public WorkShift? WorkShift { get; set; }
        [ForeignKey("CustomerId")] public Customer? Customer { get; set; }
        [ForeignKey("OrderStatusId")] public OrderStatus OrderStatus { get; set; } = null!;
        [ForeignKey("TableId")] public TableCafe? Table { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<OrderPromotion> OrderPromotions { get; set; } = new List<OrderPromotion>();
        public ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public Delivery? Delivery { get; set; }
    }

    [Table("Order_Status_History")]
    public class OrderStatusHistory
    {
        [Key] public long Id { get; set; }
        public long OrderId { get; set; }
        public int StatusId { get; set; }
        public int? ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        [MaxLength(255)] public string? Note { get; set; }

        [ForeignKey("OrderId")] public Order Order { get; set; } = null!;
        [ForeignKey("StatusId")] public OrderStatus Status { get; set; } = null!;
        [ForeignKey("ChangedBy")] public Account? Account { get; set; }
    }

    [Table("Order_Promotion")]
    public class OrderPromotion
    {
        public long OrderId { get; set; }
        public int PromotionId { get; set; }
        [Column(TypeName = "decimal(12,2)")] public decimal DiscountValue { get; set; } = 0;

        [ForeignKey("OrderId")] public Order Order { get; set; } = null!;
        [ForeignKey("PromotionId")] public Promotion Promotion { get; set; } = null!;
    }

    // ============================================================
    // PHẦN 7: ORDER ITEMS
    // ============================================================

    [Table("Order_Item")]
    public class OrderItem
    {
        [Key] public long Id { get; set; }
        public long OrderId { get; set; }
        public int MenuItemId { get; set; }
        public int Quantity { get; set; } = 1;
        [Column(TypeName = "decimal(12,2)")] public decimal UnitPrice { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column(TypeName = "decimal(12,2)")] public decimal TotalPrice { get; set; }
        [MaxLength(255)] public string? Note { get; set; }

        [ForeignKey("OrderId")] public Order Order { get; set; } = null!;
        [ForeignKey("MenuItemId")] public MenuItem MenuItem { get; set; } = null!;
        public ICollection<OrderItemOption> Options { get; set; } = new List<OrderItemOption>();
    }

    [Table("Order_Item_Option")]
    public class OrderItemOption
    {
        public long OrderItemId { get; set; }
        public int OptionId { get; set; }

        [ForeignKey("OrderItemId")] public OrderItem OrderItem { get; set; } = null!;
        [ForeignKey("OptionId")] public Option Option { get; set; } = null!;
    }

    // ============================================================
    // PHẦN 8: PAYMENT & DELIVERY
    // ============================================================

    [Table("Payment")]
    public class Payment
    {
        [Key] public long Id { get; set; }
        public long OrderId { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal Amount { get; set; }
        [Required, MaxLength(20)] public string Method { get; set; } = string.Empty;
        [MaxLength(20)] public string Status { get; set; } = "SUCCESS";
        [MaxLength(100)] public string? Reference { get; set; }
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("OrderId")] public Order Order { get; set; } = null!;
        public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
    }

    [Table("Refund")]
    public class Refund
    {
        [Key] public long Id { get; set; }
        public long OrderId { get; set; }
        public long? PaymentId { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal Amount { get; set; }
        [Required, MaxLength(500)] public string Reason { get; set; } = string.Empty;
        public int? ProcessedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("OrderId")] public Order Order { get; set; } = null!;
        [ForeignKey("PaymentId")] public Payment? Payment { get; set; }
        [ForeignKey("ProcessedBy")] public Account? Account { get; set; }
    }

    [Table("Delivery")]
    public class Delivery
    {
        [Key] public long Id { get; set; }
        public long OrderId { get; set; }
        [Required, MaxLength(500)] public string Address { get; set; } = string.Empty;
        [Required, MaxLength(15)] public string Phone { get; set; } = string.Empty;
        [MaxLength(100)] public string? ShipperName { get; set; }
        [MaxLength(15)] public string? ShipperPhone { get; set; }
        public DateTime? EstimatedTime { get; set; }
        public DateTime? DeliveredAt { get; set; }
        [MaxLength(20)] public string Status { get; set; } = "PREPARING";
        [Column(TypeName = "decimal(12,2)")] public decimal DeliveryFee { get; set; } = 0;
        [MaxLength(255)] public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("OrderId")] public Order Order { get; set; } = null!;
    }
}