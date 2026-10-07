using CI_BFL.Helpers;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace CI_BFL.Dedicated
{
    internal static class CI_BFLConsoleCommands
    {
        private static bool _registered;

        public static void Register()
        {
            if (_registered)
            {
                return;
            }

            DedicatedServerConsoleCommandManager.AddType(typeof(CI_BFLConsoleCommands));
            _registered = true;
            Log.Print("Console commands registered.");
        }

        [ConsoleCommandMethod("ci_bfl_flag_removal_time",
            "Seconds after round start before flags collapse to one. 0 disables. Default 60.")]
        private static void SetFlagRemovalTime(string input)
        {
            if (!int.TryParse(input, out int seconds) || seconds < 0)
            {
                Log.Warn($"ci_bfl_flag_removal_time: could not parse '{input}' as non-negative int.");
                return;
            }

            CI_BFLConfig.Instance.FlagRemovalTime = seconds;
            Log.Print($"FlagRemovalTime set to {seconds}.");
        }

        [ConsoleCommandMethod("ci_bfl_prevent_hp_sync",
            "[True/False] Hide real enemy HP from opponents on Flag Domination modes.")]
        private static void SetPreventHpSync(string input)
        {
            if (!bool.TryParse(input, out bool value))
            {
                Log.Warn($"ci_bfl_prevent_hp_sync: could not parse '{input}' as bool.");
                return;
            }

            CI_BFLConfig.Instance.PreventHpSyncToEnemies = value;
            Log.Print($"PreventHpSyncToEnemies set to {value}.");
        }

        [ConsoleCommandMethod("ci_bfl_corpse_fadeout_timer",
            "Seconds before corpses fade. Negative = native default.")]
        private static void SetCorpseFadeout(string input)
        {
            if (!int.TryParse(input, out int seconds))
            {
                Log.Warn($"ci_bfl_corpse_fadeout_timer: could not parse '{input}' as int.");
                return;
            }

            CI_BFLConfig.Instance.CorpseFadeoutTimer = seconds;
            Log.Print($"CorpseFadeoutTimer set to {seconds}.");
        }
    }
}
