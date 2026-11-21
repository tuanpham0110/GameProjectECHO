using UnityEngine;

namespace ProjectEcho
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        public LevelManager Level { get; set; }
        public PlayerInput input;
        public PlayerTuning tuning;
        public InteractionSystem interaction;
        public PlayerEquipment equipment;
        public ThirdPersonCamera view;
        public Renderer body;
        private CharacterController motor;
        private float standingHeight;
        private float verticalSpeed;
        private float nextFootstep;
        private Vector3 dashVelocity;
        private float dashRemaining;
        private HideSpot hiding;
        private float hideRemaining;
        private bool exitingHide;
        public bool IsSafelyHidden { get; private set; }
        public bool IsHiding => hiding != null;
        public HideSpot CurrentHide => hiding;
        public float HiddenSeconds { get; private set; }
        public bool IsCrouched => motor.height < standingHeight - 0.01f;
        public bool Grounded => motor.isGrounded;
        public Vector3 Eye => transform.position + Vector3.up * (motor.height - 0.22f);
        public string HidePrompt { get; private set; }
        private void Awake()
        {
            motor = GetComponent<CharacterController>();
            standingHeight = motor.height;
        }

        public void Bind(LevelManager level, ThirdPersonCamera cameraView)
        {
            Level = level;
            view = cameraView;
        }

        private void Start()
        {
            if (Level != null) view.ResetPose(Level.playerSpawn.rotation);
        }

        private void Update()
        {
            if (Level == null || !Level.game.CanAct)
            {
                interaction.Clear();
                return;
            }

            float delta = Time.deltaTime;
            if (IsHiding)
            {
                TickHide(delta);
                equipment.Tick(delta, false);
                return;
            }

            if (input.Crouch.IsPressed()) SetHeight(tuning.crouchHeight);
            else if (IsCrouched && HasStandingClearance(transform.position)) SetHeight(standingHeight);
            equipment.Tick(delta, true);
            Move(delta);
            interaction.Refresh();
            if (transform.position.y < Level.playerSpawn.position.y - 15) Level.RespawnPlayer();
        }

        private bool HasStandingClearance(Vector3 feet)
        {
            float radius = motor.radius - motor.skinWidth;
            return !Physics.CheckCapsule(feet + Vector3.up * (motor.height - radius),
                feet + Vector3.up * (standingHeight - radius), radius, Level.worldMask, QueryTriggerInteraction.Ignore);
        }

        private void SetHeight(float height)
        {
            motor.height = height;
            motor.center = Vector3.up * (height * 0.5f);
            if (body == null) return;
            body.transform.localPosition = motor.center;
            Vector3 scale = body.transform.localScale;
            scale.y = height * 0.5f;
            body.transform.localScale = scale;
        }

        private void Move(float delta)
        {
            if (Grounded && verticalSpeed < 0) verticalSpeed = -2;
            Vector2 move = input.Move.ReadValue<Vector2>();
            bool sprint = !IsCrouched && input.Sprint.IsPressed() && move.y > 0;
            float longitudinal = move.y >= 0 ? tuning.forwardSpeed : tuning.backSpeed;
            if (sprint) longitudinal = tuning.sprintSpeed;
            float lateral = IsCrouched ? tuning.crouchSpeed : tuning.strafeSpeed;
            if (IsCrouched) longitudinal = tuning.crouchSpeed;
            Vector3 horizontal = view.Heading * new Vector3(move.x * lateral, 0, move.y * longitudinal);
            if (horizontal.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(horizontal), 780 * delta);

            if (Grounded && !IsCrouched && input.Jump.WasPressedThisFrame() && dashRemaining <= 0)
            {
                verticalSpeed = Mathf.Sqrt(2 * tuning.gravity * tuning.jumpHeight);
                Level.EmitNoise(transform.position, 7);
            }
            verticalSpeed -= tuning.gravity * delta;
            if (dashRemaining > 0)
            {
                float dashStep = Mathf.Min(delta, dashRemaining);
                dashRemaining -= dashStep;
                motor.Move(dashVelocity * dashStep + Vector3.up * (verticalSpeed * dashStep));
                return;
            }

            Vector3 before = transform.position;
            ApplyMovement(horizontal * equipment.SpeedMultiplier * delta + Vector3.up * (verticalSpeed * delta));
            Vector3 traveled = transform.position - before;
            traveled.y = 0;
            if (Grounded && traveled.sqrMagnitude > 0.00005f && Time.time >= nextFootstep)
            {
                nextFootstep = Time.time + (sprint ? 0.29f : 0.48f);
                Level.EmitNoise(transform.position, IsCrouched ? 2 : sprint ? 13 : 6);
            }
        }

        public CollisionFlags ApplyMovement(Vector3 displacement)
        {
            var flags = motor.Move(displacement);
            if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
            return flags;
        }

        public bool TryDash()
        {
            if (!Grounded || IsCrouched || dashRemaining > 0) return false;
            Vector3 direction = view.Heading * Vector3.forward;
            float radius = motor.radius - motor.skinWidth;
            Vector3 bottom = transform.position + Vector3.up * (radius + 0.08f);
            Vector3 top = transform.position + Vector3.up * (standingHeight - radius);
            if (Physics.CapsuleCast(bottom, top, radius, direction, out var hit, tuning.dashDistance,
                    Level.worldMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 lift = Vector3.up * tuning.dashVaultClearance;
                Vector3 landing = transform.position + direction * tuning.dashDistance;
                if (hit.distance < motor.radius ||
                    Physics.CapsuleCast(bottom, top, radius, Vector3.up, lift.magnitude, Level.worldMask, QueryTriggerInteraction.Ignore) ||
                    Physics.CapsuleCast(bottom + lift, top + lift, radius, direction, tuning.dashDistance, Level.worldMask, QueryTriggerInteraction.Ignore) ||
                    !Physics.Raycast(landing + lift, Vector3.down, lift.magnitude + 0.25f, Level.worldMask, QueryTriggerInteraction.Ignore))
                    return false;
                motor.Move(lift);
                verticalSpeed = 0;
            }
            dashRemaining = tuning.dashDuration;
            dashVelocity = direction * (tuning.dashDistance / tuning.dashDuration);
            Level.EmitNoise(transform.position, 12);
            return true;
        }

        public void RequestHide(HideSpot spot)
        {
            if (IsHiding || !Grounded || spot.inside == null || spot.outside == null) return;
            hiding = spot;
            hideRemaining = Mathf.Max(0.001f, spot.entryDelay);
            HidePrompt = "Đang vào chỗ nấp…";
        }

        private void TickHide(float delta)
        {
            if (hideRemaining > 0)
            {
                hideRemaining -= delta;
                if (hideRemaining > 0) return;
                if (exitingHide)
                {
                    Place(hiding.outside.position, hiding.outside.rotation);
                    ClearHiding();
                    return;
                }
                IsSafelyHidden = hiding.absolute && !Level.hunter.IsTrackingPlayer;
                Place(hiding.inside.position, hiding.inside.rotation);
                SetHeight(tuning.crouchHeight);
                HidePrompt = "C · Ra khỏi chỗ nấp";
                HiddenSeconds = 0;
                return;
            }
            if (!exitingHide) HiddenSeconds += delta;
            if (!exitingHide && input.Hide.WasPressedThisFrame())
            {
                exitingHide = true;
                IsSafelyHidden = false;
                hideRemaining = Mathf.Max(0.001f, hiding.exitDelay);
                HidePrompt = "Đang rời chỗ nấp…";
            }
        }

        public void OnCaptured()
        {
            ClearHiding();
            equipment.OnCaptured();
        }

        public void ResetRun() => equipment.ResetRun();

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            ClearHiding();
            SetHeight(standingHeight);
            Place(position, rotation);
            view.ResetPose(rotation);
        }

        private void Place(Vector3 position, Quaternion rotation)
        {
            motor.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            motor.enabled = true;
            verticalSpeed = 0;
            dashRemaining = 0;
        }

        private void ClearHiding()
        {
            hiding = null;
            HiddenSeconds = 0;
            HidePrompt = null;
            IsSafelyHidden = false;
            exitingHide = false;
            hideRemaining = 0;
        }
    }
}
