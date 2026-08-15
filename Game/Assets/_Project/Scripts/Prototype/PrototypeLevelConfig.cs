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
        public PrototypeLevelDataset Dataset;

        // Turn-based branch.
        public int MaxTurns = 15;
        public int FirstOutbreakTurn = 3;
        public int RespawnDelayTurns = 2;
        public int ActiveOutbreakDelayTurns = 3;

        // Realtime branch. Kept in the same level data so both A/B builds use identical content.
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
            new PrototypeLevelConfig
            {
                Id = "01_basics",
                NameRu = "Обычный отчёт",
                NameEn = "Routine Report",
                ReportGoals = PrototypeReportGoals.SalaryTotal | PrototypeReportGoals.OvertimeTotal,
                RefEnabled = false,
                MaxTurns = 10,
                DurationSeconds = 300f,
                Dataset = new PrototypeLevelDataset
                {
                    Hours = new[] { 41d, 37d, 44d, 36d, 40d },
                    Salary = new[] { 59d, 72d, 64d, 68d, 55d },
                    Overtime = new[] { 2d, 5d, 1d, 4d, 3d },
                    Bonus = new[] { 4d, 8d, 3d, 6d, 5d }
                }
            },
            new PrototypeLevelConfig
            {
                Id = "02_filter",
                NameRu = "Срочная сверка",
                NameEn = "Urgent Reconciliation",
                ReportGoals = PrototypeReportGoals.SalaryTotal | PrototypeReportGoals.BonusAtLeastFour,
                RefEnabled = true,
                MaxTurns = 13,
                FirstOutbreakTurn = 3,
                RespawnDelayTurns = 2,
                ActiveOutbreakDelayTurns = 3,
                DurationSeconds = 300f,
                FirstOutbreakDelaySeconds = 90f,
                AnomalyStepSeconds = 20f,
                RespawnDelaySeconds = 50f,
                ActiveOutbreakDelaySeconds = 75f,
                Dataset = new PrototypeLevelDataset
                {
                    Hours = new[] { 39d, 46d, 34d, 42d, 37d },
                    Salary = new[] { 61d, 57d, 74d, 66d, 52d },
                    Overtime = new[] { 3d, 1d, 6d, 2d, 4d },
                    Bonus = new[] { 6d, 4d, 8d, 7d, 2d }
                }
            },
            new PrototypeLevelConfig
            {
                Id = "03_semantic",
                NameRu = "Несходящиеся данные",
                NameEn = "Inconsistent Data",
                ReportGoals = PrototypeReportGoals.SalaryOfMaxOvertime | PrototypeReportGoals.SalaryForHoursBelowForty,
                RefEnabled = true,
                MaxTurns = 18,
                FirstOutbreakTurn = 2,
                RespawnDelayTurns = 2,
                ActiveOutbreakDelayTurns = 3,
                DurationSeconds = 300f,
                FirstOutbreakDelaySeconds = 60f,
                AnomalyStepSeconds = 18f,
                RespawnDelaySeconds = 40f,
                ActiveOutbreakDelaySeconds = 60f,
                Dataset = new PrototypeLevelDataset
                {
                    Hours = new[] { 38d, 43d, 35d, 46d, 37d },
                    Salary = new[] { 62d, 69d, 76d, 54d, 71d },
                    Overtime = new[] { 2d, 4d, 7d, 1d, 5d },
                    Bonus = new[] { 3d, 7d, 5d, 9d, 4d }
                }
            }
        };

        public static IReadOnlyList<PrototypeLevelConfig> Levels => levels;
        public static int Count => levels.Length;
        public static PrototypeLevelConfig Get(int index) => levels[Mathf.Clamp(index, 0, levels.Length - 1)];
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

        public static bool Advance()
        {
            if (IsLast) return false;
            CurrentIndex++;
            return true;
        }
    }
}
