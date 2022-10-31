using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Werewolf
{
    public static class SilverTreatedUtility
    {
        public static bool IsSilverTreated(this ThingWithComps thing)
        {
            return thing?.TryGetComp<CompSilverTreated>() is {treated: true} ||
                   (thing?.def?.IsMeleeWeapon ?? false) && thing.Stuff == ThingDefOf.Silver;
        }

        /// Determine how much silver is needed to give a silver treatment.
        public static int AmountRequired(Thing thingToApplyTo)
        {
            var workToMake = thingToApplyTo.GetStatValue(StatDefOf.WorkToMake);
            var massAmount = thingToApplyTo.GetStatValue(StatDefOf.Mass);
            var math = workToMake * massAmount / 100;
            return (int) Mathf.Clamp(math, 100f, 500f);
        }

        public static Thing FindSilver(Pawn pawn)
        {
            bool predicate(Thing x)
            {
                return !x.IsForbidden(pawn) && pawn.CanReserve(x) && x.def == ThingDefOf.Silver;
            }

            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(ThingDefOf.Silver),
                PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, predicate);
        }

        // RimWorld.TradeUtility
        public static bool ColonyHasEnoughSilver(Map map, int amount)
        {
            return (from t in map.listerThings.ThingsOfDef(ThingDefOf.Silver)
                select t).Sum(t => t.stackCount) >= amount;
        }

        public static void ApplySilverTreatment(ThingWithComps thingWithComps, List<Thing> silverToUse)
        {
            if (thingWithComps?.GetComp<CompSilverTreated>() is not { } silverTreatment)
            {
                return;
            }

            foreach (var thing in silverToUse)
            {
                if (thing.DestroyedOrNull())
                {
                    continue;
                }

                thing.Destroy();
            }

            silverTreatment.treated = true;
        }
    }
}