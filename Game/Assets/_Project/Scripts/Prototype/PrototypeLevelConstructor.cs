using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ExcelHell.Prototype
{
    [DefaultExecutionOrder(1400)]
    public sealed class PrototypeLevelConstructor : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo SelectionField = typeof(ExcelHellPrototype).GetField("selection", Flags);
        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly string[] Records = { "ivanov", "petrov", "sidorov", "volkova", "kim" };
        private static readonly string[] Fields = { "hours", "salary", "overtime", "bonus" };
        private static readonly PrototypeReportGoals[] GoalKinds =
        {
            PrototypeReportGoals.SalaryTotal, PrototypeReportGoals.OvertimeTotal, PrototypeReportGoals.BonusTotal,
            PrototypeReportGoals.BonusAtLeastFour, PrototypeReportGoals.SalaryOfMaxOvertime, PrototypeReportGoals.SalaryForHoursBelowForty
        };

        private readonly Dictionary<int, PrototypeLevelConfig> cleanTemplates = new();
        private ExcelHellPrototype prototype;
        private bool visible = true;
        private bool rebuildPending;
        private Rect windowRect = new(1080f, 58f, 500f, 810f);
        private Vector2 scroll;
        private GUIStyle windowStyle;
        private Texture2D windowTexture;
        private int selectedRow = -1, selectedColumn = -1, recordIndex, fieldIndex, goalIndex;
        private string valueText = "", maxTurnsText = "", firstOutbreakText = "", respawnText = "", activeOutbreakText = "";
        private string status = "AUTHORING MODE: ходы и #REF! отключены";

        private void Awake()
        {
            PrototypeAuthoringMode.Active = true;
            for (var i = 0; i < PrototypeLevelCatalog.Count; i++) cleanTemplates[i] = CloneLevel(PrototypeLevelCatalog.Get(i));
            PullLevelFields();
        }

        private void OnDestroy()
        {
            if (windowTexture != null) Destroy(windowTexture);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame) visible = !visible;
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != null)
            {
                prototype = current;
                var selection = SelectionField?.GetValue(prototype) as List<CellModel>;
                if (selection != null && selection.Count == 1)
                {
                    selectedRow = selection[0].Row;
                    selectedColumn = selection[0].Column;
                    SyncValueText();
                }
            }
            if (rebuildPending && current == null)
            {
                rebuildPending = false;
                prototype = new GameObject("[GAMEPLAY] Worksheet Core").AddComponent<ExcelHellPrototype>();
            }
        }

        private void OnGUI()
        {
            if (!visible) return;
            EnsureWindowStyle();
            windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "EXEL HELL — LEVEL CONSTRUCTOR [F2]", windowStyle);
        }

        private void EnsureWindowStyle()
        {
            if (windowStyle != null) return;
            windowTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            windowTexture.SetPixel(0, 0, new Color(0.075f, 0.085f, 0.095f, 1f));
            windowTexture.Apply();
            windowStyle = new GUIStyle(GUI.skin.window) { normal = { background = windowTexture }, padding = new RectOffset(12, 12, 24, 12) };
        }

        private void DrawWindow(int id)
        {
            var level = PrototypeLevelRuntime.Current;
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("ШАБЛОН УРОВНЯ");
            GUILayout.BeginHorizontal();
            for (var i = 0; i < PrototypeLevelCatalog.Count; i++) { var index = i; if (GUILayout.Button($"L{i + 1}", GUILayout.Height(28))) LoadTemplate(index); }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("IMPORT FROM CLIPBOARD", GUILayout.Height(30))) ImportClipboard();
            GUILayout.Label($"{level.Id} — {level.NameRu}");
            GUILayout.Label("F2 скрывает панель; authoring mode на этой сцене остаётся активен.");
            GUILayout.Space(6);

            DrawCell(level);
            GUILayout.Space(8);
            DrawTokens(level);
            GUILayout.Space(8);
            DrawFormulas(level);
            GUILayout.Space(8);
            DrawGoals(level);
            GUILayout.Space(8);
            DrawParameters(level);
            GUILayout.Space(10);
            if (GUILayout.Button("EXPORT → CLIPBOARD", GUILayout.Height(36))) Export(level);
            if (GUILayout.Button("ПЕРЕСОБРАТЬ / REBUILD", GUILayout.Height(30))) { SyncBoardToConfig(level); RequestRebuild(); }
            GUILayout.Space(8);
            GUILayout.Label(status);
            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0, 0, 10000, 25));
        }

        private void DrawCell(PrototypeLevelConfig level)
        {
            GUILayout.Label("ВЫБРАННАЯ ЯЧЕЙКА");
            if (!HasSelected(level)) { GUILayout.Label("Кликни по ячейке таблицы."); return; }
            var token = FindToken(level, selectedRow, selectedColumn);
            var formula = FindFormula(level, selectedRow, selectedColumn);
            var goal = FindGoal(level, selectedRow, selectedColumn);
            GUILayout.Label($"{Address(selectedRow, selectedColumn)}   token: {Describe(token)}   formula: {(formula?.Formula.ToString() ?? "-")}   goal: {(goal?.Goal.ToString() ?? "-")}");
            if (GUILayout.Button("ОЧИСТИТЬ TOKEN + FORMULA", GUILayout.Height(28)))
            {
                SyncBoardToConfig(level); RemoveTokenAt(level, selectedRow, selectedColumn); RemoveFormulaAt(level, selectedRow, selectedColumn); RequestRebuild();
            }
        }

        private void DrawTokens(PrototypeLevelConfig level)
        {
            GUILayout.Label("КЛЮЧИ И ЗНАЧЕНИЯ");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Record: {Records[recordIndex]}")) recordIndex = (recordIndex + 1) % Records.Length;
            if (GUILayout.Button($"Field: {Fields[fieldIndex]}")) fieldIndex = (fieldIndex + 1) % Fields.Length;
            GUILayout.EndHorizontal();
            GUI.enabled = HasSelected(level);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("DATA")) PlaceToken(level, PrototypePlacementKind.Data);
            if (GUILayout.Button("RECORD KEY")) PlaceToken(level, PrototypePlacementKind.RecordKey);
            if (GUILayout.Button("FIELD KEY")) PlaceToken(level, PrototypePlacementKind.FieldKey);
            GUILayout.EndHorizontal();
            GUI.enabled = true;
            var token = HasSelected(level) ? FindToken(level, selectedRow, selectedColumn) : null;
            if (token?.Kind == PrototypePlacementKind.Data)
            {
                GUILayout.BeginHorizontal(); GUILayout.Label("Value", GUILayout.Width(52)); valueText = GUILayout.TextField(valueText, GUILayout.Width(110));
                if (GUILayout.Button("APPLY VALUE")) ApplyDataValue(level); GUILayout.EndHorizontal();
            }
        }

        private void DrawFormulas(PrototypeLevelConfig level)
        {
            GUILayout.Label("ФОРМУЛЬНЫЕ ЯЧЕЙКИ");
            GUI.enabled = HasSelected(level);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("=SORT()")) PlaceFormula(level, FormulaKind.Sort);
            if (GUILayout.Button("=SUM()")) PlaceFormula(level, FormulaKind.Sum);
            if (GUILayout.Button("NO FORMULA")) { SyncBoardToConfig(level); RemoveFormulaAt(level, selectedRow, selectedColumn); RequestRebuild(); }
            GUILayout.EndHorizontal(); GUI.enabled = true;
        }

        private void DrawGoals(PrototypeLevelConfig level)
        {
            GUILayout.Label("REPORT GOAL");
            if (GUILayout.Button($"Goal: {GoalKinds[goalIndex]}")) goalIndex = (goalIndex + 1) % GoalKinds.Length;
            GUI.enabled = HasSelected(level); GUILayout.BeginHorizontal();
            if (GUILayout.Button("ASSIGN GOAL")) { SyncBoardToConfig(level); var goal = GoalKinds[goalIndex]; var list = (level.GoalLayout ?? Array.Empty<PrototypeReportGoalPlacement>()).Where(x => x.Goal != goal && (x.Row != selectedRow || x.Column != selectedColumn)).ToList(); list.Add(new PrototypeReportGoalPlacement { Goal = goal, Row = selectedRow, Column = selectedColumn }); level.GoalLayout = list.ToArray(); RebuildGoalFlags(level); RequestRebuild(); }
            if (GUILayout.Button("REMOVE GOAL")) { SyncBoardToConfig(level); level.GoalLayout = (level.GoalLayout ?? Array.Empty<PrototypeReportGoalPlacement>()).Where(x => x.Row != selectedRow || x.Column != selectedColumn).ToArray(); RebuildGoalFlags(level); RequestRebuild(); }
            GUILayout.EndHorizontal(); GUI.enabled = true;
        }

        private void DrawParameters(PrototypeLevelConfig level)
        {
            GUILayout.Label("ПАРАМЕТРЫ ЭКСПОРТИРУЕМОГО УРОВНЯ");
            level.RefEnabled = GUILayout.Toggle(level.RefEnabled, "#REF! enabled в реальной игре");
            DrawInt("Max turns", ref maxTurnsText, v => level.MaxTurns = Mathf.Max(1, v));
            DrawInt("First outbreak", ref firstOutbreakText, v => level.FirstOutbreakTurn = Mathf.Max(1, v));
            DrawInt("Respawn delay", ref respawnText, v => level.RespawnDelayTurns = Mathf.Max(1, v));
            DrawInt("Active delay", ref activeOutbreakText, v => level.ActiveOutbreakDelayTurns = Mathf.Max(1, v));
        }

        private static void DrawInt(string label, ref string text, Action<int> setter)
        {
            GUILayout.BeginHorizontal(); GUILayout.Label(label, GUILayout.Width(120)); text = GUILayout.TextField(text, GUILayout.Width(80));
            if (GUILayout.Button("SET", GUILayout.Width(54)) && int.TryParse(text, out var value)) setter(value); GUILayout.EndHorizontal();
        }

        private void PlaceToken(PrototypeLevelConfig level, PrototypePlacementKind kind)
        {
            if (!HasSelected(level)) return;
            SyncBoardToConfig(level); RemoveFormulaAt(level, selectedRow, selectedColumn); RemoveTokenAt(level, selectedRow, selectedColumn);
            if (kind == PrototypePlacementKind.Data) RemoveSemanticToken(level, kind, Records[recordIndex], Fields[fieldIndex]);
            else if (kind == PrototypePlacementKind.RecordKey) RemoveSemanticToken(level, kind, Records[recordIndex], null);
            else RemoveSemanticToken(level, kind, null, Fields[fieldIndex]);
            AddToken(level, new PrototypeTokenPlacement { Row = selectedRow, Column = selectedColumn, Kind = kind, RecordId = kind == PrototypePlacementKind.FieldKey ? null : Records[recordIndex], FieldId = kind == PrototypePlacementKind.RecordKey ? null : Fields[fieldIndex] });
            RequestRebuild();
        }

        private void PlaceFormula(PrototypeLevelConfig level, FormulaKind kind)
        {
            if (!HasSelected(level)) return;
            SyncBoardToConfig(level); RemoveTokenAt(level, selectedRow, selectedColumn); RemoveFormulaAt(level, selectedRow, selectedColumn);
            var list = (level.FormulaLayout ?? Array.Empty<PrototypeFormulaPlacement>()).ToList(); list.Add(new PrototypeFormulaPlacement { Row = selectedRow, Column = selectedColumn, Formula = kind }); level.FormulaLayout = list.ToArray(); RequestRebuild();
        }

        private void ApplyDataValue(PrototypeLevelConfig level)
        {
            SyncBoardToConfig(level); var token = FindToken(level, selectedRow, selectedColumn); if (token?.Kind != PrototypePlacementKind.Data) return;
            if (!double.TryParse(valueText.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) { status = "Некорректное значение"; return; }
            var record = Array.IndexOf(Records, token.RecordId); var data = DatasetArray(level.Dataset, token.FieldId); if (record < 0 || data == null) return; data[record] = value; RequestRebuild();
        }

        private void LoadTemplate(int index)
        {
            PrototypeLevelRuntime.SetCurrentIndex(index); CopyLevel(cleanTemplates[index], PrototypeLevelCatalog.Get(index)); selectedRow = selectedColumn = -1; PullLevelFields(); status = $"Загружен L{index + 1}"; RequestRebuild();
        }

        private void SyncBoardToConfig(PrototypeLevelConfig level)
        {
            if (prototype == null) return;
            var cells = CellsField?.GetValue(prototype) as CellModel[,]; if (cells == null) return;
            var tokens = new List<PrototypeTokenPlacement>(); var formulas = new List<PrototypeFormulaPlacement>();
            foreach (var cell in cells)
            {
                if (cell.State != CellState.Normal) continue;
                if (cell.Formula != FormulaKind.None) formulas.Add(new PrototypeFormulaPlacement { Row = cell.Row, Column = cell.Column, Formula = cell.Formula });
                var t = cell.Occupant; if (t == null || t.Kind == ContentKind.Aggregate) continue;
                PrototypeTokenPlacement p = t.Kind switch
                {
                    ContentKind.Data => new PrototypeTokenPlacement { Row = cell.Row, Column = cell.Column, Kind = PrototypePlacementKind.Data, RecordId = t.RecordId, FieldId = t.FieldId },
                    ContentKind.RecordKey => new PrototypeTokenPlacement { Row = cell.Row, Column = cell.Column, Kind = PrototypePlacementKind.RecordKey, RecordId = t.RecordId },
                    ContentKind.FieldKey => new PrototypeTokenPlacement { Row = cell.Row, Column = cell.Column, Kind = PrototypePlacementKind.FieldKey, FieldId = t.FieldId },
                    ContentKind.Label => new PrototypeTokenPlacement { Row = cell.Row, Column = cell.Column, Kind = PrototypePlacementKind.Label, TokenId = t.Id, StringId = t.StringId }, _ => null
                };
                if (p != null) tokens.Add(p);
            }
            level.TokenLayout = tokens.ToArray(); level.FormulaLayout = formulas.ToArray();
        }

        private void Export(PrototypeLevelConfig level)
        {
            SyncBoardToConfig(level); var sb = new StringBuilder();
            sb.AppendLine("new PrototypeLevelConfig"); sb.AppendLine("{");
            sb.AppendLine($"    Id = \"{Escape(level.Id)}_edit\","); sb.AppendLine($"    NameRu = \"{Escape(level.NameRu)}\","); sb.AppendLine($"    NameEn = \"{Escape(level.NameEn)}\",");
            sb.AppendLine($"    Rows = {level.Rows}, Columns = {level.Columns},"); sb.AppendLine($"    ReportGoals = (PrototypeReportGoals){(int)level.ReportGoals},");
            sb.AppendLine($"    RefEnabled = {level.RefEnabled.ToString().ToLowerInvariant()}, FormulaCellsEnabled = true,");
            sb.AppendLine($"    MaxTurns = {level.MaxTurns}, FirstOutbreakTurn = {level.FirstOutbreakTurn}, RespawnDelayTurns = {level.RespawnDelayTurns}, ActiveOutbreakDelayTurns = {level.ActiveOutbreakDelayTurns},");
            sb.AppendLine($"    CorruptionStepsBeforeDestroy = {level.CorruptionStepsBeforeDestroy}, SpawnPreferredDistance = {level.SpawnPreferredDistance}, SpawnDistanceVariation = {level.SpawnDistanceVariation}, SpawnCandidatePoolSize = {level.SpawnCandidatePoolSize},");
            sb.AppendLine($"    Dataset = Dataset({ArrayCode(level.Dataset.Hours)}, {ArrayCode(level.Dataset.Salary)}, {ArrayCode(level.Dataset.Overtime)}, {ArrayCode(level.Dataset.Bonus)}),");
            sb.AppendLine("    TokenLayout = new[]\n    {"); foreach (var p in level.TokenLayout.OrderBy(x => x.Row).ThenBy(x => x.Column)) sb.AppendLine($"        {TokenCode(p)},"); sb.AppendLine("    },");
            sb.AppendLine("    FormulaLayout = new[]\n    {"); foreach (var p in level.FormulaLayout.OrderBy(x => x.Row).ThenBy(x => x.Column)) sb.AppendLine($"        Formula(\"{Address(p.Row, p.Column)}\", FormulaKind.{p.Formula}),"); sb.AppendLine("    },");
            sb.AppendLine("    GoalLayout = new[]\n    {"); foreach (var p in level.GoalLayout.OrderBy(x => x.Row).ThenBy(x => x.Column)) sb.AppendLine($"        Goal(\"{Address(p.Row, p.Column)}\", PrototypeReportGoals.{p.Goal}),"); sb.AppendLine("    }\n};");
            GUIUtility.systemCopyBuffer = sb.ToString(); status = $"Экспорт: {level.TokenLayout.Length} tokens / {level.FormulaLayout.Length} formulas";
        }

        private void ImportClipboard()
        {
            if (!TryParseExport(GUIUtility.systemCopyBuffer ?? "", out var imported, out var error)) { status = "IMPORT ERROR: " + error; return; }
            CopyLevel(imported, PrototypeLevelCatalog.Get(PrototypeLevelRuntime.CurrentIndex)); selectedRow = selectedColumn = -1; PullLevelFields(); status = "Импортирован " + imported.Id; RequestRebuild();
        }

        private static bool TryParseExport(string text, out PrototypeLevelConfig level, out string error)
        {
            level = null; error = null;
            if (!text.Contains("new PrototypeLevelConfig")) { error = "в буфере нет PrototypeLevelConfig"; return false; }
            try
            {
                var p = new PrototypeLevelConfig
                {
                    Id = CaptureString(text, "Id"), NameRu = CaptureString(text, "NameRu"), NameEn = CaptureString(text, "NameEn"),
                    Rows = CaptureInt(text, "Rows", 8), Columns = CaptureInt(text, "Columns", 8), ReportGoals = (PrototypeReportGoals)CaptureRegexInt(text, "ReportGoals\\s*=\\s*\\(PrototypeReportGoals\\)(\\d+)", 0),
                    RefEnabled = CaptureBool(text, "RefEnabled", true), FormulaCellsEnabled = true, MaxTurns = CaptureInt(text, "MaxTurns", 15), FirstOutbreakTurn = CaptureInt(text, "FirstOutbreakTurn", 3), RespawnDelayTurns = CaptureInt(text, "RespawnDelayTurns", 2), ActiveOutbreakDelayTurns = CaptureInt(text, "ActiveOutbreakDelayTurns", 3), CorruptionStepsBeforeDestroy = CaptureInt(text, "CorruptionStepsBeforeDestroy", 2), SpawnPreferredDistance = CaptureInt(text, "SpawnPreferredDistance", 2), SpawnDistanceVariation = CaptureInt(text, "SpawnDistanceVariation", 1), SpawnCandidatePoolSize = CaptureInt(text, "SpawnCandidatePoolSize", 4)
                };
                var d = Regex.Match(text, "Dataset\\s*=\\s*Dataset\\((new\\[\\]\\s*\\{[^}]*\\}),\\s*(new\\[\\]\\s*\\{[^}]*\\}),\\s*(new\\[\\]\\s*\\{[^}]*\\}),\\s*(new\\[\\]\\s*\\{[^}]*\\})\\)", RegexOptions.Singleline);
                if (!d.Success) throw new FormatException("Dataset(...) не найден");
                p.Dataset = new PrototypeLevelDataset { Hours = ParseArray(d.Groups[1].Value), Salary = ParseArray(d.Groups[2].Value), Overtime = ParseArray(d.Groups[3].Value), Bonus = ParseArray(d.Groups[4].Value) };
                var tokens = new List<PrototypeTokenPlacement>();
                foreach (Match m in Regex.Matches(text, "Data\\(\"([A-Z]+\\d+)\",\\s*\"([^\"]+)\",\\s*\"([^\"]+)\"\\)")) { ParseAddress(m.Groups[1].Value, out var r, out var c); tokens.Add(new PrototypeTokenPlacement { Row = r, Column = c, Kind = PrototypePlacementKind.Data, RecordId = m.Groups[2].Value, FieldId = m.Groups[3].Value }); }
                foreach (Match m in Regex.Matches(text, "Record\\(\"([A-Z]+\\d+)\",\\s*\"([^\"]+)\"\\)")) { ParseAddress(m.Groups[1].Value, out var r, out var c); tokens.Add(new PrototypeTokenPlacement { Row = r, Column = c, Kind = PrototypePlacementKind.RecordKey, RecordId = m.Groups[2].Value }); }
                foreach (Match m in Regex.Matches(text, "Field\\(\"([A-Z]+\\d+)\",\\s*\"([^\"]+)\"\\)")) { ParseAddress(m.Groups[1].Value, out var r, out var c); tokens.Add(new PrototypeTokenPlacement { Row = r, Column = c, Kind = PrototypePlacementKind.FieldKey, FieldId = m.Groups[2].Value }); }
                foreach (Match m in Regex.Matches(text, "Label\\(\"([A-Z]+\\d+)\"\\)")) { ParseAddress(m.Groups[1].Value, out var r, out var c); tokens.Add(new PrototypeTokenPlacement { Row = r, Column = c, Kind = PrototypePlacementKind.Label, TokenId = "report.label", StringId = "label.report" }); }
                p.TokenLayout = tokens.ToArray();
                var formulas = new List<PrototypeFormulaPlacement>(); foreach (Match m in Regex.Matches(text, "Formula\\(\"([A-Z]+\\d+)\",\\s*FormulaKind\\.(Sum|Sort)\\)")) { ParseAddress(m.Groups[1].Value, out var r, out var c); formulas.Add(new PrototypeFormulaPlacement { Row = r, Column = c, Formula = (FormulaKind)Enum.Parse(typeof(FormulaKind), m.Groups[2].Value) }); } p.FormulaLayout = formulas.ToArray();
                var goals = new List<PrototypeReportGoalPlacement>(); foreach (Match m in Regex.Matches(text, "Goal\\(\"([A-Z]+\\d+)\",\\s*PrototypeReportGoals\\.([A-Za-z0-9_]+)\\)")) { ParseAddress(m.Groups[1].Value, out var r, out var c); goals.Add(new PrototypeReportGoalPlacement { Row = r, Column = c, Goal = (PrototypeReportGoals)Enum.Parse(typeof(PrototypeReportGoals), m.Groups[2].Value) }); } p.GoalLayout = goals.ToArray(); RebuildGoalFlags(p);
                level = p; return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        private static string CaptureString(string text, string field) { var m = Regex.Match(text, field + "\\s*=\\s*\"([^\"]*)\""); return m.Success ? m.Groups[1].Value : field.ToLowerInvariant(); }
        private static int CaptureInt(string text, string field, int fallback) => CaptureRegexInt(text, field + "\\s*=\\s*(\\d+)", fallback);
        private static int CaptureRegexInt(string text, string pattern, int fallback) { var m = Regex.Match(text, pattern); return m.Success && int.TryParse(m.Groups[1].Value, out var v) ? v : fallback; }
        private static bool CaptureBool(string text, string field, bool fallback) { var m = Regex.Match(text, field + "\\s*=\\s*(true|false)", RegexOptions.IgnoreCase); return m.Success ? bool.Parse(m.Groups[1].Value) : fallback; }
        private static double[] ParseArray(string source) => Regex.Matches(source, "-?\\d+(?:\\.\\d+)?d?").Cast<Match>().Select(x => double.Parse(x.Value.TrimEnd('d'), CultureInfo.InvariantCulture)).ToArray();
        private static string ArrayCode(double[] values) => "new[] { " + string.Join(", ", values.Select(v => v.ToString("0.###", CultureInfo.InvariantCulture) + "d")) + " }";
        private static string Escape(string value) => (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        private static string TokenCode(PrototypeTokenPlacement p) => p.Kind switch { PrototypePlacementKind.Data => $"Data(\"{Address(p.Row, p.Column)}\", \"{p.RecordId}\", \"{p.FieldId}\")", PrototypePlacementKind.RecordKey => $"Record(\"{Address(p.Row, p.Column)}\", \"{p.RecordId}\")", PrototypePlacementKind.FieldKey => $"Field(\"{Address(p.Row, p.Column)}\", \"{p.FieldId}\")", _ => $"Label(\"{Address(p.Row, p.Column)}\")" };

        private void RequestRebuild() { if (rebuildPending) return; var old = FindFirstObjectByType<ExcelHellPrototype>(); if (old != null) Destroy(old.gameObject); prototype = null; rebuildPending = true; }
        private void PullLevelFields() { var l = PrototypeLevelRuntime.Current; maxTurnsText = l.MaxTurns.ToString(); firstOutbreakText = l.FirstOutbreakTurn.ToString(); respawnText = l.RespawnDelayTurns.ToString(); activeOutbreakText = l.ActiveOutbreakDelayTurns.ToString(); }
        private void SyncValueText() { var t = FindToken(PrototypeLevelRuntime.Current, selectedRow, selectedColumn); if (t?.Kind != PrototypePlacementKind.Data) return; var i = Array.IndexOf(Records, t.RecordId); var a = DatasetArray(PrototypeLevelRuntime.Current.Dataset, t.FieldId); if (i >= 0 && a != null) valueText = a[i].ToString(CultureInfo.InvariantCulture); }
        private bool HasSelected(PrototypeLevelConfig l) => l != null && selectedRow >= 0 && selectedColumn >= 0 && selectedRow < l.Rows && selectedColumn < l.Columns;
        private static PrototypeTokenPlacement FindToken(PrototypeLevelConfig l, int r, int c) => (l.TokenLayout ?? Array.Empty<PrototypeTokenPlacement>()).FirstOrDefault(x => x.Row == r && x.Column == c);
        private static PrototypeFormulaPlacement FindFormula(PrototypeLevelConfig l, int r, int c) => (l.FormulaLayout ?? Array.Empty<PrototypeFormulaPlacement>()).FirstOrDefault(x => x.Row == r && x.Column == c);
        private static PrototypeReportGoalPlacement FindGoal(PrototypeLevelConfig l, int r, int c) => (l.GoalLayout ?? Array.Empty<PrototypeReportGoalPlacement>()).FirstOrDefault(x => x.Row == r && x.Column == c);
        private static string Describe(PrototypeTokenPlacement p) => p == null ? "-" : p.Kind == PrototypePlacementKind.Data ? $"{p.RecordId}.{p.FieldId}" : p.Kind == PrototypePlacementKind.RecordKey ? $"record:{p.RecordId}" : p.Kind == PrototypePlacementKind.FieldKey ? $"field:{p.FieldId}" : "label";
        private static void RemoveTokenAt(PrototypeLevelConfig l, int r, int c) => l.TokenLayout = (l.TokenLayout ?? Array.Empty<PrototypeTokenPlacement>()).Where(x => x.Row != r || x.Column != c).ToArray();
        private static void RemoveFormulaAt(PrototypeLevelConfig l, int r, int c) => l.FormulaLayout = (l.FormulaLayout ?? Array.Empty<PrototypeFormulaPlacement>()).Where(x => x.Row != r || x.Column != c).ToArray();
        private static void RemoveSemanticToken(PrototypeLevelConfig l, PrototypePlacementKind k, string r, string f) => l.TokenLayout = (l.TokenLayout ?? Array.Empty<PrototypeTokenPlacement>()).Where(x => x.Kind != k || (k == PrototypePlacementKind.Data ? x.RecordId != r || x.FieldId != f : k == PrototypePlacementKind.RecordKey ? x.RecordId != r : x.FieldId != f)).ToArray();
        private static void AddToken(PrototypeLevelConfig l, PrototypeTokenPlacement p) { var a = (l.TokenLayout ?? Array.Empty<PrototypeTokenPlacement>()).ToList(); a.Add(p); l.TokenLayout = a.ToArray(); }
        private static void RebuildGoalFlags(PrototypeLevelConfig l) { l.ReportGoals = (PrototypeReportGoals)0; foreach (var g in l.GoalLayout ?? Array.Empty<PrototypeReportGoalPlacement>()) l.ReportGoals |= g.Goal; }
        private static double[] DatasetArray(PrototypeLevelDataset d, string f) => f == "hours" ? d.Hours : f == "salary" ? d.Salary : f == "overtime" ? d.Overtime : d.Bonus;
        private static string Address(int r, int c) => $"{ExcelHellPrototype.ColumnName(c)}{r + 1}";
        private static void ParseAddress(string s, out int r, out int c) { var m = Regex.Match(s, "^([A-Z]+)(\\d+)$", RegexOptions.IgnoreCase); if (!m.Success) throw new FormatException("Invalid address: " + s); var n = 0; foreach (var ch in m.Groups[1].Value.ToUpperInvariant()) n = n * 26 + ch - 'A' + 1; c = n - 1; r = int.Parse(m.Groups[2].Value) - 1; }
        private static PrototypeLevelConfig CloneLevel(PrototypeLevelConfig s) { var c = new PrototypeLevelConfig(); CopyLevel(s, c); return c; }
        private static void CopyLevel(PrototypeLevelConfig s, PrototypeLevelConfig t)
        {
            t.Id=s.Id;t.NameRu=s.NameRu;t.NameEn=s.NameEn;t.Rows=s.Rows;t.Columns=s.Columns;t.ReportGoals=s.ReportGoals;t.RefEnabled=s.RefEnabled;t.FormulaCellsEnabled=s.FormulaCellsEnabled;t.MaxTurns=s.MaxTurns;t.FirstOutbreakTurn=s.FirstOutbreakTurn;t.RespawnDelayTurns=s.RespawnDelayTurns;t.ActiveOutbreakDelayTurns=s.ActiveOutbreakDelayTurns;t.CorruptionStepsBeforeDestroy=s.CorruptionStepsBeforeDestroy;t.SpawnPreferredDistance=s.SpawnPreferredDistance;t.SpawnDistanceVariation=s.SpawnDistanceVariation;t.SpawnCandidatePoolSize=s.SpawnCandidatePoolSize;
            t.Dataset=new PrototypeLevelDataset{Hours=(double[])s.Dataset.Hours.Clone(),Salary=(double[])s.Dataset.Salary.Clone(),Overtime=(double[])s.Dataset.Overtime.Clone(),Bonus=(double[])s.Dataset.Bonus.Clone()};
            t.TokenLayout=(s.TokenLayout??Array.Empty<PrototypeTokenPlacement>()).Select(x=>new PrototypeTokenPlacement{Row=x.Row,Column=x.Column,Kind=x.Kind,RecordId=x.RecordId,FieldId=x.FieldId,TokenId=x.TokenId,StringId=x.StringId}).ToArray();
            t.FormulaLayout=(s.FormulaLayout??Array.Empty<PrototypeFormulaPlacement>()).Select(x=>new PrototypeFormulaPlacement{Row=x.Row,Column=x.Column,Formula=x.Formula}).ToArray();
            t.GoalLayout=(s.GoalLayout??Array.Empty<PrototypeReportGoalPlacement>()).Select(x=>new PrototypeReportGoalPlacement{Goal=x.Goal,Row=x.Row,Column=x.Column}).ToArray();
        }
    }
}
