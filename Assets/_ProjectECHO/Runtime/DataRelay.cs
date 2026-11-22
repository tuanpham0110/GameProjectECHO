using UnityEngine;

namespace ProjectEcho
{
    public sealed class DataRelay : Interactable
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly Color Waiting = new(0.05f, 0.22f, 0.24f);
        private static readonly Color Ready = new(0.13f, 1, 0.88f);
        private static readonly Color Done = new(0.18f, 1, 0.44f);
        [Tooltip("Màn hình đổi màu theo trạng thái; để trống sẽ dùng con tên \"Screen\".")] public Renderer screen;
        private MaterialPropertyBlock block;
        private MissionStage stage;
        private bool missionKnown;

        public override string Prompt => !missionKnown ? "Data Relay" : stage switch
        {
            MissionStage.Collect => "Data Relay · cần đủ dữ liệu",
            MissionStage.Relay => "Bắt đầu truyền dữ liệu",
            MissionStage.Transmit => "Data Relay · đang truyền",
            _ => "Data Relay · đã truyền xong"
        };

        public override void Interact(PlayerController player) => player.Level.game.BeginRelay();

        public void SetStage(MissionStage value)
        {
            missionKnown = true;
            stage = value;
            Apply(1);
        }

        private void Awake()
        {
            if (screen == null) screen = transform.Find("Screen")?.GetComponent<Renderer>();
        }

        private void Update()
        {
            if (missionKnown && stage == MissionStage.Transmit) Apply(0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.time * 3.2f)));
        }

        private void Apply(float pulse)
        {
            if (screen == null) return;
            block ??= new MaterialPropertyBlock();
            Color color = stage switch
            {
                MissionStage.Collect => Waiting,
                MissionStage.Exit => Done,
                _ => Ready
            };
            float glow = stage == MissionStage.Collect ? 0.3f : stage == MissionStage.Relay ? 1.6f : 1.4f * pulse;
            block.SetColor(BaseColor, color);
            block.SetColor(EmissionColor, color * glow);
            screen.SetPropertyBlock(block);
        }
    }
}
