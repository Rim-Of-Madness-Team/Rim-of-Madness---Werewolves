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
        public static void HarmonyPatches_GraphicRender(Harmony harmony)
        {
            //DebugMessage();
            harmony.Patch(AccessTools.Method(typeof(PawnGraphicSet), nameof(PawnGraphicSet.ResolveAllGraphics)),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(ResolveAllGraphicsWereWolf)));
            
            //DebugMessage();
            // harmony.Patch(AccessTools.Method(typeof(PawnRenderer), name: "RenderPawnInternal",
            //         new[] { typeof(Vector3), typeof(float), typeof(bool), typeof(Rot4), typeof(RotDrawMode), typeof(PawnRenderFlags)}), 
            //     postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(RenderPawnInternalPostFix)));
        }

        //Biotech Baby Swaddling
        public delegate Color SwaddleColor(PawnGraphicSet graphicSet);

        public static readonly SwaddleColor swaddleColor =
            AccessTools.MethodDelegate<SwaddleColor>(AccessTools.Method(typeof(PawnGraphicSet), "SwaddleColor"));

        [HarmonyBefore("rimworld.erdelf.alien_race.main")]
        public static bool ResolveAllGraphicsWereWolf(PawnGraphicSet __instance)
        {
            var pawn = __instance.pawn;
            var compWerewolf = pawn.GetComp<CompWerewolf>();
            if (compWerewolf == null || !compWerewolf.IsTransformed)
            {
                return true;
            }

            var graphic = compWerewolf.CurrentWerewolfForm.def.graphicData.Graphic;
            __instance.nakedGraphic =
                //graphic.GetColoredVersion(graphic.Shader, pawn.story.HairColor, pawn.story.HairColor)
                compWerewolf.CurrentWerewolfForm.def.graphicData.GraphicColoredFor(__instance.pawn);
            __instance.rottingGraphic = null;
            __instance.dessicatedGraphic = null;
            __instance.headGraphic = null;
            __instance.desiccatedHeadGraphic = null;
            __instance.skullGraphic = null;
            __instance.hairGraphic = null;
            __instance.headStumpGraphic = null;
            __instance.desiccatedHeadStumpGraphic = null;

            if (ModLister.BiotechInstalled)
            {
                __instance.furCoveredGraphic = null;
            }

            if (ModsConfig.BiotechActive)
            {
                __instance.swaddledBabyGraphic = GraphicDatabase.Get<Graphic_Multi>(
                    "Things/Pawn/Humanlike/Apparel/SwaddledBaby/Swaddled_Child", ShaderDatabase.Cutout, Vector2.one,
                    swaddleColor(__instance));
            }

            __instance.faceTattooGraphic = null;
            __instance.bodyTattooGraphic = null;
            __instance.beardGraphic = null;

            __instance.ResolveApparelGraphics();
            __instance.ResolveGeneGraphics();

            PortraitsCache.SetDirty(pawn);
            GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);

            return false;
        }
    }
}