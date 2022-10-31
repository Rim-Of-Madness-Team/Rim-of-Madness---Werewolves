using RimWorld;
using Verse;

namespace Werewolf
{
    public class CompSilverTreated : ThingComp
    {
        public bool treated;

        public override string TransformLabel(string label)
        {
            return treated ? label + "ROM_SilverAmmo".Translate() : base.TransformLabel(label);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref treated, "treated");
            if (Scribe.mode != LoadSaveMode.LoadingVars)
            {
                return;
            }

            if (!treated && parent?.Stuff == ThingDefOf.Silver)
            {
                treated = true;
            }
        }
    }
}