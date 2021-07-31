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
    [StaticConstructorOnStartup]
    internal static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("rimworld.jecrell.werewolves");
            harmony.Patch(AccessTools.Property(typeof(Pawn), nameof(Pawn.BodySize)).GetGetMethod(), null,
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(WerewolfBodySize)));
            //Log.Message("1");
            harmony.Patch(AccessTools.Property(typeof(Pawn), nameof(Pawn.HealthScale)).GetGetMethod(), null,
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(WerewolfHealthScale)));

            //Log.Message("2");
            harmony.Patch(AccessTools.Method(typeof(Building_Door), nameof(Building_Door.PawnCanOpen)), null,
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(WerewolfCantOpen)));

            //Log.Message("3");
            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), "SoundHitPawn"), new HarmonyMethod(
                typeof(HarmonyPatches),
                nameof(SoundHitPawnPrefix)));

            //Log.Message("4");
            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), "SoundMiss"), new HarmonyMethod(
                typeof(HarmonyPatches),
                nameof(SoundMiss_Prefix)));

            //Log.Message("5");
            harmony.Patch(AccessTools.Method(typeof(ThingWithComps), nameof(ThingWithComps.InitializeComps)), null,
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(InitializeWWComps)));
            //Log.Message("6");
            harmony.Patch(
                AccessTools.Method(typeof(Pawn_PathFollower), "CostToMoveIntoCell",
                    new[] {typeof(Pawn), typeof(IntVec3)}), null, new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(PathOfNature)));
            //Log.Message("7");

            harmony.Patch(AccessTools.Method(typeof(LordToil_AssaultColony), "UpdateAllDuties"), null,
                new HarmonyMethod(typeof(HarmonyPatches),
                    nameof(UpdateAllDuties_PostFix)));
            //Log.Message("8");

            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.Kill)), new HarmonyMethod(typeof(HarmonyPatches),
                nameof(WerewolfKill)));
            //Log.Message("9");

            // Cant find a replacement for this and im tired and its late
            //harmony.Patch(AccessTools.Method(typeof(Pawn_GuestTracker), "resistance"),
            //    new HarmonyMethod(
            //        typeof(HarmonyPatches),
            //        nameof(UnrecruitableSworn)));
            //Log.Message("10");

            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.Destroy)), new HarmonyMethod(
                typeof(HarmonyPatches),
                nameof(WerewolfDestroy)));
            //Log.Message("11");

            harmony.Patch(AccessTools.Method(typeof(TickManager), nameof(TickManager.DebugSetTicksGame)), null,
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(MoonTicksUpdate)));
            //Log.Message("12");

            harmony.Patch(AccessTools.Method(typeof(Dialog_DebugActionsMenu), "DoListingItems"), null,
                new HarmonyMethod(typeof(HarmonyPatches),
                    nameof(DebugMoonActions)));
            //Log.Message("13");

            harmony.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.SetDead)),
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(IgnoreDoubleDeath)));
            //Log.Message("14");

            harmony.Patch(AccessTools.Method(typeof(HealthUtility), nameof(HealthUtility.DamageUntilDowned)),
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(DebugDownWerewolf)));
            //Log.Message("15");

            harmony.Patch(AccessTools.Method(typeof(JobGiver_OptimizeApparel), "TryGiveJob"), new HarmonyMethod(
                typeof(HarmonyPatches),
                nameof(DontOptimizeWerewolfApparel)));
            //Log.Message("16");

            harmony.Patch(typeof(DamageWorker_AddInjury)
                    .GetMethods(AccessTools.all).First(mi => mi.GetParameters().Length >= 4 &&
                                                             mi.GetParameters().ElementAt(1).ParameterType ==
                                                             typeof(Hediff_Injury)),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(WerewolfDmgFixFinalizeAndAddInjury)));
            //Log.Message("17");

            harmony.Patch(AccessTools.Method(typeof(Scenario), nameof(Scenario.Notify_PawnGenerated)), null,
                new HarmonyMethod(typeof(HarmonyPatches), nameof(AddRecentWerewolves)));
            //Log.Message("18");

            harmony.Patch(AccessTools.Method(typeof(PawnGraphicSet), nameof(PawnGraphicSet.ResolveAllGraphics)),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(ResolveAllGraphicsWereWolf)));
            //Log.Message("19");

            harmony.Patch(
                AccessTools.Method(typeof(PawnRenderer), "RenderPawnInternal"),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(RenderPawnInternal)));
            //Log.Message("20");

            //harmony.Patch(
            //    AccessTools.Method(typeof(PawnRenderer), "RenderPawnAt"), null, null,
            //    new HarmonyMethod(typeof(PawnRenderer_RenderPawnAt_Patch), "Transpiler"));

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
            var value = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            CompWerewolf compWerewolf;
            bool result;
            if ((compWerewolf = value?.GetComp<CompWerewolf>()) != null && compWerewolf.IsTransformed)
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
                    if (bodyDrawType == RotDrawMode.Rotting && !value.RaceProps.Humanlike &&
                        __instance.graphics.dessicatedGraphic != null && (flags & PawnRenderFlags.Portrait) == 0)
                    {
                        __instance.graphics.dessicatedGraphic.Draw(vector, bodyFacing, value);
                    }
                    else
                    {
                        var mesh = __instance.graphics.nakedGraphic.MeshAt(bodyFacing);
                        var list = __instance.graphics.MatsBodyBaseAt(bodyFacing, bodyDrawType);
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
                                .RenderOverBody(vector3, mesh, Quaternion.identity, true,
                                    BodyTypeDef.WoundLayer.Body, bodyFacing);
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


        private static RotDrawMode CurRotDrawMode(Pawn pawn)
        {
            return pawn.Dead && pawn.Corpse != null ? pawn.Corpse.CurRotDrawMode : RotDrawMode.Fresh;
        }

        public static bool RenderPawnAt(PawnRenderer __instance, Vector3 drawLoc)
        {
            var p = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (p?.GetComp<CompWerewolf>() is not { } compWerewolf || !compWerewolf.IsTransformed)
            {
                return true;
            }

            var loc = new Vector3(drawLoc.x, drawLoc.y, drawLoc.z);
            var quaternion = Quaternion.AngleAxis(0f, Vector3.up);
            var mesh = compWerewolf.CurrentWerewolfForm.bodyGraphicData.GraphicColoredFor(p).MeshAt(p.Rotation);
            var list = __instance.graphics.MatsBodyBaseAt(p.Rotation, CurRotDrawMode(p));
            foreach (var baseMat in list)
            {
                var damagedMat = __instance.graphics.flasher.GetDamagedMat(baseMat);
                GenDraw.DrawMeshNowOrLater(mesh, drawLoc, quaternion, damagedMat, false);
                loc.y += 0.00390625f;
            }

            if (CurRotDrawMode(p) != RotDrawMode.Fresh)
            {
                return false;
            }

            loc.y += 0.01953125f;
            Traverse.Create(__instance).Field("woundOverlays").GetValue<PawnWoundDrawer>()
                .RenderOverBody(drawLoc, mesh, quaternion, false, BodyTypeDef.WoundLayer.Body, p.Rotation);

            return false;
        }

        public static Color WerewolfColor(Pawn p, WerewolfForm w)
        {
            var hairColor = new Color(p.story.hairColor.r, p.story.hairColor.g, p.story.hairColor.b);
            return w.def != WWDefOf.ROM_Glabro ? hairColor : Color.white;
        }

        public static bool RenderWerewolf(PawnGraphicSet __instance)
        {
            var p = __instance.pawn;
            if (p?.GetComp<CompWerewolf>() is not { } compWerewolf || !compWerewolf.IsTransformed)
            {
                return true;
            }

            __instance.ClearCache();
            if (compWerewolf.CurrentWerewolfForm.bodyGraphicData != null && __instance.nakedGraphic != null)
            {
                return false;
            }

            compWerewolf.CurrentWerewolfForm.bodyGraphicData = compWerewolf.CurrentWerewolfForm.def.graphicData;
            __instance.nakedGraphic = GraphicDatabase.Get<Graphic_Multi>(
                compWerewolf.CurrentWerewolfForm.bodyGraphicData.texPath, ShaderDatabase.Cutout,
                compWerewolf.CurrentWerewolfForm.bodyGraphicData.drawSize,
                WerewolfColor(p, compWerewolf.CurrentWerewolfForm));
            __instance.headGraphic = null;
            __instance.hairGraphic = null;

            return false;
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

        public static bool ShouldModifyDamage(Pawn instigator)
        {
            return !instigator?.TryGetComp<CompWerewolf>()?.IsTransformed ?? false;
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

        // RimWorld.JobGiver_OptimizeApparel
        public static bool DontOptimizeWerewolfApparel(ref Job __result, Pawn pawn)
        {
            if (pawn?.GetComp<CompWerewolf>() is not { } ww || !ww.IsTransformed)
            {
                return true;
            }

            __result = null;
            return false;
        }

        // Verse.Pawn_HealthTracker
        public static bool IgnoreDoubleDeath(ref Pawn_HealthTracker __instance)
        {
            return !__instance.Dead;
        }

        // Verse.Pawn_HealthTracker
        public static bool DontWarnOnNonExistingThings(ref bool __result, ref ThingOwner __instance, Thing thing,
            IntVec3 dropLoc, Map map, ThingPlaceMode mode, out Thing lastResultingThing,
            Action<Thing, int> placedAction = null, Predicate<IntVec3> nearPlaceValidator = null,
            bool playDropSound = true)
        {
            lastResultingThing = null;
            if (!__instance.Contains(thing))
            {
                return true;
            }

            __result = false;
            return false;
        }

        // Verse.HealthUtility
        public static void DebugDownWerewolf(Pawn p)
        {
            if (p?.GetComp<CompWerewolf>() is {IsWerewolf: true, IsTransformed: true} w)
            {
                w.TransformBack();
            }
        }

        // Verse.Dialog_DebugActionsMenu
        public static void DebugMoonActions(Dialog_DebugActionsMenu __instance)
        {
            AccessTools.Method(typeof(Dialog_DebugActionsMenu), "DoLabel")
                .Invoke(__instance, new object[] {"Tools - Werewolves"});

            AccessTools.Method(typeof(Dialog_DebugActionsMenu), "DebugToolMap").Invoke(__instance, new object[]
            {
                "Give Lycanthropy (Normal)", new Action(() =>
                {
                    var pawn = Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).Where(t => t is Pawn)
                        .Cast<Pawn>().FirstOrDefault();
                    if (pawn == null)
                    {
                        return;
                    }

                    if (!pawn.IsWerewolf())
                    {
                        pawn.story.traits.GainTrait(new Trait(WWDefOf.ROM_Werewolf));
                        //pawn.health.AddHediff(VampDefOf.ROM_Vampirism, null, null);
                        pawn.Drawer.Notify_DebugAffected();
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, pawn.LabelShort + " is now a werewolf");
                    }
                    else
                    {
                        Messages.Message(pawn.LabelCap + " is already a werewolf.", MessageTypeDefOf.RejectInput);
                    }
                }),
                false
            });

            AccessTools.Method(typeof(Dialog_DebugActionsMenu), "DebugToolMap").Invoke(__instance, new object[]
            {
                "Give Lycanthropy (Metis Chance)", new Action(() =>
                {
                    var pawn = Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).Where(t => t is Pawn)
                        .Cast<Pawn>().FirstOrDefault();
                    if (pawn == null)
                    {
                        return;
                    }

                    if (!pawn.IsWerewolf())
                    {
                        pawn.story.traits.GainTrait(new Trait(WWDefOf.ROM_Werewolf, -1));
                        //pawn.health.AddHediff(VampDefOf.ROM_Vampirism, null, null);
                        pawn.Drawer.Notify_DebugAffected();
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, pawn.LabelShort + " is now a werewolf");
                    }
                    else
                    {
                        Messages.Message(pawn.LabelCap + " is already a werewolf.", MessageTypeDefOf.RejectInput);
                    }
                }),
                false
            });

            AccessTools.Method(typeof(Dialog_DebugActionsMenu), "DebugToolMap").Invoke(__instance, new object[]
            {
                "Remove Lycanthropy", new Action(() =>
                {
                    var pawn = Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).Where(t => t is Pawn)
                        .Cast<Pawn>().FirstOrDefault();
                    if (pawn == null)
                    {
                        return;
                    }

                    if (pawn.IsWerewolf())
                    {
                        if (pawn.CompWW().IsTransformed)
                        {
                            pawn.CompWW().TransformBack();
                        }

                        pawn.story.traits.allTraits.RemoveAll(x =>
                            x.def == WWDefOf.ROM_Werewolf); //GainTrait(new Trait(WWDefOf.ROM_Werewolf, -1));
                        //pawn.health.AddHediff(VampDefOf.ROM_Vampirism, null, null);
                        pawn.Drawer.Notify_DebugAffected();
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, pawn.LabelShort + " is no longer a werewolf");
                    }
                    else
                    {
                        Messages.Message(pawn.LabelCap + " is not a werewolf.", MessageTypeDefOf.RejectInput);
                    }
                }),
                false
            });

            AccessTools.Method(typeof(Dialog_DebugActionsMenu), "DebugAction").Invoke(__instance, new object[]
            {
                "Regenerate Moons",
                new Action(() => Find.World.GetComponent<WorldComponent_MoonCycle>().DebugRegenerateMoons(Find.World)),
                false
            });

            AccessTools.Method(typeof(Dialog_DebugActionsMenu), "DebugAction").Invoke(__instance, new object[]
            {
                "Next Full Moon",
                new Action(() => Find.World.GetComponent<WorldComponent_MoonCycle>().DebugTriggerNextFullMoon()),
                false
            });
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


        /// Werewolves must revert before being destroyed.
        public static void WerewolfDestroy(Pawn __instance, DestroyMode mode = DestroyMode.Vanish)
        {
            if (__instance?.GetComp<CompWerewolf>() is not { } w || !w.IsWerewolf || !w.IsTransformed)
            {
                return;
            }

            //Log.Message("WerewolfDestroy");
            w.TransformBack(true);
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


        // Verse.Pawn
        public static void WerewolfKill(Pawn __instance, DamageInfo? dinfo)
        {
            if (__instance?.GetComp<CompWerewolf>() is not { } w || !w.IsTransformed || w.IsReverting)
            {
                return;
            }

            //Log.Message("WerewolfKill");
            w.TransformBack(true);
        }

        // RimWorld.LordToil_AssaultColony
        public static void UpdateAllDuties_PostFix(LordToil_AssaultColony __instance)
        {
            foreach (var pawn in __instance.lord.ownedPawns)
            {
                if (pawn is { } p && p.GetComp<CompWerewolf>() is {IsWerewolf: true})
                {
                    p.mindState.duty = new PawnDuty(DefDatabase<DutyDef>.GetNamed("ROM_WerewolfAssault"));
                }
            }
        }

        // Verse.AI.Pawn_PathFollower
        public static void PathOfNature(Pawn pawn, IntVec3 c, ref int __result)
        {
            if (pawn?.GetComp<CompWerewolf>() is not { } compWerewolf ||
                compWerewolf.CurrentWerewolfForm?.def != WWDefOf.ROM_Lupus)
            {
                return;
            }

            var num = c.x == pawn.Position.x || c.z == pawn.Position.z
                ? pawn.TicksPerMoveCardinal
                : pawn.TicksPerMoveDiagonal;

            //num += pawn.Map.pathGrid.CalculatedCostAt(c, false, pawn.Position);
            var edifice = c.GetEdifice(pawn.Map);
            if (edifice != null)
            {
                num += edifice.PathWalkCostFor(pawn);
            }

            if (num > 450)
            {
                num = 450;
            }

            if (pawn.jobs.curJob != null)
            {
                switch (pawn.jobs.curJob.locomotionUrgency)
                {
                    case LocomotionUrgency.Amble:
                        num *= 3;
                        if (num < 60)
                        {
                            num = 60;
                        }

                        break;
                    case LocomotionUrgency.Walk:
                        num *= 2;
                        if (num < 50)
                        {
                            num = 50;
                        }

                        break;
                    case LocomotionUrgency.Jog:
                        num *= 1;
                        break;
                    case LocomotionUrgency.Sprint:
                        num = Mathf.RoundToInt(num * 0.75f);
                        break;
                }
            }

            __result = Mathf.Max(num, 1);
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

        // RimWorld.Building_Door
        public static void WerewolfCantOpen(Pawn p, ref bool __result)
        {
            __result = __result && p?.mindState?.mentalStateHandler?.CurState?.def != WWDefOf.ROM_WerewolfFury;
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
    }
}