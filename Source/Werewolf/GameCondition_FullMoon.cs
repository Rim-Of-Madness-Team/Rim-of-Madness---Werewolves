﻿using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Werewolf
{
    public class GameCondition_FullMoon : GameCondition
    {
        private bool firstTick = true;
        private Moon moon;

        private WorldComponent_MoonCycle wcMoonCycle;


        public GameCondition_FullMoon()
        {
        }

        public GameCondition_FullMoon(Moon newMoon)
        {
            moon = newMoon;
        }

        public Moon Moon => moon;

        public WorldComponent_MoonCycle WCMoonCycle =>
            wcMoonCycle ?? (wcMoonCycle = Find.World?.GetComponent<WorldComponent_MoonCycle>());

        public override string Label => "ROM_MoonCycle_FullMoon".Translate(Moon.Name);
        public override string TooltipString => "ROM_MoonCycle_FullMoonDesc".Translate(Moon.Name);

        public override void GameConditionTick()
        {
            base.GameConditionTick();
            if (!firstTick)
            {
                return;
            }

            firstTick = false;

            var allPawnsSpawned = new List<Pawn>(PawnsFinder.AllMaps);
            if ((allPawnsSpawned?.Count ?? 0) <= 0)
            {
                return;
            }

            foreach (var pawn in allPawnsSpawned)
            {
                if (pawn?.needs?.mood?.thoughts?.memories is { } m)
                {
                    m.TryGainMemory(WWDefOf.ROMWW_SawFullMoon);
                }

                if (pawn?.GetComp<CompWerewolf>() is not { } w || !ShouldTransform(pawn, w))
                {
                    continue;
                }

                if (pawn.Faction == Faction.OfPlayerSilentFail)
                {
                    w.TransformRandom(true);
                }
                else if (Rand.Value <= 0.02) //2% chance of messing up your colony
                {
                    w.TransformRandom(true);
                }
                else
                {
                    Messages.Message("ROM_WerewolfTransformationFailure".Translate(pawn),
                        MessageTypeDefOf.CautionInput);
                }
            }
        }

        private bool ShouldTransform(Pawn pawn, CompWerewolf w)
        {
            return w.IsWerewolf && !w.IsTransformed &&
                   (!w.IsBlooded || w.FuryToggled) &&
                   !pawn.PositionHeld.Fogged(pawn.MapHeld);
        }

        public override void End()
        {
            Messages.Message("ROM_MoonCycle_FullMoonPasses".Translate(Moon.Name),
                MessageTypeDefOf.NeutralEvent); //MessageSound.Standard);
            gameConditionManager.ActiveConditions.Remove(this);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref moon, "moon");
            Scribe_Values.Look(ref firstTick, "firstTick", true);
        }
    }
}