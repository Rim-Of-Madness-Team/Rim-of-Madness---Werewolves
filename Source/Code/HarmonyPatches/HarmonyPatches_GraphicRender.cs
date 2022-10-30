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
            DebugMessage();
            harmony.Patch(AccessTools.Method(typeof(PawnGraphicSet), nameof(PawnGraphicSet.ResolveAllGraphics)),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(ResolveAllGraphicsWereWolf)));

            DebugMessage();
            harmony.Patch(
                AccessTools.Method(typeof(PawnRenderer), "RenderPawnInternal"),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(RenderPawnInternal)));
        }

        [HarmonyBefore("rimworld.erdelf.alien_race.main")]
        public static bool ResolveAllGraphicsWereWolf(PawnGraphicSet __instance)
        {
            if (Current.ProgramState != ProgramState.Playing)
            {
                return true;
            }

            var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (!pawn.Spawned)
            {
                return true;
            }

            var compWerewolf = pawn.GetComp<CompWerewolf>();
            if (compWerewolf == null || !compWerewolf.IsTransformed)
            {
                return true;
            }

            compWerewolf.CurrentWerewolfForm.bodyGraphicData = compWerewolf.CurrentWerewolfForm.def.graphicData;
            __instance.nakedGraphic = compWerewolf.CurrentWerewolfForm.bodyGraphicData.Graphic;
            __instance.ResolveApparelGraphics();
            PortraitsCache.SetDirty(pawn);
            return false;
        }

        
        // PawnRenderer.RenderPawnInternal
        private static bool RenderPawnInternal(PawnRenderer __instance, Vector3 rootLoc, bool renderBody,
            Rot4 bodyFacing,
            RotDrawMode bodyDrawType, PawnRenderFlags flags)
        {
            var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            CompWerewolf compWerewolf;
            bool result;
            if ((compWerewolf = pawn?.GetComp<CompWerewolf>()) != null && compWerewolf.IsTransformed)
            {
                if (compWerewolf.CurrentWerewolfForm.bodyGraphicData == null ||
                    __instance.graphics.nakedGraphic == null)
                {
                    compWerewolf.CurrentWerewolfForm.bodyGraphicData = compWerewolf.CurrentWerewolfForm.def.graphicData;
                    __instance.graphics.nakedGraphic = compWerewolf.CurrentWerewolfForm.bodyGraphicData.Graphic;
                }

                if (renderBody)
                {
                    var vector = rootLoc;
                    vector.y += 0.0046875f;
                    if (bodyDrawType == RotDrawMode.Rotting && !pawn.RaceProps.Humanlike &&
                        __instance.graphics.dessicatedGraphic != null && (flags & PawnRenderFlags.Portrait) == 0)
                    {
                        __instance.graphics.dessicatedGraphic.Draw(vector, bodyFacing, pawn);
                    }
                    else
                    {
                        var mesh = __instance.graphics.nakedGraphic.MeshAt(bodyFacing);
                        var list = __instance.graphics.MatsBodyBaseAt(bodyFacing, pawn.Dead, bodyDrawType);
                        foreach (var baseMat in list)
                        {
                            var damagedMat = __instance.graphics.flasher.GetDamagedMat(baseMat);
                            var vector2 = new Vector3(vector.x, vector.y, vector.z);
                            if ((flags & PawnRenderFlags.Portrait) != 0)
                            {
                                vector2.x *= 1f + (1f - compWerewolf.CurrentWerewolfForm.def.CustomPortraitDrawSize.x);
                                vector2.z *= 1f + (1f - compWerewolf.CurrentWerewolfForm.def.CustomPortraitDrawSize.y);
                            }
                            else
                            {
                                vector2 = Vector3.zero;
                            }

                            GenDraw.DrawMeshNowOrLater(mesh, vector + vector2, Quaternion.identity, damagedMat,
                                true);
                            vector.y += 0.0046875f;
                        }

                        if (bodyDrawType == 0)
                        {
                            var vector3 = rootLoc;
                            vector3.y += 0.01875f;
                            Traverse.Create(__instance).Field("woundOverlays").GetValue<PawnWoundDrawer>()
                                .RenderPawnOverlay(vector3, mesh, Quaternion.identity, true,
                                    PawnOverlayDrawer.OverlayLayer.Body, bodyFacing, false);
                        }
                    }
                }

                result = false;
            }
            else
            {
                result = true;
            }

            return result;
        }

        
        // private static RotDrawMode CurRotDrawMode(Pawn pawn)
        // {
        //     return pawn.Dead && pawn.Corpse != null ? pawn.Corpse.CurRotDrawMode : RotDrawMode.Fresh;
        // }

        // public static bool RenderPawnAt(PawnRenderer __instance, Vector3 drawLoc)
        // {
        //     var p = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
        //     if (p?.GetComp<CompWerewolf>() is not { } compWerewolf || !compWerewolf.IsTransformed)
        //     {
        //         return true;
        //     }
        //
        //     var loc = new Vector3(drawLoc.x, drawLoc.y, drawLoc.z);
        //     var quaternion = Quaternion.AngleAxis(0f, Vector3.up);
        //     var mesh = compWerewolf.CurrentWerewolfForm.bodyGraphicData.GraphicColoredFor(p).MeshAt(p.Rotation);
        //     var list = __instance.graphics.MatsBodyBaseAt(p.Rotation, CurRotDrawMode(p));
        //     foreach (var baseMat in list)
        //     {
        //         var damagedMat = __instance.graphics.flasher.GetDamagedMat(baseMat);
        //         GenDraw.DrawMeshNowOrLater(mesh, drawLoc, quaternion, damagedMat, false);
        //         loc.y += 0.00390625f;
        //     }
        //
        //     if (CurRotDrawMode(p) != RotDrawMode.Fresh)
        //     {
        //         return false;
        //     }
        //
        //     loc.y += 0.01953125f;
        //     Traverse.Create(__instance).Field("woundOverlays").GetValue<PawnWoundDrawer>()
        //         .RenderOverBody(drawLoc, mesh, quaternion, false, BodyTypeDef.WoundLayer.Body, p.Rotation);
        //
        //     return false;
        // }

        // public static Color WerewolfColor(Pawn p, WerewolfForm w)
        // {
        //     var hairColor = new Color(p.story.HairColor.r, p.story.HairColor.g, p.story.HairColor.b);
        //     return w.def != WWDefOf.ROM_Glabro ? hairColor : Color.white;
        // }

        // public static bool RenderWerewolf(PawnGraphicSet __instance)
        // {
        //     var p = __instance.pawn;
        //     if (p?.GetComp<CompWerewolf>() is not { } compWerewolf || !compWerewolf.IsTransformed)
        //     {
        //         return true;
        //     }
        //
        //     __instance.ClearCache();
        //     if (compWerewolf.CurrentWerewolfForm.bodyGraphicData != null && __instance.nakedGraphic != null)
        //     {
        //         return false;
        //     }
        //
        //     compWerewolf.CurrentWerewolfForm.bodyGraphicData = compWerewolf.CurrentWerewolfForm.def.graphicData;
        //     __instance.nakedGraphic = GraphicDatabase.Get<Graphic_Multi>(
        //         compWerewolf.CurrentWerewolfForm.bodyGraphicData.texPath, ShaderDatabase.Cutout,
        //         compWerewolf.CurrentWerewolfForm.bodyGraphicData.drawSize,
        //         WerewolfColor(p, compWerewolf.CurrentWerewolfForm));
        //     __instance.headGraphic = null;
        //     __instance.hairGraphic = null;
        //
        //     return false;
        // }
        
    }
}