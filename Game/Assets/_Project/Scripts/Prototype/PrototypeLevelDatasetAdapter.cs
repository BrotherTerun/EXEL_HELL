using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ExcelHell.Prototype
{
    [DefaultExecutionOrder(600)]
    public sealed class PrototypeLevelDatasetAdapter : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly FieldInfo GoalsField = typeof(ExcelHellPrototype).GetField("goals", Flags);
        private static readonly FieldInfo RequiredForPlayField = typeof(ExcelHellPrototype).GetField("requiredForPlay", Flags);
        private static readonly FieldInfo ReservedCellsField = typeof(ExcelHellPrototype).GetField("reservedCells", Flags);
        private static readonly MethodInfo RefreshAllMethod = typeof(ExcelHellPrototype).GetMethod("RefreshAll", Flags);
        private static readonly MethodInfo InitializeAnomalyMethod = typeof(ExcelHellPrototype).GetMethod("InitializeAnomaly", Flags);

        private ExcelHellPrototype appliedTo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PrototypeLevelDatasetAdapter>() != null) return;
            var adapter = new GameObject("EXEL HELL Level Dataset Adapter").AddComponent<PrototypeLevelDatasetAdapter>();
            DontDestroyOnLoad(adapter.gameObject);
        }

        private void LateUpdate()
        {
            var prototype = FindFirstObjectByType<ExcelHellPrototype>();
            if (prototype == null || prototype == appliedTo) return;
            Apply(prototype, PrototypeLevelRuntime.Current);
            appliedTo = prototype;
        }

        private static void Apply(ExcelHellPrototype prototype, PrototypeLevelConfig level)
        {
            if (level?.Dataset == null) return;
            if (level.FormulaCellsEnabled) ApplyAuthoredFormulaLayout(prototype, level);
            else ApplyDatasetOnly(prototype, level.Dataset);
        }

        private static void ApplyAuthoredFormulaLayout(ExcelHellPrototype prototype, PrototypeLevelConfig level)
        {
            var cells = CellsField?.GetValue(prototype) as CellModel[,];
            var goals = GoalsField?.GetValue(prototype) as List<ReportGoal>;
            var requiredForPlay = RequiredForPlayField?.GetValue(prototype) as HashSet<string>;
            var reservedCells = ReservedCellsField?.GetValue(prototype) as HashSet<(int Row, int Column)>;
            if (cells == null || goals == null || requiredForPlay == null || reservedCells == null) return;

            foreach (var cell in cells)
            {
                cell.Occupant = null;
                cell.Formula = FormulaKind.None;
                cell.State = CellState.Normal;
                cell.CorruptionAge = 0;
            }

            goals.Clear();
            requiredForPlay.Clear();
            reservedCells.Clear();

            foreach (var placement in level.GoalLayout ?? Array.Empty<PrototypeReportGoalPlacement>())
            {
                var goal = BuildGoal(placement, level.Dataset);
                goals.Add(goal);
                reservedCells.Add((goal.TargetRow, goal.TargetColumn));
                foreach (var sourceId in goal.ExpectedSourceIds) requiredForPlay.Add(sourceId);
                if (!string.IsNullOrEmpty(goal.ExpectedDirectTokenId)) requiredForPlay.Add(goal.ExpectedDirectTokenId);
            }

            // Semantic lookup data remain critical even when they are not part of the arithmetic result.
            if ((level.ReportGoals & PrototypeReportGoals.SalaryForHoursBelowForty) != 0)
                foreach (var record in Records) requiredForPlay.Add(DataId(record, "hours"));
            if ((level.ReportGoals & PrototypeReportGoals.SalaryOfMaxOvertime) != 0)
                foreach (var record in Records) requiredForPlay.Add(DataId(record, "overtime"));

            foreach (var placement in level.TokenLayout ?? Array.Empty<PrototypeTokenPlacement>())
            {
                ValidateCoordinate(cells, placement.Row, placement.Column, level.Id);
                var target = cells[placement.Row, placement.Column];
                if (target.Occupant != null)
                    throw new InvalidOperationException($"Level {level.Id}: duplicate token placement at {target.Address}.");
                target.Occupant = BuildToken(placement, level.Dataset, requiredForPlay);
            }

            foreach (var placement in level.FormulaLayout ?? Array.Empty<PrototypeFormulaPlacement>())
            {
                ValidateCoordinate(cells, placement.Row, placement.Column, level.Id);
                var target = cells[placement.Row, placement.Column];
                if (target.Occupant != null)
                    throw new InvalidOperationException($"Level {level.Id}: formula {placement.Formula} overlaps token at {target.Address}.");
                PrototypeFormulaCells.AssignFormula(target, placement.Formula);
            }

            foreach (var goal in goals)
            {
                var target = cells[goal.TargetRow, goal.TargetColumn];

                // Report goals intentionally support two aggregate workflows:
                // 1) report SUM cell: compute directly in the report;
                // 2) plain ReportCell: compute elsewhere, then MOVE the aggregate into the report.
                // This lets level authors experiment with formula scarcity without fighting the adapter.
                if (string.IsNullOrEmpty(goal.ExpectedDirectTokenId))
                {
                    if (target.Formula == FormulaKind.Sort)
                        Debug.LogWarning($"EXEL HELL level {level.Id}: aggregate report target {target.Address} cannot use SORT formula; use SUM or a plain delivery ReportCell.");
                }
                else if (target.Formula != FormulaKind.None)
                {
                    Debug.LogWarning($"EXEL HELL level {level.Id}: direct-value report target {target.Address} should be a plain ReportCell.");
                }
            }

            InitializeAnomalyMethod?.Invoke(prototype, null);
            RefreshAllMethod?.Invoke(prototype, null);
        }

        private static ReportGoal BuildGoal(PrototypeReportGoalPlacement placement, PrototypeLevelDataset dataset)
        {
            return new ReportGoal(
                GoalStringId(placement.Goal),
                Expected(GoalStringId(placement.Goal), dataset),
                placement.Row,
                placement.Column,
                ExpectedSources(placement.Goal, dataset),
                ExpectedDirectToken(placement.Goal, dataset));
        }

        private static ContentToken BuildToken(PrototypeTokenPlacement placement, PrototypeLevelDataset dataset, HashSet<string> requiredForPlay)
        {
            switch (placement.Kind)
            {
                case PrototypePlacementKind.Data:
                {
                    var index = RecordIndex(placement.RecordId);
                    var id = DataId(placement.RecordId, placement.FieldId);
                    return ContentToken.Data(id, placement.RecordId, placement.FieldId,
                        dataset.Value(placement.FieldId, index), requiredForPlay.Contains(id));
                }
                case PrototypePlacementKind.RecordKey:
                    return ContentToken.RecordKey(placement.RecordId);
                case PrototypePlacementKind.FieldKey:
                    return ContentToken.FieldKey(placement.FieldId);
                case PrototypePlacementKind.Label:
                    return ContentToken.Label(
                        string.IsNullOrEmpty(placement.TokenId) ? "label" : placement.TokenId,
                        string.IsNullOrEmpty(placement.StringId) ? "label.report" : placement.StringId);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void ApplyDatasetOnly(ExcelHellPrototype prototype, PrototypeLevelDataset dataset)
        {
            var cells = CellsField?.GetValue(prototype) as CellModel[,];
            var goals = GoalsField?.GetValue(prototype) as List<ReportGoal>;
            if (cells == null || goals == null) return;

            foreach (var cell in cells)
            {
                var token = cell.Occupant;
                if (token?.Kind != ContentKind.Data || string.IsNullOrEmpty(token.RecordId) || string.IsNullOrEmpty(token.FieldId)) continue;
                token.Number = dataset.Value(token.FieldId, RecordIndex(token.RecordId));
            }

            var rebuilt = goals.Select(goal => new ReportGoal(
                goal.NameStringId,
                Expected(goal.NameStringId, dataset),
                goal.TargetRow,
                goal.TargetColumn,
                goal.ExpectedSourceIds,
                goal.ExpectedDirectTokenId)).ToList();
            goals.Clear();
            goals.AddRange(rebuilt);
            RefreshAllMethod?.Invoke(prototype, null);
        }

        private static string GoalStringId(PrototypeReportGoals goal) => goal switch
        {
            PrototypeReportGoals.SalaryTotal => "goal.salary",
            PrototypeReportGoals.OvertimeTotal => "goal.overtime",
            PrototypeReportGoals.BonusTotal => "goal.bonus",
            PrototypeReportGoals.BonusAtLeastFour => "goal.bonus5",
            PrototypeReportGoals.SalaryOfMaxOvertime => "goal.maxOvertimeSalary",
            PrototypeReportGoals.SalaryForHoursBelowForty => "goal.lowHoursSalary",
            _ => throw new ArgumentOutOfRangeException(nameof(goal), goal, "Goal placement must contain one goal flag.")
        };

        private static IEnumerable<string> ExpectedSources(PrototypeReportGoals goal, PrototypeLevelDataset dataset)
        {
            switch (goal)
            {
                case PrototypeReportGoals.SalaryTotal:
                    return Records.Select(record => DataId(record, "salary"));
                case PrototypeReportGoals.OvertimeTotal:
                    return Records.Select(record => DataId(record, "overtime"));
                case PrototypeReportGoals.BonusTotal:
                    return Records.Select(record => DataId(record, "bonus"));
                case PrototypeReportGoals.BonusAtLeastFour:
                    return Records.Where((record, index) => dataset.Bonus[index] >= 5d).Select(record => DataId(record, "bonus"));
                case PrototypeReportGoals.SalaryOfMaxOvertime:
                    return new[] { DataId(Records[MaxIndex(dataset.Overtime)], "salary") };
                case PrototypeReportGoals.SalaryForHoursBelowForty:
                    return Records.Where((record, index) => dataset.Hours[index] < 40d).Select(record => DataId(record, "salary"));
                default:
                    return Array.Empty<string>();
            }
        }

        private static string ExpectedDirectToken(PrototypeReportGoals goal, PrototypeLevelDataset dataset) =>
            goal == PrototypeReportGoals.SalaryOfMaxOvertime ? DataId(Records[MaxIndex(dataset.Overtime)], "salary") : null;

        private static double Expected(string goalStringId, PrototypeLevelDataset dataset)
        {
            switch (goalStringId)
            {
                case "goal.salary": return dataset.Salary.Sum();
                case "goal.overtime": return dataset.Overtime.Sum();
                case "goal.bonus": return dataset.Bonus.Sum();
                case "goal.bonus5": return dataset.Bonus.Where(value => value >= 5d).Sum();
                case "goal.maxOvertimeSalary": return dataset.Salary[MaxIndex(dataset.Overtime)];
                case "goal.lowHoursSalary":
                {
                    var total = 0d;
                    for (var i = 0; i < dataset.Hours.Length && i < dataset.Salary.Length; i++)
                        if (dataset.Hours[i] < 40d) total += dataset.Salary[i];
                    return total;
                }
                default: return 0d;
            }
        }

        private static int MaxIndex(double[] values)
        {
            var maxIndex = 0;
            for (var i = 1; i < values.Length; i++) if (values[i] > values[maxIndex]) maxIndex = i;
            return maxIndex;
        }

        private static int RecordIndex(string recordId)
        {
            for (var i = 0; i < Records.Length; i++) if (Records[i] == recordId) return i;
            throw new ArgumentOutOfRangeException(nameof(recordId), recordId, "Unknown prototype record.");
        }

        private static void ValidateCoordinate(CellModel[,] cells, int row, int column, string levelId)
        {
            if (row < 0 || column < 0 || row >= cells.GetLength(0) || column >= cells.GetLength(1))
                throw new InvalidOperationException($"Level {levelId}: placement ({row},{column}) is outside board bounds.");
        }

        private static string DataId(string recordId, string fieldId) => $"data.{recordId}.{fieldId}";
        private static readonly string[] Records = { "ivanov", "petrov", "sidorov", "volkova", "kim" };
    }
}
