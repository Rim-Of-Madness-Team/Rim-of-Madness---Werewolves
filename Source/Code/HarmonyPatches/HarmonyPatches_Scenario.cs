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
        public static void HarmonyPatches_Scenario(Harmony harmony)
        {
            DebugMessage();
            harmony.Patch(AccessTools.Method(typeof(Scenario), nameof(Scenario.Notify_PawnGenerated)), null,
                new HarmonyMethod(typeof(HarmonyPatches), nameof(AddRecentWerewolves)));
        }
        
        // RimWorld.Scenario
        public static void AddRecentWerewolves(Pawn pawn)
        {
            if (!pawn.IsWerewolf())
            {
                return;
            }

            var recentWerewolves = Find.World.GetComponent<WorldComponent_MoonCycle>().recentWerewolves;
            recentWerewolves?.Add(pawn, 1);
        }

    }
}