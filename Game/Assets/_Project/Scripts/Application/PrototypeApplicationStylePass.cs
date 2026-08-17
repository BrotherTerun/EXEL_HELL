using System.Collections.Generic;
using ExcelHell.Prototype;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Application
{
    /// <summary>
    /// Release-only presentation skin for the runtime application shell.
    /// Deliberately touches visuals only: no navigation, gameplay, save, audio or level-flow logic.
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public sealed class PrototypeApplicationStylePass : MonoBehaviour
    {
        private const float RetryInterval = 0.35f;
        private const float MinGlitchDelay = 20f;
        private const float MaxGlitchDelay = 30f;

        private sealed class MenuGlitchTarget
        {
            public Button Button;
            public RectTransform Label;
            public Image Surface;
            public Vector2 BasePosition;
            public Color BaseColor;
        }

        private ExcelHellApplication application;
        private Canvas appCanvas;
        private GameObject mainScreen;
        private Font displayFont;
        private bool styled;
        private float nextRetryAt;
        private float nextMenuGlitchAt;
        private float glitchEndsAt;
        private float nextGlitchPhaseAt;
        private int glitchPhase;
        private MenuGlitchTarget activeGlitch;
        private readonly List<MenuGlitchTarget> mainMenuGlitchTargets = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeApplicationStylePass>(FindObjectsInactive.Include) != null) return;

            var root = new GameObject("[PRESENTATION] Application Style Pass");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeApplicationStylePass>();
        }

        private void Update()
        {
            if (PrototypeAuthoringMode.Active) return;

            var current = FindFirstObjectByType<ExcelHellApplication>(FindObjectsInactive.Include);
            if (current != application)
            {
                RestoreActiveGlitch();
                application = current;
                appCanvas = null;
                mainScreen = null;
                styled = false;
                mainMenuGlitchTargets.Clear();
                nextRetryAt = 0f;
            }

            if (!styled && Time.unscaledTime >= nextRetryAt)
            {
                nextRetryAt = Time.unscaledTime + RetryInterval;
                TryApply();
            }

            TickMainMenuGlitch();
        }

        private void TryApply()
        {
            if (application == null) return;

            foreach (var candidate in application.GetComponentsInChildren<Canvas>(true))
            {
                if (candidate.gameObject.name != "Application Canvas") continue;
                appCanvas = candidate;
                break;
            }

            if (appCanvas == null) return;

            mainScreen = FindDirectChild(appCanvas.transform, "Main Menu")?.gameObject;
            var pauseScreen = FindDirectChild(appCanvas.transform, "Pause Menu")?.gameObject;
            var settingsScreen = FindDirectChild(appCanvas.transform, "Settings")?.gameObject;
            var loadScreen = FindDirectChild(appCanvas.transform, "Load Game")?.gameObject;
            var helpScreen = FindDirectChild(appCanvas.transform, "Help")?.gameObject;
            if (mainScreen == null || pauseScreen == null || settingsScreen == null || loadScreen == null || helpScreen == null)
                return;

            displayFont = Resources.Load<Font>("Fonts/Tiny5-Regular") ?? PrototypeVisualTheme.MonoFont;

            BuildMainMenuBackdrop(mainScreen.transform);
            StyleScreen(mainScreen, 1f);
            StyleScreen(pauseScreen, 0.94f);
            StyleScreen(settingsScreen, 0.985f);
            StyleScreen(loadScreen, 0.985f);
            StyleScreen(helpScreen, 0.985f);

            var mainPanel = FindDirectChild(mainScreen.transform, "Panel");
            var pausePanel = FindDirectChild(pauseScreen.transform, "Panel");
            var settingsPanel = FindDirectChild(settingsScreen.transform, "Panel");
            var loadPanel = FindDirectChild(loadScreen.transform, "Panel");
            var helpPanel = FindDirectChild(helpScreen.transform, "Panel");

            StylePanel(mainPanel, true);
            StylePanel(pausePanel, false);
            StylePanel(settingsPanel, false);
            StylePanel(loadPanel, false);
            StylePanel(helpPanel, false);

            StyleMainMenu(mainPanel);
            StyleSectionTitle(pausePanel, 36);
            StyleSectionTitle(settingsPanel, 34);
            StyleSectionTitle(loadPanel, 34);
            StyleSectionTitle(helpPanel, 34);

            foreach (var text in appCanvas.GetComponentsInChildren<Text>(true))
            {
                if (text == null) continue;
                if (text == DirectTextAt(mainPanel, 0)) continue;
                text.font = PrototypeVisualTheme.UiFont;
                text.color = PrototypeVisualTheme.Text;
            }

            // Re-assert authored display fonts after the generic text pass.
            StyleMainMenu(mainPanel);
            StyleSectionTitle(pausePanel, 36);
            StyleSectionTitle(settingsPanel, 34);
            StyleSectionTitle(loadPanel, 34);
            StyleSectionTitle(helpPanel, 34);

            foreach (var button in appCanvas.GetComponentsInChildren<Button>(true))
                StyleButton(button);

            foreach (var slider in appCanvas.GetComponentsInChildren<Slider>(true))
                StyleSlider(slider);

            var resolutionPopup = FindDirectChild(appCanvas.transform, "Resolution Popup");
            if (resolutionPopup != null)
            {
                var image = resolutionPopup.GetComponent<Image>();
                if (image != null) image.color = PrototypeVisualTheme.Panel;
                AddOutline(image, new Color(PrototypeVisualTheme.Mint.r, PrototypeVisualTheme.Mint.g, PrototypeVisualTheme.Mint.b, 0.24f), 1f);
            }

            CollectMainMenuGlitchTargets();
            nextMenuGlitchAt = Time.unscaledTime + Random.Range(MinGlitchDelay, MaxGlitchDelay);
            styled = true;
            Debug.Log("[APP/UI] Final style pass applied: office menu, unified typography, framed controls and subtle menu glitch.");
        }

        private void BuildMainMenuBackdrop(Transform parent)
        {
            if (FindDirectChild(parent, "Menu Office Backdrop") != null) return;

            var backdrop = new GameObject("Menu Office Backdrop", typeof(RectTransform), typeof(RawImage));
            backdrop.transform.SetParent(parent, false);
            backdrop.transform.SetAsFirstSibling();
            Stretch(backdrop.GetComponent<RectTransform>());

            var raw = backdrop.GetComponent<RawImage>();
            raw.raycastTarget = false;
            var texture = Resources.Load<Texture2D>("Art/OfficeBackground");
            raw.texture = texture;
            raw.color = texture != null ? Color.white : PrototypeVisualTheme.Night;
            if (texture != null) texture.filterMode = FilterMode.Point;

            var veil = new GameObject("Menu Office Veil", typeof(RectTransform), typeof(Image));
            veil.transform.SetParent(parent, false);
            veil.transform.SetSiblingIndex(1);
            Stretch(veil.GetComponent<RectTransform>());
            var veilImage = veil.GetComponent<Image>();
            veilImage.raycastTarget = false;
            veilImage.color = new Color(0.025f, 0.035f, 0.055f, 0.62f);

            var leftShade = new GameObject("Menu Left Shade", typeof(RectTransform), typeof(Image));
            leftShade.transform.SetParent(parent, false);
            leftShade.transform.SetSiblingIndex(2);
            var rect = leftShade.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0.58f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var shade = leftShade.GetComponent<Image>();
            shade.raycastTarget = false;
            shade.color = new Color(0f, 0f, 0f, 0.19f);
        }

        private static void StyleScreen(GameObject screen, float alpha)
        {
            if (screen == null) return;
            var image = screen.GetComponent<Image>();
            if (image == null) return;
            image.color = new Color(PrototypeVisualTheme.Night.r, PrototypeVisualTheme.Night.g, PrototypeVisualTheme.Night.b, alpha);
        }

        private static void StylePanel(RectTransform panel, bool main)
        {
            if (panel == null) return;

            var image = panel.GetComponent<Image>();
            if (image != null)
            {
                var alpha = main ? 0.935f : 0.985f;
                image.color = new Color(PrototypeVisualTheme.Panel.r, PrototypeVisualTheme.Panel.g, PrototypeVisualTheme.Panel.b, alpha);
                AddOutline(image, new Color(PrototypeVisualTheme.Mint.r, PrototypeVisualTheme.Mint.g, PrototypeVisualTheme.Mint.b, main ? 0.30f : 0.20f), 1f);
            }

            if (!main) return;
            panel.anchoredPosition = new Vector2(-390f, 0f);
            panel.sizeDelta = new Vector2(Mathf.Max(panel.sizeDelta.x, 620f), panel.sizeDelta.y);
            AddPanelAccent(panel, "Main Accent", PrototypeVisualTheme.Danger, 5f, true);
            AddPanelAccent(panel, "Main Top Rule", PrototypeVisualTheme.Mint, 2f, false);
        }

        private void StyleMainMenu(RectTransform panel)
        {
            if (panel == null) return;

            var title = DirectTextAt(panel, 0);
            if (title != null)
            {
                title.font = displayFont;
                title.fontSize = 68;
                title.fontStyle = FontStyle.Normal;
                title.color = PrototypeVisualTheme.Text;
                title.alignment = TextAnchor.MiddleLeft;
                var layout = title.GetComponent<LayoutElement>();
                if (layout != null)
                {
                    layout.minHeight = 82f;
                    layout.preferredHeight = 82f;
                }

                var shadow = title.GetComponent<Shadow>() ?? title.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(PrototypeVisualTheme.Danger.r, PrototypeVisualTheme.Danger.g, PrototypeVisualTheme.Danger.b, 0.75f);
                shadow.effectDistance = new Vector2(3f, -1f);
                shadow.useGraphicAlpha = true;
            }

            var subtitle = DirectTextAt(panel, 1);
            if (subtitle != null)
            {
                subtitle.font = PrototypeVisualTheme.MonoFont;
                subtitle.fontSize = 15;
                subtitle.fontStyle = FontStyle.Normal;
                subtitle.color = PrototypeVisualTheme.MutedText;
                subtitle.alignment = TextAnchor.MiddleLeft;
            }
        }

        private static void StyleSectionTitle(RectTransform panel, int size)
        {
            var title = DirectTextAt(panel, 0);
            if (title == null) return;
            title.font = PrototypeVisualTheme.MonoFont;
            title.fontSize = size;
            title.fontStyle = FontStyle.Bold;
            title.color = PrototypeVisualTheme.Text;
            title.alignment = TextAnchor.MiddleLeft;
        }

        private static void StyleButton(Button button)
        {
            if (button == null) return;
            var surface = button.GetComponent<Image>();
            if (surface != null)
            {
                surface.color = PrototypeVisualTheme.ChromeRaised;
                AddOutline(surface, new Color(0.03f, 0.04f, 0.06f, 0.85f), 1f);
            }

            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.10f, 1.10f, 1.10f, 1f);
            colors.pressedColor = new Color(0.82f, 0.86f, 0.91f, 1f);
            colors.selectedColor = new Color(1.04f, 1.04f, 1.04f, 1f);
            colors.disabledColor = new Color(0.48f, 0.50f, 0.54f, 0.62f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.07f;
            button.colors = colors;

            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.font = PrototypeVisualTheme.UiFont;
                label.fontSize = Mathf.Clamp(label.fontSize, 17, 19);
                label.fontStyle = FontStyle.Bold;
                label.color = PrototypeVisualTheme.Text;
            }

            if (surface != null && button.transform.Find("Style Accent") == null)
            {
                var accentGo = new GameObject("Style Accent", typeof(RectTransform), typeof(Image));
                accentGo.transform.SetParent(button.transform, false);
                accentGo.transform.SetAsFirstSibling();
                var accentRect = accentGo.GetComponent<RectTransform>();
                accentRect.anchorMin = new Vector2(0f, 0f);
                accentRect.anchorMax = new Vector2(0f, 1f);
                accentRect.pivot = new Vector2(0f, 0.5f);
                accentRect.anchoredPosition = Vector2.zero;
                accentRect.sizeDelta = new Vector2(3f, 0f);
                var accent = accentGo.GetComponent<Image>();
                accent.raycastTarget = false;
                accent.color = IsDangerButton(label != null ? label.text : string.Empty)
                    ? PrototypeVisualTheme.Danger
                    : new Color(PrototypeVisualTheme.Mint.r, PrototypeVisualTheme.Mint.g, PrototypeVisualTheme.Mint.b, 0.78f);
            }
        }

        private static bool IsDangerButton(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            var upper = text.ToUpperInvariant();
            return upper.Contains("ВЫХОД") || upper.Contains("QUIT") ||
                   upper.Contains("УДАЛИТЬ") || upper.Contains("DELETE") ||
                   upper.Contains("СБРОСИТЬ") || upper.Contains("RESET");
        }

        private static void StyleSlider(Slider slider)
        {
            if (slider == null) return;
            var background = slider.transform.Find("Background")?.GetComponent<Image>();
            var fill = slider.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
            var handle = slider.transform.Find("Handle Slide Area/Handle")?.GetComponent<Image>();
            if (background != null) background.color = PrototypeVisualTheme.Chrome;
            if (fill != null) fill.color = PrototypeVisualTheme.Mint;
            if (handle != null) handle.color = PrototypeVisualTheme.Text;
        }

        private void CollectMainMenuGlitchTargets()
        {
            mainMenuGlitchTargets.Clear();
            if (mainScreen == null) return;

            foreach (var button in mainScreen.GetComponentsInChildren<Button>(true))
            {
                var label = button.GetComponentInChildren<Text>(true);
                var surface = button.GetComponent<Image>();
                if (label == null || surface == null) continue;

                mainMenuGlitchTargets.Add(new MenuGlitchTarget
                {
                    Button = button,
                    Label = label.rectTransform,
                    Surface = surface,
                    BasePosition = label.rectTransform.anchoredPosition,
                    BaseColor = PrototypeVisualTheme.ChromeRaised
                });
            }
        }

        private void TickMainMenuGlitch()
        {
            if (!styled || mainScreen == null || !mainScreen.activeInHierarchy)
            {
                RestoreActiveGlitch();
                return;
            }

            var now = Time.unscaledTime;
            if (activeGlitch != null)
            {
                if (now >= glitchEndsAt)
                {
                    RestoreActiveGlitch();
                    nextMenuGlitchAt = now + Random.Range(MinGlitchDelay, MaxGlitchDelay);
                    return;
                }

                if (now < nextGlitchPhaseAt) return;
                nextGlitchPhaseAt = now + 0.028f;
                glitchPhase++;
                var sign = (glitchPhase & 1) == 0 ? 1f : -1f;
                activeGlitch.Label.anchoredPosition = activeGlitch.BasePosition +
                                                      new Vector2(sign * Random.Range(2f, 4f), Random.Range(-1f, 1f));
                activeGlitch.Surface.color = Color.Lerp(
                    activeGlitch.BaseColor,
                    PrototypeVisualTheme.Danger,
                    (glitchPhase & 1) == 0 ? 0.26f : 0.10f);
                return;
            }

            if (now < nextMenuGlitchAt || mainMenuGlitchTargets.Count == 0) return;

            for (var attempt = 0; attempt < mainMenuGlitchTargets.Count; attempt++)
            {
                var candidate = mainMenuGlitchTargets[Random.Range(0, mainMenuGlitchTargets.Count)];
                if (candidate.Button == null || candidate.Label == null || candidate.Surface == null) continue;
                if (!candidate.Button.interactable) continue;

                candidate.BasePosition = candidate.Label.anchoredPosition;
                candidate.BaseColor = PrototypeVisualTheme.ChromeRaised;
                activeGlitch = candidate;
                glitchPhase = 0;
                glitchEndsAt = now + Random.Range(0.09f, 0.14f);
                nextGlitchPhaseAt = now;
                return;
            }

            nextMenuGlitchAt = now + Random.Range(MinGlitchDelay, MaxGlitchDelay);
        }

        private void RestoreActiveGlitch()
        {
            if (activeGlitch == null) return;
            if (activeGlitch.Label != null) activeGlitch.Label.anchoredPosition = activeGlitch.BasePosition;
            if (activeGlitch.Surface != null) activeGlitch.Surface.color = activeGlitch.BaseColor;
            activeGlitch = null;
        }

        private static void AddPanelAccent(RectTransform panel, string name, Color color, float thickness, bool vertical)
        {
            if (panel == null || panel.Find(name) != null) return;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(panel, false);
            var layout = go.GetComponent<LayoutElement>();
            layout.ignoreLayout = true;

            var rect = go.GetComponent<RectTransform>();
            if (vertical)
            {
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(thickness, -12f);
            }
            else
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(-12f, thickness);
            }

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = color;
        }

        private static void AddOutline(Graphic graphic, Color color, float distance)
        {
            if (graphic == null) return;
            var outline = graphic.GetComponent<Outline>() ?? graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
        }

        private static RectTransform FindDirectChild(Transform parent, string name)
        {
            if (parent == null) return null;
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i) as RectTransform;
                if (child != null && child.gameObject.name == name) return child;
            }
            return null;
        }

        private static Text DirectTextAt(RectTransform parent, int index)
        {
            if (parent == null) return null;
            var found = 0;
            for (var i = 0; i < parent.childCount; i++)
            {
                var text = parent.GetChild(i).GetComponent<Text>();
                if (text == null) continue;
                if (found == index) return text;
                found++;
            }
            return null;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
