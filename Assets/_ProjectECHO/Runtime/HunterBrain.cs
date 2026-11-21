using UnityEngine;
using UnityEngine.AI;

namespace ProjectEcho
{
    public enum HunterState { Patrol, Investigate, Search, Chase, Ambush, Capture, Stunned, Recovery }

    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class HunterBrain : MonoBehaviour
    {
        public HunterProfile profile;
        public LevelManager level;
        public HunterState Current { get; private set; }
        public bool IsTrackingPlayer { get; private set; }
        public Vector3? LastKnownPosition { get; private set; }
        public int Threat => level.game.Mission.Threat;
        public string NavigationIssue { get; private set; }
        private NavMeshAgent agent;
        private NavMeshPath candidatePath;
        private Vector3? goal;
        private Vector3 searchOrigin;
        private float reaction;
        private float stateElapsed;
        private float scanTime;
        private float repath;
        private float stuck;
        private float stun;
        private float recovery;
        private float emp;
        private float enrage;
        private float enrageDuration;
        private float enrageBonus;
        private int waypoint;
        private int failedPaths;
        private Vector3 previous;
        private OfficeDoor pendingDoor;
        private Vector3? doorDestination;
        private HideSpot checkedHide;
        private Vector3 Eye => transform.position + Vector3.up * profile.eyeHeight;
        private int Obstacles => profile.obstructionMask & level.worldMask;
        private bool Arrived => goal.HasValue && !agent.pathPending &&
            (agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.25f) ||
             (!agent.hasPath && (transform.position - goal.Value).sqrMagnitude <= 0.16f));

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            candidatePath = new NavMeshPath();
        }

        private void Start()
        {
            ResetRun(level.hunterSpawn);
            if (agent.isOnNavMesh) Debug.Log("Project ECHO: Hunter navigation ready.", this);
        }

        private void OnEnable()
        {
            if (level != null) level.Noise += OnNoise;
        }

        private void OnDisable()
        {
            if (level != null) level.Noise -= OnNoise;
        }

        private void Update()
        {
            if (!level.game.CanAct)
            {
                IsTrackingPlayer = false;
                if (agent.isOnNavMesh) agent.isStopped = true;
                return;
            }

            float delta = Time.deltaTime;
            repath -= delta;
            if (!agent.isOnNavMesh && !TryRecoverNavigation()) return;
            emp = Mathf.Max(0, emp - delta);
            stateElapsed += delta;
            if (Current == HunterState.Stunned || Current == HunterState.Recovery)
            {
                TickDisabled(delta);
                return;
            }

            enrage = Mathf.Max(0, enrage - delta);
            HunterTier tier = profile.ForLevel(Threat);
            agent.speed = level.player.tuning.forwardSpeed * tier.speedRatio * (emp > 0 ? 0.5f : 1) * (enrage > 0 ? 1 + enrageBonus : 1);
            agent.isStopped = false;
            Observe(tier, delta);
            if (TryCapture()) return;
            CheckLongHide();
            DetectDoorAhead();
            if (TickDoor()) return;

            switch (Current)
            {
                case HunterState.Chase:
                    if (IsTrackingPlayer && LastKnownPosition.HasValue) MoveTo(LastKnownPosition.Value);
                    else if (LastKnownPosition.HasValue) Enter(HunterState.Investigate, LastKnownPosition);
                    break;
                case HunterState.Investigate:
                    if (Arrived || stateElapsed >= profile.investigateSeconds * 2 || !goal.HasValue)
                        Enter(HunterState.Search, LastKnownPosition);
                    break;
                case HunterState.Search:
                    if (stateElapsed >= tier.searchSeconds) ReturnToRoute();
                    else if (Arrived || !goal.HasValue) ScanArea();
                    break;
                case HunterState.Ambush:
                    if (!goal.HasValue) MoveTo(level.game.Mission.CanExit ? level.exit.transform.position : level.relay.transform.position);
                    if (Arrived || stateElapsed >= 8)
                    {
                        searchOrigin = goal ?? transform.position;
                        Enter(HunterState.Search);
                    }
                    break;
                default:
                    FollowRoute();
                    break;
            }
            TrackMovement(delta);
        }

        private void Observe(HunterTier tier, float delta)
        {
            var player = level.player;
            IsTrackingPlayer = !player.IsSafelyHidden &&
                HunterPerception.HasSight(Eye, transform.forward, player.Eye, tier.sight, tier.angle, Obstacles);
            if (!IsTrackingPlayer)
            {
                reaction = 0;
                return;
            }
            LastKnownPosition = player.transform.position;
            reaction += delta;
            if (reaction >= tier.reaction && Current != HunterState.Chase) Enter(HunterState.Chase, LastKnownPosition);
        }

        private bool TryCapture()
        {
            if (Current != HunterState.Chase || !IsTrackingPlayer || !level.game.CanBeCaptured) return false;
            if ((transform.position - level.player.transform.position).sqrMagnitude > profile.captureRange * profile.captureRange) return false;
            if (!level.game.TryCapture()) return false;
            Enter(HunterState.Capture);
            agent.isStopped = true;
            LastKnownPosition = null;
            IsTrackingPlayer = false;
            return true;
        }

        private void OnNoise(Vector3 position, float radius)
        {
            if (!level.game.CanAct || IsTrackingPlayer || Current is HunterState.Stunned or HunterState.Recovery) return;
            float range = Mathf.Min(profile.ForLevel(Threat).hearing, radius);
            if (!HunterPerception.IsClear(Eye, position + Vector3.up * 0.8f, Obstacles))
                range *= profile.occludedHearing;
            if (range > 0 && (transform.position - position).sqrMagnitude <= range * range) InvestigateSignal(position);
        }

        public void ReceiveScannerSignal(Vector3 position, float radius)
        {
            if (!level.game.CanAct || IsTrackingPlayer || Current is HunterState.Stunned or HunterState.Recovery) return;
            if ((transform.position - position).sqrMagnitude <= radius * radius) InvestigateSignal(position);
        }

        private void InvestigateSignal(Vector3 position)
        {
            Vector2 uncertainty = Random.insideUnitCircle * profile.signalUncertainty;
            LastKnownPosition = position + new Vector3(uncertainty.x, 0, uncertainty.y);
            Enter(HunterState.Investigate, LastKnownPosition);
        }

        private void FollowRoute()
        {
            if (Current == HunterState.Capture) Enter(HunterState.Patrol);
            if (level.waypoints.Length == 0 || (goal.HasValue && !Arrived) || repath > 0) return;
            if (Threat == 4 && waypoint % 3 == 2)
            {
                waypoint++;
                Enter(HunterState.Ambush);
                return;
            }
            if (Threat is 2 or 3 && waypoint % 2 == 1)
            {
                searchOrigin = level.waypoints[waypoint++ % level.waypoints.Length].position;
                Enter(HunterState.Search);
                MoveTo(searchOrigin);
                return;
            }
            MoveTo(level.waypoints[waypoint++ % level.waypoints.Length].position);
        }

        private void ScanArea()
        {
            if (stateElapsed < scanTime) return;
            scanTime = stateElapsed + 0.8f;
            Vector2 offset = Random.insideUnitCircle * (2 + Threat * 0.6f);
            MoveTo(searchOrigin + new Vector3(offset.x, 0, offset.y));
        }

        private void ReturnToRoute()
        {
            LastKnownPosition = null;
            Enter(HunterState.Patrol);
        }

        private void Enter(HunterState state, Vector3? location = null)
        {
            Current = state;
            stateElapsed = 0;
            scanTime = 0;
            repath = 0;
            if (location.HasValue)
            {
                searchOrigin = location.Value;
                MoveTo(location.Value);
            }
            else goal = null;
        }

        private bool MoveTo(Vector3 target)
        {
            if (!agent.isOnNavMesh || repath > 0) return false;
            repath = profile.repathSeconds;
            if (NavMesh.SamplePosition(target, out var hit, 2.5f, agent.areaMask) &&
                agent.CalculatePath(hit.position, candidatePath))
            {
                if (candidatePath.status == NavMeshPathStatus.PathComplete)
                {
                    goal = hit.position;
                    failedPaths = 0;
                    NavigationIssue = null;
                    ReleaseDoor();
                    return agent.SetPath(candidatePath);
                }
                // Đường bị chặn một phần: nếu điểm cuối nằm sát một cửa đóng chưa khóa, đi tới cửa rồi mở theo luật cửa.
                if (candidatePath.status == NavMeshPathStatus.PathPartial && candidatePath.corners.Length > 0)
                {
                    Vector3 end = candidatePath.corners[^1];
                    var door = ClosedDoorNear(end, profile.doorSearchRadius);
                    if (door != null)
                    {
                        HoldDoor(door, hit.position);
                        goal = end;
                        failedPaths = 0;
                        NavigationIssue = null;
                        return agent.SetPath(candidatePath);
                    }
                }
            }

            goal = null;
            agent.ResetPath();
            failedPaths++;
            NavigationIssue = "Mục tiêu nằm ngoài đường đi hợp lệ.";
            if (!TryOpenDoor()) ReleaseDoor();
            return false;
        }

        private OfficeDoor ClosedDoorNear(Vector3 point, float radius)
        {
            OfficeDoor best = null;
            float bestDistance = radius * radius;
            foreach (var target in level.interactions)
            {
                if (target is not OfficeDoor door || door.IsOpen || door.locked || !door.isActiveAndEnabled) continue;
                float distance = (door.transform.position - point).sqrMagnitude;
                if (distance > bestDistance) continue;
                bestDistance = distance;
                best = door;
            }
            return best;
        }

        private void HoldDoor(OfficeDoor door, Vector3 destination)
        {
            if (pendingDoor != null && pendingDoor != door) pendingDoor.CancelHunterOpen();
            pendingDoor = door;
            doorDestination = destination;
        }

        private void ReleaseDoor()
        {
            if (pendingDoor != null) pendingDoor.CancelHunterOpen();
            pendingDoor = null;
            doorDestination = null;
        }

        /// <summary>Đứng tại cửa đóng cho tới khi mở xong, sau đó tính lại đường tới đích ban đầu.</summary>
        private bool TickDoor()
        {
            if (pendingDoor == null) return false;
            if (pendingDoor.locked)
            {
                ReleaseDoor();
                return false;
            }
            if (pendingDoor.IsOpen)
            {
                Vector3? destination = doorDestination;
                pendingDoor = null;
                doorDestination = null;
                agent.isStopped = false;
                if (destination.HasValue && (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete))
                {
                    repath = 0;
                    MoveTo(destination.Value);
                }
                return false;
            }
            Vector3 offset = pendingDoor.transform.position - transform.position;
            offset.y = 0;
            if (offset.sqrMagnitude > profile.doorSearchRadius * profile.doorSearchRadius) return false;
            agent.isStopped = true;
            if (offset.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(offset), agent.angularSpeed * Time.deltaTime);
            pendingDoor.RequestHunterOpen();
            stuck = 0;
            return true;
        }

        /// <summary>Tìm cửa đóng chưa khóa mà đoạn đường sắp đi (≤ 2 m) cắt qua.</summary>
        private void DetectDoorAhead()
        {
            if (pendingDoor != null || !agent.hasPath || agent.pathPending) return;
            OfficeDoor near = null;
            foreach (var target in level.interactions)
            {
                if (target is not OfficeDoor door || door.IsOpen || door.locked || door.Blocker == null) continue;
                if ((door.transform.position - transform.position).sqrMagnitude > 9) continue;
                near = door;
                break;
            }
            if (near == null) return;
            var corners = agent.path.corners;
            Vector3 from = transform.position;
            float budget = 2.0f;
            for (int i = 0; i < corners.Length && budget > 0; i++)
            {
                Vector3 to = corners[i];
                Vector3 step = to - from;
                step.y = 0;
                float length = step.magnitude;
                if (length < 0.01f) continue;
                float span = Mathf.Min(length, budget);
                foreach (var target in level.interactions)
                {
                    if (target is not OfficeDoor door || door.IsOpen || door.locked) continue;
                    var blocker = door.Blocker;
                    if (blocker == null) continue;
                    Bounds bounds = blocker.bounds;
                    bounds.Expand(new Vector3(0.3f, 0, 0.3f));
                    var ray = new Ray(new Vector3(from.x, bounds.center.y, from.z), step / length);
                    if (bounds.IntersectRay(ray, out float distance) && distance <= span)
                    {
                        HoldDoor(door, agent.destination);
                        return;
                    }
                }
                budget -= length;
                from = to;
            }
        }

        private bool TryOpenDoor()
        {
            var door = ClosedDoorNear(transform.position, profile.doorSearchRadius);
            if (door == null) return false;
            HoldDoor(door, doorDestination ?? goal ?? LastKnownPosition ?? door.transform.position);
            door.RequestHunterOpen();
            return true;
        }

        /// <summary>
        /// Gameplay Specification tr. 19, 35: nấp tương đối quá lâu khiến Hunter tới kiểm tra.
        /// Hunter chỉ nhận vị trí chỗ nấp (có sai số), không nhận vị trí chính xác của TAY.
        /// </summary>
        private void CheckLongHide()
        {
            var player = level.player;
            var spot = player.CurrentHide;
            if (spot == null || spot.absolute || player.HiddenSeconds < profile.relativeHideCheckSeconds)
            {
                if (spot == null) checkedHide = null;
                return;
            }
            if (checkedHide == spot || Current is HunterState.Chase or HunterState.Investigate) return;
            checkedHide = spot;
            InvestigateSignal(spot.outside != null ? spot.outside.position : spot.transform.position);
        }

        private void TrackMovement(float delta)
        {
            Vector3 position = transform.position;
            bool intendsToMove = goal.HasValue && !Arrived && !agent.pathPending;
            stuck = intendsToMove && (position - previous).sqrMagnitude < 0.000025f ? stuck + delta : 0;
            previous = position;
            bool blocked = stuck >= profile.stuckSeconds || failedPaths >= 5 || (intendsToMove && agent.isPathStale);
            if (!blocked) return;
            Vector3? destination = goal ?? LastKnownPosition;
            stuck = 0;
            failedPaths = 0;
            repath = 0;
            if (TryOpenDoor()) return;
            if (destination.HasValue && MoveTo(destination.Value))
            {
                if (agent.velocity.sqrMagnitude > 0.01f) return;
                Vector3 side = Vector3.Cross(Vector3.up, transform.forward) * 0.8f;
                if (MoveAside(position, side) || MoveAside(position, -side)) return;
            }
            if (Current != HunterState.Chase) ReturnToRoute();
        }

        private bool MoveAside(Vector3 position, Vector3 offset)
        {
            if (!NavMesh.SamplePosition(position + offset, out var nearby, 0.45f, agent.areaMask) ||
                (nearby.position - position).sqrMagnitude < 0.09f ||
                !HunterPerception.IsClear(Eye, nearby.position + Vector3.up * profile.eyeHeight, Obstacles)) return false;
            repath = 0;
            return MoveTo(nearby.position);
        }

        private bool TryRecoverNavigation()
        {
            IsTrackingPlayer = false;
            if (repath > 0) return false;
            repath = profile.stuckSeconds;
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (NavMesh.SamplePosition(transform.position, out var nearest, 1.25f, filter) &&
                HunterPerception.IsClear(Eye, nearest.position + Vector3.up * profile.eyeHeight, Obstacles) &&
                BindAgent(nearest.position))
            {
                previous = transform.position;
                stuck = 0;
                repath = 0;
                NavigationIssue = null;
                return true;
            }
            NavigationIssue = "Hunter chưa thể trở lại NavMesh từ vị trí hiện tại.";
            return false;
        }

        public void ApplyEmp(float seconds) => emp = Mathf.Max(emp, seconds);

        public void ApplyStun(float seconds, float fasterFor, float speedBonus)
        {
            if (Current == HunterState.Recovery || enrage > 0 || !agent.isOnNavMesh) return;
            if (Current != HunterState.Stunned) Enter(HunterState.Stunned);
            stun = Mathf.Max(stun, seconds);
            enrageDuration = fasterFor;
            enrageBonus = speedBonus;
            IsTrackingPlayer = false;
            agent.isStopped = true;
        }

        private void TickDisabled(float delta)
        {
            agent.isStopped = true;
            if (Current == HunterState.Stunned)
            {
                stun -= delta;
                if (stun > 0) return;
                recovery = profile.recoverySeconds;
                Enter(HunterState.Recovery);
                return;
            }
            recovery -= delta;
            if (recovery > 0) return;
            enrage = enrageDuration;
            if (LastKnownPosition.HasValue) Enter(HunterState.Investigate, LastKnownPosition);
            else ReturnToRoute();
        }

        public void ResetRun(Transform spawn)
        {
            if (agent.isOnNavMesh) agent.ResetPath();
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (!NavMesh.SamplePosition(spawn.position, out var hit, 2, filter) || !BindAgent(hit.position))
            {
                NavigationIssue = "Hunter Spawn chưa kết nối NavMesh.";
                Debug.LogWarning($"Project ECHO: {NavigationIssue}", this);
            }
            else NavigationIssue = null;
            transform.rotation = spawn.rotation;
            previous = transform.position;
            LastKnownPosition = null;
            IsTrackingPlayer = false;
            waypoint = 0;
            ReleaseDoor();
            checkedHide = null;
            failedPaths = 0;
            reaction = stuck = stun = recovery = emp = enrage = 0;
            Enter(HunterState.Patrol);
        }

        private bool BindAgent(Vector3 position)
        {
            if (!agent.enabled)
            {
                transform.position = position;
                agent.enabled = true;
            }
            return agent.Warp(position);
        }
    }
}
