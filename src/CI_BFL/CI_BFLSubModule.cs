using CI_BFL.Dedicated;
using CI_BFL.Helpers;
using CI_BFL.MissionBehaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace CI_BFL
{
    public sealed class CI_BFLSubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            CI_BFLConsoleCommands.Register();
            Log.Print("Loaded.");
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            ChatCommandNetworkBridge.Update();
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            base.OnBeforeMissionBehaviorInitialize(mission);
            if (!GameNetwork.IsDedicatedServer)
            {
                return;
            }

            CI_BFLConfig options = CI_BFLConfig.Instance;

            if (options.CorpseFadeoutTimer >= 0)
            {
                mission.SetMissionCorpseFadeOutTimeInSeconds(options.CorpseFadeoutTimer);
            }

            MissionMultiplayerGameModeBase? gamemode = mission.GetMissionBehavior<MissionMultiplayerGameModeBase>();
            if (gamemode is MissionMultiplayerFlagDomination)
            {
                if (options.PreventHpSyncToEnemies
                    && mission.GetMissionBehavior<HpSyncAntiCheat>() == null)
                {
                    mission.AddMissionBehavior(new HpSyncAntiCheat());
                }

                if (mission.GetMissionBehavior<FlagRemovalBehavior>() == null)
                {
                    mission.AddMissionBehavior(new FlagRemovalBehavior());
                }
            }

            if (mission.GetMissionBehavior<FactionColorUpdater>() == null)
            {
                mission.AddMissionBehavior(new FactionColorUpdater());
            }
        }

        public override void OnGameEnd(Game game)
        {
            base.OnGameEnd(game);
            ChatCommandNetworkBridge.Unregister();
        }
    }
}
