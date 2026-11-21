using UnityEngine;
using UnityEngine.AI;

namespace ProjectEcho
{
    public sealed class OfficeDoor : Interactable
    {
        [Tooltip("Collider chặn khi cửa đóng.")] public GameObject panel;
        [Tooltip("Mesh hiển thị cánh cửa (ví dụ cánh cửa trong FBX).")] public GameObject[] visuals = System.Array.Empty<GameObject>();
        public NavMeshObstacle obstacle;
        public Collider Blocker => panel != null && panel.activeInHierarchy && panel.TryGetComponent<Collider>(out var c) ? c : null;
        public bool locked;
        public bool startsOpen = true;
        [Tooltip("Gameplay Specification tr. 21: đóng cửa cản Hunter 3 giây.")] [Min(0)] public float hunterOpenDelay = 3;
        [Tooltip("Xoay cánh cửa quanh bản lề khi mở; tắt để ẩn cánh cửa khi mở.")] public bool swingVisuals = true;
        [Range(10, 120)] public float swingAngle = 95;
        [Min(0.01f)] public float swingSeconds = 0.35f;
        public bool IsOpen { get; private set; }
        public override string Prompt => locked ? "Cửa khóa" : IsOpen ? "Đóng cửa" : "Mở cửa";
        public override Vector3 Point => transform.position + Vector3.up;
        private float openAt;
        private Vector3[] closedPositions;
        private Quaternion[] closedRotations;
        private Vector3[] hinges;
        private float[] angles;
        private float openAmount;
        private float openTarget;

        public override void Interact(PlayerController player)
        {
            if (locked)
            {
                player.Level.game.Notify("Cửa đang khóa.");
                return;
            }
            if (IsOpen && IsBlockedBy(player.transform.position)) return;
            SetOpen(!IsOpen, false);
            player.Level.EmitNoise(transform.position, 6);
        }

        /// <summary>Hunter yêu cầu mở cửa; trả về true khi cửa đang được xử lý (chưa mở xong) hoặc đã mở.</summary>
        public bool RequestHunterOpen()
        {
            if (locked) return false;
            if (IsOpen) return true;
            if (openAt <= 0) openAt = Time.time + hunterOpenDelay;
            if (Time.time >= openAt) SetOpen(true, false);
            return true;
        }

        public void CancelHunterOpen() => openAt = 0;
        public override void ResetRun() => SetOpen(startsOpen, true);
        private void Start() => SetOpen(startsOpen, true);

        private void Update()
        {
            if (!swingVisuals || hinges == null || Mathf.Approximately(openAmount, openTarget)) return;
            openAmount = Mathf.MoveTowards(openAmount, openTarget, Time.deltaTime / swingSeconds);
            ApplySwing(Mathf.SmoothStep(0, 1, openAmount));
        }

        private bool IsBlockedBy(Vector3 feet)
        {
            if (panel == null || !panel.TryGetComponent<Collider>(out var blocker)) return (feet - transform.position).sqrMagnitude < 1;
            Vector3 body = feet + Vector3.up * 0.9f;
            Vector3 closest = blocker.bounds.ClosestPoint(body);
            return (closest - body).sqrMagnitude < 0.45f * 0.45f;
        }

        private void SetOpen(bool open, bool immediate)
        {
            openAt = 0;
            IsOpen = open;
            if (panel != null) panel.SetActive(!open);
            // Chỉ cửa khóa mới cắt NavMesh để Hunter tìm đường khác; cửa đóng chưa khóa vẫn nằm trên tuyến đường
            // và Hunter dừng lại mở cửa (HunterBrain.DetectDoorAhead).
            if (obstacle != null) obstacle.enabled = !open && locked;
            if (!swingVisuals)
            {
                foreach (var visual in visuals) if (visual != null) visual.SetActive(!open);
                return;
            }
            CacheHinges();
            openTarget = open ? 1 : 0;
            if (!immediate && Application.isPlaying) return;
            openAmount = openTarget;
            ApplySwing(openAmount);
        }

        /// <summary>Bản lề nằm ở mép ngoài mỗi cánh (xa tâm khung cửa nhất); hai cánh mở về cùng một phía tường.</summary>
        private void CacheHinges()
        {
            if (hinges != null) return;
            int count = visuals.Length;
            closedPositions = new Vector3[count];
            closedRotations = new Quaternion[count];
            hinges = new Vector3[count];
            angles = new float[count];
            Vector3 along = transform.right;
            if (panel != null && panel.TryGetComponent<BoxCollider>(out var box) && box.size.z > box.size.x) along = transform.forward;
            Vector3 normal = Vector3.Cross(Vector3.up, along);
            for (int i = 0; i < count; i++)
            {
                var visual = visuals[i];
                if (visual == null) continue;
                closedPositions[i] = visual.transform.position;
                closedRotations[i] = visual.transform.rotation;
                var renderers = visual.GetComponentsInChildren<Renderer>(true);
                Bounds bounds = new(visual.transform.position, Vector3.zero);
                if (renderers.Length > 0)
                {
                    bounds = renderers[0].bounds;
                    foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                }
                float side = Mathf.Sign(Vector3.Dot(bounds.center - transform.position, along));
                if (Mathf.Abs(Vector3.Dot(bounds.center - transform.position, along)) < 0.05f) side = 1;
                float half = Mathf.Abs(Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(along.x), 0, Mathf.Abs(along.z))));
                hinges[i] = bounds.center + along * (side * half);
                Vector3 leaf = bounds.center - hinges[i];
                leaf.y = 0;
                angles[i] = Vector3.Dot(Quaternion.Euler(0, swingAngle, 0) * leaf, normal) >= 0 ? swingAngle : -swingAngle;
            }
        }

        private void ApplySwing(float amount)
        {
            if (hinges == null) return;
            for (int i = 0; i < visuals.Length; i++)
            {
                var visual = visuals[i];
                if (visual == null) continue;
                visual.transform.SetPositionAndRotation(closedPositions[i], closedRotations[i]);
                if (amount > 0) visual.transform.RotateAround(hinges[i], Vector3.up, angles[i] * amount);
            }
        }
    }
}
