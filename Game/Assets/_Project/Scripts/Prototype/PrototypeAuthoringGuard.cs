using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Constructor scene is a spatial authoring sandbox, not a playable level.
    /// Keep core interaction available, but continuously neutralize turn/deadline/#REF state.
    /// </summary>
    [DefaultExecutionOrder(2000)]
    public sealed class PrototypeAuthoringGuard : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo TurnField = typeof(ExcelHellPrototype).GetField("turn", Flags);
        private static readonly FieldInfo FinishedField = typeof(ExcelHellPrototype).GetField("finished", Flags);
        private static readonly FieldInfo PendingSpawnField = typeof(ExcelHellPrototype).GetField("pendingSpawnIntent", Flags);
        private static readonly FieldInfo CurrentIntentField = typeof(ExcelHellPrototype).GetField("currentIntent", Flags);
        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly FieldInfo TurnTextField = typeof(ExcelHellPrototype).GetField("turnText", Flags);
        private static readonly FieldInfo IntentTextField = typeof(ExcelHellPrototype).GetField("intentText", Flags);

        private ExcelHellPrototype prototype;

        private void LateUpdate()
        {
            if (!PrototypeAuthoringMode.Active) return;
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) prototype = current;
            if (prototype == null) return;

            TurnField?.SetValue(prototype, 0);
            FinishedField?.SetValue(prototype, false);
            PendingSpawnField?.SetValue(prototype, null);
            CurrentIntentField?.SetValue(prototype, null);

            var cells = CellsField?.GetValue(prototype) as CellModel[,];
            if (cells != null)
            {
                foreach (var cell in cells)
                {
                    if (cell.State != CellState.Normal) cell.State = CellState.Normal;
                    cell.CorruptionAge = 0;
                    if (cell.Occupant != null) cell.Occupant.IsAccessible = true;
                }
            }

            var turnText = TurnTextField?.GetValue(prototype) as Text;
            if (turnText != null) turnText.gameObject.SetActive(false);

            var intentText = IntentTextField?.GetValue(prototype) as Text;
            if (intentText != null)
            {
                intentText.gameObject.SetActive(true);
                intentText.text = "AUTHORING MODE — #REF! DISABLED";
            }
        }
    }
}
