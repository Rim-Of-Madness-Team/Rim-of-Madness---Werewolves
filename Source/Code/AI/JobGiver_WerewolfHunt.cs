using RimWorld;
using Verse;
using Verse.AI;

namespace Werewolf
{
    public class JobGiver_WerewolfHunt : ThinkNode_JobGiver
    {
        private const int MinMeleeChaseTicks = 900;

        private const int MaxMeleeChaseTicks = 1800;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.TryGetAttackVerb(null) == null)
            {
                return null;
            }

            var pawn2 = FindPawnTarget(pawn);
            if (pawn2 != null && pawn.CanReach(pawn2, PathEndMode.Touch, Danger.Deadly))
            {
                return MeleeAttackJob(pawn2);
            }

            var building = FindTurretTarget(pawn);
            if (building != null)
            {
                return MeleeAttackJob(building);
            }

            if (pawn2 != null)
            {
                using var pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, pawn2.Position,
                    TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings));
                if (!pawnPath.Found)
                {
                    return null;
                }

                var thing = pawnPath.FirstBlockingBuilding(out _, pawn);
                if (thing != null)
                {
                    //Job job = DigUtility.PassBlockerJob(pawn, thing, cellBeforeBlocker, true);
                    //if (job != null)
                    //{
                    return MeleeAttackJob(thing);
                    //}
                }

                pawnPath.TryFindLastCellBeforeBlockingDoor(pawn, out var loc);
                var randomCell = CellFinder.RandomRegionNear(loc.GetRegion(pawn.Map), 9, TraverseParms.For(pawn))
                    .RandomCell;
                return randomCell == pawn.Position
                    ? new Job(JobDefOf.Wait, 30)
                    : new Job(JobDefOf.Goto, randomCell);
            }

            Building buildingDoor = FindDoorTarget(pawn);
            return buildingDoor != null ? MeleeAttackJob(buildingDoor) : null;
        }

        private Job MeleeAttackJob(Thing target)
        {
            return new Job(JobDefOf.AttackMelee, target)
            {
                maxNumMeleeAttacks = 1,
                expiryInterval = Rand.Range(MinMeleeChaseTicks, MaxMeleeChaseTicks),
                attackDoorIfTargetLost = true,
                killIncappedTarget = true
            };
        }

        private Pawn FindPawnTarget(Pawn pawn)
        {
            return (Pawn) AttackTargetFinder.BestAttackTarget(pawn, TargetScanFlags.NeedReachable,
                x => x is Pawn {Dead: false} && x.def.race.intelligence >= Intelligence.ToolUser, 0f, 9999f, default,
                3.40282347E+38f, true);
        }

        private Building FindTurretTarget(Pawn pawn)
        {
            return (Building) AttackTargetFinder.BestAttackTarget(pawn,
                TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedLOSToNonPawns | TargetScanFlags.NeedReachable |
                TargetScanFlags.NeedThreat, t => t is Building, 0f, 70f);
        }


        private Building_Door FindDoorTarget(Pawn pawn)
        {
            return (Building_Door) AttackTargetFinder.BestAttackTarget(pawn, TargetScanFlags.NeedReachable,
                t => t is Building_Door, 0f, 70f);
        }
    }
}