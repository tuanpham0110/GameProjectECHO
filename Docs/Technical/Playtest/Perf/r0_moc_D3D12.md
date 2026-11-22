# Đo hiệu năng Office_Level01

- Thời điểm: 2026-11-22 14:02
- Biến thể: không (cấu hình phát hành)
- Màn hình: 1920×1080, cửa sổ, vSync tắt
- GPU: NVIDIA GeForce GTX 1650 (Direct3D12, 3962 MB)
- CPU: Intel(R) Core(TM) i5-10300H CPU @ 2.50GHz (8 luồng), RAM 16177 MB
- Chất lượng: PC, FrameTimingManager: bật
- Renderer bật: 6545, đổ bóng: 6539

## Góc nhìn cố định (Hunter đứng yên)

| Vị trí | Hướng | FPS | TB (ms) | p95 (ms) | Tối đa (ms) | CPU main | CPU render | GPU | Batches | SetPass | Tam giác | Shadow casters |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Sảnh lễ tân (Player Spawn) | Bắc | 87 | 11.4 | 18.7 | 27.8 | 6.7 | 1.7 | 4.0 | 9,577 | 38 | 2,527,255 | 133 |
| Sảnh lễ tân (Player Spawn) | Đông | 160 | 6.3 | 10.8 | 14.8 | 1.8 | 1.2 | 3.6 | 1,627 | 37 | 1,463,332 | 692 |
| Sảnh lễ tân (Player Spawn) | Nam | 219 | 4.6 | 7.2 | 8.7 | 1.3 | 0.8 | 3.0 | 258 | 24 | 156,225 | 128 |
| Sảnh lễ tân (Player Spawn) | Tây | 220 | 4.5 | 7.3 | 8.7 | 1.2 | 0.7 | 2.9 | 199 | 19 | 137,112 | 114 |
| Phòng server (giữa hai dãy rack) | Bắc | 116 | 8.6 | 17.0 | 21.7 | 3.1 | 1.6 | 5.8 | 2,837 | 25 | 1,929,087 | n/a |
| Phòng server (giữa hai dãy rack) | Đông | 192 | 5.2 | 8.7 | 10.2 | 1.1 | 0.6 | 3.4 | 75 | 17 | 95,708 | n/a |
| Phòng server (giữa hai dãy rack) | Nam | 171 | 5.9 | 10.2 | 14.1 | 1.6 | 1.1 | 3.5 | 1,114 | 31 | 728,973 | 530 |
| Phòng server (giữa hai dãy rack) | Tây | 85 | 11.7 | 22.4 | 29.4 | 6.1 | 2.3 | 4.8 | 6,172 | 44 | 2,154,731 | 207 |
| Data Relay (đủ dữ liệu) | Đông nam | 198 | 5.1 | 9.3 | 10.4 | 1.3 | 0.8 | 3.2 | 264 | 25 | 212,532 | 133 |
| Data Relay (đủ dữ liệu) | Tây nam | 156 | 6.4 | 11.1 | 15.4 | 1.9 | 1.3 | 3.7 | 1,424 | 37 | 1,184,984 | 669 |
| Data Relay (đủ dữ liệu) | Tây bắc | 75 | 13.3 | 21.8 | 30.2 | 7.8 | 1.7 | 5.3 | 11,371 | 32 | 3,498,974 | 14 |
| Data Relay (đủ dữ liệu) | Đông bắc | 227 | 4.4 | 7.6 | 9.6 | 1.0 | 0.6 | 3.1 | 97 | 18 | 131,601 | n/a |

- Sảnh lễ tân (Player Spawn) (4 hướng): 172 FPS, 5.8 ms TB, p95 14.0 ms, tối đa 27.8 ms; CPU main 2.1, render 1.0, GPU 3.2 ms; batches TB 1,745.
- Phòng server (giữa hai dãy rack) (4 hướng): 141 FPS, 7.1 ms TB, p95 16.2 ms, tối đa 29.4 ms; CPU main 2.4, render 1.2, GPU 4.1 ms; batches TB 1,881.
- Data Relay (đủ dữ liệu) (4 hướng): 164 FPS, 6.1 ms TB, p95 15.1 ms, tối đa 30.2 ms; CPU main 2.1, render 1.0, GPU 3.5 ms; batches TB 1,752.

## Chạy một vòng qua các phòng, Lockdown, Hunter hoạt động

- Vòng chạy: 160 FPS, 6.2 ms TB, p95 11.1 ms, tối đa 26.8 ms; CPU main 2.0, render 1.3, GPU 3.5 ms; batches TB 1,126.
- 9601 khung trong 60 s; khung > 25 ms: 7, > 33 ms: 0, > 50 ms: 0; p99 14.1 ms. Quãng đường 240 m, tới 3/8 điểm, Hunter đuổi 9 lần, bị bắt 10 lần, mở 0 cửa.
- 5 khung nặng nhất: 26.8 ms (main 10.0, render 5.6, GPU 5.5); 26.6 ms (main 8.3, render 2.4, GPU 5.5); 26.4 ms (main 8.2, render 1.4, GPU 5.4); 25.6 ms (main 7.8, render 1.8, GPU 5.6); 25.6 ms (main 6.7, render 1.9, GPU 4.6).
