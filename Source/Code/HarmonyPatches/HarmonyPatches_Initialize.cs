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
        public static void HarmonyPatches_Initialize(Harmony harmony)
        {
            DebugMessage();
            harmony.Patch(AccessTools.Method(typeof(ThingWithComps), nameof(ThingWithComps.InitializeComps)), null,
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(InitializeWWComps)));
        }
        
        // Verse.ThingWithComps
        public static void InitializeWWComps(ThingWithComps __instance)
        {
            if (!__instance.def.IsRangedWeapon)
            {
                return;
            }

            var comps = (List<ThingComp>) AccessTools.Field(typeof(ThingWithComps), "comps").GetValue(__instance);
            var thingComp = (ThingComp) Activator.CreateInstance(typeof(CompSilverTreated));
            thingComp.parent = __instance;
            comps.Add(thingComp);
            thingComp.Initialize(null);
        }
    }
}