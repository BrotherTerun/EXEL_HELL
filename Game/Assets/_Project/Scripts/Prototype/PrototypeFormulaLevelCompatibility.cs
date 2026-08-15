using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Keeps MVP 0.5 authored levels isolated from legacy toolbar/tutorial/report assumptions.
    /// This adapter can be deleted with the experiment without changing the frozen prototype core.
    /// </summary>
    [DefaultExecutionOrder(1050)]
    public sealed class PrototypeFormulaLevelCompatibility : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly FieldInfo ReportColumnField = typeof(ExcelHellPrototype).GetField("reportColumn", Flags);
        private static readonly MethodInfo RefreshAllMethod = typeof(ExcelHellPrototype).GetMethod("RefreshAll", Flags);
        private static readonly MethodInfo InitializeAnomalyMethod = typeof(ExcelHellPrototype).GetMethod("InitializeAnomaly", Flags);
        private static readonly FieldInfo RefCommittedField = typeof(PrototypeRefSpawnCommitment).GetField("committed", Flags);

        private ExcelHellPrototype appliedTo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PrototypeFormulaLevelCompatibility>() != null) return;
            var helper = new GameObject("EXEL HELL Formula Level Compatibility")
                .AddComponent<PrototypeFormulaLevelCompatibility>();
            DontDestroyOnLoad(helper.gameObject);
        }

        private void LateUpdate()
        {
            var prototype = FindFirstObjectByType<ExcelHellPrototype>();
            if (prototype == null || prototype == appliedTo) return;

            var level = PrototypeLevelRuntime.Current;
            if (level?.FormulaCellsEnabled != true)
            {
                appliedTo = prototype;
                return;
            }

            HideLegacyFormulaButtons(prototype);
            DisableLegacyTutorial();
            RestoreThreatSemantics(prototype, level);
            UnlockFormulaReportTargets(prototype, level);
            RefreshAllMethod?.Invoke(prototype, null);
            appliedTo = prototype;
        }

        private static void HideLegacyFormulaButtons(ExcelHellPrototype prototype)
        {
            foreach (var button in prototype.GetComponentsInChildren<Button>(true))
            {
                if (button.gameObject.name == "ui.sum" || button.gameObject.name == "ui.sort")
                    button.gameObject.SetActive(false);
            }
        }

        private static void DisableLegacyTutorial()
        {
            var tutorial = FindFirstObjectByType<PrototypeContextualTutorial>();
            if (tutorial != null) tutorial.enabled = false;
        }

        private static void RestoreThreatSemantics(ExcelHellPrototype prototype, PrototypeLevelConfig level)
        {
            var cells = CellsField?.GetValue(prototype) as CellModel[,];
            if (cells == null) return;

            if ((level.ReportGoals & PrototypeReportGoals.SalaryForHoursBelowForty) != 0)
            {
                foreach (var cell in cells)
                    if (cell.Occupant?.Kind == ContentKind.Data && cell.Occupant.FieldId == "hours")
                        cell.Occupant.IsRequiredSource = true;
            }

            if ((level.ReportGoals & PrototypeReportGoals.SalaryOfMaxOvertime) != 0)
            {
                foreach (var cell in cells)
                    if (cell.Occupant?.Kind == ContentKind.Data && cell.Occupant.FieldId == "overtime")
                        cell.Occupant.IsRequiredSource = true;
            }
        }

        private static void UnlockFormulaReportTargets(ExcelHellPrototype prototype, PrototypeLevelConfig level)
        {
            // Legacy core protects the whole report column from #REF! through IsReportInterfaceCell.
            // Formula-cell rules require report SUM coordinates to be ordinary vulnerable worksheet fields.
            // Setting the legacy reportColumn sentinel outside the board removes only that old protection:
            // goal lookup/green presentation still use ReportGoal coordinates, and player DELETE remains blocked by FormulaCells.
            ReportColumnField?.SetValue(prototype, -1);

            if (!level.RefEnabled) return;

            // Re-plan the first outbreak after removing legacy report protection. The commitment helper ran earlier
            // (execution order 700), so release its temporary commitment; it will commit the new authored intent next frame.
            InitializeAnomalyMethod?.Invoke(prototype, null);
            var commitment = FindFirstObjectByType<PrototypeRefSpawnCommitment>();
            if (commitment != null) RefCommittedField?.SetValue(commitment, false);
        }
    }
}
