# Đo hiệu năng Office_Level01

- Thời điểm: 2026-11-22 14:11
- Biến thể: hidesmall
- Màn hình: 1920×1080, cửa sổ, vSync tắt
- GPU: NVIDIA GeForce GTX 1650 (Direct3D12, 3962 MB)
- CPU: Intel(R) Core(TM) i5-10300H CPU @ 2.50GHz (8 luồng), RAM 16177 MB
- Chất lượng: PC, FrameTimingManager: bật
- Renderer bật: 6545, đổ bóng: 6539
- Vật nhỏ (cạnh lớn nhất < 0.35 m): 4624 renderer

## Góc nhìn cố định (Hunter đứng yên)

| Vị trí | Hướng | FPS | TB (ms) | p95 (ms) | Tối đa (ms) | CPU main | CPU render | GPU | Batches | SetPass | Tam giác | Shadow casters |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Sảnh lễ tân (Player Spawn) | Bắc | 114 | 8.8 | 16.8 | 21.4 | 4.1 | 1.9 | 3.2 | 3,426 | 34 | 1,085,738 | 133 |
| Sảnh lễ tân (Player Spawn) | Đông | 159 | 6.3 | 10.8 | 13.9 | 1.8 | 1.3 | 3.6 | 1,613 | 37 | 1,458,983 | 687 |
| Sảnh lễ tân (Player Spawn) | Nam | 198 | 5.1 | 8.9 | 10.6 | 1.3 | 0.9 | 3.2 | 258 | 24 | 156,406 | 128 |
| Sảnh lễ tân (Player Spawn) | Tây | 218 | 4.6 | 7.4 | 9.3 | 1.2 | 0.8 | 3.0 | 199 | 19 | 137,242 | 114 |
| Phòng server (giữa hai dãy rack) | Bắc | 150 | 6.7 | 9.4 | 14.1 | 1.4 | 0.8 | 5.8 | 1,276 | 17 | 1,566,923 | n/a |
| Phòng server (giữa hai dãy rack) | Đông | 195 | 5.1 | 8.4 | 11.4 | 1.0 | 0.6 | 3.6 | 75 | 17 | 95,825 | n/a |
| Phòng server (giữa hai dãy rack) | Nam | 176 | 5.7 | 9.4 | 10.5 | 1.6 | 1.0 | 3.7 | 1,110 | 31 | 726,917 | 530 |
| Phòng server (giữa hai dãy rack) | Tây | 130 | 7.7 | 14.0 | 18.7 | 2.6 | 1.6 | 4.2 | 2,383 | 38 | 1,118,805 | 204 |
| Data Relay (đủ dữ liệu) | Đông nam | 180 | 5.5 | 9.5 | 10.5 | 1.4 | 0.8 | 3.4 | 264 | 25 | 212,830 | 133 |
| Data Relay (đủ dữ liệu) | Tây nam | 157 | 6.4 | 10.6 | 15.1 | 1.9 | 1.3 | 3.8 | 1,415 | 37 | 1,185,989 | 667 |
| Data Relay (đủ dữ liệu) | Tây bắc | 109 | 9.2 | 18.6 | 42.2 | 3.7 | 1.7 | 4.3 | 3,692 | 30 | 1,688,824 | 14 |
| Data Relay (đủ dữ liệu) | Đông bắc | 207 | 4.8 | 8.6 | 10.1 | 1.1 | 0.6 | 3.2 | 97 | 18 | 131,615 | n/a |

- Sảnh lễ tân (Player Spawn) (4 hướng): 172 FPS, 5.8 ms TB, p95 12.2 ms, tối đa 21.4 ms; CPU main 1.9, render 1.1, GPU 3.2 ms; batches TB 1,079.
- Phòng server (giữa hai dãy rack) (4 hướng): 163 FPS, 6.2 ms TB, p95 11.3 ms, tối đa 18.7 ms; CPU main 1.6, render 1.0, GPU 4.3 ms; batches TB 1,092.
- Data Relay (đủ dữ liệu) (4 hướng): 163 FPS, 6.1 ms TB, p95 12.8 ms, tối đa 42.2 ms; CPU main 1.8, render 1.0, GPU 3.6 ms; batches TB 1,059.

## Chạy một vòng qua các phòng, Lockdown, Hunter hoạt động

- Vòng chạy: 161 FPS, 6.2 ms TB, p95 12.7 ms, tối đa 23.9 ms; CPU main 2.0, render 1.2, GPU 3.6 ms; batches TB 1,303.
- 9679 khung trong 60 s; khung > 25 ms: 0, > 33 ms: 0, > 50 ms: 0; p99 15.0 ms. Quãng đường 204 m, tới 9/8 điểm, Hunter đuổi 5 lần, bị bắt 2 lần, mở 2 cửa.
- 5 khung nặng nhất: 23.9 ms (main 2.9, render 1.2, GPU 3.5); 21.0 ms (main 2.7, render 1.0, GPU 4.8); 20.5 ms (main 3.3, render 0.7, GPU 3.7); 20.4 ms (main 3.2, render 1.0, GPU 3.4); 20.3 ms (main 2.8, render 1.7, GPU 3.5).
