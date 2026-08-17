using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExcelHell.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// L1 guided onboarding driven by actual worksheet state rather than ActionNumber.
    /// Every instruction is spoken by the protagonist BEFORE the corresponding action and paired with
    /// a top-most, raycast-free highlight frame. The full help manual remains the fallback reference.
    /// </summary>
    [DefaultExecutionOrder(2310)]
    public sealed class PrototypeGuidedOnboarding : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly FieldInfo ViewsField = typeof(ExcelHellPrototype).GetField("views", Flags);
        private static readonly FieldInfo GoalsField = typeof(ExcelHellPrototype).GetField("goals", Flags);
        private static readonly FieldInfo SelectionField = typeof(ExcelHellPrototype).GetField("selection", Flags);
        private static readonly FieldInfo FinishedField = typeof(ExcelHellPrototype).GetField("finished", Flags);
        private static readonly FieldInfo ChatWindowField = typeof(PrototypeProductionHud).GetField("chatWindow", Flags);
        private static readonly FieldInfo ChatReservedField = typeof(PrototypeProductionHud).GetField("chatReserved", Flags);
        private static readonly FieldInfo TasksReservedField = typeof(PrototypeProductionHud).GetField("tasksReserved", Flags);

        private static readonly Color Cyan = new(0.01f, 0.92f, 0.92f, 1f);
        private static readonly Color Gold = new(1.0f, 0.69f, 0.18f, 1f);

        private ExcelHellPrototype prototype;
        private PrototypeProductionHud hud;
        private PrototypeProtagonistPresenter protagonist;
        private Canvas canvas;
        private CellModel[,] cells;
        private ExcelHellCellView[,] views;
        private List<ReportGoal> goals;
        private List<CellModel> selection;
        private RectTransform overlayRoot;
        private readonly List<HighlightFrame> frames = new();
        private int step;
        private int announcedStep = -1;
        private float boundAt;
        private bool completed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeGuidedOnboarding>() != null) return;
            var root = new GameObject("[PRESENTATION] Guided Onboarding");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeGuidedOnboarding>();
        }

        private void LateUpdate()
        {
            if (PrototypeAuthoringMode.Active) return;

            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            DisableLegacyTutorials();

            if (!Active)
            {
                HideFrames();
                return;
            }

            hud ??= FindFirstObjectByType<PrototypeProductionHud>();
            protagonist ??= FindFirstObjectByType<PrototypeProtagonistPresenter>();
            EnsureOverlayRoot();
            AdvanceFromState();
            AnnounceCurrentStep();
            DrawCurrentHighlights();
        }

        private bool Active => prototype != null && PrototypeLevelRuntime.CurrentIndex == 0 && !completed;

        private void Bind(ExcelHellPrototype owner)
        {
            DestroyOverlay();
            prototype = owner;
            hud = null;
            protagonist = null;
            canvas = null;
            cells = null;
            views = null;
            goals = null;
            selection = null;
            step = 0;
            announcedStep = -1;
            completed = false;
            boundAt = Time.unscaledTime;

            if (prototype == null || PrototypeLevelRuntime.CurrentIndex != 0) return;
            canvas = prototype.GetComponentsInChildren<Canvas>(true).FirstOrDefault();
            cells = CellsField?.GetValue(prototype) as CellModel[,];
            views = ViewsField?.GetValue(prototype) as ExcelHellCellView[,];
            goals = GoalsField?.GetValue(prototype) as List<ReportGoal>;
            selection = SelectionField?.GetValue(prototype) as List<CellModel>;
        }

        private void AdvanceFromState()
        {
            if (cells == null || goals == null) return;

            switch (step)
            {
                case 0:
                    if (ChatIsOpen()) step = 1;
                    break;
                case 1:
                    if (FieldIsAssembled("salary")) step = 2;
                    break;
                case 2:
                    if (SelectionMatchesFieldSpan("salary")) step = 3;
                    break;
                case 3:
                    if (GoalSatisfied("goal.salary")) step = 4;
                    break;
                case 4:
                    if (FieldIsAssembled("bonus")) step = 5;
                    break;
                case 5:
                    if (CountLowBonusesInsideAssembledSpan() == 0) step = 6;
                    break;
                case 6:
                    if (SelectionMatchesFieldSpan("bonus")) step = 7;
                    break;
                case 7:
                    if (GoalSatisfied("goal.bonus5")) step = 8;
                    break;
                case 8:
                    if (FinishedField?.GetValue(prototype) is bool finished && finished)
                    {
                        completed = true;
                        HideFrames();
                    }
                    break;
            }
        }

        private void AnnounceCurrentStep()
        {
            if (announcedStep == step || protagonist == null) return;
            // Let the authored LevelStart line land first. Step 0 itself is primarily the visible chat highlight.
            if (step == 0 && Time.unscaledTime - boundAt < 1.65f) return;

            var text = StepText(step);
            if (string.IsNullOrWhiteSpace(text)) return;

            var effect = new NarrativeEffectDefinition
            {
                type = NarrativeEffectType.ProtagonistLine,
                text = text,
                mood = ProtagonistMood.Normal,
                priority = 50,
                lifetime = new NarrativeLifetime
                {
                    dismissMode = NarrativeDismissMode.TimedOrClick,
                    duration = step == 0 ? 5.5f : 6.5f
                }
            };
            protagonist.Receive(new NarrativeEffectTicket(new NarrativeEffectRequest($"guided.l1.{step}", effect)));
            announcedStep = step;
            Debug.Log($"[TUTORIAL/GUIDED] step={step} text=\"{text}\"");
        }

        private static string StepText(int value) => value switch
        {
            0 => "Задачи на сегодня пришли в чат. Сначала открою сообщение начальника — там указаны нужные показатели и клетки отчёта.",
            1 => "Начну с зарплаты. Обычный drag переносит данные: перетащу голубой ключ «Зарплата» прямо в подсвеченную =SORT(). SORT по ключу показателя соберёт весь столбец.",
            2 => "Теперь нужен диапазон. Зажимаю Shift и протягиваю от первой до последней зарплаты — так выделяется прямоугольник. Само выделение ход не тратит.",
            3 => "Диапазон выделен. Перетащу его прямо в зелёную =SUM() отчёта. SUM принимает минимум два числа и пропускает обычные пустые клетки. Выделенный диапазон также можно двигать целиком обычным drag.",
            4 => "Первая сумма готова. SORT умеет и фамилии: ключ сотрудника собирает его данные в строку. А занятую формулу сначала освобождают MOVE'ом; пустую формулу можно переносить, DELETE её не уничтожает. Теперь соберу «Премию» вторым SORT.",
            5 => "Нужны только премии 5 и выше. SUM не умеет сам фильтровать числа, зато игнорирует пустоты: вытащу премии ниже 5 обычным drag в любые свободные клетки вне этого столбца. MOVE проверяет только конечные клетки, поэтому можно двигать по диагонали и через занятые места.",
            6 => "В рабочем столбце остались подходящие числа и пустоты. Снова Shift+drag по всему пятистрочному диапазону.",
            7 => "Теперь перетаскиваю выделенный диапазон в вторую зелёную =SUM(). В обычной формуле SUM схлопывает исходные числа; при записи прямо в клетку отчёта исходные данные сохраняются.",
            8 => "Обе клетки отчёта заполнены. Осталось нажать «ОТПРАВИТЬ ОТЧЁТ». Если забуду правило или сочетание — полная справка всегда под кнопкой «?». Каждое успешное действие двигает рабочее время к 18:00.",
            _ => null
        };

        private void DrawCurrentHighlights()
        {
            HideFrames();
            if (overlayRoot == null) return;

            switch (step)
            {
                case 0:
                    Highlight(ChatReserved(), Cyan, 5f);
                    break;
                case 1:
                    Highlight(Rect(View(FindTokenCell(token => token.Kind == ContentKind.FieldKey && token.FieldId == "salary"))), Cyan, 4f);
                    Highlight(Rect(View(FindEmptyFormula(FormulaKind.Sort))), Gold, 4f);
                    break;
                case 2:
                    foreach (var cell in FieldSpan("salary")) Highlight(Rect(View(cell)), Cyan, 3f);
                    break;
                case 3:
                    Highlight(Rect(View(GoalCell("goal.salary"))), Gold, 5f);
                    break;
                case 4:
                    Highlight(Rect(View(FindTokenCell(token => token.Kind == ContentKind.FieldKey && token.FieldId == "bonus"))), Cyan, 4f);
                    Highlight(Rect(View(FindEmptyFormula(FormulaKind.Sort))), Gold, 4f);
                    break;
                case 5:
                    foreach (var cell in FieldSpan("bonus").Where(IsLowBonus)) Highlight(Rect(View(cell)), Gold, 4f);
                    foreach (var cell in FreeCellsOutsideFieldSpan("bonus").Take(4)) Highlight(Rect(View(cell)), Cyan, 2f);
                    break;
                case 6:
                    foreach (var cell in FieldSpan("bonus")) Highlight(Rect(View(cell)), Cyan, 3f);
                    break;
                case 7:
                    Highlight(Rect(View(GoalCell("goal.bonus5"))), Gold, 5f);
                    break;
                case 8:
                    Highlight(TasksReserved(), Gold, 5f);
                    break;
            }
        }

        private bool ChatIsOpen()
        {
            hud ??= FindFirstObjectByType<PrototypeProductionHud>();
            return ChatWindowField?.GetValue(hud) is GameObject window && window.activeSelf;
        }

        private RectTransform ChatReserved()
        {
            hud ??= FindFirstObjectByType<PrototypeProductionHud>();
            return ChatReservedField?.GetValue(hud) as RectTransform;
        }

        private RectTransform TasksReserved()
        {
            hud ??= FindFirstObjectByType<PrototypeProductionHud>();
            return TasksReservedField?.GetValue(hud) as RectTransform;
        }

        private bool FieldIsAssembled(string fieldId)
        {
            var key = FindTokenCell(token => token.Kind == ContentKind.FieldKey && token.FieldId == fieldId);
            if (key == null) return false;
            var span = FieldSpan(fieldId).ToList();
            return span.Count == 5 && span.All(cell => cell.State == CellState.Normal && cell.Occupant?.FieldId == fieldId);
        }

        private bool SelectionMatchesFieldSpan(string fieldId)
        {
            selection = SelectionField?.GetValue(prototype) as List<CellModel>;
            if (selection == null) return false;
            var span = FieldSpan(fieldId).ToList();
            return span.Count == 5 && selection.Count == span.Count && span.All(selection.Contains);
        }

        private bool GoalSatisfied(string goalId)
        {
            var goal = goals?.FirstOrDefault(item => item.NameStringId == goalId);
            if (goal == null) return false;
            var target = cells[goal.TargetRow, goal.TargetColumn];
            return target.State == CellState.Normal && goal.IsSatisfiedBy(target.Occupant);
        }

        private CellModel GoalCell(string goalId)
        {
            var goal = goals?.FirstOrDefault(item => item.NameStringId == goalId);
            return goal == null ? null : cells[goal.TargetRow, goal.TargetColumn];
        }

        private CellModel FindEmptyFormula(FormulaKind kind)
        {
            if (cells == null) return null;
            foreach (var cell in cells)
                if (cell.State == CellState.Normal && cell.Formula == kind && cell.Occupant == null)
                    return cell;
            return null;
        }

        private CellModel FindTokenCell(Func<ContentToken, bool> predicate)
        {
            if (cells == null) return null;
            foreach (var cell in cells)
                if (cell.Occupant != null && predicate(cell.Occupant)) return cell;
            return null;
        }

        private IEnumerable<CellModel> FieldSpan(string fieldId)
        {
            var key = FindTokenCell(token => token.Kind == ContentKind.FieldKey && token.FieldId == fieldId);
            if (key == null) yield break;
            for (var row = key.Row + 1; row <= key.Row + 5 && row < cells.GetLength(0); row++)
                yield return cells[row, key.Column];
        }

        private int CountLowBonusesInsideAssembledSpan() => FieldSpan("bonus").Count(IsLowBonus);

        private static bool IsLowBonus(CellModel cell)
        {
            var token = cell?.Occupant;
            return token?.FieldId == "bonus" && token.Number.HasValue && token.Number.Value < 5d;
        }

        private IEnumerable<CellModel> FreeCellsOutsideFieldSpan(string fieldId)
        {
            var span = new HashSet<CellModel>(FieldSpan(fieldId));
            foreach (var cell in cells)
                if (!span.Contains(cell) && cell.State == CellState.Normal && cell.Occupant == null && !cell.IsFormula)
                    yield return cell;
        }

        private ExcelHellCellView View(CellModel cell) => cell == null || views == null ? null : views[cell.Row, cell.Column];

        private static RectTransform Rect(ExcelHellCellView view) =>
            view == null ? null : view.GetComponent<RectTransform>();

        private void EnsureOverlayRoot()
        {
            if (canvas == null && prototype != null)
                canvas = prototype.GetComponentsInChildren<Canvas>(true).FirstOrDefault();
            if (canvas == null) return;

            if (overlayRoot == null)
            {
                var root = new GameObject("Guided Onboarding Overlay", typeof(RectTransform));
                root.transform.SetParent(canvas.transform, false);
                overlayRoot = root.GetComponent<RectTransform>();
                overlayRoot.anchorMin = Vector2.zero;
                overlayRoot.anchorMax = Vector2.one;
                overlayRoot.offsetMin = Vector2.zero;
                overlayRoot.offsetMax = Vector2.zero;
            }
            overlayRoot.SetAsLastSibling();
        }

        private void Highlight(RectTransform target, Color color, float thickness)
        {
            if (target == null || canvas == null || overlayRoot == null) return;
            var frame = AcquireFrame();
            frame.SetTarget(target, canvas.GetComponent<RectTransform>(), color, thickness);
        }

        private HighlightFrame AcquireFrame()
        {
            var frame = frames.FirstOrDefault(item => !item.Active);
            if (frame == null)
            {
                frame = new HighlightFrame(overlayRoot);
                frames.Add(frame);
            }
            frame.Active = true;
            return frame;
        }

        private void HideFrames()
        {
            foreach (var frame in frames) frame.Hide();
            if (overlayRoot != null) overlayRoot.SetAsLastSibling();
        }

        private void DestroyOverlay()
        {
            frames.Clear();
            if (overlayRoot != null) Destroy(overlayRoot.gameObject);
            overlayRoot = null;
        }

        private static void DisableLegacyTutorials()
        {
            var oldPanel = FindFirstObjectByType<PrototypeContextualTutorial>();
            if (oldPanel != null) oldPanel.enabled = false;
            var oldGuide = FindFirstObjectByType<PrototypeTutorialVisualGuide>();
            if (oldGuide != null) oldGuide.enabled = false;
        }

        private sealed class HighlightFrame
        {
            private readonly RectTransform root;
            private readonly Image top;
            private readonly Image bottom;
            private readonly Image left;
            private readonly Image right;
            public bool Active;

            public HighlightFrame(RectTransform parent)
            {
                var go = new GameObject("Tutorial Highlight", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                root = go.GetComponent<RectTransform>();
                root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
                root.pivot = new Vector2(0.5f, 0.5f);
                top = Edge(go.transform, "Top");
                bottom = Edge(go.transform, "Bottom");
                left = Edge(go.transform, "Left");
                right = Edge(go.transform, "Right");
            }

            public void SetTarget(RectTransform target, RectTransform canvasRect, Color color, float thickness)
            {
                if (target == null || canvasRect == null) { Hide(); return; }
                var corners = new Vector3[4];
                target.GetWorldCorners(corners);
                var min = canvasRect.InverseTransformPoint(corners[0]);
                var max = canvasRect.InverseTransformPoint(corners[2]);
                var pad = 4f;
                root.anchoredPosition = (min + max) * 0.5f;
                root.sizeDelta = new Vector2(Mathf.Abs(max.x - min.x) + pad * 2f, Mathf.Abs(max.y - min.y) + pad * 2f);

                var pulse = 0.58f + 0.42f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6.4f));
                color.a = pulse;
                Configure(top, color, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -thickness), new Vector2(0f, thickness));
                Configure(bottom, color, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, thickness), new Vector2(0f, thickness));
                Configure(left, color, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(thickness, 0f), new Vector2(thickness, 0f));
                Configure(right, color, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-thickness, 0f), new Vector2(thickness, 0f));
                root.gameObject.SetActive(true);
                root.SetAsLastSibling();
                Active = true;
            }

            public void Hide()
            {
                Active = false;
                if (root != null) root.gameObject.SetActive(false);
            }

            private static Image Edge(Transform parent, string name)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                var image = go.GetComponent<Image>();
                image.raycastTarget = false;
                return image;
            }

            private static void Configure(Image image, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
            {
                if (image == null) return;
                var rect = image.rectTransform;
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = sizeDelta;
                image.color = color;
                image.gameObject.SetActive(true);
            }
        }
    }
}
