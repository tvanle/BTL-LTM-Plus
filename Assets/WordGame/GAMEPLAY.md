Mục tiêu & Tổng quan
Thể loại: Word‑Brain realtime (grid chữ cái dạng shape bất kỳ, tạo từ đúng bằng cách điền hết ô).

Phong cách: giống Quizizz: mỗi câu hỏi là một level, tất cả người chơi trong phòng chơi đồng bộ trong level, tính điểm theo tốc độ & độ chính xác, bảng xếp hạng live, booster hỗ trợ hoặc cản đối thủ.

Vai trò:
Mỗi user  có 1 account riêng lưu vào database(có lịch sử chơi).

Admin/Host: tạo phòng, chọn chủ đề & bộ câu hỏi, bấm Start.

Player: join bằng room code, nối chữ trong grid → done level → được điểm, đua top.

Luồng chơi (game flow)
Lobby

Admin tạo phòng → nhận RoomCode (6 ký tự). Chọn Topic, thời lượng mỗi level (30s mặc định), số level (10 mặc định).

Người chơi nhập RoomCode → join → thấy danh sách player + trạng thái Ready.

Bắt đầu

Khi Admin Start, server gửi START kèm serverStartTime. Mọi client hiển thị Level 1 đồng bộ.

Mỗi Level đã được định nghĩa sẵn ở game client này rồi và có từng chủ đề, logic game phía client cũng đã có sẵn.

Server chấm đúng/sai; cuối mỗi level hiển thị ai đúng/sai + leaderboard cập nhật live (giống Kahoot).

Kết thúc trận

Sau level cuối: hiện Final Leaderboard, Top 3 huy chương, thống kê (độ chính xác, tốc độ trung bình, streak, booster dùng).

Admin có thể Rematch hoặc Close Room.

Quy tắc:

Từ phải tồn tại trong từ điển chủ đề/chung.

Độ dài tối thiểu: 3 ký tự.

Cho phép ký tự lặp nếu có nhiều ô cùng chữ.

Điểm số & Xếp hạng 4.1 Công thức điểm
Base: 1000/câu đúng.

Speed factor: phụ thuộc % thời gian còn lại (0.5–1.0).

Streak multiplier: +0.1 mỗi streak, tối đa 1.5.

Booster multiplier: ví dụ DoubleUp = x2.

Penalty: −150 mỗi lần sai (tối đa 2 lần).

4.2 Streak & tie‑break

Streak +1 khi đúng liên tiếp, reset khi sai.

Tie‑break theo tổng thời gian thấp + streak cao.

4.3 Bảng xếp hạng

Realtime Leaderboard hiển thị:

Ai đúng (✓) / sai (✗) ở level vừa xong.

Điểm cộng/trừ + tổng điểm.

Thứ hạng thay đổi live.

Các bảng: Trong phòng, Theo chủ đề, Cá nhân (PB).

Booster (power‑ups) Booster Tác dụng Phạm vi Hạn chế DoubleUp x2 điểm câu hiện tại Bản thân 1 lần/3 level Freeze Khóa thao tác đối thủ 3s Tất cả Shield chặn được Reveal Lật 1 ô đúng Bản thân −100 điểm Time+5 +5s cho đồng hồ cá nhân Bản thân Tối đa 2 lần/câu Shield Miễn nhiễm 1 hiệu ứng Bản thân Hết sau khi chặn StreakSave Sai 1 lần không mất streak Bản thân 1 lần/4 level SkipHalf Bỏ qua câu, nhận 50% điểm base Bản thân Không tính streak