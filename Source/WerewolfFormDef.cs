using Verse;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;

namespace Werewolf
{
    public class WerewolfFormDef : Def
    {
        public GraphicData graphicData = null;
        public HediffDef formHediff;
        public HediffDef jawHediff;
        public HediffDef clawHediff;
        public float rageUsageFactor = 1.0f;
        public float sizeFactor = 1.0f;
        public float healthFactor = 1.0f;
        public float rageFactorPerLevel = 0.5f;
        public float rageFactorPerLevelMax = 60.0f;
        public Vector2 CustomPortraitDrawSize = Vector2.one;
        public SoundDef transformSound = null;
        public SoundDef attackSound = null;
        public string iconTexPath = null;

        public Texture2D Icon
        {
            get
            {
                return ContentFinder<Texture2D>.Get(iconTexPath, true);
            }
        }

        // Verse.ThingDef
        public override void ResolveReferences()
        {
            if (this.graphicData != null)
            {
                this.graphicData.ResolveReferencesSpecial();
            }
        }

    }
}