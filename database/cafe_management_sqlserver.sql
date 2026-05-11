-- ================================================================
--  PBL3 CAFE MANAGEMENT — SQL SERVER (T-SQL) COMPLETE SCRIPT
--  Tương thích: SQL Server 2016+ / SSMS
--  Bao gồm: Schema + Constraints + Indexes + Sample Data
-- ================================================================

USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'pbl3_cafe_management')
BEGIN
    ALTER DATABASE pbl3_cafe_management SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE pbl3_cafe_management;
END
GO

CREATE DATABASE pbl3_cafe_management
    COLLATE Vietnamese_CI_AS;
GO

USE pbl3_cafe_management;
GO

-- ================================================================
-- PHẦN 1: SCALABILITY — CHI NHÁNH & NHÀ CUNG CẤP
-- ================================================================

CREATE TABLE Branch (
    id          INT             IDENTITY(1,1) PRIMARY KEY,
    name        NVARCHAR(100)   NOT NULL,
    address     NVARCHAR(255)   NOT NULL,
    phone       VARCHAR(20)     NULL,
    email       VARCHAR(100)    NULL,
    is_deleted  BIT             NOT NULL DEFAULT 0,
    deleted_at  DATETIME2       NULL,
    created_at  DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    updated_at  DATETIME2       NOT NULL DEFAULT SYSDATETIME()
);
GO

CREATE TABLE Supplier (
    id          INT             IDENTITY(1,1) PRIMARY KEY,
    name        NVARCHAR(100)   NOT NULL,
    phone       VARCHAR(20)     NOT NULL,
    address     NVARCHAR(255)   NULL,
    email       VARCHAR(100)    NULL,
    is_deleted  BIT             NOT NULL DEFAULT 0,
    deleted_at  DATETIME2       NULL,
    created_at  DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    updated_at  DATETIME2       NOT NULL DEFAULT SYSDATETIME()
);
GO

-- ================================================================
-- PHẦN 2: HR & SECURITY — NHÂN VIÊN, VAI TRÒ, TÀI KHOẢN
-- ================================================================

CREATE TABLE Employee (
    id          INT             IDENTITY(1,1) PRIMARY KEY,
    branch_id   INT             NOT NULL,
    full_name   NVARCHAR(100)   NOT NULL,
    phone       VARCHAR(15)     NOT NULL,
    email       VARCHAR(100)    NULL,
    -- Thông tin nghiệp vụ bổ sung
    position    NVARCHAR(50)    NULL,           -- Pha chế / Thu ngân / Phục vụ / Quản lý
    salary      DECIMAL(15,2)   NOT NULL DEFAULT 0,
    hired_date  DATE            NULL,
    -- Soft delete
    is_deleted  BIT             NOT NULL DEFAULT 0,
    deleted_at  DATETIME2       NULL,
    created_at  DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    updated_at  DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT UQ_Employee_Phone UNIQUE (phone),
    CONSTRAINT FK_Employee_Branch FOREIGN KEY (branch_id) REFERENCES Branch(id)
);
GO

CREATE TABLE Role (
    id      INT             IDENTITY(1,1) PRIMARY KEY,
    name    NVARCHAR(50)    NOT NULL,
    CONSTRAINT UQ_Role_Name UNIQUE (name)
);
GO

CREATE TABLE Account (
    id              INT             IDENTITY(1,1) PRIMARY KEY,
    employee_id     INT             NOT NULL,
    role_id         INT             NOT NULL,
    username        VARCHAR(50)     NOT NULL,
    password_hash   VARCHAR(255)    NOT NULL,
    -- Soft delete
    is_deleted      BIT             NOT NULL DEFAULT 0,
    deleted_at      DATETIME2       NULL,
    created_at      DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    updated_at      DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT UQ_Account_Employee  UNIQUE (employee_id),
    CONSTRAINT UQ_Account_Username  UNIQUE (username),
    CONSTRAINT FK_Account_Employee  FOREIGN KEY (employee_id) REFERENCES Employee(id),
    CONSTRAINT FK_Account_Role      FOREIGN KEY (role_id)     REFERENCES Role(id)
);
GO

-- Log đăng nhập (ai, lúc nào, từ IP nào)
CREATE TABLE Login_Log (
    id          BIGINT          IDENTITY(1,1) PRIMARY KEY,
    account_id  INT             NOT NULL,
    ip_address  VARCHAR(50)     NULL,
    user_agent  NVARCHAR(255)   NULL,
    login_at    DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    is_success  BIT             NOT NULL DEFAULT 1,
    CONSTRAINT FK_LoginLog_Account FOREIGN KEY (account_id) REFERENCES Account(id)
);
GO

-- Audit log: ai làm gì với bảng nào, record nào
CREATE TABLE Audit_Log (
    id          BIGINT          IDENTITY(1,1) PRIMARY KEY,
    account_id  INT             NOT NULL,
    action      VARCHAR(20)     NOT NULL,   -- CREATE / UPDATE / DELETE
    entity      VARCHAR(50)     NOT NULL,   -- Tên bảng bị tác động
    entity_id   BIGINT          NULL,       -- ID của record bị tác động
    old_value   NVARCHAR(MAX)   NULL,       -- Giá trị cũ (JSON)
    new_value   NVARCHAR(MAX)   NULL,       -- Giá trị mới (JSON)
    created_at  DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_AuditLog_Account FOREIGN KEY (account_id) REFERENCES Account(id)
);
GO

-- ================================================================
-- PHẦN 3: CRM — KHÁCH HÀNG & KHUYẾN MÃI
-- ================================================================

CREATE TABLE Customer (
    id              INT             IDENTITY(1,1) PRIMARY KEY,
    full_name       NVARCHAR(100)   NOT NULL,
    phone           VARCHAR(15)     NOT NULL,
    email           VARCHAR(100)    NULL,
    birthday        DATE            NULL,
    points          INT             NOT NULL DEFAULT 0,  -- Điểm tích lũy
    member_tier     VARCHAR(20)     NOT NULL DEFAULT 'BRONZE', -- BRONZE / SILVER / GOLD
    -- Soft delete
    is_deleted      BIT             NOT NULL DEFAULT 0,
    deleted_at      DATETIME2       NULL,
    created_at      DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    updated_at      DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT UQ_Customer_Phone UNIQUE (phone),
    CONSTRAINT CK_Customer_Points CHECK (points >= 0),
    CONSTRAINT CK_Customer_Tier  CHECK (member_tier IN ('BRONZE','SILVER','GOLD'))
);
GO

CREATE TABLE Promotion (
    id                  INT             IDENTITY(1,1) PRIMARY KEY,
    name                NVARCHAR(100)   NOT NULL,
    code                VARCHAR(50)     NULL,           -- Mã coupon (có thể NULL nếu tự động)
    discount_type       VARCHAR(20)     NOT NULL,       -- PERCENTAGE / FIXED
    value               DECIMAL(12,2)   NOT NULL,       -- % hoặc số tiền cụ thể
    min_order_value     DECIMAL(12,2)   NULL,           -- Đơn tối thiểu để áp dụng
    max_discount_value  DECIMAL(12,2)   NULL,           -- Giảm tối đa (dùng khi PERCENTAGE)
    usage_limit         INT             NULL,           -- Tổng số lần dùng tối đa
    used_count          INT             NOT NULL DEFAULT 0, -- Đã dùng bao nhiêu lần
    start_date          DATETIME2       NULL,
    end_date            DATETIME2       NULL,
    -- Soft delete
    is_deleted          BIT             NOT NULL DEFAULT 0,
    deleted_at          DATETIME2       NULL,
    created_at          DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    updated_at          DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT UQ_Promotion_Code    UNIQUE (code),
    CONSTRAINT CK_Promotion_Type    CHECK (discount_type IN ('PERCENTAGE','FIXED')),
    CONSTRAINT CK_Promotion_Value   CHECK (value > 0),
    CONSTRAINT CK_Promotion_Used    CHECK (used_count >= 0)
);
GO

-- ================================================================
-- PHẦN 4: INVENTORY — NGUYÊN LIỆU & KHO
-- ================================================================

CREATE TABLE Ingredient (
    id          INT             IDENTITY(1,1) PRIMARY KEY,
    name        NVARCHAR(100)   NOT NULL,
    unit        NVARCHAR(20)    NOT NULL,   -- kg / lít / gói / lon...
    -- Soft delete
    is_deleted  BIT             NOT NULL DEFAULT 0,
    deleted_at  DATETIME2       NULL,
    created_at  DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    updated_at  DATETIME2       NOT NULL DEFAULT SYSDATETIME()
);
GO

-- Tồn kho nguyên liệu theo từng chi nhánh
CREATE TABLE Branch_Ingredient (
    branch_id               INT             NOT NULL,
    ingredient_id           INT             NOT NULL,
    current_stock           DECIMAL(12,2)   NOT NULL DEFAULT 0,
    cost_per_unit           DECIMAL(12,2)   NOT NULL DEFAULT 0,
    min_stock_threshold     DECIMAL(12,2)   NOT NULL DEFAULT 0,   -- Ngưỡng cảnh báo sắp hết
    CONSTRAINT PK_Branch_Ingredient PRIMARY KEY (branch_id, ingredient_id),
    CONSTRAINT FK_BI_Branch         FOREIGN KEY (branch_id)     REFERENCES Branch(id),
    CONSTRAINT FK_BI_Ingredient     FOREIGN KEY (ingredient_id) REFERENCES Ingredient(id),
    CONSTRAINT CK_BI_Stock          CHECK (current_stock >= 0),
    CONSTRAINT CK_BI_Cost           CHECK (cost_per_unit >= 0),
    CONSTRAINT CK_BI_MinThreshold   CHECK (min_stock_threshold >= 0)
);
GO

-- Phiếu nhập kho
CREATE TABLE Import_Receipt (
    id              BIGINT          IDENTITY(1,1) PRIMARY KEY,
    branch_id       INT             NOT NULL,
    supplier_id     INT             NOT NULL,
    employee_id     INT             NULL,           -- Nhân viên lập phiếu
    total_amount    DECIMAL(15,2)   NOT NULL DEFAULT 0,
    status          VARCHAR(20)     NOT NULL DEFAULT 'COMPLETED', -- PENDING / COMPLETED / CANCELLED
    note            NVARCHAR(500)   NULL,
    -- Soft delete
    is_deleted      BIT             NOT NULL DEFAULT 0,
    deleted_at      DATETIME2       NULL,
    created_at      DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    updated_at      DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_IR_Branch     FOREIGN KEY (branch_id)   REFERENCES Branch(id),
    CONSTRAINT FK_IR_Supplier   FOREIGN KEY (supplier_id) REFERENCES Supplier(id),
    CONSTRAINT FK_IR_Employee   FOREIGN KEY (employee_id) REFERENCES Employee(id),
    CONSTRAINT CK_IR_Status     CHECK (status IN ('PENDING','COMPLETED','CANCELLED'))
);
GO

-- Chi tiết phiếu nhập
CREATE TABLE Import_Receipt_Item (
    id                  BIGINT          IDENTITY(1,1) PRIMARY KEY,
    import_receipt_id   BIGINT          NOT NULL,
    ingredient_id       INT             NOT NULL,
    quantity            DECIMAL(12,2)   NOT NULL,
    unit_price          DECIMAL(12,2)   NOT NULL,
    total_price         AS (quantity * unit_price) PERSISTED,  -- Computed column
    CONSTRAINT FK_IRI_Receipt    FOREIGN KEY (import_receipt_id) REFERENCES Import_Receipt(id),
    CONSTRAINT FK_IRI_Ingredient FOREIGN KEY (ingredient_id)     REFERENCES Ingredient(id),
    CONSTRAINT CK_IRI_Quantity   CHECK (quantity > 0),
    CONSTRAINT CK_IRI_UnitPrice  CHECK (unit_price >= 0)
);
GO

-- Giao dịch kho (nhập / xuất / điều chỉnh)
CREATE TABLE Inventory_Transaction (
    id                  BIGINT          IDENTITY(1,1) PRIMARY KEY,
    branch_id           INT             NOT NULL,
    ingredient_id       INT             NOT NULL,
    employee_id         INT             NULL,           -- Ai thực hiện
    import_receipt_id   BIGINT          NULL,           -- Liên kết phiếu nhập (nếu có)
    type                VARCHAR(20)     NOT NULL,       -- IMPORT / EXPORT / ADJUST
    quantity            DECIMAL(12,2)   NOT NULL,       -- Dương = nhập vào, Âm = xuất ra
    note                NVARCHAR(255)   NULL,
    -- Soft delete
    is_deleted          BIT             NOT NULL DEFAULT 0,
    deleted_at          DATETIME2       NULL,
    created_at          DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    updated_at          DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_IT_Branch     FOREIGN KEY (branch_id)         REFERENCES Branch(id),
    CONSTRAINT FK_IT_Ingredient FOREIGN KEY (ingredient_id)     REFERENCES Ingredient(id),
    CONSTRAINT FK_IT_Employee   FOREIGN KEY (employee_id)       REFERENCES Employee(id),
    CONSTRAINT FK_IT_Receipt    FOREIGN KEY (import_receipt_id) REFERENCES Import_Receipt(id),
    CONSTRAINT CK_IT_Type       CHECK (type IN ('IMPORT','EXPORT','ADJUST'))
);
GO

-- ================================================================
-- PHẦN 5: MENU — DANH MỤC, MÓN, CÔNG THỨC, LỊCH SỬ GIÁ
-- ================================================================

CREATE TABLE Category (
    id      INT             IDENTITY(1,1) PRIMARY KEY,
    name    NVARCHAR(100)   NOT NULL,
    CONSTRAINT UQ_Category_Name UNIQUE (name)
);
GO

CREATE TABLE Menu_Item (
    id              INT             IDENTITY(1,1) PRIMARY KEY,
    category_id     INT             NOT NULL,
    name            NVARCHAR(100)   NOT NULL,
    price           DECIMAL(12,2)   NOT NULL,
    image_url       NVARCHAR(500)   NULL,
    is_available    BIT             NOT NULL DEFAULT 1,  -- Đang bán / Đang ẩn (toggle UI)
    description     NVARCHAR(500)   NULL,
    -- Soft delete
    is_deleted      BIT             NOT NULL DEFAULT 0,
    deleted_at      DATETIME2       NULL,
    created_at      DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    updated_at      DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_MenuItem_Category FOREIGN KEY (category_id) REFERENCES Category(id),
    CONSTRAINT CK_MenuItem_Price    CHECK (price >= 0)
);
GO

-- Công thức nguyên liệu cho từng món
CREATE TABLE Recipe (
    menu_item_id        INT             NOT NULL,
    ingredient_id       INT             NOT NULL,
    quantity_required   DECIMAL(12,4)   NOT NULL,   -- Lượng nguyên liệu cần cho 1 phần
    CONSTRAINT PK_Recipe            PRIMARY KEY (menu_item_id, ingredient_id),
    CONSTRAINT FK_Recipe_MenuItem   FOREIGN KEY (menu_item_id)  REFERENCES Menu_Item(id),
    CONSTRAINT FK_Recipe_Ingredient FOREIGN KEY (ingredient_id) REFERENCES Ingredient(id),
    CONSTRAINT CK_Recipe_Qty        CHECK (quantity_required > 0)
);
GO

-- Lịch sử thay đổi giá món
CREATE TABLE Price_History (
    id              BIGINT          IDENTITY(1,1) PRIMARY KEY,
    menu_item_id    INT             NOT NULL,
    old_price       DECIMAL(12,2)   NOT NULL,
    new_price       DECIMAL(12,2)   NOT NULL,
    changed_by      INT             NULL,       -- account_id người thay đổi
    start_date      DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    end_date        DATETIME2       NULL,
    CONSTRAINT FK_PH_MenuItem   FOREIGN KEY (menu_item_id) REFERENCES Menu_Item(id),
    CONSTRAINT FK_PH_Account    FOREIGN KEY (changed_by)   REFERENCES Account(id)
);
GO

-- Tùy chọn món: ít đá, ít đường, không đường...
CREATE TABLE [Option] (
    id      INT             IDENTITY(1,1) PRIMARY KEY,
    name    NVARCHAR(50)    NOT NULL,
    CONSTRAINT UQ_Option_Name UNIQUE (name)
);
GO

-- ================================================================
-- PHẦN 6: VẬN HÀNH — BÀN, CA LÀM VIỆC, ORDER
-- ================================================================

CREATE TABLE Table_Cafe (
    id          INT             IDENTITY(1,1) PRIMARY KEY,
    branch_id   INT             NOT NULL,
    name        NVARCHAR(50)    NOT NULL,
    capacity    INT             NOT NULL DEFAULT 4,         -- Số ghế
    status      VARCHAR(20)     NOT NULL DEFAULT 'EMPTY',  -- EMPTY / OCCUPIED / CLEANING
    -- Soft delete
    is_deleted  BIT             NOT NULL DEFAULT 0,
    deleted_at  DATETIME2       NULL,
    created_at  DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    updated_at  DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Table_Branch  FOREIGN KEY (branch_id) REFERENCES Branch(id),
    CONSTRAINT CK_Table_Status  CHECK (status IN ('EMPTY','OCCUPIED','CLEANING')),
    CONSTRAINT CK_Table_Cap     CHECK (capacity > 0)
);
GO

-- Ca làm việc — chốt két
CREATE TABLE Work_Shift (
    id                  BIGINT          IDENTITY(1,1) PRIMARY KEY,
    branch_id           INT             NOT NULL,
    employee_id         INT             NOT NULL,   -- Nhân viên phụ trách ca
    account_id          INT             NULL,       -- Account đăng nhập mở ca
    start_time          DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    end_time            DATETIME2       NULL,
    starting_cash       DECIMAL(15,2)   NOT NULL DEFAULT 0,     -- Tiền lẻ đầu ca
    actual_ending_cash  DECIMAL(15,2)   NULL,                   -- Thực tế đếm được cuối ca
    expected_cash       DECIMAL(15,2)   NULL,                   -- Dự kiến (tính từ doanh thu)
    difference          DECIMAL(15,2)   NULL,                   -- Chênh lệch (thừa/thiếu)
    status              VARCHAR(20)     NOT NULL DEFAULT 'OPEN', -- OPEN / CLOSED
    note                NVARCHAR(500)   NULL,
    CONSTRAINT FK_Shift_Branch      FOREIGN KEY (branch_id)   REFERENCES Branch(id),
    CONSTRAINT FK_Shift_Employee    FOREIGN KEY (employee_id) REFERENCES Employee(id),
    CONSTRAINT FK_Shift_Account     FOREIGN KEY (account_id)  REFERENCES Account(id),
    CONSTRAINT CK_Shift_Status      CHECK (status IN ('OPEN','CLOSED')),
    CONSTRAINT CK_Shift_Cash        CHECK (starting_cash >= 0)
);
GO

-- Bảng tra cứu trạng thái đơn hàng (thay vì ENUM)
CREATE TABLE Order_Status (
    id      INT             IDENTITY(1,1) PRIMARY KEY,
    name    VARCHAR(50)     NOT NULL,
    CONSTRAINT UQ_OrderStatus_Name UNIQUE (name)
);
GO

-- Đơn hàng
CREATE TABLE [Order] (
    id                  BIGINT          IDENTITY(1,1) PRIMARY KEY,
    branch_id           INT             NOT NULL,
    shift_id            BIGINT          NULL,       -- Ca làm việc của thu ngân
    customer_id         INT             NULL,       -- NULL = khách vãng lai
    order_status_id     INT             NOT NULL,
    table_id            INT             NULL,       -- NULL = mang về / giao hàng
    order_type          VARCHAR(20)     NOT NULL,   -- DINE_IN / TAKE_AWAY / DELIVERY
    sub_total           DECIMAL(15,2)   NOT NULL DEFAULT 0,     -- Tổng trước giảm giá
    discount_amount     DECIMAL(15,2)   NOT NULL DEFAULT 0,     -- Tổng tiền được giảm
    final_amount        DECIMAL(15,2)   NOT NULL DEFAULT 0,     -- Thực thu = sub_total - discount
    cost_amount         DECIMAL(15,2)   NOT NULL DEFAULT 0,     -- Chi phí nguyên liệu
    note                NVARCHAR(500)   NULL,
    -- Soft delete
    is_deleted          BIT             NOT NULL DEFAULT 0,
    deleted_at          DATETIME2       NULL,
    created_at          DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    updated_at          DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Order_Branch      FOREIGN KEY (branch_id)       REFERENCES Branch(id),
    CONSTRAINT FK_Order_Shift       FOREIGN KEY (shift_id)        REFERENCES Work_Shift(id),
    CONSTRAINT FK_Order_Customer    FOREIGN KEY (customer_id)     REFERENCES Customer(id),
    CONSTRAINT FK_Order_Table       FOREIGN KEY (table_id)        REFERENCES Table_Cafe(id),
    CONSTRAINT FK_Order_Status      FOREIGN KEY (order_status_id) REFERENCES Order_Status(id),
    CONSTRAINT CK_Order_Type        CHECK (order_type IN ('DINE_IN','TAKE_AWAY','DELIVERY')),
    CONSTRAINT CK_Order_SubTotal    CHECK (sub_total >= 0),
    CONSTRAINT CK_Order_Discount    CHECK (discount_amount >= 0),
    CONSTRAINT CK_Order_Final       CHECK (final_amount >= 0)
);
GO

-- Lịch sử thay đổi trạng thái đơn hàng
CREATE TABLE Order_Status_History (
    id          BIGINT      IDENTITY(1,1) PRIMARY KEY,
    order_id    BIGINT      NOT NULL,
    status_id   INT         NOT NULL,
    changed_by  INT         NULL,       -- account_id
    changed_at  DATETIME2   NOT NULL DEFAULT SYSDATETIME(),
    note        NVARCHAR(255) NULL,
    CONSTRAINT FK_OSH_Order     FOREIGN KEY (order_id)   REFERENCES [Order](id),
    CONSTRAINT FK_OSH_Status    FOREIGN KEY (status_id)  REFERENCES Order_Status(id),
    CONSTRAINT FK_OSH_Account   FOREIGN KEY (changed_by) REFERENCES Account(id)
);
GO

-- Khuyến mãi áp dụng cho đơn hàng
CREATE TABLE Order_Promotion (
    order_id        BIGINT          NOT NULL,
    promotion_id    INT             NOT NULL,
    discount_value  DECIMAL(12,2)   NOT NULL DEFAULT 0,  -- Số tiền thực tế được giảm
    CONSTRAINT PK_Order_Promotion   PRIMARY KEY (order_id, promotion_id),
    CONSTRAINT FK_OP_Order          FOREIGN KEY (order_id)     REFERENCES [Order](id),
    CONSTRAINT FK_OP_Promotion      FOREIGN KEY (promotion_id) REFERENCES Promotion(id)
);
GO

-- ================================================================
-- PHẦN 7: CHI TIẾT ĐƠN HÀNG & TÙY CHỌN
-- ================================================================

CREATE TABLE Order_Item (
    id              BIGINT          IDENTITY(1,1) PRIMARY KEY,
    order_id        BIGINT          NOT NULL,
    menu_item_id    INT             NOT NULL,
    quantity        INT             NOT NULL DEFAULT 1,
    unit_price      DECIMAL(12,2)   NOT NULL,   -- Giá tại thời điểm đặt
    total_price     AS (quantity * unit_price) PERSISTED,   -- Tự động tính
    note            NVARCHAR(255)   NULL,        -- Ghi chú tay: "ít đá ít đường"
    CONSTRAINT FK_OI_Order      FOREIGN KEY (order_id)     REFERENCES [Order](id),
    CONSTRAINT FK_OI_MenuItem   FOREIGN KEY (menu_item_id) REFERENCES Menu_Item(id),
    CONSTRAINT CK_OI_Quantity   CHECK (quantity > 0),
    CONSTRAINT CK_OI_Price      CHECK (unit_price >= 0)
);
GO

-- Tùy chọn cố định cho từng item trong order
CREATE TABLE Order_Item_Option (
    order_item_id   BIGINT  NOT NULL,
    option_id       INT     NOT NULL,
    CONSTRAINT PK_OIO               PRIMARY KEY (order_item_id, option_id),
    CONSTRAINT FK_OIO_OrderItem     FOREIGN KEY (order_item_id) REFERENCES Order_Item(id),
    CONSTRAINT FK_OIO_Option        FOREIGN KEY (option_id)     REFERENCES [Option](id)
);
GO

-- ================================================================
-- PHẦN 8: THANH TOÁN, HOÀN TIỀN, GIAO HÀNG
-- ================================================================

CREATE TABLE Payment (
    id          BIGINT          IDENTITY(1,1) PRIMARY KEY,
    order_id    BIGINT          NOT NULL,
    amount      DECIMAL(15,2)   NOT NULL,
    method      VARCHAR(20)     NOT NULL,       -- CASH / BANK / CARD
    status      VARCHAR(20)     NOT NULL DEFAULT 'SUCCESS', -- SUCCESS / FAILED / PENDING
    reference   NVARCHAR(100)   NULL,           -- Mã giao dịch ngân hàng (nếu có)
    paid_at     DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Payment_Order FOREIGN KEY (order_id) REFERENCES [Order](id),
    CONSTRAINT CK_Payment_Method CHECK (method IN ('CASH','BANK','CARD')),
    CONSTRAINT CK_Payment_Status CHECK (status IN ('SUCCESS','FAILED','PENDING')),
    CONSTRAINT CK_Payment_Amount CHECK (amount > 0)
);
GO

CREATE TABLE Refund (
    id          BIGINT          IDENTITY(1,1) PRIMARY KEY,
    order_id    BIGINT          NOT NULL,
    payment_id  BIGINT          NULL,           -- Hoàn từ payment nào
    amount      DECIMAL(15,2)   NOT NULL,
    reason      NVARCHAR(500)   NOT NULL,
    processed_by INT            NULL,           -- account_id xử lý hoàn tiền
    created_at  DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Refund_Order      FOREIGN KEY (order_id)      REFERENCES [Order](id),
    CONSTRAINT FK_Refund_Payment    FOREIGN KEY (payment_id)    REFERENCES Payment(id),
    CONSTRAINT FK_Refund_Account    FOREIGN KEY (processed_by)  REFERENCES Account(id),
    CONSTRAINT CK_Refund_Amount     CHECK (amount > 0)
);
GO

CREATE TABLE Delivery (
    id              BIGINT          IDENTITY(1,1) PRIMARY KEY,
    order_id        BIGINT          NOT NULL,
    address         NVARCHAR(500)   NOT NULL,
    phone           VARCHAR(15)     NOT NULL,
    shipper_name    NVARCHAR(100)   NULL,
    shipper_phone   VARCHAR(15)     NULL,
    estimated_time  DATETIME2       NULL,
    delivered_at    DATETIME2       NULL,
    status          VARCHAR(20)     NOT NULL DEFAULT 'PREPARING', -- PREPARING / DELIVERING / DELIVERED / FAILED
    delivery_fee    DECIMAL(12,2)   NOT NULL DEFAULT 0,
    note            NVARCHAR(255)   NULL,
    created_at      DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    updated_at      DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Delivery_Order    FOREIGN KEY (order_id) REFERENCES [Order](id),
    CONSTRAINT CK_Delivery_Status   CHECK (status IN ('PREPARING','DELIVERING','DELIVERED','FAILED')),
    CONSTRAINT CK_Delivery_Fee      CHECK (delivery_fee >= 0)
);
GO

-- ================================================================
-- PHẦN 9: INDEXES — TỐI ƯU TRUY VẤN
-- ================================================================

-- Order (bảng hay query nhất)
CREATE INDEX idx_Order_BranchId         ON [Order](branch_id);
CREATE INDEX idx_Order_CreatedAt        ON [Order](created_at);
CREATE INDEX idx_Order_CustomerId       ON [Order](customer_id);
CREATE INDEX idx_Order_StatusId         ON [Order](order_status_id);
CREATE INDEX idx_Order_ShiftId          ON [Order](shift_id);
CREATE INDEX idx_Order_TableId          ON [Order](table_id);

-- Order_Item
CREATE INDEX idx_OrderItem_OrderId      ON Order_Item(order_id);
CREATE INDEX idx_OrderItem_MenuItemId   ON Order_Item(menu_item_id);

-- Inventory
CREATE INDEX idx_IT_BranchIngredient    ON Inventory_Transaction(branch_id, ingredient_id);
CREATE INDEX idx_IT_CreatedAt           ON Inventory_Transaction(created_at);
CREATE INDEX idx_BI_BranchId            ON Branch_Ingredient(branch_id);

-- Customer
CREATE INDEX idx_Customer_Phone         ON Customer(phone);
CREATE INDEX idx_Customer_Tier          ON Customer(member_tier);

-- Menu_Item
CREATE INDEX idx_MenuItem_Category      ON Menu_Item(category_id);
CREATE INDEX idx_MenuItem_Available     ON Menu_Item(is_available) WHERE is_available = 1;

-- Payment
CREATE INDEX idx_Payment_OrderId        ON Payment(order_id);
CREATE INDEX idx_Payment_Method         ON Payment(method);

-- Audit & Login Log
CREATE INDEX idx_AuditLog_AccountId     ON Audit_Log(account_id);
CREATE INDEX idx_AuditLog_CreatedAt     ON Audit_Log(created_at);
CREATE INDEX idx_LoginLog_AccountId     ON Login_Log(account_id);

-- Work_Shift
CREATE INDEX idx_Shift_BranchStatus     ON Work_Shift(branch_id, status);
CREATE INDEX idx_Shift_EmployeeId       ON Work_Shift(employee_id);

GO

-- ================================================================
-- PHẦN 10: DỮ LIỆU MẪU (SAMPLE DATA)
-- ================================================================

-- Trạng thái đơn hàng
INSERT INTO Order_Status (name) VALUES
    ('PENDING'),    -- 1 Chờ xử lý
    ('COOKING'),    -- 2 Đang pha chế
    ('SERVED'),     -- 3 Đã phục vụ
    ('COMPLETED'),  -- 4 Hoàn thành
    ('CANCELLED');  -- 5 Đã huỷ
GO

-- Vai trò hệ thống
INSERT INTO Role (name) VALUES
    ('ADMIN'),
    ('MANAGER'),
    ('STAFF');
GO

-- Tùy chọn món
INSERT INTO [Option] (name) VALUES
    (N'Ít đá'),
    (N'Không đá'),
    (N'Ít đường'),
    (N'Không đường'),
    (N'Đá riêng'),
    (N'Thêm sữa');
GO

-- Chi nhánh
INSERT INTO Branch (name, address, phone, email) VALUES
    (N'Chi nhánh Trung Tâm',    N'123 Lê Lợi, Quận 1, TP.HCM',             '0282345678', 'trungtam@abccoffee.vn'),
    (N'Chi nhánh Quận 3',       N'456 Nguyễn Đình Chiểu, Quận 3, TP.HCM',  '0282345679', 'quan3@abccoffee.vn'),
    (N'Chi nhánh Đà Nẵng',      N'789 Nguyễn Huệ, Hải Châu, Đà Nẵng',     '02363456789','danang@abccoffee.vn');
GO

-- Nhà cung cấp
INSERT INTO Supplier (name, phone, address, email) VALUES
    (N'Cà phê Trung Nguyên',    '0988123456', N'Buôn Ma Thuột, Đắk Lắk',  'supplier@trungnguyen.vn'),
    (N'Sữa Vinamilk',           '0988234567', N'Bình Dương',               'supplier@vinamilk.vn'),
    (N'Trà Lipton',             '0988345678', N'Hà Nội',                   'supplier@lipton.vn');
GO

-- Nhân viên
INSERT INTO Employee (branch_id, full_name, phone, email, position, salary, hired_date) VALUES
    (1, N'Nguyễn Văn An',   '0901000001', 'an.nv@abccoffee.vn',   N'Quản lý',  15000000, '2023-01-01'),
    (1, N'Trần Thị Mai',    '0901000002', 'mai.tt@abccoffee.vn',  N'Thu ngân', 9000000,  '2023-03-01'),
    (1, N'Lê Văn Hùng',     '0901000003', 'hung.lv@abccoffee.vn', N'Pha chế',  8500000,  '2024-01-01'),
    (1, N'Phạm Thị Thu',    '0901000004', 'thu.pt@abccoffee.vn',  N'Phục vụ',  8000000,  '2024-06-15'),
    (2, N'Hoàng Minh Tuấn', '0901000005', 'tuan.hm@abccoffee.vn', N'Quản lý',  14000000, '2023-06-01'),
    (3, N'Võ Thị Lan',      '0901000006', 'lan.vt@abccoffee.vn',  N'Quản lý',  14000000, '2024-01-01');
GO

-- Tài khoản hệ thống
INSERT INTO Account (employee_id, role_id, username, password_hash) VALUES
    (1, 1, 'admin_an',      '$2a$12$exampleHashAdmin001'),   -- ADMIN
    (2, 3, 'staff_mai',     '$2a$12$exampleHashStaff002'),   -- STAFF
    (3, 3, 'staff_hung',    '$2a$12$exampleHashStaff003'),   -- STAFF
    (5, 2, 'manager_tuan',  '$2a$12$exampleHashMgr005'),     -- MANAGER
    (6, 2, 'manager_lan',   '$2a$12$exampleHashMgr006');     -- MANAGER
GO

-- Khách hàng
INSERT INTO Customer (full_name, phone, email, birthday, points, member_tier) VALUES
    (N'Nguyễn Thị Hoa',    '0901234567', 'hoa@gmail.com',     '1995-05-20', 350,  'SILVER'),
    (N'Trần Văn Bình',      '0902345678', 'binh@gmail.com',    '1990-08-15', 1200, 'GOLD'),
    (N'Lê Thị Cúc',         '0903456789', 'cuc@gmail.com',     '2000-02-10', 50,   'BRONZE'),
    (N'Phạm Minh Đức',      '0904567890', NULL,                NULL,         0,    'BRONZE');
GO

-- Khuyến mãi
INSERT INTO Promotion (name, code, discount_type, value, min_order_value, max_discount_value, usage_limit, start_date, end_date) VALUES
    (N'Mừng khai trương giảm 20%',     'KHAITUONG20',  'PERCENTAGE',   20,     50000,  50000,  500,    '2025-01-01', '2026-12-31'),
    (N'Giảm 10k cho đơn từ 100k',      'GIAM10K',      'FIXED',        10000,  100000, NULL,   1000,   '2025-01-01', '2026-12-31'),
    (N'Member Gold giảm 15%',           'GOLD15',       'PERCENTAGE',   15,     0,      100000, NULL,   '2025-01-01', '2026-12-31');
GO

-- Danh mục món
INSERT INTO Category (name) VALUES
    (N'Cà phê'),
    (N'Trà'),
    (N'Nước ép'),
    (N'Bánh & Snack'),
    (N'Trà sữa');
GO

-- Nguyên liệu
INSERT INTO Ingredient (name, unit) VALUES
    (N'Cà phê hạt robusta',     N'kg'),
    (N'Sữa đặc',                N'lon'),
    (N'Đường trắng',            N'kg'),
    (N'Trà túi lọc Lipton',     N'gói'),
    (N'Đào hộp',                N'hộp'),
    (N'Cam tươi',               N'kg'),
    (N'Dưa hấu',                N'kg'),
    (N'Bột trà sữa',            N'kg'),
    (N'Sữa tươi',               N'lít'),
    (N'Đá viên',                N'kg');
GO

-- Tồn kho tại chi nhánh 1
INSERT INTO Branch_Ingredient (branch_id, ingredient_id, current_stock, cost_per_unit, min_stock_threshold) VALUES
    (1, 1,  1.8,    200000, 2.0),   -- Cà phê hạt: SẮP HẾT (< ngưỡng 2kg)
    (1, 2,  38,     15000,  10),    -- Sữa đặc: ĐỦ
    (1, 3,  5.2,    20000,  3.0),   -- Đường: CÒN ÍT
    (1, 4,  142,    3000,   30),    -- Trà túi lọc: ĐỦ
    (1, 5,  3,      45000,  5),     -- Đào hộp: SẮP HẾT (< ngưỡng 5)
    (1, 6,  11.5,   30000,  3.0),   -- Cam tươi: ĐỦ
    (1, 7,  8.0,    25000,  3.0),   -- Dưa hấu: ĐỦ
    (1, 8,  4.0,    150000, 2.0),   -- Bột trà sữa: ĐỦ
    (1, 9,  15.0,   18000,  5.0),   -- Sữa tươi: ĐỦ
    (1, 10, 20.0,   5000,   5.0);   -- Đá viên: ĐỦ
GO

-- Tồn kho tại chi nhánh 2
INSERT INTO Branch_Ingredient (branch_id, ingredient_id, current_stock, cost_per_unit, min_stock_threshold) VALUES
    (2, 1, 5.0,  200000, 2.0),
    (2, 2, 24,   15000,  10),
    (2, 3, 8.0,  20000,  3.0),
    (2, 9, 10.0, 18000,  5.0),
    (2, 10, 15.0, 5000,  5.0);
GO

-- Menu_Item
INSERT INTO Menu_Item (category_id, name, price, is_available, description) VALUES
    (1, N'Cà phê đen',      25000,  1, N'Cà phê phin truyền thống'),
    (1, N'Cà phê sữa',      30000,  1, N'Cà phê phin + sữa đặc'),
    (1, N'Bạc xỉu',         32000,  0, N'Nhiều sữa ít cà phê'),
    (1, N'Cà phê đá xay',   45000,  1, N'Blended coffee'),
    (2, N'Trà đào',         35000,  1, N'Trà túi lọc + đào hộp'),
    (2, N'Hồng trà sữa',    38000,  1, N'Hồng trà + sữa tươi'),
    (3, N'Cam ép',          40000,  1, N'Cam tươi nguyên chất'),
    (3, N'Dưa hấu ép',      35000,  1, N'Dưa hấu tươi'),
    (5, N'Trà sữa trân châu',45000, 1, N'Bột trà sữa + trân châu'),
    (4, N'Bánh croissant',  25000,  1, N'Bánh sừng bò bơ Pháp');
GO

-- Công thức nguyên liệu
INSERT INTO Recipe (menu_item_id, ingredient_id, quantity_required) VALUES
    -- Cà phê đen: 0.05kg cà phê hạt + 0.1kg đá
    (1, 1, 0.05), (1, 10, 0.1),
    -- Cà phê sữa: 0.05kg cà phê + 0.5 lon sữa + 0.1kg đá
    (2, 1, 0.05), (2, 2, 0.5), (2, 10, 0.1),
    -- Bạc xỉu: 0.03kg cà phê + 0.8 lon sữa + 0.1kg đá
    (3, 1, 0.03), (3, 2, 0.8), (3, 10, 0.1),
    -- Trà đào: 2 gói trà + 0.25 hộp đào + 0.15kg đá
    (5, 4, 2), (5, 5, 0.25), (5, 10, 0.15),
    -- Cam ép: 0.3kg cam + 0.05kg đường + 0.1kg đá
    (7, 6, 0.3), (7, 3, 0.05), (7, 10, 0.1),
    -- Dưa hấu ép: 0.4kg dưa + 0.1kg đá
    (8, 7, 0.4), (8, 10, 0.1),
    -- Trà sữa: 0.05kg bột trà sữa + 0.2 lít sữa tươi + 0.15kg đá
    (9, 8, 0.05), (9, 9, 0.2), (9, 10, 0.15);
GO

-- Lịch sử giá (giả lập giá cũ trước khi tăng)
INSERT INTO Price_History (menu_item_id, old_price, new_price, changed_by, start_date, end_date) VALUES
    (1, 20000, 25000, 1, '2025-01-01', '2025-06-30'),
    (2, 25000, 30000, 1, '2025-01-01', '2025-06-30'),
    (7, 35000, 40000, 1, '2025-07-01', NULL);
GO

-- Bàn tại chi nhánh 1
INSERT INTO Table_Cafe (branch_id, name, capacity, status) VALUES
    (1, N'Bàn 01', 4, 'EMPTY'),
    (1, N'Bàn 02', 4, 'OCCUPIED'),
    (1, N'Bàn 03', 2, 'EMPTY'),
    (1, N'Bàn 04', 4, 'OCCUPIED'),
    (1, N'Bàn 05', 6, 'CLEANING'),
    (1, N'Bàn 06', 4, 'EMPTY'),
    (1, N'Bàn 07', 4, 'OCCUPIED'),
    (1, N'Bàn 08', 6, 'EMPTY'),
    (1, N'Bàn 09', 4, 'EMPTY'),
    (2, N'Bàn 01', 4, 'EMPTY'),
    (2, N'Bàn 02', 4, 'EMPTY'),
    (2, N'Bàn 03', 6, 'EMPTY');
GO

-- Mở ca làm việc
INSERT INTO Work_Shift (branch_id, employee_id, account_id, starting_cash, status) VALUES
    (1, 2, 2, 500000, 'OPEN');  -- Thu ngân Trần Thị Mai (account id=2) mở ca với 500k
GO

-- ----------------------------------------------------------------
-- TẠO ORDER MẪU
-- ----------------------------------------------------------------

-- Order 1: Khách vãng lai, tại bàn 01, DINE_IN
INSERT INTO [Order] (branch_id, shift_id, customer_id, order_status_id, table_id, order_type, sub_total, discount_amount, final_amount, cost_amount)
VALUES (1, 1, NULL, 4, 1, 'DINE_IN', 55000, 0, 55000, 5000);

-- Order 2: Khách thành viên (Nguyễn Thị Hoa), có khuyến mãi, TAKE_AWAY
INSERT INTO [Order] (branch_id, shift_id, customer_id, order_status_id, table_id, order_type, sub_total, discount_amount, final_amount, cost_amount)
VALUES (1, 1, 1, 4, NULL, 'TAKE_AWAY', 70000, 14000, 56000, 9000);

-- Order 3: Đang pha chế, bàn 07
INSERT INTO [Order] (branch_id, shift_id, customer_id, order_status_id, table_id, order_type, sub_total, discount_amount, final_amount)
VALUES (1, 1, 2, 2, 7, 'DINE_IN', 125000, 0, 125000);
GO

-- Chi tiết Order 1: 1 Cà phê đen + 1 Cam ép
INSERT INTO Order_Item (order_id, menu_item_id, quantity, unit_price, note) VALUES
    (1, 1, 1, 25000, NULL),
    (1, 7, 1, 40000, N'Ít đá');

-- Chi tiết Order 2: 1 Trà đào + 1 Hồng trà sữa (ít đường)
INSERT INTO Order_Item (order_id, menu_item_id, quantity, unit_price, note) VALUES
    (2, 5, 1, 35000, N'Ít đá, ít đường'),
    (2, 6, 1, 38000, NULL);

-- Liên kết Option cố định cho item 1 của Order 1
INSERT INTO Order_Item_Option (order_item_id, option_id) VALUES (2, 1); -- Ít đá

-- Chi tiết Order 3: 2 Cà phê sữa + 1 Trà sữa trân châu
INSERT INTO Order_Item (order_id, menu_item_id, quantity, unit_price, note) VALUES
    (3, 2, 2, 30000, NULL),
    (3, 9, 1, 45000, N'Không đường');
INSERT INTO Order_Item_Option (order_item_id, option_id) VALUES (6, 3); -- Ít đường
GO

-- Khuyến mãi áp dụng cho Order 2 (giảm 20%)
INSERT INTO Order_Promotion (order_id, promotion_id, discount_value) VALUES (2, 1, 14000);
UPDATE Promotion SET used_count = used_count + 1 WHERE id = 1;
GO

-- Lịch sử trạng thái Order 3 (PENDING → COOKING)
INSERT INTO Order_Status_History (order_id, status_id, changed_by) VALUES
    (3, 1, 2),  -- PENDING lúc tạo
    (3, 2, 2);  -- COOKING khi bếp nhận
GO

-- Thanh toán
-- Order 1: Tiền mặt
INSERT INTO Payment (order_id, amount, method, status) VALUES (1, 55000, 'CASH', 'SUCCESS');
-- Order 2: Thanh toán chia đôi (20k tiền mặt + 36k chuyển khoản)
INSERT INTO Payment (order_id, amount, method, status) VALUES
    (2, 20000, 'CASH', 'SUCCESS'),
    (2, 36000, 'BANK', 'SUCCESS');
GO

-- Nhập kho mẫu
INSERT INTO Import_Receipt (branch_id, supplier_id, employee_id, total_amount, note) VALUES
    (1, 1, 1, 500000, N'Nhập cà phê hạt đầu tháng');

INSERT INTO Import_Receipt_Item (import_receipt_id, ingredient_id, quantity, unit_price) VALUES
    (1, 1, 2.5, 200000);

INSERT INTO Inventory_Transaction (branch_id, ingredient_id, employee_id, import_receipt_id, type, quantity, note) VALUES
    (1, 1, 1, 1, 'IMPORT', 2.5, N'Nhập kho theo phiếu #1');

UPDATE Branch_Ingredient
SET current_stock = current_stock + 2.5
WHERE branch_id = 1 AND ingredient_id = 1;
GO

-- Login Log mẫu
INSERT INTO Login_Log (account_id, ip_address, user_agent, is_success) VALUES
    (1, '192.168.1.10', 'Mozilla/5.0 Chrome/120', 1),
    (2, '192.168.1.15', 'Mozilla/5.0 Chrome/120', 1);

-- Audit Log mẫu
INSERT INTO Audit_Log (account_id, action, entity, entity_id, old_value, new_value) VALUES
    (1, 'UPDATE', 'Menu_Item', 1, N'{"price":20000}', N'{"price":25000}'),
    (1, 'CREATE', 'Import_Receipt', 1, NULL, N'{"total_amount":500000}');
GO

-- Đóng ca
UPDATE Work_Shift
SET end_time           = SYSDATETIME(),
    expected_cash      = 500000 + 55000 + 20000,  -- starting + tiền mặt thu được
    actual_ending_cash = 574000,
    difference         = 574000 - (500000 + 55000 + 20000),  -- = -1000 (thiếu 1k)
    status             = 'CLOSED',
    note               = N'Thiếu 1.000đ, kiểm tra lại'
WHERE id = 1;
GO

-- ================================================================
-- PHẦN 11: VIEWS HỖ TRỢ BACKEND / BÁO CÁO
-- ================================================================

-- View: Tồn kho với trạng thái cảnh báo
CREATE OR ALTER VIEW vw_Stock_Status AS
SELECT
    b.name          AS branch_name,
    i.name          AS ingredient_name,
    i.unit,
    bi.current_stock,
    bi.min_stock_threshold,
    bi.cost_per_unit,
    CASE
        WHEN bi.current_stock <= 0                              THEN N'Hết hàng'
        WHEN bi.current_stock < bi.min_stock_threshold          THEN N'Sắp hết'
        WHEN bi.current_stock < bi.min_stock_threshold * 1.5   THEN N'Còn ít'
        ELSE                                                         N'Đủ hàng'
    END AS stock_status
FROM Branch_Ingredient bi
JOIN Branch      b ON b.id = bi.branch_id
JOIN Ingredient  i ON i.id = bi.ingredient_id
WHERE b.is_deleted = 0 AND i.is_deleted = 0;
GO

-- View: Chi phí & lợi nhuận món theo chi nhánh
CREATE OR ALTER VIEW vw_MenuItem_Profit AS
SELECT
    b.id            AS branch_id,
    b.name          AS branch_name,
    m.id            AS menu_item_id,
    m.name          AS item_name,
    m.price,
    m.is_available,
    ISNULL(SUM(r.quantity_required * bi.cost_per_unit), 0)              AS cost_price,
    m.price - ISNULL(SUM(r.quantity_required * bi.cost_per_unit), 0)   AS profit,
    CASE
        WHEN m.price = 0 THEN 0
        ELSE ROUND(
            (m.price - ISNULL(SUM(r.quantity_required * bi.cost_per_unit), 0))
            / m.price * 100, 2)
    END AS profit_margin_pct
FROM Menu_Item m
JOIN Category           c  ON c.id  = m.category_id
LEFT JOIN Recipe        r  ON r.menu_item_id  = m.id
LEFT JOIN Branch_Ingredient bi ON bi.ingredient_id = r.ingredient_id
LEFT JOIN Branch        b  ON b.id  = bi.branch_id
WHERE m.is_deleted = 0
GROUP BY b.id, b.name, m.id, m.name, m.price, m.is_available;
GO

-- View: Doanh thu theo giờ (hôm nay)
CREATE OR ALTER VIEW vw_Revenue_By_Hour AS
SELECT
    branch_id,
    DATEPART(HOUR, created_at)      AS hour_of_day,
    COUNT(*)                        AS total_orders,
    SUM(final_amount)               AS total_revenue,
    SUM(cost_amount)                AS total_cost,
    SUM(final_amount - cost_amount) AS total_profit
FROM [Order]
WHERE is_deleted = 0
  AND order_status_id = 4          -- COMPLETED
  AND CAST(created_at AS DATE) = CAST(SYSDATETIME() AS DATE)
GROUP BY branch_id, DATEPART(HOUR, created_at);
GO

-- View: Dashboard tổng quan hôm nay
CREATE OR ALTER VIEW vw_Dashboard_Today AS
SELECT
    o.branch_id,
    COUNT(*)                                                AS total_orders,
    SUM(o.final_amount)                                     AS total_revenue,
    SUM(o.cost_amount)                                      AS total_cost,
    SUM(o.final_amount - o.cost_amount)                     AS total_profit,
    SUM(CASE WHEN os.name = 'CANCELLED' THEN 1 ELSE 0 END) AS cancelled_orders,
    SUM(CASE WHEN os.name = 'CANCELLED' THEN o.final_amount ELSE 0 END) AS cancelled_revenue_loss,
    AVG(o.final_amount)                                     AS avg_order_value
FROM [Order] o
JOIN Order_Status os ON os.id = o.order_status_id
WHERE o.is_deleted = 0
  AND CAST(o.created_at AS DATE) = CAST(SYSDATETIME() AS DATE)
GROUP BY o.branch_id;
GO

-- View: Hiệu suất nhân viên hôm nay
CREATE OR ALTER VIEW vw_Staff_Performance_Today AS
SELECT
    ws.branch_id,
    e.id        AS employee_id,
    e.full_name,
    e.position,
    ws.start_time,
    ws.end_time,
    ws.status   AS shift_status,
    COUNT(o.id)             AS total_orders,
    SUM(o.final_amount)     AS total_revenue
FROM Work_Shift ws
JOIN Employee   e  ON e.id  = ws.employee_id
LEFT JOIN [Order] o ON o.shift_id = ws.id AND o.is_deleted = 0
WHERE CAST(ws.start_time AS DATE) = CAST(SYSDATETIME() AS DATE)
GROUP BY ws.branch_id, e.id, e.full_name, e.position,
         ws.start_time, ws.end_time, ws.status;
GO

-- View: Top món bán chạy (30 ngày gần nhất)
CREATE OR ALTER VIEW vw_Top_Items_30Days AS
SELECT
    o.branch_id,
    m.id            AS menu_item_id,
    m.name          AS item_name,
    c.name          AS category_name,
    SUM(oi.quantity)            AS total_quantity,
    SUM(oi.total_price)         AS total_revenue,
    RANK() OVER (
        PARTITION BY o.branch_id
        ORDER BY SUM(oi.quantity) DESC
    ) AS rank_by_qty
FROM Order_Item oi
JOIN [Order]    o  ON o.id = oi.order_id
JOIN Menu_Item  m  ON m.id = oi.menu_item_id
JOIN Category   c  ON c.id = m.category_id
WHERE o.is_deleted = 0
  AND o.order_status_id = 4   -- COMPLETED
  AND o.created_at >= DATEADD(DAY, -30, SYSDATETIME())
GROUP BY o.branch_id, m.id, m.name, c.name;
GO

-- ================================================================
-- PHẦN 12: STORED PROCEDURES HỖ TRỢ
-- ================================================================

-- SP: Tạo order mới và trả về ID
CREATE OR ALTER PROCEDURE sp_CreateOrder
    @branch_id      INT,
    @shift_id       BIGINT,
    @customer_id    INT,
    @table_id       INT,
    @order_type     VARCHAR(20),
    @order_id       BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [Order] (branch_id, shift_id, customer_id, order_status_id, table_id, order_type, sub_total, discount_amount, final_amount)
    VALUES (@branch_id, @shift_id, @customer_id, 1, @table_id, @order_type, 0, 0, 0);
    SET @order_id = SCOPE_IDENTITY();

    -- Ghi lịch sử trạng thái PENDING
    INSERT INTO Order_Status_History (order_id, status_id) VALUES (@order_id, 1);

    -- Cập nhật trạng thái bàn nếu DINE_IN
    IF @order_type = 'DINE_IN' AND @table_id IS NOT NULL
        UPDATE Table_Cafe SET status = 'OCCUPIED' WHERE id = @table_id;
END;
GO

-- SP: Cập nhật tổng tiền order (gọi sau khi thêm/xóa item)
CREATE OR ALTER PROCEDURE sp_RecalculateOrder
    @order_id BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @sub_total DECIMAL(15,2);

    SELECT @sub_total = SUM(total_price) FROM Order_Item WHERE order_id = @order_id;
    SET @sub_total = ISNULL(@sub_total, 0);

    DECLARE @discount DECIMAL(15,2);
    SELECT @discount = ISNULL(SUM(discount_value), 0)
    FROM Order_Promotion WHERE order_id = @order_id;

    UPDATE [Order]
    SET sub_total      = @sub_total,
        discount_amount = @discount,
        final_amount   = @sub_total - @discount,
        updated_at     = SYSDATETIME()
    WHERE id = @order_id;
END;
GO

-- SP: Hoàn thành order + trừ kho nguyên liệu
CREATE OR ALTER PROCEDURE sp_CompleteOrder
    @order_id   BIGINT,
    @account_id INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @branch_id INT;
        SELECT @branch_id = branch_id FROM [Order] WHERE id = @order_id;

        -- Tính cost & trừ kho theo công thức
        DECLARE @cost DECIMAL(15,2) = 0;

        SELECT @cost = ISNULL(SUM(oi.quantity * r.quantity_required * bi.cost_per_unit), 0)
        FROM Order_Item  oi
        JOIN Recipe      r  ON r.menu_item_id  = oi.menu_item_id
        JOIN Branch_Ingredient bi ON bi.ingredient_id = r.ingredient_id
                                 AND bi.branch_id = @branch_id
        WHERE oi.order_id = @order_id;

        -- Trừ kho
        UPDATE bi
        SET bi.current_stock = bi.current_stock
            - (SELECT SUM(oi.quantity * r.quantity_required)
               FROM Order_Item oi
               JOIN Recipe r ON r.menu_item_id = oi.menu_item_id
                             AND r.ingredient_id = bi.ingredient_id
               WHERE oi.order_id = @order_id)
        FROM Branch_Ingredient bi
        WHERE bi.branch_id = @branch_id;

        -- Ghi Inventory_Transaction xuất kho
        INSERT INTO Inventory_Transaction (branch_id, ingredient_id, employee_id, type, quantity, note)
        SELECT @branch_id, r.ingredient_id, NULL, 'EXPORT',
               -SUM(oi.quantity * r.quantity_required),
               N'Xuất kho cho Order #' + CAST(@order_id AS NVARCHAR)
        FROM Order_Item oi
        JOIN Recipe r ON r.menu_item_id = oi.menu_item_id
        WHERE oi.order_id = @order_id
        GROUP BY r.ingredient_id;

        -- Cập nhật order
        UPDATE [Order]
        SET order_status_id = 4,
            cost_amount     = @cost,
            updated_at      = SYSDATETIME()
        WHERE id = @order_id;

        -- Cập nhật trạng thái bàn về EMPTY
        UPDATE Table_Cafe
        SET status = 'EMPTY', updated_at = SYSDATETIME()
        WHERE id = (SELECT table_id FROM [Order] WHERE id = @order_id)
          AND status = 'OCCUPIED';

        -- Lịch sử trạng thái
        INSERT INTO Order_Status_History (order_id, status_id, changed_by)
        VALUES (@order_id, 4, @account_id);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- SP: Cộng điểm khách hàng sau khi thanh toán
CREATE OR ALTER PROCEDURE sp_AddCustomerPoints
    @customer_id    INT,
    @order_amount   DECIMAL(15,2)
AS
BEGIN
    SET NOCOUNT ON;
    -- Quy tắc: 1 điểm cho mỗi 1.000đ
    DECLARE @points_to_add INT = FLOOR(@order_amount / 1000);

    UPDATE Customer
    SET points = points + @points_to_add,
        member_tier = CASE
            WHEN points + @points_to_add >= 1000 THEN 'GOLD'
            WHEN points + @points_to_add >= 300  THEN 'SILVER'
            ELSE 'BRONZE'
        END,
        updated_at = SYSDATETIME()
    WHERE id = @customer_id;
END;
GO

PRINT N'✅ Database pbl3_cafe_management đã được tạo thành công!';
PRINT N'   Bao gồm: 28 bảng | 6 Views | 4 Stored Procedures | Index đầy đủ';
PRINT N'   Dữ liệu mẫu: 3 chi nhánh | 6 nhân viên | 10 món | 4 khách hàng | 3 orders';
GO
