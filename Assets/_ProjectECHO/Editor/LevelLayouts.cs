using UnityEngine;

namespace ProjectEcho.Editor
{
    /// <summary>
    /// Giá trị khởi tạo cho asset LevelLayout. Chỉ dùng khi asset chưa tồn tại; sau đó designer chỉnh trực tiếp asset.
    /// Tọa độ Office đo từ FBX bằng Tools → Analyze Office Map.
    /// </summary>
    internal static class LevelLayouts
    {
        private static readonly string[] DataNames =
        {
            "Research File", "Memory Fragment", "Clone ID", "DNA Sample", "Access Key", "Archive Fragment"
        };

        internal static void FillOffice(LevelLayout layout)
        {
            layout.environment = LevelEnvironment.Model;
            layout.modelFolder = OfficeLevelTools.OfficeFolder;
            layout.modelName = "Map office (Tong hop)";
            // Mesh sàn "Sàn nhà": X −24,65…34,97, Z −59,06…9,64. Trần "Mái" cao 4,6–4,8 m.
            layout.walkBounds = Rect.MinMaxRect(-24.6f, -59.0f, 34.9f, 9.6f);
            layout.boundaryHeight = 4.7f;
            layout.minColliderSize = 0.3f;
            layout.maxColliderBottom = 2.3f;
            layout.collisionExclusions = new[] { "light pipe", "Neon", "LED", "spill", "luminous", "Glow", "VentStripe",
                // Lá chậu cây cạnh cửa phụ phòng họp chặn TAY ở (-3,3; 0,2) dù NavMesh coi là đi được (playtest 2026-11-22).
                "faceted leaf", "Plant stem" };

            // Sảnh lễ tân (logo AETHER) là điểm xâm nhập; Hunter khởi động ở dải trống phía bắc phòng server.
            // Dải sàn bên ngoài tường bao không có cửa nối vào tòa nhà nên không đặt gameplay ở đó.
            layout.playerSpawn = new LevelLayout.Pose(-12, -38.5f, 0);
            layout.hunterSpawn = new LevelLayout.Pose(24.5f, -9.8f, 270);
            // Data Relay ở khu trống phía nam phòng server (rack + đèn ServerRed); màn hình quay về lối vào phía bắc.
            layout.relay = new LevelLayout.Pose(23.2f, -28.6f, 180);
            // Cổng thoát ở khe 3,5 m giữa sảnh và phòng phía tây nam, đi qua theo hướng nam.
            layout.exit = new LevelLayout.Pose(-15.9f, -40.4f, 180);

            Vector3[] data =
            {
                new(-9.5f, 0, 7.4f),    // Phòng họp, cạnh màn hình trình chiếu
                new(14.4f, 0, -5.4f),   // Phòng hồ sơ
                new(-12, 0, -24),       // Khu làm việc mở phía tây
                new(-5.5f, 0, -44),     // Hành lang chờ phía nam sảnh
                new(4.5f, 0, -42.5f),   // Phòng giữa phía nam
                new(18.5f, 0, -38.6f)   // Phòng kỹ thuật
            };
            layout.data = new LevelLayout.DataPoint[data.Length];
            for (int i = 0; i < data.Length; i++)
                layout.data[i] = new LevelLayout.DataPoint { id = $"office-data-{i + 1:00}", displayName = DataNames[i], position = data[i] };

            layout.supports = new[]
            {
                new LevelLayout.SupportPoint { item = SupportItem.Emp, position = new Vector3(-3, 0, -12) },
                new LevelLayout.SupportPoint { item = SupportItem.Emp, position = new Vector3(8, 0, -37) },
                new LevelLayout.SupportPoint { item = SupportItem.Stun, position = new Vector3(-6, 0, -38) },
                new LevelLayout.SupportPoint { item = SupportItem.Stun, position = new Vector3(10.5f, 0, 1.5f) }
            };

            layout.waypoints = new Vector3[]
            {
                new(14.3f, 0, 0), new(10, 0, -1), new(1, 0, -1.5f), new(-4, 0, -6), new(-11, 0, -12), new(-4, 0, -27),
                new(-7, 0, -38), new(8, 0, -36), new(14, 0, -33), new(24, 0, -27.3f), new(30.5f, 0, -18.9f), new(28, 0, -9.8f),
                new(22.2f, 0, -4)
            };

            // Tủ trắng và "Tu ke" trong FBX có khoang trong (sàn, trần, vách) nên dùng làm chỗ nấp tuyệt đối.
            layout.hideSpots = new[]
            {
                new LevelLayout.HidePoint { inside = new Vector3(-4.0f, 0.12f, -31.1f), outside = new Vector3(-5.1f, 0, -31.1f), absolute = true },
                new LevelLayout.HidePoint { inside = new Vector3(-4.0f, 0.12f, -33.88f), outside = new Vector3(-5.1f, 0, -33.88f), absolute = true },
                new LevelLayout.HidePoint { inside = new Vector3(13.41f, 0.12f, 5.82f), outside = new Vector3(13.41f, 0, 4.7f), absolute = true },
                // Nấp tương đối: sát quầy lễ tân phụ (−11,84; −18,61) và kệ Shelf.004 (6,8; −21,4).
                new LevelLayout.HidePoint { inside = new Vector3(-10.75f, 0, -18.6f), outside = new Vector3(-9.9f, 0, -18.6f), absolute = false },
                new LevelLayout.HidePoint { inside = new Vector3(6.05f, 0, -21.4f), outside = new Vector3(5.2f, 0, -21.4f), absolute = false }
            };

            // Mọi ô cửa trong FBX đều có cánh đang đóng: 2 cửa đơn "Door handle"
            // và 12 cửa đôi ghép từ cặp mesh "Cube.0xx" 0,79 m. Ba cửa bắt đầu đóng để tạo điểm nghẽn quanh phòng họp và phòng server.
            layout.doors = new[]
            {
                Door("Meeting Room Side Door", -2.16f, -0.75f, false, false, "Door handle.002"),
                Door("Server Room North Door", 20.34f, -8.63f, true, false, "Door handle.004"),
                Door("Server Room West Door", 16.27f, -10.91f, false, false, "Cube.007", "Cube.008"),
                Door("Meeting Room Door", -9.28f, -3.72f, true, true, "Cube.003", "Cube.004"),
                Door("Workspace North Door", -0.155f, -3.72f, true, true, "Cube.002", "Cube.005"),
                Door("Records Room Door", 12.34f, 1.69f, false, true, "Cube.014", "Cube.015"),
                Door("Server Room South Door", 17.785f, -31.57f, true, true, "Cube.025", "Cube.026"),
                Door("Annex West Door", 19.945f, -33.11f, false, true, "Cube.027", "Cube.028"),
                Door("Annex South Door", 22.69f, -35.8f, true, true, "Cube.029", "Cube.030"),
                Door("Plant Room Door", 21.075f, -49.5f, true, true, "Cube.035", "Cube.036"),
                Door("Deposit Room Door", 11.235f, -47.85f, true, true, "Cube.037", "Cube.038"),
                Door("South Wing Door", 5.64f, -40.28f, true, true, "Cube.039", "Cube.040"),
                Door("Technical Room Door", 12.925f, -43.4f, false, true, "Cube.041", "Cube.042"),
                Door("Generator Room Door", 12.925f, -51.86f, false, true, "Cube.043", "Cube.044")
            };
        }

        /// <summary>Kích thước để trống: công cụ đo từ bounds các cánh cửa khi dựng scene.</summary>
        private static LevelLayout.DoorPoint Door(string name, float x, float z, bool alongX, bool startsOpen, params string[] leaves) => new()
        {
            name = name,
            modelPaths = leaves,
            center = new Vector3(x, 0, z),
            size = Vector3.zero,
            startsOpen = startsOpen
        };

        internal static void FillBlockout(LevelLayout layout)
        {
            layout.environment = LevelEnvironment.Blockout;
            layout.modelFolder = "";
            layout.modelName = "";
            layout.walkBounds = Rect.MinMaxRect(-19, -14, 19, 14);
            layout.boundaryHeight = 3.4f;
            layout.playerSpawn = new LevelLayout.Pose(-15.7f, -11.5f, 0);
            layout.hunterSpawn = new LevelLayout.Pose(16, 4, 215);
            layout.relay = new LevelLayout.Pose(-15, 11.2f, 0);
            layout.exit = new LevelLayout.Pose(12.5f, 11.9f, 0);
            Vector3[] data =
            {
                new(-14, 0, -5), new(-8, 0, 9), new(4, 0, -9), new(15, 0, -6), new(7, 0, 7), new(16, 0, 10)
            };
            layout.data = new LevelLayout.DataPoint[data.Length];
            for (int i = 0; i < data.Length; i++)
                layout.data[i] = new LevelLayout.DataPoint { id = $"office-data-{i + 1:00}", displayName = DataNames[i], position = data[i] };
            layout.supports = new[]
            {
                new LevelLayout.SupportPoint { item = SupportItem.Emp, position = new Vector3(-12, 0, -11) },
                new LevelLayout.SupportPoint { item = SupportItem.Emp, position = new Vector3(9, 0, -4) },
                new LevelLayout.SupportPoint { item = SupportItem.Stun, position = new Vector3(-12, 0, 5) },
                new LevelLayout.SupportPoint { item = SupportItem.Stun, position = new Vector3(6, 0, 10) }
            };
            layout.waypoints = new Vector3[]
            {
                new(13, 0, 5), new(14, 0, -9), new(3, 0, -3), new(-7, 0, -2), new(-12, 0, 8), new(-2, 0, 7)
            };
            layout.hideSpots = new[]
            {
                new LevelLayout.HidePoint { inside = new Vector3(-15, 0.04f, 6), outside = new Vector3(-15, 0.04f, 5.2f), absolute = false },
                new LevelLayout.HidePoint { inside = new Vector3(14.85f, 0.04f, -9.2f), outside = new Vector3(15.8f, 0.04f, -9.2f), absolute = true, buildLocker = true }
            };
            layout.doors = new[]
            {
                new LevelLayout.DoorPoint { name = "Partition Door", center = new Vector3(0, 0.02f, -3.6f), size = new Vector3(0.36f, 2.3f, 2.72f), startsOpen = true }
            };
        }
    }
}
