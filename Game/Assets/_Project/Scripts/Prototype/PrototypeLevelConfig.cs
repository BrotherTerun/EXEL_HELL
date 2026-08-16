using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExcelHell.Prototype
{
    [Serializable]
    public sealed class PrototypeLevelDataset
    {
        public double[] Hours;
        public double[] Salary;
        public double[] Overtime;
        public double[] Bonus;

        public double Value(string fieldId, int recordIndex)
        {
            var source = fieldId switch
            {
                "hours" => Hours,
                "salary" => Salary,
                "overtime" => Overtime,
                "bonus" => Bonus,
                _ => null
            };

            if (source == null || recordIndex < 0 || recordIndex >= source.Length)
                throw new ArgumentOutOfRangeException(nameof(recordIndex), $"Dataset has no value for {fieldId}[{recordIndex}].");
            return source[recordIndex];
        }
    }

    public enum PrototypePlacementKind { Data, RecordKey, FieldKey, Label }

    [Serializable]
    public sealed class PrototypeTokenPlacement
    {
        public int Row;
        public int Column;
        public PrototypePlacementKind Kind;
        public string RecordId;
        public string FieldId;
        public string TokenId;
        public string StringId;
    }

    [Serializable]
    public sealed class PrototypeFormulaPlacement
    {
        public int Row;
        public int Column;
        public FormulaKind Formula;
    }

    [Serializable]
    public sealed class PrototypeReportGoalPlacement
    {
        public PrototypeReportGoals Goal;
        public int Row;
        public int Column;
    }

    [Serializable]
    public sealed class PrototypeLevelConfig
    {
        public string Id;
        public string NameRu;
        public string NameEn;
        public int Rows = 8;
        public int Columns = 8;
        public PrototypeReportGoals ReportGoals;
        public bool RefEnabled = true;
        public bool FormulaCellsEnabled;
        public PrototypeLevelDataset Dataset;
        public PrototypeTokenPlacement[] TokenLayout = Array.Empty<PrototypeTokenPlacement>();
        public PrototypeFormulaPlacement[] FormulaLayout = Array.Empty<PrototypeFormulaPlacement>();
        public PrototypeReportGoalPlacement[] GoalLayout = Array.Empty<PrototypeReportGoalPlacement>();

        public int MaxTurns = 15;
        public int FirstOutbreakTurn = 3;
        public int RespawnDelayTurns = 2;
        public int ActiveOutbreakDelayTurns = 3;

        public float DurationSeconds = 300f;
        public float AnomalyStepSeconds = 20f;
        public float FirstOutbreakDelaySeconds = 60f;
        public float RespawnDelaySeconds = 40f;
        public float ActiveOutbreakDelaySeconds = 60f;

        public int CorruptionStepsBeforeDestroy = 2;
        public int SpawnPreferredDistance = 2;
        public int SpawnDistanceVariation = 1;
        public int SpawnCandidatePoolSize = 4;
    }

    public static class PrototypeLevelCatalog
    {
        // Static field initializers run top-to-bottom. StandardScramble uses this array while
        // the level catalog itself is being constructed, so it must be initialized first.
        private static readonly string[] Records = { "ivanov", "petrov", "sidorov", "volkova", "kim" };

        private static readonly PrototypeLevelConfig[] levels =
        {
            BuildFormulaIntro(),
            BuildRoutineReport(),
            BuildUrgentReconciliation(),
            BuildInconsistentData(),
            BuildFinalReconciliation()
        };

        public static IReadOnlyList<PrototypeLevelConfig> Levels => levels;
        public static int Count => levels.Length;
        public static PrototypeLevelConfig Get(int index) => levels[Mathf.Clamp(index, 0, levels.Length - 1)];

        private static PrototypeLevelConfig BuildFormulaIntro()
        {
            return new PrototypeLevelConfig
            {
                Id = "01_formula_intro",
                NameRu = "Формульная сверка",
                NameEn = "Formula Reconciliation",
                ReportGoals = PrototypeReportGoals.SalaryTotal | PrototypeReportGoals.OvertimeTotal,
                RefEnabled = false,
                FormulaCellsEnabled = true,
                MaxTurns = 8,
                Dataset = Dataset(
                    new[] { 41d, 37d, 44d, 36d, 40d },
                    new[] { 59d, 72d, 64d, 68d, 55d },
                    new[] { 2d, 5d, 1d, 4d, 3d },
                    new[] { 4d, 8d, 3d, 6d, 5d }),
                TokenLayout = StandardScramble(
                    salaryField: "B1", hoursField: "C1", overtimeField: "E1", bonusField: "G1",
                    hours: new[] { "B3", "G4", "C5", "E6", "B8" },
                    salary: new[] { "G3", "C4", "E5", "B6", "G7" },
                    overtime: new[] { "E3", "B4", "G5", "C6", "E7" },
                    bonus: new[] { "C3", "E4", "B5", "G6", "C7" }),
                FormulaLayout = new[]
                {
                    Formula("D2", FormulaKind.Sort), Formula("F2", FormulaKind.Sort),
                    Formula("H2", FormulaKind.Sum), Formula("H3", FormulaKind.Sum)
                },
                GoalLayout = new[]
                {
                    Goal("H2", PrototypeReportGoals.SalaryTotal),
                    Goal("H3", PrototypeReportGoals.OvertimeTotal)
                }
            };
        }

        private static PrototypeLevelConfig BuildRoutineReport()
        {
            return new PrototypeLevelConfig
            {
                Id = "02_routine_report",
                NameRu = "Обычный отчёт",
                NameEn = "Routine Report",
                ReportGoals = PrototypeReportGoals.SalaryTotal | PrototypeReportGoals.BonusAtLeastFour,
                RefEnabled = false,
                FormulaCellsEnabled = true,
                MaxTurns = 10,
                Dataset = Dataset(
                    new[] { 41d, 37d, 44d, 36d, 40d },
                    new[] { 59d, 72d, 64d, 68d, 55d },
                    new[] { 2d, 5d, 1d, 4d, 3d },
                    new[] { 4d, 8d, 3d, 6d, 5d }),
                TokenLayout = StandardScramble(
                    salaryField: "B1", hoursField: "C1", overtimeField: "E1", bonusField: "G1",
                    hours: new[] { "B3", "G4", "C5", "E6", "B8" },
                    salary: new[] { "G3", "C4", "E5", "B6", "G7" },
                    overtime: new[] { "E3", "B4", "G5", "C6", "E7" },
                    bonus: new[] { "C3", "E4", "B5", "G6", "C7" }),
                FormulaLayout = new[]
                {
                    Formula("D2", FormulaKind.Sort), Formula("F2", FormulaKind.Sort),
                    Formula("H2", FormulaKind.Sum), Formula("H3", FormulaKind.Sum)
                },
                GoalLayout = new[]
                {
                    Goal("H2", PrototypeReportGoals.SalaryTotal),
                    Goal("H3", PrototypeReportGoals.BonusAtLeastFour)
                }
            };
        }

        private static PrototypeLevelConfig BuildUrgentReconciliation()
        {
            return new PrototypeLevelConfig
            {
                Id = "03_urgent_reconciliation",
                NameRu = "Срочная сверка",
                NameEn = "Urgent Reconciliation",
                ReportGoals = PrototypeReportGoals.SalaryTotal | PrototypeReportGoals.BonusAtLeastFour,
                RefEnabled = true,
                FormulaCellsEnabled = true,
                MaxTurns = 13,
                FirstOutbreakTurn = 4,
                RespawnDelayTurns = 3,
                ActiveOutbreakDelayTurns = 4,
                Dataset = Dataset(
                    new[] { 39d, 46d, 34d, 42d, 37d },
                    new[] { 61d, 57d, 74d, 66d, 52d },
                    new[] { 3d, 1d, 6d, 2d, 4d },
                    new[] { 6d, 4d, 8d, 7d, 2d }),
                TokenLayout = StandardScramble(
                    salaryField: "C1", hoursField: "E1", overtimeField: "G1", bonusField: "B1",
                    hours: new[] { "C3", "E4", "G5", "C6", "E8" },
                    salary: new[] { "E3", "G4", "C5", "E6", "G7" },
                    overtime: new[] { "G3", "C4", "E5", "G6", "C8" },
                    bonus: new[] { "B3", "B4", "B5", "B6", "B7" }),
                FormulaLayout = new[]
                {
                    Formula("D2", FormulaKind.Sort), Formula("F2", FormulaKind.Sort), Formula("B2", FormulaKind.Sort),
                    Formula("H2", FormulaKind.Sum), Formula("H3", FormulaKind.Sum)
                },
                GoalLayout = new[]
                {
                    Goal("H2", PrototypeReportGoals.SalaryTotal),
                    Goal("H3", PrototypeReportGoals.BonusAtLeastFour)
                }
            };
        }

        private static PrototypeLevelConfig BuildInconsistentData()
        {
            return new PrototypeLevelConfig
            {
                Id = "04_inconsistent_data",
                NameRu = "Несходящиеся данные",
                NameEn = "Inconsistent Data",
                ReportGoals = PrototypeReportGoals.SalaryOfMaxOvertime | PrototypeReportGoals.SalaryForHoursBelowForty,
                RefEnabled = true,
                FormulaCellsEnabled = true,
                MaxTurns = 18,
                FirstOutbreakTurn = 4,
                RespawnDelayTurns = 3,
                ActiveOutbreakDelayTurns = 4,
                Dataset = Dataset(
                    new[] { 38d, 43d, 35d, 46d, 37d },
                    new[] { 62d, 69d, 76d, 54d, 71d },
                    new[] { 2d, 4d, 7d, 1d, 5d },
                    new[] { 3d, 7d, 5d, 9d, 4d }),
                TokenLayout = StandardScramble(
                    salaryField: "B1", hoursField: "D1", overtimeField: "F1", bonusField: "G1",
                    hours: new[] { "C3", "G4", "E5", "C6", "G7" },
                    salary: new[] { "E3", "C4", "G5", "E6", "C7" },
                    overtime: new[] { "G3", "E4", "C5", "G6", "E7" },
                    bonus: new[] { "C8", "D8", "E8", "F8", "G8" }),
                FormulaLayout = new[]
                {
                    Formula("B2", FormulaKind.Sort), Formula("D2", FormulaKind.Sort), Formula("F2", FormulaKind.Sort),
                    Formula("H5", FormulaKind.Sum)
                },
                GoalLayout = new[]
                {
                    // Direct-value report target: protected ReportCell, intentionally not a formula.
                    Goal("H2", PrototypeReportGoals.SalaryOfMaxOvertime),
                    Goal("H5", PrototypeReportGoals.SalaryForHoursBelowForty)
                }
            };
        }

        private static PrototypeLevelConfig BuildFinalReconciliation()
        {
            return new PrototypeLevelConfig
            {
                Id = "05_final_reconciliation",
                NameRu = "Финальная сверка",
                NameEn = "Final Reconciliation",
                ReportGoals = PrototypeReportGoals.SalaryForHoursBelowForty |
                              PrototypeReportGoals.OvertimeTotal |
                              PrototypeReportGoals.BonusTotal,
                RefEnabled = true,
                FormulaCellsEnabled = true,
                MaxTurns = 16,
                FirstOutbreakTurn = 4,
                RespawnDelayTurns = 3,
                ActiveOutbreakDelayTurns = 4,
                Dataset = Dataset(
                    new[] { 36d, 45d, 39d, 48d, 34d },
                    new[] { 67d, 56d, 73d, 61d, 78d },
                    new[] { 4d, 2d, 6d, 1d, 5d },
                    new[] { 5d, 8d, 3d, 7d, 6d }),
                TokenLayout = new[]
                {
                    Field("B1", "hours"), Field("D1", "salary"), Field("F1", "overtime"), Field("G1", "bonus"), Label("H1"),
                    Record("A3", "ivanov"), Record("A4", "petrov"), Record("A5", "sidorov"), Record("A6", "volkova"), Record("A7", "kim"),

                    // Hours occupy the spare C-lane at start. Sorting Hours into B frees C as a general backup SORT lane.
                    Data("C3", "ivanov", "hours"), Data("C4", "petrov", "hours"), Data("C5", "sidorov", "hours"), Data("C6", "volkova", "hours"), Data("C7", "kim", "hours"),
                    Data("E3", "ivanov", "salary"), Data("E4", "petrov", "salary"), Data("E5", "sidorov", "salary"), Data("E6", "volkova", "salary"), Data("E7", "kim", "salary"),
                    Data("B8", "ivanov", "overtime"), Data("C8", "petrov", "overtime"), Data("D8", "sidorov", "overtime"), Data("E8", "volkova", "overtime"), Data("F8", "kim", "overtime"),
                    // Bonus is already contiguous: one goal can be completed without consuming another SORT lane.
                    Data("G3", "ivanov", "bonus"), Data("G4", "petrov", "bonus"), Data("G5", "sidorov", "bonus"), Data("G6", "volkova", "bonus"), Data("G7", "kim", "bonus")
                },
                FormulaLayout = new[]
                {
                    Formula("B2", FormulaKind.Sort), Formula("C2", FormulaKind.Sort),
                    Formula("D2", FormulaKind.Sort), Formula("F2", FormulaKind.Sort),
                    Formula("H2", FormulaKind.Sum), Formula("H3", FormulaKind.Sum), Formula("H4", FormulaKind.Sum)
                },
                GoalLayout = new[]
                {
                    Goal("H2", PrototypeReportGoals.SalaryForHoursBelowForty),
                    Goal("H3", PrototypeReportGoals.OvertimeTotal),
                    Goal("H4", PrototypeReportGoals.BonusTotal)
                }
            };
        }

        private static PrototypeLevelDataset Dataset(double[] hours, double[] salary, double[] overtime, double[] bonus) =>
            new() { Hours = hours, Salary = salary, Overtime = overtime, Bonus = bonus };

        private static PrototypeTokenPlacement[] StandardScramble(
            string salaryField, string hoursField, string overtimeField, string bonusField,
            string[] hours, string[] salary, string[] overtime, string[] bonus)
        {
            var list = new List<PrototypeTokenPlacement>
            {
                Field(salaryField, "salary"), Field(hoursField, "hours"), Field(overtimeField, "overtime"), Field(bonusField, "bonus"), Label("H1"),
                Record("A3", "ivanov"), Record("A4", "petrov"), Record("A5", "sidorov"), Record("A6", "volkova"), Record("A7", "kim")
            };
            for (var i = 0; i < 5; i++)
            {
                list.Add(Data(hours[i], Records[i], "hours"));
                list.Add(Data(salary[i], Records[i], "salary"));
                list.Add(Data(overtime[i], Records[i], "overtime"));
                list.Add(Data(bonus[i], Records[i], "bonus"));
            }
            return list.ToArray();
        }

        private static PrototypeTokenPlacement Data(string address, string recordId, string fieldId) => Placement(address, PrototypePlacementKind.Data, recordId: recordId, fieldId: fieldId);
        private static PrototypeTokenPlacement Record(string address, string recordId) => Placement(address, PrototypePlacementKind.RecordKey, recordId: recordId);
        private static PrototypeTokenPlacement Field(string address, string fieldId) => Placement(address, PrototypePlacementKind.FieldKey, fieldId: fieldId);
        private static PrototypeTokenPlacement Label(string address) => Placement(address, PrototypePlacementKind.Label, tokenId: "report.label", stringId: "label.report");

        private static PrototypeFormulaPlacement Formula(string address, FormulaKind kind)
        {
            ParseAddress(address, out var row, out var column);
            return new PrototypeFormulaPlacement { Row = row, Column = column, Formula = kind };
        }

        private static PrototypeReportGoalPlacement Goal(string address, PrototypeReportGoals goal)
        {
            ParseAddress(address, out var row, out var column);
            return new PrototypeReportGoalPlacement { Goal = goal, Row = row, Column = column };
        }

        private static PrototypeTokenPlacement Placement(string address, PrototypePlacementKind kind,
            string recordId = null, string fieldId = null, string tokenId = null, string stringId = null)
        {
            ParseAddress(address, out var row, out var column);
            return new PrototypeTokenPlacement { Row = row, Column = column, Kind = kind, RecordId = recordId, FieldId = fieldId, TokenId = tokenId, StringId = stringId };
        }

        private static void ParseAddress(string address, out int row, out int column)
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Cell address is required.", nameof(address));
            var index = 0;
            var oneBasedColumn = 0;
            while (index < address.Length && char.IsLetter(address[index]))
            {
                oneBasedColumn = oneBasedColumn * 26 + (char.ToUpperInvariant(address[index]) - 'A' + 1);
                index++;
            }
            if (oneBasedColumn <= 0 || index >= address.Length || !int.TryParse(address.Substring(index), out var oneBasedRow) || oneBasedRow <= 0)
                throw new FormatException($"Invalid worksheet address: {address}");
            row = oneBasedRow - 1;
            column = oneBasedColumn - 1;
        }
    }

    public static class PrototypeLevelRuntime
    {
        public static int CurrentIndex { get; private set; }
        public static PrototypeLevelConfig Current => PrototypeLevelCatalog.Get(CurrentIndex);
        public static bool IsLast => CurrentIndex >= PrototypeLevelCatalog.Count - 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetForPlayMode() => CurrentIndex = 0;
        public static void SetCurrentIndex(int index) => CurrentIndex = Mathf.Clamp(index, 0, PrototypeLevelCatalog.Count - 1);
        public static bool Advance()
        {
            if (IsLast) return false;
            CurrentIndex++;
            return true;
        }
    }
}