using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Werewolf
{
    public class WorldComponent_MoonCycle : WorldComponent
    {
        private GameCondition gcMoonCycle;
        public List<Moon> moons;

        public Dictionary<Pawn, int> recentWerewolves = new Dictionary<Pawn, int>();
        public int ticksPerMoonCycle = -1;
        public int ticksUntilFullMoon = -1;
        public bool traitsGivenToHiddenWerewolves = false;

        public WorldComponent_MoonCycle(World world) : base(world)
        {
            if (moons.NullOrEmpty())
            {
                GenerateMoons(world);
            }
        }

        public int DaysUntilFullMoon => 0;

        public void AdvanceOneDay()
        {
            if (moons.NullOrEmpty())
            {
                return;
            }

            foreach (var moon in moons)
            {
                moon.AdvanceOneDay();
            }
        }

        public void AdvanceOneQuadrum()
        {
            if (moons.NullOrEmpty())
            {
                return;
            }

            foreach (var moon in moons)
            {
                moon.AdvanceOneQuadrum();
            }
        }

        public void DebugTriggerNextFullMoon()
        {
            if (moons.NullOrEmpty() || gcMoonCycle == null)
            {
                return;
            }

            var soonestMoon = moons.MinBy(x => x.DaysUntilFull);
            for (var i = 0; i < soonestMoon.DaysUntilFull; i++)
            {
                AdvanceOneDay();
            }

            soonestMoon.FullMoonIncident();
        }

        public void DebugRegenerateMoons(World thisWorld)
        {
            moons = null;
            GenerateMoons(thisWorld);
            Messages.Message("DEBUG :: Moons Regenerated", MessageTypeDefOf.TaskCompletion);
        }

        public void GenerateMoons(World thisWorld)
        {
            if (moons == null)
            {
                moons = new List<Moon>();
            }

            //1-2% chance there are more than 3 moons.
            var numMoons = 1;
            var val = Rand.Value;
            if (val > 0.98)
            {
                numMoons = Rand.Range(4, 6);
            }
            else if (val > 0.7)
            {
                numMoons = 3;
            }
            else if (val > 0.4)
            {
                numMoons = 2;
            }

            for (var i = 0; i < numMoons; i++)
            {
                var uniqueID = 1;
                if (moons.Any())
                {
                    uniqueID = moons.Max(o => o.UniqueID) + 1;
                }

                moons.Add(new Moon(uniqueID, thisWorld, Rand.Range(350000 * (i + 1), 600000 * (i + 1))));
            }
        }


        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (!moons.NullOrEmpty())
            {
                foreach (var moon in moons)
                {
                    moon.Tick();
                }
            }

            if (gcMoonCycle == null)
            {
                gcMoonCycle = GameConditionMaker.MakeConditionPermanent(WWDefOf.ROM_MoonCycle);
                Find.World.gameConditionManager.RegisterCondition(gcMoonCycle);
            }

            if (recentWerewolves.Any())
            {
                recentWerewolves.RemoveAll(x => x.Key.Dead || x.Key.DestroyedOrNull());
            }

            if (!recentWerewolves.Any())
            {
                return;
            }

            var recentVampiresKeys = new List<Pawn>(recentWerewolves.Keys);
            foreach (var key in recentVampiresKeys)
            {
                recentWerewolves[key] += 1;
                if (recentWerewolves[key] <= 100)
                {
                    continue;
                }

                recentWerewolves.Remove(key);
                if (!key.Spawned || key.Faction == Faction.OfPlayerSilentFail)
                {
                    continue;
                }

                Find.LetterStack.ReceiveLetter("ROM_WerewolfEncounterLabel".Translate(),
                    "ROM_WerewolfEncounterDesc".Translate(key.LabelShort), LetterDefOf.ThreatSmall, key);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksUntilFullMoon, "ticksUntilFullMoon", -1);
            Scribe_Values.Look(ref traitsGivenToHiddenWerewolves, "traitsGivenToHiddenWerewolves", false);
            Scribe_References.Look(ref gcMoonCycle, "gcMoonCycle");
            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs && gcMoonCycle == null)
            {
                gcMoonCycle = Find.World.GameConditionManager.GetActiveCondition<GameCondition_MoonCycle>();
                Log.Warning(
                    $"{this}.gcMoonCycle wasn't a proper reference in save file, likely due to outdated ROM Werewolf; " +
                    $"attempting to default to first active GameCondition_MoonCycle: {gcMoonCycle?.GetUniqueLoadID() ?? "null"}");
                if (gcMoonCycle != null)
                {
                    if (gcMoonCycle.uniqueID == -1)
                    {
                        var uniqueID = Find.UniqueIDsManager.GetNextGameConditionID();
                        Log.Warning($"{this}.gcMoonCycle.uniqueID is unassigned (-1); setting it to {uniqueID}");
                        gcMoonCycle.uniqueID = uniqueID;
                    }

                    if (gcMoonCycle.def != WWDefOf.ROM_MoonCycle)
                    {
                        Log.Warning($"{this}.gcMoonCycle.def is {gcMoonCycle.def.ToStringSafe()}; " +
                                    $"setting it to {WWDefOf.ROM_MoonCycle}");
                        gcMoonCycle.def = WWDefOf.ROM_MoonCycle;
                    }
                }
            }

            Scribe_Collections.Look(ref moons, "moons", LookMode.Deep);
        }
    }
}