# Đo hiệu năng Office_Level01

- Thời điểm: 2026-11-22 14:06
- Biến thể: nossao
- Màn hình: 1920×1080, cửa sổ, vSync tắt
- GPU: NVIDIA GeForce GTX 1650 (Direct3D12, 3962 MB)
- CPU: Intel(R) Core(TM) i5-10300H CPU @ 2.50GHz (8 luồng), RAM 16177 MB
- Chất lượng: PC, FrameTimingManager: bật
- Renderer bật: 6545, đổ bóng: 6539
- ScreenSpaceAmbientOcclusion: tắt 1 feature
- Vật nhỏ (cạnh lớn nhất < 0.35 m): 4624 renderer

## Góc nhìn cố định (Hunter đứng yên)

| Vị trí | Hướng | FPS | TB (ms) | p95 (ms) | Tối đa (ms) | CPU main | CPU render | GPU | Batches | SetPass | Tam giác | Shadow casters |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Sảnh lễ tân (Player Spawn) | Bắc | 103 | 9.7 | 15.9 | 18.6 | 6.2 | 1.5 | 2.2 | 4,831 | 28 | 1,296,587 | 133 |
| Sảnh lễ tân (Player Spawn) | Đông | 180 | 5.6 | 10.3 | 13.7 | 2.1 | 1.2 | 1.7 | 1,154 | 31 | 974,288 | 692 |
| Sảnh lễ tân (Player Spawn) | Nam | 240 | 4.2 | 7.6 | 8.7 | 1.3 | 0.7 | 1.4 | 194 | 20 | 125,306 | 127 |
| Sảnh lễ tân (Player Spawn) | Tây | 259 | 3.9 | 7.0 | 8.0 | 1.3 | 0.7 | 1.2 | 157 | 15 | 114,041 | 114 |
| Phòng server (giữa hai dãy rack) | Bắc | 167 | 6.0 | 10.7 | 13.5 | 2.4 | 1.0 | 2.1 | 1,438 | 16 | 985,138 | n/a |
| Phòng server (giữa hai dãy rack) | Đông | 295 | 3.4 | 6.0 | 7.0 | 1.2 | 0.5 | 0.9 | 38 | 13 | 49,655 | n/a |
| Phòng server (giữa hai dãy rack) | Nam | 216 | 4.6 | 8.3 | 9.1 | 1.7 | 0.8 | 1.4 | 793 | 25 | 524,775 | 534 |
| Phòng server (giữa hai dãy rack) | Tây | 122 | 8.2 | 13.9 | 18.6 | 5.0 | 1.4 | 1.9 | 3,565 | 29 | 1,195,316 | 125 |
| Data Relay (đủ dữ liệu) | Đông nam | 258 | 3.9 | 7.1 | 8.2 | 1.4 | 0.7 | 1.1 | 199 | 19 | 160,596 | 132 |
| Data Relay (đủ dữ liệu) | Tây nam | 207 | 4.8 | 8.3 | 9.3 | 1.9 | 1.1 | 1.4 | 1,042 | 33 | 820,444 | 669 |
| Data Relay (đủ dữ liệu) | Tây bắc | 98 | 10.2 | 16.6 | 20.1 | 6.8 | 1.5 | 2.4 | 5,708 | 22 | 1,748,212 | 14 |
| Data Relay (đủ dữ liệu) | Đông bắc | 252 | 4.0 | 7.6 | 8.7 | 1.1 | 0.5 | 1.1 | 50 | 12 | 65,999 | n/a |

- Sảnh lễ tân (Player Spawn) (4 hướng): 195 FPS, 5.1 ms TB, p95 11.8 ms, tối đa 18.6 ms; CPU main 2.2, render 0.9, GPU 1.5 ms; batches TB 1,012.
- Phòng server (giữa hai dãy rack) (4 hướng): 200 FPS, 5.0 ms TB, p95 11.0 ms, tối đa 18.6 ms; CPU main 2.2, render 0.8, GPU 1.4 ms; batches TB 1,072.
- Data Relay (đủ dữ liệu) (4 hướng): 204 FPS, 4.9 ms TB, p95 11.8 ms, tối đa 20.1 ms; CPU main 2.1, render 0.8, GPU 1.3 ms; batches TB 1,027.

## Chạy một vòng qua các phòng, Lockdown, Hunter hoạt động

- Vòng chạy: 203 FPS, 4.9 ms TB, p95 8.9 ms, tối đa 22.4 ms; CPU main 2.0, render 1.0, GPU 1.5 ms; batches TB 879.
- 12195 khung trong 60 s; khung > 25 ms: 0, > 33 ms: 0, > 50 ms: 0; p99 12.5 ms. Quãng đường 257 m, tới 5/8 điểm, Hunter đuổi 7 lần, bị bắt 7 lần, mở 1 cửa.
- 5 khung nặng nhất: 22.4 ms (main 1.4, render 0.7, GPU 1.5); 19.7 ms (main 7.0, render 1.2, GPU 2.5); 19.4 ms (main 5.5, render 1.1, GPU 2.0); 18.8 ms (main 6.4, render 3.0, GPU 2.5); 18.6 ms (main 6.5, render 2.7, GPU 2.5).
