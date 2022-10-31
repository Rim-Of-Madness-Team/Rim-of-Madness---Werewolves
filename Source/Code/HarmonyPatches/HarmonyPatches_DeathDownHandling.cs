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
        public static void HarmonyPatches_DeathDownHandling(Harmony harmony)
        {
            //DebugMessage();
            harmony.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.SetDead)),
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(IgnoreDoubleDeath)));

            //DebugMessage();
            harmony.Patch(AccessTools.Method(typeof(HealthUtility), nameof(HealthUtility.DamageUntilDowned)),
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(DebugDownWerewolf)));
            
            //DebugMessage();
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.Kill)), new HarmonyMethod(typeof(HarmonyPatches),
                nameof(WerewolfKill)));

            //DebugMessage();
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.Destroy)), new HarmonyMethod(
                typeof(HarmonyPatches),
                nameof(WerewolfDestroy)));
            
            //DebugMessage()
            //harmony.Patch(AccessTools.Method(typeof(PawnRenderer), name: "DrawHeadHair"),
            //    null, null,
            //    transpiler: new HarmonyMethod(typeof(HarmonyDwarves), nameof(BeardCheckTranspiler)));

        }

                
        // Verse.Pawn_HealthTracker
        public static bool IgnoreDoubleDeath(ref Pawn_HealthTracker __instance)
        {
            return !__instance.Dead;
        }
        
        // Verse.HealthUtility
        public static void DebugDownWerewolf(Pawn p)
        {
            if (p?.GetComp<CompWerewolf>() is {IsWerewolf: true, IsTransformed: true} w)
            {
                w.TransformBack();
            }
        }
        
        // Verse.Pawn
        public static void WerewolfKill(Pawn __instance, DamageInfo? dinfo)
        {
            if (__instance?.GetComp<CompWerewolf>() is not { } w || !w.IsTransformed || w.IsReverting)
            {
                return;
            }
            w.TransformBack(true);
        }

        
        /// Werewolves must revert before being destroyed.
        public static void WerewolfDestroy(Pawn __instance, DestroyMode mode = DestroyMode.Vanish)
        {
            if (__instance?.GetComp<CompWerewolf>() is not { } w || !w.IsWerewolf || !w.IsTransformed)
            {
                return;
            }
            w.TransformBack(true);
        }

        
    }
}