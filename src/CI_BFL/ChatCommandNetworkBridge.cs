using System;
using CI_BFL.Helpers;
using NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace CI_BFL
{
    internal sealed class ChatCommandHandler
    {
        public void HandleChatMessage(NetworkCommunicator peer, string message)
        {
            if (string.IsNullOrWhiteSpace(message) || !message.TrimStart().StartsWith("!", StringComparison.Ordinal))
            {
                return;
            }

            string trimmed = message.Trim();
            string[] parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return;
            }

            string command = parts[0].ToLowerInvariant();
            switch (command)
            {
                case "!setflagtimer":
                    HandleSetFlagTimer(peer, parts);
                    break;
                case "!flagtimer":
                    Log.Broadcast($"Flag removal timer is {CI_BFLConfig.Instance.FlagRemovalTime}s (0 = disabled).");
                    break;
            }
        }

        private static void HandleSetFlagTimer(NetworkCommunicator peer, string[] parts)
        {
            if (peer == null || !peer.IsAdmin)
            {
                Log.Broadcast("Only admins can change the flag removal timer.");
                return;
            }

            if (parts.Length < 2 || !int.TryParse(parts[1], out int seconds) || seconds < 0)
            {
                Log.SendToPeer(peer, "Usage: !setflagtimer <seconds>  (0 disables auto flag removal)");
                return;
            }

            CI_BFLConfig.Instance.FlagRemovalTime = seconds;
            if (seconds == 0)
            {
                Log.Broadcast($"{peer.UserName} disabled automatic flag removal.");
            }
            else
            {
                Log.Broadcast($"{peer.UserName} set flag removal timer to {seconds}s (applies next round).");
            }
        }
    }

    /// <summary>
    /// Hooks player chat at the network layer for dedicated servers.
    /// </summary>
    internal static class ChatCommandNetworkBridge
    {
        private static ChatCommandHandler? _handler;
        private static bool _networkHandlersRegistered;

        public static void Update()
        {
            if (!GameNetwork.IsDedicatedServer || _networkHandlersRegistered || !GameNetwork.IsServer)
            {
                return;
            }

            _handler ??= new ChatCommandHandler();

            var registerer = new GameNetwork.NetworkMessageHandlerRegisterer(
                GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);
            registerer.Register<NetworkMessages.FromClient.PlayerMessageAll>(HandlePlayerMessageAll);
            registerer.Register<NetworkMessages.FromClient.PlayerMessageTeam>(HandlePlayerMessageTeam);
            _networkHandlersRegistered = true;
            Log.Print("Chat command network handlers registered.");
        }

        public static void Unregister()
        {
            if (!_networkHandlersRegistered)
            {
                return;
            }

            var registerer = new GameNetwork.NetworkMessageHandlerRegisterer(
                GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Remove);
            registerer.Register<NetworkMessages.FromClient.PlayerMessageAll>(HandlePlayerMessageAll);
            registerer.Register<NetworkMessages.FromClient.PlayerMessageTeam>(HandlePlayerMessageTeam);
            _networkHandlersRegistered = false;
        }

        private static bool HandlePlayerMessageAll(NetworkCommunicator peer, NetworkMessages.FromClient.PlayerMessageAll message)
        {
            _handler?.HandleChatMessage(peer, message.Message);
            return true;
        }

        private static bool HandlePlayerMessageTeam(NetworkCommunicator peer, NetworkMessages.FromClient.PlayerMessageTeam message)
        {
            _handler?.HandleChatMessage(peer, message.Message);
            return true;
        }
    }
}
