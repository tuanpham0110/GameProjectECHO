using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace ProjectEcho.Tests
{
    public sealed class LayoutAndDoorTests
    {
        private const string Settings = "Assets/_ProjectECHO/Settings/";

        [Test]
        public void OfficeLayoutMatchesRulesAndStaysInsideFloor()
        {
            var layout = AssetDatabase.LoadAssetAtPath<LevelLayout>(Settings + "Office_Level01_Layout.asset");
            var rules = AssetDatabase.LoadAssetAtPath<LevelRules>(Settings + "Office_Level01_Rules.asset");
            Assert.That(layout, Is.Not.Null, "Chạy Tools → Project ECHO → Build Vertical Slice để tạo layout.");
            Assert.That(rules, Is.Not.Null);
            Assert.That(layout.environment, Is.EqualTo(LevelEnvironment.Model));
            Assert.That(layout.data.Length, Is.EqualTo(rules.requiredData));
            Assert.That(layout.data.Select(d => d.id).Distinct().Count(), Is.EqualTo(layout.data.Length));
            Assert.That(layout.data.All(d => !string.IsNullOrWhiteSpace(d.id)), Is.True);
            Assert.That(layout.waypoints.Length, Is.GreaterThanOrEqualTo(2));
            var points = layout.data.Select(d => d.position).Concat(layout.waypoints).Concat(layout.supports.Select(s => s.position))
                .Append(layout.playerSpawn.position).Append(layout.hunterSpawn.position).Append(layout.relay.position).Append(layout.exit.position);
            foreach (var point in points)
                Assert.That(layout.walkBounds.Contains(new Vector2(point.x, point.z)), Is.True, $"{point} nằm ngoài vùng sàn.");
            Assert.That(Vector3.Distance(layout.relay.position, layout.exit.position), Is.GreaterThan(15),
                "Relay và cổng thoát cần cách nhau để tạo giai đoạn chạy thoát.");
            Assert.That(Vector3.Distance(layout.playerSpawn.position, layout.hunterSpawn.position), Is.GreaterThan(20),
                "Hunter không được khởi động sát TAY.");
        }

        [Test]
        public void DoorTogglesBlockerVisualAndObstacleAndRespectsHunterDelay()
        {
            var root = new GameObject("Door");
            try
            {
                var door = root.AddComponent<OfficeDoor>();
                door.panel = new GameObject("Panel");
                door.panel.transform.SetParent(root.transform);
                var visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform);
                door.visuals = new[] { visual };
                door.obstacle = root.AddComponent<NavMeshObstacle>();
                door.startsOpen = false;
                door.swingVisuals = false;
                door.ResetRun();
                Assert.That(door.IsOpen, Is.False);
                Assert.That(door.panel.activeSelf && visual.activeSelf, Is.True);
                Assert.That(door.obstacle.enabled, Is.False, "Cửa đóng chưa khóa không cắt NavMesh; Hunter dừng lại mở cửa.");

                door.hunterOpenDelay = 3;
                Assert.That(door.RequestHunterOpen(), Is.True);
                Assert.That(door.IsOpen, Is.False, "Hunter phải chờ hết thời gian mở cửa.");
                door.CancelHunterOpen();

                door.hunterOpenDelay = 0;
                Assert.That(door.RequestHunterOpen(), Is.True);
                Assert.That(door.IsOpen, Is.True);
                Assert.That(door.panel.activeSelf || visual.activeSelf || door.obstacle.enabled, Is.False);

                door.locked = true;
                door.ResetRun();
                Assert.That(door.obstacle.enabled, Is.True, "Cửa khóa cắt NavMesh để Hunter tìm đường khác.");
                Assert.That(door.RequestHunterOpen(), Is.False, "Cửa khóa không được mở bởi Hunter.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
