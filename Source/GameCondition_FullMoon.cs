using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Werewolf
{
    public class GameCondition_FullMoon : GameCondition
    {
        private Moon moon = null;
        public Moon Moon => moon;
        private bool firstTick = true;

        private WorldComponent_MoonCycle wcMoonCycle = null;
        public WorldComponent_MoonCycle WCMoonCycle => (wcMoonCycle == null) ? wcMoonCycle = Find.World?.GetComponent<WorldComponent_MoonCycle>() : wcMoonCycle;
        

        public GameCondition_FullMoon() { }
        public GameCondition_FullMoon(Moon newMoon)
        {
            moon = newMoon;
        }

        public override void GameConditionTick()
        {
            base.GameConditionTick();
            if (firstTick)
            {
                firstTick = false;

                List<Pawn> allPawnsSpawned = new List<Pawn>(PawnsFinder.AllMaps);
                if ((allPawnsSpawned?.Count ?? 0) > 0)
                {
                    foreach (Pawn pawn in allPawnsSpawned)
                    {

                        if (pawn?.needs?.mood?.thoughts?.memories is MemoryThoughtHandler m)
                        {
                            m.TryGainMemory(WWDefOf.ROMWW_SawFullMoon);
                        }

                        if (pawn?.GetComp<CompWerewolf>() is CompWerewolf w && ShouldTransform(pawn, w))
                        {
                            if (pawn.Faction == Faction.OfPlayerSilentFail)
                                w.TransformRandom(true);
                            else if (Rand.Value <= 0.02) //2% chance of messing up your colony
                                w.TransformRandom(true);
                            else
                                Messages.Message("ROM_WerewolfTransformationFailure".Translate(pawn), MessageTypeDefOf.CautionInput);
                        }
                    }
                }
            }
        }

        private bool ShouldTransform(Pawn pawn, CompWerewolf w) => w.IsWerewolf && !w.IsTransformed &&
                                                        (!w.IsBlooded || w.FuryToggled) &&
                                                        !pawn.PositionHeld.Fogged(pawn.MapHeld);

        public override void End()
        {
            Messages.Message("ROM_MoonCycle_FullMoonPasses".Translate(Moon.Name), MessageTypeDefOf.NeutralEvent);//MessageSound.Standard);
            this.gameConditionManager.ActiveConditions.Remove(this);
        }

        public override string Label => "ROM_MoonCycle_FullMoon".Translate(Moon.Name);
        public override string TooltipString => "ROM_MoonCycle_FullMoonDesc".Translate(Moon.Name);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Moon>(ref this.moon, "moon");
            Scribe_Values.Look<bool>(ref this.firstTick, "firstTick", true);
        }
    }
}
