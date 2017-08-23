using System;
using UnityEngine;
using Verse;

namespace Werewolf
{
    [StaticConstructorOnStartup]
    internal class Gizmo_HediffRageStatus : Gizmo
    {
        public HediffComp_Rage rage;

        private static readonly Texture2D FullRageBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.8f, 0.1f, 0.1f));

        private static readonly Texture2D EmptyRageBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

        public override float Width
        {
            get
            {
                return 140f;
            }
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft)
        {
            Rect overRect = new Rect(topLeft.x, topLeft.y, this.Width, 75f);
            Find.WindowStack.ImmediateWindow(984688, overRect, WindowLayer.GameUI, delegate
            {
                Rect rect = overRect.AtZero().ContractedBy(6f);
                Rect rect2 = rect;
                rect2.height = overRect.height / 2f;
                Text.Font = GameFont.Tiny;
                Widgets.Label(rect2, "ROM_RageLeft".Translate());
                Rect rect3 = rect;
                rect3.yMin = overRect.height / 2f;
                float fillPercent = rage.RageRemaining / Mathf.Max(1f, rage.BaseRageDuration());
                Widgets.FillableBar(rect3, fillPercent, Gizmo_HediffRageStatus.FullRageBarTex, Gizmo_HediffRageStatus.EmptyRageBarTex, false);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect3, (rage.RageRemaining).ToString("F0") + " / " + (rage.BaseRageDuration()).ToString("F0"));
                Text.Anchor = TextAnchor.UpperLeft;
            }, true, false, 1f);
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
