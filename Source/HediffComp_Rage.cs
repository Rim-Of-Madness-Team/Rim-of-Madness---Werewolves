using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Werewolf
{
    public class HediffComp_Rage : HediffComp
    {
        protected const int SeverityUpdateInterval = 200;

        private HediffCompProperties_Rage Props
        {
            get
            {
                return (HediffCompProperties_Rage)this.props;
            }
        }

        private bool werewolfFlagged = false;
        
        Effecter effecter = null;
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Find.TickManager.TicksGame % 60 == 0)
            {

                //if (!werewolfFlagged && Pawn?.GetComp<CompWerewolf>() is CompWerewolf compWW && compWW.CurrentWerewolfForm != null)
                //{
                //    werewolfFlagged = true;
                //    baseRageDuration = null;
                //    rageRemaining = -999;
                //}

                if (Pawn.Spawned)
                {

                    if (effecter == null)
                    {
                        EffecterDef progressBar = EffecterDefOf.ProgressBar;
                        effecter = progressBar.Spawn();
                    }
                    else
                    {
                        LocalTargetInfo target = Pawn;
                        if (Pawn.Spawned)
                        {
                            effecter.EffectTick(Pawn, TargetInfo.Invalid);
                        }
                        MoteProgressBar mote = ((SubEffecter_ProgressBar)effecter.children[0]).mote;
                        if (mote != null)
                        {
                            float result = 1f - (float)(this.BaseRageDuration() - this.RageRemaining) / (float)this.BaseRageDuration();

                            mote.progress = Mathf.Clamp01(result);
                            mote.offsetZ = -1.0f;
                        }
                    }
                }

                if (RageRemaining < 0 || !Pawn.Spawned || (Pawn.GetComp<CompWerewolf>() is CompWerewolf ww && !ww.IsTransformed))
                {
                    this.effecter.Cleanup();

                    Log.Message("Rage ended");
                    severityAdjustment = -999.99f;
                    if (Pawn?.mindState?.mentalStateHandler?.CurState is MentalState_WerewolfFury fury)
                    {
                        fury.RecoverFromState();
                    }
                    if (Pawn?.GetComp<CompWerewolf>() is CompWerewolf compWerewolf)
                    {

                        if (compWerewolf.CurrentWerewolfForm != null) compWerewolf.TransformBack();

                    }
                    return;
                }
                RageRemaining--;
            }
        }

        private int rageRemaining = -999;
        public int RageRemaining
        {
            get
            {
                if (rageRemaining == -999)
                {
                    rageRemaining = (int)this.BaseRageDuration();
                }
                return rageRemaining;
            }
            set
            {
                rageRemaining = value;
            }
        }

        private float? baseRageDuration;
        public virtual float BaseRageDuration()
        {
            if (!baseRageDuration.HasValue)
            {
                float math = 0;
                float bonusFactor = 0.05f;
                if (Pawn?.GetComp<CompWerewolf>() is CompWerewolf compWerewolf && compWerewolf.CurrentWerewolfForm != null)
                {
                    math = bonusFactor * compWerewolf.CurrentWerewolfForm.level;
                    math *= this.Props.baseRageSeconds;
                    math = Mathf.Clamp(math, 0f, 60.0f);
                }
                baseRageDuration = this.Props.baseRageSeconds + math;
            }
            return baseRageDuration.Value;
        }

        public override string CompDebugString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.CompDebugString());
            if (!base.Pawn.Dead)
            {
                stringBuilder.AppendLine(this.RageRemaining.ToString("F3") + " second(s) remaining");
            }
            return stringBuilder.ToString();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.rageRemaining, "rageRemaining", 0);
        }
    }
}
