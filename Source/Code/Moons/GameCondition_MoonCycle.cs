using System.Text;
using RimWorld;
using Verse;

namespace Werewolf
{
    public class GameCondition_MoonCycle : GameCondition
    {
        private WorldComponent_MoonCycle wcMoonCycle;

        public WorldComponent_MoonCycle WCMoonCycle
        {
            get
            {
                if (wcMoonCycle == null)
                {
                    wcMoonCycle = Find.World?.GetComponent<WorldComponent_MoonCycle>();
                }

                return wcMoonCycle;
            }
        }

        public int SoonestFullMoonInDays
        {
            get
            {
                var result = -1;
                if (WCMoonCycle.moons is not { } moons || moons.NullOrEmpty())
                {
                    return result;
                }

                foreach (var moon in moons)
                {
                    if (result == -1)
                    {
                        result = moon.DaysUntilFull;
                    }

                    if (moon.DaysUntilFull < result)
                    {
                        result = moon.DaysUntilFull;
                    }
                }

                return result;
            }
        }

        public override string Label
        {
            get
            {
                var result = SoonestFullMoonInDays > 0
                    ? (string) "ROM_MoonCycle_UntilNextFullMoon".Translate(SoonestFullMoonInDays)
                    : (string) "ROM_MoonCycle_FullMoonImminentArgless".Translate();
                return result;
            }
        }

        public override string TooltipString
        {
            get
            {
                string result = "ROM_MoonCycle_CurrentPhaseDesc".Translate(WCMoonCycle.world.info.name);
                var s = new StringBuilder();
                s.AppendLine(result);
                s.AppendLine();
                if (WCMoonCycle.moons is not { } MoonList || MoonList.NullOrEmpty())
                {
                    return s.ToString().TrimEndNewlines();
                }

                s.AppendLine("ROM_MoonCycle_Moons".Translate(WCMoonCycle.world.info.name));
                s.AppendLine("------");

                foreach (var m in MoonList)
                {
                    var daysLeft = m.DaysUntilFull;
                    if (daysLeft > 0)
                    {
                        s.AppendLine("  " + "ROM_MoonCycle_CurrentPhase".Translate(m.Name, m.DaysUntilFull));
                    }
                    else
                    {
                        s.AppendLine("  " + "ROM_MoonCycle_FullMoonImminent".Translate(m.Name));
                    }
                }

                return s.ToString().TrimEndNewlines();
            }
        }

        public override void End()
        {
            gameConditionManager.ActiveConditions.Remove(this);
        }
    }
}