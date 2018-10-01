using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Werewolf
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.jecrell.werewolves");
            //HarmonyInstance.DEBUG = true;
            harmony.Patch(AccessTools.Property(typeof(Pawn), nameof(Pawn.BodySize)).GetGetMethod(), null,
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(WerewolfBodySize)), null);
            harmony.Patch(AccessTools.Property(typeof(Pawn), nameof(Pawn.HealthScale)).GetGetMethod(), null,
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(WerewolfHealthScale)), null);

            //Log.Message("2");

            harmony.Patch(AccessTools.Method(typeof(Building_Door), nameof(Building_Door.PawnCanOpen)), null,
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(WerewolfCantOpen)), null);
            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), "SoundHitPawn"), new HarmonyMethod(
                typeof(HarmonyPatches),
                nameof(SoundHitPawnPrefix)), null);
            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), "SoundMiss"), new HarmonyMethod(
                typeof(HarmonyPatches),
                nameof(SoundMiss_Prefix)), null);
            // harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"), null, new HarmonyMethod(typeof(HarmonyPatches),
            //nameof(OrderForSilverTreatment)));
            harmony.Patch(AccessTools.Method(typeof(ThingWithComps), nameof(ThingWithComps.InitializeComps)), null,
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(InitializeWWComps)));
            harmony.Patch(
                AccessTools.Method(typeof(Pawn_PathFollower), "CostToMoveIntoCell",
                    new[] {typeof(Pawn), typeof(IntVec3)}), null, new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(PathOfNature)), null);
            harmony.Patch(AccessTools.Method(typeof(LordToil_AssaultColony), "UpdateAllDuties"), null,
                new HarmonyMethod(typeof(HarmonyPatches),
                    nameof(UpdateAllDuties_PostFix)), null);
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.Kill)), new HarmonyMethod(typeof(HarmonyPatches),
                nameof(WerewolfKill)), null);
            harmony.Patch(AccessTools.Method(typeof(PawnUtility), nameof(PawnUtility.RecruitDifficulty)),
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(UnrecruitableSworn)), null);
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.Destroy)), new HarmonyMethod(
                typeof(HarmonyPatches),
                nameof(WerewolfDestroy)), null);
            harmony.Patch(AccessTools.Method(typeof(TickManager), nameof(TickManager.DebugSetTicksGame)), null,
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(MoonTicksUpdate)), null);
            harmony.Patch(AccessTools.Method(typeof(Dialog_DebugActionsMenu), "DoListingItems_MapActions"), null,
                new HarmonyMethod(typeof(HarmonyPatches),
                    nameof(DebugMoonActions)), null);
            harmony.Patch(AccessTools.Method(typeof(HealthUtility), nameof(HealthUtility.DamageUntilDowned)),
                new HarmonyMethod(
                    typeof(HarmonyPatches),
                    nameof(DebugDownWerewolf)), null);
            harmony.Patch(AccessTools.Method(typeof(JobGiver_OptimizeApparel), "TryGiveJob"), new HarmonyMethod(
                typeof(HarmonyPatches),
                nameof(DontOptimizeWerewolfApparel)), null);
            harmony.Patch((typeof(DamageWorker_AddInjury).GetMethods(AccessTools.all)
                    .Where(mi => mi.GetParameters().Count() >= 4 &&
                                 mi.GetParameters().ElementAt(1).ParameterType == typeof(Hediff_Injury)).First()),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(WerewolfDmgFixFinalizeAndAddInjury)), null);
            harmony.Patch(AccessTools.Method(typeof(Scenario), nameof(Scenario.Notify_PawnGenerated)), null,
                new HarmonyMethod(typeof(HarmonyPatches), nameof(AddRecentWerewolves)));


            harmony.Patch(
                AccessTools.Method(typeof(PawnRenderer), "RenderPawnInternal",
                    new[]
                    {
                        typeof(Vector3), typeof(float), typeof(bool), typeof(Rot4), typeof(Rot4), typeof(RotDrawMode),
                        typeof(bool), typeof(bool)
                    }),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(RenderPawnInternal)), null);


//            harmony.Patch(AccessTools.Method(typeof(PawnGraphicSet), "ResolveAllGraphics"), new HarmonyMethod(
//                typeof(HarmonyPatches),
//                nameof(RenderWerewolf)), null);
//
//            harmony.Patch(AccessTools.Method(typeof(PawnRenderer), "RenderPawnAt", new[] {typeof(Vector3)}),
//                new HarmonyMethod(typeof(HarmonyPatches), nameof(RenderPawnAt)), null);
        }

        // PawnRenderer.RenderPawnInternal
        private static bool RenderPawnInternal(PawnRenderer __instance, Vector3 rootLoc, float angle, bool renderBody,
            Rot4 bodyFacing, Rot4 headFacing,
            RotDrawMode bodyDrawType, bool portrait, bool headStump)
        {
			Pawn value = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
			CompWerewolf compWerewolf;
			bool flag = (compWerewolf = ((value != null) ? value.GetComp<CompWerewolf>() : null)) != null && compWerewolf.IsTransformed;
			bool result;
			if (flag)
			{
				if (compWerewolf.CurrentWerewolfForm.bodyGraphicData == null || __instance.graphics.nakedGraphic == null)
				{
					compWerewolf.CurrentWerewolfForm.bodyGraphicData = compWerewolf.CurrentWerewolfForm.def.graphicData;
					__instance.graphics.nakedGraphic = compWerewolf.CurrentWerewolfForm.bodyGraphicData.Graphic;
				}
				if (renderBody)
				{
					Vector3 vector = rootLoc;
					vector.y += 0.0046875f;
					bool flag3 = bodyDrawType == RotDrawMode.Rotting && !value.RaceProps.Humanlike && __instance.graphics.dessicatedGraphic != null && !portrait;
					if (flag3)
					{
						__instance.graphics.dessicatedGraphic.Draw(vector, bodyFacing, value, 0f);
					}
					else
					{
						Mesh mesh = __instance.graphics.nakedGraphic.MeshAt(bodyFacing);
						List<Material> list = __instance.graphics.MatsBodyBaseAt(bodyFacing, bodyDrawType);
						for (int i = 0; i < list.Count; i++)
						{
							Material damagedMat = __instance.graphics.flasher.GetDamagedMat(list[i]);
							Vector3 vector2 = new Vector3(vector.x, vector.y, vector.z);
							if (portrait)
							{
								vector2.x *= 1f + (1f - (portrait ? compWerewolf.CurrentWerewolfForm.def.CustomPortraitDrawSize : compWerewolf.CurrentWerewolfForm.bodyGraphicData.drawSize).x);
								vector2.z *= 1f + (1f - (portrait ? compWerewolf.CurrentWerewolfForm.def.CustomPortraitDrawSize : compWerewolf.CurrentWerewolfForm.bodyGraphicData.drawSize).y);
							}
							else
							{
							    vector2 = Vector3.zero;
							}
							GenDraw.DrawMeshNowOrLater(mesh, vector + vector2, Quaternion.identity, damagedMat, portrait);
							vector.y += 0.0046875f;
						}
						bool flag4 = bodyDrawType == 0;
						if (flag4)
						{
							Vector3 vector3 = rootLoc;
							vector3.y += 0.01875f;
							Traverse.Create(__instance).Field("woundOverlays").GetValue<PawnWoundDrawer>().RenderOverBody(vector3, mesh, Quaternion.identity, portrait);
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
            if (pawn.Dead && pawn.Corpse != null)
            {
                return pawn.Corpse.CurRotDrawMode;
            }

            return RotDrawMode.Fresh;
        }

        public static bool RenderPawnAt(PawnRenderer __instance, Vector3 drawLoc)
        {
            Pawn p = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (p?.GetComp<CompWerewolf>() is CompWerewolf compWerewolf && compWerewolf.IsTransformed)
            {
                var loc = new Vector3(drawLoc.x, drawLoc.y, drawLoc.z);
                Quaternion quaternion = Quaternion.AngleAxis(0f, Vector3.up);
                Mesh mesh = compWerewolf.CurrentWerewolfForm.bodyGraphicData.GraphicColoredFor(p).MeshAt(p.Rotation);
                List<Material> list = __instance.graphics.MatsBodyBaseAt(p.Rotation, CurRotDrawMode(p));
                for (int i = 0; i < list.Count; i++)
                {
                    Material damagedMat = __instance.graphics.flasher.GetDamagedMat(list[i]);
                    GenDraw.DrawMeshNowOrLater(mesh, drawLoc, quaternion, damagedMat, false);
                    loc.y += 0.00390625f;
                }

                if (CurRotDrawMode(p) == RotDrawMode.Fresh)
                {
                    loc.y += 0.01953125f;
                    Traverse.Create(__instance).Field("woundOverlays").GetValue<PawnWoundDrawer>()
                        .RenderOverBody(drawLoc, mesh, quaternion, false);
                }

                return false;
            }

            return true;
        }

        public static Color WerewolfColor(Pawn p, WerewolfForm w)
        {
            var hairColor = new Color(p.story.hairColor.r, p.story.hairColor.g, p.story.hairColor.b);
            if (w.def != WWDefOf.ROM_Glabro)
            {
                return hairColor;
            }

            return Color.white;
        }

        public static bool RenderWerewolf(PawnGraphicSet __instance)
        {
            Pawn p = __instance.pawn;
            if (p?.GetComp<CompWerewolf>() is CompWerewolf compWerewolf && compWerewolf.IsTransformed)
            {
                __instance.ClearCache();
                if (compWerewolf.CurrentWerewolfForm.bodyGraphicData == null ||
                    __instance.nakedGraphic == null)
                {
                    compWerewolf.CurrentWerewolfForm.bodyGraphicData = compWerewolf.CurrentWerewolfForm.def.graphicData;
                    __instance.nakedGraphic = GraphicDatabase.Get<Graphic_Multi>(
                        path: compWerewolf.CurrentWerewolfForm.bodyGraphicData.texPath, shader: ShaderDatabase.Cutout,
                        drawSize: compWerewolf.CurrentWerewolfForm.bodyGraphicData.drawSize,
                        color: WerewolfColor(p, compWerewolf.CurrentWerewolfForm));
                    __instance.headGraphic = null;
                    __instance.hairGraphic = null;
                }

                return false;
            }

            return true;
        }

        // RimWorld.Scenario
        public static void AddRecentWerewolves(Scenario __instance, Pawn pawn, PawnGenerationContext context)
        {
            if (pawn.IsWerewolf())
            {
                var recentWerewolves = Find.World.GetComponent<WorldComponent_MoonCycle>().recentWerewolves;
                recentWerewolves?.Add(pawn, 1);
            }
        }

        public static bool ShouldModifyDamage(Pawn instigator)
        {
            if (!instigator?.TryGetComp<CompWerewolf>()?.IsTransformed ?? false)
                return true;
            return false;
        }

        //public class DamageWorker_AddInjury : DamageWorker
        public static void WerewolfDmgFixFinalizeAndAddInjury(DamageWorker_AddInjury __instance, Pawn pawn,
            ref Hediff_Injury injury, ref DamageInfo dinfo, ref DamageWorker.DamageResult result)
        {
            if (dinfo.Amount > 0 && pawn.TryGetComp<CompWerewolf>() is CompWerewolf ww && ww.IsWerewolf &&
                ww.CurrentWerewolfForm != null)
            {
                if (dinfo.Instigator is Pawn a && ShouldModifyDamage(a))
                {
                    if (a?.equipment?.Primary is ThingWithComps b && !b.IsSilverTreated())
                    {
                        int math = (int) (dinfo.Amount) -
                                   (int) (dinfo.Amount *
                                          (ww.CurrentWerewolfForm.DmgImmunity)); //10% damage. Decimated damage.
                        dinfo.SetAmount(math);
                        injury.Severity = math;
                        //Log.Message(dinfo.Amount.ToString());
                    }
                }
            }
        }

        // RimWorld.JobGiver_OptimizeApparel
        public static bool DontOptimizeWerewolfApparel(JobGiver_OptimizeApparel __instance, ref Job __result, Pawn pawn)
        {
            if (pawn?.GetComp<CompWerewolf>() is CompWerewolf ww && ww.IsTransformed)
            {
                __result = null;
                return false;
            }

            return true;
        }

        // Verse.HealthUtility
        public static void DebugDownWerewolf(Pawn p)
        {
            if (p?.GetComp<CompWerewolf>() is CompWerewolf w && w.IsWerewolf && w.IsTransformed)
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
                    Pawn pawn = Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).Where((Thing t) => t is Pawn)
                        .Cast<Pawn>().FirstOrDefault<Pawn>();
                    if (pawn != null)
                    {
                        if (!pawn.IsWerewolf())
                        {
                            pawn.story.traits.GainTrait(new Trait(WWDefOf.ROM_Werewolf, 0));
                            //pawn.health.AddHediff(VampDefOf.ROM_Vampirism, null, null);
                            pawn.Drawer.Notify_DebugAffected();
                            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, pawn.LabelShort + " is now a werewolf", -1f);
                        }
                        else
                            Messages.Message(pawn.LabelCap + " is already a werewolf.", MessageTypeDefOf.RejectInput);
                    }
                })
            });

            AccessTools.Method(typeof(Dialog_DebugActionsMenu), "DebugToolMap").Invoke(__instance, new object[]
            {
                "Give Lycanthropy (Metis Chance)", new Action(() =>
                {
                    Pawn pawn = Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).Where((Thing t) => t is Pawn)
                        .Cast<Pawn>().FirstOrDefault<Pawn>();
                    if (pawn != null)
                    {
                        if (!pawn.IsWerewolf())
                        {
                            pawn.story.traits.GainTrait(new Trait(WWDefOf.ROM_Werewolf, -1));
                            //pawn.health.AddHediff(VampDefOf.ROM_Vampirism, null, null);
                            pawn.Drawer.Notify_DebugAffected();
                            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, pawn.LabelShort + " is now a werewolf", -1f);
                        }
                        else
                            Messages.Message(pawn.LabelCap + " is already a werewolf.", MessageTypeDefOf.RejectInput);
                    }
                })
            });

            AccessTools.Method(typeof(Dialog_DebugActionsMenu), "DebugToolMap").Invoke(__instance, new object[]
            {
                "Remove Lycanthropy", new Action(() =>
                {
                    Pawn pawn = Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).Where((Thing t) => t is Pawn)
                        .Cast<Pawn>().FirstOrDefault<Pawn>();
                    if (pawn != null)
                    {
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
                            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, pawn.LabelShort + " is no longer a werewolf",
                                -1f);
                        }
                        else
                            Messages.Message(pawn.LabelCap + " is not a werewolf.", MessageTypeDefOf.RejectInput);
                    }
                })
            });

            AccessTools.Method(typeof(Dialog_DebugActionsMenu), "DebugAction").Invoke(__instance, new object[]
            {
                "Regenerate Moons",
                new Action(() =>
                {
                    Find.World.GetComponent<WorldComponent_MoonCycle>().DebugRegenerateMoons(Find.World);
                })
            });

            AccessTools.Method(typeof(Dialog_DebugActionsMenu), "DebugAction").Invoke(__instance, new object[]
            {
                "Next Full Moon",
                new Action(() => { Find.World.GetComponent<WorldComponent_MoonCycle>().DebugTriggerNextFullMoon(); })
            });
        }

        // Verse.TickManager
        public static void MoonTicksUpdate(TickManager __instance, int newTicksGame)
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
            if (__instance?.GetComp<CompWerewolf>() is CompWerewolf w && w.IsWerewolf && w.IsTransformed)
            {
                w.TransformBack(true);
            }
        }


        // RimWorld.PawnUtility
        public static void UnrecruitableSworn(ref float __result, Pawn pawn, Faction recruiterFaction)
        {
            if (pawn?.story?.traits?.allTraits?.FirstOrDefault(x => x.def == WWDefOf.ROM_Werewolf && x.Degree == 2) !=
                null)
            {
                __result = 0.99f;
            }
        }


        // Verse.Pawn
        public static void WerewolfKill(Pawn __instance, DamageInfo? dinfo)
        {
            if (__instance?.GetComp<CompWerewolf>() is CompWerewolf w && w.IsTransformed && !w.IsReverting)
            {
                w.TransformBack(true);
            }
        }

        // RimWorld.LordToil_AssaultColony
        public static void UpdateAllDuties_PostFix(LordToil_AssaultColony __instance)
        {
            for (int i = 0; i < __instance.lord.ownedPawns.Count; i++)
            {
                if (__instance.lord.ownedPawns[i] is Pawn p && p.GetComp<CompWerewolf>() is CompWerewolf w &&
                    w.IsWerewolf)
                    p.mindState.duty = new PawnDuty(DefDatabase<DutyDef>.GetNamed("ROM_WerewolfAssault"));
            }
        }

        // Verse.AI.Pawn_PathFollower
        public static void PathOfNature(Pawn_PathFollower __instance, Pawn pawn, IntVec3 c, ref int __result)
        {
            if (pawn?.GetComp<CompWerewolf>() is CompWerewolf compWerewolf &&
                compWerewolf?.CurrentWerewolfForm?.def == WWDefOf.ROM_Lupus)
            {
                int num;
                if (c.x == pawn.Position.x || c.z == pawn.Position.z)
                {
                    num = pawn.TicksPerMoveCardinal;
                }
                else
                {
                    num = pawn.TicksPerMoveDiagonal;
                }

                //num += pawn.Map.pathGrid.CalculatedCostAt(c, false, pawn.Position);
                Building edifice = c.GetEdifice(pawn.Map);
                if (edifice != null)
                {
                    num += (int) edifice.PathWalkCostFor(pawn);
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
                            num = Mathf.RoundToInt((float) num * 0.75f);
                            break;
                    }
                }

                __result = Mathf.Max(num, 1);
            }
        }


        // Verse.ThingWithComps
        public static void InitializeWWComps(ThingWithComps __instance)
        {
            if (__instance.def.IsRangedWeapon)
            {
                var comps = (List<ThingComp>) AccessTools.Field(typeof(ThingWithComps), "comps").GetValue(__instance);
                ThingComp thingComp = (ThingComp) Activator.CreateInstance(typeof(CompSilverTreated));
                thingComp.parent = __instance;
                comps.Add(thingComp);
                thingComp.Initialize(null);
            }
        }


        public static void OrderForSilverTreatment(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            IntVec3 c = IntVec3.FromVector3(clickPos);
            foreach (Thing current in c.GetThingList(pawn.Map))
            {
                if (current is ThingWithComps target && pawn != null && pawn != target)
                {
                    if ((target?.def?.IsWeapon ?? false))
                    {
                        if (pawn?.Map?.listerBuildings
                            ?.AllBuildingsColonistOfDef(DefDatabase<ThingDef>.GetNamed("TableMachining"))
                            ?.FirstOrDefault(x => x is Building_WorkTable) is Building_WorkTable machiningTable)
                        {
                            if (target.IsSilverTreated())
                            {
                                //Do nothing
                            }
                            else if (!pawn.CanReach(target, PathEndMode.OnCell, Danger.Deadly, false,
                                TraverseMode.ByPawn))
                            {
                                opts.Add(new FloatMenuOption(
                                    "ROM_CannotApplySilverTreatment".Translate() + " (" + "NoPath".Translate() + ")",
                                    null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            }
                            else if (!pawn.CanReserve(target, 1))
                            {
                                opts.Add(new FloatMenuOption(
                                    "ROM_CannotApplySilverTreatment".Translate() + ": " + "Reserved".Translate(), null,
                                    MenuOptionPriority.Default, null, null, 0f, null, null));
                            }
                            else if (!pawn.CanReach(machiningTable, PathEndMode.OnCell, Danger.Deadly, false,
                                TraverseMode.ByPawn))
                            {
                                opts.Add(new FloatMenuOption(
                                    "ROM_CannotApplySilverTreatment".Translate() + " (" +
                                    "ROM_NoPathToMachiningTable".Translate() + ")", null, MenuOptionPriority.Default,
                                    null, null, 0f, null, null));
                            }
                            else if (!pawn.CanReserve(machiningTable, 1))
                            {
                                opts.Add(new FloatMenuOption(
                                    "ROM_CannotApplySilverTreatment".Translate() + ": " +
                                    "ROM_MachiningTableReserved".Translate(), null, MenuOptionPriority.Default, null,
                                    null, 0f, null, null));
                            }
                            else if (pawn.Map.resourceCounter.Silver < SilverTreatedUtility.AmountRequired(target))
                            {
                                opts.Add(new FloatMenuOption(
                                    "ROM_CannotApplySilverTreatment".Translate() + ": " +
                                    "ROM_NeedsSilver".Translate(SilverTreatedUtility.AmountRequired(target)), null,
                                    MenuOptionPriority.Default, null, null, 0f, null, null));
                            }
                            else
                            {
                                Action action = delegate
                                {
                                    Job job = new Job(WWDefOf.ROM_ApplySilverTreatment, target,
                                        SilverTreatedUtility.FindSilver(pawn), machiningTable);
                                    job.count = SilverTreatedUtility.AmountRequired(target);
                                    pawn.jobs.TryTakeOrderedJob(job);
                                };
                                opts.Add(new FloatMenuOption("ROM_ApplySilverTreatment".Translate(new object[]
                                {
                                    target.LabelCap,
                                    SilverTreatedUtility.AmountRequired(target)
                                }), action, MenuOptionPriority.High, null, target, 0f, null, null));
                            }
                        }
                    }
                }
            }
        }

        // RimWorld.Verb_MeleeAttack
        public static void SoundMiss_Prefix(ref SoundDef __result, Verb_MeleeAttack __instance)
        {
            if (__instance.caster is Pawn pawn && pawn.GetComp<CompWerewolf>() is CompWerewolf w && w.IsTransformed)
            {
                if (w.CurrentWerewolfForm.def.attackSound is SoundDef soundToPlay)
                {
                    if (Rand.Value < 0.5f)
                        soundToPlay.PlayOneShot(new TargetInfo(pawn));
                }
            }
        }


        public static void SoundHitPawnPrefix(ref SoundDef __result, Verb_MeleeAttack __instance)
        {
            if (__instance.caster is Pawn pawn && pawn.GetComp<CompWerewolf>() is CompWerewolf w && w.IsTransformed)
            {
                if (w.CurrentWerewolfForm.def.attackSound is SoundDef soundToPlay)
                {
                    if (Rand.Value < 0.5f)
                        soundToPlay.PlayOneShot(new TargetInfo(pawn));
                }
            }
        }

        // RimWorld.Building_Door
        public static void WerewolfCantOpen(Pawn p, ref bool __result)
        {
            __result = __result && (p?.mindState?.mentalStateHandler?.CurState?.def != WWDefOf.ROM_WerewolfFury);
        }

        // Verse.Pawn
        public static void WerewolfBodySize(Pawn __instance, ref float __result)
        {
            if (__instance?.GetComp<CompWerewolf>() is CompWerewolf w && w.IsWerewolf && w.IsTransformed)
            {
                __result = w.CurrentWerewolfForm
                    .FormBodySize; //Mathf.Clamp((__result * w.CurrentWerewolfForm.def.sizeFactor) + (w.CurrentWerewolfForm.level * 0.1f), __result, __result * (w.CurrentWerewolfForm.def.sizeFactor * 2));
            }
        }

        // Verse.Pawn
        public static void WerewolfHealthScale(Pawn __instance, ref float __result)
        {
            if (__instance?.GetComp<CompWerewolf>() is CompWerewolf w && w.IsWerewolf && w.IsTransformed)
            {
                __result = w.CurrentWerewolfForm
                    .FormHealthScale; //Mathf.Clamp((__result * w.CurrentWerewolfForm.def.healthFactor) + (w.CurrentWerewolfForm.level * 0.1f), __result, __result * (w.CurrentWerewolfForm.def.healthFactor * 2));
            }
        }
    }
}