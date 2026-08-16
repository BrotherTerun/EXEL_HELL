# Типовые цепочки действий — Formula Cells 2.0 / MVP 0.5

Считаем только действия, которые тратят ход.

## Базовая грамматика управления

Бесплатно:
- click / выбор одной клетки;
- `Shift + Drag` для выделения прямоугольного диапазона;
- смена выделения;
- чтение Formula Bar;
- наведение/просмотр содержимого;
- `SUBMIT` как проверка уже собранного отчёта.

Платно, если действие успешно:
- `MOVE` одного token в обычную/отчётную клетку — 1 ход;
- `MOVE` выделенного диапазона — 1 ход;
- `MOVE` пустой FormulaCell — 1 ход;
- drop ключа в пустую `=SORT()` — 1 ход;
- drop выделенного числового диапазона в пустую `=SUM()` — 1 ход;
- `DELETE` / карантин — 1 ход.

Невалидный drop не тратит ход.

`CUT/PASTE` больше не являются частью Formula Cells 2.0. `Drag = MOVE`, `Shift+Drag = SELECT`.

## Семантика MOVE

### Обычный token

`source token -> drag -> destination`

Стоимость: **1 ход**.

Путь по таблице не учитывается. Разрешены диагональные перемещения и перемещение «сквозь» занятые клетки; проверяется только конечная позиция.

### Диапазон

После `Shift+Drag` обычный drag из занятой клетки внутри selection переносит весь movable payload выделения с одинаковым offset.

- относительное положение token'ов сохраняется;
- обычные пустые клетки внутри selection не являются payload;
- собственные исходные клетки не блокируют сдвиг диапазона;
- чужой occupant, FormulaCell, `#REF!` или Destroyed в конечном footprint делают drop невалидным.

Стоимость: **1 ход** независимо от числа перемещаемых token'ов.

### FormulaCell

Formula property является отдельным слоем клетки.

- если FormulaCell занята token'ом, первый MOVE переносит **occupant**;
- пока occupant находится внутри, FormulaCell закреплена;
- после извлечения occupant пустую FormulaCell можно MOVE в другую пустую доступную клетку;
- пустую FormulaCell можно наложить на пустую ReportCell;
- Report property при переносе Formula property остаётся на месте.

Единый lifecycle для SUM и SORT:

`empty formula -> input drop -> occupied formula -> occupant MOVE out -> empty formula -> formula MOVE`

Специальных исключений для SORT нет.

## Формулы активируются только через DROP

Отдельного действия «activate formula» и click-to-activate больше нет.

### SUM

`selected numeric range -> drag -> =SUM()`

Стоимость: **1 ход**.

Условия:
- минимум 2 numeric token;
- обычные пустые клетки в прямоугольном selection допустимы;
- результат появляется как aggregate occupant внутри FormulaCell;
- обычные numeric sources destructively consumed;
- occupant ReportCell является persistent operand: SUM читает его, но не уничтожает.

### SORT

`FieldKey/RecordKey -> drag -> =SORT()`

Стоимость: **1 ход**.

- FieldKey spill идёт вниз;
- RecordKey spill идёт вправо;
- fallback-направления нет;
- key становится occupant FormulaCell;
- пока key внутри, SORT нельзя переносить;
- для повторного использования key нужно MOVE наружу за 1 ход.

## Базовые цепочки

1. **MOVE одного значения — 1 ход**  
   `token -> destination`

2. **MOVE диапазона — 1 ход**  
   `selected range -> translated destination`

3. **Прямая агрегация в report-SUM — 1 ход**  
   `numeric range -> report =SUM()`

4. **Агрегация через workspace-SUM с доставкой результата — 2 хода**  
   `range -> =SUM(n) -> aggregate MOVE -> ReportCell`

   Второй MOVE одновременно доставляет результат и освобождает SUM для повторного использования.

5. **Сборка параметра — 1 ход**  
   `FieldKey -> =SORT()`

6. **Сборка параметра + агрегация — 2 хода при готовом report-SUM**  
   `FieldKey -> =SORT() -> resulting range -> report =SUM()`

7. **Перенос пустой формулы — +1 ход**  
   `=SUM()/=SORT() -> MOVE -> new coordinate`

8. **Расчистить spill + SORT + SUM — 3 хода**  
   `blocking token -> MOVE -> safe cell -> key -> =SORT() -> range -> report =SUM()`

   В старой модели CUT+PASTE делали расчистку двухходовой; Formula Cells 2.0 сжимает само перемещение до одного решения/хода.

9. **Карантин + SORT + SUM — 3 хода**  
   `#REF! -> DELETE -> key -> =SORT() -> range -> report =SUM()`

10. **Эвакуация нужного значения + SORT + SUM — 3 хода**  
    `required token -> MOVE -> safe cell -> key -> =SORT() -> range -> report =SUM()`

11. **Спасти готовый/промежуточный token от Intent — +1 ход**  
    `token -> MOVE -> safe cell`

12. **Освободить занятую `=SUM(n)` — +1 ход**  
    `aggregate -> MOVE -> destination`

    Если destination является нужной ReportCell, этот же ход одновременно завершает доставку результата.

13. **Освободить занятую `=SORT(key)` — +1 ход**  
    `key -> MOVE -> safe/needed cell`

14. **Повторно использовать один SORT для двух ключей — 3 хода до вычислений**  
    `key A -> =SORT(A) -> key A MOVE out -> key B -> =SORT(B)`

    В отличие от CUT/PASTE отдельного «сохранить key через PASTE» больше нет: сам MOVE уже сохраняет его в destination.

15. **Две независимые прямые агрегации**

    При двух готовых report-SUM:
    `range A -> SUM(A) -> range B -> SUM(B)` = **2 хода**.

    При одной переиспользуемой workspace-SUM:
    `range A -> SUM -> aggregate A -> Report A -> range B -> SUM -> aggregate B -> Report B` = **4 хода**.

    Это ключевой новый trade-off: FormulaCell, оставленная внутри заполненной ReportCell, становится pinned вместе с результатом и не может быть переиспользована без нарушения уже собранного отчёта.

16. **Две независимые сборки + две агрегации**

    Достаточная инфраструктура (`2 SORT`, `2 report-SUM`) — **4 хода**:
    `key A -> SORT A -> range A -> SUM A -> key B -> SORT B -> range B -> SUM B`.

    Один переиспользуемый SORT добавляет `+1` извлечение key.

    Одна workspace-SUM вместо двух report-SUM добавляет `+2` доставки aggregate в ReportCell.

    `1 SORT + 1 workspace-SUM` даёт **7 ходов** для той же логики.

17. **Два subtotal -> final total**

    При трёх независимых SUM-позициях, включая final report-SUM:
    `range A -> SUM A -> range B -> SUM B -> [aggregate A + aggregate B] -> SUM final` = **3 хода**.

    При одной workspace-SUM:
    `range A -> SUM -> aggregate A MOVE out -> range B -> SUM -> aggregate B MOVE out -> [A+B] -> SUM -> final aggregate MOVE to Report` = **6 ходов**.

    Поэтому scarcity формул нельзя считать бесплатным «пространственным интересом»: чрезмерный reuse быстро превращается в action tax.

## Нижние границы для знакомых report goals

Это **семантические lower bounds**, а не готовые уровни. Они предполагают подходящую геометрию, достаточное число формул и отсутствие `#REF!`. Реальный `C0` будущего поля может быть выше из-за relocation, spill, filtering geometry и formula scarcity.

| Goal / набор goals | Нижняя граница | Базовая последовательность |
|---|---:|---|
| `SalaryTotal` | 2 | SORT Salary + SUM |
| `Bonus >= 5` | 4 | SORT Bonus + MOVE двух неподходящих значений + SUM |
| `SalaryOfMaxOvertime` | 3 | SORT Overtime + SORT Salary + MOVE найденной Salary в Report |
| `SalaryForHoursBelowForty` | 5 | SORT Hours + SORT Salary + MOVE двух исключённых Salary + SUM |
| `SalaryTotal + Bonus >= 5` | 6 | 2 SORT + 2 filter MOVE + 2 SUM |
| `SalaryOfMaxOvertime + SalaryForHoursBelowForty` | 7 + `G_rect` | SORT Overtime + Hours + Salary; direct Salary -> Report; 2 filter MOVE; SUM с persistent report operand |
| `SalaryForHoursBelowForty + OvertimeTotal + BonusTotal` | 9 | 4 SORT + 2 filter MOVE + 3 SUM |

`G_rect` — дополнительная стоимость геометрии, если persistent operand в первой ReportCell и остальные low-hours Salary нельзя включить в один валидный прямоугольный SUM-range без дополнительного MOVE.

### Чувствительность к scarcity формул

Для знакомых наборов:

| Goal set | Достаточно формул | `1 SORT` | `1 workspace-SUM` | `1 SORT + 1 workspace-SUM` |
|---|---:|---:|---:|---:|
| `SalaryTotal + Bonus >= 5` | 6 | 7 | 8 | 9 |
| `MaxOvertimeSalary + LowHoursSalary` | 7 + `G_rect` | 9 + `G_rect` | 8 + `G_rect` | 10 + `G_rect` |
| `LowHoursSalary + OvertimeTotal + BonusTotal` | 9 | 12 | 12 | 15 |

Эта таблица — главный предохранитель перед новым level design. Если scarcity добавляет 40–60% действий, формулы начинают обслуживать сами себя вместо того, чтобы создавать пространственные решения.

## Условная сложность одной цели по минимальной цепочке

После сжатия CUT+PASTE в MOVE старые границы нужно немного снизить:

- **L0 — 0 ходов:** ответ уже находится в требуемой ReportCell;
- **L1 — 1–2 хода:** прямой SUM, direct MOVE или SORT+SUM;
- **L2 — 3–4 хода:** filtering, formula/result delivery, одна relocation, одна эвакуация/карантин;
- **L3 — 5–7 ходов:** несколько проекций, несколько filter MOVE, dependency или обязательный formula reuse;
- **L4 — 8+ ходов:** длинная составная цепочка; допустима для многозадачного позднего уровня, но не как solo-goal.

## Goal overlap и ReportCell > FormulaCell

Сложность уровня нельзя считать простой суммой solo-cost целей.

Для каждой цели `g`:
- `S_g` — raw/source tokens, необходимые цели;
- `S_a ∩ S_b = ∅` — цели независимы по данным;
- overlap может давать shared preparation;
- destructive SUM может создавать обязательный порядок.

Formula Cells 2.0 добавляет легальный способ разрешать часть overlap без COPY:

> occupant ReportCell является persistent operand.

Поэтому token можно сначала MOVE в ReportCell для одной цели, а позже включить эту ReportCell в SUM другой цели. SUM читает значение, но ReportCell сохраняет occupant.

Это не второй режим SUM: правило исходит из свойства ReportCell и одинаково для всех будущих вычислительных FormulaCell.

Если даже с report-mediated dependency не существует легальной последовательности, задаче назначается `C0 = ∞` и поле редизайнится.

## Что теперь реально повышает сложность

Помимо длины action sequence:
- число экземпляров `SUM`/`SORT` относительно числа активаций;
- обязательное извлечение occupant для reuse FormulaCell;
- число обязательных MOVE самой FormulaCell;
- количество реально пригодных позиций FormulaCell;
- spill geometry выбранной позиции SORT;
- число token MOVE, требуемых для filtering/расчистки;
- peak staging occupancy после таких MOVE;
- goal overlap и обязательный порядок;
- возможность использовать ReportCell как persistent operand;
- последний ход, когда нужен конкретный token/formula placement;
- direct outbreak exposure FormulaCell;
- стоимость эвакуации empty formula (=1) и occupied formula+occupant (обычно до 2);
- количество outbreak windows, открывающихся до `C0`.

Главный критерий Formula Cells 2.0: дополнительные ходы должны соответствовать **пространственному выбору**, а не обслуживанию формул. Если Formula Handling становится значительной долей `C0`, поле нужно упрощать или увеличивать formula inventory.
