using UnityEngine;

namespace ProjectEcho
{
    public sealed class SupportDevice : MonoBehaviour
    {
        private LevelManager level;
        private PlayerTuning tuning;
        private SupportItem item;
        private Vector3 velocity;
        private float fuse;

        public static void Spawn(LevelManager level, PlayerTuning tuning, SupportItem item, Vector3 position, Vector3 velocity)
        {
            var node = GameObject.CreatePrimitive(item == SupportItem.Emp ? PrimitiveType.Cylinder : PrimitiveType.Sphere);
            node.name = item == SupportItem.Emp ? "EMP Mine" : "Stun Grenade";
            node.layer = 2;
            node.GetComponent<Collider>().enabled = false;
            node.transform.SetPositionAndRotation(position, Quaternion.identity);
            node.transform.localScale = item == SupportItem.Emp ? new Vector3(0.33f, 0.07f, 0.33f) : Vector3.one * 0.21f;
            var tint = new MaterialPropertyBlock();
            tint.SetColor("_BaseColor", item == SupportItem.Emp ? new Color(0.16f, 0.86f, 0.95f) : new Color(1, 0.56f, 0.17f));
            node.GetComponent<Renderer>().SetPropertyBlock(tint);
            var device = node.AddComponent<SupportDevice>();
            device.level = level;
            device.tuning = tuning;
            device.item = item;
            device.velocity = velocity;
            device.fuse = 0.9f;
        }

        private void Update()
        {
            if (!level.game.IsRunning) return;
            var hunter = level.hunter;
            Vector3 position = transform.position;
            if (item == SupportItem.Emp)
            {
                if (InRange(hunter, position, tuning.empRadius))
                {
                    hunter.ApplyEmp(tuning.empSeconds);
                    Destroy(gameObject);
                }
                return;
            }

            float delta = Time.deltaTime;
            fuse -= delta;
            velocity += Physics.gravity * delta;
            Vector3 step = velocity * delta;
            if (Physics.SphereCast(position, 0.1f, step.normalized, out var hit, step.magnitude, level.worldMask, QueryTriggerInteraction.Ignore))
            {
                position += step.normalized * Mathf.Max(0, hit.distance - 0.015f);
                velocity = Vector3.Reflect(velocity, hit.normal) * 0.42f;
            }
            else position += step;
            transform.position = position;
            if (fuse > 0) return;
            if (InRange(hunter, position, tuning.stunRadius))
                hunter.ApplyStun(tuning.stunSeconds, tuning.enrageSeconds, tuning.enrageBonus);
            level.EmitNoise(position, 15);
            Destroy(gameObject);
        }

        private bool InRange(HunterBrain hunter, Vector3 origin, float radius)
        {
            Vector3 target = hunter.transform.position + Vector3.up * 0.6f;
            return (target - origin).sqrMagnitude <= radius * radius &&
                !Physics.Linecast(origin, target, level.worldMask, QueryTriggerInteraction.Ignore);
        }
    }
}
