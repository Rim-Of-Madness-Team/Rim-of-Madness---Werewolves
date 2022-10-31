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
        public static void HarmonyPatches_Moon(Harmony harmony)
        {
            //DebugMessage();
            harmony.Patch(AccessTools.Method(typeof(TickManager), nameof(TickManager.DebugSetTicksGame)), null,
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(MoonTicksUpdate)));
        }

        // Verse.TickManager
        public static void MoonTicksUpdate(int newTicksGame)
        {
            if (newTicksGame <= Find.TickManager.TicksGame + GenDate.TicksPerDay + 1000)
            {
                Find.World.GetComponent<WorldComponent_MoonCycle>().AdvanceOneDay();
            }
            else if (newTicksGame <= Find.TickManager.TicksGame + GenDate.TicksPerQuadrum + 1000)
            {
                Find.World.GetComponent<WorldComponent_MoonCycle>().AdvanceOneQuadrum();
            }
        }
    }
}