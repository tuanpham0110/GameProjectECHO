# Office Level 01 · quy trình Editor

## Mở và chơi

Mở `Assets/_ProjectECHO/Scenes/Office_Level01.unity` rồi nhấn Play. Nếu scene chưa có map Office (vẫn là blockout cũ), chạy **Tools → Project ECHO → Build Vertical Slice** một lần. Menu này gắn FBX Office, sinh collider, cửa, tường biên, đặt gameplay theo layout, bake NavMesh, kiểm tra đường đi và lưu scene.

Chọn **Chuẩn bị nhiệm vụ**, chọn **Burst Sprint**, xác nhận rồi bắt đầu. Xuất phát ở sảnh lễ tân (logo AETHER). Nhặt đủ sáu gói dữ liệu bằng F, đến Data Relay ở phía nam phòng server, nhấn F và ở trong vùng tải. Khi cổng mở, quay về cổng cạnh logo AETHER và đi qua cổng.

Dòng mục tiêu ở góc trên trái cho biết chặng hiện tại (1/3, 2/3, 3/3). Ở chặng 2–3, bảng dưới màn hình chỉ đường NavMesh tới Relay hoặc cổng. Ở chặng 1, bật Scanner (T) để xem khoảng cách và hướng của tín hiệu dữ liệu gần nhất. Mô tả vị trí Relay và cổng lấy từ `relayLocation`/`exitLocation` trong `Office_Level01_Rules`.

| Phím | Thao tác |
| --- | --- |
| WASD, chuột | Di chuyển, xoay camera |
| Shift, Ctrl, Space | Chạy, giữ để cúi, nhảy |
| F | Tương tác: nhặt, mở/đóng cửa, Relay, cổng |
| C | Vào/ra chỗ nấp được đánh dấu |
| E, T | Dùng kỹ năng đã trang bị, bật/tắt Scanner |
| 1/2, G hoặc Q | Chọn EMP/Stun, dùng vật phẩm đã chọn |
| Esc | Tạm dừng hoặc tiếp tục |

## Menu Tools → Project ECHO

| Menu | Việc làm | Chạy lại an toàn |
| --- | --- | --- |
| Build Vertical Slice | Generate Level, Setup Gameplay, Bake Navigation, Run Validation, đăng ký scene build. | Có |
| Build Blockout Sandbox | Dựng `Scenes/Sandbox/Blockout_Sandbox.unity` (blockout có hình học biết trước, dùng cho test hồi quy). | Có |
| Validate Project | Tạo thư mục, kiểm tra FBX, URP, layout khớp LevelRules, cấu hình agent NavMesh. | Có |
| Generate Level | Gắn FBX nguyên hierarchy dưới `Level/Environment`, sinh `Generated/Office_Level01_Collision.prefab`, tường biên, ánh sáng nội thất. Scene đời cũ được sao lưu vào `Backups/` trước khi gỡ nhóm blockout. | Có, prefab collision được sinh lại |
| Setup Gameplay | Xóa và dựng lại `Level systems`, `Authoring markers`, `Gameplay objects` theo `Settings/Office_Level01_Layout.asset`. | Có |
| Bake Navigation | Đặt agent (r 0,4 m, cao 1,8 m, bậc 0,3 m), bake NavMesh, đặt marker lên NavMesh gần nhất. | Có |
| Run Validation | Kiểm tra tham chiếu, ID dữ liệu, spawn không chồng collider, đường NavMesh từ Player Spawn và Hunter Spawn tới mọi mục tiêu, Relay → cổng. Ghi `Docs/Technical/OfficeMap/Office_Level01_Validation.md` và ảnh `navmesh_Office_Level01.png`. | Có |
| Capture Level Previews | Chụp ảnh từ spawn, Relay, cổng, Hunter, dữ liệu và ảnh tổng quan vào `Docs/Technical/OfficeMap/`. | Có |
| Analyze Office Map | Đo FBX trong preview scene riêng, xuất lưới đi được, ảnh mặt bằng, danh sách đối tượng có tọa độ. | Có |
| Diagnose Doorways | Đo các ô cửa dưới biển EXIT và liệt kê collider chặn. | Có |
| Review Office Geometry / Create Colliders For Selected | Công cụ rà soát mesh của phiên trước. | Có |
| Build Windows x64 | Kiểm tra scene, bật prebake collision mesh và xuất `Builds/Windows/ProjectECHO.exe`. | Có |

## Chỉnh layout

Mọi vị trí gameplay nằm trong `Assets/_ProjectECHO/Settings/Office_Level01_Layout.asset`: spawn, Hunter, Relay, cổng, dữ liệu (ID, tên, vị trí), vật phẩm, waypoint, chỗ nấp, cửa (đường dẫn cánh cửa trong FBX, trạng thái đầu, khóa) và vùng sàn. Sau khi sửa, chạy **Setup Gameplay**, **Bake Navigation**, **Run Validation**. Tọa độ không cần chính xác tới từng cm: Bake đặt marker lên NavMesh trong bán kính 2 m.

Luật level (`Office_Level01_Rules`), thông số TAY (`TAY_Tuning`) và Hunter (`Hunter_Office_Profile`) nằm cùng thư mục và không bị công cụ ghi đè.

## Thêm Level mới

1. Đặt FBX vào `Assets/_ProjectECHO/Art/Environments/<Tên>/`, chạy **Analyze Office Map** (đổi `modelFolder` trong layout) để lấy số đo.
2. Tạo LevelRules và LevelLayout mới (Create → Project ECHO).
3. Khai báo thêm một `LevelTarget` trong `OfficeLevelTools` (scene, rules, layout, hàm giá trị mặc định) và menu tương ứng.

## Batch mode

Không chạy batch trên project đang mở trong Editor. Phiên này dùng bản sao ở đường dẫn ASCII `D:\EchoVerify` (đồng bộ `Assets` bằng `robocopy /MIR`), với các điểm vào:

```
Unity.exe -batchmode -projectPath D:\EchoVerify -executeMethod ProjectEcho.Editor.OfficeLevelTools.BuildAllForVerification -quit
Unity.exe -batchmode -projectPath D:\EchoVerify -runTests -testPlatform EditMode -testResults EditMode.xml
Unity.exe -batchmode -projectPath D:\EchoVerify -runTests -testPlatform PlayMode -testResults PlayMode.xml
Unity.exe -batchmode -projectPath D:\EchoVerify -executeMethod ProjectEcho.Editor.OfficeLevelTools.BuildWindows -quit
```

Playtest tự động bằng input thật (bàn phím/chuột ảo, Hunter thật, ghi `Builds/Validation/Playtest_Office.md`):

```
Unity.exe -batchmode -projectPath D:\EchoVerify -runTests -testPlatform PlayMode -testFilter OfficePlaythroughTests -testResults Playtest.xml
Unity.exe -batchmode -projectPath D:\EchoVerify -runTests -testPlatform PlayMode -testFilter Evasion -testResults Evasion.xml
```

Đo hiệu năng trong bản build. Probe đo sảnh, phòng server, Relay theo 4 hướng camera rồi chạy một vòng 60 s có Hunter; ghi CPU main/render/GPU, chụp UI và ảnh kiểm tra hiển thị; tự thoát sau khoảng 2 phút:

```
Builds\Windows\ProjectECHO.exe -monitor 1 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -echoPerf -echoPerfOut Builds\Validation\Perf\perf.md
```

Một lượt đo mất khoảng 4 phút và gồm ba phần:
- 3 vị trí × 4 hướng, Hunter đứng yên.
- Hai vòng qua 8 điểm, giữ Lockdown. Hunter đuổi nhưng không bắt (`GameManager.captureSuppressed`, chỉ probe dùng). Có số đo theo từng chặng.
- 60 s chạy theo luật thật, có bắt và hồi sinh.

`-monitor 1` giữ game trên màn hình chính. Nếu mở trên màn hình có DPI scale, cửa sổ 1080p sẽ bị phóng to (ví dụ thành 2402×1352) và số đo không so được với mốc. Thêm `-echoPerfVariant noshadows,nossao,nosmallshadows,hidesmall,gpuocclusion,ssaodepth,smallmesh=0.4` (chọn một hoặc nhiều) để tắt thử từng yếu tố khi profile. **Tools → Project ECHO → Audit Rendering** liệt kê đèn, bóng, renderer và static flag vào `Docs/Technical/Playtest/Office_Level01_RenderAudit.md`.

Gói gửi tester: nén `Builds/Windows` (bỏ `ProjectECHO_BurstDebugInformation_DoNotShip`) kèm `Docs/Technical/Playtest/Checklist_NguoiChoiMoi.md`.

Lưu ý: một Unity batch khởi động khi Editor khác đang mở có thể tắt tiến trình IL Post Processing runner dùng chung. Editor đang mở sẽ tự khởi động lại runner khi biên dịch.
