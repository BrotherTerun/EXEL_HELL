using System;
using System.Linq;
using System.Reflection;
using ExcelHell.Application;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Production system manual. Unlike contextual onboarding this is intentionally non-diegetic and exhaustive.
    /// It replaces the old one-paragraph help mirror inside ProductionHud while keeping the existing Help button/window.
    /// </summary>
    [DefaultExecutionOrder(2215)]
    public sealed class PrototypeHelpManualPresenter : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly Color Window = new(0.055f, 0.070f, 0.090f, 0.995f);
        private static readonly Color Viewport = new(0.075f, 0.090f, 0.112f, 1f);
        private static readonly Color Text = new(0.91f, 0.93f, 0.95f, 1f);
        private static readonly Color Muted = new(0.55f, 0.64f, 0.71f, 1f);
        private static readonly Color Cyan = new(0.01f, 0.92f, 0.92f, 1f);

        private PrototypeProductionHud hud;
        private FieldInfo helpWindowField;
        private FieldInfo helpBodyField;
        private FieldInfo helpHeaderField;
        private GameObject helpWindow;
        private Text manualText;
        private RectTransform manualContent;
        private ScrollRect scroll;
        private Font inter;
        private Font mono;
        private string languageCode;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeHelpManualPresenter>() != null) return;
            var root = new GameObject("[PRESENTATION] Production Help Manual");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeHelpManualPresenter>();
        }

        private void LateUpdate()
        {
            if (PrototypeAuthoringMode.Active) return;

            var current = FindFirstObjectByType<PrototypeProductionHud>();
            if (current != hud) BindHud(current);
            if (hud == null) return;

            var currentWindow = helpWindowField?.GetValue(hud) as GameObject;
            if (currentWindow != helpWindow) BindWindow(currentWindow);
            if (helpWindow == null || manualText == null) return;

            MaintainSkin();
            var currentLanguage = ExcelHellApplication.CurrentLanguageCode ?? "ru";
            if (!string.Equals(languageCode, currentLanguage, StringComparison.OrdinalIgnoreCase))
            {
                languageCode = currentLanguage;
                RefreshManual();
            }
        }

        private void BindHud(PrototypeProductionHud owner)
        {
            hud = owner;
            helpWindow = null;
            manualText = null;
            manualContent = null;
            scroll = null;
            languageCode = null;

            helpWindowField = helpBodyField = helpHeaderField = null;
            if (hud == null) return;

            var type = typeof(PrototypeProductionHud);
            helpWindowField = type.GetField("helpWindow", PrivateInstance);
            helpBodyField = type.GetField("helpBody", PrivateInstance);
            helpHeaderField = type.GetField("helpHeader", PrivateInstance);
            inter = Resources.Load<Font>("Fonts/Inter-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mono = Resources.Load<Font>("Fonts/IBMPlexMono-Regular") ?? inter;
        }

        private void BindWindow(GameObject window)
        {
            helpWindow = window;
            manualText = null;
            manualContent = null;
            scroll = null;
            languageCode = null;
            if (helpWindow == null) return;

            var legacy = helpBodyField?.GetValue(hud) as Text;
            if (legacy != null) legacy.enabled = false;

            var existing = helpWindow.transform.Find("Production Manual View");
            if (existing != null) Destroy(existing.gameObject);

            BuildManualView();
            MaintainSkin();
            RefreshManual();
            Debug.Log("[HELP/UI] Production FC2 manual bound.");
        }

        private void BuildManualView()
        {
            var viewportGo = new GameObject("Production Manual View", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewportGo.transform.SetParent(helpWindow.transform, false);
            SetTopLeft(viewportGo.GetComponent<RectTransform>(), 18f, -58f, 614f, 334f);
            var viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = Viewport;
            viewportImage.raycastTarget = true;

            var contentGo = new GameObject("Manual Content", typeof(RectTransform), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            manualContent = contentGo.GetComponent<RectTransform>();
            manualContent.anchorMin = new Vector2(0f, 1f);
            manualContent.anchorMax = new Vector2(1f, 1f);
            manualContent.pivot = new Vector2(0.5f, 1f);
            manualContent.anchoredPosition = Vector2.zero;
            manualContent.sizeDelta = Vector2.zero;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            manualText = CreateText(contentGo.transform, string.Empty, 14, FontStyle.Normal, TextAnchor.UpperLeft, inter, Text);
            var textRect = manualText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = new Vector2(0f, -14f);
            textRect.sizeDelta = new Vector2(-28f, 100f);
            manualText.horizontalOverflow = HorizontalWrapMode.Wrap;
            manualText.verticalOverflow = VerticalWrapMode.Overflow;

            var textFitter = manualText.gameObject.AddComponent<ContentSizeFitter>();
            textFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll = viewportGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportGo.GetComponent<RectTransform>();
            scroll.content = manualContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
        }

        private void MaintainSkin()
        {
            if (helpWindow == null) return;
            var bg = helpWindow.GetComponent<Image>();
            if (bg != null) bg.color = Window;

            var header = helpHeaderField?.GetValue(hud) as Text;
            if (header != null)
            {
                header.font = inter;
                header.fontStyle = FontStyle.Bold;
                header.fontSize = 19;
                header.color = Text;
            }

            foreach (var button in helpWindow.GetComponentsInChildren<Button>(true))
            {
                var label = button.GetComponentInChildren<Text>(true);
                if (label == null) continue;
                label.font = inter;
                if (label.text == "×") label.color = Muted;
            }
        }

        private void RefreshManual()
        {
            if (manualText == null) return;
            manualText.text = IsRussian() ? RussianManual() : EnglishManual();
            LayoutRebuilder.ForceRebuildLayoutImmediate(manualText.rectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(manualContent);
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        private static string RussianManual() =>
            "<b>ЦЕЛЬ И ВРЕМЯ</b>\n" +
            "Задачи текущего дня приходят от НАЧАЛЬНИКА в чате. Нужно получить требуемые значения и оставить их в зелёных клетках ОТЧЁТА. Адреса целей указаны в сообщении начальника.\n" +
            "Каждое успешное игровое действие двигает рабочее время к 18:00. Простое выделение и неудачная попытка ход не тратят. Когда цели готовы — нажмите «ОТПРАВИТЬ ОТЧЁТ».\n\n" +

            "<b>ВЫБОР И MOVE</b>\n" +
            "• Клик — выбрать одну клетку.\n" +
            "• <b>Shift + Drag</b> — выделить прямоугольный диапазон.\n" +
            "• Обычный <b>Drag</b> токена — MOVE. Если тянуть токен из уже выделенного диапазона, выбранные данные перемещаются вместе.\n" +
            "• MOVE может идти по диагонали и визуально пересекать занятые клетки: проверяются только конечные клетки. Они должны быть обычными, доступными, без чужих данных и без формул.\n" +
            "• Подписи не переносятся.\n" +
            "• Занятая формула закреплена: сначала вынесите из неё ключ/результат MOVE'ом. Пустую формулу после этого можно перенести в свободную обычную клетку. Саму формулу DELETE не уничтожает.\n\n" +

            "<b>=SORT()</b>\n" +
            "Перетащите в пустую SORT-формулу ровно ОДИН голубой ключ.\n" +
            "• Ключ показателя («Зарплата», «Часы»...) собирает значения этого показателя вертикальным столбцом.\n" +
            "• Ключ сотрудника собирает его показатели горизонтальной строкой.\n" +
            "• Для полного результата нужен непрерывный свободный участок рядом с формулой. Если мешает край листа, повреждение, формула или чужие данные — получите #SPILL!.\n" +
            "• После SORT сам ключ остаётся в формульной клетке. Чтобы использовать формулу снова, вынесите ключ MOVE'ом.\n\n" +

            "<b>=SUM()</b>\n" +
            "Сначала Shift+Drag выделите диапазон, затем перетащите выделенный диапазон прямо в пустую SUM-формулу.\n" +
            "• В диапазоне должно быть минимум ДВА числа.\n" +
            "• Обычные пустые клетки игнорируются.\n" +
            "• Ключи, подписи, #REF! и уничтоженные клетки в диапазоне недопустимы.\n" +
            "• SUM схлопывает числовые источники: после вычисления обычные исходные числа исчезают, а результат остаётся в SUM-формуле. Поэтому сначала сформируйте именно нужный диапазон.\n" +
            "• Значения, уже лежащие в защищённых клетках отчёта и попавшие в SUM как источник, сохраняются.\n" +
            "• Чтобы переиспользовать SUM, сначала вынесите её результат MOVE'ом.\n\n" +

            "<b>ОТЧЁТ</b>\n" +
            "Зелёные клетки — защищённый интерфейс отчёта. Их нельзя удалить. В некоторых заданиях зелёная клетка одновременно является формулой: тогда результат вычисления уже попадает в нужное поле. Сверяйтесь с адресами и формулировками в чате начальника.\n\n" +

            "<b>#REF! И DELETE</b>\n" +
            "Красный #REF! — активный очаг повреждения. Он способен заражать соседние клетки, а повреждённые клетки со временем уничтожаются. Телеграф/глитч заранее показывает место будущей угрозы.\n" +
            "• Чтобы локализовать активный #REF!, выберите его клетку и нажмите кнопку DELETE или клавишу <b>Delete</b>. Очаг исчезнет, но клетка станет уничтоженной и больше не пригодится.\n" +
            "• DELETE обычной клетки тоже уничтожает её навсегда — используйте только когда это действительно выгодно.\n" +
            "• Чёрная/погасшая Destroyed-клетка — постоянная дыра: MOVE, SORT и SUM не могут использовать её как рабочую клетку.\n\n" +

            "<b>БЫСТРО</b>\n" +
            "Клик = одна клетка  ·  Shift+Drag = диапазон  ·  Drag = MOVE  ·  Delete = уничтожить выбранную клетку / локализовать #REF!\n" +
            "Чат = задачи и сообщения  ·  ? = эта справка";

        private static string EnglishManual() =>
            "<b>GOAL & TIME</b>\n" +
            "Daily requirements arrive from the BOSS in chat. Produce the requested values and leave them in the green REPORT cells; the boss message lists their addresses.\n" +
            "Every successful gameplay action advances work time toward 18:00. Selection and failed attempts are free. When all goals are ready, press SUBMIT REPORT.\n\n" +

            "<b>SELECTION & MOVE</b>\n" +
            "• Click selects one cell.\n" +
            "• <b>Shift + Drag</b> selects a rectangular range.\n" +
            "• Normal token <b>Drag</b> performs MOVE. Dragging from an already selected range moves the selected data together.\n" +
            "• MOVE may travel diagonally and visually cross occupied cells; only destination cells matter. Destinations must be Normal, available, free of unrelated data and formula-free.\n" +
            "• Labels cannot be moved.\n" +
            "• An occupied formula is anchored: MOVE its key/result out first. The resulting empty formula can then be moved to an empty Normal cell. DELETE never destroys a formula field.\n\n" +

            "<b>=SORT()</b>\n" +
            "Drop exactly ONE blue key into an empty SORT formula.\n" +
            "• A field key (Salary, Hours...) assembles that field vertically.\n" +
            "• An employee key assembles that employee horizontally.\n" +
            "• SORT needs a contiguous available span beside the formula. Worksheet edges, damage, formulas or unrelated data cause #SPILL!.\n" +
            "• The key remains inside the formula after SORT; MOVE it out before reusing the formula.\n\n" +

            "<b>=SUM()</b>\n" +
            "Select a range with Shift+Drag, then drag that selected range directly into an empty SUM formula.\n" +
            "• At least TWO numbers are required.\n" +
            "• Normal empty cells are ignored.\n" +
            "• Keys, labels, #REF! and Destroyed cells are invalid inside the range.\n" +
            "• SUM collapses numeric sources: normal source numbers are consumed and the aggregate remains in the SUM formula. Prepare the exact range you need first.\n" +
            "• Values already stored in protected report cells are preserved when used as SUM sources.\n" +
            "• MOVE the result out before reusing SUM.\n\n" +

            "<b>REPORT</b>\n" +
            "Green cells are protected report interface cells and cannot be deleted. Some are formulas themselves; in that case the formula result is already in the report. Follow the addresses and requirements from the boss chat.\n\n" +

            "<b>#REF! & DELETE</b>\n" +
            "A red #REF! is an active corruption source. It can infect neighbours and corrupted cells eventually become destroyed. Telegraph/glitch visuals warn about future threat locations.\n" +
            "• Select an active #REF! and press the DELETE button or the <b>Delete</b> key to quarantine it. The outbreak stops, but that cell becomes permanently Destroyed.\n" +
            "• DELETE also permanently destroys normal cells, so use it deliberately.\n" +
            "• A black/dead Destroyed cell is a permanent hole and cannot be used by MOVE, SORT or SUM.\n\n" +

            "<b>QUICK CONTROLS</b>\n" +
            "Click = one cell  ·  Shift+Drag = range  ·  Drag = MOVE  ·  Delete = destroy selected cell / quarantine #REF!\n" +
            "Chat = tasks & messages  ·  ? = this manual";

        private static bool IsRussian() =>
            string.Equals(ExcelHellApplication.CurrentLanguageCode, "ru", StringComparison.OrdinalIgnoreCase);

        private static Text CreateText(Transform parent, string value, int size, FontStyle style, TextAnchor anchor,
            Font font, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = color;
            text.supportRichText = true;
            text.raycastTarget = false;
            return text;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }
    }
}
