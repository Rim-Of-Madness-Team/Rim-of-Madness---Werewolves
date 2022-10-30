using UnityEngine;
using Verse;

namespace Werewolf
{
    public class WerewolfFormDef : Def
    {
        public SoundDef attackSound = null;
        public HediffDef clawHediff;
        public Vector2 CustomPortraitDrawSize = Vector2.one;
        public HediffDef formHediff;
        public GraphicData graphicData = null;
        public float healthFactor = 1.0f;
        public string iconTexPath = null;
        public HediffDef jawHediff;
        public float rageFactorPerLevel = 0.5f;
        public float rageFactorPerLevelMax = 60.0f;
        public float rageUsageFactor = 1.0f;
        public float sizeFactor = 1.0f;
        public SoundDef transformSound = null;

        public Texture2D Icon => ContentFinder<Texture2D>.Get(iconTexPath);

        // Verse.ThingDef
        public override void ResolveReferences()
        {
            if (graphicData != null)
            {
                graphicData.ResolveReferencesSpecial();
            }
        }
    }
}