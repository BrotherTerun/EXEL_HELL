using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExcelHell.Prototype
{
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

        public int MaxTurns = 15;
        public int FirstOutbreakTurn = 3;
        public int RespawnDelayTurns = 2;
        public int ActiveOutbreakDelayTurns = 3;

        // Realtime defaults are intentionally aggressive enough that a practiced player
        // still sees the anomaly before finishing the worksheet.
        public float DurationSeconds = 270f;
        public float AnomalyStepSeconds = 10f;
        public float FirstOutbreakDelaySeconds = 30f;
        public float RespawnDelaySeconds = 20f;
        public float ActiveOutbreakDelaySeconds = 30f;

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
                DurationSeconds = 270f
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
                DurationSeconds = 270f,
                FirstOutbreakDelaySeconds = 45f,
                AnomalyStepSeconds = 10f,
                RespawnDelaySeconds = 25f,
                ActiveOutbreakDelaySeconds = 38f
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
                DurationSeconds = 270f,
                FirstOutbreakDelaySeconds = 30f,
                AnomalyStepSeconds = 9f,
                RespawnDelaySeconds = 20f,
                ActiveOutbreakDelaySeconds = 30f
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
