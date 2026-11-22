# Đo hiệu năng Office_Level01

- Thời điểm: 2026-11-22 14:04
- Biến thể: noshadows
- Màn hình: 1920×1080, cửa sổ, vSync tắt
- GPU: NVIDIA GeForce GTX 1650 (Direct3D12, 3962 MB)
- CPU: Intel(R) Core(TM) i5-10300H CPU @ 2.50GHz (8 luồng), RAM 16177 MB
- Chất lượng: PC, FrameTimingManager: bật
- Renderer bật: 6545, đổ bóng: 6539
- Vật nhỏ (cạnh lớn nhất < 0.35 m): 4624 renderer

## Góc nhìn cố định (Hunter đứng yên)

| Vị trí | Hướng | FPS | TB (ms) | p95 (ms) | Tối đa (ms) | CPU main | CPU render | GPU | Batches | SetPass | Tam giác | Shadow casters |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Sảnh lễ tân (Player Spawn) | Bắc | 91 | 11.0 | 18.4 | 21.8 | 6.1 | 1.5 | 3.9 | 9,524 | 31 | 2,408,661 | n/a |
| Sảnh lễ tân (Player Spawn) | Đông | 184 | 5.4 | 10.4 | 11.8 | 1.4 | 0.8 | 3.2 | 949 | 20 | 977,411 | n/a |
| Sảnh lễ tân (Player Spawn) | Nam | 225 | 4.4 | 7.1 | 8.3 | 1.0 | 0.7 | 3.0 | 132 | 18 | 62,086 | n/a |
| Sảnh lễ tân (Player Spawn) | Tây | 222 | 4.5 | 7.5 | 9.0 | 1.0 | 0.6 | 2.8 | 87 | 13 | 46,604 | n/a |
| Phòng server (giữa hai dãy rack) | Bắc | 129 | 7.7 | 13.5 | 21.9 | 2.4 | 1.5 | 5.8 | 2,833 | 25 | 1,937,209 | n/a |
| Phòng server (giữa hai dãy rack) | Đông | 192 | 5.2 | 8.5 | 10.0 | 1.1 | 0.6 | 3.5 | 75 | 17 | 95,775 | n/a |
| Phòng server (giữa hai dãy rack) | Nam | 180 | 5.6 | 9.6 | 13.3 | 1.3 | 0.8 | 3.4 | 593 | 20 | 366,759 | n/a |
| Phòng server (giữa hai dãy rack) | Tây | 87 | 11.5 | 21.9 | 25.2 | 6.2 | 2.0 | 4.6 | 5,966 | 33 | 2,008,662 | n/a |
| Data Relay (đủ dữ liệu) | Đông nam | 214 | 4.7 | 8.3 | 10.3 | 1.1 | 0.7 | 3.2 | 133 | 19 | 104,402 | n/a |
| Data Relay (đủ dữ liệu) | Tây nam | 176 | 5.7 | 10.0 | 10.8 | 1.4 | 0.9 | 3.4 | 769 | 20 | 730,397 | n/a |
| Data Relay (đủ dữ liệu) | Tây bắc | 78 | 12.8 | 21.5 | 30.1 | 7.3 | 1.6 | 5.2 | 11,356 | 31 | 3,526,416 | n/a |
| Data Relay (đủ dữ liệu) | Đông bắc | 209 | 4.8 | 8.7 | 10.2 | 1.1 | 0.7 | 3.2 | 97 | 18 | 131,628 | n/a |

- Sảnh lễ tân (Player Spawn) (4 hướng): 180 FPS, 5.5 ms TB, p95 13.9 ms, tối đa 21.8 ms; CPU main 1.8, render 0.8, GPU 3.1 ms; batches TB 1,507.
- Phòng server (giữa hai dãy rack) (4 hướng): 147 FPS, 6.8 ms TB, p95 15.5 ms, tối đa 25.2 ms; CPU main 2.2, render 1.1, GPU 4.1 ms; batches TB 1,713.
- Data Relay (đủ dữ liệu) (4 hướng): 169 FPS, 5.9 ms TB, p95 14.1 ms, tối đa 30.1 ms; CPU main 1.9, render 0.8, GPU 3.4 ms; batches TB 1,582.

## Chạy một vòng qua các phòng, Lockdown, Hunter hoạt động

- Vòng chạy: 186 FPS, 5.4 ms TB, p95 9.7 ms, tối đa 40.9 ms; CPU main 1.5, render 0.8, GPU 3.2 ms; batches TB 727.
- 11144 khung trong 60 s; khung > 25 ms: 11, > 33 ms: 4, > 50 ms: 0; p99 13.2 ms. Quãng đường 240 m, tới 3/8 điểm, Hunter đuổi 9 lần, bị bắt 10 lần, mở 0 cửa.
- 5 khung nặng nhất: 40.9 ms (main 2.4, render 2.4, GPU 3.3); 39.7 ms (main 2.4, render 1.7, GPU 3.3); 35.7 ms (main 2.5, render 0.7, GPU 3.3); 34.4 ms (main 19.2, render 1.0, GPU 3.2); 32.7 ms (main 2.9, render 0.7, GPU 3.2).
