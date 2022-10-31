using System;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System.Linq;

namespace Werewolf
{
    public class Moon : IExposable, ILoadReferenceable
    {
        private readonly World hostPlanet;
        private string name = "";
        private int ticksInCycle = -1;
        private int ticksLeftInCycle = -1;
        private int uniqueID;

        public Moon()
        {
        }

        public Moon(int uniqueID, World world, int newTicks)
        {
            this.uniqueID = uniqueID;
            name = NameGenerator.GenerateName(RulePackDefOf.NamerWorld,
                x => x != (hostPlanet?.info?.name ?? "") && x.Length < 9);
            hostPlanet = world;
            ticksInCycle = newTicks;
            ticksLeftInCycle = ticksInCycle;
            //Log.Message("New moon: " + name + " initialized. " + ticksInCycle + " ticks per cycle.");
        }

        public int UniqueID => uniqueID;
        public int DaysUntilFull => (int)ticksLeftInCycle.TicksToDays();
        public string Name => name;

        public void ExposeData()
        {
            Scribe_Values.Look(ref uniqueID, "uniqueID");
            Scribe_Values.Look(ref name, "name", "Moonbase Alpha");
            Scribe_Values.Look(ref ticksInCycle, "ticksInCycle", -1);
            Scribe_Values.Look(ref ticksLeftInCycle, "ticksLeftInCycle", -1);
        }

        public string GetUniqueLoadID()
        {
            return "Moon_" + uniqueID;
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
                if (Find.CurrentMap is { } m)
                {
                    var time = GenLocalDate.HourInteger(m);
                    if (time <= 3 || time >= 21)
                    {
                        FullMoonIncident();
                    }
                    else
                    {
                        ticksLeftInCycle += GenDate.TicksPerHour;
                    }
                }
            }

            ticksLeftInCycle--;
        }

        public void FullMoonIncident()
        {
            ticksLeftInCycle = ticksInCycle;

            var def = GameConditionDef.Named("ROM_FullMoon");
            GameCondition_FullMoon gameCondition = (GameCondition_FullMoon)Activator.CreateInstance(def.conditionClass);
            gameCondition.Moon = this;
            gameCondition.startTick = Find.TickManager.TicksGame;
            gameCondition.def = def;
            gameCondition.Duration = GenDate.TicksPerDay;
            gameCondition.uniqueID = Find.UniqueIDsManager.GetNextGameConditionID();
            gameCondition.PostMake();
            
            Find.World.gameConditionManager.RegisterCondition(gameCondition);
            //Log.Message("Full Moon Incident");
        }
    }
}