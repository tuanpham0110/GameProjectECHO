# Đo hiệu năng Office_Level01

- Thời điểm: 2026-11-22 15:14
- Biến thể: không (cấu hình phát hành)
- Màn hình: 1920×1080, cửa sổ, vSync tắt
- GPU: NVIDIA GeForce GTX 1650 (Direct3D11, 3962 MB)
- CPU: Intel(R) Core(TM) i5-10300H CPU @ 2.50GHz (8 luồng), RAM 16177 MB
- Chất lượng: PC, FrameTimingManager: bật, GPU Resident Drawer: InstancedDrawing
- Renderer bật: 6545, đổ bóng: 6539

## Góc nhìn cố định (Hunter đứng yên)

| Vị trí | Hướng | FPS | TB (ms) | p95 (ms) | Tối đa (ms) | CPU main | CPU render | GPU | Batches | SetPass | Tam giác | Shadow casters |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Sảnh lễ tân (Player Spawn) | Bắc | 215 | 4.7 | 6.3 | 8.5 | 2.6 | 2.4 | 4.7 | 3,048 | 23 | 1,393,128 | 135 |
| Sảnh lễ tân (Player Spawn) | Đông | 240 | 4.2 | 6.5 | 11.4 | 2.1 | 0.8 | 4.0 | 1,235 | 29 | 1,024,535 | 764 |
| Sảnh lễ tân (Player Spawn) | Nam | 262 | 3.8 | 6.4 | 8.0 | 1.2 | 0.3 | 3.8 | 196 | 17 | 126,701 | 125 |
| Sảnh lễ tân (Player Spawn) | Tây | 272 | 3.7 | 5.8 | 7.2 | 1.2 | 0.3 | 3.6 | 158 | 16 | 115,667 | 111 |
| Phòng server (giữa hai dãy rack) | Bắc | 154 | 6.5 | 8.1 | 10.5 | 1.4 | 0.8 | 6.5 | 810 | 13 | 1,050,442 | n/a |
| Phòng server (giữa hai dãy rack) | Đông | 257 | 3.9 | 6.6 | 8.0 | 1.2 | 0.3 | 3.8 | 43 | 10 | 48,063 | n/a |
| Phòng server (giữa hai dãy rack) | Nam | 232 | 4.3 | 6.2 | 8.0 | 1.6 | 0.6 | 4.3 | 896 | 23 | 588,481 | 597 |
| Phòng server (giữa hai dãy rack) | Tây | 181 | 5.5 | 7.4 | 13.4 | 2.3 | 1.8 | 5.5 | 1,843 | 27 | 1,255,645 | 206 |
| Data Relay (đủ dữ liệu) | Đông nam | 252 | 4.0 | 5.9 | 7.3 | 1.3 | 0.3 | 3.9 | 225 | 17 | 169,839 | 153 |
| Data Relay (đủ dữ liệu) | Tây nam | 238 | 4.2 | 6.0 | 7.5 | 1.9 | 0.7 | 4.2 | 1,100 | 29 | 855,619 | 713 |
| Data Relay (đủ dữ liệu) | Tây bắc | 147 | 6.8 | 9.5 | 22.9 | 2.7 | 2.6 | 6.6 | 3,258 | 17 | 1,906,086 | 14 |
| Data Relay (đủ dữ liệu) | Đông bắc | 276 | 3.6 | 5.5 | 7.4 | 1.1 | 0.3 | 3.6 | 54 | 11 | 66,037 | n/a |

- Sảnh lễ tân (Player Spawn) (4 hướng): 247 FPS, 4.0 ms TB, p95 6.4 ms, tối đa 11.4 ms; CPU main 1.7, render 0.9, GPU 3.9 ms; batches TB 1,057.
- Phòng server (giữa hai dãy rack) (4 hướng): 206 FPS, 4.9 ms TB, p95 7.7 ms, tối đa 13.4 ms; CPU main 1.6, render 0.8, GPU 4.8 ms; batches TB 822.
- Data Relay (đủ dữ liệu) (4 hướng): 228 FPS, 4.4 ms TB, p95 7.4 ms, tối đa 22.9 ms; CPU main 1.6, render 0.8, GPU 4.3 ms; batches TB 890.

## Chạy một vòng qua các phòng, Lockdown, Hunter hoạt động

- Vòng chạy: 242 FPS, 4.1 ms TB, p95 6.1 ms, tối đa 28.7 ms; CPU main 2.0, render 0.7, GPU 4.0 ms; batches TB 852.
- 14515 khung trong 60 s; khung > 25 ms: 2, > 33 ms: 0, > 50 ms: 0; p99 7.3 ms. Quãng đường 257 m, tới 4/8 điểm, Hunter đuổi 7 lần, bị bắt 7 lần, mở 1 cửa.
- 5 khung nặng nhất: 28.7 ms (main 2.2, render 0.5, GPU 0.0); 26.2 ms (main 2.2, render 0.8, GPU 0.0); 24.4 ms (main 2.0, render 0.8, GPU 0.0); 17.2 ms (main 2.3, render 1.9, GPU 0.0); 16.3 ms (main 2.4, render 1.7, GPU 3.1).
