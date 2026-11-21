using UnityEngine;

namespace ProjectEcho
{
    [CreateAssetMenu(menuName = "Project ECHO/Player Tuning")]
    public sealed class PlayerTuning : ScriptableObject
    {
        [Min(0.1f)] public float forwardSpeed = 3.5f;
        [Min(0.1f)] public float backSpeed = 2.5f;
        [Min(0.1f)] public float strafeSpeed = 3.5f;
        [Min(0.1f)] public float sprintSpeed = 5;
        [Min(0.1f)] public float crouchSpeed = 1.7f;
        [Min(0.7f)] public float crouchHeight = 1.05f;
        [Min(0.1f)] public float jumpHeight = 1.2f;
        [Min(0.1f)] public float gravity = 15;
        [Min(0.01f)] public float mouseSensitivity = 0.12f;
        [Min(0.1f)] public float interactionRange = 2.2f;
        [Min(1)] public float scannerExposure = 20;
        [Min(0.1f)] public float scannerSignalRadius = 15;
        [Min(1)] public float burstMultiplier = 1.45f;
        [Min(0.1f)] public float burstDuration = 4;
        [Min(0)] public float burstCooldown = 25;
        [Min(0.1f)] public float dashDistance = 3;
        [Min(0.1f)] public float dashDuration = 0.24f;
        [Min(0)] public float dashCooldown = 18;
        [Min(0.1f)] public float dashVaultClearance = 0.65f;
        [Min(0.1f)] public float sensorRange = 15;
        [Min(0.1f)] public float sensorDuration = 5;
        [Min(0.1f)] public float sensorInterval = 1;
        [Min(0)] public float sensorCooldown = 30;
        [Min(0.1f)] public float empRadius = 2;
        [Min(0.1f)] public float empSeconds = 8;
        [Min(0.1f)] public float stunRadius = 3.2f;
        [Min(0.1f)] public float stunSeconds = 5;
        [Min(0.1f)] public float enrageSeconds = 4;
        [Range(0, 1)] public float enrageBonus = 0.15f;
    }
}
