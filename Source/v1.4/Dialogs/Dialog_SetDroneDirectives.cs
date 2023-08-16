using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MechHumanlikes
{
    public class Dialog_SetDroneDirectives : Window
    {
        private Pawn pawn;

        private CompReprogrammableDrone programComp;

        private int directiveComplexity;

        private int maxDirectives;

        private int inherentDirectiveCount = 0;

        private List<DirectiveDef> selectedDirectives = new List<DirectiveDef>();

        private List<DirectiveDef> inherentDirectives = new List<DirectiveDef>();

        private bool? selectedCollapsed = false;

        private Dictionary<string, bool> collapsedCategories = new Dictionary<string, bool>();

        private bool hoveringOverDirective;

        private DirectiveDef hoveredDirective;

        private float selectedHeight;

        private float unselectedHeight;

        private float scrollHeight;

        private Vector2 scrollPosition;

        private static readonly CachedTexture backgroundTexture = new CachedTexture("UI/Icons/Settings/DrawPocket");

        private static readonly Vector2 ButSize = new Vector2(150f, 38f);

        private const float directiveBlockWidth = 130f;

        private const float directiveBlockHeight = 80f;

        public override Vector2 InitialSize => new Vector2(Mathf.Min(UI.screenWidth, 1036), UI.screenHeight - 4);

        protected override float Margin => 6f;

        protected List<DirectiveDef> SelectedDirectives => selectedDirectives;

        protected string Header => "MDR_SetDroneDirectives".Translate();

        protected string AcceptButtonLabel => "Confirm".Translate().CapitalizeFirst();

        public Dialog_SetDroneDirectives(Pawn pawn, ref List<DirectiveDef> proposedDirectives)
        {
            forcePause = true;
            closeOnAccept = false;
            absorbInputAroundWindow = true;
            foreach (string category in MDR_Utils.directiveCategories)
            {
                collapsedCategories.Add(category, false);
            }
            this.pawn = pawn;
            programComp = pawn.GetComp<CompReprogrammableDrone>();
            int activeDirectiveComplexity = programComp.GetComplexityFromSource("Active Directives");
            directiveComplexity = activeDirectiveComplexity;
            MDR_ProgrammableDroneExtension programmableDroneExtension = pawn.def.GetModExtension<MDR_ProgrammableDroneExtension>();
            maxDirectives = programmableDroneExtension?.maxDirectives ?? 3;
            inherentDirectiveCount = programmableDroneExtension?.inherentDirectives?.Count ?? 0;
            foreach (DirectiveDef directiveDef in pawn.def.GetModExtension<MDR_ProgrammableDroneExtension>()?.inherentDirectives)
            {
                inherentDirectives.Add(directiveDef);
            }
            selectedDirectives = proposedDirectives;
        }

        public override void DoWindowContents(Rect rect)
        {
            Rect fullWindow = rect;
            fullWindow.yMax -= ButSize.y + Margin;
            Rect header = new Rect(fullWindow.x, fullWindow.y, fullWindow.width, 35f);
            Text.Font = GameFont.Medium;
            Widgets.Label(header, Header);
            Text.Font = GameFont.Small;
            fullWindow.yMin += 39f;
            Rect directiveWindow = new Rect(fullWindow.x + Margin, fullWindow.y, fullWindow.width - Margin * 2f, fullWindow.height - Margin);
            DrawDirectives(directiveWindow);
            Rect footerButtons = rect;
            footerButtons.yMin = footerButtons.yMax - ButSize.y;
            DoBottomButtons(footerButtons);
        }

        // UI Section containing all possible directives
        private void DrawDirectives(Rect rect)
        {
            hoveringOverDirective = false;
            GUI.BeginGroup(rect);
            float yIndex = 0f;
            DrawSection(new Rect(rect.x, rect.y, rect.width, selectedHeight), selectedDirectives, "MDR_SelectedDirectives".Translate(), ref yIndex, ref selectedHeight, adding: false, rect, ref selectedCollapsed);
            if (!selectedCollapsed.Value)
            {
                yIndex += 10f;
            }
            float selectedDirectiveHeight = yIndex;
            Widgets.Label(0f, ref yIndex, rect.width, "MDR_Directives".Translate());
            yIndex += 10f;
            float height = yIndex - selectedDirectiveHeight - Margin;
            if (Widgets.ButtonText(new Rect(rect.width - 150f - (2 * Margin), selectedDirectiveHeight, 150f, height), "CollapseAllCategories".Translate()))
            {
                SoundDefOf.TabClose.PlayOneShotOnCamera();
                foreach (string allDef in MDR_Utils.directiveCategories)
                {
                    collapsedCategories[allDef] = true;
                }
            }
            if (Widgets.ButtonText(new Rect(rect.width - 300f - Margin - (2 * Margin), selectedDirectiveHeight, 150f, height), "ExpandAllCategories".Translate()))
            {
                SoundDefOf.TabOpen.PlayOneShotOnCamera();
                foreach (string allDef in MDR_Utils.directiveCategories)
                {
                    collapsedCategories[allDef] = false;
                }
            }
            float nonSelectorHeight = yIndex;
            Rect directiveSelectorSection = new Rect(0f, yIndex, rect.width - 16f, scrollHeight);
            Widgets.BeginScrollView(new Rect(0f, yIndex, rect.width, rect.height - yIndex), ref scrollPosition, directiveSelectorSection);
            Rect containingRect = directiveSelectorSection;
            containingRect.y = yIndex + scrollPosition.y;
            containingRect.height = rect.height;
            bool? collapsed = null;
            DrawSection(rect, MDR_Utils.cachedSortedDirectives, null, ref yIndex, ref unselectedHeight, adding: true, containingRect, ref collapsed);
            if (Event.current.type == EventType.Layout)
            {
                scrollHeight = yIndex - nonSelectorHeight;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            if (!hoveringOverDirective)
            {
                hoveredDirective = null;
            }
        }

        private void DrawSection(Rect rect, List<DirectiveDef> directives, string label, ref float yIndex, ref float sectionHeight, bool adding, Rect containingRect, ref bool? collapsed)
        {
            float xIndex = Margin;
            if (!label.NullOrEmpty())
            {
                Rect headerSection = new Rect(0f, yIndex, rect.width, Text.LineHeight);
                headerSection.xMax -= (adding ? 16f : (Text.CalcSize("ClickToAddOrRemove".Translate()).x + Margin));
                if (collapsed.HasValue)
                {
                    Rect collapsibleMenuSection = new Rect(headerSection.x, headerSection.y + (headerSection.height - 18f) / 2f, 18f, 18f);
                    GUI.DrawTexture(collapsibleMenuSection, collapsed.Value ? TexButton.Reveal : TexButton.Collapse);
                    if (Widgets.ButtonInvisible(headerSection))
                    {
                        collapsed = !collapsed;
                        if (collapsed.Value)
                        {
                            SoundDefOf.TabClose.PlayOneShotOnCamera();
                        }
                        else
                        {
                            SoundDefOf.TabOpen.PlayOneShotOnCamera();
                        }
                    }
                    if (Mouse.IsOver(headerSection))
                    {
                        Widgets.DrawHighlight(headerSection);
                    }
                    headerSection.xMin += collapsibleMenuSection.width;
                }
                Widgets.Label(headerSection, label);
                if (!adding)
                {
                    Text.Anchor = TextAnchor.UpperRight;
                    GUI.color = ColoredText.SubtleGrayColor;
                    Widgets.Label(new Rect(headerSection.xMax - (3 * Margin), yIndex, rect.width - headerSection.width, Text.LineHeight), "ClickToAddOrRemove".Translate());
                    GUI.color = Color.white;
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                yIndex += Text.LineHeight + 3f;
            }
            if (collapsed == true)
            {
                if (Event.current.type == EventType.Layout)
                {
                    sectionHeight = 0f;
                }
                return;
            }
            float headerSectionHeight = yIndex;
            bool reachedCategoryEnd = false;
            float contentSectionWidth = rect.width - 16f;
            float directiveBlockWithMarginWidth = directiveBlockWidth + Margin;
            float contentNullspaceWidth = (contentSectionWidth - directiveBlockWithMarginWidth * Mathf.Floor(contentSectionWidth / directiveBlockWithMarginWidth)) / 2f;
            Rect contentBGSection = new Rect(0f, yIndex, rect.width, sectionHeight);
            if (!adding)
            {
                Widgets.DrawRectFast(contentBGSection, Widgets.MenuSectionBGFillColor);
            }
            yIndex += Margin;
            if (!directives.Any())
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = ColoredText.SubtleGrayColor;
                Widgets.Label(contentBGSection, "(" + "NoneLower".Translate() + ")");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else
            {
                string directiveCategory = null;
                for (int i = 0; i < directives.Count; i++)
                {
                    DirectiveDef directiveDef = directives[i];
                    if (!directiveDef.EverValidFor(pawn))
                    {
                        continue;
                    }
                    bool reachedWidthLimit = false;
                    if (xIndex + directiveBlockWidth > contentSectionWidth)
                    {
                        xIndex = Margin;
                        yIndex += directiveBlockHeight;
                        reachedWidthLimit = true;
                    }
                    bool categoryCollapsed = collapsedCategories[directiveDef.directiveCategory];
                    if (adding && directiveCategory != directiveDef.directiveCategory)
                    {
                        if (!reachedWidthLimit && reachedCategoryEnd)
                        {
                            xIndex = Margin;
                            yIndex += directiveBlockHeight;
                        }
                        directiveCategory = directiveDef.directiveCategory;
                        Rect categoryHeaderSection = new Rect(xIndex, yIndex, rect.width - 8f, Text.LineHeight);
                        Rect categoryCollapseIconSection = new Rect(categoryHeaderSection.x, categoryHeaderSection.y + (categoryHeaderSection.height - 18f) / 2f, 18f, 18f);
                        GUI.DrawTexture(categoryCollapseIconSection, categoryCollapsed ? TexButton.Reveal : TexButton.Collapse);
                        if (Widgets.ButtonInvisible(categoryHeaderSection))
                        {
                            collapsedCategories[directiveDef.directiveCategory] = !collapsedCategories[directiveDef.directiveCategory];
                            if (collapsedCategories[directiveDef.directiveCategory])
                            {
                                SoundDefOf.TabClose.PlayOneShotOnCamera();
                            }
                            else
                            {
                                SoundDefOf.TabOpen.PlayOneShotOnCamera();
                            }
                        }
                        if (i % 2 == 1)
                        {
                            Widgets.DrawLightHighlight(categoryHeaderSection);
                        }
                        if (Mouse.IsOver(categoryHeaderSection))
                        {
                            Widgets.DrawHighlight(categoryHeaderSection);
                        }
                        categoryHeaderSection.xMin += categoryCollapseIconSection.width;
                        Widgets.Label(categoryHeaderSection, directiveCategory);
                        yIndex += categoryHeaderSection.height;
                        if (!categoryCollapsed)
                        {
                            GUI.color = Color.grey;
                            Widgets.DrawLineHorizontal(xIndex, yIndex, rect.width - Margin);
                            GUI.color = Color.white;
                            yIndex += Margin;
                        }
                    }
                    if (adding && categoryCollapsed)
                    {
                        reachedCategoryEnd = false;
                        if (Event.current.type == EventType.Layout)
                        {
                            sectionHeight = yIndex - headerSectionHeight;
                        }
                        continue;
                    }
                    xIndex = Mathf.Max(xIndex, contentNullspaceWidth);
                    reachedCategoryEnd = true;
                    if (DrawDirective(directiveDef, !adding, ref xIndex, yIndex, directiveBlockWidth, containingRect) && directiveDef.ValidFor(pawn) && CompatibleWithSelections(directiveDef))
                    {
                        if (selectedDirectives.Contains(directiveDef))
                        {
                            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                            DirectiveRemoved(directiveDef);
                        }
                        else
                        {
                            SoundDefOf.Tick_High.PlayOneShotOnCamera();
                            DirectiveAdded(directiveDef);
                        }
                        break;
                    }
                }
            }
            if (!adding || reachedCategoryEnd)
            {
                yIndex += directiveBlockHeight;
            }
            if (Event.current.type == EventType.Layout)
            {
                sectionHeight = yIndex - headerSectionHeight;
            }
        }

        private bool DrawDirective(DirectiveDef directiveDef, bool listAllSection, ref float xIndex, float yIndex, float blockWidth, Rect containingRect)
        {
            bool result = false;
            Rect blockSection = new Rect(xIndex, yIndex, blockWidth, 74f);
            if (!containingRect.Overlaps(blockSection))
            {
                xIndex = blockSection.xMax + Margin;
                return false;
            }
            bool selected = !listAllSection && selectedDirectives.Contains(directiveDef);
            Widgets.DrawOptionBackground(blockSection, selected);
            xIndex += Margin;
            float textLineHeight = Text.LineHeightOf(GameFont.Small);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.LabelFit(new Rect(xIndex, yIndex + Margin, 38f - textLineHeight, textLineHeight), directiveDef.complexityCost.ToStringWithSign());
            Text.Anchor = TextAnchor.UpperLeft;
            Rect statSection = new Rect(xIndex, yIndex + Margin, 38f, textLineHeight);
            if (Mouse.IsOver(statSection))
            {
                Widgets.DrawHighlight(statSection);
                TooltipHandler.TipRegion(statSection, "MDR_Complexity".Translate().Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + "MDR_ComplexityDirectiveDesc".Translate());
            }
            xIndex += 34f;
            Rect directiveSection = new Rect(xIndex, yIndex + Margin, 87f, 68f);
            GUI.BeginGroup(directiveSection);
            float num = blockSection.width - Text.LineHeight;
            Rect blockIconSection = new Rect(directiveSection.width / 2f - num / 2f, 0f, num, num);
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
                    string text = directiveDef.LabelCap.Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + directiveDef.description + "\n" + directiveDef.CustomDescription;
                    if (DirectiveTip(directiveDef) != null)
                    {
                        string tooltip = DirectiveTip(directiveDef);
                        if (!tooltip.NullOrEmpty() && tooltip != "ClickToRemove" && tooltip != "ClickToAdd")
                        {
                            text = text + "\n\n" + tooltip.Colorize(ColorLibrary.RedReadable);
                        }
                    }
                    return text;
                }, 201231048);
            }
            xIndex += 90f;
            if (Mouse.IsOver(blockSection))
            {
                hoveredDirective = directiveDef;
                hoveringOverDirective = true;
            }
            else if (hoveredDirective != null && !directiveDef.CompatibleWith(hoveredDirective))
            {
                Widgets.DrawLightHighlight(blockSection);
            }
            if (Widgets.ButtonInvisible(blockSection))
            {
                result = true;
            }
            xIndex = Mathf.Max(xIndex, blockSection.xMax + Margin);
            return result;
        }

        private string DirectiveTip(DirectiveDef directiveDef)
        {
            if (inherentDirectives.NotNullAndContains(directiveDef))
            {
                return "MDR_CantRemoveInherentDirectives".Translate(directiveDef.LabelCap, pawn.LabelShortCap);
            }
            if (!selectedDirectives.NotNullAndContains(directiveDef))
            {
                AcceptanceReport isValid = directiveDef.ValidFor(pawn);
                if (!isValid)
                {
                    return "MDR_InvalidFor".Translate(directiveDef.LabelCap, pawn.LabelShortCap, isValid.Reason);
                }
            }
            AcceptanceReport compatibilityReport = CompatibleWithSelections(directiveDef);
            if (!compatibilityReport)
            {
                return compatibilityReport.Reason;
            }
            return (selectedDirectives.Contains(directiveDef) ? "ClickToRemove" : "ClickToAdd").Translate().Colorize(ColoredText.SubtleGrayColor);
        }

        private void DirectiveAdded(DirectiveDef directiveDef)
        {
            selectedDirectives.Add(directiveDef);
            directiveComplexity += directiveDef.complexityCost;
        }

        private void DirectiveRemoved(DirectiveDef directiveDef)
        {
            selectedDirectives.Remove(directiveDef);
            directiveComplexity -= directiveDef.complexityCost;
        }

        private void DoBottomButtons(Rect rect)
        {
            if (Widgets.ButtonText(new Rect(rect.xMax - ButSize.x, rect.y, ButSize.x, ButSize.y), AcceptButtonLabel) && CanAccept())
            {
                Accept();
            }
        }

        private bool CanAccept()
        {
            foreach (DirectiveDef selectedDirective in selectedDirectives)
            {
                if (!selectedDirective.requirementWorkers.NullOrEmpty())
                {
                    foreach (DirectiveRequirementWorker requirementWorker in selectedDirective.requirementWorkers)
                    {
                        AcceptanceReport valid = requirementWorker.ValidFor(pawn);
                        if (!valid)
                        {
                            Messages.Message("MDR_InvalidFor".Translate(selectedDirective.label, pawn.LabelShortCap, valid.Reason), null, MessageTypeDefOf.RejectInput, historical: false);
                            return false;
                        }
                    }
                }
            }
            if (!selectedDirectives.NullOrEmpty() && selectedDirectives.Count - inherentDirectiveCount > maxDirectives)
            {
                Messages.Message("MDR_MaxDirectivesExceeded".Translate(selectedDirectives.Count - inherentDirectiveCount, pawn.LabelShortCap, maxDirectives), null, MessageTypeDefOf.RejectInput, historical: false);
            }
            return true;
        }

        // Check whether a proposed directive is compatible with other selected directives. RequirementWorkers do not have access to the selected list.
        private AcceptanceReport CompatibleWithSelections(DirectiveDef directiveDef)
        {
            foreach (DirectiveDef selectedDirective in selectedDirectives)
            {
                if (!selectedDirective.requirementWorkers.NullOrEmpty())
                {
                    foreach (DirectiveRequirementWorker requirementWorker in selectedDirective.requirementWorkers)
                    {
                        AcceptanceReport compatibility = requirementWorker.CompatibleWith(directiveDef);
                        if (!compatibility)
                        {
                            return compatibility.Reason;
                        }
                    }
                }
                if (!directiveDef.requirementWorkers.NullOrEmpty())
                {
                    foreach (DirectiveRequirementWorker requirementWorker in directiveDef.requirementWorkers)
                    {
                        AcceptanceReport compatibility = requirementWorker.CompatibleWith(selectedDirective);
                        if (!compatibility)
                        {
                            return compatibility.Reason;
                        }
                    }
                }
            }
            return true;
        }

        private void Accept()
        {
            programComp.UpdateComplexity("Active Directives", directiveComplexity);
            programComp.SetDirectives(selectedDirectives);
            Log.Warning("[Directives] Count: " + programComp.ActiveDirectives.Count());
            Close();
        }
    }

}