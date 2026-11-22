# Thử cơ chế né Hunter (2026-11-22 23:40)

Input: bàn phím ảo Input System. Thời gian là thời gian trong game.


## Burst Sprint (E), bị bắt và lượt mới

- [00:00] Làn đo từ (-12.0; -38.5) theo hướng (0.7; 0.7).
- [00:02] Đi bộ 3.5 m/s, bấm E: 5.1 m/s (×1.45). HUD: “E  Burst Sprint  ·  Hiệu lực 4s      T  Scanner tắt”.
- [00:06] Hết hiệu lực, hồi chiêu 25 s. HUD: “E  Burst Sprint  ·  Hiệu lực 1s      T  Scanner tắt”.
- [00:06] Bấm E khi đang hồi: không kích hoạt. Thông báo: “Burst Sprint đang hồi, dùng lại sau 25 s.”.
- [00:01] Chuẩn bị lượt mới: hồi chiêu 0 s (trước đó 25 s).
- [00:03] Hunter từ (-12.0; -34.5): Investigate → Investigate+thấy → Chase+thấy; cách TAY 2.1 m.
- [00:03] Bị Hunter bắt khi đang Burst. Burst còn hiệu lực: không, hồi chiêu 25 s, Scanner tắt. Thông báo: “Bị bắt.”.
- [00:05] Hồi sinh tại (-12.0; -38.5), hồi chiêu còn 25 s. HUD: “E  Burst Sprint  ·  Hồi chiêu 25s      T  Scanner tắt”.

## EMP Mine và Stun Grenade

- [00:01] Bấm F nhặt EMP Mine tại (-3.0; -12.0). Thông báo: “Đã nhặt EMP Mine.”. HUD: “1  › EMP ×1      2  Stun ×0      G  Dùng”.
- [00:01] Bấm F nhặt EMP Mine tại (8.0; -37.0). Thông báo: “Đã nhặt EMP Mine.”. HUD: “1  › EMP ×2      2  Stun ×0      G  Dùng”.
- [00:01] Bấm F nhặt Stun Grenade tại (-6.0; -38.0). Thông báo: “Đã nhặt Stun Grenade.”. HUD: “1  › EMP ×2      2  Stun ×1      G  Dùng”.
- [00:02] Bấm F nhặt Stun Grenade tại (10.5; 1.5). Thông báo: “Đã nhặt Stun Grenade.”. HUD: “1  › EMP ×2      2  Stun ×2      G  Dùng”.
- [00:02] Bấm 1 rồi G: đặt EMP tại (9.5; -27.4). Thông báo: “Đã đặt EMP Mine. Hunter đi qua trong 2 m sẽ chậm 50% trong 8 s.”. HUD: “1  › EMP ×1      2  Stun ×2      G  Dùng”.
- [00:03] EMP kích hoạt khi Hunter đi qua: tốc độ 2.80 → 1.40 m/s trong 8 s. Thông báo: “EMP kích hoạt: Hunter chậm 50% trong 8 s.”. HUD: “EMP: HUNTER CHẬM 8s  ·  HUNTER 1 · PATROL”.
- [00:11] Hết EMP, tốc độ về 2.80 m/s.
- [00:11] Bấm 2 rồi G: ném Stun về phía Hunter cách 6.0 m. Thông báo: “Đã ném Stun Grenade.”.
- [00:12] Hunter choáng. Thông báo: “Stun trúng: Hunter choáng 5 s, sau đó nổi giận và nhanh hơn.”. HUD: “HUNTER BỊ CHOÁNG 5s  ·  HUNTER 1 · PATROL”.
- [00:18] Hunter hồi phục sau 5.5 s (Investigate), Enrage 4.0 s, tốc độ 3.22 m/s. HUD: “HUNTER NỔI GIẬN 4s  ·  HUNTER 1 · PATROL”.
- [00:19] Ném Stun thứ hai khi Hunter nổi giận (2.7 s còn lại): Hunter Investigate. Thông báo: “Stun không có tác dụng: Hunter vừa hồi phục và đang nổi giận.”.
- [00:22] Bấm G khi hết Stun. Thông báo: “Không còn Stun Grenade. Tìm thêm trên map.”.

## Tủ kín (−4; −31,1), vào khi Hunter đang thấy TAY (cấp 1)

- [00:01] Hunter từ (-5.1; -38.1): Investigate → Investigate+thấy → Chase+thấy; cách TAY 5.1 m.
- [00:01] Hunter ở (-5.1; -36.2), cách 5.1 m, đã phát hiện và đang đuổi TAY. HUD: “HUNTER 1 · PATROL”. Bấm C.
- [00:02] Sau khi vào: trong tủ có, an toàn không, Hunter Chase, cách 2.0 m. HUD: “Hunter đã thấy bạn vào tủ!    C  Ra ngay”.
- [00:03] Sau 12 s: Hunter Capture, cách cửa tủ 1.0 m, bị bắt 1 lần.

## Tủ kín (−4; −31,1), vào khi Hunter chưa thấy, Lockdown

- [00:01] Đứng trước tủ, HUD: “C  Chỗ nấp kín”. Bấm C.
- [00:02] Đã vào tủ sau 1.0 s, an toàn tuyệt đối. HUD: “Trong tủ kín · an toàn    C  Ra”.
- [00:02] Hunter Lockdown xuất phát từ (-5.1; -25.1), nhận tín hiệu tại cửa tủ.
- [00:38] Hunter Lockdown điều tra cửa tủ, tới gần nhất 1.1 m; nấp 36 s; Chase: không; bị bắt 0 lần; Hunter Search .
- [00:39] Bấm C, ra khỏi tủ sau 0.8 s, đứng tại cửa tủ (-5.1; -31.1).

## Nấp sau vật cản (−10,75; −18,6) quá 30 s

- [00:02] Bấm C, nấp sau vật cản. HUD: “Nấp sau vật cản · 0 s    C  Ra”. Hunter tuần tra từ (-12.0; -38.0).
- [00:40] Hunter tới kiểm tra chỗ nấp: có, lúc đã nấp 30 s; thấy TAY trước mốc 30 s: không; bị bắt 1 lần, Hunter Capture. HUD 5 s trước mốc: “Nấp sau vật cản · 24 s · Hunter kiểm tra sau 6 s    C  Ra”.

## Scanner (T) bật liên tục

- [00:01] Bấm T, Scanner bật. HUD: “E  Burst Sprint      T  Scanner bật · phát tín hiệu sau 20s”.
- [00:21] Hunter nhận tín hiệu sau 20.0 s bật liên tục (mốc 20 s), điều tra (14.5; -20.0). Thông báo: “Scanner đã phát tín hiệu điện từ.”. HUD vài giây trước đó: “E  Burst Sprint      T  Scanner bật · phát tín hiệu sau 6s”.
- [00:21] Bấm T lần nữa, Scanner tắt.

## Sandbox · Dash (E)

- [00:01] Bấm E trước vật thấp (−9; −8, cao 0,4 m) từ (-9.0; -7.2): lướt từ z -10.2 tới -7.2, hồi chiêu 17 s. Thông báo: “”. HUD: “E  Dash  ·  Hồi chiêu 18s      T  Scanner tắt”.
- [00:02] Bấm E khi tường cách ~1,5 m: x -17.30 → -17.30, hồi chiêu 0 s. Thông báo: “Không lướt được: vật cản quá cao.”.

## Sandbox · Hunter Sensor (E)

- [00:01] Bấm E, Hunter cách 11.0 m (sau tường giữa). HUD: “E  Hunter Sensor  ·  Hiệu lực 5s      T  Scanner tắt      Hunter 11.0 m · phía trước”.
- [00:02] Hunter ra xa 30 m: mất tín hiệu sau lần cập nhật kế tiếp.
- [00:06] Hết 5 s hiệu lực, hồi chiêu 30 s. HUD: “E  Hunter Sensor  ·  Hồi chiêu 30s      T  Scanner tắt”.
- [00:06] Bấm E khi đang hồi: không kích hoạt. Thông báo: “Hunter Sensor đang hồi, dùng lại sau 30 s.”.
