# Đo hiệu năng Office_Level01

- Thời điểm: 2026-11-22 14:32
- Biến thể: không (cấu hình phát hành)
- Màn hình: 1920×1080, cửa sổ, vSync tắt
- GPU: NVIDIA GeForce GTX 1650 (Direct3D11, 3962 MB)
- CPU: Intel(R) Core(TM) i5-10300H CPU @ 2.50GHz (8 luồng), RAM 16177 MB
- Chất lượng: PC, FrameTimingManager: bật, GPU Resident Drawer: InstancedDrawing
- Renderer bật: 6545, đổ bóng: 6539

## Góc nhìn cố định (Hunter đứng yên)

| Vị trí | Hướng | FPS | TB (ms) | p95 (ms) | Tối đa (ms) | CPU main | CPU render | GPU | Batches | SetPass | Tam giác | Shadow casters |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Sảnh lễ tân (Player Spawn) | Bắc | 162 | 6.2 | 7.3 | 8.2 | 2.8 | 4.1 | 5.1 | 5,950 | 29 | 2,681,447 | 135 |
| Sảnh lễ tân (Player Spawn) | Đông | 218 | 4.6 | 6.1 | 8.2 | 1.9 | 0.9 | 3.7 | 1,695 | 31 | 1,518,500 | 764 |
| Sảnh lễ tân (Player Spawn) | Nam | 241 | 4.2 | 6.5 | 7.4 | 1.4 | 0.4 | 2.6 | 255 | 17 | 157,611 | 125 |
| Sảnh lễ tân (Player Spawn) | Tây | 250 | 4.0 | 6.4 | 7.1 | 1.3 | 0.3 | 2.6 | 194 | 16 | 138,876 | 111 |
| Phòng server (giữa hai dãy rack) | Bắc | 162 | 6.2 | 7.0 | 8.1 | 1.4 | 0.9 | 6.0 | 1,609 | 16 | 2,100,652 | n/a |
| Phòng server (giữa hai dãy rack) | Đông | 239 | 4.2 | 5.7 | 7.0 | 1.0 | 0.2 | 3.3 | 75 | 10 | 95,894 | n/a |
| Phòng server (giữa hai dãy rack) | Nam | 217 | 4.6 | 6.0 | 7.1 | 1.6 | 0.7 | 3.9 | 1,184 | 25 | 772,508 | 597 |
| Phòng server (giữa hai dãy rack) | Tây | 167 | 6.0 | 7.1 | 7.7 | 2.2 | 2.5 | 5.6 | 3,469 | 33 | 2,347,600 | 206 |
| Data Relay (đủ dữ liệu) | Đông nam | 240 | 4.2 | 6.5 | 7.3 | 1.3 | 0.4 | 2.8 | 286 | 18 | 221,876 | 153 |
| Data Relay (đủ dữ liệu) | Tây nam | 214 | 4.7 | 6.2 | 10.6 | 1.9 | 0.8 | 3.8 | 1,475 | 30 | 1,222,339 | 713 |
| Data Relay (đủ dữ liệu) | Tây bắc | 142 | 7.1 | 8.1 | 9.3 | 2.8 | 4.2 | 6.7 | 6,491 | 23 | 3,804,455 | 14 |
| Data Relay (đủ dữ liệu) | Đông bắc | 240 | 4.2 | 6.3 | 7.2 | 1.0 | 0.3 | 3.1 | 97 | 12 | 131,772 | n/a |

- Sảnh lễ tân (Player Spawn) (4 hướng): 218 FPS, 4.6 ms TB, p95 6.8 ms, tối đa 8.2 ms; CPU main 1.7, render 1.2, GPU 3.3 ms; batches TB 1,658.
- Phòng server (giữa hai dãy rack) (4 hướng): 196 FPS, 5.1 ms TB, p95 6.8 ms, tối đa 8.1 ms; CPU main 1.5, render 1.0, GPU 4.5 ms; batches TB 1,419.
- Data Relay (đủ dữ liệu) (4 hướng): 209 FPS, 4.8 ms TB, p95 7.5 ms, tối đa 10.6 ms; CPU main 1.6, render 1.1, GPU 3.6 ms; batches TB 1,588.

## Chạy một vòng qua các phòng, Lockdown, Hunter hoạt động

- Vòng chạy: 211 FPS, 4.8 ms TB, p95 6.7 ms, tối đa 20.4 ms; CPU main 1.9, render 0.9, GPU 3.7 ms; batches TB 1,296.
- 12632 khung trong 60 s; khung > 25 ms: 0, > 33 ms: 0, > 50 ms: 0; p99 7.6 ms. Quãng đường 257 m, tới 4/8 điểm, Hunter đuổi 7 lần, bị bắt 7 lần, mở 1 cửa.
- 5 khung nặng nhất: 20.4 ms (main 3.2, render 0.9, GPU 3.3); 18.1 ms (main 2.7, render 0.8, GPU 3.7); 14.9 ms (main 2.2, render 0.6, GPU 3.4); 14.8 ms (main 2.1, render 0.7, GPU 0.0); 12.3 ms (main 2.0, render 0.6, GPU 3.9).
