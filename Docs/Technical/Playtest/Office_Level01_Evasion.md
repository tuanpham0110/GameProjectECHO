# Office_Level01 · Cơ chế né Hunter (2026-11-22)

Chuẩn: D06–D09, D13 trong `DesignDecisions.md`. Bổ sung D20, D21 sau lượt này.

## Cách thử

Mọi thao tác trong game đi qua bàn phím ảo của Input System, qua đúng `PlayerInput` như người chơi: C, T, F, 1/2, G, E, W, Esc. Bộ test chỉ đặt sẵn vị trí TAY và Hunter để dựng tình huống. AI Hunter chạy thật. Bản sao `Hunter_Office_Profile` trong test đặt `signalUncertainty = 0` để kết quả lặp lại được; asset gốc không đổi.

- Mã thử: `Tests/PlayMode/EvasionPlaytests.cs`.
  - `OfficeEvasionPlaytests`: 6 tình huống trên map Office.
  - `SandboxEvasionPlaytests`: 2 tình huống trên Blockout_Sandbox. Cấu hình kiểm thử riêng dùng bản sao `Office_Level01_Rules` có `playtestSkills` = Burst, Dash, Sensor.
- Nhật ký từng bước: `Evasion/Playtest_Evasion_log.md` (bản mới nhất: `Builds/Validation/Playtest_Evasion.md`).
- Lệnh: `Unity.exe -batchmode -projectPath D:\EchoVerify -runTests -testPlatform PlayMode -testFilter Evasion`.

## Kết quả từng cơ chế

| Cơ chế | Thao tác | Kết quả sau khi sửa |
| --- | --- | --- |
| Tủ kín, Hunter chưa thấy | Đứng trước tủ (−4; −31,1), HUD "C  Chỗ nấp kín", bấm C. Thu đủ 6 gói, Hunter Lockdown tới điều tra cửa tủ trong 36 s. | Vào sau 1,0 s, HUD "Trong tủ kín · an toàn". Hunter tới cách 1,1 m, không đuổi, không bắt, không kiểm tra sau 30 s. Bấm C: ra sau 0,8 s, đứng tại cửa tủ. |
| Tủ kín, Hunter đã thấy | Hunter cấp 1 cách 7 m, đã phát hiện và đang đuổi TAY. Bấm C. | Vào được tủ, HUD "Hunter đã thấy bạn vào tủ!  C  Ra ngay". Hunter tới cửa tủ và bắt TAY sau khoảng 1 s. |
| Nấp sau vật cản quá 30 s | Bấm C tại (−10,75; −18,6). Hunter tuần tra từ (−12; −38). | HUD đếm giây, từ giây 20 báo "Hunter kiểm tra sau N s". Đúng mốc 30 s, Hunter tới điều tra chỗ nấp và bắt TAY. |
| Scanner | Bấm T, giữ bật cạnh vách tây phòng server, Hunter cách 4 m sau vách. | HUD "Scanner bật · phát tín hiệu sau N s". Sau đúng 20,0 s: thông báo "Scanner đã phát tín hiệu điện từ", Hunter điều tra đúng chỗ TAY, HUD "Hunter đã bắt sóng". Bấm T lần nữa thì tắt. |
| Nhặt vật phẩm | F tại 2 EMP và 2 Stun của Office. | Thông báo "Đã nhặt …", HUD đếm ×1, ×2. |
| EMP Mine | 1 → "Chọn EMP Mine (còn 2)", G đặt trên lối đi, Hunter đi qua. | "Đã đặt EMP Mine…", rồi "EMP kích hoạt: Hunter chậm 50% trong 8 s". HUD "EMP: HUNTER CHẬM 8s". Tốc độ 2,80 → 1,40 m/s, sau 8 s về 2,80 m/s. |
| Stun Grenade | 2, G ném về Hunter cách 6 m. | "Đã ném", rồi "Stun trúng: Hunter choáng 5 s…". HUD "HUNTER BỊ CHOÁNG". Hồi phục sau 5,5 s (5 s choáng + 0,45 s), rồi Enrage 4 s, tốc độ 3,22 m/s (+15 %), HUD "HUNTER NỔI GIẬN". |
| Stun khi Hunter nổi giận | Ném quả thứ hai lúc Enrage còn 2,7 s. | Không choáng. Thông báo "Stun không có tác dụng: Hunter vừa hồi phục và đang nổi giận". G khi hết: "Không còn Stun Grenade". |
| Burst Sprint | Giữ W, bấm E. | 3,5 → 5,1 m/s (×1,45) trong 4 s, rồi hồi chiêu 25 s. Bấm E khi đang hồi: "Burst Sprint đang hồi, dùng lại sau 25 s". |
| Bị bắt khi đang Burst | Bật T, bấm E, để Hunter bắt. | Burst kết thúc, hồi chiêu 25 s được giữ (D05 không reset kỹ năng), Scanner tắt. Sau hồi sinh vẫn còn 25 s. |
| Lượt mới | Esc → Chuẩn bị lượt mới → Bắt đầu. | Hồi chiêu về 0 (đã test). EMP/Stun đặt dở bị xóa và vật phẩm trên map hiện lại theo `LevelManager.ResetRun`: đã đọc code, chưa có test riêng. |
| Dash (Sandbox) | E trước vật thấp 0,4 m ở (−9; −8). | Lướt 3 m qua vật (z −10,2 → −7,2), hồi chiêu 18 s. E khi tường cách 1,5 m: không lướt, không mất hồi chiêu, thông báo "Không lướt được: vật cản quá cao". |
| Hunter Sensor (Sandbox) | E khi Hunter cách 11 m sau tường giữa. | Radar và HUD "Hunter 11.0 m · phía trước". Hunter ra 30 m thì mất tín hiệu sau lần cập nhật kế tiếp (1 s). Hết 5 s, hồi chiêu 30 s, bấm E: "đang hồi". |

## Lỗi tái hiện được và đã sửa

### 1. Hunter thấy TAY vào tủ kín nhưng không lôi TAY ra (D09)

- **Tái hiện:**
  1. Lượt mới.
  2. Đứng ở cửa tủ (−5,1; −31,1).
  3. Để Hunter cách khoảng 7 m phát hiện và đuổi TAY.
  4. Bấm C trước khi Hunter tới.
- **Trước khi sửa** (chạy test hồi quy trên logic cũ):
  - TAY vào được tủ, không được bảo vệ.
  - Trong tủ, cánh tủ che tầm nhìn nên Hunter mất dấu. Hunter điều tra rồi quay về tuyến.
  - Sau 12 s, Hunter ở cách tủ 19,5 m, bị bắt 0 lần.
  - Nghĩa là vào tủ ngay trước mặt Hunter cũng an toàn như vào lúc chưa bị thấy.
- **Nguyên nhân:**
  - `IsSafelyHidden` chỉ xét Hunter có thấy TAY ở khung cuối của lúc vào.
  - Ngoài cờ đó, Hunter không có thông tin nào về việc TAY ở trong tủ.
- **Sửa:**
  - `PlayerController` ghi nhận lần vào tủ bị thấy (`HideWitnessed`) nếu ở bất kỳ khung nào trong lúc vào, Hunter vừa đang đuổi vừa thấy TAY.
  - Khi đó `HunterBrain` biết TAY ở trong tủ, tới cửa tủ và bắt.
  - Một cái liếc chưa đủ thời gian phản ứng không tính là bị thấy (D20).
- **Test hồi quy:** `HunterThatSeesLockerEntryPullsTayOut`. Hỏng trên logic cũ, đạt sau khi sửa: bị bắt 1 lần sau khoảng 1 s.

### 2. Đứng yên thì E (Burst/Dash) và Space đôi khi bị từ chối

- **Tái hiện:**
  1. Vào Sandbox, trang bị Dash.
  2. Đứng yên tại (−9; −10,2), nhìn về phía bắc.
  3. Bấm E.
- **Trước khi sửa:**
  - Không lướt, không có hồi chiêu.
  - Thông báo mới thêm cho biết lý do là "đang cúi hoặc trên không", dù TAY đang đứng trên sàn.
- **Nguyên nhân:**
  - Khi đứng yên, mỗi khung CharacterController chỉ dịch xuống `2 m/s × Δt`.
  - Khung càng ngắn thì bước này càng dễ nhỏ hơn `minMoveDistance` mặc định 0,001 m, vì vậy Unity bỏ qua bước di chuyển và `isGrounded` thành false.
  - Lỗi xảy ra khi test chạy không giới hạn FPS. Ở mức 200–260 FPS của bản build hiện tại thì chưa ghi nhận, nhưng máy mạnh hơn hoặc màn chơi nhẹ hơn có thể gặp.
- **Sửa:** `minMoveDistance = 0` lúc chạy, đúng như Unity khuyên dùng.
- **Test hồi quy:** `DashVaultsLowObstacleAndReportsBlockedDash`: đứng yên, bấm E, lướt qua vật thấp.

## Phản hồi HUD bổ sung

Chỉ dùng HUD và thông báo sẵn có. Không thêm asset mới.

- **Chỗ nấp:**
  - Tủ kín: "Trong tủ kín · an toàn" hoặc "Hunter đã thấy bạn vào tủ!  C  Ra ngay".
  - Nấp sau vật cản: đếm giây; 10 s trước mốc kiểm tra thì báo "Hunter kiểm tra sau N s".
- **Scanner:** "phát tín hiệu sau N s", rồi "Hunter đã bắt sóng".
- **Kỹ năng:**
  - Lúc kích hoạt: "Burst Sprint: tăng tốc 4 s", hoặc Sensor đã/không định vị được Hunter.
  - Bấm E khi đang hiệu lực hoặc đang hồi: báo số giây còn lại.
  - Khi bị từ chối: báo lý do, ví dụ đang cúi, trên không, vật cản quá cao hoặc không có chỗ đáp.
  - Dòng kỹ năng chuyển màu xanh khi đang hiệu lực.
- **Vật phẩm:**
  - 1/2: "Chọn … (còn n). G để dùng."
  - G: "Đã đặt EMP…" hoặc "Đã ném Stun Grenade".
  - Khi phát huy tác dụng: "EMP kích hoạt…", "Stun trúng…", "Stun không có tác dụng…" hoặc "Stun nổ trượt…".
  - Hết vật phẩm: "Không còn … Tìm thêm trên map."
- **Trạng thái Hunter** (góc trên phải): "EMP: HUNTER CHẬM Ns", "HUNTER BỊ CHOÁNG Ns", "HUNTER NỔI GIẬN Ns" (màu đỏ).

## Kết quả kiểm thử (Unity 6000.3.29f1, bản sao `D:\EchoVerify`)

- EditMode **20/20**.
- PlayMode **18/18** (10 test cũ và 8 test né Hunter mới), 212 s.
- Bot playtest Office hoàn tất sau 202 s, bị bắt 1 lần, kẹt 0, không ghi nhận vấn đề.
- Layout, Rules, `TAY_Tuning`, `Hunter_Office_Profile` không đổi, nên không cần chạy lại Setup Gameplay, Bake hay Validation.

## Cần người thiết kế quyết định

- **Scanner:**
  - Tắt rồi bật lại thì đồng hồ 20 s bắt đầu lại. Nhấp T trước mốc là tránh được hoàn toàn rủi ro bị bắt sóng.
  - Mỗi lần bật, Scanner chỉ phát tín hiệu một lần.
  - Cả hai điều này D08 và D17 đều chưa nói rõ. Lượt này không đổi.
- **Dash:** gặp vật cản quá cao thì Dash bị từ chối, không mất hồi chiêu và có thông báo, chứ không lướt tới sát vật cản. D07 chỉ ghi "chịu va chạm với vách/cửa", nên cách xử lý này cần xác nhận.
- **Stun:** Hunter miễn choáng trong lúc hồi phục và Enrage (code có từ trước). D08 chưa ghi điều này. Giờ người chơi đã được báo khi gặp trường hợp này.
- **Tủ bị thấy:** Hunter biết TAY ở trong tủ cho tới khi TAY ra hoặc bị bắt, kể cả sau khi Hunter bị Stun. Cách thoát là Stun Hunter rồi ra khỏi tủ và chạy đi.
