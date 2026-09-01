using UnityEngine;
using UnityEngine.UI;
using TMPro;
using com.github.lhervier.ksp.steaminput.ui.styles;
using com.github.lhervier.ksp.shared.ugui.styles;
using com.github.lhervier.ksp.shared.ugui.sprites;
using com.github.lhervier.ksp.shared.ugui.button;
using com.github.lhervier.ksp.shared.ugui;
using com.github.lhervier.ksp.shared;
using com.github.lhervier.ksp.shared.ugui.combo;

namespace com.github.lhervier.ksp.steaminput.ui.ugui.body.settings
{
    /// <summary>
    /// The settings screen shown in place of the cheat sheet (see BodyBuilder.OnShowSettings).
    /// Mirrors the mockup's #view-settings: a head with a back button, a "Logging" section (the
    /// log level rotating button + a hint), and a "Diagnostic" section. Stacked top to bottom.
    /// </summary>
    public class SettingsBuilder : IUGUIBuilder<SettingsController>
    {
        private const string BackGlyph = "‹"; // ‹ (U+2039)
        private static Sprite _hintSprite;

        // ======================================
        // Builder parameters
        // ======================================

        private CheatSheetViewModel _viewModel;
        public SettingsBuilder WithViewModel(CheatSheetViewModel viewModel)
        {
            this._viewModel = viewModel;
            return this;
        }

        // ================================
        // Build
        // ================================

        public SettingsController Build()
        {
            var go = new GameObject("SteamInput.Body.Settings", typeof(RectTransform));
            
            // Stacks the head and the sections back-to-back; each section sizes to its content.
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildHead(go.transform, out ButtonController backButtonController);
            
            BuildSeparator(go.transform);
            
            BuildLoggingSection(go.transform, out ComboController logLevelComboController);
            BuildSeparator(go.transform);

            DiagnosticController diagnosticController = new DiagnosticBuilder()
                .WithViewModel(_viewModel)
                .Build();
            diagnosticController.transform.SetParent(go.transform, false);

            return go
                .AddComponent<SettingsController>()
                .WithViewModel(_viewModel)
                .WithLogLevelComboController(logLevelComboController)
                .WithBackButtonController(backButtonController);
        }

        // Head: back button (returns to the cheat sheet) + the "Settings" title. Mockup .kset-head.
        private void BuildHead(Transform parent, out ButtonController buttonController)
        {
            var go = new GameObject("Head", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.sprite = SpritesGlobal.FillSprite;
            image.type = Image.Type.Simple;
            image.color = SteamInputPalette.SettingsHeadBgColor;
            image.raycastTarget = false;

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(
                SteamInputPalette.SettingsHeadPaddingH,
                SteamInputPalette.SettingsHeadPaddingH,
                SteamInputPalette.SettingsHeadPaddingV,
                SteamInputPalette.SettingsHeadPaddingV);
            layout.spacing = SteamInputPalette.SettingsHeadSpacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            buttonController = new ButtonBuilder()
                .WithObjectName("Back")
                .WithLabel(BackGlyph)
                .Build();
            buttonController.transform.SetParent(go.transform, false);

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(go.transform, false);
            var titleElement = titleGo.AddComponent<LayoutElement>();
            titleElement.flexibleWidth = 1f;

            var title = UGUILabels.AddLabel(titleGo);
            title.text = ModLocalization.GetString("settings").ToUpperInvariant();
            title.fontSize = SteamInputPalette.SettingsHeadTitleFontSize;
            title.fontStyle = FontStyles.Bold;
            title.color = SteamInputPalette.SettingsHeadTitleColor;
            title.alignment = TextAlignmentOptions.Left;
        }

        // Logging section: section label + the log level rotating button + a hint box.
        private void BuildLoggingSection(Transform parent, out ComboController logLevelComboController)
        {
            Transform section = BuildSection(parent, "LoggingSection");

            BuildSectionLabel(section, ModLocalization.GetString("settings_logging"));

            logLevelComboController = new ComboBuilder()
                .WithParent(section)
                .WithLabel(ModLocalization.GetString("settings_logLevel"))
                .WithLabelFor(LogLevels.LabelFor)
                .Build();

            BuildHint(section, ModLocalization.GetString("settings_loggingHint"));
        }

        // ".kset-section": padded vertical container for a section's content.
        private static Transform BuildSection(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(
                SteamInputPalette.SettingsSectionPaddingH,
                SteamInputPalette.SettingsSectionPaddingH,
                SteamInputPalette.SettingsSectionPaddingTop,
                SteamInputPalette.SettingsSectionPaddingBottom);
            layout.spacing = SteamInputPalette.SettingsSectionSpacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return go.transform;
        }

        // ".kset-label": accent, uppercase section title.
        private static void BuildSectionLabel(Transform parent, string text)
        {
            var go = new GameObject("SectionLabel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = UGUILabels.AddLabel(go);
            label.text = (text ?? "").ToUpperInvariant();
            label.fontSize = SteamInputPalette.SettingsLabelFontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = SteamInputPalette.SettingsLabelColor;
            label.alignment = TextAlignmentOptions.Left;
        }

        // ".kset-hint": boxed grey help text under a control.
        private static void BuildHint(Transform parent, string text)
        {
            if (_hintSprite == null)
            {
                _hintSprite = SpritesGlobal.MakeChipSprite(
                    SteamInputPalette.SettingsHintBgColor,
                    SteamInputPalette.SettingsHintBorderColor,
                    1);
            }

            var go = new GameObject("Hint", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.sprite = _hintSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = false;

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(
                SteamInputPalette.SettingsHintPaddingH,
                SteamInputPalette.SettingsHintPaddingH,
                SteamInputPalette.SettingsHintPaddingV,
                SteamInputPalette.SettingsHintPaddingV);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var label = UGUILabels.AddLabel(textGo);
            label.text = text;
            label.fontSize = SteamInputPalette.SettingsHintFontSize;
            label.color = SteamInputPalette.SettingsHintTextColor;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.enableWordWrapping = true;

            // Wrapped text needs the fitter to claim its multi-line height.
            var fitter = textGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // ".kset-sep": 1px horizontal divider between the head and the sections.
        private static void BuildSeparator(Transform parent)
        {
            var go = new GameObject("Separator", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minHeight = 1f;
            layoutElement.preferredHeight = 1f;

            var image = go.AddComponent<Image>();
            image.sprite = SpritesGlobal.FillSprite;
            image.type = Image.Type.Simple;
            image.color = SteamInputPalette.SettingsSeparatorColor;
            image.raycastTarget = false;
        }
    }
}
