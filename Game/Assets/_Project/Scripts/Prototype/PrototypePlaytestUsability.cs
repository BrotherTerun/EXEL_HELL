using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    public sealed class PrototypePlaytestUsability : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly FieldInfo SelectionField = typeof(ExcelHellPrototype).GetField("selection", Flags);
        private static readonly FieldInfo PendingSumSourcesField = typeof(ExcelHellPrototype).GetField("pendingSumSources", Flags);
        private static readonly FieldInfo PendingSumField = typeof(ExcelHellPrototype).GetField("pendingSum", Flags);
        private static readonly FieldInfo AwaitingSumTargetField = typeof(ExcelHellPrototype).GetField("awaitingSumTarget", Flags);
        private static readonly FieldInfo StatusTextField = typeof(ExcelHellPrototype).GetField("statusText", Flags);
        private static readonly FieldInfo LocalizationField = typeof(ExcelHellPrototype).GetField("loc", Flags);
        private static readonly FieldInfo ElapsedSecondsField = typeof(ExcelHellPrototype).GetField("elapsedSeconds", Flags);
        private static readonly FieldInfo RemainingSecondsField = typeof(ExcelHellPrototype).GetField("remainingSeconds", Flags);
        private static readonly MethodInfo CanActMethod = typeof(ExcelHellPrototype).GetMethod("CanAct", Flags);

        private ExcelHellPrototype prototype;
        private Button sumButton;
        private bool tutorialVisible;
        private bool tutorialShownThisSession;
        private int tutorialPage;
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

            if (prototype == null)
            {
                TutorialOpen = false;
                return;
            }

            var levelOne = PrototypeLevelRuntime.CurrentIndex == 0;
            if (!levelOne && tutorialVisible)
                CloseTutorial();

            if (tutorialVisible)
            {
                TutorialOpen = true;
                SetSpreadsheetInput(false);
                CompensateRealtimeClock();
            }
            else
            {
                TutorialOpen = false;
                SetSpreadsheetInput(true);
            }
        }

        private void Bind(ExcelHellPrototype owner)
        {
            if (prototype != null)
                SetSpreadsheetInput(true);

            prototype = owner;
            sumButton = null;
            TutorialOpen = false;

            if (prototype == null) return;

            sumButton = prototype.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(button => button.gameObject.name == "ui.sum");
            if (sumButton != null)
            {
                sumButton.onClick.RemoveAllListeners();
                sumButton.onClick.AddListener(OnSum);
            }

            if (PrototypeLevelRuntime.CurrentIndex == 0 && !tutorialShownThisSession)
            {
                tutorialShownThisSession = true;
                tutorialPage = 0;
                tutorialVisible = true;
                TutorialOpen = true;
            }
        }

        private void OnSum()
        {
            if (prototype == null || tutorialVisible) return;
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

        private void CompensateRealtimeClock()
        {
            if (ElapsedSecondsField == null || RemainingSecondsField == null) return;

            if (ElapsedSecondsField.GetValue(prototype) is float elapsed)
                ElapsedSecondsField.SetValue(prototype, Mathf.Max(0f, elapsed - Time.unscaledDeltaTime));
            if (RemainingSecondsField.GetValue(prototype) is float remaining)
                RemainingSecondsField.SetValue(prototype, remaining + Time.unscaledDeltaTime);
        }

        private void SetSpreadsheetInput(bool enabled)
        {
            if (prototype == null) return;
            foreach (var raycaster in prototype.GetComponentsInChildren<GraphicRaycaster>(true))
                if (raycaster != null) raycaster.enabled = enabled;
        }

        private PrototypeLocalization Localization => LocalizationField?.GetValue(prototype) as PrototypeLocalization;

        private string Ru(string russian, string english)
        {
            return Localization?.Language == PrototypeLanguage.English ? english : russian;
        }

        private void SetStatus(string value)
        {
            var status = StatusTextField?.GetValue(prototype) as Text;
            if (status != null) status.text = value;
        }

        private void CloseTutorial()
        {
            tutorialVisible = false;
            TutorialOpen = false;
            SetSpreadsheetInput(true);
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (PrototypeLevelRuntime.CurrentIndex != 0 || prototype == null) return;

            if (!tutorialVisible)
            {
                var label = Ru("?  ОБУЧЕНИЕ", "?  TUTORIAL");
                if (GUI.Button(new Rect(18, 62, 150, 34), label, buttonStyle))
                {
                    tutorialPage = 0;
                    tutorialVisible = true;
                    TutorialOpen = true;
                }
                return;
            }

            var width = Mathf.Min(760f, Screen.width - 80f);
            var height = Mathf.Min(500f, Screen.height - 80f);
            var rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(rect, GUIContent.none);

            GUI.Label(new Rect(rect.x + 30, rect.y + 24, rect.width - 60, 42), TutorialTitle(), titleStyle);
            GUI.Label(new Rect(rect.x + 32, rect.y + 82, rect.width - 64, rect.height - 170), TutorialBody(), bodyStyle);
            GUI.Label(new Rect(rect.x + 32, rect.yMax - 74, 150, 28), $"{tutorialPage + 1} / 4", smallStyle);

            if (tutorialPage > 0 && GUI.Button(new Rect(rect.x + 190, rect.yMax - 70, 150, 40),
                    Ru("НАЗАД", "BACK"), buttonStyle))
                tutorialPage--;

            if (tutorialPage < 3)
            {
                if (GUI.Button(new Rect(rect.xMax - 190, rect.yMax - 70, 150, 40),
                        Ru("ДАЛЬШЕ", "NEXT"), buttonStyle))
                    tutorialPage++;
            }
            else if (GUI.Button(new Rect(rect.xMax - 220, rect.yMax - 70, 180, 40),
                         Ru("НАЧАТЬ", "START"), buttonStyle))
            {
                CloseTutorial();
            }
        }

        private string TutorialTitle()
        {
            var ru = new[] { "1. ЧТО НУЖНО СДЕЛАТЬ", "2. SORT — СОБРАТЬ ДАННЫЕ", "3. SUM — ПОСЧИТАТЬ РЕЗУЛЬТАТ", "4. ОСТАЛЬНЫЕ ИНСТРУМЕНТЫ" };
            var en = new[] { "1. WHAT YOU NEED TO DO", "2. SORT — ASSEMBLE DATA", "3. SUM — CALCULATE A RESULT", "4. OTHER TOOLS" };
            return Localization?.Language == PrototypeLanguage.English ? en[tutorialPage] : ru[tutorialPage];
        }

        private string TutorialBody()
        {
            var ru = new[]
            {
                "Справа перечислены показатели, которые требует отчёт.\n\nСоберите нужные числа из данных таблицы и поместите ответы в соответствующие ЗЕЛЁНЫЕ клетки отчёта. Затем нажмите «ОТПРАВИТЬ ОТЧЁТ».\n\nНа первом уровне аномалий нет: здесь можно спокойно разобраться с инструментами. Выделение клеток и диапазонов само по себе ничего не тратит.",
                "SORT здесь работает не как обычная сортировка Excel.\n\nВыберите ОДНУ голубую клетку-ключ и нажмите SORT.\n\n• Ключ параметра («Зарплата», «Часы»...) собирает все значения этого параметра в столбец рядом с ключом — по порядку сотрудников.\n• Ключ-фамилия собирает все параметры одного сотрудника в строку.\n\nЕсли впереди не хватает непрерывного места, SORT попробует собрать группу с другой стороны. Если мешает чужая клетка или повреждение — получите #SPILL!.",
                "Выделите прямоугольный диапазон, в котором есть МИНИМУМ ДВА числа, и нажмите SUM. Затем выберите пустую клетку для результата.\n\n• Пустые обычные клетки внутри диапазона SUM игнорирует.\n• #REF! или уничтоженная клетка ломают диапазон — через них SUM не работает.\n• На обычном листе SUM СХЛОПЫВАЕТ исходные числа в один результат и удаляет источники.\n• Если целью выбрана зелёная клетка ОТЧЁТА, результат записывается туда, а исходные числа сохраняются.",
                "ВЫРЕЗАТЬ — забирает один обычный токен в буфер.\nВСТАВИТЬ — кладёт токен из буфера в пустую обычную клетку.\nУДАЛИТЬ — уничтожает клетку. Это НЕ пустая клетка: SUM через уничтоженное место не проходит. Позже УДАЛИТЬ можно локализовать #REF!.\nОТПРАВИТЬ ОТЧЁТ — проверяет только итоговые числа в зелёных полях.\n\nЗелёная область отчёта защищена от удаления и #REF!. К памятке можно вернуться кнопкой «? ОБУЧЕНИЕ»."
            };

            var en = new[]
            {
                "The report requirements are listed on the right.\n\nDerive the required numbers from the worksheet and put each answer into its GREEN report cell. Then press SUBMIT REPORT.\n\nLevel 1 has no anomalies: use it to learn the tools. Selecting cells or ranges costs nothing by itself.",
                "SORT does NOT behave like normal Excel sorting here.\n\nSelect ONE blue key cell and press SORT.\n\n• A field key (Salary, Hours...) assembles that field into a column beside the key, in employee order.\n• An employee-name key assembles that employee's fields into a row.\n\nIf there is no continuous space ahead, SORT tries the other side. Foreign cells or damage can cause #SPILL!.",
                "Select a rectangular range containing AT LEAST TWO numbers, press SUM, then choose an empty result cell.\n\n• Normal empty cells inside the range are ignored.\n• #REF! or destroyed cells break the range; SUM cannot cross them.\n• On the worksheet, SUM COLLAPSES its source numbers into one result and consumes the sources.\n• If the target is a green REPORT cell, the answer is written there without consuming the source numbers.",
                "CUT — moves one normal token to the clipboard.\nPASTE — places the clipboard token into an empty normal cell.\nDELETE — destroys a cell. It is NOT a blank: SUM cannot cross a destroyed cell. Later DELETE can quarantine #REF!.\nSUBMIT REPORT — checks only the final numbers in the green fields.\n\nThe green report interface is protected from DELETE and #REF!. Reopen this guide with the ? TUTORIAL button."
            };

            return Localization?.Language == PrototypeLanguage.English ? en[tutorialPage] : ru[tutorialPage];
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.UpperLeft, wordWrap = true, richText = false };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold };
        }

        private static string FormatNumber(double value) => Math.Abs(value % 1d) < 0.001d ? value.ToString("0") : value.ToString("0.##");
    }
}
