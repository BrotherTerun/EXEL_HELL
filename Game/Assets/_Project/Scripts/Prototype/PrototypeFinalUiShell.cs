using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    [DefaultExecutionOrder(1800)]
    public sealed class PrototypeFinalUiShell : MonoBehaviour
    {
        public const float ReferenceWidth = 1600f;
        public const float ReferenceHeight = 900f;

        private const float WindowX = 32f;
        private const float WindowY = -28f;
        private const float WindowWidth = 1536f;
        private const float WindowHeight = 844f;
        private const float WorksheetX = 54f;
        private const float WorksheetY = -132f;
        private const float WorksheetWidth = 918f;
        private const float WorksheetHeight = 684f;
        private const float RailX = 1000f;
        private const float RailY = -100f;
        private const float RailWidth = 540f;

        private ExcelHellPrototype prototype;
        private Canvas canvas;
        private RectTransform background;
        private RectTransform spreadsheet;
        private RectTransform sidebar;
        private RectTransform formulaBar;
        private RectTransform title;
        private RectTransform legacyTurn;
        private GameObject chromeRoot;
        private bool applied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeFinalUiShell>() != null) return;
            var root = new GameObject("[PRESENTATION] Final UI Shell");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeFinalUiShell>();
        }

        private void LateUpdate()
        {
            if (PrototypeAuthoringMode.Active)
            {
                if (prototype != null || chromeRoot != null) Bind(null);
                return;
            }

            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            if (prototype == null || applied) return;
            TryApply();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            prototype = owner;
            applied = false;
            canvas = null;
            background = null;
            spreadsheet = null;
            sidebar = null;
            formulaBar = null;
            title = null;
            legacyTurn = null;
            DestroyChrome();
            if (prototype != null) canvas = prototype.GetComponentsInChildren<Canvas>(true).FirstOrDefault();
        }

        private void TryApply()
        {
            if (canvas == null) return;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            background = FindRect(canvas.transform, "Background");
            spreadsheet = FindRect(canvas.transform, "Spreadsheet");
            sidebar = FindRect(canvas.transform, "Sidebar");
            formulaBar = FindRect(canvas.transform, "Formula Bar");
            var labels = background != null ? background.GetComponentsInChildren<Text>(true) : System.Array.Empty<Text>();
            title = labels.FirstOrDefault(text => text.fontSize >= 26)?.rectTransform;
            legacyTurn = labels.FirstOrDefault(text => text != null && text.rectTransform != title && text.fontSize == 20)?.rectTransform;
            if (background == null || spreadsheet == null || sidebar == null || formulaBar == null) return;

            ApplyBackground();
            BuildChrome();
            ApplyWorksheetGeometry();
            ApplySidebarGeometry();
            ApplyHeaderGeometry();
            applied = true;
            Debug.Log("[UI-SHELL] Final gameplay layout applied at 1600x900 reference resolution.");
        }

        private void ApplyBackground()
        {
            Stretch(background);
            var image = background.GetComponent<Image>();
            if (image != null) image.color = new Color(0.075f, 0.082f, 0.095f, 1f);
        }

        private void BuildChrome()
        {
            chromeRoot = new GameObject("Final Game Window", typeof(RectTransform));
            chromeRoot.transform.SetParent(background, false);
            chromeRoot.transform.SetAsFirstSibling();
            var rootRect = chromeRoot.GetComponent<RectTransform>();
            SetTopLeft(rootRect, WindowX, WindowY, WindowWidth, WindowHeight);
            var window = CreatePanel(chromeRoot.transform, "Window Surface", new Color(0.91f, 0.925f, 0.94f, 1f));
            Stretch(window.rectTransform);
            var header = CreatePanel(chromeRoot.transform, "Window Header", new Color(0.12f, 0.14f, 0.17f, 1f));
            SetTopLeft(header.rectTransform, 0f, 0f, WindowWidth, 58f);
            var workSurface = CreatePanel(chromeRoot.transform, "Worksheet Surface", new Color(0.965f, 0.97f, 0.975f, 1f));
            SetTopLeft(workSurface.rectTransform, 16f, -74f, 928f, 696f);
            var railSurface = CreatePanel(chromeRoot.transform, "Right Rail Surface", new Color(0.89f, 0.905f, 0.92f, 1f));
            SetTopLeft(railSurface.rectTransform, 960f, -74f, 560f, 696f);
            var avatar = CreatePanel(chromeRoot.transform, "Avatar Reserved", new Color(0.18f, 0.20f, 0.24f, 1f));
            SetTopLeft(avatar.rectTransform, 976f, -92f, 164f, 164f);
            AddPlaceholderLabel(avatar.transform, "PROTAGONIST", 13);
            var clock = CreatePanel(chromeRoot.transform, "Clock Reserved", new Color(0.09f, 0.105f, 0.125f, 1f));
            SetTopLeft(clock.rectTransform, 1156f, -92f, 348f, 72f);
            AddPlaceholderLabel(clock.transform, "09:00  —  18:00", 22);
            var chat = CreatePanel(chromeRoot.transform, "Chat Reserved", new Color(0.82f, 0.84f, 0.87f, 1f));
            SetTopLeft(chat.rectTransform, 1156f, -180f, 348f, 76f);
            AddPlaceholderLabel(chat.transform, "CHAT / NOTIFICATIONS", 14);
            var footer = CreatePanel(chromeRoot.transform, "Footer Reserved", new Color(0.12f, 0.14f, 0.17f, 1f));
            SetTopLeft(footer.rectTransform, 0f, -786f, WindowWidth, 58f);
            AddPlaceholderLabel(footer.transform, "SYSTEM / STATUS", 12);
        }

        private void ApplyWorksheetGeometry()
        {
            spreadsheet.SetParent(background, false);
            SetTopLeft(spreadsheet, WorksheetX, WorksheetY, WorksheetWidth, WorksheetHeight);
            var grid = spreadsheet.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                var columnCount = Mathf.Max(1, grid.constraintCount);
                var rowCount = Mathf.Max(1, spreadsheet.childCount / columnCount);
                grid.cellSize = new Vector2(Mathf.Floor(WorksheetWidth / columnCount), Mathf.Floor(WorksheetHeight / rowCount));
            }
            formulaBar.SetParent(background, false);
            SetTopLeft(formulaBar, WorksheetX, -94f, WorksheetWidth, 30f);
        }

        private void ApplySidebarGeometry()
        {
            sidebar.SetParent(background, false);
            SetTopLeft(sidebar, RailX, RailY - 178f, RailWidth, 516f);
            var image = sidebar.GetComponent<Image>();
            if (image != null) image.color = new Color(0.94f, 0.945f, 0.95f, 1f);
        }

        private void ApplyHeaderGeometry()
        {
            if (title != null)
            {
                title.SetParent(background, false);
                SetTopLeft(title, 58f, -39f, 720f, 46f);
                var text = title.GetComponent<Text>();
                if (text != null) text.color = new Color(0.96f, 0.97f, 0.98f, 1f);
            }
            if (legacyTurn != null)
            {
                legacyTurn.SetParent(background, false);
                SetTopLeft(legacyTurn, 1180f, -40f, 320f, 42f);
                var text = legacyTurn.GetComponent<Text>();
                if (text != null) text.color = new Color(0.82f, 0.85f, 0.89f, 1f);
            }
        }

        private void DestroyChrome()
        {
            if (chromeRoot != null) Destroy(chromeRoot);
            chromeRoot = null;
        }

        private static RectTransform FindRect(Transform root, string objectName)
        {
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
                if (rect.gameObject.name == objectName) return rect;
            return null;
        }

        private static Image CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void AddPlaceholderLabel(Transform parent, string value, int fontSize)
        {
            var go = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            Stretch(rect, 8f);
            var label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = value;
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.78f, 0.81f, 0.86f, 1f);
            label.raycastTarget = false;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
