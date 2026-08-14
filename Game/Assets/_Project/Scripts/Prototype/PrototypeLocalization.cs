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
                ["ui.title"] = "EXEL HELL // ПРОТОТИП",
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
                ["ui.refDormant"] = "#REF! неактивен — проснётся после хода {0}",
                ["ui.refNext"] = "СЛЕДУЮЩИЙ #REF! → {0}",
                ["ui.refNoPath"] = "СЛЕДУЮЩИЙ #REF! → нет пути",
                ["ui.spill"] = "#SPILL! Недостаточно непрерывного свободного места.",
                ["ui.select"] = "Выберите данные или ключ. SORT собирает связанную группу; SUM схлопывает числовой диапазон.",
                ["ui.sumNeedRange"] = "SUM нужен выделенный числовой диапазон.",
                ["ui.sumInvalid"] = "SUM принимает только обычные числовые токены.",
                ["ui.sumTarget"] = "SUM = {0}. Выберите пустую обычную клетку для результата.",
                ["ui.sumBadTarget"] = "Результат SUM можно поместить только в пустую обычную клетку.",
                ["ui.sumDone"] = "SUM схлопнул {0} значений в {1}.",
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
                ["ui.quarantineDone"] = "#REF! в {0} карантинизирован: клетка уничтожена, источник распространения удалён.",
                ["ui.finishSum"] = "Сначала выберите клетку для результата SUM.",
                ["ui.finished"] = "Уровень завершён. Нажмите СБРОС.",
                ["ui.deadline"] = "ДЕДЛАЙН ПРОПУЩЕН. Нажмите СБРОС.",
                ["ui.accepted"] = "ОТЧЁТ ПРИНЯТ на ходу {0}/{1}.",
                ["ui.rejected"] = "ОТЧЁТ ОТКЛОНЁН: {0}",
                ["ui.goal"] = "{0}: {1} / {2} → {3}",
                ["ui.help"] = "Перетаскивайте ЛКМ для диапазона.\nSORT собирает связанную группу. SUM схлопывает числовой диапазон.\nКрасный #REF! — активное заражение: клетка заблокирована, распространяется и позже погибает.\nЧёрная клетка — инертная уничтоженная дырка. DELETE по #REF! карантинизирует его.",

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
                ["goal.bonus"] = "Итого премии"
            },
            [PrototypeLanguage.English] = new Dictionary<string, string>
            {
                ["ui.title"] = "EXEL HELL // PROTOTYPE",
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
                ["ui.refDormant"] = "#REF! dormant — wakes after turn {0}",
                ["ui.refNext"] = "NEXT #REF! → {0}",
                ["ui.refNoPath"] = "NEXT #REF! → no valid path",
                ["ui.spill"] = "#SPILL! Not enough contiguous free space.",
                ["ui.select"] = "Select data or a key. SORT assembles a semantic group; SUM collapses a numeric range.",
                ["ui.sumNeedRange"] = "SUM needs a selected numeric range.",
                ["ui.sumInvalid"] = "SUM accepts only normal numeric tokens.",
                ["ui.sumTarget"] = "SUM = {0}. Pick an empty Normal cell for the result.",
                ["ui.sumBadTarget"] = "SUM result needs an empty Normal cell.",
                ["ui.sumDone"] = "SUM collapsed {0} values into {1}.",
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
                ["ui.quarantineDone"] = "#REF! at {0} quarantined: the cell is destroyed and stops spreading.",
                ["ui.finishSum"] = "Finish SUM by picking its result cell.",
                ["ui.finished"] = "Run finished. Press RESET.",
                ["ui.deadline"] = "DEADLINE MISSED. Press RESET.",
                ["ui.accepted"] = "REPORT ACCEPTED on turn {0}/{1}.",
                ["ui.rejected"] = "REPORT REJECTED: {0}",
                ["ui.goal"] = "{0}: {1} / {2} → {3}",
                ["ui.help"] = "Drag LMB to select a range.\nSORT assembles related data. SUM collapses a numeric range.\nRed #REF! is active infection: the cell is locked, spreads, then dies.\nBlack cells are inert destroyed holes. DELETE on #REF! quarantines it.",

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
                ["goal.bonus"] = "Bonus total"
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
