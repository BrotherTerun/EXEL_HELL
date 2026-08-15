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
                DurationSeconds = 300f
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
                ActiveOutbreakDelaySeconds = 75f
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
                ActiveOutbreakDelaySeconds = 60f
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
