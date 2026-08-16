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

        private const float AppX = 24f;
        private const float AppY = -20f;
        private const float AppWidth = 1220f;
        private const float AppHeight = 856f;

        private const float WorksheetX = 40f;
        private const float WorksheetY = -132f;
        private const float WorksheetWidth = 1188f;
        private const float WorksheetHeight = 720f;
        private const float FormulaY = -90f;
        private const float FormulaWidth = 1124f;
        private const float FormulaHeight = 34f;

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

                var aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : ReferenceWidth / ReferenceHeight;
                scaler.matchWidthOrHeight = aspect >= ReferenceWidth / ReferenceHeight ? 1f : 0f;
            }

            background = FindRect(canvas.transform, "Background");
            spreadsheet = FindRect(canvas.transform, "Spreadsheet");
            sidebar = FindRect(canvas.transform, "Sidebar");
            formulaBar = FindRect(canvas.transform, "Formula Bar");
            if (background == null || spreadsheet == null || sidebar == null || formulaBar == null) return;

            var labels = background.GetComponentsInChildren<Text>(true);
            title = labels.FirstOrDefault(text => text.fontSize >= 26)?.rectTransform;
            legacyTurn = labels.FirstOrDefault(text => text != null && text.rectTransform != title && text.fontSize == 20)?.rectTransform;

            ApplyBackground();
            BuildChrome();
            ApplyWorksheetGeometry();
            HideLegacyChrome();
            applied = true;
            Debug.Log("[UI-SHELL] Visual shell v1 applied: office backdrop + spreadsheet surface + world protagonist slot.");
        }

        private void ApplyBackground()
        {
            Stretch(background);
            var image = background.GetComponent<Image>();
            if (image != null) image.color = PrototypeVisualTheme.Night;
        }

        private void BuildChrome()
        {
            chromeRoot = new GameObject("Final Game Window", typeof(RectTransform));
            chromeRoot.transform.SetParent(background, false);
            chromeRoot.transform.SetAsFirstSibling();
            Stretch(chromeRoot.GetComponent<RectTransform>());

            BuildOfficeBackdrop();

            var veil = CreatePanel(chromeRoot.transform, "Office Veil", new Color(0.02f, 0.03f, 0.05f, 0.12f));
            Stretch(veil.rectTransform);

            var shadow = CreatePanel(chromeRoot.transform, "Spreadsheet Shadow", new Color(0f, 0f, 0f, 0.38f));
            SetTopLeft(shadow.rectTransform, AppX + 10f, AppY - 10f, AppWidth, AppHeight);

            var app = CreatePanel(chromeRoot.transform, "Spreadsheet App", new Color(0.92f, 0.93f, 0.945f, 0.985f));
            SetTopLeft(app.rectTransform, AppX, AppY, AppWidth, AppHeight);

            var topbar = CreatePanel(app.transform, "Topbar Surface", PrototypeVisualTheme.Chrome);
            SetTopLeft(topbar.rectTransform, 0f, 0f, AppWidth, 56f);

            CreateReservedButton(app.transform, "Tasks Reserved", "ЗАДАЧИ", 16f, -8f, 128f, 40f);
            CreateReservedButton(app.transform, "Help Reserved", "?", 152f, -8f, 44f, 40f);
            CreateReservedButton(app.transform, "Clock Reserved", "09:00", 838f, -8f, 128f, 40f);
            CreateReservedButton(app.transform, "Chat Reserved", "✉", 974f, -8f, 56f, 40f);
            CreateReservedButton(app.transform, "Menu Reserved", "МЕНЮ", 1038f, -8f, 166f, 40f);

            var formulaRow = CreatePanel(app.transform, "Formula Row Surface", PrototypeVisualTheme.SheetSoft);
            SetTopLeft(formulaRow.rectTransform, 16f, -70f, WorksheetWidth, FormulaHeight);
            CreateReservedButton(app.transform, "Delete Reserved", "DEL", 1140f, -70f, 64f, FormulaHeight);

            var worksheetSurface = CreatePanel(app.transform, "Worksheet Surface", PrototypeVisualTheme.Sheet);
            SetTopLeft(worksheetSurface.rectTransform, 16f, -112f, WorksheetWidth, WorksheetHeight);

            var officeZone = new GameObject("Office Scene Reserved", typeof(RectTransform));
            officeZone.transform.SetParent(chromeRoot.transform, false);
            SetTopLeft(officeZone.GetComponent<RectTransform>(), 1250f, -64f, 330f, 800f);

            var avatar = new GameObject("Avatar Reserved", typeof(RectTransform));
            avatar.transform.SetParent(chromeRoot.transform, false);
            SetTopLeft(avatar.GetComponent<RectTransform>(), 1254f, -568f, 322f, 286f);
        }

        private void BuildOfficeBackdrop()
        {
            var go = new GameObject("Office Backdrop", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(chromeRoot.transform, false);
            Stretch(go.GetComponent<RectTransform>());

            var image = go.GetComponent<RawImage>();
            image.raycastTarget = false;
            image.color = Color.white;
            image.texture = Resources.Load<Texture2D>("Art/OfficeBackground");
            if (image.texture == null)
            {
                image.color = new Color(0.085f, 0.10f, 0.13f, 1f);
                Debug.LogWarning("[VISUAL] Art/OfficeBackground not found in Resources; using dusk fallback until the PNG is imported.");
            }
        }

        private void ApplyWorksheetGeometry()
        {
            spreadsheet.SetParent(background, false);
            SetTopLeft(spreadsheet, WorksheetX, WorksheetY, WorksheetWidth, WorksheetHeight);
            spreadsheet.SetAsLastSibling();

            var grid = spreadsheet.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                var columnCount = Mathf.Max(1, grid.constraintCount);
                var rowCount = Mathf.Max(1, Mathf.CeilToInt((float)spreadsheet.childCount / columnCount));
                grid.cellSize = new Vector2(Mathf.Floor(WorksheetWidth / columnCount), Mathf.Floor(WorksheetHeight / rowCount));
            }

            formulaBar.SetParent(background, false);
            SetTopLeft(formulaBar, WorksheetX, FormulaY, FormulaWidth, FormulaHeight);
            formulaBar.SetAsLastSibling();

            var formulaTexts = formulaBar.GetComponentsInChildren<Text>(true);
            var expression = formulaTexts.FirstOrDefault(text => text.text != "fx");
            foreach (var text in formulaTexts)
            {
                text.font = PrototypeVisualTheme.MonoFont;
                text.color = PrototypeVisualTheme.Ink;
            }
            if (expression != null)
                SetTopLeft(expression.rectTransform, 48f, 0f, FormulaWidth - 56f, FormulaHeight);
        }

        private void HideLegacyChrome()
        {
            sidebar.SetParent(background, false);
            sidebar.gameObject.SetActive(false);
            if (title != null) title.gameObject.SetActive(false);
            if (legacyTurn != null) legacyTurn.gameObject.SetActive(false);
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

        private static void CreateReservedButton(Transform parent, string name, string label, float x, float y, float width, float height)
        {
            var panel = CreatePanel(parent, name, PrototypeVisualTheme.ChromeRaised);
            SetTopLeft(panel.rectTransform, x, y, width, height);
            AddPlaceholderLabel(panel.transform, label, label == "✉" ? 21 : 14, PrototypeVisualTheme.Text);
        }

        private static void AddPlaceholderLabel(Transform parent, string value, int fontSize, Color color)
        {
            var go = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            Stretch(rect, 6f);
            var label = go.GetComponent<Text>();
            label.font = PrototypeVisualTheme.UiFont;
            label.text = value;
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            label.raycastTarget = false;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            rect.localScale = Vector3.one;
        }
    }
}
