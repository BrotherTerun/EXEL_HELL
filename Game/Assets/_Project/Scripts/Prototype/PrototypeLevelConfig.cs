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
        // Static field initializers run top-to-bottom. Keep shared data above the authored catalog.
        private static readonly string[] Records = { "ivanov", "petrov", "sidorov", "volkova", "kim" };

        private static readonly PrototypeLevelConfig[] levels =
        {
            BuildTutorial(),
            BuildLightPressure(),
            BuildReplanTest(),
            BuildFinalPressure()
        };

        public static IReadOnlyList<PrototypeLevelConfig> Levels => levels;
        public static int Count => levels.Length;
        public static PrototypeLevelConfig Get(int index) => levels[Mathf.Clamp(index, 0, levels.Length - 1)];

        /// <summary>
        /// L1: teach FC2 grammar without #REF!.
        /// C_sem=4, intended C0=5. D-SORT has two blockers; moving the formula to B2 is the clean route.
        /// </summary>
        private static PrototypeLevelConfig BuildTutorial()
        {
            return new PrototypeLevelConfig
            {
                Id = "01_fc2_tutorial",
                NameRu = "Учебная сверка",
                NameEn = "Training Reconciliation",
                ReportGoals = PrototypeReportGoals.SalaryTotal | PrototypeReportGoals.OvertimeTotal,
                RefEnabled = false,
                FormulaCellsEnabled = true,
                MaxTurns = 8,
                Dataset = Dataset(
                    new[] { 41d, 37d, 44d, 36d, 40d },
                    new[] { 59d, 72d, 64d, 68d, 55d },
                    new[] { 2d, 5d, 1d, 4d, 3d },
                    new[] { 4d, 8d, 3d, 6d, 5d }),
                TokenLayout = TutorialLayout(),
                FormulaLayout = new[]
                {
                    Formula("D2", FormulaKind.Sort),
                    Formula("F2", FormulaKind.Sort),
                    Formula("H2", FormulaKind.Sum),
                    Formula("H3", FormulaKind.Sum)
                },
                GoalLayout = new[]
                {
                    Goal("H2", PrototypeReportGoals.SalaryTotal),
                    Goal("H3", PrototypeReportGoals.OvertimeTotal)
                }
            };
        }

        /// <summary>
        /// L2: exact L1 puzzle under a late, light anomaly.
        /// C0 remains 5; first outbreak on action 4 gives one visible outbreak window without demanding a response.
        /// </summary>
        private static PrototypeLevelConfig BuildLightPressure()
        {
            return new PrototypeLevelConfig
            {
                Id = "02_fc2_light_pressure",
                NameRu = "Сверка под наблюдением",
                NameEn = "Watched Reconciliation",
                ReportGoals = PrototypeReportGoals.SalaryTotal | PrototypeReportGoals.OvertimeTotal,
                RefEnabled = true,
                FormulaCellsEnabled = true,
                MaxTurns = 8,
                FirstOutbreakTurn = 4,
                RespawnDelayTurns = 6,
                ActiveOutbreakDelayTurns = 6,
                CorruptionStepsBeforeDestroy = 2,
                SpawnPreferredDistance = 2,
                SpawnDistanceVariation = 1,
                SpawnCandidatePoolSize = 4,
                Dataset = Dataset(
                    new[] { 41d, 37d, 44d, 36d, 40d },
                    new[] { 59d, 72d, 64d, 68d, 55d },
                    new[] { 2d, 5d, 1d, 4d, 3d },
                    new[] { 4d, 8d, 3d, 6d, 5d }),
                TokenLayout = TutorialLayout(),
                FormulaLayout = new[]
                {
                    Formula("D2", FormulaKind.Sort),
                    Formula("F2", FormulaKind.Sort),
                    Formula("H2", FormulaKind.Sum),
                    Formula("H3", FormulaKind.Sum)
                },
                GoalLayout = new[]
                {
                    Goal("H2", PrototypeReportGoals.SalaryTotal),
                    Goal("H3", PrototypeReportGoals.OvertimeTotal)
                }
            };
        }

        /// <summary>
        /// L3: semantic dependency + 2-3 expected replan moments.
        /// No-REF C0~=7 with three SORTs and one report SUM. Initial deterministic spawn candidate is F2.
        /// </summary>
        private static PrototypeLevelConfig BuildReplanTest()
        {
            return new PrototypeLevelConfig
            {
                Id = "03_fc2_replan",
                NameRu = "Несходящиеся данные",
                NameEn = "Inconsistent Data",
                ReportGoals = PrototypeReportGoals.SalaryOfMaxOvertime | PrototypeReportGoals.SalaryForHoursBelowForty,
                RefEnabled = true,
                FormulaCellsEnabled = true,
                MaxTurns = 11,
                FirstOutbreakTurn = 2,
                RespawnDelayTurns = 3,
                ActiveOutbreakDelayTurns = 4,
                CorruptionStepsBeforeDestroy = 2,
                SpawnPreferredDistance = 2,
                SpawnDistanceVariation = 1,
                SpawnCandidatePoolSize = 4,
                Dataset = Dataset(
                    new[] { 38d, 43d, 35d, 46d, 37d },
                    new[] { 62d, 69d, 76d, 54d, 71d },
                    new[] { 2d, 4d, 7d, 1d, 5d },
                    new[] { 3d, 7d, 5d, 9d, 4d }),
                TokenLayout = SemanticScatterLayout(),
                FormulaLayout = new[]
                {
                    Formula("B2", FormulaKind.Sort),
                    Formula("D2", FormulaKind.Sort),
                    Formula("F2", FormulaKind.Sort),
                    Formula("H5", FormulaKind.Sum)
                },
                GoalLayout = new[]
                {
                    Goal("H2", PrototypeReportGoals.SalaryOfMaxOvertime),
                    Goal("H5", PrototypeReportGoals.SalaryForHoursBelowForty)
                }
            };
        }

        /// <summary>
        /// L4: three-goal composition under real #REF! pressure.
        /// C_sem=9, intended C0~=10. Three SORTs serve four required projections via one purposeful reuse.
        /// </summary>
        private static PrototypeLevelConfig BuildFinalPressure()
        {
            return new PrototypeLevelConfig
            {
                Id = "04_fc2_final_pressure",
                NameRu = "Финальная сверка",
                NameEn = "Final Reconciliation",
                ReportGoals = PrototypeReportGoals.SalaryForHoursBelowForty |
                              PrototypeReportGoals.OvertimeTotal |
                              PrototypeReportGoals.BonusTotal,
                RefEnabled = true,
                FormulaCellsEnabled = true,
                MaxTurns = 14,
                FirstOutbreakTurn = 2,
                RespawnDelayTurns = 2,
                ActiveOutbreakDelayTurns = 3,
                CorruptionStepsBeforeDestroy = 2,
                SpawnPreferredDistance = 2,
                SpawnDistanceVariation = 0,
                SpawnCandidatePoolSize = 2,
                Dataset = Dataset(
                    new[] { 36d, 45d, 39d, 48d, 34d },
                    new[] { 67d, 56d, 73d, 61d, 78d },
                    new[] { 4d, 2d, 6d, 1d, 5d },
                    new[] { 5d, 8d, 3d, 7d, 6d }),
                TokenLayout = FinalPressureLayout(),
                FormulaLayout = new[]
                {
                    Formula("B2", FormulaKind.Sort),
                    Formula("D2", FormulaKind.Sort),
                    Formula("F2", FormulaKind.Sort),
                    Formula("H2", FormulaKind.Sum),
                    Formula("H3", FormulaKind.Sum),
                    Formula("H4", FormulaKind.Sum)
                },
                GoalLayout = new[]
                {
                    Goal("H2", PrototypeReportGoals.SalaryForHoursBelowForty),
                    Goal("H3", PrototypeReportGoals.OvertimeTotal),
                    Goal("H4", PrototypeReportGoals.BonusTotal)
                }
            };
        }

        private static PrototypeTokenPlacement[] TutorialLayout()
        {
            return new[]
            {
                Field("B1", "salary"), Field("D1", "overtime"), Field("F1", "hours"), Field("G1", "bonus"), Label("H1"),
                Record("A3", "ivanov"), Record("A4", "petrov"), Record("A5", "sidorov"), Record("A6", "volkova"), Record("A7", "kim"),

                Data("C3", "ivanov", "hours"), Data("E4", "petrov", "hours"), Data("G5", "sidorov", "hours"), Data("C6", "volkova", "hours"), Data("E8", "kim", "hours"),
                Data("E3", "ivanov", "salary"), Data("G4", "petrov", "salary"), Data("C5", "sidorov", "salary"), Data("E6", "volkova", "salary"), Data("G7", "kim", "salary"),
                Data("G3", "ivanov", "overtime"), Data("C4", "petrov", "overtime"), Data("E5", "sidorov", "overtime"), Data("G6", "volkova", "overtime"), Data("C8", "kim", "overtime"),

                // D3:D7 is deliberately unattractive: two unrelated blockers make moving D2 SORT to the open B-lane cheaper.
                Data("C7", "ivanov", "bonus"), Data("E7", "petrov", "bonus"), Data("G8", "sidorov", "bonus"), Data("D4", "volkova", "bonus"), Data("D6", "kim", "bonus")
            };
        }

        private static PrototypeTokenPlacement[] SemanticScatterLayout()
        {
            return new[]
            {
                Field("C1", "salary"), Field("E1", "hours"), Field("G1", "overtime"), Field("B1", "bonus"), Label("H1"),
                Record("A3", "ivanov"), Record("A4", "petrov"), Record("A5", "sidorov"), Record("A6", "volkova"), Record("A7", "kim"),

                // B/D/F rows 3..7 start clear. They are useful SORT lanes, but movable FormulaCells can abandon a threatened lane.
                Data("C3", "ivanov", "hours"), Data("G4", "petrov", "hours"), Data("E5", "sidorov", "hours"), Data("C6", "volkova", "hours"), Data("G7", "kim", "hours"),
                Data("E3", "ivanov", "salary"), Data("C4", "petrov", "salary"), Data("G5", "sidorov", "salary"), Data("E6", "volkova", "salary"), Data("C7", "kim", "salary"),
                Data("G3", "ivanov", "overtime"), Data("E4", "petrov", "overtime"), Data("C5", "sidorov", "overtime"), Data("G6", "volkova", "overtime"), Data("E7", "kim", "overtime"),
                Data("C8", "ivanov", "bonus"), Data("D8", "petrov", "bonus"), Data("E8", "sidorov", "bonus"), Data("F8", "volkova", "bonus"), Data("G8", "kim", "bonus")
            };
        }

        private static PrototypeTokenPlacement[] FinalPressureLayout()
        {
            return new[]
            {
                Field("C1", "salary"), Field("E1", "hours"), Field("G1", "overtime"), Field("B1", "bonus"), Label("H1"),
                Record("A3", "ivanov"), Record("A4", "petrov"), Record("A5", "sidorov"), Record("A6", "volkova"), Record("A7", "kim"),

                Data("C3", "ivanov", "hours"), Data("G4", "petrov", "hours"), Data("E5", "sidorov", "hours"), Data("C6", "volkova", "hours"), Data("G7", "kim", "hours"),
                Data("E3", "ivanov", "salary"), Data("C4", "petrov", "salary"), Data("G5", "sidorov", "salary"), Data("E6", "volkova", "salary"), Data("C7", "kim", "salary"),

                // Overtime and Bonus deliberately interlock: neither goal starts as a clean direct SUM range.
                Data("C8", "ivanov", "overtime"), Data("E4", "petrov", "overtime"), Data("C5", "sidorov", "overtime"), Data("G6", "volkova", "overtime"), Data("E7", "kim", "overtime"),
                Data("G3", "ivanov", "bonus"), Data("D8", "petrov", "bonus"), Data("E8", "sidorov", "bonus"), Data("F8", "volkova", "bonus"), Data("G8", "kim", "bonus")
            };
        }

        private static PrototypeLevelDataset Dataset(double[] hours, double[] salary, double[] overtime, double[] bonus) =>
            new() { Hours = hours, Salary = salary, Overtime = overtime, Bonus = bonus };

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
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Cell address is required.", nameof(address));

            var index = 0;
            var oneBasedColumn = 0;
            while (index < address.Length && char.IsLetter(address[index]))
            {
                oneBasedColumn = oneBasedColumn * 26 + (char.ToUpperInvariant(address[index]) - 'A' + 1);
                index++;
            }

            if (oneBasedColumn <= 0 || index >= address.Length ||
                !int.TryParse(address.Substring(index), out var oneBasedRow) || oneBasedRow <= 0)
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

        public static void SetCurrentIndex(int index) =>
            CurrentIndex = Mathf.Clamp(index, 0, PrototypeLevelCatalog.Count - 1);

        public static bool Advance()
        {
            if (IsLast) return false;
            CurrentIndex++;
            return true;
        }
    }
}
