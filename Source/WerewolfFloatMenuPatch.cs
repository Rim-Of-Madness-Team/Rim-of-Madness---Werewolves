using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JecsTools;
using Verse;
using UnityEngine;
using Verse.AI;
using RimWorld;

namespace Werewolf
{
    public class Werewolf : FloatMenuPatch
    {
        public override IEnumerable<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>> GetFloatMenus()
        {
            List<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>> floatMenus = new List<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>>();

            _Condition silverCondition = new _Condition(_ConditionType.IsType, typeof(ThingWithComps));
            Func<Vector3, Pawn, Thing, List<FloatMenuOption>> silverFunc = delegate (Vector3 clickPos, Pawn pawn, Thing curThing)
            {
                List<FloatMenuOption> opts = null;
                ThingWithComps target = curThing as ThingWithComps;
                if ((target?.def?.IsWeapon ?? false))
                {
                    if (pawn?.Map?.listerBuildings?.AllBuildingsColonistOfDef(DefDatabase<ThingDef>.GetNamed("TableMachining"))?
                    .FirstOrDefault(x => x is Building_WorkTable) is Building_WorkTable machiningTable)
                    {
                        opts = new List<FloatMenuOption>();

                        if (target.IsSilverTreated())
                        {
                            //Do nothing
                        }
                        else if (!pawn.CanReach(target, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                        {
                            opts.Add(new FloatMenuOption("ROM_CannotApplySilverTreatment".Translate() + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                        }
                        else if (!pawn.CanReserve(target, 1))
                        {
                            opts.Add(new FloatMenuOption("ROM_CannotApplySilverTreatment".Translate() + ": " + "Reserved".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null));
                        }
                        else if (!pawn.CanReach(machiningTable, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                        {
                            opts.Add(new FloatMenuOption("ROM_CannotApplySilverTreatment".Translate() + " (" + "ROM_NoPathToMachiningTable".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                        }
                        else if (!pawn.CanReserve(machiningTable, 1))
                        {
                            opts.Add(new FloatMenuOption("ROM_CannotApplySilverTreatment".Translate() + ": " + "ROM_MachiningTableReserved".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null));
                        }
                        else if (pawn.Map.resourceCounter.Silver < SilverTreatedUtility.AmountRequired(target))
                        {
                            opts.Add(new FloatMenuOption("ROM_CannotApplySilverTreatment".Translate() + ": " + "ROM_NeedsSilver".Translate(SilverTreatedUtility.AmountRequired(target)), null, MenuOptionPriority.Default, null, null, 0f, null, null));
                        }
                        else
                        {
                            Action action = delegate
                            {
                                Job job = new Job(WWDefOf.ROM_ApplySilverTreatment, target, SilverTreatedUtility.FindSilver(pawn), machiningTable);
                                job.count = SilverTreatedUtility.AmountRequired(target);
                                pawn.jobs.TryTakeOrderedJob(job);
                            };
                            opts.Add(new FloatMenuOption("ROM_ApplySilverTreatment".Translate(new object[]
                            {
                                    target.LabelCap,
                                    SilverTreatedUtility.AmountRequired(target)
                            }), action, MenuOptionPriority.High, null, target, 0f, null, null));
                        }
                        return opts;
                    }
                }
                return null;

            };
            KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>> curSec = new KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>(silverCondition, silverFunc);
            floatMenus.Add(curSec);
            return floatMenus;
        }
    }
}