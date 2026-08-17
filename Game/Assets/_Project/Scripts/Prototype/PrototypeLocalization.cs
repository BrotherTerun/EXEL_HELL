using System.Collections.Generic;

namespace ExcelHell.Prototype
{
    public enum PrototypeLanguage
    {
        Russian,
        English
    }

    public sealed class PrototypeLocalization
    {
        private readonly Dictionary<PrototypeLanguage, Dictionary<string, string>> tables = new()
        {
            [PrototypeLanguage.Russian] = new Dictionary<string, string>
            {
                ["ui.title"] = "EXCEL HELL // MVP 0.5",
                ["ui.reportTask"] = "ЗАДАЧА ОТЧЁТА",
                ["ui.sum"] = "SUM",
                ["ui.sort"] = "SORT",
                ["ui.cut"] = "ВЫРЕЗАТЬ",
                ["ui.paste"] = "ВСТАВИТЬ",
                ["ui.delete"] = "УДАЛИТЬ",
                ["ui.submit"] = "ОТПРАВИТЬ ОТЧЁТ",
                ["ui.reset"] = "СБРОС",
                ["ui.language"] = "EN",
                ["ui.turn"] = "ХОД {0}/{1}",
                ["ui.clipboard"] = "БУФЕР: {0}",
                ["ui.empty"] = "—",
                ["ui.refNext"] = "СЛЕДУЮЩИЙ #REF! → {0}",
                ["ui.refNoPath"] = "#REF! изолирован — ожидается следующая вспышка",
                ["ui.refSpawn"] = "НОВЫЙ #REF! → {0} через {1} ход(а)",
                ["ui.spill"] = "#SPILL! Недостаточно непрерывного свободного места.",
                ["ui.select"] = "Drag перемещает; Shift+Drag выделяет диапазон. Перетащите аргумент прямо в =SORT() или =SUM().",
                ["ui.sumNeedRange"] = "SUM нужен выделенный диапазон минимум с двумя числами.",
                ["ui.sumNeedTwo"] = "SUM требует минимум две числовые клетки; обычные пустые клетки игнорируются.",
                ["ui.sumInvalid"] = "SUM игнорирует обычные пустые клетки, но не проходит через #REF!, Destroyed, ключи или подписи.",
                ["ui.sumTarget"] = "SUM = {0}. Выберите пустую клетку для результата.",
                ["ui.sumBadTarget"] = "Результат SUM можно поместить только в пустую обычную клетку.",
                ["ui.sumDone"] = "SUM схлопнул {0} значений в {1}.",
                ["ui.sumReported"] = "SUM записал результат из {0} значений в отчёт {1}; источники сохранены.",
                ["ui.sortNeedKey"] = "SORT требует ровно один ключ записи или параметра.",
                ["ui.sortDone"] = "SORT собрал {0} значений вокруг ключа {1}{2}.",
                ["ui.sortFallback"] = " (альтернативное направление)",
                ["ui.cutNeed"] = "ВЫРЕЗАТЬ требует один непустой обычный токен.",
                ["ui.cutDone"] = "{0} → буфер.",
                ["ui.pasteEmpty"] = "Буфер пуст.",
                ["ui.pasteTarget"] = "ВСТАВИТЬ можно только в пустую обычную клетку.",
                ["ui.pasteDone"] = "Вставлено в {0}.",
                ["ui.deleteNeed"] = "УДАЛИТЬ требует одну клетку.",
                ["ui.deleteDone"] = "Клетка {0} уничтожена.",
                ["ui.deleteReportProtected"] = "Структуру отчёта удалить нельзя.",
                ["ui.quarantineDone"] = "Очаг #REF! в {0} помещён в карантин. Распад документа продолжается.",
                ["ui.finishSum"] = "Сначала завершите текущую операцию SUM.",
                ["ui.finished"] = "Уровень завершён. Нажмите СБРОС.",
                ["ui.deadline"] = "ДЕДЛАЙН ПРОПУЩЕН. Нажмите СБРОС.",
                ["ui.accepted"] = "ОТЧЁТ ПРИНЯТ на ходу {0}/{1}.",
                ["ui.rejected"] = "ОТЧЁТ ОТКЛОНЁН: {0}",
                ["ui.goal"] = "{0}: {1} / {2} → {3}",
                ["ui.noGoals"] = "В Config не выбрано ни одной задачи отчёта.",
                ["ui.help"] = "Drag = MOVE. Shift+Drag = выделение прямоугольного диапазона. DELETE остаётся кнопкой.\nПеретащите ключ в =SORT(); выделите минимум две числовые клетки и перетащите диапазон в =SUM(). Обычные пустые клетки SUM игнорирует; #REF!, Destroyed, ключи и подписи делают диапазон недопустимым.\nЧтобы считать прямо в ячейку отчёта, сначала перенесите пустую =SUM() в нужную report-cell, затем перетащите на неё диапазон. Уже заполненные report-cells можно использовать в SUM как операнды: их значения читаются, но не исчезают.\nПустые =SUM() и =SORT() можно переносить. Если формула уже содержит результат или ключ, сначала вынесите содержимое, затем переносите саму формулу. MOVE проверяет только конечные клетки: можно двигать по диагонали и поверх занятых клеток.\nОранжевый = Intent, красный = #REF!, чёрный = Destroyed; синяя рамка = выделение.",

                ["record.ivanov"] = "Иванов",
                ["record.petrov"] = "Петров",
                ["record.sidorov"] = "Сидоров",
                ["record.volkova"] = "Волкова",
                ["record.kim"] = "Ким",
                ["field.hours"] = "Часы",
                ["field.salary"] = "Зарплата",
                ["field.overtime"] = "Переработки",
                ["field.bonus"] = "Премия",
                ["label.report"] = "ОТЧЁТ",
                ["goal.salary"] = "Итого зарплата",
                ["goal.overtime"] = "Итого переработки",
                ["goal.bonus"] = "Итого премии",
                ["goal.bonus5"] = "Премии ≥ 5",
                ["goal.maxOvertimeSalary"] = "Зарплата сотрудника с max переработкой",
                ["goal.lowHoursSalary"] = "Зарплаты сотрудников с часами < 40"
            },
            [PrototypeLanguage.English] = new Dictionary<string, string>
            {
                ["ui.title"] = "EXCEL HELL // MVP 0.5",
                ["ui.reportTask"] = "REPORT TASK",
                ["ui.sum"] = "SUM",
                ["ui.sort"] = "SORT",
                ["ui.cut"] = "CUT",
                ["ui.paste"] = "PASTE",
                ["ui.delete"] = "DELETE",
                ["ui.submit"] = "SUBMIT REPORT",
                ["ui.reset"] = "RESET",
                ["ui.language"] = "RU",
                ["ui.turn"] = "TURN {0}/{1}",
                ["ui.clipboard"] = "CLIPBOARD: {0}",
                ["ui.empty"] = "—",
                ["ui.refNext"] = "NEXT #REF! → {0}",
                ["ui.refNoPath"] = "#REF! isolated — waiting for the next outbreak",
                ["ui.refSpawn"] = "NEW #REF! → {0} in {1} turn(s)",
                ["ui.spill"] = "#SPILL! Not enough contiguous free space.",
                ["ui.select"] = "Drag moves; Shift+Drag selects a range. Drop the argument directly into =SORT() or =SUM().",
                ["ui.sumNeedRange"] = "SUM needs a selected range containing at least two numbers.",
                ["ui.sumNeedTwo"] = "SUM requires at least two numeric cells; normal blanks are ignored.",
                ["ui.sumInvalid"] = "SUM ignores normal blanks, but cannot cross #REF!, Destroyed cells, keys, or labels.",
                ["ui.sumTarget"] = "SUM = {0}. Pick an empty cell for the result.",
                ["ui.sumBadTarget"] = "SUM result needs an empty Normal cell.",
                ["ui.sumDone"] = "SUM collapsed {0} values into {1}.",
                ["ui.sumReported"] = "SUM wrote {0} values into report cell {1}; sources were preserved.",
                ["ui.sortNeedKey"] = "SORT needs exactly one record or field key.",
                ["ui.sortDone"] = "SORT assembled {0} values around {1}{2}.",
                ["ui.sortFallback"] = " (fallback direction)",
                ["ui.cutNeed"] = "CUT needs one non-empty Normal token.",
                ["ui.cutDone"] = "{0} → Clipboard.",
                ["ui.pasteEmpty"] = "Clipboard is empty.",
                ["ui.pasteTarget"] = "PASTE needs an empty Normal cell.",
                ["ui.pasteDone"] = "Pasted into {0}.",
                ["ui.deleteNeed"] = "DELETE needs one cell.",
                ["ui.deleteDone"] = "Cell {0} destroyed.",
                ["ui.deleteReportProtected"] = "Report structure cannot be deleted.",
                ["ui.quarantineDone"] = "#REF! at {0} quarantined. Document decay continues.",
                ["ui.finishSum"] = "Finish the current SUM operation first.",
                ["ui.finished"] = "Run finished. Press RESET.",
                ["ui.deadline"] = "DEADLINE MISSED. Press RESET.",
                ["ui.accepted"] = "REPORT ACCEPTED on turn {0}/{1}.",
                ["ui.rejected"] = "REPORT REJECTED: {0}",
                ["ui.goal"] = "{0}: {1} / {2} → {3}",
                ["ui.noGoals"] = "No report goals selected in Config.",
                ["ui.help"] = "Drag = MOVE. Shift+Drag = rectangular range selection. DELETE remains a button.\nDrop a key into =SORT(); select at least two numeric cells and drop the range into =SUM(). SUM ignores normal blanks; #REF!, Destroyed cells, keys, and labels make the range invalid.\nTo calculate directly into a report cell, first move an empty =SUM() onto the target report cell, then drop the selected range onto it. Filled report cells may be used as SUM operands: their values are read but remain in place.\nEmpty =SUM() and =SORT() formulas can be moved. If a formula already contains a result or key, move that content out first, then move the empty formula itself. MOVE checks destination cells only, so diagonal moves and crossing occupied cells are allowed.\nOrange = Intent, red = #REF!, black = Destroyed; blue outline = selection.",

                ["record.ivanov"] = "Ivanov",
                ["record.petrov"] = "Petrov",
                ["record.sidorov"] = "Sidorov",
                ["record.volkova"] = "Volkova",
                ["record.kim"] = "Kim",
                ["field.hours"] = "Hours",
                ["field.salary"] = "Salary",
                ["field.overtime"] = "Overtime",
                ["field.bonus"] = "Bonus",
                ["label.report"] = "REPORT",
                ["goal.salary"] = "Salary total",
                ["goal.overtime"] = "Overtime total",
                ["goal.bonus"] = "Bonus total",
                ["goal.bonus5"] = "Bonuses ≥ 5",
                ["goal.maxOvertimeSalary"] = "Salary of employee with max overtime",
                ["goal.lowHoursSalary"] = "Salaries of employees with hours < 40"
            }
        };

        public PrototypeLanguage Language { get; private set; } = PrototypeLanguage.Russian;

        public void ToggleLanguage()
        {
            Language = Language == PrototypeLanguage.Russian ? PrototypeLanguage.English : PrototypeLanguage.Russian;
        }

        public string Get(string id)
        {
            return tables[Language].TryGetValue(id, out var value) ? value : id;
        }

        public string Format(string id, params object[] args)
        {
            return string.Format(Get(id), args);
        }
    }
}
