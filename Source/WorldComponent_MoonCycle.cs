using System;
using Verse;
using RimWorld;
using System.Collections.Generic;
using RimWorld.Planet;
using System.Linq;

namespace Werewolf
{
    public class WorldComponent_MoonCycle : WorldComponent
    {
        public List<Moon> moons;
        GameCondition gcMoonCycle = null;
        public int ticksUntilFullMoon = -1;
        public int ticksPerMoonCycle = -1;
        
        public WorldComponent_MoonCycle(World world) : base(world)
        {
            if (moons.NullOrEmpty())
            {
                GenerateMoons(world);
            }
        }

        public void AdvanceOneDay()
        {
            if (!moons.NullOrEmpty())
                foreach (Moon moon in moons) moon.AdvanceOneDay();
        }

        public void AdvanceOneQuadrum()
        {
            if (!moons.NullOrEmpty())
                foreach (Moon moon in moons) moon.AdvanceOneQuadrum();
        }

        public void DebugTriggerNextFullMoon()
        {
            Moon soonestMoon = null;
            if (!moons.NullOrEmpty() && gcMoonCycle != null)
            {
                soonestMoon = moons.MinBy(x => x.DaysUntilFull);
                for (int i = 0; i < soonestMoon.DaysUntilFull; i++)
                    AdvanceOneDay();
                soonestMoon.FullMoonIncident();
            }
        }

        public void GenerateMoons(World world)
        {
            if (moons == null) moons = new List<Moon>();

            //1-2% chance there are more than 3 moons.
            int numMoons = 1;
            float val = Rand.Value;
            if (val > 0.98) numMoons = Rand.Range(4, 6);
            else if (val > 0.7) numMoons = 3;
            else if (val > 0.4) numMoons = 2;
            for (int i = 0; i < numMoons; i++)
            {
                int uniqueID = 1;
                if (this.moons.Any<Moon>())
                    uniqueID = this.moons.Max((Moon o) => o.UniqueID) + 1;

                moons.Add(new Moon(uniqueID, world, Rand.Range(350000 * (i + 1), 600000 * (i + 1))));
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (!moons.NullOrEmpty())
            {
                foreach (Moon moon in moons)
                {
                    moon.Tick();
                }
            }
            if (gcMoonCycle == null)
            {
                gcMoonCycle = new GameCondition_MoonCycle();
                gcMoonCycle.Permanent = true;
                Find.World.gameConditionManager.RegisterCondition(gcMoonCycle);
            }
        }
        

        public int DaysUntilFullMoon
        {
            get
            {
                return 0;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.ticksUntilFullMoon, "ticksUntilFullMoon", -1, false);
            Scribe_Deep.Look<GameCondition>(ref this.gcMoonCycle, "gcMoonCycle");
            Scribe_Collections.Look<Moon>(ref this.moons, "moons", LookMode.Deep, new object[0]);
        }
    }
}
