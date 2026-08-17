using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// GridLayoutGroup treats every direct Spreadsheet child as another worksheet cell.
    /// Psychosis v2 needs full-sheet overlay roots, so mark those roots as layout-ignored and restore their stretch
    /// rect after creation. This is presentation-only and does not touch the worksheet model or cell transforms.
    /// </summary>
    [DefaultExecutionOrder(2190)]
    public sealed class PrototypeSpreadsheetPsychosisLayoutGuard : MonoBehaviour
    {
        private RectTransform spreadsheet;
        private int lastFixedCount = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeSpreadsheetPsychosisLayoutGuard>() != null) return;
            var root = new GameObject("[PRESENTATION] Spreadsheet Psychosis Layout Guard");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeSpreadsheetPsychosisLayoutGuard>();
        }

        private void LateUpdate()
        {
            if (PrototypeAuthoringMode.Active) return;

            if (spreadsheet == null)
            {
                var prototype = FindFirstObjectByType<ExcelHellPrototype>();
                spreadsheet = prototype == null
                    ? null
                    : prototype.GetComponentsInChildren<RectTransform>(true)
                        .FirstOrDefault(r => r != null && r.gameObject.name == "Spreadsheet");
                lastFixedCount = -1;
            }

            if (spreadsheet == null) return;

            var fixedCount = 0;
            for (var i = 0; i < spreadsheet.childCount; i++)
            {
                var child = spreadsheet.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.name.StartsWith("Psychosis v2 ")) continue;

                var element = child.GetComponent<LayoutElement>();
                if (element == null) element = child.gameObject.AddComponent<LayoutElement>();
                element.ignoreLayout = true;

                // GridLayoutGroup may already have driven the rect earlier in the frame; restore full-sheet geometry.
                child.anchorMin = Vector2.zero;
                child.anchorMax = Vector2.one;
                child.pivot = spreadsheet.pivot;
                child.anchoredPosition = Vector2.zero;
                child.sizeDelta = Vector2.zero;
                child.offsetMin = Vector2.zero;
                child.offsetMax = Vector2.zero;
                child.SetAsLastSibling();
                fixedCount++;
            }

            if (fixedCount != lastFixedCount)
            {
                if (fixedCount > 0)
                    Debug.Log($"[PSYCHOSIS/V2] LayoutGuard fixed {fixedCount} full-sheet overlay root(s); Spreadsheet GridLayoutGroup ignored them.");
                lastFixedCount = fixedCount;
            }
        }
    }
}
