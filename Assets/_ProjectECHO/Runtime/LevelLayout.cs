using System;
using UnityEngine;

namespace ProjectEcho
{
    public enum LevelEnvironment { Blockout, Model }

    /// <summary>
    /// Bố trí gameplay của một Level. Công cụ Editor đọc asset này để dựng scene, nên designer chỉnh vị trí tại đây
    /// rồi chạy lại Setup Gameplay thay vì sửa code. Mọi tọa độ là world space, Y = cao độ sàn mong muốn.
    /// </summary>
    [CreateAssetMenu(menuName = "Project ECHO/Level Layout")]
    public sealed class LevelLayout : ScriptableObject
    {
        [Serializable]
        public struct Pose
        {
            public Vector3 position;
            [Range(-360, 360)] public float yaw;
            public Pose(float x, float z, float yaw) { position = new Vector3(x, 0, z); this.yaw = yaw; }
            public Quaternion Rotation => Quaternion.Euler(0, yaw, 0);
        }

        [Serializable]
        public struct DataPoint
        {
            public string id;
            public string displayName;
            public Vector3 position;
        }

        [Serializable]
        public struct SupportPoint
        {
            public SupportItem item;
            public Vector3 position;
        }

        [Serializable]
        public struct HidePoint
        {
            public Vector3 inside;
            public Vector3 outside;
            public bool absolute;
            [Tooltip("Dựng khung tủ placeholder khi môi trường không có sẵn tủ.")] public bool buildLocker;
        }

        [Serializable]
        public struct DoorPoint
        {
            public string name;
            [Tooltip("Đường dẫn các cánh cửa trong model; để trống để dùng tấm cửa placeholder.")] public string[] modelPaths;
            public Vector3 center;
            public Vector3 size;
            public bool startsOpen;
            public bool locked;
        }

        public LevelEnvironment environment = LevelEnvironment.Model;
        [Tooltip("Thư mục chứa FBX môi trường, dùng khi environment = Model.")] public string modelFolder = "Assets/_ProjectECHO/Art/Environments/Office";
        public string modelName = "Map office (Tong hop)";
        [Tooltip("Vùng sàn XZ có thể đi; công cụ dựng tường vô hình quanh vùng này.")] public Rect walkBounds;
        [Min(1)] public float boundaryHeight = 4.7f;
        [Tooltip("Mesh nhỏ hơn giá trị này (cạnh lớn nhất, m) không sinh collider.")] [Min(0)] public float minColliderSize = 0.3f;
        [Tooltip("Mesh có đáy cao hơn mức này không sinh collider, trừ mesh rất lớn như trần.")] public float maxColliderBottom = 2.3f;
        [Tooltip("Chuỗi con trong đường dẫn mesh bị loại khỏi collision (đèn, decal...).")] public string[] collisionExclusions = Array.Empty<string>();

        public Pose playerSpawn;
        public Pose hunterSpawn;
        public Pose relay;
        public Pose exit;
        public DataPoint[] data = Array.Empty<DataPoint>();
        public SupportPoint[] supports = Array.Empty<SupportPoint>();
        public Vector3[] waypoints = Array.Empty<Vector3>();
        public HidePoint[] hideSpots = Array.Empty<HidePoint>();
        public DoorPoint[] doors = Array.Empty<DoorPoint>();
    }
}
