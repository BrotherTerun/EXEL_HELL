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
        /// L1: FC2 tutorial without #REF!. New authored layout keeps four formulas but asks for
        /// SalaryTotal + BonusAtLeastFour so the second report already requires one filtering decision.
        /// Intended semantic route C0 ~= 5, B=8, recovery reserve 3.
        /// </summary>
        private static PrototypeLevelConfig BuildTutorial()
        {
            return new PrototypeLevelConfig
            {
                Id = "01_fc2_tutorial_edit",
                NameRu = "Учебная сверка",
                NameEn = "Training Reconciliation",
                Rows = 8,
                Columns = 8,
                ReportGoals = PrototypeReportGoals.SalaryTotal | PrototypeReportGoals.BonusAtLeastFour,
                RefEnabled = false,
                FormulaCellsEnabled = true,
                MaxTurns = 8,
                FirstOutbreakTurn = 3,
                RespawnDelayTurns = 2,
                ActiveOutbreakDelayTurns = 3,
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
                    Goal("H3", PrototypeReportGoals.BonusAtLeastFour)
                }
            };
        }

        /// <summary>
        /// L2: formula-scarcity control under one light outbreak window.
        /// One SORT + one SUM are deliberately reused; B=12 gives ~3 recovery actions above C0 ~= 9.
        /// Spawn distance 3±1 moves the initial deterministic pressure away from the only SUM.
        /// </summary>
        private static PrototypeLevelConfig BuildLightPressure()
        {
            return new PrototypeLevelConfig
            {
                Id = "02_fc2_light_pressure_edit_edit",
                NameRu = "Сверка под наблюдением",
                NameEn = "Watched Reconciliation",
                Rows = 8,
                Columns = 8,
                ReportGoals = PrototypeReportGoals.SalaryTotal | PrototypeReportGoals.BonusAtLeastFour,
                RefEnabled = true,
                FormulaCellsEnabled = true,
                MaxTurns = 12,
                FirstOutbreakTurn = 4,
                RespawnDelayTurns = 6,
                ActiveOutbreakDelayTurns = 6,
                CorruptionStepsBeforeDestroy = 2,
                SpawnPreferredDistance = 3,
                SpawnDistanceVariation = 1,
                SpawnCandidatePoolSize = 4,
                Dataset = Dataset(
                    new[] { 41d, 37d, 44d, 36d, 40d },
                    new[] { 59d, 72d, 64d, 68d, 55d },
                    new[] { 2d, 5d, 1d, 4d, 3d },
                    new[] { 4d, 8d, 3d, 6d, 5d }),
                TokenLayout = LightPressureLayout(),
                FormulaLayout = new[]
                {
                    Formula("F1", FormulaKind.Sort),
                    Formula("G1", FormulaKind.Sum)
                },
                GoalLayout = new[]
                {
                    Goal("H2", PrototypeReportGoals.SalaryTotal),
                    Goal("H3", PrototypeReportGoals.BonusAtLeastFour)
                }
            };
        }

        /// <summary>
        /// L3: semantic dependency and formula reuse under two expected outbreak windows.
        /// B=13 keeps recovery reserve around three actions for a C0 ~= 10 route.
        /// </summary>
        private static PrototypeLevelConfig BuildReplanTest()
        {
            return new PrototypeLevelConfig
            {
                Id = "03_fc2_replan_edit",
                NameRu = "Несходящиеся данные",
                NameEn = "Inconsistent Data",
                Rows = 8,
                Columns = 8,
                ReportGoals = PrototypeReportGoals.SalaryOfMaxOvertime | PrototypeReportGoals.SalaryForHoursBelowForty,
                RefEnabled = true,
                FormulaCellsEnabled = true,
                MaxTurns = 13,
                FirstOutbreakTurn = 3,
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
                TokenLayout = ReplanLayout(),
                FormulaLayout = new[]
                {
                    Formula("B2", FormulaKind.Sort),
                    Formula("D2", FormulaKind.Sort),
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
        /// L4: three-goal encounter tuned toward the historical high-interest multi-goal pressure profile.
        /// Sparse formula inventory is intentional. B=18 gives ~4 model reserve actions above C0 ~= 14,
        /// while F=3/A=4/Resp=3 targets roughly three outbreak windows instead of five.
        /// </summary>
        private static PrototypeLevelConfig BuildFinalPressure()
        {
            return new PrototypeLevelConfig
            {
                Id = "04_fc2_final_pressure_edit",
                NameRu = "Финальная сверка",
                NameEn = "Final Reconciliation",
                Rows = 8,
                Columns = 8,
                ReportGoals = PrototypeReportGoals.SalaryForHoursBelowForty |
                              PrototypeReportGoals.OvertimeTotal |
                              PrototypeReportGoals.BonusTotal,
                RefEnabled = true,
                FormulaCellsEnabled = true,
                MaxTurns = 18,
                FirstOutbreakTurn = 3,
                RespawnDelayTurns = 3,
                ActiveOutbreakDelayTurns = 4,
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
                    Formula("F2", FormulaKind.Sort),
                    Formula("H3", FormulaKind.Sum),
                    Formula("D6", FormulaKind.Sum)
                },
                GoalLayout = new[]
                {
                    Goal("H2", PrototypeReportGoals.SalaryForHoursBelowForty),
                    Goal("H3", PrototypeReportGoals.OvertimeTotal),
                    Goal("G6", PrototypeReportGoals.BonusTotal)
                }
            };
        }

        private static PrototypeTokenPlacement[] TutorialLayout()
        {
            return new[]
            {
                Field("B1", "salary"),
                Field("D1", "overtime"),
                Field("F1", "hours"),
                Field("G1", "bonus"),
                Label("H1"),
                Record("A3", "ivanov"),
                Data("C3", "ivanov", "hours"),
                Data("E3", "ivanov", "salary"),
                Data("G3", "ivanov", "overtime"),
                Record("A4", "petrov"),
                Data("C4", "petrov", "overtime"),
                Data("D4", "volkova", "bonus"),
                Data("E4", "petrov", "hours"),
                Data("G4", "petrov", "salary"),
                Record("A5", "sidorov"),
                Data("C5", "sidorov", "salary"),
                Data("E5", "sidorov", "overtime"),
                Data("G5", "sidorov", "hours"),
                Record("A6", "volkova"),
                Data("C6", "volkova", "hours"),
                Data("D6", "kim", "bonus"),
                Data("E6", "volkova", "salary"),
                Data("G6", "volkova", "overtime"),
                Record("A7", "kim"),
                Data("C7", "ivanov", "bonus"),
                Data("E7", "petrov", "bonus"),
                Data("G7", "kim", "salary"),
                Data("C8", "kim", "overtime"),
                Data("E8", "kim", "hours"),
                Data("G8", "sidorov", "bonus")
            };
        }

        private static PrototypeTokenPlacement[] LightPressureLayout()
        {
            return new[]
            {
                Field("B1", "salary"),
                Field("C1", "overtime"),
                Field("D1", "hours"),
                Field("E1", "bonus"),
                Label("H1"),
                Record("A2", "ivanov"),
                Data("C2", "ivanov", "hours"),
                Data("E2", "ivanov", "salary"),
                Data("G2", "ivanov", "overtime"),
                Record("A3", "petrov"),
                Data("C3", "petrov", "overtime"),
                Data("E3", "petrov", "hours"),
                Data("F3", "volkova", "bonus"),
                Data("G3", "petrov", "salary"),
                Record("A4", "sidorov"),
                Data("C4", "sidorov", "salary"),
                Data("E4", "sidorov", "overtime"),
                Data("G4", "sidorov", "hours"),
                Record("A5", "volkova"),
                Data("C5", "volkova", "hours"),
                Data("E5", "volkova", "salary"),
                Data("F5", "kim", "bonus"),
                Data("G5", "volkova", "overtime"),
                Record("A6", "kim"),
                Data("C6", "ivanov", "bonus"),
                Data("E6", "petrov", "bonus"),
                Data("G6", "kim", "salary"),
                Data("C7", "kim", "overtime"),
                Data("E7", "kim", "hours"),
                Data("G7", "sidorov", "bonus")
            };
        }

        private static PrototypeTokenPlacement[] ReplanLayout()
        {
            return new[]
            {
                Field("B1", "bonus"),
                Field("C1", "salary"),
                Field("E1", "hours"),
                Field("G1", "overtime"),
                Label("H1"),
                Record("A3", "ivanov"),
                Data("C3", "ivanov", "hours"),
                Data("E3", "ivanov", "salary"),
                Data("G3", "ivanov", "overtime"),
                Record("A4", "petrov"),
                Data("B4", "petrov", "bonus"),
                Data("C4", "petrov", "salary"),
                Data("E4", "petrov", "overtime"),
                Data("G4", "petrov", "hours"),
                Record("A5", "sidorov"),
                Data("C5", "sidorov", "overtime"),
                Data("E5", "sidorov", "hours"),
                Data("G5", "sidorov", "salary"),
                Record("A6", "volkova"),
                Data("B6", "volkova", "bonus"),
                Data("C6", "volkova", "hours"),
                Data("E6", "volkova", "salary"),
                Data("G6", "volkova", "overtime"),
                Record("A7", "kim"),
                Data("C7", "kim", "salary"),
                Data("E7", "kim", "overtime"),
                Data("G7", "kim", "hours"),
                Data("C8", "ivanov", "bonus"),
                Data("E8", "sidorov", "bonus"),
                Data("G8", "kim", "bonus")
            };
        }

        private static PrototypeTokenPlacement[] FinalPressureLayout()
        {
            return new[]
            {
                Field("B1", "bonus"),
                Field("C1", "salary"),
                Field("E1", "hours"),
                Field("G1", "overtime"),
                Label("H1"),
                Record("A2", "ivanov"),
                Data("C3", "ivanov", "hours"),
                Data("E3", "ivanov", "salary"),
                Data("G3", "ivanov", "bonus"),
                Record("A4", "petrov"),
                Data("D4", "petrov", "salary"),
                Data("E4", "petrov", "overtime"),
                Data("F4", "petrov", "bonus"),
                Data("G4", "petrov", "hours"),
                Record("A5", "sidorov"),
                Data("C5", "sidorov", "overtime"),
                Data("E5", "sidorov", "hours"),
                Data("G5", "sidorov", "salary"),
                Record("B6", "volkova"),
                Data("C6", "volkova", "hours"),
                Data("E6", "volkova", "salary"),
                Data("F6", "volkova", "bonus"),
                Data("H6", "volkova", "overtime"),
                Record("A7", "kim"),
                Data("C7", "kim", "salary"),
                Data("E7", "kim", "overtime"),
                Data("G7", "kim", "hours"),
                Data("C8", "ivanov", "overtime"),
                Data("D8", "sidorov", "bonus"),
                Data("H8", "kim", "bonus")
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
