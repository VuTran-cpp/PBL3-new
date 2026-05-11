-- ============================================================
-- Seed rich data for CafeManagement demo
-- Run after init-database.sql
-- ============================================================
SET QUOTED_IDENTIFIER ON;
GO

USE pbl3_cafe_management;
GO

-- ── Update Menu Items with real image URLs ──────────────────
UPDATE Menu_Item SET image_url = 'https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=300&q=80' WHERE name = N'Cà phê đen';
UPDATE Menu_Item SET image_url = 'https://images.unsplash.com/photo-1461023058943-07fcbe16d735?w=300&q=80' WHERE name = N'Cà phê sữa';
UPDATE Menu_Item SET image_url = 'https://images.unsplash.com/photo-1572442388796-11668a67e53d?w=300&q=80' WHERE name = N'Bạc xỉu';
UPDATE Menu_Item SET image_url = 'https://images.unsplash.com/photo-1572442388796-11668a67e53d?w=300&q=80' WHERE name = N'Cappuccino';
UPDATE Menu_Item SET image_url = 'https://images.unsplash.com/photo-1570968915860-54d5c301fa9f?w=300&q=80' WHERE name = N'Latte';
UPDATE Menu_Item SET image_url = 'https://images.unsplash.com/photo-1556679343-c7306c1976bc?w=300&q=80' WHERE name LIKE N'Trà đào%';
UPDATE Menu_Item SET image_url = 'https://images.unsplash.com/photo-1544787219-7f47ccb76574?w=300&q=80' WHERE name = N'Trà vải';
UPDATE Menu_Item SET image_url = 'https://images.unsplash.com/photo-1638176066666-ffb2f013c7dd?w=300&q=80' WHERE name = N'Sinh tố bơ';
UPDATE Menu_Item SET image_url = 'https://images.unsplash.com/photo-1546173159-315724a31696?w=300&q=80' WHERE name = N'Sinh tố xoài';
UPDATE Menu_Item SET image_url = 'https://images.unsplash.com/photo-1621506289937-a8e4df240d0b?w=300&q=80' WHERE name = N'Nước ép cam';
UPDATE Menu_Item SET image_url = 'https://images.unsplash.com/photo-1571877227200-a0d98ea607e9?w=300&q=80' WHERE name = N'Bánh tiramisu';
UPDATE Menu_Item SET image_url = 'https://images.unsplash.com/photo-1555507036-ab1f4038024a?w=300&q=80' WHERE name = N'Croissant bơ';

-- ── More Menu Items ─────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Menu_Item WHERE name = N'Americano')
BEGIN
    INSERT INTO Menu_Item (category_id, name, price, is_available, description, image_url) VALUES
        (1, N'Americano',         35000, 1, N'Espresso pha loãng kiểu Mỹ',        'https://images.unsplash.com/photo-1551030173-122aabc4489c?w=300&q=80'),
        (1, N'Espresso',          30000, 1, N'Espresso đậm đặc nguyên chất',      'https://images.unsplash.com/photo-1510707577719-ae7c14805e3a?w=300&q=80'),
        (1, N'Mocha',             50000, 1, N'Cà phê chocolate sữa',              'https://images.unsplash.com/photo-1578314675249-a6910f80cc4e?w=300&q=80'),
        (1, N'Cold Brew',         45000, 1, N'Cà phê ủ lạnh 24h',                 'https://images.unsplash.com/photo-1517701550927-30cf4ba1dba5?w=300&q=80'),
        (2, N'Trà sữa trân châu', 42000, 1, N'Trà sữa truyền thống với trân châu','https://images.unsplash.com/photo-1558857563-b371033873b8?w=300&q=80'),
        (2, N'Trà chanh',         28000, 1, N'Trà chanh tươi mát',                'https://images.unsplash.com/photo-1556881286-fc6915169721?w=300&q=80'),
        (2, N'Matcha latte',      48000, 1, N'Trà xanh Nhật Bản sữa',             'https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=300&q=80'),
        (3, N'Sinh tố dâu',      42000, 1, N'Sinh tố dâu tây tươi',              'https://images.unsplash.com/photo-1553530666-ba11a7da3888?w=300&q=80'),
        (4, N'Nước ép dưa hấu',  30000, 1, N'Nước ép dưa hấu tươi mát',          'https://images.unsplash.com/photo-1527661591475-527312dd65f5?w=300&q=80'),
        (5, N'Bánh flan',         30000, 1, N'Bánh flan caramel mềm mịn',          'https://images.unsplash.com/photo-1624353365286-3f8d62daad51?w=300&q=80'),
        (5, N'Cheesecake',        60000, 1, N'Cheesecake New York béo ngậy',       'https://images.unsplash.com/photo-1524351199432-d330df15f4a7?w=300&q=80');
END

-- ── More Customers ──────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Customer WHERE phone = '0912345678')
BEGIN
    INSERT INTO Customer (full_name, phone, email, birthday, points, member_tier) VALUES
        (N'Nguyễn Văn An',    '0912345678', 'an.nguyen@gmail.com',   '1995-03-15', 1200, 'SILVER'),
        (N'Trần Thị Bình',    '0923456789', 'binh.tran@gmail.com',   '1990-07-22', 3500, 'GOLD'),
        (N'Lê Hoàng Cường',   '0934567890', 'cuong.le@gmail.com',    '1988-11-05', 800,  'SILVER'),
        (N'Phạm Minh Dương',  '0945678901', 'duong.pham@gmail.com',  '1998-01-20', 200,  'BRONZE'),
        (N'Hoàng Thị Em',     '0956789012', 'em.hoang@gmail.com',    '1992-09-30', 5500, 'PLATINUM'),
        (N'Võ Đức Phong',     '0967890123', 'phong.vo@gmail.com',    '1985-05-12', 150,  'BRONZE'),
        (N'Đặng Quỳnh Giao',  '0978901234', 'giao.dang@gmail.com',  '1993-12-18', 2800, 'GOLD'),
        (N'Bùi Thanh Hải',    '0989012345', 'hai.bui@gmail.com',     '1997-04-08', 450,  'BRONZE'),
        (N'Ngô Thị Iphương',  '0990123456', 'iphuong.ngo@gmail.com', '1991-06-25', 1800, 'SILVER'),
        (N'Trịnh Văn Khôi',   '0901234569', 'khoi.trinh@gmail.com',  '1994-08-14', 4200, 'GOLD');
END

-- ── More Employees + Accounts ───────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Employee WHERE phone = '0900000002')
BEGIN
    INSERT INTO Employee (branch_id, full_name, phone, email, position, salary, hired_date) VALUES
        (1, N'Nguyễn Thị Lan',  '0900000002', 'lan@coffee.vn',   'BARISTA',  8000000, '2024-03-15'),
        (1, N'Trần Văn Minh',   '0900000003', 'minh@coffee.vn',  'CASHIER',  7500000, '2024-04-01'),
        (1, N'Phạm Hoàng Nam',  '0900000004', 'nam@coffee.vn',   'BARISTA',  8000000, '2024-05-10'),
        (1, N'Lê Thị Oanh',    '0900000005', 'oanh@coffee.vn',  'WAITER',   6500000, '2024-06-20'),
        (1, N'Hoàng Đức Phúc',  '0900000006', 'phuc@coffee.vn',  'MANAGER', 12000000, '2024-02-01');

    -- Create accounts for employees
    DECLARE @staffRole INT = (SELECT id FROM Role WHERE name = 'STAFF');
    DECLARE @mgrRole INT = (SELECT id FROM Role WHERE name = 'MANAGER');

    INSERT INTO Account (employee_id, role_id, username, password_hash) VALUES
        ((SELECT id FROM Employee WHERE phone = '0900000002'), @staffRole, 'lan',
         '$2a$11$ywZ61h8liNiOU7t0B53duOGRQiPeh.aRB8ZmBG7cI63LD5XW.jvNO'),
        ((SELECT id FROM Employee WHERE phone = '0900000003'), @staffRole, 'minh',
         '$2a$11$ywZ61h8liNiOU7t0B53duOGRQiPeh.aRB8ZmBG7cI63LD5XW.jvNO'),
        ((SELECT id FROM Employee WHERE phone = '0900000006'), @mgrRole, 'phuc',
         '$2a$11$ywZ61h8liNiOU7t0B53duOGRQiPeh.aRB8ZmBG7cI63LD5XW.jvNO');
END

-- ── Ingredients stock for branch 1 ──────────────────────────
IF NOT EXISTS (SELECT 1 FROM Branch_Ingredient WHERE branch_id = 1)
BEGIN
    INSERT INTO Branch_Ingredient (branch_id, ingredient_id, current_stock, cost_per_unit, min_stock_threshold)
    SELECT 1, id, 
        CASE unit WHEN 'g' THEN 5000 WHEN 'ml' THEN 10000 ELSE 50 END,
        CASE unit WHEN 'g' THEN 200 WHEN 'ml' THEN 50 ELSE 5000 END,
        CASE unit WHEN 'g' THEN 500 WHEN 'ml' THEN 1000 ELSE 10 END
    FROM Ingredient;
END

-- ── Supplier ────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Supplier WHERE name = N'Trung Nguyên Coffee')
BEGIN
    INSERT INTO Supplier (name, phone, email, address) VALUES
        (N'Trung Nguyên Coffee',  '0281234567', 'contact@trungnguyen.vn',   N'Buôn Ma Thuột, Đắk Lắk'),
        (N'Vinamilk',             '02839541970', 'info@vinamilk.com.vn',    N'TP.HCM'),
        (N'TH True Milk',         '1800599991', 'cskh@thtruemilk.vn',      N'Nghĩa Đàn, Nghệ An');
END

-- ── Promotions ──────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Promotion WHERE code = 'WELCOME10')
BEGIN
    INSERT INTO Promotion (name, code, discount_type, value, min_order_value, max_discount_value, usage_limit, start_date, end_date) VALUES
        (N'Giảm 10% lần đầu',  'WELCOME10', 'PERCENT', 10, 50000, 30000, 100, '2024-01-01', '2026-12-31'),
        (N'Giảm 20K đơn 100K', 'SALE20K',   'FIXED',   20000, 100000, NULL, 200, '2024-01-01', '2026-12-31'),
        (N'Happy Hour 15%',     'HAPPY15',   'PERCENT', 15, 0, 50000, NULL, '2024-01-01', '2026-12-31');
END

-- ── Create a sample work shift (OPEN) ───────────────────────
IF NOT EXISTS (SELECT 1 FROM Work_Shift WHERE status = 'OPEN' AND branch_id = 1)
BEGIN
    DECLARE @empId1 INT = (SELECT id FROM Employee WHERE phone = '0900000001');
    INSERT INTO Work_Shift (branch_id, employee_id, starting_cash, start_time, status)
    VALUES (1, @empId1, 500000, GETUTCDATE(), 'OPEN');
END

-- ── Create sample COMPLETED orders (for order history) ──────
DECLARE @shiftId BIGINT = (SELECT TOP 1 id FROM Work_Shift WHERE branch_id = 1 ORDER BY id DESC);
DECLARE @completedStatusId INT = (SELECT id FROM Order_Status WHERE name = 'COMPLETED');
DECLARE @cookingStatusId INT = (SELECT id FROM Order_Status WHERE name = 'COOKING');
DECLARE @pendingStatusId INT = (SELECT id FROM Order_Status WHERE name = 'PENDING');

IF NOT EXISTS (SELECT 1 FROM [Order])
BEGIN
    -- Completed orders (historical)
    INSERT INTO [Order] (branch_id, shift_id, customer_id, order_status_id, table_id, order_type, sub_total, discount_amount, final_amount, cost_amount, note, created_at)
    VALUES
        (1, @shiftId, NULL, @completedStatusId, 1, 'DINE_IN',  75000,  0,     75000,  30000, N'Đơn demo 1',  DATEADD(HOUR, -5, GETUTCDATE())),
        (1, @shiftId, NULL, @completedStatusId, 3, 'DINE_IN',  125000, 10000, 115000, 50000, N'Có khuyến mãi', DATEADD(HOUR, -4, GETUTCDATE())),
        (1, @shiftId, NULL, @completedStatusId, NULL, 'TAKE_AWAY', 55000,  0,    55000,  22000, N'Mang về',     DATEADD(HOUR, -3, GETUTCDATE())),
        (1, @shiftId, 1,   @completedStatusId, 2, 'DINE_IN',  180000, 0,     180000, 72000, N'Khách VIP',    DATEADD(HOUR, -2, GETUTCDATE())),
        (1, @shiftId, NULL, @completedStatusId, 5, 'DINE_IN',  95000,  0,     95000,  38000, NULL,            DATEADD(HOUR, -1, GETUTCDATE())),
        (1, @shiftId, 2,   @completedStatusId, 4, 'DINE_IN',  210000, 20000, 190000, 84000, N'Bàn tiệc nhỏ', DATEADD(MINUTE, -90, GETUTCDATE())),
        (1, @shiftId, NULL, @completedStatusId, NULL, 'TAKE_AWAY', 45000,  0,    45000,  18000, NULL,           DATEADD(MINUTE, -60, GETUTCDATE())),
        (1, @shiftId, 3,   @completedStatusId, 6, 'DINE_IN',  160000, 0,     160000, 64000, NULL,            DATEADD(MINUTE, -45, GETUTCDATE()));

    -- Active orders (COOKING/PENDING)
    INSERT INTO [Order] (branch_id, shift_id, customer_id, order_status_id, table_id, order_type, sub_total, discount_amount, final_amount, cost_amount, note, created_at)
    VALUES
        (1, @shiftId, NULL, @cookingStatusId, 1, 'DINE_IN', 85000, 0, 85000, 34000, N'Đang chế biến', DATEADD(MINUTE, -15, GETUTCDATE())),
        (1, @shiftId, NULL, @pendingStatusId, 7, 'DINE_IN', 65000, 0, 65000, 26000, N'Chờ xử lý',     DATEADD(MINUTE, -5, GETUTCDATE()));

    -- Order items for completed orders
    INSERT INTO Order_Item (order_id, menu_item_id, quantity, unit_price, note)
    SELECT o.id, mi.id, 
        CASE WHEN o.id % 3 = 0 THEN 2 ELSE 1 END,
        mi.price,
        NULL
    FROM [Order] o
    CROSS APPLY (SELECT TOP 1 id, price FROM Menu_Item WHERE is_deleted = 0 ORDER BY NEWID()) mi
    WHERE NOT EXISTS (SELECT 1 FROM Order_Item WHERE order_id = o.id);

    -- Add more items to some orders
    INSERT INTO Order_Item (order_id, menu_item_id, quantity, unit_price)
    SELECT o.id, mi.id, 1, mi.price
    FROM [Order] o
    CROSS APPLY (SELECT TOP 1 id, price FROM Menu_Item WHERE is_deleted = 0 AND id > 3 ORDER BY NEWID()) mi
    WHERE o.sub_total > 100000
    AND NOT EXISTS (SELECT 1 FROM Order_Item oi JOIN Menu_Item m ON oi.menu_item_id = m.id WHERE oi.order_id = o.id AND m.id = mi.id);

    -- Update tables for active orders to OCCUPIED
    UPDATE Table_Cafe SET status = 'OCCUPIED' WHERE id IN (
        SELECT table_id FROM [Order] o
        JOIN Order_Status os ON o.order_status_id = os.id
        WHERE os.name IN ('COOKING','PENDING','SERVED') AND table_id IS NOT NULL
    );
END

PRINT N'✅ Seed data đã được thêm thành công!';
GO
