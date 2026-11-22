# Đo hiệu năng Office_Level01

- Thời điểm: 2026-11-22 14:51
- Biến thể: không (cấu hình phát hành)
- Màn hình: 1920×1080, cửa sổ, vSync tắt
- GPU: NVIDIA GeForce GTX 1650 (Direct3D11, 3962 MB)
- CPU: Intel(R) Core(TM) i5-10300H CPU @ 2.50GHz (8 luồng), RAM 16177 MB
- Chất lượng: PC, FrameTimingManager: bật, GPU Resident Drawer: InstancedDrawing
- Renderer bật: 6545, đổ bóng: 6539

## Góc nhìn cố định (Hunter đứng yên)

| Vị trí | Hướng | FPS | TB (ms) | p95 (ms) | Tối đa (ms) | CPU main | CPU render | GPU | Batches | SetPass | Tam giác | Shadow casters |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Sảnh lễ tân (Player Spawn) | Bắc | 192 | 5.2 | 6.9 | 10.5 | 2.4 | 2.6 | 4.0 | 3,048 | 23 | 1,393,128 | 135 |
| Sảnh lễ tân (Player Spawn) | Đông | 224 | 4.5 | 6.2 | 19.3 | 1.9 | 0.8 | 3.5 | 1,235 | 29 | 1,024,541 | 764 |
| Sảnh lễ tân (Player Spawn) | Nam | 254 | 3.9 | 5.3 | 21.0 | 1.2 | 0.3 | 2.8 | 196 | 17 | 126,707 | 125 |
| Sảnh lễ tân (Player Spawn) | Tây | 261 | 3.8 | 5.0 | 5.8 | 1.2 | 0.3 | 2.8 | 158 | 16 | 115,673 | 111 |
| Phòng server (giữa hai dãy rack) | Bắc | 177 | 5.6 | 6.1 | 7.4 | 1.2 | 0.7 | 5.5 | 810 | 13 | 1,050,450 | n/a |
| Phòng server (giữa hai dãy rack) | Đông | 257 | 3.9 | 4.8 | 6.0 | 1.0 | 0.2 | 3.2 | 43 | 10 | 48,085 | n/a |
| Phòng server (giữa hai dãy rack) | Nam | 216 | 4.6 | 6.4 | 11.0 | 1.4 | 0.5 | 3.5 | 896 | 23 | 588,493 | 597 |
| Phòng server (giữa hai dãy rack) | Tây | 188 | 5.3 | 5.9 | 7.2 | 1.9 | 1.6 | 5.0 | 1,843 | 27 | 1,255,653 | 206 |
| Data Relay (đủ dữ liệu) | Đông nam | 251 | 4.0 | 5.2 | 6.7 | 1.2 | 0.3 | 2.9 | 225 | 17 | 169,839 | 153 |
| Data Relay (đủ dữ liệu) | Tây nam | 225 | 4.4 | 5.4 | 7.0 | 1.7 | 0.7 | 3.8 | 1,100 | 29 | 855,619 | 713 |
| Data Relay (đủ dữ liệu) | Tây bắc | 169 | 5.9 | 6.7 | 8.0 | 2.4 | 2.6 | 5.5 | 3,258 | 17 | 1,906,086 | 14 |
| Data Relay (đủ dữ liệu) | Đông bắc | 266 | 3.8 | 4.7 | 5.2 | 1.0 | 0.2 | 2.9 | 54 | 11 | 66,037 | n/a |

- Sảnh lễ tân (Player Spawn) (4 hướng): 232 FPS, 4.3 ms TB, p95 6.3 ms, tối đa 21.0 ms; CPU main 1.6, render 0.9, GPU 3.2 ms; batches TB 1,024.
- Phòng server (giữa hai dãy rack) (4 hướng): 209 FPS, 4.8 ms TB, p95 6.2 ms, tối đa 11.0 ms; CPU main 1.4, render 0.7, GPU 4.2 ms; batches TB 829.
- Data Relay (đủ dữ liệu) (4 hướng): 228 FPS, 4.4 ms TB, p95 6.1 ms, tối đa 8.0 ms; CPU main 1.5, render 0.8, GPU 3.6 ms; batches TB 953.

## Chạy một vòng qua các phòng, Lockdown, Hunter hoạt động

- Vòng chạy: 222 FPS, 4.5 ms TB, p95 6.5 ms, tối đa 17.6 ms; CPU main 1.8, render 0.7, GPU 3.3 ms; batches TB 865.
- 13296 khung trong 60 s; khung > 25 ms: 0, > 33 ms: 0, > 50 ms: 0; p99 7.3 ms. Quãng đường 252 m, tới 3/8 điểm, Hunter đuổi 8 lần, bị bắt 8 lần, mở 0 cửa.
- 5 khung nặng nhất: 17.6 ms (main 8.1, render 2.3, GPU 4.4); 13.5 ms (main 1.9, render 1.0, GPU 0.0); 13.2 ms (main 2.2, render 1.1, GPU 3.9); 12.5 ms (main 1.4, render 0.4, GPU 3.0); 11.8 ms (main 1.4, render 0.4, GPU 2.9).
