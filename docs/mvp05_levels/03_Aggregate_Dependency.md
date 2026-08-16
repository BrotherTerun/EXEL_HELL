# Level 03 — Несходящиеся данные / Inconsistent Data

Роль: первый уровень, где `#REF!` должен реально менять план. Целевой playtest-profile: примерно 2–3 replans за успешную попытку; первое прохождение часто заканчивается рестартом, второе — победой.

## Параметры

- Поле: 8×8.
- Goals: `SalaryOfMaxOvertime`, `SalaryForHoursBelowForty`.
- Dataset: Hours = 38,43,35,46,37; Salary = 62,69,76,54,71; OT = 2,4,7,1,5.
- Ответы: max-OT salary = 76; low-hours salaries = 62+76+71 = 209.
- `B = 11`.
- `FirstOutbreakTurn = 2`.
- `ActiveOutbreakDelayTurns = 4`.
- `RespawnDelayTurns = 3`.
- Corruption lifetime = 2.
- Spawn preferred distance = 2 ± 1; pool = 4.
- Formula inventory: `3 SORT + 1 report-SUM`.

## Геометрия

Field keys: Salary C1, Hours E1, Overtime G1.

Три стартовых SORT: `B2`, `D2`, `F2`; lanes `B3:B7`, `D3:D7`, `F3:F7` стартуют чистыми. Данные разбросаны по C/E/G.

Report:
- `H2` — plain ReportCell для прямого значения 76;
- `H5` — ReportCell + SUM для 209;
- `H3/H4` — обычные staging cells.

## Легальная no-REF линия

1. DROP Overtime key -> один SORT.
2. DROP Hours key -> второй SORT.
3. DROP Salary key -> третий SORT.
4. MOVE salary 76 (Sidorov) -> `H2`.
5. MOVE salary 62 (Ivanov) -> `H3`.
6. MOVE salary 71 (Kim) -> `H4`.
7. Shift+Drag `H2:H4`, DROP range -> `H5 =SUM()` => 209.

`H2` остаётся 76, потому что ReportCell occupant является persistent operand.

`C_sem = 7`.
`C0 = 7` для благоприятного порядка/размещения.
`B = 11`, `R = 4`.

## Dependency

Множества целей пересекаются по salary Сидорова:
- max-OT goal использует Sidorov Salary;
- low-hours goal использует Ivanov + Sidorov + Kim Salary.

`J = 1/3 ≈ 0.33`.

Тип связи: **report-mediated**. Сначала 76 сдаётся в H2, затем H2 используется неразрушающе внутри SUM для второй цели.

Это намеренно проверяет, понимает ли игрок свойство ReportCell без введения COPY.

## Formula diagnostics

Base route не требует formula scarcity tax:
- `F_act = 4`;
- `F_move = 0` в чистой линии;
- `F_extract = 0`;
- `FHL = 0`.

Но movable FormulaCells являются главным defensive option: угрожаемый пустой SORT можно эвакуировать одним MOVE вместо рестарта.

Approximate no-anomaly `PL`: normal/involved, ориентир `35–45`.

## Anomaly cadence

`N_A = 1 + floor((7 - 2)/4) = 2` outbreak opportunities по грубой модели.

Дополнительно каждый активный очаг генерирует movement intent, поэтому фактических решений «оставить / эвакуировать / поменять порядок» ожидается больше, чем `N_A`.

Целевой `AT`: примерно `40–55`, уточняется после фактического Unity trace.

## Ожидаемое поведение

Первая попытка должна научить неприятному факту: если выполнить три SORT в привычном порядке и оставить критичную инфраструктуру/данные на траектории intent, исходный план может перестать быть оптимальным.

Хорошее прохождение должно допускать несколько ответов:
- ускорить использование угрожаемого SORT;
- MOVE пустой SORT в другой пригодный участок;
- изменить порядок Hours/Salary/Overtime;
- эвакуировать конкретный critical token;
- потратить DELETE на очаг, если это выгоднее двух дальнейших реакций.

Если после теста выяснится, что существует одна доминирующая линия, полностью игнорирующая anomaly, L3 недодавлен. Если потеря одного раннего объекта почти всегда делает задачу математически невозможной — передавлен.