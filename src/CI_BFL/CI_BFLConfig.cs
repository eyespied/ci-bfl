namespace CI_BFL
{
    internal sealed class CI_BFLConfig
    {
        public static CI_BFLConfig Instance { get; } = new CI_BFLConfig();

        /// <summary>Seconds after round start before flags collapse to one. &lt;= 0 disables.</summary>
        public int FlagRemovalTime { get; set; } = 60;

        /// <summary>Hide real enemy HP from opponents (Flag Domination only).</summary>
        public bool PreventHpSyncToEnemies { get; set; } = true;

        /// <summary>Seconds before corpses fade. Negative = native default.</summary>
        public int CorpseFadeoutTimer { get; set; } = -1;
    }
}
