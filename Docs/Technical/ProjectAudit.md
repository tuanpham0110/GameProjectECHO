# Audit ban đầu

- `ProjectVersion.txt` xác nhận Unity 6000.3.29f1. Project có URP 17.3.0, Input System 1.20.1, Cinemachine 3.1.7, AI Navigation 2.0.16, Test Framework 1.6.0 và uGUI 2.0.0 trong manifest và cache. Input System đang là input backend. Các package nền bắt buộc đã có, Windows Standalone Support hiện diện trong editor. Scene duy nhất trong danh sách build ban đầu là `SampleScene`.
- Nguồn gameplay là `GDD - PROJECT ECHO.pdf` (53 trang) và `GAMEPLAY SPECIFICATION.pdf` (51 trang). `[Game] Báo cáo Giữa kỳ.pdf` (125 trang) có phần so sánh hai game ở đầu và phần Project ECHO bắt đầu ở trang 43. Đã trích xuất văn bản cả ba và kiểm tra các trang chỉ có ảnh có dữ liệu raster. Specification tr. 47 có sơ đồ bom/bẫy/cửa. Các ảnh GDD tương ứng là mẫu tài liệu, còn ảnh báo cáo là lịch sản xuất.
- `Map office (Tong hop).fbx` là FBX nhị phân 7400, 44.556.572 byte, Y-up và đơn vị khai báo tương đương mét. Duyệt `Objects` và liên kết `OO` thấy 6.509 node Mesh, 384 nhóm Null, 29 đèn, 896 model root, 3.348 Geometry Mesh, 595 Material, 12 Texture, 10 Video Clip và 2 AnimationStack. Tên node lẫn `floor`, `Wall`, `Door handle` và nhiều `01_Material_*`. Cần kiểm tra hình học để xác nhận chức năng và kiểm tra importer để biết animation tác động đối tượng nào. Chưa tìm thấy deformer, tài nguyên nhân vật riêng, rig, animation TAY/Hunter hoặc texture rời trong Office.
- Unity import được 6.509 MeshRenderer, 29 Light và 4 AnimationClip. Importer cảnh báo hai clip tên `KEY | Front03…` có độ dài 0 frame. Bounds tổng có tâm X=5,05 m, Y=2,04 m, Z=−16,65 m và kích thước khoảng 59,82 × 5,56 × 93,02 m. Mesh tên `Wall` rộng khoảng 57 × 72 m trên mặt phẳng ngang, còn các mesh tên `floor` được lấy mẫu rộng chỉ 0,5 × 2,6 m. Bố trí đường đi cần dựa vào hình học kiểm tra trực tiếp.
- `Temp/UnityLockfile` và các process editor/worker xác nhận project gốc đang mở trong lúc audit. Batch mode chỉ chạy trên project kiểm chứng riêng. Scene gốc, FBX và project settings hiện có được giữ nguyên trừ việc đăng ký scene build theo lệnh Editor có chủ đích.

| Phân loại | Yêu cầu |
| --- | --- |
| Đã chốt | Một Hunter bốn cấp, phát hiện có nguồn, nạp đủ một lần, tạm dừng khi rời vùng Relay, thoát trước hết giờ, giữ đồng hồ khi bắt, giữ tối đa một kỹ năng mỗi lượt. |
| Chưa rõ | Mốc mở khóa Arc, thời gian hồi phục/miễn bắt, thông số phụ của kỹ năng, phạm vi tương tác, cách dùng hình học Office, ánh sáng và hành vi cửa/vật cản đặc biệt. |
| Mâu thuẫn | Giữ/hạ cấp Hunter, giữ/đóng cổng đã mở, giới hạn level, nút vật phẩm và thời lượng EMP. |
| Thiếu tài nguyên | Nhân vật, rig/animation dùng được, âm thanh, ánh xạ sàn/vách/cửa/điểm tương tác đáng tin cậy cho Office và layout nhiệm vụ đã duyệt. |

## Audit bổ sung 2026-11-22 · hình học Office đo trực tiếp

Đo bằng **Tools → Project ECHO → Analyze Office Map** (raycast + overlap trên lưới 0,25 m với MeshCollider tạm trong preview scene). Kết quả đầy đủ ở `OfficeMap/`.

- Một tầng. Sàn "Sàn nhà" X −24,65…34,97, Z −59,06…9,64, mặt sàn Y = 0. Mái "Mái" cao 4,6–4,8 m. Không có cầu thang hay tầng lửng.
- Khối nhà kín, có tường bao riêng. Phần sàn ngoài tường bao không có cửa nối vào và không có tường chắn mép sàn.
- Chức năng phòng nhận biết qua tên và hình: sảnh lễ tân (logo AETHER, sofa), phòng họp, khu bàn làm việc, văn phòng mở, phòng hồ sơ, phòng server (rack, đèn ServerRed), các phòng kỹ thuật phía nam (máy phát, làm mát, bồn).
- 14 ô cửa, tất cả đang đóng: 2 cửa đơn "Door handle.002/.004" và 12 cửa đôi ghép từ cặp mesh `Cube.0xx` (mỗi cánh 0,79 × 2,35 m). Biển EXIT treo trên các ô cửa. Hai clip animation trong FBX dài 1 s nhưng không có curve, không có animation cửa.
- Tủ trắng, "Tu ke" có khoang trong (sàn, trần, vách). "Tall battered locker" có cánh tủ 1,08 × 2,2 m.
- Collider sinh tự động: 1.684 MeshCollider trên tổng 6.509 mesh (bỏ mesh < 0,3 m, vật treo trên 2,3 m trừ mái, dải phát sáng, cánh cửa).
- NavMesh (agent r 0,4 m, bậc 0,3 m) liên thông toàn bộ nội thất khi các ô cửa được xử lý như cửa tương tác. Ảnh: `OfficeMap/navmesh_Office_Level01.png`.

| Phân loại | Cập nhật |
| --- | --- |
| Đã giải quyết | Cách dùng hình học Office, vị trí cửa, vùng đi được, ranh giới map. |
| Quyết định tạm (D11–D16) | Vị trí Relay/cổng/dữ liệu, trạng thái đầu của cửa, thời gian kiểm tra chỗ nấp, thông số agent. |
| Thiếu tài nguyên | Rig/animation/âm thanh TAY và Hunter; animation mở cửa (đang xoay cánh bằng code). |

