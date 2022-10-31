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
            //DebugMessage();
            harmony.Patch(AccessTools.Method(typeof(ThingWithComps), nameof(ThingWithComps.InitializeComps)), null,
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(InitializeWWComps)));
            
            //DebugMessage()
            //Werewolf trait needs a werewolf gene if Biotech is installed 
            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn),
                    new[] { typeof(PawnGenerationRequest) }),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Post_GeneratePawn)));
        }
        
        public static void Post_GeneratePawn(Pawn __result)
        {
            if (!ModsConfig.BiotechActive) return;
            if (__result?.story?.traits?.GetTrait(WWDefOf.ROM_Werewolf) is Trait werewolfTrait)
            {
                Gene werewolfGene = null;

                switch (werewolfTrait.Degree)
                {
                    case -1:
                    case 0:
                    case 1:
                    case 2:
                        werewolfGene = __result?.genes?.GetGene(WWDefOf.ROMW_WerewolfGene) ??
                                       GeneMaker.MakeGene(WWDefOf.ROMW_WerewolfGene, __result);
                        break;
                    case 3:
                        werewolfGene = __result?.genes?.GetGene(WWDefOf.ROM_WerewolfMetisSterile) ??
                                       GeneMaker.MakeGene(WWDefOf.ROM_WerewolfMetisSterile, __result);
                        break;
                }

                if (werewolfGene != null)
                {
                        __result.genes.Endogenes.Add(werewolfGene);
                        werewolfTrait.sourceGene = werewolfGene;
                }
            }
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