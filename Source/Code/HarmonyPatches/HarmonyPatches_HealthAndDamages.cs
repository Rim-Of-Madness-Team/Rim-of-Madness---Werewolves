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
        public static void HarmonyPatches_HealthAndDamages(Harmony harmony)
        {
            //DebugMessage();
            harmony.Patch(AccessTools.Property(typeof(Pawn), nameof(Pawn.BodySize)).GetGetMethod(), null,
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(WerewolfBodySize)));

            //DebugMessage();
            harmony.Patch(AccessTools.Property(typeof(Pawn), nameof(Pawn.HealthScale)).GetGetMethod(), null,
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(WerewolfHealthScale)));

            //Damage for Werewolves are decimated (10% of normal) unless they are wielding silver weapons
            //DebugMessage();
            harmony.Patch(typeof(DamageWorker_AddInjury)
                    .GetMethods(AccessTools.all).First(mi => mi.GetParameters().Length >= 4 &&
                                                             mi.GetParameters().ElementAt(1).ParameterType ==
                                                             typeof(Hediff_Injury)),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(WerewolfDmgFixFinalizeAndAddInjury)));
            
        }

        
        // Verse.Pawn
        public static void WerewolfBodySize(Pawn __instance, ref float __result)
        {
            if (__instance?.GetComp<CompWerewolf>() is {IsWerewolf: true, IsTransformed: true} w)
            {
                __result = w.CurrentWerewolfForm
                    .FormBodySize; //Mathf.Clamp((__result * w.CurrentWerewolfForm.def.sizeFactor) + (w.CurrentWerewolfForm.level * 0.1f), __result, __result * (w.CurrentWerewolfForm.def.sizeFactor * 2));
            }
        }

        // Verse.Pawn
        public static void WerewolfHealthScale(Pawn __instance, ref float __result)
        {
            if (__instance?.GetComp<CompWerewolf>() is {IsWerewolf: true, IsTransformed: true} w)
            {
                __result = w.CurrentWerewolfForm
                    .FormHealthScale; //Mathf.Clamp((__result * w.CurrentWerewolfForm.def.healthFactor) + (w.CurrentWerewolfForm.level * 0.1f), __result, __result * (w.CurrentWerewolfForm.def.healthFactor * 2));
            }
        }


        //public class DamageWorker_AddInjury : DamageWorker
        public static void WerewolfDmgFixFinalizeAndAddInjury(Pawn pawn, ref Hediff_Injury injury,
            ref DamageInfo dinfo)
        {
            if (!(dinfo.Amount > 0) || pawn.TryGetComp<CompWerewolf>() is not { } ww || !ww.IsWerewolf ||
                ww.CurrentWerewolfForm == null)
            {
                return;
            }

            if (dinfo.Instigator is not Pawn a || !ShouldModifyDamage(a))
            {
                return;
            }

            if (a.equipment?.Primary is not { } b || b.IsSilverTreated())
            {
                return;
            }

            var math = (int) dinfo.Amount -
                       (int) (dinfo.Amount *
                              ww.CurrentWerewolfForm.DmgImmunity); //10% damage. Decimated damage.
            dinfo.SetAmount(math);
            injury.Severity = math;
            //Log.Message(dinfo.Amount.ToString());
        }
        private static bool ShouldModifyDamage(Pawn instigator)
        {
            return !instigator?.TryGetComp<CompWerewolf>()?.IsTransformed ?? false;
        }

    }
}