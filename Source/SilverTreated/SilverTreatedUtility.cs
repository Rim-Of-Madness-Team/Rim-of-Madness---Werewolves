using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;
using System;
using Verse.AI;

namespace Werewolf
{
    public static class SilverTreatedUtility
    {
        public static bool IsSilverTreated(this ThingWithComps thing)
        {
            return (thing?.TryGetComp<CompSilverTreated>() is CompSilverTreated t && t.treated) || 
                ((thing?.def?.IsMeleeWeapon ?? false) && (thing?.Stuff == ThingDefOf.Silver));
        }

        /// Determine how much silver is needed to give a silver treatment.
        public static int AmountRequired(Thing thingToApplyTo)
        {
            float workToMake = thingToApplyTo.GetStatValue(StatDefOf.WorkToMake);
            float massAmount = thingToApplyTo.GetStatValue(StatDefOf.Mass);
            var math = (workToMake * massAmount) / 100;
            return (int)Mathf.Clamp(math, 100f, 1000f);
        }

        public static Thing FindSilver(Pawn pawn)
        {
            Predicate<Thing> predicate = (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x, 1, -1, null, false) && x.def == ThingDefOf.Silver;
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(ThingDefOf.Silver), PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, predicate, null, 0, -1, false, RegionType.Set_Passable, false);
        }

        // RimWorld.TradeUtility
        public static bool ColonyHasEnoughSilver(Map map, int amount)
        {
            return (from t in map.listerThings.ThingsOfDef(ThingDefOf.Silver)
                    select t).Sum((Thing t) => t.stackCount) >= amount;
        }
        
        public static void ApplySilverTreatment(ThingWithComps n, List<Thing> silverToUse)
        {
            if (n?.GetComp<CompSilverTreated>() is CompSilverTreated silverTreatment)
            {
                for (int i = 0; i < silverToUse.Count(); i++)
                {
                    silverToUse[i].Destroy(DestroyMode.Vanish);
                }
                silverTreatment.treated = true;
            }
        }
    }
}
