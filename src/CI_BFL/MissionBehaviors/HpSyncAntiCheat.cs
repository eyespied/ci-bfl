using CI_BFL.Helpers;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace CI_BFL.MissionBehaviors
{
    /// <summary>
    /// Enemies see full HP; teammates/spectators see real HP.
    /// Blocks cheat tools that read enemy health over the network.
    /// </summary>
    internal sealed class HpSyncAntiCheat : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void AfterStart()
        {
            base.AfterStart();
            if (!GameNetwork.IsDedicatedServer)
            {
                return;
            }

            MissionPeer.OnTeamChanged += OnMissionPeerTeamChanged;
            NetworkCommunicator.OnPeerSynchronized += OnPeerSynchronized;
            Log.Print("HP sync anti-cheat active.");
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
            agent.UpdateSyncHealthToAllClients(false);
            agent.OnAgentHealthChanged += OnAgentHealthChanged;
        }

        public override void OnAgentDeleted(Agent affectedAgent)
        {
            affectedAgent.OnAgentHealthChanged -= OnAgentHealthChanged;
            base.OnAgentDeleted(affectedAgent);
        }

        public override void OnAgentMount(Agent agent)
        {
            base.OnAgentMount(agent);
            if (agent?.MountAgent == null || !GameNetwork.IsServerOrRecorder)
            {
                return;
            }

            SyncHealthToRelevantClients(agent.MountAgent);
        }

        protected override void OnEndMission()
        {
            if (GameNetwork.IsDedicatedServer)
            {
                MissionPeer.OnTeamChanged -= OnMissionPeerTeamChanged;
                NetworkCommunicator.OnPeerSynchronized -= OnPeerSynchronized;
            }

            base.OnEndMission();
        }

        private void OnPeerSynchronized(NetworkCommunicator networkPeer)
        {
            if (networkPeer == null || !networkPeer.IsSynchronized)
            {
                return;
            }

            MissionPeer? missionPeer = networkPeer.GetComponent<MissionPeer>();
            if (missionPeer?.Team == null)
            {
                return;
            }

            SyncCurrentHealthOfAllRelevantAgentsToClient(networkPeer, missionPeer.Team);
        }

        private void OnAgentHealthChanged(Agent agent, float oldHealth, float newHealth)
        {
            SyncHealthToRelevantClients(agent);
        }

        private void OnMissionPeerTeamChanged(NetworkCommunicator networkPeer, Team previousTeam, Team newTeam)
        {
            if (networkPeer == null)
            {
                return;
            }

            SyncCurrentHealthOfAllRelevantAgentsToClient(networkPeer, newTeam);
        }

        private void SyncCurrentHealthOfAllRelevantAgentsToClient(NetworkCommunicator networkPeer, Team team)
        {
            foreach (Agent agent in Mission.Agents)
            {
                int healthToSend;
                if (team == Mission.SpectatorTeam
                    || team == agent.Team
                    || (agent.RiderAgent != null && agent.RiderAgent.Team == team))
                {
                    healthToSend = (int)agent.Health;
                }
                else
                {
                    healthToSend = (int)agent.HealthLimit;
                }

                GameNetwork.BeginModuleEventAsServer(networkPeer);
                GameNetwork.WriteMessage(new SetAgentHealth(agent.Index, healthToSend));
                GameNetwork.EndModuleEventAsServer();
            }
        }

        private void SyncHealthToRelevantClients(Agent agent)
        {
            if (agent.IsMount && agent.RiderAgent == null)
            {
                return;
            }

            Team targetAgentTeam = !agent.IsMount ? agent.Team : agent.RiderAgent.Team;
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                if (!networkPeer.IsSynchronized)
                {
                    continue;
                }

                MissionPeer? missionPeer = networkPeer.GetComponent<MissionPeer>();
                if (missionPeer == null)
                {
                    continue;
                }

                if (missionPeer.Team != targetAgentTeam && missionPeer.Team != Mission.SpectatorTeam)
                {
                    continue;
                }

                GameNetwork.BeginModuleEventAsServer(networkPeer);
                GameNetwork.WriteMessage(new SetAgentHealth(agent.Index, (int)agent.Health));
                GameNetwork.EndModuleEventAsServer();
            }
        }
    }
}
