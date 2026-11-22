# Project ECHO · Kế hoạch triển khai

Cập nhật 2026-11-22 (chiều). Kết quả kiểm thử lấy từ Unity 6000.3.29f1 batch mode trên bản sao `D:\EchoVerify`.

| Giai đoạn | Nội dung | Trạng thái | Xác minh |
| --- | --- | --- | --- |
| 1. Audit | Đọc GDD, Gameplay Specification, báo cáo giữa kỳ. Kiểm tra package, FBX, importer. **Đo hình học FBX Office** (lưới 0,25 m, ảnh mặt bằng, danh sách đối tượng, ô cửa). | Xong | `ProjectAudit.md`, `OfficeMap/OfficeMapAnalysis.md`, `OfficeMap/OfficeDoorways.md` |
| 2. Editor Automation | Menu Validate / Generate Level / Setup Gameplay / Bake Navigation / Run Validation. Gắn FBX nguyên hierarchy, sinh prefab collision (1.604 MeshCollider từ 6.509 mesh, bỏ lá/thân chậu cây), 14 cửa tương tác từ cánh cửa FBX, tường biên, layout dạng asset, snap marker lên NavMesh, báo cáo đường đi, ảnh NavMesh và ảnh xem trước. | Xong | Validation đạt cho Office (63 tuyến NavMesh) và Blockout Sandbox |
| 3. Nền tảng gameplay | GameManager, GameStateMachine, LevelManager, PlayerController (CharacterController), PlayerInput (Input System), ThirdPersonCamera (Cinemachine 3), InteractionSystem, MissionManager, InventorySystem, TimerSystem, SaveSystem. | Xong | EditMode: timer, trạng thái, save. PlayMode: va chạm tường blockout và tường thật Office |
| 4. Hunter AI | Bốn cấp Patrol/Search/Hunt/Lockdown, thị giác + LoS raycast, thính giác theo sự kiện, vị trí biết cuối, Investigate/Search/Chase/Ambush/Capture/Stunned/Recovery, phát hiện kẹt, mở cửa đóng trên tuyến đường (3 s), kiểm tra chỗ nấp tương đối > 30 s. | Xong ở mức Vertical Slice | PlayMode: tuần tra, tìm đường vòng, không nhìn xuyên tường thật, tự mở cửa phòng server |
| 5. Vòng chơi | Sảnh chọn kỹ năng → thu thập → Hunter tăng cấp → Relay (rời vùng giữ tiến độ) → cổng → thắng trước giờ. Bị bắt, mất 2 dữ liệu, hồi sinh, nhặt lại, retry. | Xong | PlayMode trên Office và Sandbox, bot input thật trên Office |
| 6. Kỹ năng, vật phẩm, UI | Burst Sprint, Dash, Hunter Sensor, cooldown, EMP, Stun, giáp, Scanner, HUD, cảnh báo Hunter, Relay progress, timer, pause, menu, kết quả. **Hướng dẫn ba chặng** (`MissionGuide`): briefing 3 bước + phím, dòng mục tiêu, chỉ đường NavMesh tới Relay/cổng, Scanner dò tín hiệu dữ liệu, cột tín hiệu trên gói dữ liệu, màn hình Relay đổi màu theo chặng, thông báo khi rời/vào vùng Relay và khi bị bắt làm hủy truyền. | Xong ở mức chức năng | Ảnh UI từ bản build trong `Playtest/` |
| 7. Playtest vertical slice | Bot input thật (bàn phím/chuột ảo Input System) chơi từ menu tới cổng, bị bắt, hồi sinh, nhặt lại, pause, lượt mới. Đo hiệu năng trong bản build (`-echoPerf`). | Xong phần tự động, chờ người chơi thật | `Playtest/Office_Level01_Playtest.md` |
| 8. Kiểm thử và build | EditMode 20/20, PlayMode 10/10, build Windows x64. | Xong | `Builds/Validation/` |
| 9. Tối ưu render | Profile bằng FrameTimingManager + tắt thử từng yếu tố. Windows dùng Direct3D 11, GPU Resident Drawer, SSAO nguồn Depth/AfterOpaque. p95 ở 3 góc nặng giảm 63–74 %. | Xong | `Playtest/Office_Level01_PerfOptimization.md` |

## Kết quả kiểm thử gần nhất

- EditMode **20/20 đạt**: thêm 3 test hướng dẫn chặng (thu thập → Relay → truyền → cổng, bị bắt khi truyền quay về chặng 1, briefing lấy số liệu từ `LevelRules`). Test layout kiểm tra thêm việc loại trừ lá chậu cây khỏi collision.
- PlayMode **10/10 đạt** (179 s, lần chạy sau tối ưu render):
  - Sandbox: CharacterController dừng ở tường, Hunter không nhìn xuyên tường, tuần tra và tìm đường vòng, vòng nhiệm vụ đầy đủ.
  - Office: đứng trên sàn thật, bị vách phòng server chặn, không rơi khỏi mép sàn. Hunter không nhìn xuyên vách thật, NavMesh nối Hunter Spawn → Relay/Player Spawn, Hunter tự mở cửa bắc phòng server. Vòng nhiệm vụ đầy đủ. **Mới:** TAY đi qua chậu cây cạnh cửa phụ phòng họp, và bot playtest bằng input thật hoàn tất lượt trên map thật.
- Bot playtest: hoàn tất ở mọi lần chạy (148–312 s thời gian game, bị bắt 0–4 lần tùy Hunter, kẹt 0 lần). Chi tiết ở `Playtest/Office_Level01_BotLog.md`.
- Validation Office: đường NavMesh Player Spawn → Relay 37,8 m, Relay → cổng 43,7 m, xa nhất Player Spawn → dữ liệu phòng họp 78,2 m.
- Windows x64: `Builds/Windows/ProjectECHO.exe`, gói gửi tester `Builds/ProjectECHO_Office_Playtest_2026-11-22.zip` (82 MB, kèm `CHECKLIST_CHOI_THU.txt`, không kèm thư mục Burst debug). Bản build cũ ở `Builds/Windows-Office/` đã lỗi thời.
- Hiệu năng sau tối ưu (GTX 1650, 1080p cửa sổ, vSync tắt): Relay nhìn tây bắc 13,3 / 21,8 → 5,9 / 6,7 ms (TB / p95), sảnh nhìn bắc 11,4 / 18,7 → 5,2 / 6,9 ms, phòng server nhìn tây 11,7 / 22,4 → 5,3 / 5,9 ms. Vòng chạy có Hunter: p99 14,1 → 7,3 ms, không còn khung > 25 ms. Hết tuyến 2 vòng (16/16 chặng, Lockdown suốt lượt, Hunter không bắt): p95 6,3, p99 7,1 ms, 0 khung > 25 ms. Xem `Playtest/Office_Level01_PerfOptimization.md`.
- Lần chạy gần nhất (sau khi thêm phần đo hết tuyến): EditMode 20/20, PlayMode 10/10 (118 s). Bot hoàn tất sau 148 s, bị bắt 1 lần, kẹt 0.

## Kiến trúc

- **Dữ liệu**: `LevelRules` (luật, kèm mô tả vị trí Relay/cổng cho HUD), `LevelLayout` (bố trí), `HunterProfile`, `PlayerTuning` là ScriptableObject trong `Settings/`.
- **Runtime**: `GameManager` điều phối `GameStateMachine`, `TimerSystem`, `MissionManager`, `InventorySystem`, `SaveSystem`. `MissionGuide` sinh câu hướng dẫn theo chặng cho HUD, briefing và menu tạm dừng. `LevelManager` nối tham chiếu scene, phát sự kiện tiếng ồn và đồng bộ trạng thái Relay/cổng/dữ liệu. `HunterBrain` chỉ biết vị trí TAY qua thị giác có LoS, sự kiện tiếng ồn, tín hiệu Scanner hoặc kiểm tra chỗ nấp lâu (có sai số). `PerformanceProbe` chỉ chạy khi có `-echoPerf`.
- **Editor**: `OfficeLevelTools` (pipeline theo `LevelTarget`), `OfficeSceneFactory` (prefab, gameplay theo layout, tự thêm cột tín hiệu vào prefab Mission Data cũ), `LevelLayouts` (giá trị khởi tạo đo từ FBX), `OfficeMapAnalysis`, `OfficeDoorwayDiagnostics`, `LevelPreviews`.
- **Scene**: `Level` (NavMeshSurface) → `Environment` (FBX instance, prefab Collision, Boundary) + nhóm gameplay sinh tự động.

## Còn tồn tại

- Chưa có lượt chơi của người thật. Bot biết trước vị trí mục tiêu nên chưa đo được thời gian tìm kiếm của người mới.
- Chặng Relay ở Lockdown: đứng yên trong vùng thì bị bắt trước 20 s. Phải rời vùng rồi quay lại. Waypoint 10 cách Relay 1,5 m. Chờ người chơi thật xác nhận trước khi đổi layout hoặc profile.
- Nhân vật TAY/Hunter vẫn là hình khối; chưa có rig, animation, âm thanh (chưa có tài nguyên).
- Hunter chưa nhảy qua vật thấp (cần off-mesh link + animation, D14). Smoke Bomb, Decoy, cửa khóa giữ F ngoài phạm vi MVP hiện tại.
- Hiệu năng: góc nặng nhất giờ bị giới hạn ở GPU (khoảng 5,5 ms, 1,9 triệu tam giác). Chưa có occlusion Umbra/static batching (cần static flag trên 6.545 node FBX). URP giảm độ phân giải bóng đổ đèn phụ để vừa atlas 2048, không ảnh hưởng frame time. Windows chỉ build Direct3D 11.

## Bước tiếp theo

1. Gửi zip cho 2–3 người chơi mới theo `Playtest/Checklist_NguoiChoiMoi.md`, chỉnh layout/luật theo phản hồi (thời gian tìm dữ liệu, số lần bị bắt ở Relay). Checklist không còn gợi ý luật Relay, nên câu trả lời sau khi chơi cho biết người mới có tự hiểu được từ game không.
2. Khi thêm nội dung, đo lại bằng `-echoPerf` và so với `Playtest/Perf/r3_ban_cuoi.md`. Nếu cần giảm GPU: thử occlusion Umbra hoặc SSAO downsample.
3. Tích hợp model/rig/animation TAY và Hunter khi có tài nguyên; thêm âm thanh bước chân, Relay, cảnh báo.
4. Dựng Level tiếp theo bằng `LevelTarget` + `LevelLayout` mới.
