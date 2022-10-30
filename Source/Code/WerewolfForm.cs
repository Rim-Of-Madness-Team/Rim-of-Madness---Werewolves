using System.Text;
using UnityEngine;
using Verse;

namespace Werewolf
{
    public class WerewolfForm : IExposable, ILoadReferenceable
    {
        public GraphicData bodyGraphicData;
        public WerewolfFormDef def;

        private float? dmgImmunity;


        private float? formBodySize;

        private float? formHealthScale;
        public int level;
        private Pawn owner;

        private int tempId;

        public WerewolfForm()
        {
        }

        public WerewolfForm(WerewolfFormDef newDef, int newLevel, Pawn newOwner)
        {
            def = newDef;
            level = newLevel;
            owner = newOwner;
            tempId = newOwner.thingIDNumber;
        }

        public string Desc
        {
            get
            {
                var s = new StringBuilder();
                var tab = "\t";

                s.AppendLine("ROM_FormLevel".Translate(def.LabelCap, level));
                s.AppendLine(def.description);
                s.AppendLine();
                s.AppendLine("ROM_FormNextLevel".Translate(tab));
                s.AppendLine("ROM_FormHealth".Translate((FormHealthScale * 100).ToString("00.##"), tab,
                    (FormHealthScaleNext * 100).ToString("00.##")));
                s.AppendLine("ROM_FormSize".Translate((FormBodySize * 100).ToString("00.##"), tab,
                    (FormBodySizeNext * 100).ToString("00.##")));
                s.AppendLine("ROM_FormImmunity".Translate((DmgImmunity * 100).ToString("00.##"), tab,
                    (DmgImmunityNext * 100).ToString("00.##")));
                s.AppendLine("ROM_FormNote".Translate());
                s.AppendLine();
                return s.ToString().TrimEndNewlines();
            }
        }

        public float DmgImmunityNext => Mathf.Clamp(0.6f + ((level + 1) * 0.05f), 0.6f, 0.90f);

        public float DmgImmunity
        {
            get
            {
                if (!dmgImmunity.HasValue)
                {
                    dmgImmunity = Mathf.Clamp(0.6f + (level * 0.05f), 0.6f, 0.90f);
                }

                return dmgImmunity.Value;
            }
            set => dmgImmunity = value;
        }

        public float FormBodySizeNext =>
            Mathf.Clamp((owner.def.race.baseBodySize * def.sizeFactor) + ((level + 1) * 0.1f),
                owner.def.race.baseBodySize, owner.def.race.baseBodySize * (def.sizeFactor * 2));

        public float FormBodySize
        {
            get
            {
                if (formBodySize.HasValue)
                {
                    return formBodySize.Value;
                }

                var oSize = owner.def.race.baseBodySize;
                formBodySize = Mathf.Clamp((oSize * def.sizeFactor) + (level * 0.1f), oSize,
                    oSize * (def.sizeFactor * 2));

                return formBodySize.Value;
            }
            set => formBodySize = value;
        }

        public float FormHealthScaleNext =>
            Mathf.Clamp((owner.def.race.baseHealthScale * def.healthFactor) + ((level + 1) * 0.1f),
                owner.def.race.baseHealthScale, owner.def.race.baseHealthScale * (def.healthFactor * 2));

        public float FormHealthScale
        {
            get
            {
                if (formHealthScale.HasValue)
                {
                    return formHealthScale.Value;
                }

                var oScale = owner.def.race.baseHealthScale;
                formHealthScale = Mathf.Clamp((oScale * def.healthFactor) + (level * 0.1f), oScale,
                    oScale * (def.healthFactor * 2));

                return formHealthScale.Value;
            }
            set => formBodySize = value;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref tempId, "tempId");
            Scribe_References.Look(ref owner, "owner");
            Scribe_Defs.Look(ref def, "formDef");
            Scribe_Values.Look(ref level, "level");
            if (Scribe.mode != LoadSaveMode.LoadingVars)
            {
                return;
            }

            if (tempId == 0)
            {
                tempId = new IntRange(100000, 999999).RandomInRange;
            }
        }

        public string GetUniqueLoadID()
        {
            return "WerewolfForm_" + def.LabelCap + tempId;
        }

        public void ResolveReferences()
        {
            if (bodyGraphicData is {graphicClass: null})
            {
                bodyGraphicData.graphicClass = typeof(Graphic_Multi);
            }
        }

        public void LevelUp()
        {
            ++level;
            formBodySize = null;
            formHealthScale = null;
            dmgImmunity = null;
        }
    }
}