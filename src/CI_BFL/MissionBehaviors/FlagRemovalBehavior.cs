using System;
using System.Collections.Generic;
using System.Reflection;
using CI_BFL.Helpers;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace CI_BFL.MissionBehaviors
{
    /// <summary>
    /// BFL-style timed flag collapse for Flag Domination (Skirmish/Battle/Captain).
    /// After FlagRemovalTime seconds into a round, keeps one random flag and removes the rest.
    /// Uses mission time (not System.Timers) so work stays on the game thread.
    /// </summary>
    internal sealed class FlagRemovalBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        private MissionMultiplayerFlagDomination? _mode;
        private MultiplayerRoundController? _roundController;
        private int _lastRemovedIndex = -1;
        private bool _timerArmed;
        private float _fireAtMissionTime;
        private bool _firedThisRound;

        public override void AfterStart()
        {
            base.AfterStart();
            if (!GameNetwork.IsDedicatedServer)
            {
                return;
            }

            _mode = Mission.GetMissionBehavior<MissionMultiplayerFlagDomination>();
            _roundController = Mission.GetMissionBehavior<MultiplayerRoundController>();
            if (_mode == null || _roundController == null)
            {
                return;
            }

            _roundController.OnRoundStarted += OnRoundStarted;
            _roundController.OnRoundEnding += OnRoundEnding;
            Log.Print($"Flag removal armed (timer={CI_BFLConfig.Instance.FlagRemovalTime}s).");
        }

        public override void OnRemoveBehavior()
        {
            if (_roundController != null)
            {
                _roundController.OnRoundStarted -= OnRoundStarted;
                _roundController.OnRoundEnding -= OnRoundEnding;
            }

            DisarmTimer();
            base.OnRemoveBehavior();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (!_timerArmed || _firedThisRound || _mode == null)
            {
                return;
            }

            if (Mission.CurrentTime < _fireAtMissionTime)
            {
                return;
            }

            _firedThisRound = true;
            _timerArmed = false;
            try
            {
                RemoveFlags(_mode);
            }
            catch (Exception ex)
            {
                Log.Warn($"Flag removal failed: {ex.Message}");
            }
        }

        private void OnRoundStarted()
        {
            DisarmTimer();
            int seconds = CI_BFLConfig.Instance.FlagRemovalTime;
            if (seconds <= 0 || _mode == null)
            {
                return;
            }

            _fireAtMissionTime = Mission.CurrentTime + seconds;
            _timerArmed = true;
            _firedThisRound = false;
            Log.Print($"Flag removal timer started ({seconds}s).");
        }

        private void OnRoundEnding()
        {
            DisarmTimer();
        }

        private void DisarmTimer()
        {
            _timerArmed = false;
            _firedThisRound = false;
        }

        private void RemoveFlags(MissionMultiplayerFlagDomination gamemode)
        {
            IEnumerable<FlagCapturePoint>? allFlags = gamemode.AllCapturePoints;
            if (allFlags == null)
            {
                return;
            }

            List<FlagCapturePoint> all = new List<FlagCapturePoint>(allFlags);
            if (all.Count == 0)
            {
                return;
            }

            List<FlagCapturePoint> valid = new List<FlagCapturePoint>();
            foreach (FlagCapturePoint point in all)
            {
                if (point != null && !point.IsDeactivated && point.FlagIndex != _lastRemovedIndex)
                {
                    valid.Add(point);
                }
            }

            if (valid.Count == 0)
            {
                foreach (FlagCapturePoint point in all)
                {
                    if (point != null && !point.IsDeactivated)
                    {
                        valid.Add(point);
                    }
                }
            }

            if (valid.Count == 0)
            {
                return;
            }

            FlagCapturePoint remainingFlag = valid[MBRandom.RandomInt(valid.Count)];
            _lastRemovedIndex = remainingFlag.FlagIndex;

            MethodInfo? removeCapturePoint = gamemode.GetMethodInfo("RemoveCapturePoint");
            if (removeCapturePoint == null)
            {
                Log.Warn("RemoveCapturePoint not found — aborting flag removal.");
                return;
            }

            foreach (FlagCapturePoint point in all)
            {
                if (point == null || point.IsDeactivated)
                {
                    continue;
                }

                if (point.FlagIndex != _lastRemovedIndex)
                {
                    removeCapturePoint.Invoke(gamemode, new object[] { point });
                }
            }

            MultiplayerGameNotificationsComponent? notifications =
                gamemode.GetFieldValue<MultiplayerGameNotificationsComponent>("NotificationsComponent");
            notifications?.FlagXRemaining(remainingFlag);

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FlagDominationMoraleChangeMessage(gamemode.MoraleRounded));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FlagDominationFlagsRemovedMessage());
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);

            gamemode.SetFieldValue("_flagRemovalOccured", true);
            gamemode.GetFieldValue<MissionMultiplayerGameModeFlagDominationClient>("_gameModeFlagDominationClient")
                ?.OnNumberOfFlagsChanged();

            foreach (MissionBehavior behavior in gamemode.Mission.MissionBehaviors)
            {
                if (behavior is IFlagRemoved flagRemoved)
                {
                    flagRemoved.OnFlagsRemoved(remainingFlag.FlagIndex);
                }
            }

            MPPerkObject.RaiseEventForAllPeers(MPPerkCondition.PerkEventFlags.FlagRemoval);
            Log.Print($"Flags collapsed — remaining flag index {_lastRemovedIndex}.");
        }
    }
}
