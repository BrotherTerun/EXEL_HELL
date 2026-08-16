# Level 02 — Сверка под наблюдением / Watched Reconciliation

Роль: контрольная пара к Level 01. Puzzle layout, dataset, goals and FormulaCells are identical; меняется только наличие аномалии.

## Параметры

- Поле: 8×8.
- Goals: `SalaryTotal`, `OvertimeTotal`.
- `B = 8`.
- `C_sem = 4`, `C0 = 5`, `R = 3`.
- `FirstOutbreakTurn = 4`.
- `ActiveOutbreakDelayTurns = 6`.
- `RespawnDelayTurns = 6`.
- Corruption lifetime = 2.
- Spawn preferred distance = 2 ± 1, candidate pool = 4.

Formula diagnostics те же, что L1:
- `F_act = 4`;
- `F_move = 1`;
- `F_tax = 0`;
- `FHL = 0.10`.

## Anomaly cadence

Для оптимальной линии:

`N_A = 1 + floor((5 - 4)/6) = 1`.

То есть за обычное прохождение ожидается одно outbreak-окно. Аномалия должна быть видимой и заставить игрока учитывать её глазами, но уровень не проектируется вокруг обязательного defensive MOVE.

Целевой профиль: `AT` примерно в лёгком диапазоне, ориентир `15–25`, но точное значение фиксируем только после Unity trace фактического spawn/intent.

## Минимальная линия

Та же, что L1:

1. MOVE `D2 SORT` -> `B2`.
2. Salary key -> SORT.
3. Salary range -> report SUM.
4. Overtime key -> SORT.
5. Overtime range -> report SUM.

Порядок двух целей свободный.

## Зачем доска полностью совпадает с L1

Это чистый A/B-control. Если игрок после L1 вдруг начинает принимать другие решения на L2, причиной является только #REF!, а не новая задача или layout.

## Что должен проверить тест

- игрок замечает telegraph;
- давление ощущается, но не ломает уже понятую задачу;
- успешная оптимальная линия может пройти без реакции на anomaly;
- при менее оптимальном порядке игрок может решить сделать один защитный MOVE, но это не должно быть обязательным;
- сравнение L1/L2 позволяет отдельно оценить вклад аномалии в напряжение.