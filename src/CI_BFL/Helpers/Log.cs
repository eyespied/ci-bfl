using System;
using NetworkMessages.FromServer;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace CI_BFL.Helpers
{
    internal static class Log
    {
        private const string Prefix = "[CI_BFL] ";

        public static void Print(string message)
        {
            Debug.Print(Prefix + message);
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(Prefix + message);
            Console.ResetColor();
        }

        public static void Warn(string message)
        {
            Debug.Print(Prefix + "[WARN] " + message);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(Prefix + "[WARN] " + message);
            Console.ResetColor();
        }

        public static void Broadcast(string message)
        {
            if (!GameNetwork.IsServer)
            {
                return;
            }

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new ServerMessage(Prefix + message));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
        }

        public static void SendToPeer(NetworkCommunicator peer, string message)
        {
            if (peer == null || !GameNetwork.IsServer)
            {
                return;
            }

            GameNetwork.BeginModuleEventAsServer(peer);
            GameNetwork.WriteMessage(new ServerMessage(Prefix + message));
            GameNetwork.EndModuleEventAsServer();
        }
    }
}
