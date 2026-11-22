# Đo hiệu năng Office_Level01

- Thời điểm: 2026-11-22 17:56
- Biến thể: không (cấu hình phát hành)
- Màn hình: 1920×1080, cửa sổ, vSync tắt
- GPU: NVIDIA GeForce GTX 1650 (Direct3D11, 3962 MB)
- CPU: Intel(R) Core(TM) i5-10300H CPU @ 2.50GHz (8 luồng), RAM 16177 MB
- Chất lượng: PC, FrameTimingManager: bật, GPU Resident Drawer: InstancedDrawing
- Renderer bật: 6545, đổ bóng: 6539

## Góc nhìn cố định (Hunter đứng yên)

| Vị trí | Hướng | FPS | TB (ms) | p95 (ms) | Tối đa (ms) | CPU main | CPU render | GPU | Batches | SetPass | Tam giác | Shadow casters |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Sảnh lễ tân (Player Spawn) | Bắc | 222 | 4.5 | 6.2 | 9.7 | 2.5 | 2.3 | 4.6 | 3,048 | 23 | 1,393,128 | 135 |
| Sảnh lễ tân (Player Spawn) | Đông | 258 | 3.9 | 5.7 | 29.7 | 2.0 | 0.7 | 3.8 | 1,235 | 29 | 1,024,535 | 764 |
| Sảnh lễ tân (Player Spawn) | Nam | 280 | 3.6 | 5.4 | 6.8 | 1.2 | 0.3 | 3.5 | 196 | 17 | 126,701 | 125 |
| Sảnh lễ tân (Player Spawn) | Tây | 294 | 3.4 | 5.1 | 6.6 | 1.2 | 0.3 | 3.3 | 158 | 16 | 115,667 | 111 |
| Phòng server (giữa hai dãy rack) | Bắc | 161 | 6.2 | 7.9 | 22.5 | 1.5 | 0.8 | 6.2 | 810 | 13 | 1,050,442 | n/a |
| Phòng server (giữa hai dãy rack) | Đông | 266 | 3.8 | 5.4 | 31.0 | 1.0 | 0.2 | 3.7 | 43 | 10 | 48,063 | n/a |
| Phòng server (giữa hai dãy rack) | Nam | 240 | 4.2 | 5.4 | 7.6 | 1.5 | 0.6 | 4.1 | 896 | 23 | 588,481 | 597 |
| Phòng server (giữa hai dãy rack) | Tây | 189 | 5.3 | 7.1 | 16.0 | 2.2 | 1.7 | 5.2 | 1,843 | 27 | 1,255,645 | 206 |
| Data Relay (đủ dữ liệu) | Đông nam | 262 | 3.8 | 5.5 | 8.3 | 1.2 | 0.3 | 3.8 | 225 | 17 | 169,839 | 153 |
| Data Relay (đủ dữ liệu) | Tây nam | 243 | 4.1 | 5.6 | 6.8 | 1.9 | 0.7 | 4.1 | 1,100 | 29 | 855,619 | 713 |
| Data Relay (đủ dữ liệu) | Tây bắc | 155 | 6.4 | 9.0 | 22.6 | 2.5 | 2.3 | 6.4 | 3,258 | 17 | 1,906,086 | 14 |
| Data Relay (đủ dữ liệu) | Đông bắc | 285 | 3.5 | 5.2 | 6.4 | 1.0 | 0.2 | 3.4 | 54 | 11 | 66,037 | n/a |

- Sảnh lễ tân (Player Spawn) (4 hướng): 264 FPS, 3.8 ms TB, p95 6.0 ms, tối đa 29.7 ms; CPU main 1.7, render 0.8, GPU 3.7 ms; batches TB 1,041.
- Phòng server (giữa hai dãy rack) (4 hướng): 214 FPS, 4.7 ms TB, p95 7.5 ms, tối đa 31.0 ms; CPU main 1.5, render 0.8, GPU 4.6 ms; batches TB 824.
- Data Relay (đủ dữ liệu) (4 hướng): 236 FPS, 4.2 ms TB, p95 7.2 ms, tối đa 22.6 ms; CPU main 1.5, render 0.7, GPU 4.2 ms; batches TB 896.

## Hết tuyến 2 vòng, giữ Lockdown, Hunter đuổi nhưng không bắt

- Hết tuyến: 255 FPS, 3.9 ms TB, p95 6.3 ms, tối đa 13.1 ms; CPU main 1.8, render 0.8, GPU 3.8 ms; batches TB 863.
- 18382 khung trong 72 s; khung > 25 ms: 0, > 33 ms: 0, > 50 ms: 0; p99 7.1 ms. Quãng đường 359 m, tới 16/16 điểm, Hunter đuổi 17 lần, bị bắt 0 lần, mở 2 cửa.
- 5 khung nặng nhất: 13.1 ms (main 6.0, render 1.5, GPU 6.3); 11.6 ms (main 1.5, render 0.4, GPU 0.0); 10.9 ms (main 2.0, render 0.6, GPU 6.2); 10.6 ms (main 6.8, render 1.7, GPU 7.5); 10.0 ms (main 1.9, render 0.6, GPU 6.3).
- Cấp Hunter thấp nhất trong lúc đo: 4 (Lockdown suốt lượt); Hunter ở trạng thái Chase 19% thời gian; áp sát trong tầm bắt 2 lần (không bắt); bỏ qua 0 điểm do kẹt.

| Chặng | Từ → tới | Thời gian (s) | TB (ms) | p95 (ms) | Tối đa (ms) | Khung > 25 ms | GPU TB | Batches TB | Hunter cách TB (m) | Hunter Chase |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | (23; -27) → (5; -41) | 4.3 | 4.1 | 6.7 | 9.9 | 0 | 4.0 | 832 | 26.3 | 0% |
| 2 | (5; -41) → (-12; -38) | 3.4 | 3.4 | 5.0 | 8.8 | 0 | 3.3 | 296 | 35.3 | 0% |
| 3 | (-12; -38) → (-11; -12) | 12.0 | 3.8 | 6.0 | 11.6 | 0 | 3.7 | 1,019 | 11.1 | 24% |
| 4 | (-11; -12) → (-9.5; 5) | 5.3 | 4.0 | 6.2 | 7.6 | 0 | 4.0 | 900 | 14.3 | 0% |
| 5 | (-9.5; 5) → (10; -1) | 5.1 | 4.3 | 6.6 | 8.5 | 0 | 4.2 | 1,171 | 8.1 | 18% |
| 6 | (10; -1) → (15; -9) | 1.9 | 3.8 | 5.8 | 7.3 | 0 | 3.7 | 985 | 5.1 | 68% |
| 7 | (15; -9) → (22; -20) | 2.8 | 4.1 | 6.6 | 7.5 | 0 | 4.0 | 455 | 6.0 | 13% |
| 8 | (22; -20) → (23; -27) | 1.3 | 4.2 | 6.1 | 7.7 | 0 | 4.1 | 696 | 10.0 | 0% |
| 9 | (23; -27) → (5; -41) | 4.4 | 4.0 | 6.4 | 13.1 | 0 | 3.9 | 768 | 14.1 | 0% |
| 10 | (5; -41) → (-12; -38) | 3.4 | 3.4 | 5.1 | 7.3 | 0 | 3.3 | 296 | 21.0 | 0% |
| 11 | (-12; -38) → (-11; -12) | 12.0 | 3.8 | 6.1 | 8.4 | 0 | 3.7 | 1,013 | 7.4 | 35% |
| 12 | (-11; -12) → (-9.5; 5) | 5.3 | 4.1 | 6.3 | 7.5 | 0 | 4.0 | 897 | 15.2 | 0% |
| 13 | (-9.5; 5) → (10; -1) | 5.1 | 4.2 | 6.4 | 7.6 | 0 | 4.1 | 1,184 | 6.7 | 32% |
| 14 | (10; -1) → (15; -9) | 1.9 | 3.8 | 5.8 | 7.1 | 0 | 3.7 | 986 | 3.7 | 68% |
| 15 | (15; -9) → (22; -20) | 2.8 | 4.1 | 6.7 | 7.6 | 0 | 4.1 | 456 | 4.6 | 35% |
| 16 | (22; -20) → (23; -27) | 1.3 | 4.2 | 6.2 | 7.7 | 0 | 4.1 | 695 | 6.2 | 0% |

## Chạy 60 s theo luật thật (có bắt, hồi sinh, mất dữ liệu)

- Vòng chạy: 240 FPS, 4.2 ms TB, p95 6.5 ms, tối đa 26.4 ms; CPU main 2.2, render 1.3, GPU 4.1 ms; batches TB 1,440.
- 14395 khung trong 60 s; khung > 25 ms: 1, > 33 ms: 0, > 50 ms: 0; p99 7.1 ms. Quãng đường 185 m, tới 8/8 điểm, Hunter đuổi 8 lần, bị bắt 2 lần, mở 0 cửa.
- 5 khung nặng nhất: 26.4 ms (main 17.0, render 11.6, GPU 5.6); 21.2 ms (main 2.4, render 0.8, GPU 0.0); 17.6 ms (main 11.4, render 4.0, GPU 9.4); 14.4 ms (main 4.4, render 4.3, GPU 17.7); 14.0 ms (main 3.6, render 3.5, GPU 4.4).
