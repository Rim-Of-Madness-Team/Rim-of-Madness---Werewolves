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
        public static void HarmonyPatches_Sounds(Harmony harmony)
        {
            //DebugMessage();
            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), "SoundHitPawn"), new HarmonyMethod(
                typeof(HarmonyPatches),
                nameof(SoundHitPawnPrefix)));

            //DebugMessage();
            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), "SoundMiss"), new HarmonyMethod(
                typeof(HarmonyPatches),
                nameof(SoundMiss_Prefix)));
        }

        // RimWorld.Verb_MeleeAttack
        public static void SoundMiss_Prefix(Verb_MeleeAttack __instance)
        {
            if (__instance.caster is not Pawn pawn || pawn.GetComp<CompWerewolf>() is not { } w ||
                !w.IsTransformed)
            {
                return;
            }

            if (w.CurrentWerewolfForm.def.attackSound is not { } soundToPlay)
            {
                return;
            }

            if (Rand.Value < 0.5f)
            {
                soundToPlay.PlayOneShot(new TargetInfo(pawn));
            }
        }


        public static void SoundHitPawnPrefix(Verb_MeleeAttack __instance)
        {
            if (__instance.caster is not Pawn pawn || pawn.GetComp<CompWerewolf>() is not { } w ||
                !w.IsTransformed)
            {
                return;
            }

            if (w.CurrentWerewolfForm.def.attackSound is not { } soundToPlay)
            {
                return;
            }

            if (Rand.Value < 0.5f)
            {
                soundToPlay.PlayOneShot(new TargetInfo(pawn));
            }
        }
    }
}