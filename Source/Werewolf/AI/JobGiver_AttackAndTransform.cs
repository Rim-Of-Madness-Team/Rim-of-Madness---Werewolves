﻿using RimWorld;
using Verse;
using Verse.AI;

namespace Werewolf
{
    public class JobGiver_AttackAndTransform : JobGiver_AIFightEnemy
    {
        protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest)
        {
            _ = !pawn.IsColonist;
            var verb = pawn.TryGetAttackVerb(null);
            if (verb != null)
            {
                return CastPositionFinder.TryFindCastPosition(new CastPositionRequest
                {
                    caster = pawn,
                    target = pawn.mindState.enemyTarget,
                    verb = verb,
                    maxRangeFromTarget = verb.verbProps.range,
                    wantCoverFromTarget = verb.verbProps.range > 5f
                }, out dest);
            }

            dest = IntVec3.Invalid;
            return false;
        }


        protected override Job TryGiveJob(Pawn pawn)
        {
            UpdateEnemyTarget(pawn);
            var enemyTarget = pawn.mindState.enemyTarget;
            if (enemyTarget == null)
            {
                return null;
            }

            _ = !pawn.IsColonist;
            var verb = pawn.TryGetAttackVerb(null);
            if (verb == null)
            {
                return null;
            }

            if (pawn.GetComp<CompWerewolf>() is {IsWerewolf: true} w)
            {
                if (!w.IsTransformed && w.IsBlooded)
                {
                    w.TransformInto(w.HighestLevelForm);
                }
            }

            if (verb.verbProps.IsMeleeAttack)
            {
                return MeleeAttackJob(enemyTarget);
            }

            if (CoverUtility.CalculateOverallBlockChance(pawn.Position, enemyTarget.Position, pawn.Map) > 0.01f &&
                pawn.Position.Standable(pawn.Map) && verb.CanHitTarget(enemyTarget) ||
                (pawn.Position - enemyTarget.Position).LengthHorizontalSquared < 25 && verb.CanHitTarget(enemyTarget))
            {
                return new Job(JobDefOf.Wait_Combat, ExpiryInterval_ShooterSucceeded.RandomInRange, true);
            }

            if (!TryFindShootingPosition(pawn, out var intVec))
            {
                return null;
            }

            if (intVec == pawn.Position)
            {
                return new Job(JobDefOf.Wait_Combat, ExpiryInterval_ShooterSucceeded.RandomInRange, true);
            }

            var newJob = new Job(JobDefOf.Goto, intVec)
            {
                expiryInterval = ExpiryInterval_ShooterSucceeded.RandomInRange,
                checkOverrideOnExpire = true
            };
            pawn.Map.pawnDestinationReservationManager.Reserve(pawn, newJob, intVec);
            return newJob;
        }
    }
}