using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Minimal contextual onboarding for level 1. It teaches one complete report loop through actual play:
    /// SORT -> SUM -> report target, then filtered SUM through CUT/PASTE. It never blocks unrelated actions.
    /// The implementation is intentionally presentation-only so the sequence can later be fed by production gameplay events.
    /// </summary>
    [DefaultExecutionOrder(400)]
    public sealed class PrototypeContextualTutorial : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly FieldInfo ViewsField = typeof(ExcelHellPrototype).GetField("views", Flags);
        private static readonly FieldInfo GoalsField = typeof(ExcelHellPrototype).GetField("goals", Flags);
        private static readonly FieldInfo ClipboardField = typeof(ExcelHellPrototype).GetField("clipboard", Flags);
        private static readonly FieldInfo AwaitingSumTargetField = typeof(ExcelHellPrototype).GetField("awaitingSumTarget", Flags);
        private static readonly FieldInfo PendingSumSourcesField = typeof(ExcelHellPrototype).GetField("pendingSumSources", Flags);
        private static readonly FieldInfo FinishedField = typeof(ExcelHellPrototype).GetField("finished", Flags);
        private static readonly FieldInfo LocalizationField = typeof(ExcelHellPrototype).GetField("loc", Flags);

        private static readonly Color PanelBorder = new(0.07f, 0.08f, 0.10f, 0.98f);
        private static readonly Color PanelBackground = new(0.12f, 0.13f, 0.16f, 0.98f);
        private static readonly Color PanelShadow = new(0f, 0f, 0f, 0.38f);
        private static readonly Color Accent = new(0.96f, 0.57f, 0.10f, 1f);
        private static readonly Color PrimaryText = new(0.95f, 0.96f, 0.98f, 1f);
        private static readonly Color SecondaryText = new(0.78f, 0.81f, 0.86f, 1f);
        private static readonly Color Divider = new(0.28f, 0.31f, 0.36f, 1f);

        private ExcelHellPrototype prototype;
        private CellModel[,] cells;
        private ExcelHellCellView[,] views;
        private List<ReportGoal> goals;
        private int step;
        private int lowBonusCountBeforePaste;
        private bool skipped;
        private bool completed;
        private GUIStyle eyebrowStyle;
        private GUIStyle textStyle;
        private GUIStyle buttonStyle;
        private readonly Dictionary<ExcelHellCellView, Outline> highlights = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PrototypeContextualTutorial>() != null) return;
            var helper = new GameObject("EXCEL HELL Contextual Tutorial").AddComponent<PrototypeContextualTutorial>();
            DontDestroyOnLoad(helper.gameObject);
        }

        private void LateUpdate()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype)
                Bind(current);

            if (!Active)
            {
                ClearHighlights();
                return;
            }

            AdvanceFromCurrentState();
            ApplyHighlights();
        }

        private bool Active =>
            prototype != null && PrototypeLevelRuntime.CurrentIndex == 0 && !skipped && !completed && !PrototypePlaytestUsability.TutorialOpen;

        private void Bind(ExcelHellPrototype owner)
        {
            ClearHighlights();
            prototype = owner;
            cells = null;
            views = null;
            goals = null;
            step = 0;
            lowBonusCountBeforePaste = 0;
            skipped = false;
            completed = false;

            if (prototype == null || PrototypeLevelRuntime.CurrentIndex != 0) return;
            cells = CellsField?.GetValue(prototype) as CellModel[,];
            views = ViewsField?.GetValue(prototype) as ExcelHellCellView[,];
            goals = GoalsField?.GetValue(prototype) as List<ReportGoal>;
        }

        private void AdvanceFromCurrentState()
        {
            if (cells == null || goals == null) return;

            switch (step)
            {
                case 0:
                    if (FieldIsAssembled("salary")) step = 1;
                    break;
                case 1:
                    if (IsAwaitingSumForField("salary")) step = 2;
                    else if (GoalSatisfied("goal.salary")) step = 3;
                    break;
                case 2:
                    if (GoalSatisfied("goal.salary")) step = 3;
                    break;
                case 3:
                    if (FieldIsAssembled("bonus"))
                    {
                        lowBonusCountBeforePaste = CountLowBonusesInsideAssembledSpan();
                        step = 4;
                    }
                    break;
                case 4:
                    if (ClipboardIsLowBonus()) step = 5;
                    else if (CountLowBonusesInsideAssembledSpan() == 0) step = 7;
                    break;
                case 5:
                    if (!ClipboardIsLowBonus())
                    {
                        var remaining = CountLowBonusesInsideAssembledSpan();
                        step = remaining == 0 ? 7 : 6;
                    }
                    break;
                case 6:
                    if (CountLowBonusesInsideAssembledSpan() == 0 && ClipboardField?.GetValue(prototype) == null)
                        step = 7;
                    break;
                case 7:
                    if (IsAwaitingFilteredBonusSum()) step = 8;
                    else if (GoalSatisfied("goal.bonus5")) step = 9;
                    break;
                case 8:
                    if (GoalSatisfied("goal.bonus5")) step = 9;
                    break;
                case 9:
                    if (FinishedField?.GetValue(prototype) is bool done && done)
                    {
                        completed = true;
                        ClearHighlights();
                    }
                    break;
            }
        }

        private bool FieldIsAssembled(string fieldId)
        {
            var key = FindTokenCell(token => token.Kind == ContentKind.FieldKey && token.FieldId == fieldId);
            if (key == null) return false;

            var matching = new List<CellModel>();
            for (var row = key.Row + 1; row <= key.Row + 5 && row < cells.GetLength(0); row++)
                matching.Add(cells[row, key.Column]);

            return matching.Count == 5 && matching.All(cell => cell.State == CellState.Normal && cell.Occupant?.FieldId == fieldId);
        }

        private bool IsAwaitingSumForField(string fieldId)
        {
            if (!(AwaitingSumTargetField?.GetValue(prototype) is bool waiting) || !waiting) return false;
            var sources = PendingSumSourcesField?.GetValue(prototype) as List<CellModel>;
            return sources != null && sources.Count >= 2 && sources.All(cell => cell.Occupant?.FieldId == fieldId);
        }

        private bool IsAwaitingFilteredBonusSum()
        {
            if (!(AwaitingSumTargetField?.GetValue(prototype) is bool waiting) || !waiting) return false;
            var sources = PendingSumSourcesField?.GetValue(prototype) as List<CellModel>;
            return sources != null && sources.Count >= 2 &&
                   sources.All(cell => cell.Occupant?.FieldId == "bonus" && cell.Occupant.Number >= 5d);
        }

        private bool GoalSatisfied(string goalId)
        {
            var goal = goals?.FirstOrDefault(item => item.NameStringId == goalId);
            if (goal == null) return false;
            var target = cells[goal.TargetRow, goal.TargetColumn];
            return target.State == CellState.Normal && goal.IsSatisfiedBy(target.Occupant);
        }

        private bool ClipboardIsLowBonus()
        {
            var token = ClipboardField?.GetValue(prototype) as ContentToken;
            return token?.FieldId == "bonus" && token.Number.HasValue && token.Number.Value < 5d;
        }

        private int CountLowBonusesInsideAssembledSpan()
        {
            var key = FindTokenCell(token => token.Kind == ContentKind.FieldKey && token.FieldId == "bonus");
            if (key == null) return 0;
            var count = 0;
            for (var row = key.Row + 1; row <= key.Row + 5 && row < cells.GetLength(0); row++)
            {
                var token = cells[row, key.Column].Occupant;
                if (token?.FieldId == "bonus" && token.Number.HasValue && token.Number.Value < 5d) count++;
            }
            return count;
        }

        private CellModel FindTokenCell(Func<ContentToken, bool> predicate)
        {
            if (cells == null) return null;
            foreach (var cell in cells)
                if (cell.Occupant != null && predicate(cell.Occupant)) return cell;
            return null;
        }

        private void ApplyHighlights()
        {
            foreach (var marker in highlights.Values)
                if (marker != null) marker.enabled = false;

            foreach (var cell in CurrentHighlightCells())
                Highlight(cell);
        }

        private IEnumerable<CellModel> CurrentHighlightCells()
        {
            switch (step)
            {
                case 0:
                {
                    var salaryKey = FindTokenCell(token => token.Kind == ContentKind.FieldKey && token.FieldId == "salary");
                    if (salaryKey != null) yield return salaryKey;
                    break;
                }
                case 1:
                    foreach (var cell in FieldSpan("salary")) yield return cell;
                    break;
                case 2:
                {
                    var goal = goals?.FirstOrDefault(item => item.NameStringId == "goal.salary");
                    if (goal != null) yield return cells[goal.TargetRow, goal.TargetColumn];
                    break;
                }
                case 3:
                {
                    var bonusKey = FindTokenCell(token => token.Kind == ContentKind.FieldKey && token.FieldId == "bonus");
                    if (bonusKey != null) yield return bonusKey;
                    break;
                }
                case 4:
                case 6:
                    foreach (var cell in FieldSpan("bonus").Where(cell =>
                                 cell.Occupant?.FieldId == "bonus" && cell.Occupant.Number.HasValue && cell.Occupant.Number.Value < 5d))
                        yield return cell;
                    break;
                case 5:
                    foreach (var cell in EmptyCellsOutsideFieldSpan("bonus").Take(8)) yield return cell;
                    break;
                case 7:
                    foreach (var cell in FieldSpan("bonus")) yield return cell;
                    break;
                case 8:
                {
                    var goal = goals?.FirstOrDefault(item => item.NameStringId == "goal.bonus5");
                    if (goal != null) yield return cells[goal.TargetRow, goal.TargetColumn];
                    break;
                }
                case 9:
                    foreach (var goal in goals ?? new List<ReportGoal>())
                        yield return cells[goal.TargetRow, goal.TargetColumn];
                    break;
            }
        }

        private IEnumerable<CellModel> FieldSpan(string fieldId)
        {
            var key = FindTokenCell(token => token.Kind == ContentKind.FieldKey && token.FieldId == fieldId);
            if (key == null) yield break;
            for (var row = key.Row + 1; row <= key.Row + 5 && row < cells.GetLength(0); row++)
                yield return cells[row, key.Column];
        }

        private IEnumerable<CellModel> EmptyCellsOutsideFieldSpan(string fieldId)
        {
            var span = new HashSet<CellModel>(FieldSpan(fieldId));
            foreach (var cell in cells)
                if (!span.Contains(cell) && cell.State == CellState.Normal && cell.Occupant == null)
                    yield return cell;
        }

        private void Highlight(CellModel cell)
        {
            if (views == null || cell == null) return;
            var view = views[cell.Row, cell.Column];
            if (view == null) return;

            if (!highlights.TryGetValue(view, out var marker) || marker == null)
            {
                marker = view.gameObject.AddComponent<Outline>();
                marker.effectColor = Accent;
                marker.effectDistance = new Vector2(4f, -4f);
                marker.useGraphicAlpha = false;
                highlights[view] = marker;
            }
            marker.enabled = true;
        }

        private void ClearHighlights()
        {
            foreach (var marker in highlights.Values)
                if (marker != null) marker.enabled = false;
        }

        private PrototypeLocalization Localization => LocalizationField?.GetValue(prototype) as PrototypeLocalization;
        private string Ru(string russian, string english) =>
            Localization?.Language == PrototypeLanguage.English ? english : russian;

        private string Message()
        {
            return step switch
            {
                0 => Ru("Сначала соберём один обычный показатель. Выберите голубой ключ «Зарплата» и нажмите SORT.",
                    "Start with one ordinary metric. Select the blue Salary key and press SORT."),
                1 => Ru("Зарплаты собраны. Выделите весь столбец из пяти значений и нажмите SUM.",
                    "Salaries are assembled. Select all five values and press SUM."),
                2 => Ru("SUM посчитан. Теперь выберите зелёную клетку отчёта «Итого зарплата»: запись в отчёт не уничтожит исходные числа.",
                    "SUM is ready. Choose the green Salary Total report cell; reporting preserves the source numbers."),
                3 => Ru("Вторая задача — сумма премий только от 5 и выше. Сначала соберите ключом «Премия» весь столбец.",
                    "The second task needs bonuses of 5 or more. First assemble the full Bonus column."),
                4 => Ru("В столбце есть премии меньше 5. Выберите одну из подсвеченных клеток и ВЫРЕЖЬТЕ её: пустую обычную клетку SUM умеет пропускать.",
                    "The column contains bonuses below 5. CUT one highlighted value; SUM can ignore a normal empty cell."),
                5 => Ru("Теперь ВСТАВЬТЕ вырезанное значение в любую свободную клетку вне столбца премий.",
                    "Now PASTE the cut value into any free cell outside the bonus column."),
                6 => Ru("Уберите из диапазона остальные премии меньше 5 тем же способом. Ненужные числа не исчезают — вы освобождаете рабочий диапазон.",
                    "Move the remaining bonuses below 5 out of the range the same way. You are clearing a workable range, not deleting data."),
                7 => Ru("В диапазоне остались только подходящие премии и пустые клетки. Выделите пять клеток столбца и нажмите SUM.",
                    "Only valid bonuses and normal blanks remain. Select the five-cell column span and press SUM."),
                8 => Ru("Запишите результат в зелёную клетку отчёта «Премии ≥ 5».",
                    "Write the result into the green Bonuses ≥ 5 report cell."),
                9 => Ru("Обе задачи заполнены. Нажмите «ОТПРАВИТЬ ОТЧЁТ». Дальше игра перестанет вести вас за руку.",
                    "Both goals are filled. Press SUBMIT REPORT. From here on the game stops guiding every action."),
                _ => string.Empty
            };
        }

        private void OnGUI()
        {
            if (!Active) return;
            EnsureStyles();

            const float outerMargin = 24f;
            var width = Mathf.Max(320f, Mathf.Min(820f, Screen.width - outerMargin * 2f));
            var compact = width < 620f;
            var height = compact ? 148f : 112f;
            var rect = new Rect((Screen.width - width) * 0.5f, Screen.height - height - 26f, width, height);

            DrawSolidRect(new Rect(rect.x + 3f, rect.y + 5f, rect.width, rect.height), PanelShadow);
            DrawSolidRect(rect, PanelBorder);
            var inner = new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f);
            DrawSolidRect(inner, PanelBackground);
            DrawSolidRect(new Rect(inner.x, inner.y, 5f, inner.height), Accent);

            var header = Ru($"ОБУЧЕНИЕ {step + 1}/10", $"TUTORIAL {step + 1}/10");
            GUI.Label(new Rect(rect.x + 22f, rect.y + 11f, rect.width - 44f, 22f), header, eyebrowStyle);

            Rect messageRect;
            Rect buttonRect;
            if (compact)
            {
                textStyle.fontSize = 14;
                messageRect = new Rect(rect.x + 22f, rect.y + 35f, rect.width - 44f, 70f);
                buttonRect = new Rect(rect.xMax - 122f, rect.yMax - 39f, 100f, 28f);
            }
            else
            {
                textStyle.fontSize = 16;
                var dividerX = rect.xMax - 146f;
                DrawSolidRect(new Rect(dividerX, rect.y + 18f, 1f, rect.height - 36f), Divider);
                messageRect = new Rect(rect.x + 22f, rect.y + 34f, rect.width - 190f, rect.height - 44f);
                buttonRect = new Rect(rect.xMax - 124f, rect.y + 39f, 102f, 34f);
            }

            GUI.Label(messageRect, Message(), textStyle);
            if (GUI.Button(buttonRect, Ru("ПРОПУСТИТЬ", "SKIP"), buttonStyle))
            {
                skipped = true;
                ClearHighlights();
            }
        }

        private void EnsureStyles()
        {
            if (textStyle != null) return;

            eyebrowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false
            };
            eyebrowStyle.normal.textColor = Accent;

            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                fontStyle = FontStyle.Normal,
                richText = true
            };
            textStyle.normal.textColor = PrimaryText;
            textStyle.hover.textColor = PrimaryText;
            textStyle.active.textColor = PrimaryText;
            textStyle.focused.textColor = PrimaryText;

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(8, 8, 4, 4)
            };

            buttonStyle.normal.background = CreateSolidTexture(new Color(0.24f, 0.27f, 0.32f, 1f));
            buttonStyle.hover.background = CreateSolidTexture(new Color(0.33f, 0.37f, 0.43f, 1f));
            buttonStyle.active.background = CreateSolidTexture(new Color(0.82f, 0.42f, 0.08f, 1f));
            buttonStyle.focused.background = buttonStyle.hover.background;
            buttonStyle.normal.textColor = PrimaryText;
            buttonStyle.hover.textColor = Color.white;
            buttonStyle.active.textColor = Color.white;
            buttonStyle.focused.textColor = Color.white;
        }

        private static void DrawSolidRect(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previous;
        }

        private static Texture2D CreateSolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            texture.SetPixel(0, 0, color);
            texture.Apply(false, true);
            return texture;
        }
    }
}
