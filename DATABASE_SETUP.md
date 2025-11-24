# Database Setup Guide - Word Game

Hướng dẫn cài đặt và thiết lập cơ sở dữ liệu MySQL cho Word Game multiplayer với đầy đủ tính năng authentication, friends, và match history.

## Yêu Cầu

- **MySQL Server 8.0+** hoặc **MariaDB 10.5+**
- **HeidiSQL** / **phpMyAdmin** / **MySQL Workbench** (tùy chọn, để quản lý database)
- **.NET 8.0** (đã được cài đặt cho server)

## Cài Đặt MySQL

### Windows

1. **Download MySQL**
   - Truy cập: https://dev.mysql.com/downloads/installer/
   - Tải MySQL Installer (mysql-installer-community-xxx.msi)

2. **Cài Đặt**
   ```
   - Chạy installer
   - Chọn "Developer Default" hoặc "Server only"
   - Cài đặt MySQL Server
   - Đặt root password (ví dụ: "root123")
   - Cấu hình MySQL Server làm Windows Service
   - Hoàn tất cài đặt
   ```

3. **Kiểm Tra Cài Đặt**
   ```bash
   # Mở Command Prompt
   mysql --version
   # Output: mysql  Ver 8.0.xx for Win64 on x86_64
   ```

### Linux / macOS

```bash
# Ubuntu/Debian
sudo apt update
sudo apt install mysql-server
sudo mysql_secure_installation

# macOS (Homebrew)
brew install mysql
brew services start mysql
mysql_secure_installation
```

## Tạo Database

### Bước 1: Đăng Nhập MySQL

```bash
# Mở MySQL CLI
mysql -u root -p
# Nhập password bạn đã đặt khi cài đặt
```

### Bước 2: Tạo Database

```sql
-- Tạo database mới
CREATE DATABASE word_game CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Sử dụng database
USE word_game;
```

### Bước 3: Chạy Schema SQL

Copy toàn bộ nội dung file `Server/Database/schema.sql` và paste vào MySQL CLI, hoặc:

```bash
# Từ Command Prompt / Terminal
mysql -u root -p word_game < Server/Database/schema.sql
```

### Bước 4: Kiểm Tra Tables

```sql
USE word_game;
SHOW TABLES;
```

**Kết quả mong đợi:**
```
+---------------------+
| Tables_in_word_game |
+---------------------+
| friendships         |
| game_invitations    |
| match_history       |
| match_player_results|
| session_tokens      |
| user_stats          |
| users               |
+---------------------+
7 rows in set
```

## Cấu Hình Connection String

### Server Configuration

Mở file `Server/GameServer/Program.cs` và cập nhật connection string:

```csharp
// Dòng 12
var connectionString = "Server=localhost;Database=word_game;User=root;Password=YOUR_PASSWORD;";
```

**Thay thế:**
- `localhost` → Địa chỉ MySQL server của bạn
- `root` → MySQL username của bạn
- `YOUR_PASSWORD` → Password của MySQL user

**Ví dụ:**
```csharp
// Localhost với password "root123"
var connectionString = "Server=localhost;Database=word_game;User=root;Password=root123;";

// Remote server
var connectionString = "Server=192.168.1.100;Database=word_game;User=gameadmin;Password=securepass;";
```

## Tạo MySQL User Riêng (Recommended)

Thay vì dùng root, nên tạo user riêng cho game:

```sql
-- Tạo user mới
CREATE USER 'gameserver'@'localhost' IDENTIFIED BY 'game123pass';

-- Cấp quyền cho database word_game
GRANT ALL PRIVILEGES ON word_game.* TO 'gameserver'@'localhost';

-- Áp dụng thay đổi
FLUSH PRIVILEGES;
```

**Connection string:**
```csharp
var connectionString = "Server=localhost;Database=word_game;User=gameserver;Password=game123pass;";
```

## Kiểm Tra Kết Nối

### Test 1: Command Line

```bash
mysql -u gameserver -p word_game
# Nhập password: game123pass
# Nếu kết nối thành công → OK
```

### Test 2: Chạy Server

```bash
cd Server/GameServer
dotnet run
```

**Output mong đợi:**
```
WordBrain Game Server
=====================
Server starting on port 8080...
Game Server started on port 8080
Press Ctrl+C to quit the server...
```

**Nếu có lỗi kết nối:**
```
Error: Unable to connect to any of the specified MySQL hosts.
```

→ Kiểm tra lại connection string và MySQL service

## Database Schema Chi Tiết

### 1. Users Table
Lưu thông tin người dùng và authentication
```sql
- id: GUID primary key
- username: Tên đăng nhập (unique)
- email: Email (unique)
- password_hash: BCrypt hashed password
- display_name: Tên hiển thị
- avatar_url: Link avatar
- is_online: Trạng thái online
- last_login_at: Lần đăng nhập cuối
- created_at, updated_at: Timestamps
```

### 2. User Stats Table
Thống kê game của người chơi
```sql
- total_score: Tổng điểm tích lũy
- best_score: Điểm cao nhất 1 ván
- best_streak: Streak tốt nhất
- games_played: Số ván đã chơi
- games_won: Số ván thắng
- total_words_found: Tổng số từ tìm được
```

### 3. Friendships Table
Quản lý quan hệ bạn bè
```sql
- user_id1, user_id2: ID 2 người
- status: 'pending', 'accepted', 'blocked'
- requester_id: Người gửi lời mời
- created_at, accepted_at: Timestamps
```

### 4. Game Invitations Table
Lời mời chơi game
```sql
- sender_id, receiver_id: Người gửi/nhận
- room_code: Mã phòng
- status: 'pending', 'accepted', 'declined', 'expired'
- expires_at: Thời gian hết hạn
```

### 5. Match History Table
Lịch sử các ván đấu
```sql
- room_code: Mã phòng
- category: Chủ đề
- host_id: Người tạo phòng
- winner_id: Người thắng
- started_at, completed_at: Thời gian
```

### 6. Match Player Results Table
Kết quả từng người chơi trong ván
```sql
- match_id: ID ván đấu
- user_id: ID người chơi
- final_score: Điểm cuối
- best_streak: Streak tốt nhất
- rank_position: Thứ hạng
```

### 7. Session Tokens Table
Quản lý phiên đăng nhập
```sql
- user_id: ID người dùng
- token: Authentication token
- expires_at: Thời gian hết hạn (30 ngày)
```

## Stored Procedures

### GetUserFriends
Lấy danh sách bạn bè của user
```sql
CALL GetUserFriends('user-guid-here');
```

### GetUserMatchHistory
Lấy lịch sử đấu của user (có phân trang)
```sql
CALL GetUserMatchHistory('user-guid-here', 10, 0);
-- Tham số: user_id, limit, offset
```

### GetGlobalLeaderboard
Lấy bảng xếp hạng toàn server
```sql
CALL GetGlobalLeaderboard(100, 0);
-- Tham số: limit, offset
```

### CleanExpiredData
Dọn dẹp session tokens và invitations hết hạn
```sql
CALL CleanExpiredData();
```

**Nên chạy định kỳ (cronjob):**
```bash
# Linux crontab - Chạy mỗi ngày lúc 2AM
0 2 * * * mysql -u gameserver -pYOUR_PASSWORD word_game -e "CALL CleanExpiredData();"
```

## Testing Database

### Tạo User Test

```sql
-- Tạo user test
INSERT INTO users (id, username, email, password_hash, created_at, updated_at)
VALUES (
    UUID(),
    'testuser',
    'test@example.com',
    '$2a$11$abcdefghijklmnopqrstuvwxyz1234567890',  -- BCrypt hash
    NOW(),
    NOW()
);

-- Tạo stats cho user
INSERT INTO user_stats (id, user_id)
SELECT UUID(), id FROM users WHERE username = 'testuser';
```

### Kiểm Tra Dữ Liệu

```sql
-- Xem tất cả users
SELECT username, email, is_online, created_at FROM users;

-- Xem stats của user
SELECT u.username, s.total_score, s.games_played, s.games_won
FROM users u
JOIN user_stats s ON u.id = s.user_id;

-- Xem friendships
SELECT
    u1.username as user1,
    u2.username as user2,
    f.status,
    f.created_at
FROM friendships f
JOIN users u1 ON f.user_id1 = u1.id
JOIN users u2 ON f.user_id2 = u2.id;
```

## Troubleshooting

### Lỗi: Can't connect to MySQL server

**Giải pháp:**
```bash
# Windows - Kiểm tra MySQL service
services.msc
# Tìm "MySQL80" hoặc "MySQL"
# Nhấp chuột phải → Start

# Linux
sudo systemctl status mysql
sudo systemctl start mysql
```

### Lỗi: Access denied for user

**Kiểm tra username/password:**
```bash
mysql -u gameserver -p
# Nhập password và test
```

**Reset password nếu quên:**
```sql
-- Đăng nhập bằng root
mysql -u root -p

-- Đổi password user
ALTER USER 'gameserver'@'localhost' IDENTIFIED BY 'new_password';
FLUSH PRIVILEGES;
```

### Lỗi: Table doesn't exist

**Chạy lại schema:**
```bash
mysql -u root -p word_game < Server/Database/schema.sql
```

### Lỗi: Character encoding issues

**Kiểm tra encoding:**
```sql
SHOW VARIABLES LIKE 'char%';
```

**Nên có:**
```
character_set_database = utf8mb4
character_set_server = utf8mb4
```

## Backup & Restore

### Backup Database

```bash
# Full backup
mysqldump -u root -p word_game > backup_word_game.sql

# Chỉ schema (không có data)
mysqldump -u root -p --no-data word_game > schema_only.sql

# Chỉ data (không có schema)
mysqldump -u root -p --no-create-info word_game > data_only.sql
```

### Restore Database

```bash
# Restore từ backup
mysql -u root -p word_game < backup_word_game.sql
```

## Security Best Practices

1. **Không dùng root user cho production**
2. **Dùng strong password cho MySQL users**
3. **Hạn chế remote access nếu không cần:**
   ```sql
   CREATE USER 'gameserver'@'localhost' ...  -- Chỉ localhost
   -- Thay vì
   CREATE USER 'gameserver'@'%' ...  -- Cho phép mọi IP (không an toàn)
   ```
4. **Backup database thường xuyên**
5. **Không commit connection string có password lên git**
   - Dùng environment variables:
   ```csharp
   var password = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");
   var connectionString = $"Server=localhost;Database=word_game;User=gameserver;Password={password};";
   ```

## Next Steps

Sau khi setup database xong:

1. ✅ Chạy server: `dotnet run` trong `Server/GameServer/`
2. ✅ Mở Unity project
3. ✅ Tạo UI Login screen prefab (xem `UNITY_UI_SETUP_GUIDE.md`)
4. ✅ Test registration → login → play game
5. ✅ Kiểm tra database có lưu match history không

## Support

Nếu gặp vấn đề:
1. Kiểm tra MySQL service đang chạy
2. Kiểm tra connection string đúng
3. Kiểm tra firewall không block port 3306
4. Xem MySQL error log: `/var/log/mysql/error.log` (Linux) hoặc `C:\ProgramData\MySQL\MySQL Server 8.0\Data\*.err` (Windows)
