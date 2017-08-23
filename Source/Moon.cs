using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Werewolf
{
    public class Moon : IExposable, ILoadReferenceable
    {
        private int uniqueID;
        private int ticksInCycle = -1;
        private int ticksLeftInCycle = -1;
        private string name = "";
        private World hostPlanet = null;

        public int UniqueID => uniqueID;
        public int DaysUntilFull => (int)((float)ticksLeftInCycle.TicksToDays());
        public string Name => name;

        public Moon() { }
        public Moon(int uniqueID, World world, int newTicks)
        {
            this.uniqueID = uniqueID;
            name = NameGenerator.GenerateName(RulePackDefOf.NamerWorld, (x => x != (hostPlanet?.info?.name ?? "") && x.Count() < 9), false);
            hostPlanet = world;
            ticksInCycle = newTicks;
            ticksLeftInCycle = ticksInCycle;
            Log.Message("New moon: " + name + " initialized. " + ticksInCycle + " ticks per cycle.");
        }

        public void AdvanceOneDay()
        {
            ticksLeftInCycle -= GenDate.TicksPerDay;
        }

        public void AdvanceOneQuadrum()
        {
            ticksLeftInCycle -= GenDate.TicksPerQuadrum;
        }

        public void Tick()
        {
            if (ticksLeftInCycle < 0)
            {
                if (Find.VisibleMap is Map m)
                {
                    int time = GenLocalDate.HourInteger(m);
                    if (time <= 3 || time >= 21)
                    {
                        FullMoonIncident();
                    }
                    else ticksLeftInCycle += GenDate.TicksPerHour;
                }

            }
            ticksLeftInCycle--;
        }

        public void FullMoonIncident()
        {
            ticksLeftInCycle = ticksInCycle;
            GameCondition_FullMoon fullMoon = new GameCondition_FullMoon(this);
            fullMoon.startTick = Find.TickManager.TicksGame;
            fullMoon.duration = GenDate.TicksPerDay;
            Find.World.gameConditionManager.RegisterCondition(fullMoon);
            Log.Message("Full Moon Incident");

        }

        public void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.uniqueID, "uniqueID", 0, false);
            Scribe_Values.Look<string>(ref this.name, "name", "Moonbase Alpha");
            Scribe_Values.Look<int>(ref this.ticksInCycle, "ticksInCycle", -1);
            Scribe_Values.Look<int>(ref this.ticksLeftInCycle, "ticksLeftInCycle", -1);
        }

        public string GetUniqueLoadID()
        {
            return "Moon_" + this.uniqueID;
        }
    }
}
