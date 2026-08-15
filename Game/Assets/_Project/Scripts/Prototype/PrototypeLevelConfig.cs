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

    public enum PrototypePlacementKind
    {
        Data,
        RecordKey,
        FieldKey,
        Label
    }

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

        // Turn-based branch.
        public int MaxTurns = 15;
        public int FirstOutbreakTurn = 3;
        public int RespawnDelayTurns = 2;
        public int ActiveOutbreakDelayTurns = 3;

        // Legacy realtime values retained in shared level data for branch compatibility.
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
        private static readonly PrototypeLevelConfig[] levels =
        {
            BuildFormulaTutorial(),
            BuildSpillReuse(),
            BuildAggregateDependency(),
            BuildFormulaPressure()
        };

        public static IReadOnlyList<PrototypeLevelConfig> Levels => levels;
        public static int Count => levels.Length;
        public static PrototypeLevelConfig Get(int index) => levels[Mathf.Clamp(index, 0, levels.Length - 1)];

        private static PrototypeLevelConfig BuildFormulaTutorial()
        {
            return new PrototypeLevelConfig
            {
                Id = "01_formula_tutorial",
                NameRu = "Формульная сверка",
                NameEn = "Formula Reconciliation",
                ReportGoals = PrototypeReportGoals.SalaryTotal | PrototypeReportGoals.OvertimeTotal,
                RefEnabled = false,
                FormulaCellsEnabled = true,
                MaxTurns = 10,
                Dataset = new PrototypeLevelDataset
                {
                    Hours = new[] { 41d, 37d, 44d, 36d, 40d },
                    Salary = new[] { 59d, 72d, 64d, 68d, 55d },
                    Overtime = new[] { 2d, 5d, 1d, 4d, 3d },
                    Bonus = new[] { 4d, 8d, 3d, 6d, 5d }
                },
                TokenLayout = new[]
                {
                    // Familiar worksheet grammar: employee keys stay on the left, field keys on top.
                    Field("B1", "salary"), Field("C1", "hours"), Field("E1", "overtime"), Field("G1", "bonus"), Label("H1"),
                    Record("A3", "ivanov"), Record("A4", "petrov"), Record("A5", "sidorov"), Record("A6", "volkova"), Record("A7", "kim"),

                    // Raw numbers are deliberately spatially scrambled. D3:D7 and F3:F7 are clean SORT spill lanes.
                    Data("B3", "ivanov", "hours"), Data("G4", "petrov", "hours"), Data("C5", "sidorov", "hours"), Data("E6", "volkova", "hours"), Data("B8", "kim", "hours"),
                    Data("G3", "ivanov", "salary"), Data("C4", "petrov", "salary"), Data("E5", "sidorov", "salary"), Data("B6", "volkova", "salary"), Data("G7", "kim", "salary"),
                    Data("E3", "ivanov", "overtime"), Data("B4", "petrov", "overtime"), Data("G5", "sidorov", "overtime"), Data("C6", "volkova", "overtime"), Data("E7", "kim", "overtime"),
                    Data("C3", "ivanov", "bonus"), Data("E4", "petrov", "bonus"), Data("B5", "sidorov", "bonus"), Data("G6", "volkova", "bonus"), Data("C7", "kim", "bonus")
                },
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

        private static PrototypeLevelConfig BuildSpillReuse()
        {
            return new PrototypeLevelConfig
            {
                Id = "02_spill_reuse",
                NameRu = "Повторная сортировка",
                NameEn = "Sort Reuse",
                ReportGoals = PrototypeReportGoals.SalaryTotal | PrototypeReportGoals.BonusTotal,
                RefEnabled = true,
                FormulaCellsEnabled = true,
                MaxTurns = 12,
                FirstOutbreakTurn = 3,
                RespawnDelayTurns = 2,
                ActiveOutbreakDelayTurns = 3,
                Dataset = new PrototypeLevelDataset
                {
                    Hours = new[] { 39d, 46d, 34d, 42d, 37d },
                    Salary = new[] { 61d, 57d, 74d, 66d, 52d },
                    Overtime = new[] { 3d, 1d, 6d, 2d, 4d },
                    Bonus = new[] { 6d, 4d, 8d, 7d, 2d }
                },
                TokenLayout = new[]
                {
                    Field("B1", "salary"), Field("C1", "bonus"), Field("E1", "hours"), Field("G1", "overtime"), Label("H1"),
                    Record("A3", "ivanov"), Record("A4", "petrov"), Record("A5", "sidorov"), Record("A6", "volkova"), Record("A7", "kim"),

                    // D3:D7 is intentionally clear: this level tests formula reuse, not an accidental blocked spill.
                    Data("B3", "ivanov", "salary"), Data("F4", "petrov", "salary"), Data("C5", "sidorov", "salary"), Data("G6", "volkova", "salary"), Data("E7", "kim", "salary"),
                    Data("G3", "ivanov", "bonus"), Data("C4", "petrov", "bonus"), Data("F5", "sidorov", "bonus"), Data("B6", "volkova", "bonus"), Data("C7", "kim", "bonus"),
                    Data("E3", "ivanov", "hours"), Data("B4", "petrov", "hours"), Data("G5", "sidorov", "hours"), Data("C6", "volkova", "hours"), Data("F7", "kim", "hours"),
                    Data("C3", "ivanov", "overtime"), Data("E4", "petrov", "overtime"), Data("B5", "sidorov", "overtime"), Data("F6", "volkova", "overtime"), Data("G7", "kim", "overtime")
                },
                FormulaLayout = new[]
                {
                    Formula("D2", FormulaKind.Sort),
                    Formula("H2", FormulaKind.Sum), Formula("H3", FormulaKind.Sum)
                },
                GoalLayout = new[]
                {
                    Goal("H2", PrototypeReportGoals.SalaryTotal),
                    Goal("H3", PrototypeReportGoals.BonusTotal)
                }
            };
        }

        private static PrototypeLevelConfig BuildAggregateDependency()
        {
            return new PrototypeLevelConfig
            {
                Id = "03_aggregate_dependency",
                NameRu = "Промежуточные итоги",
                NameEn = "Intermediate Totals",
                ReportGoals = PrototypeReportGoals.SalaryTotal | PrototypeReportGoals.BonusTotal,
                RefEnabled = true,
                FormulaCellsEnabled = true,
                MaxTurns = 12,
                FirstOutbreakTurn = 3,
                RespawnDelayTurns = 2,
                ActiveOutbreakDelayTurns = 3,
                Dataset = new PrototypeLevelDataset
                {
                    Hours = new[] { 38d, 42d, 35d, 47d, 39d },
                    Salary = new[] { 63d, 54d, 71d, 58d, 66d },
                    Overtime = new[] { 1d, 4d, 6d, 3d, 2d },
                    Bonus = new[] { 7d, 2d, 5d, 9d, 4d }
                },
                TokenLayout = new[]
                {
                    Field("B1", "salary"), Field("C1", "hours"), Field("D1", "overtime"), Field("G1", "bonus"), Label("H1"),
                    Record("A3", "ivanov"), Record("A4", "petrov"), Record("A5", "sidorov"), Record("A6", "volkova"), Record("A7", "kim"),

                    // Salary keeps the two intentional subtotal clusters; the remaining fields are scrambled around them.
                    Data("B3", "ivanov", "salary"), Data("B4", "petrov", "salary"), Data("B5", "sidorov", "salary"), Data("D6", "volkova", "salary"), Data("D7", "kim", "salary"),
                    Data("G3", "ivanov", "hours"), Data("C4", "petrov", "hours"), Data("E5", "sidorov", "hours"), Data("B6", "volkova", "hours"), Data("G7", "kim", "hours"),
                    Data("D3", "ivanov", "overtime"), Data("G4", "petrov", "overtime"), Data("C5", "sidorov", "overtime"), Data("E6", "volkova", "overtime"), Data("B7", "kim", "overtime"),
                    Data("C3", "ivanov", "bonus"), Data("E4", "petrov", "bonus"), Data("G5", "sidorov", "bonus"), Data("C6", "volkova", "bonus"), Data("E7", "kim", "bonus")
                },
                FormulaLayout = new[]
                {
                    Formula("C2", FormulaKind.Sum), Formula("F2", FormulaKind.Sort),
                    Formula("H2", FormulaKind.Sum), Formula("H3", FormulaKind.Sum)
                },
                GoalLayout = new[]
                {
                    Goal("H2", PrototypeReportGoals.SalaryTotal),
                    Goal("H3", PrototypeReportGoals.BonusTotal)
                }
            };
        }

        private static PrototypeLevelConfig BuildFormulaPressure()
        {
            return new PrototypeLevelConfig
            {
                Id = "04_formula_pressure",
                NameRu = "Формулы под давлением",
                NameEn = "Formulas Under Pressure",
                ReportGoals = PrototypeReportGoals.SalaryForHoursBelowForty |
                              PrototypeReportGoals.OvertimeTotal |
                              PrototypeReportGoals.BonusTotal,
                RefEnabled = true,
                FormulaCellsEnabled = true,
                MaxTurns = 18,
                FirstOutbreakTurn = 2,
                RespawnDelayTurns = 2,
                ActiveOutbreakDelayTurns = 3,
                Dataset = new PrototypeLevelDataset
                {
                    Hours = new[] { 36d, 45d, 39d, 48d, 34d },
                    Salary = new[] { 67d, 56d, 73d, 61d, 78d },
                    Overtime = new[] { 4d, 2d, 6d, 1d, 5d },
                    Bonus = new[] { 5d, 8d, 3d, 7d, 6d }
                },
                TokenLayout = new[]
                {
                    Field("A1", "hours"), Field("C1", "salary"), Field("E1", "overtime"), Field("G1", "bonus"), Label("H1"),
                    Record("A3", "ivanov"), Record("A4", "petrov"), Record("A5", "sidorov"), Record("A6", "volkova"), Record("A7", "kim"),

                    // Late pressure level: B5 is an intentional Bonus blocker in the first SORT lane.
                    // Other tokens remain spatially scrambled; moving-set tokens may already sit in their future lanes.
                    Data("B3", "ivanov", "hours"), Data("C4", "petrov", "hours"), Data("E5", "sidorov", "hours"), Data("G6", "volkova", "hours"), Data("B7", "kim", "hours"),
                    Data("D3", "ivanov", "salary"), Data("G4", "petrov", "salary"), Data("C5", "sidorov", "salary"), Data("E6", "volkova", "salary"), Data("D7", "kim", "salary"),
                    Data("F3", "ivanov", "overtime"), Data("C3", "petrov", "overtime"), Data("F5", "sidorov", "overtime"), Data("G5", "volkova", "overtime"), Data("F7", "kim", "overtime"),
                    Data("G3", "ivanov", "bonus"), Data("E4", "petrov", "bonus"), Data("B5", "sidorov", "bonus"), Data("C6", "volkova", "bonus"), Data("E7", "kim", "bonus")
                },
                FormulaLayout = new[]
                {
                    Formula("B2", FormulaKind.Sort), Formula("D2", FormulaKind.Sort), Formula("F2", FormulaKind.Sort),
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

        private static PrototypeTokenPlacement Data(string address, string recordId, string fieldId) =>
            Placement(address, PrototypePlacementKind.Data, recordId: recordId, fieldId: fieldId);

        private static PrototypeTokenPlacement Record(string address, string recordId) =>
            Placement(address, PrototypePlacementKind.RecordKey, recordId: recordId);

        private static PrototypeTokenPlacement Field(string address, string fieldId) =>
            Placement(address, PrototypePlacementKind.FieldKey, fieldId: fieldId);

        private static PrototypeTokenPlacement Label(string address) =>
            Placement(address, PrototypePlacementKind.Label, tokenId: "report.label", stringId: "label.report");

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
            return new PrototypeTokenPlacement
            {
                Row = row,
                Column = column,
                Kind = kind,
                RecordId = recordId,
                FieldId = fieldId,
                TokenId = tokenId,
                StringId = stringId
            };
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
        private static void ResetForPlayMode()
        {
            CurrentIndex = 0;
        }

        public static void SetCurrentIndex(int index)
        {
            CurrentIndex = Mathf.Clamp(index, 0, PrototypeLevelCatalog.Count - 1);
        }

        public static bool Advance()
        {
            if (IsLast) return false;
            CurrentIndex++;
            return true;
        }
    }
}
