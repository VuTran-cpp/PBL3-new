-- ============================================================
-- PBL3 Cafe Management — Database Initialization Script
-- Server: localhost\MSSQLSERVER01
-- Database: pbl3_cafe_management
-- ============================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'pbl3_cafe_management')
    CREATE DATABASE pbl3_cafe_management;
GO

USE pbl3_cafe_management;
GO

-- ── TABLES ──────────────────────────────────────────────────

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Branch')
CREATE TABLE Branch (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    address NVARCHAR(255) NOT NULL,
    phone NVARCHAR(20),
    email NVARCHAR(100),
    is_deleted BIT DEFAULT 0,
    deleted_at DATETIME2,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE()
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Supplier')
CREATE TABLE Supplier (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    phone NVARCHAR(20) NOT NULL,
    address NVARCHAR(255),
    email NVARCHAR(100),
    is_deleted BIT DEFAULT 0,
    deleted_at DATETIME2,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE()
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Employee')
CREATE TABLE Employee (
    id INT IDENTITY(1,1) PRIMARY KEY,
    branch_id INT NOT NULL,
    full_name NVARCHAR(100) NOT NULL,
    phone NVARCHAR(15) NOT NULL,
    email NVARCHAR(100),
    position NVARCHAR(50),
    salary DECIMAL(15,2) DEFAULT 0,
    hired_date DATE,
    is_deleted BIT DEFAULT 0,
    deleted_at DATETIME2,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Employee_Branch FOREIGN KEY (branch_id) REFERENCES Branch(id),
    CONSTRAINT UQ_Employee_Phone UNIQUE (phone)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Role')
CREATE TABLE Role (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(50) NOT NULL,
    CONSTRAINT UQ_Role_Name UNIQUE (name)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Account')
CREATE TABLE Account (
    id INT IDENTITY(1,1) PRIMARY KEY,
    employee_id INT NOT NULL,
    role_id INT NOT NULL,
    username NVARCHAR(50) NOT NULL,
    password_hash NVARCHAR(255) NOT NULL,
    is_deleted BIT DEFAULT 0,
    deleted_at DATETIME2,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Account_Employee FOREIGN KEY (employee_id) REFERENCES Employee(id),
    CONSTRAINT FK_Account_Role FOREIGN KEY (role_id) REFERENCES Role(id),
    CONSTRAINT UQ_Account_Username UNIQUE (username),
    CONSTRAINT UQ_Account_Employee UNIQUE (employee_id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Login_Log')
CREATE TABLE Login_Log (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    account_id INT NOT NULL,
    ip_address NVARCHAR(50),
    user_agent NVARCHAR(255),
    login_at DATETIME2 DEFAULT GETUTCDATE(),
    is_success BIT DEFAULT 1,
    CONSTRAINT FK_LoginLog_Account FOREIGN KEY (account_id) REFERENCES Account(id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Audit_Log')
CREATE TABLE Audit_Log (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    account_id INT NOT NULL,
    action NVARCHAR(20) NOT NULL,
    entity NVARCHAR(50) NOT NULL,
    entity_id BIGINT,
    old_value NVARCHAR(MAX),
    new_value NVARCHAR(MAX),
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_AuditLog_Account FOREIGN KEY (account_id) REFERENCES Account(id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Customer')
CREATE TABLE Customer (
    id INT IDENTITY(1,1) PRIMARY KEY,
    full_name NVARCHAR(100) NOT NULL,
    phone NVARCHAR(15) NOT NULL,
    email NVARCHAR(100),
    birthday DATE,
    points INT DEFAULT 0,
    member_tier NVARCHAR(20) DEFAULT 'BRONZE',
    is_deleted BIT DEFAULT 0,
    deleted_at DATETIME2,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Customer_Phone UNIQUE (phone)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Promotion')
CREATE TABLE Promotion (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    code NVARCHAR(50),
    discount_type NVARCHAR(20) NOT NULL,
    value DECIMAL(12,2) NOT NULL,
    min_order_value DECIMAL(12,2),
    max_discount_value DECIMAL(12,2),
    usage_limit INT,
    used_count INT DEFAULT 0,
    start_date DATETIME2,
    end_date DATETIME2,
    is_deleted BIT DEFAULT 0,
    deleted_at DATETIME2,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Promotion_Code UNIQUE (code)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Ingredient')
CREATE TABLE Ingredient (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    unit NVARCHAR(20) NOT NULL,
    is_deleted BIT DEFAULT 0,
    deleted_at DATETIME2,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE()
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Branch_Ingredient')
CREATE TABLE Branch_Ingredient (
    branch_id INT NOT NULL,
    ingredient_id INT NOT NULL,
    current_stock DECIMAL(12,2) DEFAULT 0,
    cost_per_unit DECIMAL(12,2) DEFAULT 0,
    min_stock_threshold DECIMAL(12,2) DEFAULT 0,
    CONSTRAINT PK_BranchIngredient PRIMARY KEY (branch_id, ingredient_id),
    CONSTRAINT FK_BI_Branch FOREIGN KEY (branch_id) REFERENCES Branch(id),
    CONSTRAINT FK_BI_Ingredient FOREIGN KEY (ingredient_id) REFERENCES Ingredient(id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Import_Receipt')
CREATE TABLE Import_Receipt (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    branch_id INT NOT NULL,
    supplier_id INT NOT NULL,
    employee_id INT,
    total_amount DECIMAL(15,2) DEFAULT 0,
    status NVARCHAR(20) DEFAULT 'COMPLETED',
    note NVARCHAR(500),
    is_deleted BIT DEFAULT 0,
    deleted_at DATETIME2,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_IR_Branch FOREIGN KEY (branch_id) REFERENCES Branch(id),
    CONSTRAINT FK_IR_Supplier FOREIGN KEY (supplier_id) REFERENCES Supplier(id),
    CONSTRAINT FK_IR_Employee FOREIGN KEY (employee_id) REFERENCES Employee(id) ON DELETE SET NULL
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Import_Receipt_Item')
CREATE TABLE Import_Receipt_Item (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    import_receipt_id BIGINT NOT NULL,
    ingredient_id INT NOT NULL,
    quantity DECIMAL(12,2) NOT NULL,
    unit_price DECIMAL(12,2) NOT NULL,
    total_price AS ([quantity]*[unit_price]),
    CONSTRAINT FK_IRI_Receipt FOREIGN KEY (import_receipt_id) REFERENCES Import_Receipt(id),
    CONSTRAINT FK_IRI_Ingredient FOREIGN KEY (ingredient_id) REFERENCES Ingredient(id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Inventory_Transaction')
CREATE TABLE Inventory_Transaction (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    branch_id INT NOT NULL,
    ingredient_id INT NOT NULL,
    employee_id INT,
    import_receipt_id BIGINT,
    type NVARCHAR(20) NOT NULL,
    quantity DECIMAL(12,2) NOT NULL,
    note NVARCHAR(255),
    is_deleted BIT DEFAULT 0,
    deleted_at DATETIME2,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_IT_Branch FOREIGN KEY (branch_id) REFERENCES Branch(id),
    CONSTRAINT FK_IT_Ingredient FOREIGN KEY (ingredient_id) REFERENCES Ingredient(id),
    CONSTRAINT FK_IT_Employee FOREIGN KEY (employee_id) REFERENCES Employee(id) ON DELETE SET NULL,
    CONSTRAINT FK_IT_Receipt FOREIGN KEY (import_receipt_id) REFERENCES Import_Receipt(id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Category')
CREATE TABLE Category (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    CONSTRAINT UQ_Category_Name UNIQUE (name)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Menu_Item')
CREATE TABLE Menu_Item (
    id INT IDENTITY(1,1) PRIMARY KEY,
    category_id INT NOT NULL,
    name NVARCHAR(100) NOT NULL,
    price DECIMAL(12,2) NOT NULL,
    image_url NVARCHAR(500),
    is_available BIT DEFAULT 1,
    description NVARCHAR(500),
    is_deleted BIT DEFAULT 0,
    deleted_at DATETIME2,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_MI_Category FOREIGN KEY (category_id) REFERENCES Category(id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Recipe')
CREATE TABLE Recipe (
    menu_item_id INT NOT NULL,
    ingredient_id INT NOT NULL,
    quantity_required DECIMAL(12,4) NOT NULL,
    CONSTRAINT PK_Recipe PRIMARY KEY (menu_item_id, ingredient_id),
    CONSTRAINT FK_Recipe_MI FOREIGN KEY (menu_item_id) REFERENCES Menu_Item(id),
    CONSTRAINT FK_Recipe_Ing FOREIGN KEY (ingredient_id) REFERENCES Ingredient(id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Price_History')
CREATE TABLE Price_History (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    menu_item_id INT NOT NULL,
    old_price DECIMAL(12,2) NOT NULL,
    new_price DECIMAL(12,2) NOT NULL,
    changed_by INT,
    start_date DATETIME2 DEFAULT GETUTCDATE(),
    end_date DATETIME2,
    CONSTRAINT FK_PH_MI FOREIGN KEY (menu_item_id) REFERENCES Menu_Item(id),
    CONSTRAINT FK_PH_Account FOREIGN KEY (changed_by) REFERENCES Account(id) ON DELETE SET NULL
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Option')
CREATE TABLE [Option] (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(50) NOT NULL,
    CONSTRAINT UQ_Option_Name UNIQUE (name)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Table_Cafe')
CREATE TABLE Table_Cafe (
    id INT IDENTITY(1,1) PRIMARY KEY,
    branch_id INT NOT NULL,
    name NVARCHAR(50) NOT NULL,
    capacity INT DEFAULT 4,
    status NVARCHAR(20) DEFAULT 'EMPTY',
    is_deleted BIT DEFAULT 0,
    deleted_at DATETIME2,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_TC_Branch FOREIGN KEY (branch_id) REFERENCES Branch(id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Work_Shift')
CREATE TABLE Work_Shift (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    branch_id INT NOT NULL,
    employee_id INT NOT NULL,
    account_id INT,
    start_time DATETIME2 DEFAULT GETUTCDATE(),
    end_time DATETIME2,
    starting_cash DECIMAL(15,2) DEFAULT 0,
    actual_ending_cash DECIMAL(15,2),
    expected_cash DECIMAL(15,2),
    difference DECIMAL(15,2),
    status NVARCHAR(20) DEFAULT 'OPEN',
    note NVARCHAR(500),
    CONSTRAINT FK_WS_Branch FOREIGN KEY (branch_id) REFERENCES Branch(id),
    CONSTRAINT FK_WS_Employee FOREIGN KEY (employee_id) REFERENCES Employee(id),
    CONSTRAINT FK_WS_Account FOREIGN KEY (account_id) REFERENCES Account(id) ON DELETE SET NULL
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Order_Status')
CREATE TABLE Order_Status (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(50) NOT NULL
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Order')
CREATE TABLE [Order] (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    branch_id INT NOT NULL,
    shift_id BIGINT,
    customer_id INT,
    order_status_id INT NOT NULL,
    table_id INT,
    order_type NVARCHAR(20) NOT NULL,
    sub_total DECIMAL(15,2) DEFAULT 0,
    discount_amount DECIMAL(15,2) DEFAULT 0,
    final_amount DECIMAL(15,2) DEFAULT 0,
    cost_amount DECIMAL(15,2) DEFAULT 0,
    note NVARCHAR(500),
    is_deleted BIT DEFAULT 0,
    deleted_at DATETIME2,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_O_Branch FOREIGN KEY (branch_id) REFERENCES Branch(id),
    CONSTRAINT FK_O_Shift FOREIGN KEY (shift_id) REFERENCES Work_Shift(id) ON DELETE SET NULL,
    CONSTRAINT FK_O_Customer FOREIGN KEY (customer_id) REFERENCES Customer(id) ON DELETE SET NULL,
    CONSTRAINT FK_O_Status FOREIGN KEY (order_status_id) REFERENCES Order_Status(id),
    CONSTRAINT FK_O_Table FOREIGN KEY (table_id) REFERENCES Table_Cafe(id) ON DELETE SET NULL
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Order_Status_History')
CREATE TABLE Order_Status_History (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    order_id BIGINT NOT NULL,
    status_id INT NOT NULL,
    changed_by INT,
    changed_at DATETIME2 DEFAULT GETUTCDATE(),
    note NVARCHAR(255),
    CONSTRAINT FK_OSH_Order FOREIGN KEY (order_id) REFERENCES [Order](id),
    CONSTRAINT FK_OSH_Status FOREIGN KEY (status_id) REFERENCES Order_Status(id),
    CONSTRAINT FK_OSH_Account FOREIGN KEY (changed_by) REFERENCES Account(id) ON DELETE SET NULL
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Order_Promotion')
CREATE TABLE Order_Promotion (
    order_id BIGINT NOT NULL,
    promotion_id INT NOT NULL,
    discount_value DECIMAL(12,2) DEFAULT 0,
    CONSTRAINT PK_OrderPromotion PRIMARY KEY (order_id, promotion_id),
    CONSTRAINT FK_OP_Order FOREIGN KEY (order_id) REFERENCES [Order](id),
    CONSTRAINT FK_OP_Promotion FOREIGN KEY (promotion_id) REFERENCES Promotion(id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Order_Item')
CREATE TABLE Order_Item (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    order_id BIGINT NOT NULL,
    menu_item_id INT NOT NULL,
    quantity INT DEFAULT 1,
    unit_price DECIMAL(12,2) NOT NULL,
    total_price AS ([quantity]*[unit_price]),
    note NVARCHAR(255),
    CONSTRAINT FK_OI_Order FOREIGN KEY (order_id) REFERENCES [Order](id),
    CONSTRAINT FK_OI_MI FOREIGN KEY (menu_item_id) REFERENCES Menu_Item(id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Order_Item_Option')
CREATE TABLE Order_Item_Option (
    order_item_id BIGINT NOT NULL,
    option_id INT NOT NULL,
    CONSTRAINT PK_OrderItemOption PRIMARY KEY (order_item_id, option_id),
    CONSTRAINT FK_OIO_OI FOREIGN KEY (order_item_id) REFERENCES Order_Item(id),
    CONSTRAINT FK_OIO_Option FOREIGN KEY (option_id) REFERENCES [Option](id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Payment')
CREATE TABLE Payment (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    order_id BIGINT NOT NULL,
    amount DECIMAL(15,2) NOT NULL,
    method NVARCHAR(20) NOT NULL,
    status NVARCHAR(20) DEFAULT 'SUCCESS',
    reference NVARCHAR(100),
    paid_at DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Payment_Order FOREIGN KEY (order_id) REFERENCES [Order](id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Refund')
CREATE TABLE Refund (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    order_id BIGINT NOT NULL,
    payment_id BIGINT,
    amount DECIMAL(15,2) NOT NULL,
    reason NVARCHAR(500) NOT NULL,
    processed_by INT,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Refund_Order FOREIGN KEY (order_id) REFERENCES [Order](id),
    CONSTRAINT FK_Refund_Payment FOREIGN KEY (payment_id) REFERENCES Payment(id),
    CONSTRAINT FK_Refund_Account FOREIGN KEY (processed_by) REFERENCES Account(id) ON DELETE SET NULL
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Delivery')
CREATE TABLE Delivery (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    order_id BIGINT NOT NULL,
    address NVARCHAR(500) NOT NULL,
    phone NVARCHAR(15) NOT NULL,
    shipper_name NVARCHAR(100),
    shipper_phone NVARCHAR(15),
    estimated_time DATETIME2,
    delivered_at DATETIME2,
    status NVARCHAR(20) DEFAULT 'PREPARING',
    delivery_fee DECIMAL(12,2) DEFAULT 0,
    note NVARCHAR(255),
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Delivery_Order FOREIGN KEY (order_id) REFERENCES [Order](id)
);
GO

-- ── SEED DATA ───────────────────────────────────────────────

-- Roles
IF NOT EXISTS (SELECT 1 FROM Role WHERE name = 'ADMIN')
    INSERT INTO Role (name) VALUES ('ADMIN');
IF NOT EXISTS (SELECT 1 FROM Role WHERE name = 'MANAGER')
    INSERT INTO Role (name) VALUES ('MANAGER');
IF NOT EXISTS (SELECT 1 FROM Role WHERE name = 'STAFF')
    INSERT INTO Role (name) VALUES ('STAFF');

-- Order Statuses
IF NOT EXISTS (SELECT 1 FROM Order_Status WHERE name = 'PENDING')
    INSERT INTO Order_Status (name) VALUES ('PENDING');
IF NOT EXISTS (SELECT 1 FROM Order_Status WHERE name = 'COOKING')
    INSERT INTO Order_Status (name) VALUES ('COOKING');
IF NOT EXISTS (SELECT 1 FROM Order_Status WHERE name = 'SERVED')
    INSERT INTO Order_Status (name) VALUES ('SERVED');
IF NOT EXISTS (SELECT 1 FROM Order_Status WHERE name = 'COMPLETED')
    INSERT INTO Order_Status (name) VALUES ('COMPLETED');
IF NOT EXISTS (SELECT 1 FROM Order_Status WHERE name = 'CANCELLED')
    INSERT INTO Order_Status (name) VALUES ('CANCELLED');

-- Branch
IF NOT EXISTS (SELECT 1 FROM Branch WHERE name = N'Chi nhánh chính')
    INSERT INTO Branch (name, address, phone, email)
    VALUES (N'Chi nhánh chính', N'123 Nguyễn Văn Linh, Đà Nẵng', '0901234567', 'abc@coffee.vn');

-- Employee (Admin)
IF NOT EXISTS (SELECT 1 FROM Employee WHERE phone = '0900000001')
    INSERT INTO Employee (branch_id, full_name, phone, email, position, salary, hired_date)
    VALUES (1, N'Quản trị viên', '0900000001', 'admin@coffee.vn', 'ADMIN', 15000000, '2024-01-01');

-- Account (admin/admin123) — BCrypt hash of 'admin123'
IF NOT EXISTS (SELECT 1 FROM Account WHERE username = 'admin')
BEGIN
    DECLARE @adminRoleId INT = (SELECT id FROM Role WHERE name = 'ADMIN');
    DECLARE @adminEmpId INT = (SELECT id FROM Employee WHERE phone = '0900000001');
    INSERT INTO Account (employee_id, role_id, username, password_hash)
    VALUES (@adminEmpId, @adminRoleId, 'admin',
        '$2a$11$KpU5L8MKnOY5GJXhkOYh0eqHC7bt.mnJPkLe4UZMlFKkagDG3PKPC');
END

-- Options
IF NOT EXISTS (SELECT 1 FROM [Option] WHERE name = N'Ít đường')
BEGIN
    INSERT INTO [Option] (name) VALUES (N'Ít đường'), (N'Nhiều đá'), (N'Ít đá'), (N'Nóng'), (N'Thêm shot');
END

-- Categories
IF NOT EXISTS (SELECT 1 FROM Category WHERE name = N'Cà phê')
BEGIN
    INSERT INTO Category (name) VALUES (N'Cà phê'), (N'Trà'), (N'Sinh tố'), (N'Nước ép'), (N'Bánh ngọt');
END

-- Menu Items
IF NOT EXISTS (SELECT 1 FROM Menu_Item WHERE name = N'Cà phê đen')
BEGIN
    INSERT INTO Menu_Item (category_id, name, price, is_available, description) VALUES
        (1, N'Cà phê đen',       25000, 1, N'Cà phê đen truyền thống'),
        (1, N'Cà phê sữa',      30000, 1, N'Cà phê sữa đá'),
        (1, N'Bạc xỉu',         35000, 1, N'Bạc xỉu đá mát lạnh'),
        (1, N'Cappuccino',       45000, 1, N'Cappuccino Ý'),
        (1, N'Latte',            45000, 1, N'Latte sữa béo'),
        (2, N'Trà đào cam sả',  40000, 1, N'Trà đào cam sả tươi mát'),
        (2, N'Trà vải',         35000, 1, N'Trà vải thanh mát'),
        (3, N'Sinh tố bơ',      45000, 1, N'Sinh tố bơ béo ngậy'),
        (3, N'Sinh tố xoài',    40000, 1, N'Sinh tố xoài tươi'),
        (4, N'Nước ép cam',     35000, 1, N'Nước ép cam tươi'),
        (5, N'Bánh tiramisu',   55000, 1, N'Tiramisu Ý truyền thống'),
        (5, N'Croissant bơ',    35000, 1, N'Croissant bơ Pháp');
END

-- Tables
IF NOT EXISTS (SELECT 1 FROM Table_Cafe WHERE branch_id = 1 AND name = N'Bàn 1')
BEGIN
    INSERT INTO Table_Cafe (branch_id, name, capacity, status) VALUES
        (1, N'Bàn 1', 4, 'EMPTY'), (1, N'Bàn 2', 4, 'EMPTY'),
        (1, N'Bàn 3', 2, 'EMPTY'), (1, N'Bàn 4', 6, 'EMPTY'),
        (1, N'Bàn 5', 4, 'EMPTY'), (1, N'Bàn 6', 2, 'EMPTY'),
        (1, N'Bàn 7', 8, 'EMPTY'), (1, N'Bàn 8', 4, 'EMPTY');
END

-- Ingredients
IF NOT EXISTS (SELECT 1 FROM Ingredient WHERE name = N'Cà phê rang xay')
BEGIN
    INSERT INTO Ingredient (name, unit) VALUES
        (N'Cà phê rang xay', 'g'), (N'Sữa đặc', 'ml'), (N'Sữa tươi', 'ml'),
        (N'Đường', 'g'), (N'Đá viên', 'viên'), (N'Trà xanh', 'g'),
        (N'Đào ngâm', 'g'), (N'Cam', 'trái'), (N'Sả', 'g'),
        (N'Bơ', 'trái'), (N'Xoài', 'trái');
END

PRINT N'✅ Database pbl3_cafe_management đã được khởi tạo thành công!';
PRINT N'👤 Tài khoản mặc định: admin / admin123';
GO
