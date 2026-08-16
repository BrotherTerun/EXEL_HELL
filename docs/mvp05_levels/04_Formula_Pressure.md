# Level 04 — Финальная сверка / Final Reconciliation

Роль: трёхцелевая композиция, где anomaly является полноценным противником. Целевой first-contact profile: игрок может потребовать 3–4 попытки до уверенной победы, но причина поражения должна быть понятной и связанной с планом, а не с необратимым случайным hard-lock.

## Параметры

- Поле: 8×8.
- Goals: `SalaryForHoursBelowForty`, `OvertimeTotal`, `BonusTotal`.
- Dataset: Hours = 36,45,39,48,34; Salary = 67,56,73,61,78; OT = 4,2,6,1,5; Bonus = 5,8,3,7,6.
- Ответы: low-hours salary = 218; OT total = 18; Bonus total = 29.
- `B = 14`.
- `FirstOutbreakTurn = 2`.
- `ActiveOutbreakDelayTurns = 3`.
- `RespawnDelayTurns = 2`.
- Corruption lifetime = 2.
- Spawn preferred distance = 2 exactly; pool = 2.
- Formula inventory: `3 SORT + 3 report-SUM`.

## Геометрия

Field keys: Salary C1, Hours E1, Overtime G1, Bonus B1.

SORT: `B2`, `D2`, `F2`. Все три стартовые вертикальные lanes пригодны для первых проекций.

Report SUM: `H2`, `H3`, `H4`.

Данные Hours/Salary/Overtime/Bonus намеренно перемешаны так, чтобы ни Overtime, ни Bonus не лежали готовым чистым диапазоном. Четыре требуемые проекции конкурируют за три SORT-узла.

## Базовая no-REF линия

Один из вариантов:

1. Hours key -> SORT.
2. Salary key -> SORT.
3. MOVE salary Петрова (Hours 45) из salary lane в безопасную клетку.
4. MOVE salary Волковой (Hours 48) из salary lane в безопасную клетку.
5. DROP оставшийся salary range -> `H2 SUM` = 218.
6. Overtime key -> третий SORT.
7. DROP OT range -> `H3 SUM` = 18.
8. MOVE occupant-key из одного уже ненужного SORT, освобождая формулу.
9. Bonus key -> освобождённый SORT.
10. DROP Bonus range -> `H4 SUM` = 29.

`C_sem = 9` — четыре SORT, два filter MOVE, три SUM при достаточной инфраструктуре.

`C0 ≈ 10` — один дополнительный handling MOVE из-за 3 SORT на 4 проекции.

`B = 14`, `R ≈ 4`.

Альтернатива: игрок может отказаться от reuse SORT и вручную перегруппировать Bonus сопоставимым числом MOVE. Это намеренно: scarcity должна создавать выбор, а не обязательную бюрократию.

## Formula diagnostics

Для reuse-route:
- `F_act = 7` (4 SORT + 3 SUM);
- `F_extract = 1`;
- `F_delivery = 0` для parking key;
- `F_tax = 1`;
- `F_move = 0` в no-REF baseline;
- `FHL ≈ 1/10 = 0.10`.

То есть Formula Handling заметен, но далеко не доминирует.

Approximate no-anomaly `PL`: ориентир `45–55`.

## Anomaly cadence

`N_A = 1 + floor((10 - 2)/3) = 3` outbreak opportunities по базовой модели.

Плюс активные очаги распространяются через intent. С `R≈4` игрок способен оплатить несколько defensive MOVE/DELETE, но не может бесконечно чинить доску.

Целевой `AT`: примерно `55–70`, уточняется после Unity trace.

## Каких решений ждём от anomaly

Угрозы должны попадать не только по raw values, но и по **будущим полезным позициям FormulaCell**. Игрок должен выбирать между:
- использовать SORT сейчас до прихода intent;
- эвакуировать пустую формулу;
- вынести occupant, затем формулу двумя действиями;
- поменять порядок целей;
- перейти с SORT-reuse на manual range MOVE;
- карантинить очаг DELETE, если это экономит будущие действия.

Это должно давать несколько потенциальных линий, а не один пароль из правильных кликов.

## Failure criteria

Уровень редизайнится, если playtest показывает любое из следующего:
- успешная линия стабильно игнорирует anomaly;
- большинство поражений происходят из-за потери единственной математически незаменимой сущности до возможности реакции;
- `FHL` фактически >0.25 из-за вынужденного постоянного обслуживания SORT;
- один и тот же порядок действий побеждает независимо от spawn/intents;
- 3–4 рестарта получаются только из-за случайного hard-lock, а не обучения маршруту угрозы.

Целевая трудность — не гарантированное число смертей, а ситуация, где знание поля и понимание FormulaCell mobility заметно повышают шанс победы от попытки к попытке.