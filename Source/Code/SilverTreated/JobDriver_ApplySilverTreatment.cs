using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Werewolf
{
    public class JobDriver_ApplySilverTreatment : JobDriver
    {
        private float totalNeededWork;
        private float workLeft;

        protected Thing Target => job.targetA.Thing;

        protected Building Building => (Building) Target.GetInnerIfMinified();

        protected int TotalNeededWork
        {
            get
            {
                var building = Building;
                var value = Mathf.RoundToInt(building.GetStatValue(StatDefOf.WorkToBuild));
                return Mathf.Clamp(value, 20, 3000);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workLeft, "workLeft");
            Scribe_Values.Look(ref totalNeededWork, "totalNeededWork");
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job) && pawn.Reserve(job.targetB, job) &&
                   pawn.Reserve(job.targetC, job);
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            var weapon = TargetIndex.A;
            var silver = TargetIndex.B;
            var machineTable = TargetIndex.C;

            var silverThings = new List<Thing>();

            //Unforbid
            yield return new Toil
            {
                initAction = delegate
                {
                    if (TargetA.Thing is { } t && t.IsForbidden(Faction.OfPlayer))
                    {
                        t.SetForbidden(false);
                    }
                }
            };
            yield return Toils_Reserve.Reserve(weapon);
            var tempToil = Toils_Goto.GotoThing(weapon, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(weapon)
                .FailOnSomeonePhysicallyInteracting(weapon);
            yield return tempToil;
            yield return Toils_Haul.StartCarryThing(weapon);
            yield return Toils_Haul.CarryHauledThingToCell(machineTable);
            yield return Toils_Haul.PlaceHauledThingInCell(machineTable, tempToil, false);
            this.FailOnForbidden(silver);
            yield return Toils_Reserve.Reserve(silver);
            var reserveSilver = Toils_Reserve.Reserve(silver);
            yield return reserveSilver;
            yield return Toils_Goto.GotoThing(silver, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(silver)
                .FailOnSomeonePhysicallyInteracting(silver);
            yield return Toils_Haul.StartCarryThing(silver, false, true).FailOnDestroyedNullOrForbidden(silver);
            yield return new Toil
            {
                initAction = delegate { silverThings.Add(TargetB.Thing); },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveSilver, silver, TargetIndex.None, true);
            yield return Toils_Goto.GotoThing(machineTable, PathEndMode.InteractionCell);
            yield return Toils_General.Wait(240).FailOnDestroyedNullOrForbidden(weapon)
                .FailOnCannotTouch(weapon, PathEndMode.ClosestTouch).FailOnDestroyedNullOrForbidden(machineTable)
                .FailOnDestroyedNullOrForbidden(silver).WithProgressBarToilDelay(silver);
            yield return new Toil
            {
                initAction = delegate
                {
                    Log.Message("Finished");
                    //this.FinishedRemoving();
                    //this.Map.designationManager.RemoveAllDesignationsOn(this.Target, false);
                    SilverTreatedUtility.ApplySilverTreatment(TargetA.Thing as ThingWithComps, silverThings);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        protected virtual void TickAction()
        {
        }
    }
}