using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Werewolf
{
    public class HediffCompProperties_Rage : HediffCompProperties
    {
        public float baseRageSeconds;

        public HediffCompProperties_Rage()
        {
            this.compClass = typeof(HediffComp_Rage);
        }
    }
}
