# Project ECHO · Kế hoạch triển khai

Cập nhật 2026-11-22. Kết quả kiểm thử lấy từ Unity 6000.3.29f1 batch mode trên bản sao `D:\EchoVerify` (project gốc đang mở trong Editor).

| Giai đoạn | Nội dung | Trạng thái | Xác minh |
| --- | --- | --- | --- |
| 1. Audit | Đọc GDD, Gameplay Specification, báo cáo giữa kỳ. Kiểm tra package, FBX, importer. **Đo hình học FBX Office** (lưới 0,25 m, ảnh mặt bằng, danh sách đối tượng, ô cửa). | Xong | `ProjectAudit.md`, `OfficeMap/OfficeMapAnalysis.md`, `OfficeMap/OfficeDoorways.md` |
| 2. Editor Automation | Menu Validate / Generate Level / Setup Gameplay / Bake Navigation / Run Validation. Gắn FBX nguyên hierarchy, sinh prefab collision (1.684 MeshCollider từ 6.509 mesh), 14 cửa tương tác từ cánh cửa FBX, tường biên, layout dạng asset, snap marker lên NavMesh, báo cáo đường đi, ảnh NavMesh và ảnh xem trước. | Xong | Validation đạt cho Office (63 tuyến NavMesh) và Blockout Sandbox |
| 3. Nền tảng gameplay | GameManager, GameStateMachine, LevelManager, PlayerController (CharacterController), PlayerInput (Input System), ThirdPersonCamera (Cinemachine 3), InteractionSystem, MissionManager, InventorySystem, TimerSystem, SaveSystem. | Xong | EditMode: timer, trạng thái, save. PlayMode: va chạm tường blockout và tường thật Office |
| 4. Hunter AI | Bốn cấp Patrol/Search/Hunt/Lockdown, thị giác + LoS raycast, thính giác theo sự kiện, vị trí biết cuối, Investigate/Search/Chase/Ambush/Capture/Stunned/Recovery, phát hiện kẹt, **mở cửa đóng trên tuyến đường (3 s)**, **kiểm tra chỗ nấp tương đối > 30 s**. | Xong ở mức Vertical Slice | PlayMode: tuần tra, tìm đường vòng, không nhìn xuyên tường thật, tự mở cửa phòng server |
| 5. Vòng chơi | Sảnh chọn kỹ năng → thu thập → Hunter tăng cấp → Relay (rời vùng giữ tiến độ) → cổng → thắng trước giờ. Bị bắt, mất 2 dữ liệu, hồi sinh, nhặt lại, retry. | Xong | PlayMode chạy cùng một kịch bản trên Office và Sandbox |
| 6. Kỹ năng, vật phẩm, UI | Burst Sprint, Dash, Hunter Sensor, cooldown, EMP, Stun, giáp, Scanner, HUD, cảnh báo Hunter (thêm "BỊ PHÁT HIỆN"), Relay progress, timer, pause, menu, kết quả. | Xong ở mức chức năng | Cân bằng cần playtest trực tiếp |
| 7. Kiểm thử và build | EditMode 17/17, PlayMode 8/8, build Windows x64. | Xem mục Kết quả | `Builds/Validation/` |

## Kết quả kiểm thử gần nhất

- EditMode **17/17 đạt**: ngưỡng cấp Hunter, chính sách cấp, mất dữ liệu khi bị bắt, giáp, Relay chỉ chạy khi đủ dữ liệu và giữ tiến độ khi rời vùng, timer, save/backup, layout Office khớp luật và nằm trong vùng sàn, cửa (delay Hunter 3 s, cửa khóa).
- PlayMode **8/8 đạt**:
  - Sandbox: CharacterController dừng ở tường, Hunter không nhìn xuyên tường, Hunter tuần tra và tìm đường vòng, vòng nhiệm vụ đầy đủ.
  - Office: TAY đứng trên sàn thật và bị vách phòng server chặn, không rơi khỏi mép sàn, Hunter không nhìn xuyên vách thật nhưng thấy qua sàn trống, NavMesh nối Hunter Spawn → Relay/Player Spawn, Hunter dừng mở cửa bắc phòng server (đang đóng) rồi đi vào, vòng nhiệm vụ đầy đủ (bị bắt, hồi sinh, Relay, cổng, retry hết giờ).
- Validation Office: đường NavMesh Player Spawn → Relay 37,8 m, Relay → cổng 43,7 m, xa nhất Player Spawn → dữ liệu phòng họp 78,3 m.
- Windows x64: build thành công với map Office, khởi động 25 giây ở chế độ cửa sổ, Direct3D 12, log "Hunter navigation ready", không có exception, ~720 MB RAM. Bản build đầy đủ ở `Builds/Windows-Office/`. `Builds/Windows/` có một bản build cũ đang chạy (PID 42256, mở lúc 01:39) khóa 10 file nên chưa thay được hoàn toàn; 9/10 file khóa giống hệt bản mới, riêng `lib_burst_generated.dll` khác. Đóng bản cũ rồi chạy lại **Build Windows x64** để đồng nhất.
- Kết quả gốc: `Builds/Validation/EditMode.xml`, `PlayMode.xml`, `WindowsStartup.log`.

## Kiến trúc

- **Dữ liệu**: `LevelRules` (luật), `LevelLayout` (bố trí), `HunterProfile`, `PlayerTuning` là ScriptableObject trong `Settings/`.
- **Runtime**: `GameManager` điều phối `GameStateMachine`, `TimerSystem`, `MissionManager`, `InventorySystem`, `SaveSystem`. `LevelManager` nối tham chiếu scene và phát sự kiện tiếng ồn. `HunterBrain` chỉ biết vị trí TAY qua thị giác có LoS, sự kiện tiếng ồn, tín hiệu Scanner hoặc kiểm tra chỗ nấp lâu (có sai số).
- **Editor**: `OfficeLevelTools` (pipeline theo `LevelTarget`), `OfficeSceneFactory` (prefab, gameplay theo layout), `LevelLayouts` (giá trị khởi tạo đo từ FBX), `OfficeMapAnalysis`, `OfficeDoorwayDiagnostics`, `LevelPreviews`.
- **Scene**: `Level` (NavMeshSurface) → `Environment` (FBX instance, prefab Collision, Boundary) + nhóm gameplay sinh tự động.

## Còn tồn tại

- Nhân vật TAY/Hunter vẫn là hình khối; chưa có rig, animation, âm thanh (chưa có tài nguyên).
- Hunter chưa nhảy qua vật thấp (cần off-mesh link + animation, D14). Smoke Bomb, Decoy, cửa khóa giữ F ngoài phạm vi MVP hiện tại.
- Hiệu năng map: 6.509 renderer, 29 đèn realtime, chưa có static batching/occlusion culling. Cần đo FPS khi playtest.
- Cân bằng (thời gian 12 phút, tốc độ, vị trí dữ liệu) cần playtest trực tiếp.

## Bước tiếp theo

1. Playtest trong Editor, chỉnh layout/luật theo cảm giác chơi.
2. Tối ưu render map (static flags trên instance FBX, occlusion culling, giới hạn bóng đổ đèn).
3. Tích hợp model/rig/animation TAY và Hunter khi có tài nguyên; thêm âm thanh bước chân, Relay, cảnh báo.
4. Dựng Level tiếp theo bằng `LevelTarget` + `LevelLayout` mới.
