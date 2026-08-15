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

        private ExcelHellPrototype prototype;
        private CellModel[,] cells;
        private ExcelHellCellView[,] views;
        private List<ReportGoal> goals;
        private int step;
        private int lowBonusCountBeforePaste;
        private bool skipped;
        private bool completed;
        private GUIStyle panelStyle;
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
                marker.effectColor = new Color(0.96f, 0.57f, 0.10f, 1f);
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

            var width = Mathf.Min(760f, Screen.width - 80f);
            var rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 145f, width, 92f);
            GUI.Box(rect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(rect.x + 18, rect.y + 12, rect.width - 150, rect.height - 24), Message(), textStyle);
            if (GUI.Button(new Rect(rect.xMax - 122, rect.y + 26, 104, 38), Ru("ПРОПУСТИТЬ", "SKIP"), buttonStyle))
            {
                skipped = true;
                ClearHighlights();
            }
        }

        private void EnsureStyles()
        {
            if (panelStyle != null) return;
            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = Texture2D.whiteTexture;
            panelStyle.normal.textColor = Color.black;

            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                fontStyle = FontStyle.Normal
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
