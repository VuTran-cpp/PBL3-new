USE pbl3_cafe_management;
GO

-- Mật khẩu mới cho tất cả tài khoản: 123456
-- Hash được tạo bởi chính ứng dụng qua endpoint /dev/hash?password=123456
UPDATE Account 
SET password_hash = '$2a$11$4rJvVHz9099BJL3c1fk9KuKTVK4ieV5OMfO.q6FNuRQjL1Kf9nZAS';

SELECT username, password_hash, LEN(password_hash) as hash_length FROM Account;
GO
