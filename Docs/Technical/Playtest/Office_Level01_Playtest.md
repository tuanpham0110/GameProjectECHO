# Office_Level01 · Playtest vertical slice (2026-11-22)

Mốc cấu hình: 6 gói dữ liệu, 12:00, Relay 20 giây (bán kính 3 m), Burst Sprint. Không đổi giá trị nào trong `Office_Level01_Rules`, `TAY_Tuning`, `Hunter_Office_Profile` hoặc vị trí mục tiêu trong `Office_Level01_Layout`.

## Cách chơi thử trong phiên này

Phiên làm việc không điều khiển được màn hình/Unity Editor bằng tay, nên chưa có lượt chơi của người thật. Thay vào đó:

- **Bot input thật** (`Tests/PlayMode/OfficePlaythroughTests.cs`): bàn phím và chuột ảo của Input System, đi qua đúng `PlayerInput` và `InputSystemUIInputModule` như người chơi. Menu dùng mũi tên + Enter, di chuyển bằng W và chuột (xoay camera), F nhặt/mở cửa/Relay, E Burst Sprint, T Scanner, Esc tạm dừng. Hunter chạy AI thật. Bot biết vị trí mục tiêu (không mô phỏng việc tìm), chạy x2 tốc độ. Khi bị phát hiện bot chạy (Shift + Burst) ra xa Hunter rồi quay lại mục tiêu.
- **Bản build Windows** chạy với `-echoPerf`: tự vào lượt, đo frame time và chụp UI thật (`Builds/Validation/Perf/`).

Chưa xác minh được: cảm giác điều khiển/camera, khả năng đọc UI trên màn hình thật, việc người mới tự tìm dữ liệu khi không biết vị trí, âm thanh (chưa có).

## Quan sát và xử lý

| # | Quan sát (vị trí, thao tác) | Ảnh hưởng | Xử lý |
| --- | --- | --- | --- |
| 1 | TAY **kẹt tại (−3,3; 0,2)** mỗi lần đi từ phòng họp (Research File) về phía nam qua cửa phụ phòng họp. Collider chạm: lá và thân `Potted plant.002`. NavMesh vẫn coi lối này đi được nên Hunter và đường chỉ dẫn đều đi qua đây. Ghi nhận 3/3 lần đi qua trong một lượt. | Cản đường, người chơi tưởng là tường vô hình | Loại `faceted leaf`, `Plant stem` khỏi collision (layout). MeshCollider 1.684 → 1.604. Test hồi quy `PlayerPassesPottedPlantByMeetingRoomSideDoor`. |
| 2 | **Đứng yên tại Relay**: trong lượt đầu (12 phút), mọi lần bắt đầu truyền đều bị bắt sau 0–13 s (cần 20 s), hết giờ với 4/6 dữ liệu. Lúc này Hunter ở cấp Lockdown, chạy 4,2 m/s; Waypoint 10 (24; −27,3) chỉ cách Relay 1,5 m; Relay phát tiếng ồn 15 m khi bắt đầu truyền. | Chặng 2 không qua được nếu không biết luật "rời vùng giữ tiến độ" | Khi bot rời vùng lúc Hunter tới rồi quay lại, cả 4 lượt sau đều truyền xong (bị bắt 0–2 lần, hoàn tất sau 148–217 s). Luật đã chơi được, nên **không chỉnh** thời gian, tốc độ hay vị trí. Thay vào đó báo rõ luật: briefing bước 2, dòng mục tiêu "Hunter tới thì rời vùng, tiến độ được giữ", thông báo khi rời/vào vùng, bảng Relay ghi "TẠM DỪNG, QUAY LẠI VÙNG". |
| 3 | Khi đủ 6/6, HUD chỉ đổi thành "HUNTER 4 · LOCKDOWN ▲", không nói việc tiếp theo hay Relay ở đâu. Bảng Relay chỉ hiện sau khi đã bấm F tại Relay. | Lạc ở chặng 2 | Thông báo "Đủ dữ liệu… Đến Data Relay (phía nam phòng server)". Bảng dưới màn hình "ĐẾN DATA RELAY · N m · hướng" theo đường NavMesh. |
| 4 | Bị bắt khi đang truyền chỉ hiện "Bị bắt. Mất 2 gói". Bị bắt **sau khi đã truyền xong** (lượt thứ 3 của bot, tại (23,9; −33,6) trên đường về cổng): dữ liệu bị hủy theo D02, cổng đóng lại, TAY hồi sinh cạnh cổng đang khóa mà không có giải thích. | Khó hiểu trạng thái nhiệm vụ | Thông báo thêm "Truyền bị hủy." hoặc "Dữ liệu đã truyền bị lỗi, cổng đóng lại." Dòng mục tiêu quay về 1/3. |
| 5 | Gói dữ liệu chỉ lọt vào tầm nhìn ở 2,5–7,4 m trên tuyến đi (Access Key (4,5; −42,5) chỉ thấy từ 2,5 m). HUD không có gợi ý tìm. | Người mới dễ bỏ sót | Thêm cột tín hiệu mảnh dùng lại vật liệu `Data signal` (không collider, không đổ bóng). Khi bật Scanner (T), dòng mục tiêu hiện "tín hiệu gần nhất ~N m · hướng" theo đường thẳng; bật lâu vẫn bị Hunter bắt sóng như cũ. |
| 6 | Briefing chỉ có một dòng, bảng phím chỉ có trong menu tạm dừng (4 phím). | Người mới không biết F/T/C/E | Briefing ghi 3 bước lấy số liệu từ `LevelRules`, thêm dòng phím đầy đủ (cả trong menu tạm dừng). |
| 7 | Màn thua chỉ ghi "Hết thời gian trước khi thoát." Khi nhặt dữ liệu không có xác nhận nào ngoài bộ đếm. | Thiếu phản hồi | Màn kết quả ghi số dữ liệu, % Relay, số lần bị bắt. Nhặt dữ liệu có thông báo "Đã nhặt X (n/6)". |
| 8 | Màn hình Relay có màu cố định. | Không nhìn ra Relay đã sẵn sàng hay chưa | Màn hình Relay đổi theo chặng: mờ khi chưa đủ dữ liệu, sáng khi sẵn sàng, nhấp nháy khi đang truyền, xanh lá khi xong. Lời nhắc F đổi theo (ví dụ "Bắt đầu truyền dữ liệu"). Cổng khóa ghi "cần truyền Relay xong". |

Đã chạy ổn, không cần sửa: điều hướng menu bằng bàn phím (Chuẩn bị nhiệm vụ → Burst Sprint → Xác nhận → Bắt đầu), mở cửa đóng bằng F (cửa tây phòng server, cửa phụ phòng họp), Esc tạm dừng (đồng hồ đứng) và tiếp tục, bị bắt thì mất 2 gói và gói trở về chỗ cũ, hồi sinh tại Player Spawn, nhặt lại, "Chuẩn bị lượt mới" từ màn kết quả và từ menu tạm dừng (lượt mới về 0/6, đồng hồ 12:00).

## Hướng dẫn ba chặng sau khi sửa

| Chặng | Dòng mục tiêu (góc trên trái) | Bảng dưới màn hình | Trong map |
| --- | --- | --- | --- |
| 1/3 Thu thập | "Tìm dữ liệu n/6 · T bật Scanner để dò tín hiệu"; khi Scanner bật: "tín hiệu gần nhất ~N m · hướng" | ẩn | Cột tín hiệu trên mỗi gói, Scanner tô sáng gói trong nón đèn, màn hình Relay mờ |
| 2/3 Relay | "Đến Data Relay (phía nam phòng server) · N m · hướng"; khi truyền: "Giữ trong vùng Relay. Hunter tới thì rời vùng, tiến độ được giữ" | "ĐẾN DATA RELAY · N m · hướng" → "DATA RELAY n%" / "TẠM DỪNG, QUAY LẠI VÙNG" | Màn hình Relay sáng, nhấp nháy khi truyền |
| 3/3 Cổng | "Về cổng thoát (sảnh lễ tân, logo AETHER) · N m · hướng" | "CỔNG THOÁT ĐÃ MỞ · N m · hướng" | Thanh xanh trên cổng, màn hình Relay xanh lá |

Khoảng cách và hướng ở chặng 2–3 tính theo đường NavMesh (hướng tới góc rẽ đầu tiên), cập nhật 2,5 lần/giây. Ảnh UI thật: `ui_briefing.png`, `ui_hud_collect.png`, `ui_hud_relay.png`.

## Kết quả kiểm tra (Unity 6000.3.29f1, bản sao `D:\EchoVerify`)

- Setup Gameplay + Bake Navigation + Run Validation: đạt cho Office và Sandbox. Đường NavMesh Player Spawn → Relay 37,8 m, Relay → cổng 43,7 m.
- EditMode **20/20** (thêm 3 test hướng dẫn chặng; test layout kiểm tra loại trừ lá cây).
- PlayMode **10/10** (thêm test lối đi cạnh chậu cây và bot playtest; cả bộ chạy 97 s).
- Lượt bot cuối (`Office_Level01_BotLog.md`): hoàn tất sau 148 s thời gian game, còn 9:32, Hunter phát hiện 13 lần, bị bắt 0 lần, né 28 s, kẹt 0 lần. Lượt 2 có retry, bị bắt (2 → 0 gói), hồi sinh, nhặt lại, Esc, lượt mới. Không có vấn đề ghi nhận.
- Bot biết trước vị trí, nên 148–217 s chỉ là mức dưới. Người mới sẽ chậm hơn nhiều.

## Hiệu năng (mốc cho lượt tối ưu)

Bản build Windows x64, 1920×1080 cửa sổ, vSync tắt, chất lượng PC, GTX 1650 (D3D12), i5-10300H. Mỗi vị trí đo 4 hướng camera, mỗi hướng 3 s.

| Vị trí | FPS TB | Frame time TB | p95 | Hướng nặng nhất |
| --- | --- | --- | --- | --- |
| Sảnh lễ tân (Player Spawn) | 168 | 6,0 ms | 14,7 ms | Bắc: 84 FPS, 11,9 ms, 8.608 batch, 2,2 triệu tam giác |
| Phòng server (giữa hai dãy rack) | 144 | 7,0 ms | 15,6 ms | Tây: 86 FPS, 11,6 ms, 6.046 batch |
| Data Relay | 167 | 6,0 ms | 15,0 ms | Tây bắc: 76 FPS, 13,2 ms, 10.988 batch, 3,3 triệu tam giác |

Hướng nhìn qua văn phòng mở tốn 6.000–11.000 batch, gấp khoảng 50 lần hướng nhìn vào tường. Kết quả này khớp với ghi chú "chưa có static batching/occlusion culling". Chi tiết ở `Office_Level01_Perf.md`.

## Câu hỏi cần người chơi thật trả lời

- Chặng 1 mất bao lâu khi không biết vị trí? Cột tín hiệu và Scanner có làm việc tìm quá dễ không?
- Lockdown ở Relay có quá gắt không (rời vùng và quay lại bao nhiêu lần)? Có cần dời Waypoint 10 ra xa Relay không?
- Bị bắt sau khi đã truyền xong mà phải làm lại cả chặng 1–2 (D02) có gây ức chế không?
