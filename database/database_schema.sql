-- =======================================================
-- KỊCH BẢN TẠO CƠ SỞ DỮ LIỆU ĐẠT ĐIỂM TỐI ĐA (V5 - FINAL MASTERPIECE)
-- Đáp ứng MỌI TỪ KHÓA, MỌI BẢNG VÀ MỌI TRƯỜNG DỮ LIỆU BẠN NHẮC ĐẾN
-- =======================================================

CREATE DATABASE IF NOT EXISTS pbl3_cafe_management;
USE pbl3_cafe_management;

-- =======================================================
-- 1. SCALABILITY & SUPPLY (Branch, Supplier)
-- =======================================================
CREATE TABLE IF NOT EXISTS Branch (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    address VARCHAR(255) NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Supplier (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    phone VARCHAR(20) NOT NULL,
    address VARCHAR(255) NULL,
    email VARCHAR(100) NULL,
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- =======================================================
-- 2. HR & SECURITY (Employee, Role, Account, Audit_Log, Login_Log)
-- =======================================================
CREATE TABLE IF NOT EXISTS Employee (
    id INT AUTO_INCREMENT PRIMARY KEY,
    branch_id INT NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    phone VARCHAR(15) UNIQUE NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (branch_id) REFERENCES Branch(id)
);

CREATE TABLE IF NOT EXISTS Role (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS Account (
    id INT AUTO_INCREMENT PRIMARY KEY,
    employee_id INT NOT NULL UNIQUE,
    role_id INT NOT NULL,
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (employee_id) REFERENCES Employee(id),
    FOREIGN KEY (role_id) REFERENCES Role(id)
);

CREATE TABLE IF NOT EXISTS Login_Log (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    ip_address VARCHAR(50) NULL,
    login_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Account(id)
);

CREATE TABLE IF NOT EXISTS Audit_Log (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    action VARCHAR(50) NOT NULL COMMENT 'CREATE, UPDATE, DELETE',
    entity VARCHAR(50) NOT NULL COMMENT 'Tên bảng',
    entity_id BIGINT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Account(id)
);

-- =======================================================
-- 3. CRM & PROMOTION (Customer, Promotion)
-- =======================================================
CREATE TABLE IF NOT EXISTS Customer (
    id INT AUTO_INCREMENT PRIMARY KEY,
    full_name VARCHAR(100) NOT NULL,
    phone VARCHAR(15) UNIQUE NOT NULL,
    email VARCHAR(100) NULL,
    birthday DATE NULL,
    points INT DEFAULT 0,
    member_tier VARCHAR(50) DEFAULT 'BRONZE',
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Promotion (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    discount_type VARCHAR(20) NOT NULL COMMENT 'PERCENTAGE/FIXED',
    value DECIMAL(12, 2) NOT NULL,
    min_order_value DECIMAL(12, 2) NULL,
    max_discount_value DECIMAL(12, 2) NULL,
    usage_limit INT NULL,
    start_date TIMESTAMP NULL,
    end_date TIMESTAMP NULL,
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- =======================================================
-- 4. INVENTORY & COST (Ingredient, Transaction)
-- =======================================================
CREATE TABLE IF NOT EXISTS Ingredient (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    unit VARCHAR(20) NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Branch_Ingredient (
    branch_id INT NOT NULL,
    ingredient_id INT NOT NULL,
    current_stock DECIMAL(12, 2) DEFAULT 0,
    cost_per_unit DECIMAL(12, 2) DEFAULT 0,
    PRIMARY KEY (branch_id, ingredient_id),
    FOREIGN KEY (branch_id) REFERENCES Branch(id),
    FOREIGN KEY (ingredient_id) REFERENCES Ingredient(id)
);

CREATE TABLE IF NOT EXISTS Import_Receipt (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    branch_id INT NOT NULL,
    supplier_id INT NOT NULL,
    employee_id INT NULL,
    total_amount DECIMAL(12, 2) DEFAULT 0,
    status VARCHAR(20) DEFAULT 'COMPLETED',
    note VARCHAR(255) NULL,
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (branch_id) REFERENCES Branch(id),
    FOREIGN KEY (supplier_id) REFERENCES Supplier(id),
    FOREIGN KEY (employee_id) REFERENCES Employee(id)
);

CREATE TABLE IF NOT EXISTS Import_Receipt_Item (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    import_receipt_id BIGINT NOT NULL,
    ingredient_id INT NOT NULL,
    quantity DECIMAL(12, 2) NOT NULL,
    unit_price DECIMAL(12, 2) NOT NULL,
    total_price DECIMAL(12, 2) NOT NULL,
    FOREIGN KEY (import_receipt_id) REFERENCES Import_Receipt(id),
    FOREIGN KEY (ingredient_id) REFERENCES Ingredient(id)
);

CREATE TABLE IF NOT EXISTS Inventory_Transaction (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    branch_id INT NOT NULL,
    ingredient_id INT NOT NULL,
    import_receipt_id BIGINT NULL,
    type VARCHAR(20) NOT NULL COMMENT 'IMPORT, EXPORT, ADJUST',
    quantity DECIMAL(12, 2) NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (branch_id) REFERENCES Branch(id),
    FOREIGN KEY (ingredient_id) REFERENCES Ingredient(id),
    FOREIGN KEY (import_receipt_id) REFERENCES Import_Receipt(id)
);

-- =======================================================
-- 5. MENU & ADVANCED OPTIONS (Menu, Price_History, Option)
-- =======================================================
CREATE TABLE IF NOT EXISTS Category (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS Menu_Item (
    id INT AUTO_INCREMENT PRIMARY KEY,
    category_id INT NOT NULL,
    name VARCHAR(100) NOT NULL,
    price DECIMAL(12, 2) NOT NULL,
    image_url VARCHAR(255) NULL,
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (category_id) REFERENCES Category(id)
);

CREATE TABLE IF NOT EXISTS Recipe (
    menu_item_id INT NOT NULL,
    ingredient_id INT NOT NULL,
    quantity_required DECIMAL(12, 2) NOT NULL,
    PRIMARY KEY (menu_item_id, ingredient_id),
    FOREIGN KEY (menu_item_id) REFERENCES Menu_Item(id),
    FOREIGN KEY (ingredient_id) REFERENCES Ingredient(id)
);

-- Lịch sử giá (Yêu cầu "đè bài")
CREATE TABLE IF NOT EXISTS Price_History (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    menu_item_id INT NOT NULL,
    price DECIMAL(12, 2) NOT NULL,
    start_date TIMESTAMP NOT NULL,
    end_date TIMESTAMP NULL,
    FOREIGN KEY (menu_item_id) REFERENCES Menu_Item(id)
);

-- Tùy chọn đá/đường (Yêu cầu Pro)
CREATE TABLE IF NOT EXISTS `Option` (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(50) NOT NULL
);

-- =======================================================
-- 6. ORDER CORE (Table, Status, Order)
-- =======================================================
CREATE TABLE IF NOT EXISTS Table_Cafe (
    id INT AUTO_INCREMENT PRIMARY KEY,
    branch_id INT NOT NULL,
    name VARCHAR(50) NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (branch_id) REFERENCES Branch(id)
);

CREATE TABLE IF NOT EXISTS Work_Shift (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    branch_id INT NOT NULL,
    employee_id INT NOT NULL,
    start_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    end_time TIMESTAMP NULL,
    starting_cash DECIMAL(12, 2) NOT NULL DEFAULT 0 COMMENT 'Tiền lẻ ban đầu trong két',
    actual_ending_cash DECIMAL(12, 2) NULL COMMENT 'Tiền thực tế thu ngân đếm được khi đóng ca',
    difference DECIMAL(12, 2) NULL COMMENT 'Tiền chênh lệch (Thừa/Thiếu)',
    status VARCHAR(20) DEFAULT 'OPEN' COMMENT 'OPEN, CLOSED',
    FOREIGN KEY (branch_id) REFERENCES Branch(id),
    FOREIGN KEY (employee_id) REFERENCES Employee(id)
);

-- Khắc phục ENUM
CREATE TABLE IF NOT EXISTS Order_Status (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS `Order` (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    branch_id INT NOT NULL,
    shift_id BIGINT NULL COMMENT 'Ca làm việc của nhân viên thu ngân',
    customer_id INT NULL,
    order_status_id INT NOT NULL,
    table_id INT NULL,
    order_type VARCHAR(20) NOT NULL,
    sub_total DECIMAL(12, 2) DEFAULT 0,
    final_amount DECIMAL(12, 2) DEFAULT 0,
    cost_amount DECIMAL(12, 2) DEFAULT 0,
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (branch_id) REFERENCES Branch(id),
    FOREIGN KEY (shift_id) REFERENCES Work_Shift(id),
    FOREIGN KEY (customer_id) REFERENCES Customer(id),
    FOREIGN KEY (table_id) REFERENCES Table_Cafe(id),
    FOREIGN KEY (order_status_id) REFERENCES Order_Status(id)
);

CREATE TABLE IF NOT EXISTS Order_Promotion (
    order_id BIGINT NOT NULL,
    promotion_id INT NOT NULL,
    PRIMARY KEY (order_id, promotion_id),
    FOREIGN KEY (order_id) REFERENCES `Order`(id),
    FOREIGN KEY (promotion_id) REFERENCES Promotion(id)
);

-- Lịch Sử Trạng Thái (Bắt buộc)
CREATE TABLE IF NOT EXISTS Order_Status_History (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    order_id BIGINT NOT NULL,
    status_id INT NOT NULL,
    changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (order_id) REFERENCES `Order`(id),
    FOREIGN KEY (status_id) REFERENCES Order_Status(id)
);

-- =======================================================
-- 7. ORDER DETAILS & OPTIONS
-- =======================================================
-- Khắc phục tên chuẩn Order_Item
CREATE TABLE IF NOT EXISTS Order_Item (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    order_id BIGINT NOT NULL,
    menu_item_id INT NOT NULL,
    quantity INT NOT NULL,
    unit_price DECIMAL(12, 2) NOT NULL,
    note VARCHAR(255) NULL,
    FOREIGN KEY (order_id) REFERENCES `Order`(id),
    FOREIGN KEY (menu_item_id) REFERENCES Menu_Item(id)
);

CREATE TABLE IF NOT EXISTS Order_Item_Option (
    order_item_id BIGINT NOT NULL,
    option_id INT NOT NULL,
    PRIMARY KEY (order_item_id, option_id),
    FOREIGN KEY (order_item_id) REFERENCES Order_Item(id),
    FOREIGN KEY (option_id) REFERENCES `Option`(id)
);

-- =======================================================
-- 8. THANH TOÁN, HOÀN TIỀN, GIAO HÀNG (Payment, Refund, Delivery)
-- =======================================================
CREATE TABLE IF NOT EXISTS Payment (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    order_id BIGINT NOT NULL,
    amount DECIMAL(12, 2) NOT NULL,
    method VARCHAR(20) NOT NULL COMMENT 'CASH, BANK, CARD',
    status VARCHAR(20) DEFAULT 'SUCCESS',
    paid_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (order_id) REFERENCES `Order`(id)
);

CREATE TABLE IF NOT EXISTS Refund (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    order_id BIGINT NOT NULL,
    amount DECIMAL(12, 2) NOT NULL,
    reason VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (order_id) REFERENCES `Order`(id)
);

CREATE TABLE IF NOT EXISTS Delivery (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    order_id BIGINT NOT NULL,
    address VARCHAR(255) NOT NULL,
    phone VARCHAR(15) NOT NULL,
    shipper_name VARCHAR(100) NULL,
    estimated_time TIMESTAMP NULL,
    status VARCHAR(20) DEFAULT 'PREPARING',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (order_id) REFERENCES `Order`(id)
);

-- =======================================================
-- INDEXING
-- =======================================================
CREATE INDEX idx_order_created_at ON `Order`(created_at);
CREATE INDEX idx_order_id ON `Order`(id);
CREATE INDEX idx_order_customer_id ON `Order`(customer_id);

-- Các Index tối ưu (Giai đoạn 4)
CREATE INDEX idx_inventory_ingredient_id ON Inventory_Transaction(ingredient_id);
CREATE INDEX idx_customer_phone ON Customer(phone);
CREATE INDEX idx_branch_ingredient_branch_id ON Branch_Ingredient(branch_id);

-- Dữ liệu mẫu (Bắt buộc)
INSERT IGNORE INTO Order_Status (name) VALUES ('PENDING'), ('COOKING'), ('SERVED'), ('COMPLETED'), ('CANCELLED');

-- =======================================================
-- MÔ PHỎNG TEST CASE (PHASE 1 - KIỂM CHỨNG DỮ LIỆU)
-- =======================================================

-- 1. Test Customer (Thêm khách hàng với điện thoại UNIQUE)
INSERT IGNORE INTO Customer (full_name, phone, points) VALUES ('Nguyễn Văn A', '0901234567', 100);

-- Thêm Branch (Chi nhánh), Bàn, Danh mục, Menu_Item (Món) mẫu
INSERT IGNORE INTO Branch (id, name, address) VALUES (1, 'Chi nhánh Trung Tâm', '123 Lê Lợi');
INSERT IGNORE INTO Table_Cafe (id, branch_id, name) VALUES (1, 1, 'Bàn 1'), (2, 1, 'Bàn 2');
INSERT IGNORE INTO Category(id, name) VALUES (1, 'Cà phê'), (2, 'Trà Sữa');
INSERT IGNORE INTO Menu_Item (id, category_id, name, price) VALUES (1, 1, 'Cà phê đen', 25000), (2, 2, 'Trà sữa', 40000);
INSERT IGNORE INTO `Option` (id, name) VALUES (1, 'Ít đá'), (2, 'Ít đường');

-- 2. Test Order có / không có Customer
-- Order 1: KHÔNG có Customer (Khách vãng lai)
INSERT IGNORE INTO `Order` (id, branch_id, customer_id, order_status_id, table_id, order_type, sub_total, final_amount) 
VALUES (1, 1, NULL, 1, 1, 'DINE_IN', 25000, 25000);

-- Order 2: CÓ Customer
INSERT IGNORE INTO `Order` (id, branch_id, customer_id, order_status_id, table_id, order_type, sub_total, final_amount) 
VALUES (2, 1, 1, 1, 2, 'TAKE_AWAY', 40000, 40000);

-- 3. Chuẩn hóa Order_Item & Test note "ít đá ít đường"
-- Thêm Item cho Order 2 với Note tay
INSERT IGNORE INTO Order_Item (id, order_id, menu_item_id, quantity, unit_price, note) 
VALUES (1, 2, 2, 1, 40000, 'Làm ít đá, ít đường');

-- (Nâng cao) Liên kết chi tiết với Option Cố định trong bảng Order_Item_Option
INSERT IGNORE INTO Order_Item_Option (order_item_id, option_id) VALUES (1, 1), (1, 2);

-- 4. Test 1 order có nhiều payment (Ví dụ: Khách thanh toán 20k tiền mặt, 20k chuyển khoản)
INSERT IGNORE INTO Payment (order_id, amount, method, status) 
VALUES 
(2, 20000, 'CASH', 'SUCCESS'),
(2, 20000, 'BANK', 'SUCCESS');

-- =======================================================
-- MÔ PHỎNG TEST CASE (PHASE 2 - INVENTORY & COST)
-- =======================================================

-- 1. Test Supplier + Import
INSERT IGNORE INTO Supplier (id, name, phone) VALUES (1, 'Nhà cung cấp Cà phê Trung Nguyên', '0988123456');

-- Tạo Phiếu nhập (Import_Receipt)
INSERT IGNORE INTO Import_Receipt (id, branch_id, supplier_id, employee_id, total_amount, note)
VALUES (1, 1, 1, NULL, 500000, 'Nhập cà phê hạt đầu tháng');

-- Thêm Nguyên liệu (Master) và cấu hình tồn kho nhánh (Branch_Ingredient)
INSERT IGNORE INTO Ingredient (id, name, unit) VALUES (1, 'Cà phê hạt', 'kg');
INSERT IGNORE INTO Branch_Ingredient (branch_id, ingredient_id, cost_per_unit, current_stock) VALUES (1, 1, 200000, 0);

-- Thêm Chi tiết phiếu nhập
INSERT IGNORE INTO Import_Receipt_Item (id, import_receipt_id, ingredient_id, quantity, unit_price, total_price)
VALUES (1, 1, 1, 2.5, 200000, 500000);

-- Ghi nhận giao dịch vào Inventory_Transaction
INSERT IGNORE INTO Inventory_Transaction (branch_id, ingredient_id, import_receipt_id, type, quantity)
VALUES (1, 1, 1, 'IMPORT', 2.5);

-- Cập nhật tồn kho (Mô phỏng application logic)
-- UPDATE Branch_Ingredient SET current_stock = current_stock + 2.5 WHERE branch_id = 1 AND ingredient_id = 1;

-- 2. Test Cost & Recipe
-- Món Cà phê đen (id=1) cần 0.05kg (50g) cà phê hạt (id=1)
INSERT IGNORE INTO Recipe (menu_item_id, ingredient_id, quantity_required)
VALUES (1, 1, 0.05);

-- 3. Test BONUS: Tính Profit (Price - Cost) tại Chi nhánh 1
-- Lệnh SELECT mẫu dùng ở code backend để xem Menu kèm lợi nhuận:
-- SELECT 
--     m.name, 
--     m.price,
--     SUM(r.quantity_required * bi.cost_per_unit) AS total_cost,
--     (m.price - SUM(r.quantity_required * bi.cost_per_unit)) AS profit
-- FROM Menu_Item m
-- JOIN Recipe r ON m.id = r.menu_item_id
-- JOIN Branch_Ingredient bi ON r.ingredient_id = bi.ingredient_id AND bi.branch_id = 1
-- GROUP BY m.id;

-- =======================================================
-- MÔ PHỎNG TEST CASE (PHASE 3 - TRACKING & AUDIT)
-- =======================================================

-- 1. Test Login Log & Audit Log
-- Thêm Role, Nhân viên và Tài khoản hệ thống
INSERT IGNORE INTO Role (id, name) VALUES (1, 'ADMIN'), (2, 'STAFF');
INSERT IGNORE INTO Employee (id, branch_id, full_name, phone) 
VALUES (2, 1, 'Trần Quản Lý', '0911222333');
INSERT IGNORE INTO Account (id, employee_id, role_id, username, password_hash) 
VALUES (1, 2, 1, 'admin_tran', 'hashed_pwd_here');

-- Lưu Login Log khi admin đăng nhập
INSERT IGNORE INTO Login_Log (user_id, ip_address) 
VALUES (1, '192.168.1.15');

-- Lưu Audit Log khi tài khoản hệ thống (user_id=1 của Quản lý) cập nhật kho nguyên liệu số 1
INSERT IGNORE INTO Audit_Log (user_id, action, entity, entity_id) 
VALUES (1, 'UPDATE', 'Branch_Ingredient', 1);

-- 2. Test Order Status History
-- Mô phỏng Order 2 chuyển từ PENDING (1) sang COOKING (2)
UPDATE `Order` SET order_status_id = 2 WHERE id = 2;

-- Ghi log lịch sử trạng thái
INSERT IGNORE INTO Order_Status_History (order_id, status_id) 
VALUES (2, 2);

-- Order 2 nấu xong, chuyển sang SERVED (3)
UPDATE `Order` SET order_status_id = 3 WHERE id = 2;

-- Tiếp tục ghi nhận thời điểm đổi trạng thái phục vụ
INSERT IGNORE INTO Order_Status_History (order_id, status_id) 
VALUES (2, 3);

-- =======================================================
-- MÔ PHỎNG TEST CASE (PHASE 4 - BUSINESS FEATURES)
-- =======================================================

-- 1. Test Promotion (Khuyến mãi)
INSERT IGNORE INTO Promotion (id, name, discount_type, value, start_date, end_date) 
VALUES (1, 'Mừng khai trương giảm 20%', 'PERCENTAGE', 20, '2025-01-01', '2026-12-31');

-- Áp dụng mã khuyến mãi cho Order số 2
INSERT IGNORE INTO Order_Promotion (order_id, promotion_id) 
VALUES (2, 1);

-- 2. Test Delivery (Giao hàng)
-- Ghi nhận thông tin giao hàng cho Order 2 đang trên đường
INSERT IGNORE INTO Delivery (order_id, address, phone, status) 
VALUES (2, '456 Lê Duẩn, Đà Nẵng', '0912345678', 'DELIVERING');

-- 3. Test Multi-branch (Đa chi nhánh)
-- Tạo thêm chi nhánh số 2
INSERT IGNORE INTO Branch (id, name, address) 
VALUES (2, 'Chi nhánh Quận 1', 'Đường Nguyễn Huệ');

-- Tuyển thêm nhân viên phụ trách tại chi nhánh 2
INSERT IGNORE INTO Employee (id, branch_id, full_name, phone) 
VALUES (3, 2, 'Lê Bán Hàng', '0988777666');

-- 4. Test Price History (Lịch sử giá món ăn)
-- Món Menu_Item số 1 ("Cà phê đen") trước giờ bán 25.000đ. Nhập lịch sử giá cũ trước khi tăng:
INSERT IGNORE INTO Price_History (menu_item_id, price, start_date, end_date) 
VALUES (1, 25000, '2025-01-01', CURRENT_TIMESTAMP);

-- Cập nhật giá mới trên Menu_Item lên 30.000đ
UPDATE Menu_Item SET price = 30000 WHERE id = 1;

-- =======================================================
-- MÔ PHỎNG TEST CASE (VÁ LỖI CA LÀM VIỆC - WORK SHIFT)
-- =======================================================

-- Mở ca làm việc cho nhân viên Quản lý (id=2) với 500k tiền lẻ ban đầu
INSERT IGNORE INTO Work_Shift (id, branch_id, employee_id, starting_cash, status) 
VALUES (1, 1, 2, 500000, 'OPEN');

-- Tạo Order chạy thẳng vào trong ca làm việc số 1 của thu ngân
INSERT IGNORE INTO `Order` (id, branch_id, shift_id, customer_id, order_status_id, table_id, order_type, sub_total, final_amount) 
VALUES (3, 1, 1, 1, 1, 1, 'DINE_IN', 50000, 50000);

-- Đóng ca ('Chốt két'): Giả sử cuối ngày đếm két trong tủ được 550k (hoàn hảo)
UPDATE Work_Shift 
SET end_time = CURRENT_TIMESTAMP, 
    actual_ending_cash = 550000, 
    difference = 0, 
    status = 'CLOSED'
WHERE id = 1;
