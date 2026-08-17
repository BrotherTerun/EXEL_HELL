using System;
using ExcelHell.Prototype;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ExcelHell.Application
{
    [DefaultExecutionOrder(32600)]
    public sealed class PrototypeCreditsScreen : MonoBehaviour
    {
        private ExcelHellApplication application;
        private Canvas appCanvas;
        private GameObject mainScreen;
        private GameObject creditsScreen;
        private Button creditsButton;
        private Text creditsButtonText;
        private Text creditsTitle;
        private Text creditsBody;
        private Text backText;
        private string lastLanguage;
        private float nextRefreshAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeCreditsScreen>(FindObjectsInactive.Include) != null) return;

            var root = new GameObject("[PRESENTATION] Credits Screen");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeCreditsScreen>();
        }

        private void Update()
        {
            if (PrototypeAuthoringMode.Active) return;

            var current = FindFirstObjectByType<ExcelHellApplication>(FindObjectsInactive.Include);
            if (current != application)
            {
                application = current;
                appCanvas = null;
                mainScreen = null;
                creditsScreen = null;
                creditsButton = null;
                creditsButtonText = null;
                lastLanguage = null;
            }

            if (application == null) return;

            if (Time.unscaledTime >= nextRefreshAt)
            {
                nextRefreshAt = Time.unscaledTime + 0.25f;
                EnsureBuilt();
                RefreshLocalizedText();
            }

            if (creditsScreen != null && creditsScreen.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                CloseCredits();
        }

        private void EnsureBuilt()
        {
            if (appCanvas == null)
            {
                foreach (var canvas in application.GetComponentsInChildren<Canvas>(true))
                {
                    if (canvas.gameObject.name != "Application Canvas") continue;
                    appCanvas = canvas;
                    break;
                }
            }

            if (appCanvas == null) return;
            if (mainScreen == null)
                mainScreen = FindDirectChild(appCanvas.transform, "Main Menu") != null
                    ? FindDirectChild(appCanvas.transform, "Main Menu").gameObject
                    : null;
            if (mainScreen == null) return;

            if (creditsScreen == null)
                BuildCreditsScreen();
            if (creditsButton == null)
                BuildCreditsButton();
        }

        private void BuildCreditsButton()
        {
            var panel = FindDirectChild(mainScreen.transform, "Panel");
            if (panel == null) return;

            var existing = FindDirectChild(panel, "Credits Button");
            if (existing != null)
            {
                creditsButton = existing.GetComponent<Button>();
                creditsButtonText = existing.GetComponentInChildren<Text>(true);
                return;
            }

            var go = new GameObject("Credits Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(panel, false);

            var layout = go.GetComponent<LayoutElement>();
            layout.minHeight = 54f;
            layout.preferredHeight = 54f;
            layout.flexibleHeight = 0f;

            var image = go.GetComponent<Image>();
            image.color = PrototypeVisualTheme.ChromeRaised;
            AddOutline(go, new Color(PrototypeVisualTheme.Mint.r, PrototypeVisualTheme.Mint.g, PrototypeVisualTheme.Mint.b, 0.26f));

            creditsButton = go.GetComponent<Button>();
            creditsButton.targetGraphic = image;
            var colors = creditsButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.82f, 0.84f, 0.88f, 1f);
            colors.disabledColor = new Color(0.52f, 0.55f, 0.60f, 0.65f);
            colors.colorMultiplier = 1f;
            creditsButton.colors = colors;
            creditsButton.onClick.AddListener(OpenCredits);

            creditsButtonText = CreateText(go.transform, string.Empty, 18, FontStyle.Bold, TextAnchor.MiddleCenter, PrototypeVisualTheme.UiFont);
            Stretch(creditsButtonText.rectTransform, 12f);
            creditsButtonText.raycastTarget = false;

            // Keep Exit/Quit as the final action if it is present.
            for (var i = 0; i < panel.childCount; i++)
            {
                var candidate = panel.GetChild(i).GetComponent<Button>();
                if (candidate == null || candidate == creditsButton) continue;
                var text = candidate.GetComponentInChildren<Text>(true);
                var label = text != null ? (text.text ?? string.Empty).Trim().ToUpperInvariant() : string.Empty;
                if (label != "ВЫХОД" && label != "QUIT") continue;
                go.transform.SetSiblingIndex(candidate.transform.GetSiblingIndex());
                break;
            }
        }

        private void BuildCreditsScreen()
        {
            creditsScreen = new GameObject("Credits", typeof(RectTransform), typeof(Image));
            creditsScreen.transform.SetParent(appCanvas.transform, false);
            Stretch(creditsScreen.GetComponent<RectTransform>(), 0f);
            creditsScreen.GetComponent<Image>().color = new Color(PrototypeVisualTheme.Night.r, PrototypeVisualTheme.Night.g, PrototypeVisualTheme.Night.b, 0.995f);

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
            panel.transform.SetParent(creditsScreen.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(1180f, 850f);
            panel.GetComponent<Image>().color = PrototypeVisualTheme.Panel;
            var outline = panel.GetComponent<Outline>();
            outline.effectColor = new Color(PrototypeVisualTheme.Mint.r, PrototypeVisualTheme.Mint.g, PrototypeVisualTheme.Mint.b, 0.28f);
            outline.effectDistance = new Vector2(1f, -1f);

            var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(panel.transform, false);
            var accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(6f, 0f);
            accent.GetComponent<Image>().color = PrototypeVisualTheme.Danger;

            creditsTitle = CreateText(panel.transform, string.Empty, 42, FontStyle.Bold, TextAnchor.MiddleLeft,
                Resources.Load<Font>("Fonts/Tiny5-Regular") ?? PrototypeVisualTheme.MonoFont);
            SetRect(creditsTitle.rectTransform, 42f, -30f, 1080f, 68f);
            creditsTitle.color = PrototypeVisualTheme.Text;

            creditsBody = CreateText(panel.transform, string.Empty, 17, FontStyle.Normal, TextAnchor.UpperLeft, PrototypeVisualTheme.UiFont);
            SetRect(creditsBody.rectTransform, 46f, -110f, 1080f, 650f);
            creditsBody.color = PrototypeVisualTheme.Text;
            creditsBody.lineSpacing = 1.05f;
            creditsBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            creditsBody.verticalOverflow = VerticalWrapMode.Truncate;

            var back = new GameObject("Back Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            back.transform.SetParent(panel.transform, false);
            var backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.anchoredPosition = new Vector2(0f, 28f);
            backRect.sizeDelta = new Vector2(360f, 54f);
            back.GetComponent<Image>().color = PrototypeVisualTheme.ChromeRaised;
            var backOutline = back.GetComponent<Outline>();
            backOutline.effectColor = new Color(PrototypeVisualTheme.Mint.r, PrototypeVisualTheme.Mint.g, PrototypeVisualTheme.Mint.b, 0.30f);
            backOutline.effectDistance = new Vector2(1f, -1f);
            var backButton = back.GetComponent<Button>();
            backButton.onClick.AddListener(CloseCredits);

            backText = CreateText(back.transform, string.Empty, 18, FontStyle.Bold, TextAnchor.MiddleCenter, PrototypeVisualTheme.UiFont);
            Stretch(backText.rectTransform, 8f);
            backText.raycastTarget = false;

            creditsScreen.SetActive(false);
        }

        private void OpenCredits()
        {
            if (creditsScreen == null || mainScreen == null) return;
            RefreshLocalizedText(true);
            mainScreen.SetActive(false);
            creditsScreen.SetActive(true);
            creditsScreen.transform.SetAsLastSibling();
        }

        private void CloseCredits()
        {
            if (creditsScreen != null) creditsScreen.SetActive(false);
            if (mainScreen != null)
            {
                mainScreen.SetActive(true);
                mainScreen.transform.SetAsLastSibling();
            }
        }

        private void RefreshLocalizedText(bool force = false)
        {
            if (creditsButtonText == null || creditsTitle == null || creditsBody == null || backText == null) return;

            var language = ExcelHellApplication.CurrentLanguageCode ?? "ru";
            if (!force && string.Equals(language, lastLanguage, StringComparison.OrdinalIgnoreCase)) return;
            var ru = !string.Equals(language, "en", StringComparison.OrdinalIgnoreCase);

            creditsButtonText.text = ru ? "АВТОРЫ" : "CREDITS";
            creditsTitle.text = ru ? "АВТОРЫ И ЛИЦЕНЗИИ" : "CREDITS & LICENSES";
            backText.text = ru ? "НАЗАД" : "BACK";
            creditsBody.text = ru ? CreditsRu : CreditsEn;
            lastLanguage = language;
        }

        private const string CreditsRu =
            "EXEL HELL\n" +
            "Игра для Dimatit & TAIFUN 3.0 Jam\n\n" +
            "РАЗРАБОТКА\n" +
            "BrotherTerun — дизайн, программирование, нарратив, интеграция и финальная сборка.\n\n" +
            "AI-ASSISTED\n" +
            "ChatGPT (GPT-5.6 Sol, OpenAI) — помощь с кодом, визуальной разработкой и саунд-дизайном.\n\n" +
            "ПЛЕЙТЕСТ\n" +
            "Отдельная благодарность: SonOfSon и DeusEx.\n\n" +
            "САУНДТРЕК\n" +
            "Late Ledger Loop / Spreadsheet Drift / Shifted Cells — созданы с помощью Suno.\n" +
            "Suno Basic / Free: использование регулируется Suno Terms of Service; только personal/non-commercial use, с обязательной атрибуцией Suno.\n\n" +
            "ЗВУК И ВИЗУАЛ\n" +
            "Оригинальные игровые SFX, офисный фон, персонаж и presentation-ассеты — подготовлены для EXEL HELL при AI-assisted workflow BrotherTerun + ChatGPT.\n\n" +
            "ШРИФТЫ\n" +
            "Inter — The Inter Project Authors — SIL Open Font License 1.1.\n" +
            "IBM Plex Mono — IBM Corp. — SIL Open Font License 1.1.\n" +
            "Tiny5 — Gissio — SIL Open Font License 1.1.\n\n" +
            "ТЕХНОЛОГИИ\n" +
            "Unity / Unity Input System.\n\n" +
            "Спасибо, что отработали эту смену.";

        private const string CreditsEn =
            "EXEL HELL\n" +
            "Created for Dimatit & TAIFUN 3.0 Jam\n\n" +
            "DEVELOPMENT\n" +
            "BrotherTerun — design, programming, narrative, integration and final build.\n\n" +
            "AI-ASSISTED\n" +
            "ChatGPT (GPT-5.6 Sol, OpenAI) — assistance with code, visual development and sound design.\n\n" +
            "PLAYTESTING\n" +
            "Special thanks: SonOfSon and DeusEx.\n\n" +
            "SOUNDTRACK\n" +
            "Late Ledger Loop / Spreadsheet Drift / Shifted Cells — created with Suno.\n" +
            "Suno Basic / Free: use governed by the Suno Terms of Service; personal/non-commercial use only, with attribution to Suno required.\n\n" +
            "SOUND & VISUALS\n" +
            "Original game SFX, office background, character and presentation assets were produced for EXEL HELL in an AI-assisted BrotherTerun + ChatGPT workflow.\n\n" +
            "FONTS\n" +
            "Inter — The Inter Project Authors — SIL Open Font License 1.1.\n" +
            "IBM Plex Mono — IBM Corp. — SIL Open Font License 1.1.\n" +
            "Tiny5 — Gissio — SIL Open Font License 1.1.\n\n" +
            "TECHNOLOGY\n" +
            "Unity / Unity Input System.\n\n" +
            "Thank you for completing your shift.";

        private static Text CreateText(Transform parent, string value, int size, FontStyle style, TextAnchor alignment, Font font)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = PrototypeVisualTheme.Text;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void AddOutline(GameObject go, Color color)
        {
            var outline = go.GetComponent<Outline>() ?? go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private static void SetRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
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
    }
}
