using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectEcho.Editor
{
    /// <summary>
    /// Thiết lập import cho model nhân vật trong Art/Characters: mesh tĩnh xuất từ Blender (không xương, không clip),
    /// vật liệu URP Lit sinh từ material description. Chỉ áp ở lần import đầu để chỉnh tay trong Inspector không bị ghi đè.
    /// </summary>
    internal sealed class CharacterModelImport : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(CharacterModels.Folder + "/") || !assetImporter.importSettingsMissing) return;
            var importer = (ModelImporter)assetImporter;
            importer.useFileScale = true;
            importer.globalScale = 1;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.importBlendShapes = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.addCollider = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }
    }

    /// <summary>Gắn model TAY vào prefab người chơi, thay capsule tạm của bản vertical slice.</summary>
    internal static class CharacterModels
    {
        internal const string Folder = OfficeLevelTools.Root + "/Art/Characters";
        internal const string TayModelPath = Folder + "/TAY/mainChar-finalModel.fbx";
        internal const string Visual = "Visual";

        /// <summary>Thay capsule "Body" bằng model. Trả về false khi chưa có FBX (giữ capsule để game vẫn chạy).</summary>
        internal static bool AttachTay(PlayerController player)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(TayModelPath);
            if (model == null)
            {
                Debug.LogWarning($"Project ECHO: Chưa có model TAY tại {TayModelPath}, giữ capsule tạm.");
                return false;
            }
            var root = player.transform;
            var old = root.Find(Visual);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            if (player.body != null) Object.DestroyImmediate(player.body.gameObject);
            player.body = null;

            var rig = new GameObject(Visual).transform;
            rig.SetParent(root, false);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, rig);
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0, FacingYaw(instance), 0));
            var avatar = rig.gameObject.AddComponent<TayAvatar>();
            avatar.player = player;
            avatar.model = instance.transform;
            var scanner = root.Find("Scanner");
            if (scanner != null) scanner.SetAsLastSibling();
            Bounds bounds = ModelBounds(instance.transform, rig);
            Debug.Log($"Project ECHO: Gắn model TAY {model.name} ({instance.GetComponentsInChildren<MeshFilter>(true).Length} mesh, " +
                      $"cao {bounds.size.y:0.00} m, rộng {bounds.size.x:0.00} m).");
            return true;
        }

        /// <summary>Prefab TAY đời trước dùng capsule: nâng cấp prefab gốc để scene và mọi Level dùng chung model.</summary>
        internal static void UpgradeTayPrefab(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(TayModelPath);
            if (asset == null || model == null) return;
            var existing = asset.transform.Find(Visual);
            if (existing != null && existing.TryGetComponent<TayAvatar>(out var current) && current.model != null &&
                PrefabUtility.GetCorrespondingObjectFromSource(current.model.gameObject) == model) return;
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (AttachTay(contents.GetComponent<PlayerController>())) PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>
        /// Model Blender không quy ước hướng mặt; mặt, mắt, tóc mái nằm phía trước đầu. Lấy trọng tâm các mảnh ở vùng
        /// đầu so với tâm thân: nằm phía -Z thì xoay 180° để TAY nhìn theo +Z như CharacterController.
        /// </summary>
        private static float FacingYaw(GameObject instance)
        {
            var parts = instance.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null)
                .Select(f => Transform(f.sharedMesh.bounds, instance.transform.worldToLocalMatrix * f.transform.localToWorldMatrix)).ToArray();
            if (parts.Length == 0) return 0;
            Bounds all = parts[0];
            foreach (var b in parts) all.Encapsulate(b);
            float headLine = all.min.y + all.size.y * 0.72f;
            var head = parts.Where(b => b.min.y > headLine && b.size.x < all.size.x * 0.5f).ToArray();
            if (head.Length == 0) return 0;
            float front = head.Average(b => b.center.z) - all.center.z;
            return front < 0 ? 180 : 0;
        }

        private static Bounds ModelBounds(Transform model, Transform space)
        {
            var filters = model.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).ToArray();
            Bounds all = Transform(filters[0].sharedMesh.bounds, space.worldToLocalMatrix * filters[0].transform.localToWorldMatrix);
            foreach (var f in filters) all.Encapsulate(Transform(f.sharedMesh.bounds, space.worldToLocalMatrix * f.transform.localToWorldMatrix));
            return all;
        }

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
