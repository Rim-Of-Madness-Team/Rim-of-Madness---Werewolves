using AbilityUser;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Werewolf
{
    public class Command_WerewolfButton : Command_Target
    {
        public CompWerewolf compAbilityUser;

        public Command_WerewolfButton(CompWerewolf compAbilityUser)
        {
            this.compAbilityUser = compAbilityUser;
        }

        public override void ProcessInput(Event ev)
        {
            void ActionToInput(LocalTargetInfo x)
            {
                action(x.Thing);
            }

            if (CurActivateSound != null)
            {
                CurActivateSound.PlayOneShotOnCamera();
            }

            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            //Targeter targeter = Find.Targeter;
            //if (base.verb.CasterIsPawn && targeter.targetingVerb != null && targeter.targetingVerb.verbProps == this.verb.verbProps)
            //{
            //    Pawn casterPawn = this.verb.CasterPawn;
            //    if (!targeter.IsPawnTargeting(casterPawn))
            //    {
            //        targeter.targetingVerbAdditionalPawns.Add(casterPawn);
            //    }
            //}
            //else
            //{
            //    Find.Targeter.BeginTargeting(this.verb);
            //    //AccessTools.Field(typeof(Targeter), "action").SetValue(Find.Targeter, new Action<LocalTargetInfo>((LocalTargetInfo x) =>
            //    //this.action(x.Thing)));
            //}
            action(null);
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            var rect = new Rect(topLeft.x, topLeft.y, 75f, 75f);
            var isMouseOver = false;
            if (Mouse.IsOver(rect))
            {
                isMouseOver = true;
                GUI.color = GenUI.MouseoverColor;
            }

            var badTex = icon;
            if (badTex == null)
            {
                badTex = BaseContent.BadTex;
            }

            GUI.DrawTexture(rect, BGTex);
            MouseoverSounds.DoRegion(rect, SoundDefOf.Mouseover_Command);
            GUI.color = defaultIconColor;
            Widgets.DrawTextureFitted(new Rect(rect), badTex, iconDrawScale * 0.85f, iconProportions, iconTexCoords);
            GUI.color = Color.white;
            var isUsed = false;
            //Rect rectFil = new Rect(topLeft.x, topLeft.y, this.Width, this.Width);

            var keyCode = hotKey?.MainKey ?? KeyCode.None;
            if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode))
            {
                var rect2 = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 18f);
                Widgets.Label(rect2, keyCode.ToStringReadable());
                GizmoGridDrawer.drawnHotKeys.Add(keyCode);
                if (hotKey is {KeyDownEvent: true})
                {
                    isUsed = true;
                    Event.current.Use();
                }
            }

            if (Widgets.ButtonInvisible(rect, false))
            {
                isUsed = true;
            }

            var labelCap = LabelCap;
            if (!labelCap.NullOrEmpty())
            {
                var num = Text.CalcHeight(labelCap, rect.width);
                num -= 2f;
                var rect3 = new Rect(rect.x, rect.yMax - num + 12f, rect.width, num);
                GUI.DrawTexture(rect3, TexUI.GrayTextBG);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(rect3, labelCap);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            GUI.color = Color.white;
            if (DoTooltip)
            {
                TipSignal tip = Desc;
                if (disabled && !disabledReason.NullOrEmpty())
                {
                    tip.text = tip.text + "\n" + StringsToTranslate.AU_DISABLED + ": " + disabledReason;
                }

                TooltipHandler.TipRegion(rect, tip);
            }

            if (!HighlightTag.NullOrEmpty() && (Find.WindowStack.FloatMenu == null ||
                                                !Find.WindowStack.FloatMenu.windowRect.Overlaps(rect)))
            {
                UIHighlighter.HighlightOpportunity(rect, HighlightTag);
            }

            float x = compAbilityUser.CooldownTicksLeft;
            float y = compAbilityUser.CooldownMaxTicks;
            var fill = x / y;
            Widgets.FillableBar(rect, fill, AbilityButtons.FullTex, AbilityButtons.EmptyTex, false);
            if (!isUsed)
            {
                return isMouseOver
                    ? new GizmoResult(GizmoState.Mouseover, null)
                    : new GizmoResult(GizmoState.Clear, null);
            }

            if (disabled)
            {
                if (!disabledReason.NullOrEmpty())
                {
                    Messages.Message(disabledReason,
                        MessageTypeDefOf.RejectInput); // MessageTypeDefOf.RejectInput);
                }

                return new GizmoResult(GizmoState.Mouseover, null);
            }

            if (!TutorSystem.AllowAction(TutorTagSelect))
            {
                return new GizmoResult(GizmoState.Mouseover, null);
            }

            var result = new GizmoResult(GizmoState.Interacted, Event.current);
            TutorSystem.Notify_Event(TutorTagSelect);
            return result;
        }

        public void FillableBarBottom(Rect rect, float fillPercent, Texture2D fillTex, Texture2D bgTex, bool doBorder)
        {
            if (doBorder)
            {
                GUI.DrawTexture(rect, BaseContent.BlackTex);
                rect = rect.ContractedBy(3f);
            }

            if (fillTex != null)
            {
                GUI.DrawTexture(rect, fillTex);
            }

            rect.height *= fillPercent;
            GUI.DrawTexture(rect, bgTex);
        }
    }
}