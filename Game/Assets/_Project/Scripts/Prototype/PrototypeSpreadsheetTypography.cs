using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Visual-only typography pass for the production worksheet.
    /// Numeric cells get the largest treatment; labels stay smaller to preserve long Russian headers.
    /// </summary>
    [DefaultExecutionOrder(1990)]
    public sealed class PrototypeSpreadsheetTypography : MonoBehaviour
    {
        private ExcelHellPrototype prototype;
        private RectTransform spreadsheet;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeSpreadsheetTypography>() != null) return;
            var root = new GameObject("[PRESENTATION] Spreadsheet Typography");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeSpreadsheetTypography>();
        }

        private void LateUpdate()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype)
            {
                prototype = current;
                spreadsheet = null;
            }
            if (prototype == null) return;

            if (spreadsheet == null)
            {
                spreadsheet = prototype.GetComponentsInChildren<RectTransform>(true)
                    .FirstOrDefault(rect => rect.gameObject.name == "Spreadsheet");
            }
            if (spreadsheet == null) return;

            foreach (var text in spreadsheet.GetComponentsInChildren<Text>(true))
                Style(text);
        }

        private static void Style(Text text)
        {
            if (text == null) return;
            text.font = PrototypeVisualTheme.MonoFont;

            var parentName = text.transform.parent != null ? text.transform.parent.name : string.Empty;
            var value = (text.text ?? string.Empty).Trim();

            if (parentName == "Report Goal Caption")
            {
                text.fontSize = 11;
                text.fontStyle = FontStyle.Bold;
                return;
            }

            if (parentName.StartsWith("Cell Message"))
            {
                text.fontSize = 18;
                text.fontStyle = FontStyle.Bold;
                return;
            }

            if (parentName == "Header")
            {
                text.fontSize = 20;
                text.fontStyle = FontStyle.Bold;
                return;
            }

            if (parentName == "Formula 2.0 Interaction")
            {
                text.fontSize = 22;
                text.fontStyle = FontStyle.Bold;
                return;
            }

            if (value == "#REF!" || value == "×")
            {
                text.fontSize = 24;
                text.fontStyle = FontStyle.Bold;
                return;
            }

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _) ||
                double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out _))
            {
                text.fontSize = 30;
                text.fontStyle = FontStyle.Bold;
                return;
            }

            text.fontSize = 19;
            text.fontStyle = FontStyle.Bold;
        }
    }
}
