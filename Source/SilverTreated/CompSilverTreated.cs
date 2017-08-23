using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Werewolf
{
    public class CompSilverTreated : ThingComp
    {
        public bool treated = false;

        public override string TransformLabel(string label)
        {
            if (treated)
            {
                return label + "ROM_SilverAmmo".Translate();
            }
            return base.TransformLabel(label);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref this.treated, "treated", false);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (!treated && this?.parent?.Stuff == ThingDefOf.Silver) treated = true;
            }
        }

    }
}
