using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Werewolf
{
    internal static partial class HarmonyPatches
    {
        public static void HarmonyPatches_AIJobsEtc(Harmony harmony)
        {
            DebugMessage();
            harmony.Patch(AccessTools.Method(typeof(Building_Door), nameof(Building_Door.PawnCanOpen)), null,
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(WerewolfCantOpen)));
            
            DebugMessage();
            harmony.Patch(
                AccessTools.Method(typeof(Pawn_PathFollower), "CostToMoveIntoCell",
                    new[] {typeof(Pawn), typeof(IntVec3)}), null, new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(PathOfNature)));

            DebugMessage();
            harmony.Patch(AccessTools.Method(typeof(LordToil_AssaultColony), "UpdateAllDuties"), null,
                new HarmonyMethod(typeof(HarmonyPatches),
                    nameof(UpdateAllDuties_PostFix)));

            DebugMessage();
            harmony.Patch(AccessTools.Method(typeof(JobGiver_OptimizeApparel), "TryGiveJob"), new HarmonyMethod(
                typeof(HarmonyPatches),
                nameof(DontOptimizeWerewolfApparel)));
        }

        
        // RimWorld.Building_Door
        public static void WerewolfCantOpen(Pawn p, ref bool __result)
        {
            __result = __result && p?.mindState?.mentalStateHandler?.CurState?.def != WWDefOf.ROM_WerewolfFury;
        }
        
        // Verse.AI.Pawn_PathFollower
        public static void PathOfNature(Pawn pawn, IntVec3 c, ref int __result)
        {
            if (pawn?.GetComp<CompWerewolf>() is not { } compWerewolf ||
                compWerewolf.CurrentWerewolfForm?.def != WWDefOf.ROM_Lupus)
            {
                return;
            }

            var num = c.x == pawn.Position.x || c.z == pawn.Position.z
                ? pawn.TicksPerMoveCardinal
                : pawn.TicksPerMoveDiagonal;

            //num += pawn.Map.pathGrid.CalculatedCostAt(c, false, pawn.Position);
            var edifice = c.GetEdifice(pawn.Map);
            if (edifice != null)
            {
                num += edifice.PathWalkCostFor(pawn);
            }

            if (num > 450)
            {
                num = 450;
            }

            if (pawn.jobs.curJob != null)
            {
                switch (pawn.jobs.curJob.locomotionUrgency)
                {
                    case LocomotionUrgency.Amble:
                        num *= 3;
                        if (num < 60)
                        {
                            num = 60;
                        }

                        break;
                    case LocomotionUrgency.Walk:
                        num *= 2;
                        if (num < 50)
                        {
                            num = 50;
                        }

                        break;
                    case LocomotionUrgency.Jog:
                        num *= 1;
                        break;
                    case LocomotionUrgency.Sprint:
                        num = Mathf.RoundToInt(num * 0.75f);
                        break;
                }
            }

            __result = Mathf.Max(num, 1);
        }

        
        // RimWorld.LordToil_AssaultColony
        public static void UpdateAllDuties_PostFix(LordToil_AssaultColony __instance)
        {
            foreach (var pawn in __instance.lord.ownedPawns)
            {
                if (pawn is { } p && p.GetComp<CompWerewolf>() is {IsWerewolf: true})
                {
                    p.mindState.duty = new PawnDuty(DefDatabase<DutyDef>.GetNamed("ROM_WerewolfAssault"));
                }
            }
        }

        // RimWorld.JobGiver_OptimizeApparel
        public static bool DontOptimizeWerewolfApparel(ref Job __result, Pawn pawn)
        {
            if (pawn?.GetComp<CompWerewolf>() is not { } ww || !ww.IsTransformed)
            {
                return true;
            }

            __result = null;
            return false;
        }
        
    }
}