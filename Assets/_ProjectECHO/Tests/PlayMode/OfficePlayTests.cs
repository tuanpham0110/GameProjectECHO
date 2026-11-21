using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ProjectEcho.Tests
{
    /// <summary>Kiểm thử chung chạy trên mọi scene Level: vòng nhiệm vụ, bị bắt, hồi sinh, Relay, cổng và retry.</summary>
    public abstract class LevelPlayTestBase
    {
        protected abstract string ScenePath { get; }
        protected LevelManager level;

        [UnitySetUp]
        public IEnumerator LoadLevel()
        {
            Time.timeScale = 1;
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync(System.IO.Path.GetFileNameWithoutExtension(ScenePath), LoadSceneMode.Single);
#endif
            level = Object.FindFirstObjectByType<LevelManager>();
            Assert.That(level, Is.Not.Null);
            yield return null;
            Assert.That(level.hunter.GetComponent<NavMeshAgent>().isOnNavMesh, Is.True);
        }

        [UnityTearDown]
        public IEnumerator RestoreTime()
        {
            Time.timeScale = 1;
            yield return null;
        }

        [UnityTest]
        public IEnumerator MissionSupportsCaptureRecoveryRetryAndEscape()
        {
            string profile = Path.Combine(Path.GetTempPath(), "EchoOfficeTest-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                level.game.LoadProfile(new SaveSystem(profile));
                StartRun();
                var game = level.game;
                var player = level.player;
                var pickups = level.interactions.OfType<DataPickup>().ToArray();
                Assert.That(pickups.Length, Is.EqualTo(level.rules.requiredData));
                player.Teleport(level.exit.transform.position, Quaternion.identity);
                Assert.That(game.TryExit(), Is.False);
                player.Teleport(level.relay.transform.position, Quaternion.identity);
                level.relay.Interact(player);
                Assert.That(game.Mission.Relay, Is.EqualTo(RelayState.Idle));

                foreach (var pickup in pickups) pickup.Interact(player);
                Assert.That(game.Mission.ValidData, Is.EqualTo(pickups.Length));
                Assert.That(game.Mission.Threat, Is.EqualTo(4));
                Assert.That(pickups.All(p => !p.gameObject.activeSelf), Is.True);
                level.relay.Interact(player);
                game.Step(3);
                Assert.That(game.Mission.RelayProgress, Is.GreaterThan(0));
                float beforeCapture = game.Timer.Remaining;
                Assert.That(game.TryCapture(), Is.True);
                Assert.That(game.TryCapture(), Is.False);
                Assert.That(game.Mission.ValidData, Is.EqualTo(pickups.Length - 2));
                Assert.That(game.Mission.Threat, Is.EqualTo(level.rules.GetThreat(pickups.Length - 2)));
                Assert.That(game.Mission.Relay, Is.EqualTo(RelayState.Idle));
                Assert.That(pickups.Count(p => p.gameObject.activeSelf), Is.EqualTo(2));
                game.Step(level.rules.captureSeconds + 0.01f);
                Assert.That(game.State.Current, Is.EqualTo(GameState.Playing));
                Assert.That(game.Timer.Remaining, Is.LessThan(beforeCapture));
                Assert.That(Vector3.Distance(player.transform.position, level.playerSpawn.position), Is.LessThan(0.02f));
                Assert.That(game.Equipped, Is.EqualTo(Skill.BurstSprint));

                foreach (var pickup in pickups.Where(p => p.gameObject.activeSelf)) pickup.Interact(player);
                player.Teleport(level.relay.transform.position, Quaternion.identity);
                level.relay.Interact(player);
                game.Step(level.rules.relaySeconds / 2);
                player.Teleport(level.playerSpawn.position, Quaternion.identity);
                game.Step(level.rules.relaySeconds);
                Assert.That(game.Mission.RelayProgress, Is.EqualTo(0.5f));
                player.Teleport(level.relay.transform.position, Quaternion.identity);
                game.Step(level.rules.relaySeconds / 2);
                Assert.That(level.exit.IsOpen, Is.True);
                Assert.That(game.TryExit(), Is.False);
                player.Teleport(level.exit.transform.position, Quaternion.identity);
                Assert.That(game.TryExit(), Is.True);
                Assert.That(game.State.Current, Is.EqualTo(GameState.Complete));
                Assert.That(new SaveSystem(profile).Load().unlocked, Does.Contain(Skill.BurstSprint));

                StartRun();
                game.TogglePause();
                float priorPause = game.Timer.Remaining;
                game.Step(1);
                Assert.That(game.Timer.Remaining, Is.EqualTo(priorPause));
                game.TogglePause();
                game.Step(game.Timer.Remaining);
                Assert.That(game.State.Current, Is.EqualTo(GameState.Failed));
                Assert.That(game.TryExit(), Is.False);
                Assert.That(pickups.All(p => p.gameObject.activeSelf), Is.True);
            }
            finally
            {
                if (Directory.Exists(profile)) Directory.Delete(profile, true);
            }
            yield return null;
        }

        protected void StartRun()
        {
            var game = level.game;
            game.OpenBriefing();
            game.Select(Skill.Dash);
            Assert.That(game.Selected, Is.Not.EqualTo(Skill.Dash));
            game.Select(Skill.BurstSprint);
            game.Confirm();
            Assert.That(game.CanStart, Is.True);
            game.StartRun();
            Assert.That(game.State.Current, Is.EqualTo(GameState.Playing));
        }

        protected static float PathLength(NavMeshPath path)
        {
            float length = 0;
            for (int i = 1; i < path.corners.Length; i++) length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return length;
        }
    }

    /// <summary>Kiểm thử trên blockout có hình học đã biết trước.</summary>
    public sealed class SandboxPlayTests : LevelPlayTestBase
    {
        protected override string ScenePath => "Assets/_ProjectECHO/Scenes/Sandbox/Blockout_Sandbox.unity";

        [UnityTest]
        public IEnumerator CharacterControllerStopsAtWallAndHunterCannotSeeThroughWall()
        {
            StartRun();
            var player = level.player;
            player.Teleport(new Vector3(-17, 0.05f, -9), Quaternion.identity);
            Physics.SyncTransforms();
            for (int i = 0; i < 40; i++) player.ApplyMovement(Vector3.left * 0.25f);
            Assert.That(player.transform.position.x, Is.GreaterThan(-18.8f));
            Assert.That(player.transform.position.x, Is.LessThan(-17.5f));

            Vector3 observer = new(-2, 1.5f, -10);
            Vector3 target = new(2, 1.5f, -10);
            Assert.That(HunterPerception.HasSight(observer, Vector3.right, target, 8, 90, level.worldMask), Is.False);
            Assert.That(HunterPerception.HasSight(observer, Vector3.right, new Vector3(-0.8f, 1.5f, -10), 8, 90, level.worldMask), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HunterPatrolsAndCanRouteAroundCentralWall()
        {
            StartRun();
            var path = new NavMeshPath();
            Assert.That(NavMesh.SamplePosition(new Vector3(-3, 0, -10), out var start, 1.25f, NavMesh.AllAreas), Is.True);
            Assert.That(NavMesh.SamplePosition(new Vector3(3, 0, -10), out var end, 1.25f, NavMesh.AllAreas), Is.True);
            Assert.That(NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path), Is.True);
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
            Assert.That(path.corners.Length, Is.GreaterThan(2));

            var origin = level.hunter.transform.position;
            yield return new WaitForSeconds(0.8f);
            Assert.That(Vector3.Distance(origin, level.hunter.transform.position), Is.GreaterThan(0.3f));
            Assert.That(level.hunter.LastKnownPosition, Is.Null);
        }
    }

    /// <summary>Kiểm thử trên hình học thật của FBX Office (số đo trong Docs/Technical/OfficeMap).</summary>
    public sealed class OfficeMapPlayTests : LevelPlayTestBase
    {
        protected override string ScenePath => "Assets/_ProjectECHO/Scenes/Office_Level01.unity";

        [UnityTest]
        public IEnumerator PlayerStandsOnOfficeFloorAndIsStoppedByRealWall()
        {
            StartRun();
            var player = level.player;
            for (int i = 0; i < 30; i++) yield return null;
            Vector3 feet = player.transform.position;
            Assert.That(Mathf.Abs(feet.y - level.playerSpawn.position.y), Is.LessThan(0.3f), $"TAY rơi khỏi Player Spawn, hiện ở {feet}.");
            Assert.That(Physics.Raycast(feet + Vector3.up * 0.5f, Vector3.down, out var floor, 0.8f, level.worldMask, QueryTriggerInteraction.Ignore),
                Is.True, $"Không có sàn dưới chân TAY tại {feet}.");
            Assert.That(floor.normal.y, Is.GreaterThan(0.7f));

            // Vách tây phòng server nằm ở X ≈ 15,9–16,4 trong khoảng Z −31…−8,4.
            player.Teleport(new Vector3(14.9f, 0.05f, -20), Quaternion.identity);
            Physics.SyncTransforms();
            for (int i = 0; i < 40; i++) player.ApplyMovement(Vector3.right * 0.25f);
            Assert.That(player.transform.position.x, Is.LessThan(15.95f), "TAY không được xuyên vách phòng server.");
            Assert.That(player.transform.position.x, Is.GreaterThan(15.0f));

            // Biên sàn: đi mãi về phía tây không rơi khỏi map.
            player.Teleport(new Vector3(-22, 0.05f, -50), Quaternion.identity);
            Physics.SyncTransforms();
            for (int i = 0; i < 40; i++) player.ApplyMovement(Vector3.left * 0.25f + Vector3.down * 0.05f);
            Assert.That(player.transform.position.x, Is.GreaterThan(-24.7f));
        }

        [UnityTest]
        public IEnumerator HunterCannotSeeThroughOfficeWallButSeesAcrossOpenFloor()
        {
            StartRun();
            yield return null;
            Assert.That(HunterPerception.HasSight(new Vector3(14.5f, 1.5f, -20), Vector3.right, new Vector3(18.5f, 1.5f, -20), 8, 90, level.worldMask), Is.False);
            Assert.That(HunterPerception.HasSight(new Vector3(9, 1.5f, -27.4f), Vector3.right, new Vector3(15, 1.5f, -27.4f), 8, 90, level.worldMask), Is.True);
        }

        [UnityTest]
        public IEnumerator NavMeshConnectsHunterSpawnToRelayAndExit()
        {
            StartRun();
            yield return null;
            var path = new NavMeshPath();
            Assert.That(NavMesh.SamplePosition(level.hunterSpawn.position, out var start, 1, NavMesh.AllAreas), Is.True);
            foreach (var target in new[] { level.relay.transform, level.playerSpawn })
            {
                Assert.That(NavMesh.SamplePosition(target.position, out var end, 1.5f, NavMesh.AllAreas), Is.True, target.name);
                Assert.That(NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path), Is.True, target.name);
                Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete), target.name);
                Assert.That(PathLength(path), Is.GreaterThan(Vector3.Distance(start.position, end.position) - 0.01f));
            }
        }

        [UnityTest]
        public IEnumerator HunterOpensClosedServerDoorToInvestigate()
        {
            StartRun();
            var door = level.interactions.OfType<OfficeDoor>().First(d => d.name.Contains("Server"));
            Assert.That(door.IsOpen, Is.False, "Cửa phòng server bắt đầu ở trạng thái đóng như trong FBX.");
            var hunter = level.hunter;
            // Bản sao profile không sai số tín hiệu để kết quả test xác định (không sửa asset gốc).
            hunter.profile = Object.Instantiate(hunter.profile);
            hunter.profile.signalUncertainty = 0;
            var agent = hunter.GetComponent<NavMeshAgent>();
            // Phía bắc cửa (trong phòng hồ sơ), đích ở dải trống phía bắc phòng server.
            agent.Warp(new Vector3(20.34f, 0, -7.4f));
            yield return null;
            Time.timeScale = 4;
            hunter.ReceiveScannerSignal(new Vector3(22f, 0, -9.8f), 100);
            float timeout = Time.time + 24;
            while (Time.time < timeout && (!door.IsOpen || hunter.transform.position.z > -9.2f)) yield return null;
            Time.timeScale = 1;
            Assert.That(door.IsOpen, Is.True, "Hunter phải mở cửa đóng chưa khóa khi tuyến đường đi qua cửa.");
            Assert.That(hunter.transform.position.z, Is.LessThan(-9.2f), "Hunter phải đi qua cửa vào phòng server.");
        }
    }
}
