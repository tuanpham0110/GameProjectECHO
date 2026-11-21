using UnityEngine;

namespace ProjectEcho
{
    public sealed class PlayerEquipment : MonoBehaviour
    {
        public PlayerController player;
        public Light scannerLight;
        public GameObject sensorMarker;
        private float active;
        private float cooldown;
        private float sensorNext;
        private float scannerTime;
        private bool scannerAlert;
        public float SkillSeconds => active > 0 ? active : cooldown;
        public bool SkillActive => active > 0;
        public bool ScannerOn => scannerLight != null && scannerLight.enabled;
        public SupportItem SelectedItem { get; private set; }
        public float SpeedMultiplier => active > 0 && player.Level.game.Equipped == Skill.BurstSprint ? player.tuning.burstMultiplier : 1;
        public Vector3? SensorPosition { get; private set; }

        public void Tick(float delta, bool canUse)
        {
            if (active > 0)
            {
                active = Mathf.Max(0, active - delta);
                if (active <= 0) EndActive();
                else if (player.Level.game.Equipped == Skill.HunterSensor)
                {
                    sensorNext -= delta;
                    if (sensorNext <= 0) ReadSensor();
                }
            }
            else cooldown = Mathf.Max(0, cooldown - delta);
            if (!canUse) return;

            var input = player.input;
            if (input.Ability.WasPressedThisFrame()) Activate();
            if (input.SelectEmp.WasPressedThisFrame()) SelectedItem = SupportItem.Emp;
            if (input.SelectStun.WasPressedThisFrame()) SelectedItem = SupportItem.Stun;
            if (input.UseItem.WasPressedThisFrame()) UseItem();
            if (input.Scanner.WasPressedThisFrame()) SetScanner(!ScannerOn);
            if (ScannerOn)
            {
                scannerLight.transform.rotation = player.view.transform.rotation;
                RefreshScanner(true);
                scannerTime += delta;
                if (scannerTime >= player.tuning.scannerExposure && !scannerAlert)
                {
                    scannerAlert = true;
                    player.Level.hunter.ReceiveScannerSignal(player.transform.position, player.tuning.scannerSignalRadius);
                    player.Level.game.Notify("Scanner đã phát tín hiệu điện từ.");
                }
            }
        }

        private void Activate()
        {
            if (active > 0 || cooldown > 0) return;
            var tuning = player.tuning;
            switch (player.Level.game.Equipped)
            {
                case Skill.BurstSprint when player.Grounded && !player.IsCrouched:
                    active = tuning.burstDuration;
                    break;
                case Skill.Dash when player.TryDash():
                    cooldown = tuning.dashCooldown;
                    break;
                case Skill.HunterSensor:
                    active = tuning.sensorDuration;
                    ReadSensor();
                    break;
            }
        }

        private void ReadSensor()
        {
            sensorNext = player.tuning.sensorInterval;
            Vector3 position = player.Level.hunter.transform.position;
            SensorPosition = (position - player.transform.position).sqrMagnitude <= Mathf.Pow(player.tuning.sensorRange, 2) ? position : null;
            if (sensorMarker == null) return;
            sensorMarker.SetActive(SensorPosition.HasValue);
            if (SensorPosition.HasValue) sensorMarker.transform.position = position + Vector3.up * 2.25f;
        }

        private void EndActive()
        {
            active = 0;
            SensorPosition = null;
            if (sensorMarker != null) sensorMarker.SetActive(false);
            cooldown = player.Level.game.Equipped switch
            {
                Skill.BurstSprint => player.tuning.burstCooldown,
                Skill.HunterSensor => player.tuning.sensorCooldown,
                _ => cooldown
            };
        }

        private void SetScanner(bool enabled)
        {
            scannerTime = 0;
            scannerAlert = false;
            if (scannerLight != null) scannerLight.enabled = enabled;
            if (!enabled) RefreshScanner(false);
        }

        private void RefreshScanner(bool enabled)
        {
            foreach (var interactable in player.Level.interactions)
            {
                if (interactable is not DataPickup data) continue;
                data.SetScanned(enabled && data.isActiveAndEnabled &&
                    HunterPerception.HasSight(scannerLight.transform.position, scannerLight.transform.forward,
                        data.Point, scannerLight.range, scannerLight.spotAngle, player.Level.worldMask));
            }
        }

        private void UseItem()
        {
            var level = player.Level;
            var inventory = level.game.Inventory;
            if (inventory.Count(SelectedItem) == 0)
            {
                level.game.Notify("Hết vật phẩm đang chọn.");
                return;
            }
            Vector3 direction = player.view.Heading * Vector3.forward;
            Vector3 start = player.Eye + direction * 0.5f;
            if (SelectedItem == SupportItem.Emp)
            {
                if (Physics.Linecast(player.Eye, start, level.worldMask, QueryTriggerInteraction.Ignore) ||
                    !Physics.Raycast(start, Vector3.down, out var ground, 2.5f, level.worldMask, QueryTriggerInteraction.Ignore) ||
                    ground.normal.y < 0.6f)
                {
                    level.game.Notify("Không thể đặt EMP tại đây.");
                    return;
                }
                SupportDevice.Spawn(level, player.tuning, SelectedItem, ground.point + ground.normal * 0.08f, Vector3.zero);
            }
            else
            {
                start = player.Eye;
                SupportDevice.Spawn(level, player.tuning, SelectedItem, start, direction * 7 + Vector3.up * 3);
            }
            inventory.Use(SelectedItem);
        }

        public void OnCaptured()
        {
            if (active > 0) EndActive();
            SetScanner(false);
        }

        public void ResetRun()
        {
            EndActive();
            cooldown = 0;
            SelectedItem = SupportItem.Emp;
            SetScanner(false);
        }
    }
}
