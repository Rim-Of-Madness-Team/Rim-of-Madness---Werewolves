using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace Werewolf
{
    public class WerewolfForm : IExposable, ILoadReferenceable
    {
        public WerewolfFormDef def;
        public int level = 0;
        public GraphicData bodyGraphicData;
        private Pawn owner;

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

        public void ResolveReferences()
        {
            if (this.bodyGraphicData != null && this.bodyGraphicData.graphicClass == null)
            {
                this.bodyGraphicData.graphicClass = typeof(Graphic_Multi);
            }
        }

        public void LevelUp()
        {
            ++this.level;
            formBodySize = null;
            formHealthScale = null;
            dmgImmunity = null;
        }

        public string Desc
        {
            get
            {
                StringBuilder s = new StringBuilder();
                string tab = "\t";

                s.AppendLine("ROM_FormLevel".Translate(new object[] { def.LabelCap, level }));
                s.AppendLine(def.description);
                s.AppendLine();
                s.AppendLine("ROM_FormNextLevel".Translate(tab));
                s.AppendLine("ROM_FormHealth".Translate(new object[] {
                (FormHealthScale * 100).ToString("00.##"),
                tab,
                (FormHealthScaleNext * 100).ToString("00.##")
                }));
                s.AppendLine("ROM_FormSize".Translate(new object[] {
                (FormBodySize * 100).ToString("00.##"),
                tab,
                (FormBodySizeNext * 100).ToString("00.##")
                }));
                s.AppendLine("ROM_FormImmunity".Translate(new object[] {
                (DmgImmunity * 100).ToString("00.##"),
                tab,
                (DmgImmunityNext * 100).ToString("00.##")
                }));
                s.AppendLine("ROM_FormNote".Translate());
                s.AppendLine();
                return s.ToString().TrimEndNewlines();
            }
        }

        private float? dmgImmunity;
        public float DmgImmunityNext => Mathf.Clamp(((0.6f) + ((level + 1) * 0.05f)), 0.6f, 0.90f);
        public float DmgImmunity
        {
            get
            {
                if (!dmgImmunity.HasValue)
                {
                    dmgImmunity = Mathf.Clamp(((0.6f) + (level * 0.05f)), 0.6f, 0.90f);
                }
                return dmgImmunity.Value;
            }
            set
            {
                dmgImmunity = value;
            }
        }


        private float? formBodySize;
        public float FormBodySizeNext => Mathf.Clamp((owner.def.race.baseBodySize * def.sizeFactor) + ((level + 1) * 0.1f), owner.def.race.baseBodySize, owner.def.race.baseBodySize * (def.sizeFactor * 2));
        public float FormBodySize
        {
            get
            {
                if (!formBodySize.HasValue)
                {
                    float oSize = owner.def.race.baseBodySize;
                    formBodySize = Mathf.Clamp((oSize * def.sizeFactor) + (level * 0.1f), oSize, oSize * (def.sizeFactor * 2));
                }
                return formBodySize.Value;
            }
            set
            {
                formBodySize = value;
            }
        }

        private float? formHealthScale;
        public float FormHealthScaleNext => Mathf.Clamp((owner.def.race.baseHealthScale * def.healthFactor) + ((level + 1) * 0.1f), owner.def.race.baseHealthScale, owner.def.race.baseHealthScale * (def.healthFactor * 2));
        public float FormHealthScale
        {
            get
            {
                if (!formHealthScale.HasValue)
                {
                    float oScale = owner.def.race.baseHealthScale;
                    formHealthScale = Mathf.Clamp((oScale * def.healthFactor) + (level * 0.1f), oScale, oScale * (def.healthFactor * 2));
                }
                return formHealthScale.Value;
            }
            set
            {
                formBodySize = value;
            }
        }

        private int tempId = 0;

        public void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.tempId, "tempId", 0);
            Scribe_References.Look<Pawn>(ref this.owner, "owner");
            Scribe_Defs.Look<WerewolfFormDef>(ref this.def, "formDef");
            Scribe_Values.Look<int>(ref this.level, "level", 0);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (tempId == 0)
                {
                    tempId = new IntRange(100000, 999999).RandomInRange;
                }
            }
        }

        public string GetUniqueLoadID()
        {
            return "WerewolfForm_" + def.LabelCap + tempId;
        }
    }
}