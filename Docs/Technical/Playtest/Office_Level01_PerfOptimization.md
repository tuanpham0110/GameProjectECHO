# Office_Level01 · Tối ưu render (2026-11-22)

Mốc: `Office_Level01_Perf.md` (sáng cùng ngày). Máy đo: GTX 1650 (laptop), i5-10300H, 1920×1080 cửa sổ, vSync tắt, chất lượng PC. Lệnh đo: `ProjectECHO.exe -echoPerf` (xem `EditorWorkflow.md`). Số liệu thô của từng vòng nằm trong `Perf/` và `Builds/Validation/Perf/Rounds/`.

## Kết quả

Ba góc ưu tiên. Mỗi ô ghi frame time trung bình / p95 / tối đa (ms) và số batch.

| Góc | Mốc r0 (D3D12) | Bản cuối r3 | Thay đổi p95 |
| --- | --- | --- | --- |
| Data Relay nhìn tây bắc | 13,3 / 21,8 / 30,2 · 11.371 batch | **5,9 / 6,7 / 8,0** · 3.258 batch | −69 % |
| Sảnh lễ tân nhìn bắc | 11,4 / 18,7 / 27,8 · 9.577 batch | **5,2 / 6,9 / 10,5** · 3.048 batch | −63 % |
| Phòng server nhìn tây | 11,7 / 22,4 / 29,4 · 6.172 batch | **5,3 / 5,9 / 7,2** · 1.843 batch | −74 % |

| Đo | Mốc r0 | Bản cuối r3 |
| --- | --- | --- |
| Sảnh, gộp 4 hướng | 172 FPS, p95 14,0 ms | 232 FPS, p95 6,3 ms |
| Phòng server, gộp 4 hướng | 141 FPS, p95 16,2 ms | 209 FPS, p95 6,2 ms |
| Relay, gộp 4 hướng | 164 FPS, p95 15,1 ms | 228 FPS, p95 6,1 ms |
| Vòng chạy 60 s có Hunter (Lockdown, bị đuổi và bắt) | 6,2 ms TB, p95 11,1, p99 14,1, tối đa 26,8; 7 khung > 25 ms | 4,5 ms TB, p95 6,5, p99 7,3, tối đa 17,6; 0 khung > 25 ms |

### Hết tuyến và giữ Lockdown (r5, `Perf/r5_het_tuyen.md`)

Vòng 60 s ở r3 chỉ tới 3/8 điểm. Lần nào bị bắt, TAY cũng về Player Spawn và mất 2 gói, nên Hunter hạ cấp. Vì thế r3 chưa đo được hiệu năng trên cả tuyến, cũng chưa đo được khi Lockdown kéo dài. Probe giờ có thêm một phần đo:
- Đi 2 vòng qua 8 điểm.
- Hunter vẫn nhìn, nghe, đuổi và áp sát, nhưng việc bị bắt bị tắt (`GameManager.captureSuppressed`, chỉ probe đặt). TAY giữ 6/6 gói.

| Đo (bản cuối, D3D11 + GRD + SSAO Depth) | Kết quả |
| --- | --- |
| Hết tuyến, 2 vòng (16/16 chặng, 359 m, 72 s, mở 2 cửa) | 3,9 ms TB, p95 6,3, p99 7,1, tối đa 13,1 ms; **0 khung > 25 ms** |
| Cấp Hunter trong lúc đo | Lockdown (4) suốt lượt, Hunter đuổi 17 lần, áp sát trong tầm bắt 2 lần |
| Chặng nặng nhất theo p95 | Relay → (5; −41) và (15; −9) → (22; −20): p95 6,6–6,7 ms. Chặng nào cũng có p95 ≤ 6,7 ms và tối đa ≤ 13,1 ms |
| 60 s theo luật thật (bị bắt 2 lần, tới 8/8 điểm) | 4,2 ms TB, p95 6,5, p99 7,1; 1 khung 26,4 ms (CPU main 17 ms, render 11,6 ms) |

Giới hạn của phần đo này:
- Bot chạy nhanh hơn Hunter ở Lockdown, nên Hunter chỉ ở trạng thái Chase 19 % thời gian, nhiều nhất ở chặng (10; −1) → (15; −9) (68 %).
- Khung 26,4 ms ở phần luật thật có CPU main cao, nhiều khả năng rơi vào lúc bị bắt và hồi sinh (dời TAY, đồng bộ dữ liệu). Probe chưa ghi thời điểm của khung này nên chưa xác định chắc.
- Ở r5, góc tĩnh Relay tây bắc đo được 6,4 / 9,0 ms, so với 6,7 ms p95 ở r3. r5 chạy trong cùng điều kiện màn hình với r4 (6,8 / 9,5 ms), nên chênh lệch nằm trong mức dao động giữa hai cấu hình màn hình đã ghi ở dưới.

Kiểm tra lại sau khi máy cắm thêm màn hình ngoài (r4, game mở trên màn hình chính 1080p): Relay tây bắc 6,8 / 9,5 ms, vòng chạy p99 7,3 ms, có 2 khung > 25 ms. Cả hai khung này có CPU main khoảng 2,2 ms và không ghi nhận GPU, tức là thời gian chờ nằm ngoài engine (present/compositor hai màn hình). Các số r0 và r3 được đo trong cùng điều kiện một màn hình.

## Cách xác định điểm nghẽn

Không mở được Profiler/Frame Debugger bằng giao diện trong phiên này. Tôi dùng các công cụ thay thế chạy được trong bản build:

1. **Audit tĩnh** (`Tools → Project ECHO → Audit Rendering`, kết quả ở `Office_Level01_RenderAudit.md`): 6.545 MeshRenderer, 6.539 đổ bóng, 599 material, không static flag, không occlusion data. 2.175 renderer `KEY` và 769 `Keyboard` là phím bàn phím. Chỉ 4 đèn đổ bóng (3 point, 1 spot, tầm 10 m); 25 đèn Rectangle realtime không được URP hỗ trợ nên bị bỏ qua.
2. **FrameTimingManager** trong probe tách CPU main, CPU render thread và GPU cho từng khung. ProfilerRecorder đếm batch, SetPass, tam giác, shadow caster.
3. **Tắt thử từng yếu tố** trên cùng một bản build (`-echoPerfVariant`). Số đo ở Relay tây bắc:

| Biến thể | TB / p95 (ms) | CPU main | GPU | Batch | Kết luận |
| --- | --- | --- | --- | --- | --- |
| Mốc | 13,3 / 21,8 | 7,8 | 5,3 | 11.371 | Frame time ≈ CPU main + GPU, nghĩa là CPU và GPU **không chạy song song** |
| Tắt mọi bóng đèn | 12,8 / 21,5 | 7,3 | 5,2 | 11.356 | Bóng đèn không phải điểm nghẽn |
| Tắt SSAO | 10,2 / 16,6 | 6,8 | 2,4 | 5.708 | Prepass DepthNormals của SSAO vẽ lại cả scene: nhân đôi batch, tốn khoảng 3 ms GPU |
| Vật < 0,35 m không đổ bóng | 13,5 / 22,4 | 7,9 | 5,2 | 11.444 | Không lợi |
| Ẩn 4.624 vật < 0,35 m | 9,2 / 18,6 | 3,7 | 4,3 | 3.692 | Vật nhỏ chiếm khoảng một nửa CPU main |

## Các thay đổi đã thử (mỗi lần một thay đổi)

| Bước | Relay tây bắc TB / p95 | Quyết định |
| --- | --- | --- |
| GPU Resident Drawer (GRD) trên D3D12 | 12,6 / 25,8 | Giảm CPU main nhưng p95 tệ hơn và các góc nhẹ chậm đi. Chưa đủ để giữ riêng. |
| GRD + GPU occlusion culling (D3D12) | 11,8 / 20,0 | Tam giác giảm từ 3,4 triệu xuống 7.700, nhưng frame time vẫn cao vì CPU và GPU vẫn chạy tuần tự |
| Cùng bản build, ép Direct3D 11 (`-force-d3d11`) | 7,1 / 8,1 | **Nguyên nhân chính**: trên D3D11, CPU và GPU chạy song song, frame time bằng phần lớn hơn thay vì tổng |
| D3D11, tắt GRD | 10,8 / 12,3 | D3D11 một mình đã xóa phần lớn p95; GRD giảm thêm CPU main ở góc nhiều vật (7,3 → 2,8 ms) |
| D3D11 + SSAO nguồn Depth, AfterOpaque (không GRD) | 9,0 / 10,0 | Bỏ prepass, batch giảm một nửa |
| GRD + occlusion + SSAO Depth + cắt mesh < 0,4 % màn hình (D3D11) | 6,4 / 7,7 | Chỉ hơn bản cuối khoảng 0,5 ms nhưng có rủi ro mất vật thể, có spike 29 ms ở sảnh. **Không dùng.** |
| **Bản cuối: D3D11 + GRD + SSAO Depth/AfterOpaque** | **5,9 / 6,7** | Giữ |

Không thử static batching hay occlusion Umbra. Sau thay đổi trên, CPU main ở góc nặng nhất chỉ còn 2,4 ms, và phần còn lại là GPU (5,5 ms, 1,9 triệu tam giác). Cả hai cách này đều cần đặt static flag cho 6.545 node FBX (tức là hàng nghìn override trong scene) và có rủi ro với cửa động. Nên để dành cho lượt sau nếu cần giảm GPU.

## Thay đổi trong project

- `ProjectSettings/ProjectSettings.asset`: Windows dùng **chỉ Direct3D 11** (`m_BuildTargetGraphicsAPIs`). Bật `enableFrameTimingStats` để probe đọc CPU/GPU, chi phí không đáng kể.
- `ProjectSettings/GraphicsSettings.asset`: BatchRendererGroup variants = Keep All (`m_BrgStripping: 2`), bắt buộc để dùng GRD.
- `Assets/Settings/PC_RPAsset.asset`: GPU Resident Drawer = Instanced Drawing. GPU occlusion và small-mesh culling giữ tắt.
- `Assets/Settings/PC_Renderer.asset`: SSAO `Source = Depth`, `AfterOpaque = true` (cường độ, bán kính, mẫu không đổi).
- `Runtime/PerformanceProbe.cs`: thêm CPU/GPU, biến thể tắt thử, vòng chạy có Hunter, ảnh kiểm tra hiển thị, khóa input trong lúc đo. `Runtime/ThirdPersonCamera.cs`: thêm `SetYaw` (internal) cho probe.
- `Editor/RenderAudit.cs`: menu Audit Rendering.

Scene, layout, NavMesh, đèn và vật liệu không đổi.

## Kiểm tra sau thay đổi

- Hiển thị (ảnh từ bản build cuối):
  - Màn hình Relay vẫn đổi màu qua MaterialPropertyBlock (`Perf/check_relay_mpb.png`).
  - Cửa FBX xoay khi mở (`Perf/check_door_open.png`, so với ảnh cửa đóng trong `Builds/Validation/Perf/08_door_closed.png`).
  - Cột tín hiệu và lời nhắc của gói dữ liệu vẫn hiện; cổng khóa và logo AETHER hiển thị bình thường.
  - SSAO nguồn Depth gần như không khác bằng mắt ở sảnh.
- Run Validation: đạt (NavMesh không đổi). EditMode 20/20. PlayMode 10/10, trong đó bot playtest hoàn tất (312 s, bị bắt 4 lần, kẹt 0).
- Sau khi thêm phần đo hết tuyến (r5): Validation đạt, EditMode 20/20, PlayMode 10/10 (118 s; bot hoàn tất sau 148 s, bị bắt 1 lần, kẹt 0). Zip đã được đóng gói lại từ bản build này. Chạy thử từ bản giải nén: Direct3D 11, "Hunter navigation ready".

## Lưu ý

- Lần mở Unity Editor tiếp theo sẽ chuyển sang D3D11 và có thể yêu cầu khởi động lại Editor.
- Máy khác có thể cho kết quả D3D12 khác. Nếu sau này cần D3D12 (ray tracing, v.v.), hãy đo lại bằng cùng lệnh `-echoPerf`.
- URP vẫn cảnh báo "Reduced additional punctual light shadows resolution… 2048 atlas". Không ảnh hưởng frame time (xem dòng "Tắt mọi bóng đèn").
