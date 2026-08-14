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
                ["ui.title"] = "EXEL HELL // MVP 0.4",
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
                ["ui.select"] = "Выберите данные или ключ. SORT собирает связанную группу; SUM работает минимум с двумя числами.",
                ["ui.sumNeedRange"] = "SUM нужен выделенный числовой диапазон.",
                ["ui.sumNeedTwo"] = "SUM требует минимум две числовые клетки.",
                ["ui.sumInvalid"] = "SUM принимает только обычные числовые токены и агрегаты.",
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
                ["ui.quarantineDone"] = "Очаг #REF! в {0} помещён в карантин. Распад документа продолжается.",
                ["ui.finishSum"] = "Сначала выберите клетку для результата SUM.",
                ["ui.finished"] = "Уровень завершён. Нажмите СБРОС.",
                ["ui.deadline"] = "ДЕДЛАЙН ПРОПУЩЕН. Нажмите СБРОС.",
                ["ui.accepted"] = "ОТЧЁТ ПРИНЯТ на ходу {0}/{1}.",
                ["ui.rejected"] = "ОТЧЁТ ОТКЛОНЁН: {0}",
                ["ui.goal"] = "{0}: {1} / {2} → {3}",
                ["ui.noGoals"] = "В Config не выбрано ни одной задачи отчёта.",
                ["ui.help"] = "Зелёные клетки — поля отчёта. SORT собирает связанные данные.\nSUM в таблице поглощает диапазон; SUM прямо в отчёт считает ответ без уничтожения источников.\nSUBMIT проверяет только итоговые числа. DELETE локализует один #REF!, но вспышки продолжаются.\nОранжевый = Intent, красный = #REF!, чёрный = Destroyed; синяя рамка = выделение.",

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
                ["ui.title"] = "EXEL HELL // MVP 0.4",
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
                ["ui.select"] = "Select data or a key. SORT assembles a semantic group; SUM needs at least two numbers.",
                ["ui.sumNeedRange"] = "SUM needs a selected numeric range.",
                ["ui.sumNeedTwo"] = "SUM requires at least two numeric cells.",
                ["ui.sumInvalid"] = "SUM accepts only normal numeric tokens and aggregates.",
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
                ["ui.quarantineDone"] = "#REF! at {0} quarantined. Document decay continues.",
                ["ui.finishSum"] = "Finish SUM by picking its result cell.",
                ["ui.finished"] = "Run finished. Press RESET.",
                ["ui.deadline"] = "DEADLINE MISSED. Press RESET.",
                ["ui.accepted"] = "REPORT ACCEPTED on turn {0}/{1}.",
                ["ui.rejected"] = "REPORT REJECTED: {0}",
                ["ui.goal"] = "{0}: {1} / {2} → {3}",
                ["ui.noGoals"] = "No report goals selected in Config.",
                ["ui.help"] = "Green cells are report targets. SORT assembles related data.\nSUM in worksheet space consumes its range; SUM directly into a report cell calculates without consuming sources.\nSUBMIT checks final numbers only. DELETE quarantines one #REF!, but outbreaks continue.\nOrange = Intent, red = #REF!, black = Destroyed; blue outline = selection.",

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
