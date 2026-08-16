using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Shared visual language for the jam presentation layer. It deliberately skins runtime-created UI
    /// without touching gameplay state or formula logic.
    /// </summary>
    [DefaultExecutionOrder(2050)]
    public sealed class PrototypeVisualTheme : MonoBehaviour
    {
        public static readonly Color Night = Hex("151A23");
        public static readonly Color Chrome = Hex("202838");
        public static readonly Color ChromeRaised = Hex("2A3548");
        public static readonly Color Panel = new(0.105f, 0.13f, 0.18f, 0.985f);
        public static readonly Color PanelSoft = new(0.14f, 0.17f, 0.22f, 0.985f);
        public static readonly Color Sheet = Hex("E9EDF2");
        public static readonly Color SheetSoft = Hex("F4F6F8");
        public static readonly Color Ink = Hex("222A35");
        public static readonly Color Text = Hex("E9EDF3");
        public static readonly Color MutedText = Hex("AEB9C8");
        public static readonly Color Mint = Hex("A8D8BF");
        public static readonly Color Warning = Hex("E68A49");
        public static readonly Color Danger = Hex("C84655");

        private ExcelHellPrototype prototype;
        private Canvas canvas;
        private float nextRefresh;

        public static Font UiFont => Resources.Load<Font>("Fonts/Inter-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Font MonoFont => Resources.Load<Font>("Fonts/IBMPlexMono-Regular") ?? UiFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeVisualTheme>() != null) return;
            var root = new GameObject("[PRESENTATION] Visual Theme");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeVisualTheme>();
        }

        private void Update()
        {
            if (PrototypeAuthoringMode.Active) return;
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype)
            {
                prototype = current;
                canvas = prototype != null ? prototype.GetComponentInChildren<Canvas>(true) : null;
                nextRefresh = 0f;
            }

            if (canvas == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.75f;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            foreach (var text in canvas.GetComponentsInChildren<Text>(true))
            {
                if (text == null) continue;
                text.font = IsSpreadsheetText(text.transform) ? MonoFont : UiFont;
            }

            foreach (var button in canvas.GetComponentsInChildren<Button>(true))
            {
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
                colors.pressedColor = new Color(0.82f, 0.84f, 0.88f, 1f);
                colors.selectedColor = Color.white;
                colors.disabledColor = new Color(0.52f, 0.55f, 0.60f, 0.65f);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.08f;
                button.colors = colors;
            }

            SkinImage("Topbar Surface", Chrome);
            SkinImage("Formula Row Surface", SheetSoft);
            SkinImage("Worksheet Surface", Sheet);
            SkinImage("Tasks Reserved", ChromeRaised);
            SkinImage("Help Reserved", ChromeRaised);
            SkinImage("Clock Reserved", Hex("18202B"));
            SkinImage("Chat Reserved", ChromeRaised);
            SkinImage("Menu Reserved", ChromeRaised);
            SkinImage("Delete Reserved", Hex("313A49"));
            SkinImage("Tasks Window", Panel);
            SkinImage("Help Window", Panel);
            SkinImage("Chat Window", Panel);
            SkinImage("Completion Modal", Panel);
            SkinImage("Chat Body", PanelSoft);
            SkinImage("Narrative Toast", Panel);

            var clock = FindRect("Clock Reserved")?.GetComponentInChildren<Text>(true);
            if (clock != null)
            {
                clock.font = MonoFont;
                clock.fontStyle = FontStyle.Bold;
                clock.color = Mint;
            }

            var formula = FindRect("Formula Bar");
            if (formula != null)
            {
                var image = formula.GetComponent<Image>();
                if (image != null) image.color = SheetSoft;
                foreach (var text in formula.GetComponentsInChildren<Text>(true))
                {
                    text.font = MonoFont;
                    text.color = Ink;
                }
            }

            var spreadsheet = FindRect("Spreadsheet");
            if (spreadsheet != null)
            {
                foreach (var text in spreadsheet.GetComponentsInChildren<Text>(true))
                    text.font = MonoFont;
            }
        }

        private void SkinImage(string name, Color color)
        {
            var rect = FindRect(name);
            if (rect == null) return;
            var image = rect.GetComponent<Image>();
            if (image != null) image.color = color;
        }

        private RectTransform FindRect(string name)
        {
            if (canvas == null) return null;
            foreach (var rect in canvas.GetComponentsInChildren<RectTransform>(true))
                if (rect.gameObject.name == name) return rect;
            return null;
        }

        private static bool IsSpreadsheetText(Transform transform)
        {
            for (var current = transform; current != null; current = current.parent)
            {
                if (current.name == "Spreadsheet" || current.name == "Formula Bar") return true;
            }
            return false;
        }

        private static Color Hex(string rgb)
        {
            return ColorUtility.TryParseHtmlString("#" + rgb, out var color) ? color : Color.white;
        }
    }
}
