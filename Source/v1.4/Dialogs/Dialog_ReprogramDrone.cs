using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace MechHumanlikes
{
    public class Dialog_ReprogramDrone : Window
    {
        private Pawn pawn;

        private CompReprogrammableDrone programComp;

        private MDR_ProgrammableDroneExtension programExtension;

        private List<DirectiveDef> proposedDirectives;

        private List<WorkTypeDef> legalWorkTypes;

        private List<WorkTypeDef> proposedEnabledWorkTypes = new List<WorkTypeDef>();

        private List<SkillDef> skillDefs = new List<SkillDef>();

        private Pawn_SkillTracker skillTracker;

        private Dictionary<SkillDef, int> inherentSkills;

        private int proposedWorkTypeComplexity;

        private float proposedSkillComplexity;

        private float scrollHeight;

        private Vector2 scrollPosition;

        private const float WidthWithTabs = 1500f;

        private const float OptionTabIn = 30f;

        private float summaryCachedWidth = -1;

        private Dictionary<string, string> summaryCachedText = new Dictionary<string, string>();

        private static readonly CachedTexture backgroundTexture = new CachedTexture("UI/Icons/Settings/DrawPocket");

        private int ProposedComplexity => programComp.Complexity;

        protected override float Margin => 12f;

        private float Height => 900f;

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(WidthWithTabs, Height);
            }
        }

        public Dialog_ReprogramDrone(Pawn pawn)
        {
            closeOnCancel = false;
            forcePause = true;
            absorbInputAroundWindow = true;
            this.pawn = pawn;
            programComp = pawn.GetComp<CompReprogrammableDrone>();
            proposedDirectives = new List<DirectiveDef>();
            proposedDirectives.AddRange(programComp.ActiveDirectives);
            programExtension = pawn.def.GetModExtension<MDR_ProgrammableDroneExtension>();
            CacheLegalWorkTypes();
            proposedWorkTypeComplexity = programComp.GetComplexityFromSource("Work Types");
            inherentSkills = programExtension.inherentSkills;
            proposedSkillComplexity = programComp.GetComplexityFromSource("Skills") - (programComp.BaselineComplexity / 10);
            skillDefs = DefDatabase<SkillDef>.AllDefsListForReading;
            skillTracker = pawn.skills;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            float curX = 0f;
            float curY = 0f;
            TaggedString title = "MDR_ReprogramDrone".Translate(pawn.LabelShortCap);
            float titleWidth = Text.CalcSize(title).x;
            Widgets.Label(curX, ref curY, titleWidth, title);
            Widgets.InfoCardButton(inRect.width - (3 * Margin), 0f, pawn);
            curY += Margin;
            Rect workTypeRect = new Rect(curX, curY, 240, scrollHeight);
            proposedEnabledWorkTypes = programComp.enabledWorkTypes;
            DrawWorkTypes(workTypeRect);
            curX += 240 + Margin;
            Rect skillsRect = new Rect(curX, curY, 240, scrollHeight);
            DrawSkills(skillsRect);
            curX += 240 + Margin;
            Rect directiveRect = new Rect(curX, curY, 450, scrollHeight);
            DrawDirectives(directiveRect);
            curX += 450 + Margin;
            if (Event.current.type == EventType.Layout)
            {
                scrollHeight = 700f;
            }
            Rect cardSection = new Rect(curX, curY, 600f, scrollHeight);
            cardSection.xMin += 2f * Margin;
            cardSection.yMax -= Margin + CloseButSize.y;
            cardSection.yMin += 32f;
            CharacterCardUtility.DrawCharacterCard(cardSection, pawn, null, default, showName: true);
            Rect summaryRect = new Rect(Margin, inRect.height - Text.LineHeight * 4.5f - Margin, inRect.width - CloseButSize.x - (2 * Margin), Text.LineHeight * 4.5f);
            DrawSummary(summaryRect);
            Rect closeButton = new Rect(inRect.width - CloseButSize.x, inRect.height - CloseButSize.y - Margin, CloseButSize.x, CloseButSize.y);
            if (Widgets.ButtonText(closeButton, "OK".Translate()))
            {
                Accept();
            }
        }

        private void DrawWorkTypes(Rect rect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(rect);
            listingStandard.Label("MDR_SelectedWorkTypes".Translate());
            foreach (WorkTypeDef workTypeDef in legalWorkTypes)
            {
                bool active = proposedEnabledWorkTypes.Contains(workTypeDef);
                if (listingStandard.RadioButton(workTypeDef.labelShort.CapitalizeFirst(), active, OptionTabIn, WorkTypeComplexityTooltip(workTypeDef, !active)))
                {
                    if (programExtension.inherentWorkTypes.NotNullAndContains(workTypeDef))
                    {
                        continue;
                    }

                    if (active)
                    {
                        programComp.enabledWorkTypes.Remove(workTypeDef);
                        proposedWorkTypeComplexity -= workTypeDef.GetModExtension<MDR_WorkTypeExtension>()?.ComplexityCostFor(pawn, false) ?? 1;
                        // If the disabled work type def would result in a skill becoming totally disabled, remove all assigned skill points for it to avoid a complexity "leak".
                        if (workTypeDef.relevantSkills != null)
                        {
                            foreach (SkillDef oldEnabledSkillDef in workTypeDef.relevantSkills)
                            {
                                if (oldEnabledSkillDef.neverDisabledBasedOnWorkTypes || programComp.enabledWorkTypes.Any(workType => workType.relevantSkills.NotNullAndContains(oldEnabledSkillDef)))
                                {
                                    continue;
                                }

                                SkillRecord skillRecord = skillTracker.GetSkill(oldEnabledSkillDef);
                                int inherentSkill = Mathf.Max(programExtension.inherentSkills.GetWithFallback(oldEnabledSkillDef, 0), 0);
                                int skillThreshold = MDR_Utils.SkillComplexityThresholdFor(pawn, oldEnabledSkillDef);
                                if (skillRecord.Level > inherentSkill)
                                {
                                    while (skillRecord.Level > inherentSkill)
                                    {
                                        if (skillRecord.Level > skillThreshold)
                                        {
                                            proposedSkillComplexity -= 1;
                                        }
                                        else
                                        {
                                            proposedSkillComplexity -= 0.5f;
                                        }
                                        skillRecord.Level--;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        programComp.enabledWorkTypes.Add(workTypeDef);
                        proposedWorkTypeComplexity += workTypeDef.GetModExtension<MDR_WorkTypeExtension>()?.ComplexityCostFor(pawn, true) ?? 1;
                    }
                    pawn.Notify_DisabledWorkTypesChanged();
                    programComp.UpdateComplexity("Work Types", proposedWorkTypeComplexity);
                }
            }
            listingStandard.End();
        }

        private void DrawSkills(Rect rect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(rect);
            listingStandard.Label("MDR_SelectSkills".Translate());
            foreach (SkillDef skillDef in skillDefs)
            {
                SkillRecord skill = pawn.skills.GetSkill(skillDef);
                // Skip skills that are disabled by work tags that the pawn does not have.
                if (skill.TotallyDisabled)
                {
                    continue;
                }
                listingStandard.Label(skillDef.LabelCap + ": " + skill.Level.ToString(), tooltip: skillDef.description + "\n\n" + SkillComplexityTooltip(skill, true) + "\n" + SkillComplexityTooltip(skill, false));
                Listing_Standard subsection = listingStandard.BeginHiddenSection(out float subsectionHeight);
                subsection.ColumnWidth = (rect.width - Margin) / 2;
                if (subsection.ButtonText("MDR_AddSkillLevel".Translate()) && MDR_Utils.SkillComplexityThresholdFor(pawn, skillDef) is int addThreshold && skill.Level < Mathf.Min(addThreshold + 4, SkillRecord.MaxLevel))
                {
                    skill.Level++;
                    if (skill.Level > addThreshold)
                    {
                        proposedSkillComplexity += 1;
                    }
                    else
                    {
                        proposedSkillComplexity += 0.5f;
                    }
                    programComp.UpdateComplexity("Skills", Mathf.Max(0, Mathf.CeilToInt(proposedSkillComplexity)));
                }

                subsection.NewHiddenColumn(ref subsectionHeight);
                if (subsection.ButtonText("MDR_RemoveSkillLevel".Translate()))
                {
                    if (skill.Level <= inherentSkills?.GetWithFallback(skill.def, -1))
                    {
                        Messages.Message("MDR_HasInherentSkills".Translate(pawn.LabelShortCap, skill.def.label, inherentSkills[skill.def]), MessageTypeDefOf.RejectInput, false);
                    }
                    else if (skill.Level > SkillRecord.MinLevel)
                    {
                        int skillThreshold = MDR_Utils.SkillComplexityThresholdFor(pawn, skillDef);
                        if (skill.Level > skillThreshold)
                        {
                            proposedSkillComplexity -= 1;
                        }
                        else
                        {
                            proposedSkillComplexity -= 0.5f;
                        }
                        skill.Level--;
                        programComp.UpdateComplexity("Skills", Mathf.Max(0, Mathf.CeilToInt(proposedSkillComplexity)));
                    }
                }
                listingStandard.EndHiddenSection(subsection, subsectionHeight);
            }
            listingStandard.End();
        }
        
        private void DrawDirectives(Rect rect)
        {
            float xIndex = rect.x;
            float yIndex = rect.y;
            Widgets.BeginScrollView(new Rect(xIndex, yIndex, rect.width, rect.height), ref scrollPosition, rect);
            Rect headerSection = new Rect(xIndex, yIndex, rect.width, Text.LineHeight);
            Widgets.Label(headerSection, "MDR_SelectedDirectives".Translate());
            yIndex += Text.LineHeight + Margin;
            if (Widgets.ButtonText(new Rect(xIndex, yIndex, rect.width, 30f), "MDR_SetDroneDirectives".Translate()))
            {
                Find.WindowStack.Add(new Dialog_SetDroneDirectives(pawn, ref proposedDirectives));
            }
            yIndex += 30f + Margin;
            float directiveBlockWidth = (rect.width - (4 * Margin)) / 3;
            float directiveBlockHeight = 80f;
            float xMaxIndex = rect.x + rect.width;
            float directiveBlockWithMarginWidth = directiveBlockWidth + Margin;
            float directiveIconSize = Mathf.Min(directiveBlockWidth - Margin / 3, directiveBlockHeight - (Margin / 4));
            Rect contentBGSection = new Rect(xIndex, yIndex, rect.width, rect.height);
            Widgets.DrawRectFast(contentBGSection, Widgets.MenuSectionBGFillColor);
            xIndex += Margin;
            yIndex += Margin;

            if (!proposedDirectives.Any())
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = ColoredText.SubtleGrayColor;
                Widgets.Label(contentBGSection, "(" + "NoneLower".Translate() + ")");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else
            {
                for (int i = 0; i < proposedDirectives.Count; i++)
                {
                    if (xIndex + directiveBlockWithMarginWidth > xMaxIndex)
                    {
                        xIndex = rect.x + Margin;
                        yIndex += directiveBlockHeight + Margin;
                    }
                    DirectiveDef directiveDef = proposedDirectives[i];
                    Rect blockSection = new Rect(xIndex, yIndex, directiveBlockWidth, directiveBlockHeight);
                    Widgets.DrawOptionBackground(blockSection, false);

                    // Stats section
                    xIndex += Margin / 3;
                    float textLineHeight = Text.LineHeightOf(GameFont.Small);
                    Text.Anchor = TextAnchor.MiddleRight;
                    string directiveComplexity = directiveDef.complexityCost.ToStringWithSign();
                    float statTextWidth = Text.CalcSize(directiveComplexity).x;
                    Widgets.LabelFit(new Rect(xIndex, yIndex + (Margin / 3), statTextWidth, textLineHeight), directiveComplexity);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Rect statSection = new Rect(xIndex, yIndex + (Margin / 3), statTextWidth, textLineHeight);
                    if (Mouse.IsOver(statSection))
                    {
                        Widgets.DrawHighlight(statSection);
                        TooltipHandler.TipRegion(statSection, "MDR_Complexity".Translate().Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + "MDR_ComplexityDirectiveDesc".Translate());
                    }
                    xIndex += statTextWidth + (Margin / 3);

                    // Icon section
                    Rect directiveSection = new Rect(xIndex, yIndex + (Margin / 3), directiveBlockWidth - statTextWidth - Margin, directiveBlockHeight - (2 * Margin / 3));
                    GUI.BeginGroup(directiveSection);
                    Rect blockIconSection = new Rect(directiveSection.width - directiveIconSize, 0f, directiveIconSize, directiveIconSize);
                    GUI.DrawTexture(blockIconSection, backgroundTexture.Texture);
                    Widgets.DefIcon(blockIconSection, directiveDef, null, 0.9f, null, drawPlaceholder: false);
                    Text.Font = GameFont.Tiny;
                    float directiveLabelHeight = Text.CalcHeight(directiveDef.LabelCap, directiveSection.width);
                    Rect directiveLabelBackground = new Rect(0f, blockSection.yMax - directiveLabelHeight, blockSection.width, directiveLabelHeight);
                    GUI.DrawTexture(new Rect(directiveLabelBackground.x, directiveLabelBackground.yMax - directiveLabelHeight, directiveLabelBackground.width, directiveLabelHeight), TexUI.GrayTextBG);
                    Text.Anchor = TextAnchor.LowerCenter;
                    Widgets.Label(directiveLabelBackground, directiveDef.LabelCap);
                    GUI.color = Color.white;
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = GameFont.Small;
                    GUI.EndGroup();
                    if (Mouse.IsOver(directiveSection))
                    {
                        TooltipHandler.TipRegion(directiveSection, delegate
                        {
                            return directiveDef.LabelCap.Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + directiveDef.description + "\n" + directiveDef.CustomDescription;
                        }, 209283172);
                    }
                    xIndex = blockSection.xMax + Margin;
                }
            }
            Widgets.EndScrollView();
        }

        private void DrawSummary(Rect summaryWrapper)
        {
            float summaryHeaderWidth = Mathf.Max(Text.CalcSize("MDR_BaselineComplexity".Translate()).x, Text.CalcSize("MDR_ComplexityEffects".Translate()).x);
            float summaryFullHeaderWidth = summaryHeaderWidth + 30f;
            float summaryRowHeight = summaryWrapper.height / 2;
            int proposedComplexity = ProposedComplexity;
            GUI.BeginGroup(summaryWrapper);
            Rect complexityRowIcon = new Rect(0f, (summaryRowHeight - GenUI.SmallIconSize) / 2f, GenUI.SmallIconSize, GenUI.SmallIconSize);
            Rect complexityRowHeader = new Rect(complexityRowIcon.xMax + Margin, 0, summaryHeaderWidth, summaryRowHeight);
            Rect complexityRow = new Rect(0f, complexityRowHeader.y, summaryWrapper.width, complexityRowHeader.height);
            Widgets.DrawHighlightIfMouseover(complexityRow);
            complexityRow.xMax = complexityRowHeader.xMax + 94f;
            TooltipHandler.TipRegion(complexityRow, "MDR_BaselineComplexityDesc".Translate());
            GUI.DrawTexture(complexityRowIcon, MDR_Textures.complexityIcon);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(complexityRowHeader, "MDR_BaselineComplexity".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Rect effectRowIcon = new Rect(0f, summaryRowHeight + (summaryRowHeight - GenUI.SmallIconSize) / 2f, GenUI.SmallIconSize, GenUI.SmallIconSize);
            Rect effectRowHeader = new Rect(effectRowIcon.xMax + Margin, summaryRowHeight, summaryHeaderWidth, summaryRowHeight);
            Rect summaryRow = new Rect(0f, effectRowHeader.y, summaryWrapper.width, effectRowHeader.height);
            Widgets.DrawHighlightIfMouseover(summaryRow);
            summaryRow.xMax = effectRowHeader.xMax + 94f;
            TooltipHandler.TipRegion(summaryRow, "MDR_ComplexityEffectsDesc".Translate());
            GUI.DrawTexture(effectRowIcon, MDR_Textures.complexityEffectIcon);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(effectRowHeader, "MDR_ComplexityEffects".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            string complexityText = programComp.BaselineComplexity.ToString();
            string complexityRelationText = proposedComplexity.ToString() + " / " + programComp.MaxComplexity.ToString();
            if (proposedComplexity > programComp.MaxComplexity)
            {
                complexityRelationText = complexityRelationText.Colorize(ColorLibrary.RedReadable);
            }
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(summaryFullHeaderWidth, 0f, 90f, summaryRowHeight), complexityText);
            Widgets.Label(new Rect(summaryFullHeaderWidth, summaryRowHeight, 90f, summaryRowHeight), complexityRelationText);
            Text.Anchor = TextAnchor.MiddleLeft;
            float width = summaryWrapper.width - summaryHeaderWidth - 90f - GenUI.SmallIconSize - Margin;
            Rect summaryRowText = new Rect(summaryFullHeaderWidth + 90f + Margin, summaryRowHeight, width, summaryRowHeight);
            if (summaryRowText.width != summaryCachedWidth)
            {
                summaryCachedWidth = summaryRowText.width;
                summaryCachedText.Clear();
            }
            string effectDescription = ComplexityEffectDescAt(proposedComplexity, programComp.MaxComplexity);
            Widgets.Label(summaryRowText, effectDescription.Truncate(summaryRowText.width, summaryCachedText));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }

        private string ComplexityEffectDescAt(int complexity, int maxComplexity)
        {
            if (complexity <= programComp.BaselineComplexity && complexity <= maxComplexity)
            {
                return "MDR_ComplexityEffectPositive".Translate();
            }
            else if (complexity <= maxComplexity)
            {
                return "MDR_ComplexityEffectNeutral".Translate();
            }
            return "MDR_ComplexityEffectNegative".Translate();
        }

        private string WorkTypeComplexityTooltip(WorkTypeDef workTypeDef, bool adding)
        {
            // Inherent work types can not be removed, inform the player of this.
            if (programExtension.inherentWorkTypes.NotNullAndContains(workTypeDef))
            {
                return "MDR_CantRemoveInherentWorkTypes".Translate(workTypeDef.labelShort.CapitalizeFirst(), pawn.LabelShortCap);
            }

            MDR_WorkTypeExtension workTypeExtension = workTypeDef.GetModExtension<MDR_WorkTypeExtension>();
            StringBuilder stringBuilder = new StringBuilder();
            if (adding)
            {
                stringBuilder.AppendLine("MDR_WorkTypeComplexityHeaderTooltip".Translate("MDR_Adding".Translate().CapitalizeFirst(), "MDR_Cost".Translate(), workTypeExtension.ComplexityCostFor(pawn, adding).ToString()));
            }
            else
            {
                stringBuilder.AppendLine("MDR_WorkTypeComplexityHeaderTooltip".Translate("MDR_Removing".Translate().CapitalizeFirst(), "MDR_Refund".Translate(), workTypeExtension.ComplexityCostFor(pawn, adding).ToString()));
            }
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("MDR_WorkTypeComplexityBaseTooltip".Translate(workTypeDef.labelShort.CapitalizeFirst(), workTypeExtension.baseComplexity.ToStringWithSign()));

            int skillTypeComplexity = 0;
            if (adding)
            {
                // The additional cost is only applied to skills that are currently disabled.
                // It does not apply if another work type enabled it already.
                foreach (SkillDef skillDef in workTypeDef.relevantSkills)
                {
                    if (pawn.skills.GetSkill(skillDef).TotallyDisabled)
                    {
                        skillTypeComplexity += 1;
                    }
                }
            }
            else
            {
                // In order to properly calculate the complexity cost reduction, the worker must take into account other enabled work types.
                // It will only refund the additional complexity for skills that have 0 other work types enabling them.
                List<WorkTypeDef> otherEnabledWorkTypes = new List<WorkTypeDef>();
                foreach (WorkTypeDef enabledWorkTypeDef in pawn.GetComp<CompReprogrammableDrone>().enabledWorkTypes)
                {
                    if (enabledWorkTypeDef != workTypeDef)
                    {
                        otherEnabledWorkTypes.Add(enabledWorkTypeDef);
                    }
                }

                // Refund all skills that would be disabled if the parent work type were removed. Account for skills that are never disabled and other work types.
                foreach (SkillDef skillDef in workTypeDef.relevantSkills)
                {
                    if (!skillDef.neverDisabledBasedOnWorkTypes && !otherEnabledWorkTypes.Any(workType => workType.relevantSkills.NotNullAndContains(skillDef)))
                    {
                        skillTypeComplexity += 1;
                    }
                }
            }
            
            if (skillTypeComplexity != 0)
            {
                stringBuilder.AppendLine("MDR_WorkTypeComplexitySkillsTooltip".Translate(workTypeDef.labelShort.CapitalizeFirst(), skillTypeComplexity));
                if (!adding)
                {
                    stringBuilder.AppendLine("MDR_WorkTypeComplexitySkillsUnusedTooltip".Translate());
                }
            }

            return stringBuilder.ToString();
        }

        private string SkillComplexityTooltip(SkillRecord skill, bool adding)
        {
            int skillThreshold = MDR_Utils.SkillComplexityThresholdFor(pawn, skill.def);
            float cost = (skill.Level > skillThreshold) ? 1 : 0.5f;

            if (adding && skill.Level > Mathf.Min(skillThreshold + 4, SkillRecord.MaxLevel))
            {
                return "MDR_CantExceedSkillMax".Translate(pawn.LabelShortCap, Mathf.Min(skillThreshold + 4, SkillRecord.MaxLevel));
            }
            else if (!adding && skill.Level <= inherentSkills?.GetWithFallback(skill.def, -1))
            {
                return "MDR_HasInherentSkills".Translate(pawn.LabelShortCap, skill.def.label, inherentSkills[skill.def]);
            }
            else if (!adding && skill.Level <= SkillRecord.MinLevel)
            {
                return "MDR_CantExceedSkillMin".Translate(pawn.LabelShortCap);
            }

            StringBuilder stringBuilder = new StringBuilder();
            if (adding)
            {
                stringBuilder.AppendLine("MDR_SkillComplexityTooltip".Translate("MDR_Adding".Translate().CapitalizeFirst(), "MDR_Cost".Translate(), cost.ToString(), pawn.LabelShortCap, skillThreshold.ToString()));
            }
            else
            {
                stringBuilder.AppendLine("MDR_SkillComplexityTooltip".Translate("MDR_Removing".Translate().CapitalizeFirst(), "MDR_Refund".Translate(), cost.ToString(), pawn.LabelShortCap, skillThreshold.ToString()));
            }

            if (cost > 0.5f)
            {
                stringBuilder.AppendLine("MDR_SkillComplexityThresholdTooltip".Translate(pawn.LabelShortCap, skillThreshold));
            }
            return stringBuilder.ToString();
        }

        private void CacheLegalWorkTypes()
        {
            legalWorkTypes = new List<WorkTypeDef>();
            foreach (WorkTypeDef workTypeDef in DefDatabase<WorkTypeDef>.AllDefs)
            {
                if ((workTypeDef.workTags != WorkTags.None || !workTypeDef.relevantSkills.NullOrEmpty())
                    && !programExtension.forbiddenWorkTypes.NotNullAndContains(workTypeDef)
                    && (workTypeDef.GetModExtension<MDR_WorkTypeExtension>()?.ValidFor(pawn).Accepted ?? true))
                {
                    legalWorkTypes.Add(workTypeDef);
                }
            }
        }

        protected void Accept()
        {
            if (programComp.MaxComplexity < ProposedComplexity)
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("MDR_ComplexityLimitExceeded".Translate(programComp.MaxComplexity, ProposedComplexity) + "\n\n" + "WantToContinue".Translate(), PostAccept));
            }
            else
            {
                PostAccept();
            }
        }

        private void PostAccept()
        {
            programComp.UpdateComplexity("Work Types", proposedWorkTypeComplexity);
            programComp.UpdateComplexity("Skills", Mathf.Max(0, Mathf.CeilToInt(proposedSkillComplexity)));
            Close();
        }
    }
}