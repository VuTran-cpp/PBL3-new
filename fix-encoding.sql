-- ============================================================
-- Fix encoding: Re-insert Vietnamese data with correct encoding
-- Run with: sqlcmd -S "localhost\MSSQLSERVER01" -E -f 65001 -i fix-encoding.sql
-- ============================================================
USE pbl3_cafe_management;
GO

-- Fix Branch
UPDATE Branch SET 
    name = N'Chi nhánh chính',
    address = N'123 Nguyễn Văn Linh, Đà Nẵng'
WHERE id = 1;

-- Fix Employee names
UPDATE Employee SET full_name = N'Quản trị viên'  WHERE phone = '0900000001';
UPDATE Employee SET full_name = N'Nguyễn Thị Lan'  WHERE phone = '0900000002';
UPDATE Employee SET full_name = N'Trần Văn Minh'   WHERE phone = '0900000003';
UPDATE Employee SET full_name = N'Phạm Hoàng Nam'  WHERE phone = '0900000004';
UPDATE Employee SET full_name = N'Lê Thị Oanh'    WHERE phone = '0900000005';
UPDATE Employee SET full_name = N'Hoàng Đức Phúc'  WHERE phone = '0900000006';

-- Fix Table names
UPDATE Table_Cafe SET name = N'Bàn 1' WHERE id = 1;
UPDATE Table_Cafe SET name = N'Bàn 2' WHERE id = 2;
UPDATE Table_Cafe SET name = N'Bàn 3' WHERE id = 3;
UPDATE Table_Cafe SET name = N'Bàn 4' WHERE id = 4;
UPDATE Table_Cafe SET name = N'Bàn 5' WHERE id = 5;
UPDATE Table_Cafe SET name = N'Bàn 6' WHERE id = 6;
UPDATE Table_Cafe SET name = N'Bàn 7' WHERE id = 7;
UPDATE Table_Cafe SET name = N'Bàn 8' WHERE id = 8;

-- Fix Menu Categories
UPDATE Category SET name = N'Cà phê'   WHERE id = 1;
UPDATE Category SET name = N'Trà'      WHERE id = 2;
UPDATE Category SET name = N'Sinh tố'  WHERE id = 3;
UPDATE Category SET name = N'Nước ép'  WHERE id = 4;
UPDATE Category SET name = N'Bánh ngọt' WHERE id = 5;

-- Fix Menu Items
UPDATE Menu_Item SET name = N'Cà phê đen',       description = N'Cà phê phin truyền thống'                WHERE image_url LIKE '%514432324607%' AND name LIKE '%ph%' AND price = 25000;
UPDATE Menu_Item SET name = N'Cà phê sữa',       description = N'Cà phê sữa đá truyền thống'              WHERE image_url LIKE '%461023058943%';
UPDATE Menu_Item SET name = N'Bạc xỉu',          description = N'Nhiều sữa, ít cà phê, vị ngọt dịu'       WHERE price = 32000 AND name LIKE '%c%x%';
UPDATE Menu_Item SET name = N'Cappuccino',        description = N'Espresso + sữa tạo bọt kiểu Ý'           WHERE price = 45000 AND name LIKE '%appucc%';
UPDATE Menu_Item SET name = N'Latte',             description = N'Cà phê sữa tươi kiểu Ý'                  WHERE price = 45000 AND name LIKE '%att%';
UPDATE Menu_Item SET name = N'Trà đào cam sả',   description = N'Trà hoa quả tươi mát'                    WHERE price = 35000;
UPDATE Menu_Item SET name = N'Trà vải',           description = N'Trà vải tươi thanh mát'                  WHERE price = 35000 AND name LIKE '%i%' AND name NOT LIKE '%dao%';
UPDATE Menu_Item SET name = N'Sinh tố bơ',        description = N'Sinh tố bơ sáp béo ngậy'                WHERE price = 45000 AND name LIKE '%b%';
UPDATE Menu_Item SET name = N'Sinh tố xoài',      description = N'Sinh tố xoài tươi mát'                  WHERE price = 40000;
UPDATE Menu_Item SET name = N'Nước ép cam',       description = N'Cam tươi ép nguyên chất'                 WHERE price = 30000 AND image_url LIKE '%621506%';
UPDATE Menu_Item SET name = N'Bánh tiramisu',     description = N'Bánh kem cà phê kiểu Ý'                  WHERE price = 55000;
UPDATE Menu_Item SET name = N'Croissant bơ',      description = N'Bánh sừng bò Pháp thơm bơ'              WHERE price = 35000;

-- Fix newly added items
UPDATE Menu_Item SET name = N'Americano',         description = N'Espresso pha loãng kiểu Mỹ'              WHERE name LIKE '%mericano%';
UPDATE Menu_Item SET name = N'Espresso',          description = N'Espresso đậm đặc nguyên chất'            WHERE name LIKE '%spresso%' AND price = 30000;
UPDATE Menu_Item SET name = N'Mocha',             description = N'Cà phê chocolate sữa'                    WHERE name LIKE '%ocha%';
UPDATE Menu_Item SET name = N'Cold Brew',         description = N'Cà phê ủ lạnh 24h'                       WHERE name LIKE '%old%rew%';
UPDATE Menu_Item SET name = N'Trà sữa trân châu', description = N'Trà sữa truyền thống với trân châu'    WHERE price = 42000 AND name LIKE '%r%';
UPDATE Menu_Item SET name = N'Trà chanh',         description = N'Trà chanh tươi mát'                      WHERE price = 28000;
UPDATE Menu_Item SET name = N'Matcha latte',      description = N'Trà xanh Nhật Bản sữa'                   WHERE price = 48000;
UPDATE Menu_Item SET name = N'Sinh tố dâu',       description = N'Sinh tố dâu tây tươi'                   WHERE price = 42000 AND name LIKE '%dau%' OR (price = 42000 AND name LIKE '%d%u%');
UPDATE Menu_Item SET name = N'Nước ép dưa hấu',  description = N'Nước ép dưa hấu tươi mát'               WHERE price = 30000 AND image_url LIKE '%527661%';
UPDATE Menu_Item SET name = N'Bánh flan',         description = N'Bánh flan caramel mềm mịn'               WHERE price = 30000 AND image_url LIKE '%624353%';
UPDATE Menu_Item SET name = N'Cheesecake',        description = N'Cheesecake New York béo ngậy'             WHERE price = 60000;

-- Fix Customer names
UPDATE Customer SET full_name = N'Nguyễn Văn An'     WHERE phone = '0912345678';
UPDATE Customer SET full_name = N'Trần Thị Bình'     WHERE phone = '0923456789';
UPDATE Customer SET full_name = N'Lê Hoàng Cường'    WHERE phone = '0934567890';
UPDATE Customer SET full_name = N'Phạm Minh Dương'   WHERE phone = '0945678901';
UPDATE Customer SET full_name = N'Hoàng Thị Em'      WHERE phone = '0956789012';
UPDATE Customer SET full_name = N'Võ Đức Phong'      WHERE phone = '0967890123';
UPDATE Customer SET full_name = N'Đặng Quỳnh Giao'   WHERE phone = '0978901234';
UPDATE Customer SET full_name = N'Bùi Thanh Hải'     WHERE phone = '0989012345';
UPDATE Customer SET full_name = N'Ngô Thị Iphương'   WHERE phone = '0990123456';
UPDATE Customer SET full_name = N'Trịnh Văn Khôi'    WHERE phone = '0901234569';

-- Fix Supplier names
UPDATE Supplier SET name = N'Trung Nguyên Coffee', address = N'Buôn Ma Thuột, Đắk Lắk'   WHERE phone = '0281234567';
UPDATE Supplier SET name = N'Vinamilk',            address = N'TP.HCM'                     WHERE phone = '02839541970';
UPDATE Supplier SET name = N'TH True Milk',        address = N'Nghĩa Đàn, Nghệ An'        WHERE phone = '1800599991';

-- Fix Promotion names
UPDATE Promotion SET name = N'Giảm 10% lần đầu'    WHERE code = 'WELCOME10';
UPDATE Promotion SET name = N'Giảm 20K đơn 100K'   WHERE code = 'SALE20K';
UPDATE Promotion SET name = N'Happy Hour 15%'       WHERE code = 'HAPPY15';

-- Fix Ingredient names
UPDATE Ingredient SET name = N'Cà phê rang xay'     WHERE id = 1;
UPDATE Ingredient SET name = N'Sữa tươi'            WHERE id = 2;
UPDATE Ingredient SET name = N'Sữa đặc'             WHERE id = 3;
UPDATE Ingredient SET name = N'Đường'                WHERE id = 4;
UPDATE Ingredient SET name = N'Đá viên'              WHERE id = 5;
UPDATE Ingredient SET name = N'Trà Oolong'           WHERE id = 6;
UPDATE Ingredient SET name = N'Đào ngâm'             WHERE id = 7;
UPDATE Ingredient SET name = N'Cam tươi'             WHERE id = 8;
UPDATE Ingredient SET name = N'Bơ sáp'               WHERE id = 9;
UPDATE Ingredient SET name = N'Xoài tươi'            WHERE id = 10;
UPDATE Ingredient SET name = N'Bột cacao'            WHERE id = 11;

-- Fix Order notes
UPDATE [Order] SET note = N'Đơn demo 1'         WHERE note LIKE '%demo%1%';
UPDATE [Order] SET note = N'Có khuyến mãi'      WHERE note LIKE '%khuy%';
UPDATE [Order] SET note = N'Mang về'             WHERE note LIKE '%Mang%';
UPDATE [Order] SET note = N'Khách VIP'           WHERE note LIKE '%VIP%';
UPDATE [Order] SET note = N'Bàn tiệc nhỏ'       WHERE note LIKE '%ti%c%';
UPDATE [Order] SET note = N'Đang chế biến'      WHERE note LIKE '%ch%bi%';
UPDATE [Order] SET note = N'Chờ xử lý'          WHERE note LIKE '%x%l%';

PRINT N'✅ Encoding đã được sửa thành công!';
GO
