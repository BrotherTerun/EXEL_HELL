using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ExcelHell.Prototype
{
    public sealed class PrototypeLevelDatasetAdapter : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly FieldInfo GoalsField = typeof(ExcelHellPrototype).GetField("goals", Flags);
        private static readonly MethodInfo RefreshAllMethod = typeof(ExcelHellPrototype).GetMethod("RefreshAll", Flags);

        private ExcelHellPrototype appliedTo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PrototypeLevelDatasetAdapter>() != null) return;
            var adapter = new GameObject("EXCEL HELL Level Dataset Adapter").AddComponent<PrototypeLevelDatasetAdapter>();
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
            var dataset = level?.Dataset;
            if (dataset == null) return;

            var cells = CellsField?.GetValue(prototype) as CellModel[,];
            var goals = GoalsField?.GetValue(prototype) as List<ReportGoal>;
            if (cells == null || goals == null) return;

            var records = new[] { "ivanov", "petrov", "sidorov", "volkova", "kim" };
            var recordIndex = records.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);

            foreach (var cell in cells)
            {
                var token = cell.Occupant;
                if (token?.Kind != ContentKind.Data || string.IsNullOrEmpty(token.RecordId) || string.IsNullOrEmpty(token.FieldId))
                    continue;
                if (!recordIndex.TryGetValue(token.RecordId, out var index)) continue;
                token.Number = dataset.Value(token.FieldId, index);
            }

            var rebuilt = new List<ReportGoal>(goals.Count);
            foreach (var goal in goals)
            {
                rebuilt.Add(new ReportGoal(
                    goal.NameStringId,
                    Expected(goal.NameStringId, dataset),
                    goal.TargetRow,
                    goal.TargetColumn,
                    goal.ExpectedSourceIds,
                    goal.ExpectedDirectTokenId));
            }

            goals.Clear();
            goals.AddRange(rebuilt);
            RefreshAllMethod?.Invoke(prototype, null);
        }

        private static double Expected(string goalStringId, PrototypeLevelDataset dataset)
        {
            switch (goalStringId)
            {
                case "goal.salary": return dataset.Salary.Sum();
                case "goal.overtime": return dataset.Overtime.Sum();
                case "goal.bonus": return dataset.Bonus.Sum();
                case "goal.bonus5": return dataset.Bonus.Where(value => value >= 5d).Sum();
                case "goal.maxOvertimeSalary":
                {
                    var maxIndex = 0;
                    for (var i = 1; i < dataset.Overtime.Length; i++)
                        if (dataset.Overtime[i] > dataset.Overtime[maxIndex]) maxIndex = i;
                    return dataset.Salary[maxIndex];
                }
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
    }
}
