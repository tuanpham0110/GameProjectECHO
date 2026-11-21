using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectEcho
{
    public enum RelayState { Idle, Transmitting, Complete }

    public sealed class MissionManager
    {
        private readonly LevelRules rules;
        private readonly List<string> data = new();
        private int highestThreat = 1;

        public int ValidData => data.Count;
        public int RequiredData => rules.requiredData;
        public int Threat { get; private set; } = 1;
        public RelayState Relay { get; private set; }
        public float RelayProgress { get; private set; }
        public bool CanExit => Relay == RelayState.Complete && ValidData >= RequiredData;

        public MissionManager(LevelRules rules) => this.rules = rules;

        public bool Collect(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || data.Contains(id)) return false;
            data.Add(id);
            RefreshThreat();
            return true;
        }

        public bool HasData(string id) => data.Contains(id);

        public bool BeginRelay()
        {
            if (Relay != RelayState.Idle || ValidData < RequiredData) return false;
            Relay = RelayState.Transmitting;
            RefreshThreat();
            return true;
        }

        public bool TickRelay(float seconds, bool inside)
        {
            if (Relay != RelayState.Transmitting || !inside || seconds <= 0) return false;
            RelayProgress = Mathf.Min(1, RelayProgress + seconds / rules.relaySeconds);
            if (RelayProgress < 0.999999f) return false;
            RelayProgress = 1;
            Relay = RelayState.Complete;
            return true;
        }

        public IReadOnlyList<string> ApplyCapture(bool protectedByArmor, Func<int, int> selectIndex)
        {
            bool protectDelivery = Relay == RelayState.Complete && (!rules.invalidateCompletedRelay || protectedByArmor);
            var lost = new List<string>();
            if (!protectedByArmor && !protectDelivery)
            {
                int count = Mathf.Min(rules.captureLoss, data.Count);
                for (int i = 0; i < count; i++)
                {
                    int index = selectIndex(data.Count);
                    lost.Add(data[index]);
                    data.RemoveAt(index);
                }
            }
            if (!protectDelivery)
            {
                Relay = RelayState.Idle;
                RelayProgress = 0;
            }
            RefreshThreat();
            return lost;
        }

        private void RefreshThreat()
        {
            int current = Relay == RelayState.Idle ? rules.GetThreat(data.Count) : 4;
            highestThreat = Mathf.Max(highestThreat, current);
            Threat = rules.escalation == EscalationPolicy.HighestReached ? highestThreat : current;
        }
    }
}
