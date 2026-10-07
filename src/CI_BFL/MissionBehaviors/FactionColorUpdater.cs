using System.Linq;
using CI_BFL.Helpers;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace CI_BFL.MissionBehaviors
{
    /// <summary>
    /// Mirror-match recolor: when both teams use the same culture, give attacker/defender
    /// distinct banner/team colors so they stay readable.
    /// </summary>
    internal sealed class FactionColorUpdater : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        private readonly FactionColor[] _factionColors =
        {
            new FactionColor("empire", 8, 15, 82, 19),
            new FactionColor("sturgia", 115, 40, 126, 122),
            new FactionColor("vlandia", 142, 84, 12, 13),
            new FactionColor("battania", 2, 143, 133, 122),
        };

        public override void AfterStart()
        {
            base.AfterStart();
            if (!GameNetwork.IsDedicatedServer)
            {
                return;
            }

            string culture1 = MultiplayerOptions.OptionType.CultureTeam1.GetStrValue();
            string culture2 = MultiplayerOptions.OptionType.CultureTeam2.GetStrValue();
            if (culture1 != culture2)
            {
                Log.Print("Color replacer skipped (teams use different cultures).");
                return;
            }

            FactionColor? colors = _factionColors.FirstOrDefault(x => x.FactionName == culture1);
            if (colors == null)
            {
                Log.Warn($"Color replacer: culture '{culture1}' not supported.");
                return;
            }

            Mission mission = Mission.Current;
            BasicCultureObject? faction = MBObjectManager.Instance.GetObject<BasicCultureObject>(culture1);
            if (faction == null || mission.AttackerTeam == null || mission.DefenderTeam == null)
            {
                return;
            }

            ApplyBannerColors(mission.AttackerTeam, colors.PrimaryColorId, colors.SecondaryColorId);
            ApplyBannerColors(mission.DefenderTeam, colors.AltPrimary, colors.AltSecondary);

            uint atkCol1 = mission.AttackerTeam.Banner.GetPrimaryColor();
            uint atkCol2 = mission.AttackerTeam.Banner.GetFirstIconColor();
            uint defCol1 = mission.DefenderTeam.Banner.GetPrimaryColor();
            uint defCol2 = mission.DefenderTeam.Banner.GetFirstIconColor();

            mission.AttackerTeam.SetPropertyValue("Color", atkCol1);
            mission.AttackerTeam.SetPropertyValue("Color2", atkCol2);
            mission.DefenderTeam.SetPropertyValue("Color", defCol1);
            mission.DefenderTeam.SetPropertyValue("Color2", defCol2);

            faction.SetPropertyValue("Color", atkCol1);
            faction.SetPropertyValue("Color2", atkCol2);
            faction.SetPropertyValue("ClothAlternativeColor", defCol1);
            faction.SetPropertyValue("ClothAlternativeColor2", defCol2);
            faction.SetPropertyValue("ForegroundColor1", atkCol2);
            faction.SetPropertyValue("ForegroundColor2", defCol2);
            faction.SetPropertyValue("BackgroundColor1", atkCol1);
            faction.SetPropertyValue("BackgroundColor2", defCol1);

            Log.Print($"Color replacer applied for mirror match ({culture1}).");
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            Mission.Current?.RemoveMissionBehavior(this);
        }

        private static void ApplyBannerColors(Team team, int primary, int secondary)
        {
            if (team.Banner?.BannerDataList == null || team.Banner.BannerDataList.Count == 0)
            {
                return;
            }

            team.Banner.BannerDataList[0].ColorId = primary;
            team.Banner.BannerDataList[0].ColorId2 = primary;
            for (int i = 1; i < team.Banner.BannerDataList.Count; i++)
            {
                team.Banner.BannerDataList[i].ColorId = secondary;
                team.Banner.BannerDataList[i].ColorId2 = secondary;
            }
        }

        private sealed class FactionColor
        {
            public string FactionName { get; }
            public int PrimaryColorId { get; }
            public int SecondaryColorId { get; }
            public int AltPrimary { get; }
            public int AltSecondary { get; }

            public FactionColor(string factionName, int primaryColorId, int secondaryColorId, int altPrimary, int altSecondary)
            {
                FactionName = factionName;
                PrimaryColorId = primaryColorId;
                SecondaryColorId = secondaryColorId;
                AltPrimary = altPrimary;
                AltSecondary = altSecondary;
            }
        }
    }
}
