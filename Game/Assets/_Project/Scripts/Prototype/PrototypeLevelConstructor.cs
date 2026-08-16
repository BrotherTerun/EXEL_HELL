using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Small jam-time runtime constructor for authored FC2 levels.
    /// F2 toggles the panel. Existing catalog levels are available as clean templates.
    /// Editing mutates only the in-memory play-mode config; EXPORT copies an authored C# block.
    /// #REF! is never placed manually: every rebuild uses the normal anomaly scheduler.
    /// </summary>
    [DefaultExecutionOrder(1400)]
    public sealed class PrototypeLevelConstructor : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo SelectionField = typeof(ExcelHellPrototype).GetField("selection", Flags);

        private static readonly string[] Records = { "ivanov", "petrov", "sidorov", "volkova", "kim" };
        private static readonly string[] Fields = { "hours", "salary", "overtime", "bonus" };
        private static readonly PrototypeReportGoals[] GoalKinds =
        {
            PrototypeReportGoals.SalaryTotal,
            PrototypeReportGoals.OvertimeTotal,
            PrototypeReportGoals.BonusTotal,
            PrototypeReportGoals.BonusAtLeastFour,
            PrototypeReportGoals.SalaryOfMaxOvertime,
            PrototypeReportGoals.SalaryForHoursBelowForty
        };

        private readonly Dictionary<int, PrototypeLevelConfig> cleanTemplates = new();

        private ExcelHellPrototype prototype;
        private bool visible;
        private bool rebuildPending;
        private Rect windowRect = new(1090f, 70f, 490f, 790f);
        private Vector2 scroll;
        private int selectedRow = -1;
        private int selectedColumn = -1;
        private int recordIndex;
        private int fieldIndex;
        private int goalIndex;
        private string valueText = string.Empty;
        private string maxTurnsText = string.Empty;
        private string firstOutbreakText = string.Empty;
        private string respawnText = string.Empty;
        private string activeOutbreakText = string.Empty;
        private string status = "F2 — открыть конструктор";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PrototypeLevelConstructor>() != null) return;
            var helper = new GameObject("EXEL HELL Level Constructor").AddComponent<PrototypeLevelConstructor>();
            DontDestroyOnLoad(helper.gameObject);
        }

        private void Awake()
        {
            for (var i = 0; i < PrototypeLevelCatalog.Count; i++)
                cleanTemplates[i] = CloneLevel(PrototypeLevelCatalog.Get(i));
            PullLevelFields();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
                visible = !visible;

            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != null)
            {
                prototype = current;
                CaptureWorksheetSelection();
            }

            if (rebuildPending && current == null)
            {
                rebuildPending = false;
                prototype = new GameObject("EXEL HELL Prototype").AddComponent<ExcelHellPrototype>();
            }
        }

        private void CaptureWorksheetSelection()
        {
            var selection = SelectionField?.GetValue(prototype) as List<CellModel>;
            if (selection == null || selection.Count != 1) return;
            var cell = selection[0];
            if (cell.Row == selectedRow && cell.Column == selectedColumn) return;
            selectedRow = cell.Row;
            selectedColumn = cell.Column;
            SyncValueTextFromSelection();
        }

        private void OnGUI()
        {
            if (!visible) return;
            windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "EXEL HELL — LEVEL CONSTRUCTOR [F2]");
        }

        private void DrawWindow(int id)
        {
            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label("ШАБЛОН УРОВНЯ");
            GUILayout.BeginHorizontal();
            for (var i = 0; i < PrototypeLevelCatalog.Count; i++)
            {
                var index = i;
                if (GUILayout.Button($"L{i + 1}", GUILayout.Height(28))) LoadTemplate(index);
            }
            GUILayout.EndHorizontal();

            var level = PrototypeLevelRuntime.Current;
            GUILayout.Label($"{level.Id} — {level.NameRu}");
            GUILayout.Space(6);

            DrawSelectedCell(level);
            GUILayout.Space(8);
            DrawTokenTools(level);
            GUILayout.Space(8);
            DrawFormulaTools(level);
            GUILayout.Space(8);
            DrawGoalTools(level);
            GUILayout.Space(8);
            DrawLevelParameters(level);
            GUILayout.Space(10);

            if (GUILayout.Button("EXPORT → CLIPBOARD", GUILayout.Height(36))) Export(level);
            if (GUILayout.Button("ПЕРЕСОБРАТЬ / REBUILD", GUILayout.Height(30))) RequestRebuild();

            GUILayout.Space(8);
            GUILayout.Label(status);
            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0, 0, 10000, 25));
        }

        private void DrawSelectedCell(PrototypeLevelConfig level)
        {
            GUILayout.Label("ВЫБРАННАЯ ЯЧЕЙКА");
            if (!HasSelected(level))
            {
                GUILayout.Label("Кликни по ячейке таблицы.");
                return;
            }

            var address = Address(selectedRow, selectedColumn);
            var token = FindToken(level, selectedRow, selectedColumn);
            var formula = FindFormula(level, selectedRow, selectedColumn);
            var goal = FindGoal(level, selectedRow, selectedColumn);
            GUILayout.Label($"{address}   token: {Describe(token)}   formula: {(formula?.Formula.ToString() ?? "-")}   goal: {(goal?.Goal.ToString() ?? "-")}");

            if (GUILayout.Button("ОЧИСТИТЬ TOKEN + FORMULA", GUILayout.Height(28)))
            {
                RemoveTokenAt(level, selectedRow, selectedColumn);
                RemoveFormulaAt(level, selectedRow, selectedColumn);
                status = $"{address}: содержимое очищено";
                RequestRebuild();
            }
        }

        private void DrawTokenTools(PrototypeLevelConfig level)
        {
            GUILayout.Label("КЛЮЧИ И ЗНАЧЕНИЯ");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Record: {Records[recordIndex]}", GUILayout.Height(28)))
                recordIndex = (recordIndex + 1) % Records.Length;
            if (GUILayout.Button($"Field: {Fields[fieldIndex]}", GUILayout.Height(28)))
                fieldIndex = (fieldIndex + 1) % Fields.Length;
            GUILayout.EndHorizontal();

            GUI.enabled = HasSelected(level);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("DATA")) PlaceData(level);
            if (GUILayout.Button("RECORD KEY")) PlaceRecordKey(level);
            if (GUILayout.Button("FIELD KEY")) PlaceFieldKey(level);
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            var selectedToken = HasSelected(level) ? FindToken(level, selectedRow, selectedColumn) : null;
            if (selectedToken?.Kind == PrototypePlacementKind.Data)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Value", GUILayout.Width(52));
                valueText = GUILayout.TextField(valueText, GUILayout.Width(110));
                if (GUILayout.Button("APPLY VALUE")) ApplySelectedDataValue(level, selectedToken);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawFormulaTools(PrototypeLevelConfig level)
        {
            GUILayout.Label("ФОРМУЛЬНЫЕ ЯЧЕЙКИ");
            GUI.enabled = HasSelected(level);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("=SORT()")) PlaceFormula(level, FormulaKind.Sort);
            if (GUILayout.Button("=SUM()")) PlaceFormula(level, FormulaKind.Sum);
            if (GUILayout.Button("NO FORMULA"))
            {
                RemoveFormulaAt(level, selectedRow, selectedColumn);
                status = $"{Address(selectedRow, selectedColumn)}: formula removed";
                RequestRebuild();
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;
        }

        private void DrawGoalTools(PrototypeLevelConfig level)
        {
            GUILayout.Label("REPORT GOAL (необязательно)");
            if (GUILayout.Button($"Goal: {GoalKinds[goalIndex]}", GUILayout.Height(27)))
                goalIndex = (goalIndex + 1) % GoalKinds.Length;

            GUI.enabled = HasSelected(level);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("ASSIGN GOAL")) AssignGoal(level);
            if (GUILayout.Button("REMOVE GOAL")) RemoveGoal(level);
            GUILayout.EndHorizontal();
            GUI.enabled = true;
        }

        private void DrawLevelParameters(PrototypeLevelConfig level)
        {
            GUILayout.Label("ПАРАМЕТРЫ");
            var newRef = GUILayout.Toggle(level.RefEnabled, "#REF! enabled (spawn остаётся автоматическим)");
            if (newRef != level.RefEnabled)
            {
                level.RefEnabled = newRef;
                status = $"REF enabled = {newRef}";
                RequestRebuild();
            }

            DrawIntField("Max turns", ref maxTurnsText, value => level.MaxTurns = Mathf.Max(1, value));
            DrawIntField("First outbreak", ref firstOutbreakText, value => level.FirstOutbreakTurn = Mathf.Max(1, value));
            DrawIntField("Respawn delay", ref respawnText, value => level.RespawnDelayTurns = Mathf.Max(1, value));
            DrawIntField("Active delay", ref activeOutbreakText, value => level.ActiveOutbreakDelayTurns = Mathf.Max(1, value));
        }

        private void DrawIntField(string label, ref string text, Action<int> setter)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(120));
            text = GUILayout.TextField(text, GUILayout.Width(80));
            if (GUILayout.Button("SET", GUILayout.Width(54)) && int.TryParse(text, out var value))
            {
                setter(value);
                status = $"{label} = {value}";
                RequestRebuild();
            }
            GUILayout.EndHorizontal();
        }

        private void LoadTemplate(int index)
        {
            if (!cleanTemplates.TryGetValue(index, out var clean)) return;
            PrototypeLevelRuntime.SetCurrentIndex(index);
            CopyLevel(clean, PrototypeLevelCatalog.Get(index));
            selectedRow = selectedColumn = -1;
            PullLevelFields();
            status = $"Загружен чистый шаблон L{index + 1}";
            RequestRebuild();
        }

        private void PlaceData(PrototypeLevelConfig level)
        {
            if (!HasSelected(level)) return;
            RemoveFormulaAt(level, selectedRow, selectedColumn);
            RemoveSemanticToken(level, PrototypePlacementKind.Data, Records[recordIndex], Fields[fieldIndex]);
            RemoveTokenAt(level, selectedRow, selectedColumn);
            AddToken(level, new PrototypeTokenPlacement
            {
                Row = selectedRow, Column = selectedColumn, Kind = PrototypePlacementKind.Data,
                RecordId = Records[recordIndex], FieldId = Fields[fieldIndex]
            });
            SyncValueTextFromSelection();
            status = $"{Address(selectedRow, selectedColumn)} ← {Records[recordIndex]}.{Fields[fieldIndex]}";
            RequestRebuild();
        }

        private void PlaceRecordKey(PrototypeLevelConfig level)
        {
            if (!HasSelected(level)) return;
            RemoveFormulaAt(level, selectedRow, selectedColumn);
            RemoveSemanticToken(level, PrototypePlacementKind.RecordKey, Records[recordIndex], null);
            RemoveTokenAt(level, selectedRow, selectedColumn);
            AddToken(level, new PrototypeTokenPlacement
            {
                Row = selectedRow, Column = selectedColumn, Kind = PrototypePlacementKind.RecordKey,
                RecordId = Records[recordIndex]
            });
            status = $"{Address(selectedRow, selectedColumn)} ← record key {Records[recordIndex]}";
            RequestRebuild();
        }

        private void PlaceFieldKey(PrototypeLevelConfig level)
        {
            if (!HasSelected(level)) return;
            RemoveFormulaAt(level, selectedRow, selectedColumn);
            RemoveSemanticToken(level, PrototypePlacementKind.FieldKey, null, Fields[fieldIndex]);
            RemoveTokenAt(level, selectedRow, selectedColumn);
            AddToken(level, new PrototypeTokenPlacement
            {
                Row = selectedRow, Column = selectedColumn, Kind = PrototypePlacementKind.FieldKey,
                FieldId = Fields[fieldIndex]
            });
            status = $"{Address(selectedRow, selectedColumn)} ← field key {Fields[fieldIndex]}";
            RequestRebuild();
        }

        private void PlaceFormula(PrototypeLevelConfig level, FormulaKind kind)
        {
            if (!HasSelected(level)) return;
            RemoveTokenAt(level, selectedRow, selectedColumn);
            RemoveFormulaAt(level, selectedRow, selectedColumn);
            var list = (level.FormulaLayout ?? Array.Empty<PrototypeFormulaPlacement>()).ToList();
            list.Add(new PrototypeFormulaPlacement { Row = selectedRow, Column = selectedColumn, Formula = kind });
            level.FormulaLayout = list.ToArray();
            status = $"{Address(selectedRow, selectedColumn)} ← ={kind.ToString().ToUpperInvariant()}()";
            RequestRebuild();
        }

        private void AssignGoal(PrototypeLevelConfig level)
        {
            if (!HasSelected(level)) return;
            var goalKind = GoalKinds[goalIndex];
            var list = (level.GoalLayout ?? Array.Empty<PrototypeReportGoalPlacement>()).
                Where(x => x.Goal != goalKind && (x.Row != selectedRow || x.Column != selectedColumn)).ToList();
            list.Add(new PrototypeReportGoalPlacement { Goal = goalKind, Row = selectedRow, Column = selectedColumn });
            level.GoalLayout = list.ToArray();
            RebuildGoalFlags(level);
            status = $"{Address(selectedRow, selectedColumn)} ← goal {goalKind}";
            RequestRebuild();
        }

        private void RemoveGoal(PrototypeLevelConfig level)
        {
            if (!HasSelected(level)) return;
            level.GoalLayout = (level.GoalLayout ?? Array.Empty<PrototypeReportGoalPlacement>())
                .Where(x => x.Row != selectedRow || x.Column != selectedColumn).ToArray();
            RebuildGoalFlags(level);
            status = $"{Address(selectedRow, selectedColumn)}: goal removed";
            RequestRebuild();
        }

        private void ApplySelectedDataValue(PrototypeLevelConfig level, PrototypeTokenPlacement token)
        {
            if (!double.TryParse(valueText.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                status = "Некорректное числовое значение";
                return;
            }

            var record = Array.IndexOf(Records, token.RecordId);
            var target = DatasetArray(level.Dataset, token.FieldId);
            if (record < 0 || target == null || record >= target.Length) return;
            target[record] = value;
            status = $"{token.RecordId}.{token.FieldId} = {value.ToString(CultureInfo.InvariantCulture)}";
            RequestRebuild();
        }

        private void Export(PrototypeLevelConfig level)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"// Exported from Level Constructor — template {level.Id}");
            sb.AppendLine("new PrototypeLevelConfig");
            sb.AppendLine("{");
            sb.AppendLine($"    Id = \"{level.Id}_edit\",");
            sb.AppendLine($"    NameRu = \"{level.NameRu}\",");
            sb.AppendLine($"    NameEn = \"{level.NameEn}\",");
            sb.AppendLine($"    Rows = {level.Rows}, Columns = {level.Columns},");
            sb.AppendLine($"    ReportGoals = (PrototypeReportGoals){(int)level.ReportGoals},");
            sb.AppendLine($"    RefEnabled = {level.RefEnabled.ToString().ToLowerInvariant()}, FormulaCellsEnabled = true,");
            sb.AppendLine($"    MaxTurns = {level.MaxTurns}, FirstOutbreakTurn = {level.FirstOutbreakTurn}, RespawnDelayTurns = {level.RespawnDelayTurns}, ActiveOutbreakDelayTurns = {level.ActiveOutbreakDelayTurns},");
            sb.AppendLine($"    CorruptionStepsBeforeDestroy = {level.CorruptionStepsBeforeDestroy}, SpawnPreferredDistance = {level.SpawnPreferredDistance}, SpawnDistanceVariation = {level.SpawnDistanceVariation}, SpawnCandidatePoolSize = {level.SpawnCandidatePoolSize},");
            sb.AppendLine($"    Dataset = Dataset({ArrayCode(level.Dataset.Hours)}, {ArrayCode(level.Dataset.Salary)}, {ArrayCode(level.Dataset.Overtime)}, {ArrayCode(level.Dataset.Bonus)}),");

            sb.AppendLine("    TokenLayout = new[]");
            sb.AppendLine("    {");
            foreach (var p in (level.TokenLayout ?? Array.Empty<PrototypeTokenPlacement>()).OrderBy(x => x.Row).ThenBy(x => x.Column))
                sb.AppendLine($"        {TokenCode(p)},");
            sb.AppendLine("    },");

            sb.AppendLine("    FormulaLayout = new[]");
            sb.AppendLine("    {");
            foreach (var p in (level.FormulaLayout ?? Array.Empty<PrototypeFormulaPlacement>()).OrderBy(x => x.Row).ThenBy(x => x.Column))
                sb.AppendLine($"        Formula(\"{Address(p.Row, p.Column)}\", FormulaKind.{p.Formula}),");
            sb.AppendLine("    },");

            sb.AppendLine("    GoalLayout = new[]");
            sb.AppendLine("    {");
            foreach (var p in (level.GoalLayout ?? Array.Empty<PrototypeReportGoalPlacement>()).OrderBy(x => x.Row).ThenBy(x => x.Column))
                sb.AppendLine($"        Goal(\"{Address(p.Row, p.Column)}\", PrototypeReportGoals.{p.Goal}),");
            sb.AppendLine("    }");
            sb.AppendLine("};");

            GUIUtility.systemCopyBuffer = sb.ToString();
            status = $"Экспорт скопирован в буфер ({level.TokenLayout.Length} tokens, {level.FormulaLayout.Length} formulas).";
        }

        private static string TokenCode(PrototypeTokenPlacement p)
        {
            var address = Address(p.Row, p.Column);
            return p.Kind switch
            {
                PrototypePlacementKind.Data => $"Data(\"{address}\", \"{p.RecordId}\", \"{p.FieldId}\")",
                PrototypePlacementKind.RecordKey => $"Record(\"{address}\", \"{p.RecordId}\")",
                PrototypePlacementKind.FieldKey => $"Field(\"{address}\", \"{p.FieldId}\")",
                PrototypePlacementKind.Label => $"Label(\"{address}\")",
                _ => "/* unknown placement */"
            };
        }

        private static string ArrayCode(double[] values) =>
            "new[] { " + string.Join(", ", (values ?? Array.Empty<double>()).Select(v => v.ToString("0.###", CultureInfo.InvariantCulture) + "d")) + " }";

        private void RequestRebuild()
        {
            if (rebuildPending) return;
            var old = FindFirstObjectByType<ExcelHellPrototype>();
            if (old != null) Destroy(old.gameObject);
            prototype = null;
            rebuildPending = true;
        }

        private void PullLevelFields()
        {
            var level = PrototypeLevelRuntime.Current;
            maxTurnsText = level.MaxTurns.ToString();
            firstOutbreakText = level.FirstOutbreakTurn.ToString();
            respawnText = level.RespawnDelayTurns.ToString();
            activeOutbreakText = level.ActiveOutbreakDelayTurns.ToString();
        }

        private void SyncValueTextFromSelection()
        {
            var level = PrototypeLevelRuntime.Current;
            var token = HasSelected(level) ? FindToken(level, selectedRow, selectedColumn) : null;
            if (token?.Kind != PrototypePlacementKind.Data) return;
            var record = Array.IndexOf(Records, token.RecordId);
            var source = DatasetArray(level.Dataset, token.FieldId);
            if (record >= 0 && source != null && record < source.Length)
                valueText = source[record].ToString(CultureInfo.InvariantCulture);
        }

        private bool HasSelected(PrototypeLevelConfig level) =>
            level != null && selectedRow >= 0 && selectedColumn >= 0 && selectedRow < level.Rows && selectedColumn < level.Columns;

        private static PrototypeTokenPlacement FindToken(PrototypeLevelConfig level, int row, int column) =>
            (level.TokenLayout ?? Array.Empty<PrototypeTokenPlacement>()).FirstOrDefault(x => x.Row == row && x.Column == column);

        private static PrototypeFormulaPlacement FindFormula(PrototypeLevelConfig level, int row, int column) =>
            (level.FormulaLayout ?? Array.Empty<PrototypeFormulaPlacement>()).FirstOrDefault(x => x.Row == row && x.Column == column);

        private static PrototypeReportGoalPlacement FindGoal(PrototypeLevelConfig level, int row, int column) =>
            (level.GoalLayout ?? Array.Empty<PrototypeReportGoalPlacement>()).FirstOrDefault(x => x.Row == row && x.Column == column);

        private static string Describe(PrototypeTokenPlacement p)
        {
            if (p == null) return "-";
            return p.Kind switch
            {
                PrototypePlacementKind.Data => $"{p.RecordId}.{p.FieldId}",
                PrototypePlacementKind.RecordKey => $"record:{p.RecordId}",
                PrototypePlacementKind.FieldKey => $"field:{p.FieldId}",
                PrototypePlacementKind.Label => "label",
                _ => p.Kind.ToString()
            };
        }

        private static void RemoveTokenAt(PrototypeLevelConfig level, int row, int column) =>
            level.TokenLayout = (level.TokenLayout ?? Array.Empty<PrototypeTokenPlacement>()).Where(x => x.Row != row || x.Column != column).ToArray();

        private static void RemoveFormulaAt(PrototypeLevelConfig level, int row, int column) =>
            level.FormulaLayout = (level.FormulaLayout ?? Array.Empty<PrototypeFormulaPlacement>()).Where(x => x.Row != row || x.Column != column).ToArray();

        private static void RemoveSemanticToken(PrototypeLevelConfig level, PrototypePlacementKind kind, string recordId, string fieldId)
        {
            level.TokenLayout = (level.TokenLayout ?? Array.Empty<PrototypeTokenPlacement>()).Where(x =>
            {
                if (x.Kind != kind) return true;
                if (kind == PrototypePlacementKind.Data) return x.RecordId != recordId || x.FieldId != fieldId;
                if (kind == PrototypePlacementKind.RecordKey) return x.RecordId != recordId;
                if (kind == PrototypePlacementKind.FieldKey) return x.FieldId != fieldId;
                return true;
            }).ToArray();
        }

        private static void AddToken(PrototypeLevelConfig level, PrototypeTokenPlacement placement)
        {
            var list = (level.TokenLayout ?? Array.Empty<PrototypeTokenPlacement>()).ToList();
            list.Add(placement);
            level.TokenLayout = list.ToArray();
        }

        private static void RebuildGoalFlags(PrototypeLevelConfig level)
        {
            var flags = (PrototypeReportGoals)0;
            foreach (var p in level.GoalLayout ?? Array.Empty<PrototypeReportGoalPlacement>()) flags |= p.Goal;
            level.ReportGoals = flags;
        }

        private static double[] DatasetArray(PrototypeLevelDataset dataset, string fieldId) => fieldId switch
        {
            "hours" => dataset?.Hours,
            "salary" => dataset?.Salary,
            "overtime" => dataset?.Overtime,
            "bonus" => dataset?.Bonus,
            _ => null
        };

        private static string Address(int row, int column) => $"{ExcelHellPrototype.ColumnName(column)}{row + 1}";

        private static PrototypeLevelConfig CloneLevel(PrototypeLevelConfig source)
        {
            var clone = new PrototypeLevelConfig();
            CopyLevel(source, clone);
            return clone;
        }

        private static void CopyLevel(PrototypeLevelConfig source, PrototypeLevelConfig target)
        {
            target.Id = source.Id;
            target.NameRu = source.NameRu;
            target.NameEn = source.NameEn;
            target.Rows = source.Rows;
            target.Columns = source.Columns;
            target.ReportGoals = source.ReportGoals;
            target.RefEnabled = source.RefEnabled;
            target.FormulaCellsEnabled = source.FormulaCellsEnabled;
            target.MaxTurns = source.MaxTurns;
            target.FirstOutbreakTurn = source.FirstOutbreakTurn;
            target.RespawnDelayTurns = source.RespawnDelayTurns;
            target.ActiveOutbreakDelayTurns = source.ActiveOutbreakDelayTurns;
            target.DurationSeconds = source.DurationSeconds;
            target.AnomalyStepSeconds = source.AnomalyStepSeconds;
            target.FirstOutbreakDelaySeconds = source.FirstOutbreakDelaySeconds;
            target.RespawnDelaySeconds = source.RespawnDelaySeconds;
            target.ActiveOutbreakDelaySeconds = source.ActiveOutbreakDelaySeconds;
            target.CorruptionStepsBeforeDestroy = source.CorruptionStepsBeforeDestroy;
            target.SpawnPreferredDistance = source.SpawnPreferredDistance;
            target.SpawnDistanceVariation = source.SpawnDistanceVariation;
            target.SpawnCandidatePoolSize = source.SpawnCandidatePoolSize;
            target.Dataset = new PrototypeLevelDataset
            {
                Hours = (double[])source.Dataset.Hours.Clone(),
                Salary = (double[])source.Dataset.Salary.Clone(),
                Overtime = (double[])source.Dataset.Overtime.Clone(),
                Bonus = (double[])source.Dataset.Bonus.Clone()
            };
            target.TokenLayout = (source.TokenLayout ?? Array.Empty<PrototypeTokenPlacement>()).Select(x => new PrototypeTokenPlacement
            {
                Row = x.Row, Column = x.Column, Kind = x.Kind, RecordId = x.RecordId,
                FieldId = x.FieldId, TokenId = x.TokenId, StringId = x.StringId
            }).ToArray();
            target.FormulaLayout = (source.FormulaLayout ?? Array.Empty<PrototypeFormulaPlacement>()).Select(x => new PrototypeFormulaPlacement
            {
                Row = x.Row, Column = x.Column, Formula = x.Formula
            }).ToArray();
            target.GoalLayout = (source.GoalLayout ?? Array.Empty<PrototypeReportGoalPlacement>()).Select(x => new PrototypeReportGoalPlacement
            {
                Goal = x.Goal, Row = x.Row, Column = x.Column
            }).ToArray();
        }
    }
}
