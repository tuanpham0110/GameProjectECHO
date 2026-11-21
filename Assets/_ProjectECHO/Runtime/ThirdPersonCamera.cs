using Unity.Cinemachine;
using UnityEngine;

namespace ProjectEcho
{
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        public PlayerController player;
        public CinemachineCamera cameraRig;
        private float yaw;
        private float pitch = 8;
        public Quaternion Heading => Quaternion.Euler(0, yaw, 0);

        private void Update()
        {
            if (!player.Level.game.CanAct) return;
            Vector2 look = player.input.Look.ReadValue<Vector2>() * player.tuning.mouseSensitivity;
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, -42, 65);
        }

        private void LateUpdate()
        {
            transform.SetPositionAndRotation(player.Eye, Quaternion.Euler(pitch, yaw, 0));
        }

        public void ResetPose(Quaternion rotation)
        {
            Vector3 previous = transform.position;
            yaw = rotation.eulerAngles.y;
            pitch = 8;
            transform.SetPositionAndRotation(player.Eye, Quaternion.Euler(pitch, yaw, 0));
            cameraRig.OnTargetObjectWarped(transform, transform.position - previous);
        }
    }
}
