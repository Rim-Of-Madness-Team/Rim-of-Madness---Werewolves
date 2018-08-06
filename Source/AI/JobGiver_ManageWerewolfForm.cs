using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace Werewolf
{
    public class JobGiver_AttackAndTransform : JobGiver_AIFightEnemy
    {
        protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest)
        {
            bool allowManualCastWeapons = !pawn.IsColonist;
            Verb verb = pawn.TryGetAttackVerb(null);
            if (verb == null)
            {
                dest = IntVec3.Invalid;
                return false;
            }
            return CastPositionFinder.TryFindCastPosition(new CastPositionRequest
            {
                caster = pawn,
                target = pawn.mindState.enemyTarget,
                verb = verb,
                maxRangeFromTarget = verb.verbProps.range,
                wantCoverFromTarget = (verb.verbProps.range > 5f)
            }, out dest);
        }


        protected override Job TryGiveJob(Pawn pawn)
        {
            this.UpdateEnemyTarget(pawn);
            Thing enemyTarget = pawn.mindState.enemyTarget;
            if (enemyTarget == null)
            {
                return null;
            }
            bool allowManualCastWeapons = !pawn.IsColonist;
            Verb verb = pawn.TryGetAttackVerb(null);
            if (verb == null)
            {
                return null;
            }

            if (pawn.GetComp<CompWerewolf>() is CompWerewolf w && w.IsWerewolf)
            {
                if (!w.IsTransformed && w.IsBlooded) w.TransformInto(w.HighestLevelForm, false);
            }

            if (verb.verbProps.IsMeleeAttack)
            {
                return this.MeleeAttackJob(enemyTarget);
            }
            bool flag = CoverUtility.CalculateOverallBlockChance(pawn.Position, enemyTarget.Position, pawn.Map) > 0.01f;
            bool flag2 = pawn.Position.Standable(pawn.Map);
            bool flag3 = verb.CanHitTarget(enemyTarget);
            bool flag4 = (pawn.Position - enemyTarget.Position).LengthHorizontalSquared < 25;
            if ((flag && flag2 && flag3) || (flag4 && flag3))
            {
                return new Job(JobDefOf.Wait_Combat, JobGiver_AIFightEnemy.ExpiryInterval_ShooterSucceeded.RandomInRange, true);
            }
            IntVec3 intVec;
            if (!this.TryFindShootingPosition(pawn, out intVec))
            {
                return null;
            }
            if (intVec == pawn.Position)
            {
                return new Job(JobDefOf.Wait_Combat, JobGiver_AIFightEnemy.ExpiryInterval_ShooterSucceeded.RandomInRange, true);
            }
            Job newJob = new Job(JobDefOf.Goto, intVec)
            {
                expiryInterval = JobGiver_AIFightEnemy.ExpiryInterval_ShooterSucceeded.RandomInRange,
                checkOverrideOnExpire = true
            };
            pawn.Map.pawnDestinationReservationManager.Reserve(pawn, newJob, intVec);
            return newJob;


        }

    }
}
