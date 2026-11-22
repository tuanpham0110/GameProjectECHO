# Đo hiệu năng Office_Level01

- Thời điểm: 2026-11-22 13:32
- Màn hình: 1920×1080, cửa sổ, vSync tắt
- GPU: NVIDIA GeForce GTX 1650 (Direct3D12, 3962 MB)
- CPU: Intel(R) Core(TM) i5-10300H CPU @ 2.50GHz (8 luồng), RAM 16177 MB
- Chất lượng: PC

| Vị trí | Hướng camera | FPS trung bình | Frame time TB (ms) | p95 (ms) | Tối đa (ms) | Batches | SetPass | Tam giác |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Sảnh lễ tân (Player Spawn) | Bắc | 84 | 11.9 | 19.4 | 24.1 | 8,608 | 36 | 2,162,306 |
| Sảnh lễ tân (Player Spawn) | Đông | 156 | 6.4 | 11.0 | 15.4 | 1,619 | 37 | 1,457,524 |
| Sảnh lễ tân (Player Spawn) | Nam | 215 | 4.6 | 7.3 | 11.1 | 260 | 24 | 157,611 |
| Sảnh lễ tân (Player Spawn) | Tây | 216 | 4.6 | 7.5 | 8.9 | 199 | 19 | 137,936 |
| Phòng server (giữa hai dãy rack) | Bắc | 125 | 8.0 | 13.8 | 21.7 | 2,784 | 25 | 1,950,549 |
| Phòng server (giữa hai dãy rack) | Đông | 192 | 5.2 | 8.6 | 9.7 | 75 | 17 | 95,894 |
| Phòng server (giữa hai dãy rack) | Nam | 171 | 5.8 | 10.0 | 12.3 | 1,113 | 31 | 721,298 |
| Phòng server (giữa hai dãy rack) | Tây | 86 | 11.6 | 22.0 | 26.7 | 6,046 | 44 | 2,125,424 |
| Data Relay (đủ dữ liệu) | Đông nam | 201 | 5.0 | 9.3 | 10.3 | 264 | 25 | 212,968 |
| Data Relay (đủ dữ liệu) | Tây nam | 162 | 6.2 | 10.4 | 13.2 | 1,425 | 37 | 1,177,233 |
| Data Relay (đủ dữ liệu) | Tây bắc | 76 | 13.2 | 21.8 | 27.0 | 10,988 | 32 | 3,338,264 |
| Data Relay (đủ dữ liệu) | Đông bắc | 230 | 4.3 | 7.1 | 9.5 | 96 | 18 | 131,448 |

Tổng hợp theo vị trí (gộp bốn hướng):

- Sảnh lễ tân (Player Spawn): 168 FPS, 6.0 ms trung bình, p95 14.7 ms.
- Phòng server (giữa hai dãy rack): 144 FPS, 7.0 ms trung bình, p95 15.6 ms.
- Data Relay (đủ dữ liệu): 167 FPS, 6.0 ms trung bình, p95 15.0 ms.
