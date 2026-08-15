using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Playtest-facing usability layer. Keeps the corrected SUM rules and exposes an always-available
    /// reference guide. First-time onboarding is handled separately by PrototypeContextualTutorial.
    /// </summary>
    public sealed class PrototypePlaytestUsability : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly FieldInfo SelectionField = typeof(ExcelHellPrototype).GetField("selection", Flags);
        private static readonly FieldInfo PendingSumSourcesField = typeof(ExcelHellPrototype).GetField("pendingSumSources", Flags);
        private static readonly FieldInfo PendingSumField = typeof(ExcelHellPrototype).GetField("pendingSum", Flags);
        private static readonly FieldInfo AwaitingSumTargetField = typeof(ExcelHellPrototype).GetField("awaitingSumTarget", Flags);
        private static readonly FieldInfo StatusTextField = typeof(ExcelHellPrototype).GetField("statusText", Flags);
        private static readonly FieldInfo LocalizationField = typeof(ExcelHellPrototype).GetField("loc", Flags);
        private static readonly MethodInfo CanActMethod = typeof(ExcelHellPrototype).GetMethod("CanAct", Flags);

        private ExcelHellPrototype prototype;
        private Button sumButton;
        private bool referenceVisible;
        private int referencePage;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;

        public static bool TutorialOpen { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PrototypePlaytestUsability>() != null) return;
            var helper = new GameObject("EXCEL HELL Playtest Usability").AddComponent<PrototypePlaytestUsability>();
            DontDestroyOnLoad(helper.gameObject);
        }

        private void LateUpdate()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype)
                Bind(current);

            TutorialOpen = referenceVisible && prototype != null;
            SetSpreadsheetInput(!TutorialOpen);
        }

        private void Bind(ExcelHellPrototype owner)
        {
            if (prototype != null)
                SetSpreadsheetInput(true);

            prototype = owner;
            sumButton = null;
            referenceVisible = false;
            TutorialOpen = false;

            if (prototype == null) return;

            sumButton = prototype.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(button => button.gameObject.name == "ui.sum");
            if (sumButton != null)
            {
                sumButton.onClick.RemoveAllListeners();
                sumButton.onClick.AddListener(OnSum);
            }
        }

        private void OnSum()
        {
            if (prototype == null || referenceVisible) return;
            if (CanActMethod == null || !(bool)CanActMethod.Invoke(prototype, null)) return;

            var selection = SelectionField?.GetValue(prototype) as List<CellModel>;
            if (selection == null || selection.Count == 0)
            {
                SetStatus(Ru("SUM: выделите диапазон минимум с двумя числами.",
                    "SUM: select a range containing at least two numbers."));
                return;
            }

            if (selection.Any(cell => cell.State != CellState.Normal))
            {
                SetStatus(Ru("SUM не проходит через #REF! или уничтоженные клетки. Пустые обычные клетки игнорируются.",
                    "SUM cannot cross #REF! or destroyed cells. Normal empty cells are ignored."));
                return;
            }

            var occupied = selection.Where(cell => cell.Occupant != null).ToList();
            var invalidOccupied = occupied.Any(cell =>
                !cell.Occupant.IsNumeric ||
                (cell.Occupant.Kind != ContentKind.Data && cell.Occupant.Kind != ContentKind.Aggregate));
            if (invalidOccupied)
            {
                SetStatus(Ru("SUM принимает числа и агрегаты; ключи и подписи в диапазоне недопустимы.",
                    "SUM accepts numbers and aggregates; keys and labels cannot be inside the range."));
                return;
            }

            var numeric = occupied.Where(cell => cell.Occupant.IsNumeric).ToList();
            if (numeric.Count < 2)
            {
                SetStatus(Ru("SUM требует минимум две числовые клетки. Пустые клетки не считаются.",
                    "SUM requires at least two numeric cells. Empty cells do not count."));
                return;
            }

            var sum = numeric.Sum(cell => cell.Occupant.Number.Value);
            PendingSumSourcesField?.SetValue(prototype, numeric);
            PendingSumField?.SetValue(prototype, sum);
            AwaitingSumTargetField?.SetValue(prototype, true);

            var loc = Localization;
            SetStatus(loc != null
                ? loc.Format("ui.sumTarget", FormatNumber(sum))
                : $"SUM = {FormatNumber(sum)}");
        }

        private void SetSpreadsheetInput(bool enabled)
        {
            if (prototype == null) return;
            foreach (var raycaster in prototype.GetComponentsInChildren<GraphicRaycaster>(true))
                if (raycaster != null) raycaster.enabled = enabled;
        }

        private PrototypeLocalization Localization => LocalizationField?.GetValue(prototype) as PrototypeLocalization;

        private string Ru(string russian, string english) =>
            Localization?.Language == PrototypeLanguage.English ? english : russian;

        private void SetStatus(string value)
        {
            var status = StatusTextField?.GetValue(prototype) as Text;
            if (status != null) status.text = value;
        }

        private void CloseReference()
        {
            referenceVisible = false;
            TutorialOpen = false;
            SetSpreadsheetInput(true);
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (prototype == null) return;

            if (!referenceVisible)
            {
                var label = Ru("?  СПРАВКА", "?  HELP");
                if (GUI.Button(new Rect(18, 62, 150, 34), label, buttonStyle))
                {
                    referencePage = 0;
                    referenceVisible = true;
                    TutorialOpen = true;
                }
                return;
            }

            var width = Mathf.Min(760f, Screen.width - 80f);
            var height = Mathf.Min(500f, Screen.height - 80f);
            var rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            var previousColor = GUI.color;
            GUI.color = new Color(0.94f, 0.95f, 0.96f, 1f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;
            GUI.Box(rect, GUIContent.none);

            GUI.Label(new Rect(rect.x + 30, rect.y + 24, rect.width - 60, 42), ReferenceTitle(), titleStyle);
            GUI.Label(new Rect(rect.x + 32, rect.y + 82, rect.width - 64, rect.height - 170), ReferenceBody(), bodyStyle);
            GUI.Label(new Rect(rect.x + 32, rect.yMax - 74, 150, 28), $"{referencePage + 1} / 4", smallStyle);

            if (referencePage > 0 && GUI.Button(new Rect(rect.x + 190, rect.yMax - 70, 150, 40),
                    Ru("НАЗАД", "BACK"), buttonStyle))
                referencePage--;

            if (referencePage < 3)
            {
                if (GUI.Button(new Rect(rect.xMax - 190, rect.yMax - 70, 150, 40),
                        Ru("ДАЛЬШЕ", "NEXT"), buttonStyle))
                    referencePage++;
            }
            else if (GUI.Button(new Rect(rect.xMax - 220, rect.yMax - 70, 180, 40),
                         Ru("ЗАКРЫТЬ", "CLOSE"), buttonStyle))
                CloseReference();
        }

        private string ReferenceTitle()
        {
            var ru = new[]
            {
                "1. ЦЕЛЬ ИГРЫ",
                "2. SORT — СОБРАТЬ ДАННЫЕ",
                "3. SUM — ПОСЧИТАТЬ РЕЗУЛЬТАТ",
                "4. ОСТАЛЬНЫЕ ИНСТРУМЕНТЫ"
            };
            var en = new[]
            {
                "1. GAME GOAL",
                "2. SORT — ASSEMBLE DATA",
                "3. SUM — CALCULATE A RESULT",
                "4. OTHER TOOLS"
            };
            return Localization?.Language == PrototypeLanguage.English ? en[referencePage] : ru[referencePage];
        }

        private string ReferenceBody()
        {
            var ru = new[]
            {
                "Справа перечислены показатели отчёта. Собирайте нужные данные в рабочие диапазоны, вычисляйте результат и помещайте ответы в ЗЕЛЁНЫЕ клетки отчёта.\n\n" +
                "Выделение клеток ничего не тратит. Ход расходуется только успешным игровым действием.",

                "Выберите ОДНУ голубую клетку-ключ и нажмите SORT.\n\n" +
                "• Ключ параметра («Зарплата», «Часы»...) собирает значения параметра в столбец рядом с ключом.\n" +
                "• Ключ-фамилия собирает параметры сотрудника в строку.\n\n" +
                "<color=#B3261E><b>#SPILL!</b></color> означает, что SORT не может разместить полный диапазон: путь блокирует чужая клетка, повреждение или край листа.",

                "Выделите прямоугольный диапазон минимум с ДВУМЯ числами и нажмите SUM.\n\n" +
                "• Пустые обычные клетки SUM игнорирует.\n" +
                "• <color=#B3261E><b>#REF!</b></color> и уничтоженные клетки ломают диапазон.\n" +
                "• SUM на листе схлопывает числа и удаляет источники.\n" +
                "• SUM прямо в зелёную клетку отчёта сохраняет исходные числа.",

                "ВЫРЕЗАТЬ — забирает токен в буфер.\n" +
                "ВСТАВИТЬ — кладёт его в пустую обычную клетку.\n" +
                "УДАЛИТЬ — уничтожает клетку; уничтоженная клетка остаётся дырой. Этим же действием можно локализовать активный <color=#B3261E><b>#REF!</b></color>.\n" +
                "ОТПРАВИТЬ ОТЧЁТ — проверяет итоговые числа в зелёных полях.\n\n" +
                "Оранжевая клетка — заранее назначенная точка появления <color=#B3261E><b>#REF!</b></color>. После объявления цель уже не меняется."
            };

            var en = new[]
            {
                "The report requirements are listed on the right. Assemble the needed data into workable ranges, calculate results and place answers into the GREEN report cells.\n\n" +
                "Selection is free. Only successful gameplay actions spend a turn.",

                "Select ONE blue key cell and press SORT.\n\n" +
                "• A field key (Salary, Hours...) assembles that field into a column beside the key.\n" +
                "• An employee key assembles that employee's fields into a row.\n\n" +
                "<color=#B3261E><b>#SPILL!</b></color> means SORT cannot place its full span because another cell, damage or the worksheet edge blocks it.",

                "Select a rectangular range containing at least TWO numbers and press SUM.\n\n" +
                "• Normal empty cells are ignored.\n" +
                "• <color=#B3261E><b>#REF!</b></color> and destroyed cells break the range.\n" +
                "• Worksheet SUM collapses and consumes its sources.\n" +
                "• SUM directly into a green report cell preserves the source numbers.",

                "CUT moves a token into the clipboard.\n" +
                "PASTE places it into an empty normal cell.\n" +
                "DELETE destroys a cell and leaves a permanent hole. It can also quarantine an active <color=#B3261E><b>#REF!</b></color>.\n" +
                "SUBMIT REPORT checks the final numbers in the green fields.\n\n" +
                "An orange cell is a committed future <color=#B3261E><b>#REF!</b></color> spawn. Once announced, its coordinate no longer changes."
            };

            return Localization?.Language == PrototypeLanguage.English ? en[referencePage] : ru[referencePage];
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                richText = true
            };
            smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
        }

        private static string FormatNumber(double value) => Math.Abs(value % 1d) < 0.001d ? value.ToString("0") : value.ToString("0.##");
    }
}
