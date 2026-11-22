# Đo hiệu năng Office_Level01

- Thời điểm: 2026-11-22 14:09
- Biến thể: nosmallshadows
- Màn hình: 1920×1080, cửa sổ, vSync tắt
- GPU: NVIDIA GeForce GTX 1650 (Direct3D12, 3962 MB)
- CPU: Intel(R) Core(TM) i5-10300H CPU @ 2.50GHz (8 luồng), RAM 16177 MB
- Chất lượng: PC, FrameTimingManager: bật
- Renderer bật: 6545, đổ bóng: 6539
- Vật nhỏ (cạnh lớn nhất < 0.35 m): 4624 renderer

## Góc nhìn cố định (Hunter đứng yên)

| Vị trí | Hướng | FPS | TB (ms) | p95 (ms) | Tối đa (ms) | CPU main | CPU render | GPU | Batches | SetPass | Tam giác | Shadow casters |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Sảnh lễ tân (Player Spawn) | Bắc | 83 | 12.0 | 20.3 | 28.2 | 7.1 | 1.8 | 4.0 | 9,711 | 38 | 2,521,037 | 133 |
| Sảnh lễ tân (Player Spawn) | Đông | 155 | 6.5 | 10.9 | 14.1 | 1.9 | 1.3 | 3.6 | 1,623 | 37 | 1,462,850 | 687 |
| Sảnh lễ tân (Player Spawn) | Nam | 213 | 4.7 | 8.4 | 10.1 | 1.3 | 0.8 | 3.1 | 258 | 24 | 156,437 | 128 |
| Sảnh lễ tân (Player Spawn) | Tây | 221 | 4.5 | 7.1 | 8.7 | 1.2 | 0.7 | 2.9 | 199 | 19 | 137,022 | 114 |
| Phòng server (giữa hai dãy rack) | Bắc | 109 | 9.2 | 19.3 | 21.3 | 3.6 | 1.9 | 5.8 | 2,815 | 25 | 1,929,379 | n/a |
| Phòng server (giữa hai dãy rack) | Đông | 190 | 5.3 | 9.0 | 10.0 | 1.1 | 0.7 | 3.5 | 75 | 17 | 95,810 | n/a |
| Phòng server (giữa hai dãy rack) | Nam | 172 | 5.8 | 9.8 | 10.6 | 1.6 | 1.1 | 3.6 | 1,112 | 31 | 728,413 | 529 |
| Phòng server (giữa hai dãy rack) | Tây | 88 | 11.3 | 21.7 | 26.4 | 5.9 | 2.3 | 4.7 | 6,166 | 44 | 2,173,292 | 204 |
| Data Relay (đủ dữ liệu) | Đông nam | 182 | 5.5 | 9.6 | 10.3 | 1.4 | 0.9 | 3.4 | 264 | 25 | 212,831 | 133 |
| Data Relay (đủ dữ liệu) | Tây nam | 153 | 6.5 | 11.0 | 13.9 | 2.0 | 1.4 | 3.7 | 1,422 | 37 | 1,185,747 | 667 |
| Data Relay (đủ dữ liệu) | Tây bắc | 74 | 13.5 | 22.4 | 25.6 | 7.9 | 1.8 | 5.2 | 11,444 | 32 | 3,557,705 | 14 |
| Data Relay (đủ dữ liệu) | Đông bắc | 207 | 4.8 | 8.6 | 10.6 | 1.1 | 0.7 | 3.2 | 97 | 18 | 131,642 | n/a |

- Sảnh lễ tân (Player Spawn) (4 hướng): 168 FPS, 6.0 ms TB, p95 14.5 ms, tối đa 28.2 ms; CPU main 2.1, render 1.0, GPU 3.2 ms; batches TB 1,723.
- Phòng server (giữa hai dãy rack) (4 hướng): 140 FPS, 7.2 ms TB, p95 16.6 ms, tối đa 26.4 ms; CPU main 2.5, render 1.3, GPU 4.1 ms; batches TB 1,889.
- Data Relay (đủ dữ liệu) (4 hướng): 154 FPS, 6.5 ms TB, p95 15.4 ms, tối đa 25.6 ms; CPU main 2.3, render 1.1, GPU 3.6 ms; batches TB 1,840.

## Chạy một vòng qua các phòng, Lockdown, Hunter hoạt động

- Vòng chạy: 144 FPS, 7.0 ms TB, p95 15.9 ms, tối đa 41.5 ms; CPU main 2.7, render 1.3, GPU 3.6 ms; batches TB 2,244.
- 8631 khung trong 60 s; khung > 25 ms: 7, > 33 ms: 1, > 50 ms: 0; p99 20.3 ms. Quãng đường 204 m, tới 9/8 điểm, Hunter đuổi 4 lần, bị bắt 2 lần, mở 2 cửa.
- 5 khung nặng nhất: 41.5 ms (main 1.6, render 0.8, GPU 4.0); 28.7 ms (main 1.8, render 1.2, GPU 4.3); 27.1 ms (main 4.9, render 6.0, GPU 4.3); 26.6 ms (main 4.5, render 2.7, GPU 5.1); 25.3 ms (main 6.2, render 1.5, GPU 3.9).
