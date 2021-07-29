using System;
using System.Collections.Generic;
using System.Linq;
using JecsTools;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Werewolf
{
    public class Werewolf : FloatMenuPatch
    {
        public override IEnumerable<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>>
            GetFloatMenus()
        {
            var floatMenus = new List<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>>();

            var silverCondition = new _Condition(_ConditionType.IsType, typeof(ThingWithComps));

            List<FloatMenuOption> silverFunc(Vector3 clickPos, Pawn pawn, Thing curThing)
            {
                var target = curThing as ThingWithComps;
                if (!(target?.def?.IsWeapon ?? false))
                {
                    return null;
                }

                if (!(pawn?.Map?.listerBuildings
                    ?.AllBuildingsColonistOfDef(DefDatabase<ThingDef>.GetNamed("TableMachining"))?
                    .FirstOrDefault(x => x is Building_WorkTable) is Building_WorkTable machiningTable))
                {
                    return null;
                }

                if (target.IsSilverTreated())
                {
                    return null;
                }

                if (!target.def.IsRangedWeapon)
                {
                    return null;
                }

                var opts = new List<FloatMenuOption>();
                if (!pawn.CanReach(target, PathEndMode.OnCell, Danger.Deadly))
                {
                    opts.Add(new FloatMenuOption(
                        "ROM_CannotApplySilverTreatment".Translate() + " (" + "NoPath".Translate() + ")", null));
                    return opts;
                }

                if (!pawn.CanReserve(target))
                {
                    opts.Add(new FloatMenuOption(
                        "ROM_CannotApplySilverTreatment".Translate() + ": " + "Reserved".Translate(), null));
                    return opts;
                }

                if (!pawn.CanReach(machiningTable, PathEndMode.OnCell, Danger.Deadly))
                {
                    opts.Add(new FloatMenuOption(
                        "ROM_CannotApplySilverTreatment".Translate() + " (" + "ROM_NoPathToMachiningTable".Translate() +
                        ")", null));
                    return opts;
                }

                if (!pawn.CanReserve(machiningTable))
                {
                    opts.Add(new FloatMenuOption(
                        "ROM_CannotApplySilverTreatment".Translate() + ": " + "ROM_MachiningTableReserved".Translate(),
                        null));
                    return opts;
                }

                if (pawn.Map.resourceCounter.Silver < SilverTreatedUtility.AmountRequired(target))
                {
                    opts.Add(new FloatMenuOption(
                        "ROM_CannotApplySilverTreatment".Translate() + ": " +
                        "ROM_NeedsSilver".Translate(SilverTreatedUtility.AmountRequired(target)), null));
                    return opts;
                }

                void action()
                {
                    var job = new Job(WWDefOf.ROM_ApplySilverTreatment, target, SilverTreatedUtility.FindSilver(pawn),
                        machiningTable)
                    {
                        count = SilverTreatedUtility.AmountRequired(target)
                    };
                    pawn.jobs.ClearQueuedJobs();
                    pawn.jobs.TryTakeOrderedJob(job);
                }

                opts.Add(new FloatMenuOption(
                    "ROM_ApplySilverTreatment".Translate(target.LabelCap, SilverTreatedUtility.AmountRequired(target)),
                    action, MenuOptionPriority.High, null, target));
                return opts;
            }

            var curSec =
                new KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>(silverCondition,
                    silverFunc);
            floatMenus.Add(curSec);
            return floatMenus;
        }
    }
}