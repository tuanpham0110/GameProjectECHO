using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectEcho
{
    /// <summary>
    /// Hoạt ảnh thủ tục cho model TAY. Model là mesh tĩnh nhiều mảnh (chưa có xương), nên thân được nhún, nghiêng,
    /// co khi cúi và hai cánh tay vung quanh vai theo nhịp bước. Tay được nhận ra từ hình học (mảnh hẹp hai bên thân,
    /// ở tầm hông–vai) nên xuất lại model từ Blender với tên khác vẫn chạy. Gắn trên node "Visual" con của TAY.
    /// </summary>
    public sealed class TayAvatar : MonoBehaviour
    {
        public PlayerController player;
        [Tooltip("Instance model nằm dưới Visual.")] public Transform model;
        [Header("Bước chân")]
        [Min(0.2f)] public float walkStride = 1.3f;
        [Min(0.2f)] public float sprintStride = 1.75f;
        [Min(0.2f)] public float crouchStride = 0.85f;
        [Range(0, 0.2f)] public float bobHeight = 0.035f;
        [Range(0, 60)] public float armSwing = 22;
        [Header("Tư thế")]
        [Range(0, 20)] public float runLean = 8;
        [Range(0.5f, 1)] public float crouchSquash = 0.66f;
        [Range(0, 30)] public float crouchLean = 11;
        [Range(0, 30)] public float dashLean = 15;
        [Tooltip("Camera gần đầu hơn khoảng này thì chỉ còn đổ bóng, model không che màn hình.")]
        [Min(0)] public float cameraClearance = 0.55f;

        private sealed class Arm
        {
            public Transform[] parts;
            public Vector3[] position;
            public Quaternion[] rotation;
            public Vector3[] pivot;
            public Vector3[] axis;
            public float angle;
        }

        private readonly List<Arm> arms = new();
        private Renderer[] renderers;
        private ShadowCastingMode[] shadowModes;
        private bool tucked;
        private Camera view;
        private Vector3 lastPosition;
        private float lastYaw;
        private float phase, speed, crouch, air, dash, lean, roll, landing, breath;
        private bool wasGrounded = true;
        private float fallSpeed;

        public int ArmCount => arms.Count;

        private void Awake()
        {
            if (player == null) player = GetComponentInParent<PlayerController>();
            if (model == null && transform.childCount > 0) model = transform.GetChild(0);
            renderers = GetComponentsInChildren<Renderer>(true);
            shadowModes = new ShadowCastingMode[renderers.Length];
            FindArms();
        }

        private void OnEnable()
        {
            var body = player != null ? player.transform : transform;
            lastPosition = body.position;
            lastYaw = body.eulerAngles.y;
        }

        /// <summary>
        /// Gom các mảnh tay theo khung Visual (x ngang, y lên, z trước): tâm lệch hẳn sang một bên, bề ngang hẹp, nằm
        /// giữa hông và vai. Mỗi bên tay xoay quanh điểm vai = đỉnh nhóm mảnh.
        /// </summary>
        private void FindArms()
        {
            if (model == null) return;
            var filters = model.GetComponentsInChildren<MeshFilter>(true);
            var boxes = new List<(Transform part, Bounds box)>();
            foreach (var filter in filters)
                if (filter.sharedMesh != null)
                    boxes.Add((filter.transform, Transform(filter.sharedMesh.bounds, transform.worldToLocalMatrix * filter.transform.localToWorldMatrix)));
            if (boxes.Count == 0) return;
            Bounds all = boxes[0].box;
            foreach (var b in boxes) all.Encapsulate(b.box);
            float height = all.size.y, floor = all.min.y;
            foreach (int side in new[] { -1, 1 })
            {
                var group = boxes.FindAll(b => b.box.center.x * side > 0.12f * height && b.box.size.x < 0.22f * height &&
                    b.box.center.y - floor > 0.25f * height && b.box.max.y - floor < 0.7f * height);
                if (group.Count == 0) continue;
                Bounds span = group[0].box;
                foreach (var g in group) span.Encapsulate(g.box);
                // Cha của các mảnh là con của Visual nên vai và trục quy đổi một lần là dùng mãi, kể cả khi Visual co giãn.
                var shoulder = transform.TransformPoint(new Vector3(span.center.x, span.max.y - 0.03f * height, span.center.z));
                var parts = group.ConvertAll(g => g.part).ToArray();
                arms.Add(new Arm
                {
                    parts = parts,
                    position = System.Array.ConvertAll(parts, p => p.localPosition),
                    rotation = System.Array.ConvertAll(parts, p => p.localRotation),
                    pivot = System.Array.ConvertAll(parts, p => p.parent.InverseTransformPoint(shoulder)),
                    axis = System.Array.ConvertAll(parts, p => p.parent.InverseTransformDirection(transform.right).normalized),
                });
            }
        }

        private void LateUpdate()
        {
            if (player == null) return;
            float delta = Time.deltaTime;
            if (delta <= 0) return;
            Vector3 position = player.transform.position;
            Vector3 velocity = (position - lastPosition) / delta;
            lastPosition = position;
            if (velocity.sqrMagnitude > 900) velocity = Vector3.zero; // dịch chuyển tức thời (respawn, vào chỗ nấp)
            Vector3 flat = Vector3.ProjectOnPlane(velocity, Vector3.up);
            float yaw = player.transform.eulerAngles.y;
            float turn = Mathf.DeltaAngle(lastYaw, yaw) / delta;
            lastYaw = yaw;

            bool grounded = player.Grounded || player.IsHiding;
            bool crouched = player.IsCrouched || player.IsHiding;
            speed = Smooth(speed, flat.magnitude, 12, delta);
            crouch = Smooth(crouch, crouched ? 1 : 0, 10, delta);
            air = Smooth(air, grounded ? 0 : 1, 9, delta);
            dash = Smooth(dash, player.IsDashing ? 1 : 0, 22, delta);
            if (!grounded) fallSpeed = Mathf.Min(fallSpeed, velocity.y);
            if (grounded && !wasGrounded) landing = Mathf.Clamp01(-fallSpeed / 9);
            if (grounded) fallSpeed = 0;
            wasGrounded = grounded;
            landing = Smooth(landing, 0, 7, delta);

            var tuning = player.tuning;
            float walk = tuning != null ? tuning.forwardSpeed : 3.5f, run = tuning != null ? tuning.sprintSpeed : 5;
            float sprint = Mathf.Clamp01(Mathf.InverseLerp(walk, run, speed));
            float moving = Mathf.Clamp01(speed / 1.2f) * (1 - air);
            float stride = Mathf.Lerp(Mathf.Lerp(walkStride, sprintStride, sprint), crouchStride, crouch);
            if (grounded) phase = Mathf.Repeat(phase + flat.magnitude * delta / stride * 2 * Mathf.PI, 2 * Mathf.PI);
            else phase = Mathf.MoveTowards(phase, Mathf.Round(phase / Mathf.PI) * Mathf.PI, 4 * delta);

            Vector3 local = player.transform.InverseTransformDirection(flat);
            lean = Smooth(lean, Mathf.Clamp(local.z / Mathf.Max(run, 0.1f), -0.5f, 1.2f) * runLean, 8, delta);
            roll = Smooth(roll, Mathf.Clamp(-turn * 0.025f * moving, -8, 8) - Mathf.Clamp(local.x * 0.9f, -4, 4), 8, delta);
            breath = Mathf.Repeat(breath + delta * 1.6f, 2 * Mathf.PI);

            float step = Mathf.Sin(phase);
            float bob = bobHeight * Mathf.Lerp(1, 1.7f, sprint) * Mathf.Lerp(1, 0.5f, crouch) * moving * Mathf.Abs(step);
            float sway = step * Mathf.Lerp(2.5f, 3.5f, sprint) * moving;
            float squash = Mathf.Lerp(1, crouchSquash, crouch) * (1 - 0.08f * landing) * (1 + 0.04f * air) *
                           (1 + 0.006f * Mathf.Sin(breath) * (1 - moving));
            float widen = 1 + 0.06f * crouch + 0.05f * landing;
            transform.localPosition = new Vector3(0, bob, 0);
            transform.localRotation = Quaternion.Euler(lean + crouch * crouchLean + dash * dashLean, 0, sway + roll);
            transform.localScale = new Vector3(widen, squash, widen * (1 + 0.1f * dash));

            float swing = step * armSwing * Mathf.Lerp(1, 1.7f, sprint) * Mathf.Lerp(1, 0.5f, crouch) * moving;
            float pose = -18 * crouch + 50 * dash - 28 * air;
            for (int i = 0; i < arms.Count; i++)
            {
                float target = (i % 2 == 0 ? swing : -swing) + pose;
                arms[i].angle = Smooth(arms[i].angle, target, 18, delta);
                Pose(arms[i]);
            }
            TuckForCamera();
        }

        /// <summary>Xoay các mảnh tay quanh vai, góc dương đưa bàn tay ra sau.</summary>
        private static void Pose(Arm arm)
        {
            for (int i = 0; i < arm.parts.Length; i++)
            {
                Quaternion rotation = Quaternion.AngleAxis(arm.angle, arm.axis[i]);
                arm.parts[i].localPosition = arm.pivot[i] + rotation * (arm.position[i] - arm.pivot[i]);
                arm.parts[i].localRotation = rotation * arm.rotation[i];
            }
        }

        private void TuckForCamera()
        {
            if (view == null) view = Camera.main;
            if (view == null) return;
            bool close = (view.transform.position - player.Eye).sqrMagnitude < cameraClearance * cameraClearance;
            if (close == tucked) return;
            tucked = close;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                if (close)
                {
                    shadowModes[i] = renderers[i].shadowCastingMode;
                    renderers[i].shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
                else renderers[i].shadowCastingMode = shadowModes[i];
            }
        }

        private static float Smooth(float current, float target, float rate, float delta) =>
            Mathf.Lerp(current, target, 1 - Mathf.Exp(-rate * delta));

        private static Bounds Transform(Bounds local, Matrix4x4 matrix)
        {
            var result = new Bounds(matrix.MultiplyPoint3x4(local.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1);
                result.Encapsulate(matrix.MultiplyPoint3x4(local.min + Vector3.Scale(local.size, corner)));
            }
            return result;
        }
    }
}
