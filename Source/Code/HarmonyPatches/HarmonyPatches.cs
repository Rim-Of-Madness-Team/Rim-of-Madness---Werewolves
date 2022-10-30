using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Werewolf
{
    [StaticConstructorOnStartup]
    

    
    internal static partial class HarmonyPatches
    {
        static void DebugMessage(
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string caller = null)
        {
            Log.Message( caller + " " + lineNumber);
        }
        
        static HarmonyPatches()
        {
            var harmony = new Harmony("rimworld.jecrell.werewolves");

            HarmonyPatches_AIJobsEtc(harmony);
            HarmonyPatches_DeathDownHandling(harmony);
            HarmonyPatches_GraphicRender(harmony);
            HarmonyPatches_HealthAndDamages(harmony);
            HarmonyPatches_Initialize(harmony);
            HarmonyPatches_Moon(harmony);
            HarmonyPatches_Scenario(harmony);
            HarmonyPatches_Sounds(harmony);

            // Cant find a replacement for this and im tired and its late
            //harmony.Patch(AccessTools.Method(typeof(Pawn_GuestTracker), "resistance"),
            //    new HarmonyMethod(
            //        typeof(HarmonyPatches),
            //        nameof(UnrecruitableSworn)));
            //Log.Message("10");

        }


        // RimWorld.PawnUtility
        public static void UnrecruitableSworn(ref float __result, Pawn ___pawn)
        {
            if (___pawn?.story?.traits?.allTraits?.FirstOrDefault(x =>
                    x.def == WWDefOf.ROM_Werewolf && x.Degree == 2) !=
                null)
            {
                __result = 100f;
            }
        }






    }
}