using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Keeps MVP 0.5 authored levels isolated from legacy toolbar/tutorial assumptions.
    /// This adapter can be deleted with the experiment without changing the frozen prototype core.
    /// </summary>
    [DefaultExecutionOrder(1050)]
    public sealed class PrototypeFormulaLevelCompatibility : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly MethodInfo RefreshAllMethod = typeof(ExcelHellPrototype).GetMethod("RefreshAll", Flags);

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

            // Reset to the required flags authored by report-goal provenance first.
            // Then add semantic lookup data: these tokens are not part of the final arithmetic result,
            // but losing them can make the report goal unsolvable because the player can no longer identify the records.
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
    }
}
