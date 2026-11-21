using System;
using Unity.AI.Navigation;
using UnityEngine;

namespace ProjectEcho
{
    public sealed class LevelManager : MonoBehaviour
    {
        public GameManager game;
        public LevelRules rules;
        public PlayerController player;
        public HunterBrain hunter;
        public ThirdPersonCamera cameraView;
        public DataRelay relay;
        public ExitGate exit;
        public Transform playerSpawn;
        public Transform hunterSpawn;
        public Transform[] waypoints;
        public NavMeshSurface navigation;
        public LayerMask worldMask = 1;
        public Interactable[] interactions;

        public event Action<Vector3, float> Noise;
        public bool IsInRelayRange => (player.transform.position - relay.transform.position).sqrMagnitude <= rules.relayRadius * rules.relayRadius;
        public bool IsAtExit => (player.transform.position - exit.transform.position).sqrMagnitude <= exit.activationRadius * exit.activationRadius;

        private void Awake() => player.Bind(this, cameraView);

        public void ResetRun()
        {
            foreach (var target in interactions) if (target != null) target.ResetRun();
            foreach (var device in FindObjectsByType<SupportDevice>(FindObjectsSortMode.None))
                if (device.gameObject.scene == gameObject.scene) Destroy(device.gameObject);
            RespawnPlayer();
            player.ResetRun();
            hunter.ResetRun(hunterSpawn);
            SyncWorld();
        }

        public void RespawnPlayer() => player.Teleport(playerSpawn.position, playerSpawn.rotation);
        public void EmitNoise(Vector3 position, float radius) => Noise?.Invoke(position, radius);

        public void SyncWorld()
        {
            foreach (var target in interactions)
                if (target is DataPickup data) data.SetCollected(game.Mission.HasData(data.dataId));
            exit.SetOpen(game.Mission.CanExit);
        }
    }
}
