namespace CafeManagement.DTOs
{
    // ================================================================
    // AUTH
    // ================================================================

    public record LoginRequest(string Username, string Password);

    public record LoginResponse(string Token, string Username, string Role, int EmployeeId, string FullName);

    // ================================================================
    // BRANCH
    // ================================================================

    public record BranchDto(int Id, string Name, string Address, string? Phone, string? Email);
    public record CreateBranchRequest(string Name, string Address, string? Phone, string? Email);
    public record UpdateBranchRequest(string? Name, string? Address, string? Phone, string? Email);

    // ================================================================
    // EMPLOYEE
    // ================================================================

    public record EmployeeDto(
        int Id, int BranchId, string BranchName,
        string FullName, string Phone, string? Email,
        string? Position, decimal Salary, DateOnly? HiredDate);

    public record CreateEmployeeRequest(
        int BranchId, string FullName, string Phone,
        string? Email, string? Position, decimal Salary, DateOnly? HiredDate,
        // Tuỳ chọn: tạo tài khoản đăng nhập ngay khi thêm nhân viên
        string? Username = null, string? Password = null, string? Role = null);

    public record UpdateEmployeeRequest(
        string? FullName, string? Phone, string? Email,
        string? Position, decimal? Salary, DateOnly? HiredDate, int? BranchId);

    // ================================================================
    // ACCOUNT
    // ================================================================

    public record AccountDto(int Id, int EmployeeId, string FullName, string Username, string RoleName);
    public record CreateAccountRequest(int EmployeeId, int RoleId, string Username, string Password);
    public record ChangePasswordRequest(string OldPassword, string NewPassword);

    // ================================================================
    // CUSTOMER
    // ================================================================

    public record CustomerDto(
        int Id, string FullName, string Phone, string? Email,
        DateOnly? Birthday, int Points, string MemberTier);

    public record CreateCustomerRequest(
        string FullName, string Phone, string? Email, DateOnly? Birthday);

    public record UpdateCustomerRequest(
        string? FullName, string? Phone, string? Email, DateOnly? Birthday);

    // ================================================================
    // CATEGORY & MENU ITEM
    // ================================================================

    public record CategoryDto(int Id, string Name, int ItemCount);
    public record CreateCategoryRequest(string Name);

    public record MenuItemDto(
        int Id, int CategoryId, string CategoryName,
        string Name, decimal Price, string? ImageUrl,
        bool IsAvailable, string? Description);

    public record CreateMenuItemRequest(
        int CategoryId, string Name, decimal Price,
        string? ImageUrl, bool IsAvailable, string? Description);

    public record UpdateMenuItemRequest(
        string? Name, decimal? Price, string? ImageUrl,
        bool? IsAvailable, string? Description, int? CategoryId);

    // ================================================================
    // TABLE
    // ================================================================

    public record TableDto(int Id, int BranchId, string BranchName, string Name, int Capacity, string Status);
    public record CreateTableRequest(int BranchId, string Name, int Capacity);
    public record UpdateTableStatusRequest(string Status);

    // ================================================================
    // WORK SHIFT
    // ================================================================

    public record WorkShiftDto(
        long Id, int BranchId, int EmployeeId, string EmployeeName,
        DateTime StartTime, DateTime? EndTime,
        decimal StartingCash, decimal? ActualEndingCash,
        decimal? ExpectedCash, decimal? Difference, string Status);

    public record OpenShiftRequest(int BranchId, int EmployeeId, decimal StartingCash);
    public record CloseShiftRequest(decimal ActualEndingCash, string? Note);

    // ================================================================
    // ORDER
    // ================================================================

    public record OrderDto(
        long Id, int BranchId, string BranchName,
        long? ShiftId, int? CustomerId, string? CustomerName,
        string OrderStatus, int? TableId, string? TableName,
        string OrderType, decimal SubTotal, decimal DiscountAmount,
        decimal FinalAmount, decimal CostAmount,
        string? Note, DateTime CreatedAt,
        List<OrderItemDto> Items,
        List<OrderPromotionDto> Promotions);

    public record OrderSummaryDto(
        long Id, string OrderType, string OrderStatus,
        int? TableId, string? TableName, string? CustomerName,
        decimal FinalAmount, DateTime CreatedAt);

    public record CreateOrderRequest(
        int BranchId, long ShiftId, int? CustomerId,
        int? TableId, string OrderType, string? Note);

    public record OrderItemDto(
        long Id, int MenuItemId, string ItemName,
        int Quantity, decimal UnitPrice, decimal TotalPrice,
        string? Note, List<string> Options);

    public record AddOrderItemRequest(
        int MenuItemId, int Quantity, string? Note, List<int>? OptionIds);

    public record UpdateOrderItemRequest(int? Quantity, string? Note, List<int>? OptionIds);

    public record UpdateItemNoteRequest(string? Note);

    public record OrderPromotionDto(int PromotionId, string PromotionName, decimal DiscountValue);
    public record ApplyPromotionRequest(string PromotionCode);

    public record UpdateOrderStatusRequest(string Status, string? Note);

    // ================================================================
    // PAYMENT
    // ================================================================

    public record PaymentDto(long Id, long OrderId, decimal Amount, string Method, string Status, DateTime PaidAt);
    public record CreatePaymentRequest(long OrderId, decimal Amount, string Method, string? Reference);

    public record RefundDto(long Id, long OrderId, decimal Amount, string Reason, DateTime CreatedAt);
    public record CreateRefundRequest(long OrderId, long? PaymentId, decimal Amount, string Reason);

    // ================================================================
    // DELIVERY
    // ================================================================

    public record DeliveryDto(
        long Id, long OrderId, string Address, string Phone,
        string? ShipperName, string? ShipperPhone,
        DateTime? EstimatedTime, DateTime? DeliveredAt,
        string Status, decimal DeliveryFee);

    public record CreateDeliveryRequest(
        long OrderId, string Address, string Phone,
        string? ShipperName, string? ShipperPhone,
        DateTime? EstimatedTime, decimal DeliveryFee);

    public record UpdateDeliveryStatusRequest(string Status, string? ShipperName, string? ShipperPhone, DateTime? EstimatedTime);

    // ================================================================
    // INVENTORY
    // ================================================================

    public record IngredientDto(int Id, string Name, string Unit);
    public record CreateIngredientRequest(string Name, string Unit);

    public record BranchIngredientDto(
        int BranchId, int IngredientId, string IngredientName,
        string Unit, decimal CurrentStock, decimal CostPerUnit, decimal MinStockThreshold,
        bool IsLowStock);

    public record UpdateStockRequest(decimal CurrentStock, decimal CostPerUnit, decimal MinStockThreshold);
    public record AdjustStockRequest(int BranchId, int IngredientId, decimal NewQuantity, string? Note);

    public record ImportReceiptDto(
        long Id, int BranchId, string BranchName,
        int SupplierId, string SupplierName,
        decimal TotalAmount, string Status, string? Note,
        DateTime CreatedAt, List<ImportReceiptItemDto> Items);

    public record ImportReceiptItemDto(
        long Id, int IngredientId, string IngredientName,
        decimal Quantity, decimal UnitPrice, decimal TotalPrice);

    public record CreateImportReceiptRequest(
        int BranchId, int SupplierId, int? EmployeeId,
        string? Note, List<CreateImportReceiptItemRequest> Items);

    public record CreateImportReceiptItemRequest(int IngredientId, decimal Quantity, decimal UnitPrice);

    // ================================================================
    // RECIPE
    // ================================================================

    public record UpsertRecipeRequest(int MenuItemId, int IngredientId, decimal QuantityRequired);

    // ================================================================
    // SUPPLIER
    // ================================================================

    public record SupplierDto(int Id, string Name, string? Phone, string? Email, string? Address);
    public record CreateSupplierRequest(string Name, string? Phone, string? Email, string? Address);
    public record UpdateSupplierRequest(string? Name, string? Phone, string? Email, string? Address);

    // ================================================================
    // PROMOTION
    // ================================================================

    public record PromotionDto(
        int Id, string Name, string? Code,
        string DiscountType, decimal Value,
        decimal? MinOrderValue, decimal? MaxDiscountValue,
        int? UsageLimit, int UsedCount,
        DateTime? StartDate, DateTime? EndDate);

    public record CreatePromotionRequest(
        string Name, string? Code, string DiscountType, decimal Value,
        decimal? MinOrderValue, decimal? MaxDiscountValue,
        int? UsageLimit, DateTime? StartDate, DateTime? EndDate);

    public record UpdatePromotionRequest(
        string? Name, string? Code, string? DiscountType, decimal? Value,
        decimal? MinOrderValue, decimal? MaxDiscountValue,
        int? UsageLimit, DateTime? StartDate, DateTime? EndDate);

    // ================================================================
    // DASHBOARD
    // ================================================================

    public record DashboardTodayDto(
        int BranchId, int TotalOrders, decimal TotalRevenue,
        decimal TotalCost, decimal TotalProfit,
        int CancelledOrders, decimal AvgOrderValue);

    public record TopMenuItemDto(
        int MenuItemId, string ItemName, string CategoryName,
        int TotalQuantity, decimal TotalRevenue, int Rank);

    // ================================================================
    // COMMON
    // ================================================================

    public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize)
    {
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public record ApiResponse<T>(bool Success, string? Message, T? Data);
}