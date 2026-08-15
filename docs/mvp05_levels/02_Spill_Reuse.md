# MVP 0.5 Level 02 — Забитый диапазон

Роль: проверить фиксированный spill и повторное использование одной SORT formula cell под умеренным `#REF!`.

## Параметры

| Параметр | Значение |
|---|---:|
| Поле | 8×8 |
| Turn budget `B` | 12 |
| First outbreak `F` | 3 |
| Active outbreak delay `A` | 3 |
| Respawn delay | 2 |
| Цели | Salary Total, Bonus Total |
| Salary Total | 310 |
| Bonus Total | 27 |

Dataset:

| Сотрудник | Часы | Зарплата | Переработка | Премия |
|---|---:|---:|---:|---:|
| Иванов | 39 | 61 | 3 | 6 |
| Петров | 46 | 57 | 1 | 4 |
| Сидоров | 34 | 74 | 6 | 8 |
| Волкова | 42 | 66 | 2 | 7 |
| Ким | 37 | 52 | 4 | 2 |

## Стартовое поле

|   | A | B | C | D | E | F | G | H |
|---|---|---|---|---|---|---|---|---|
| 1 | K:SAL | · | K:B | K:H | · | K:OT | · | REP |
| 2 | · | S↓ | · | · | · | · | · | Σ Salary |
| 3 | 61 | · | 39 | 3 | 6 | · | R:Иванов | Σ Bonus |
| 4 | 57 | · | 46 | 1 | 4 | · | R:Петров | · |
| 5 | 74 | **39 blocker** | 34 | 6 | 8 | · | R:Сидоров | · |
| 6 | 66 | · | 42 | 2 | 7 | · | R:Волкова | · |
| 7 | 52 | · | 37 | 4 | 2 | · | R:Ким | · |
| 8 | · | · | · | · | · | · | · | · |

`B5` содержит обычный нецелевой числовой token из поля Hours и блокирует обязательный spill `B3:B7`.

Formula cells:
- `B2 = SORT`, down, единственная SORT formula на уровне;
- `H2 = SUM`, report Salary Total;
- `H3 = SUM`, report Bonus Total.

Безопасная staging cell для blocker: `F2`.

## Минимальная легальная последовательность

1. `CUT B5`.
2. `PASTE F2`.
3. `K:SAL -> B2 =SORT()`.
4. `B3:B7 -> H2 =SUM(310)`.
5. `CUT B2` — убрать Salary key из занятой `=SORT(Salary)`, формула остаётся `=SORT()`.
6. `K:B -> B2 =SORT()`.
7. `B3:B7 -> H3 =SUM(27)`.

`C0 = 7`.

Главная новая стоимость — не «доставка диапазона в формулу», а расчистка фиксированного lane и освобождение занятой formula cell.

## Функция balance model

`PL = 100*(0.45*C_n + 0.20*FL + 0.15*SP + 0.10*DI + 0.10*WP)`

`AT = 100*(0.30*SN + 0.25*Q + 0.15*AP + 0.15*KS + 0.15*OR)`

`D = 0.55*PL + 0.45*AT`

### Puzzle metrics

| Метрика | Значение | Причина |
|---|---:|---|
| `C0` | 7 | blocker relocation + 2 goals + SORT reuse |
| `B` | 12 | 5 recovery turns |
| `R` | 5 | normal reserve |
| `F_need` | 4 | SORT Salary, SUM Salary, SORT Bonus, SUM Bonus |
| `F_reuse` | 1 | B2 reused |
| `FL` | 0.25 | 1/4 |
| `SP` | 0.20 | 1 mandatory blocker / 5-cell spill |
| `DI` | 0 | Salary и Bonus independent |
| `OC` | 0 | goals may swap after lane is cleared |
| `WP` | 0.05 | one staging cell out of ~20 usable empties |

`C_n = 7/12 = 0.583`.

`PL ≈ 34.8`.

### Critical lifetime estimate

Authoring estimate before runtime playtest:

| Сущность | `U_e` | `T_e` | `S_e` | Direct outbreak `O_e` | Recovery `K_e` |
|---|---:|---:|---:|---:|---:|
| B2 SORT coordinate | 6 | 7 | +1 | 1 | ∞ |
| H2 Salary SUM | 4 | 7 | +3 | 1 | ∞ |
| H3 Bonus SUM | 7 | 8 | +1 | 1 | ∞ |
| Salary source set | 4 | 6 | +2 | 0 | ∞ |
| Bonus source set | 7 | 7 | 0 | 0 | ∞ |
| K:Bonus | 6 | 7 | +1 | 0 | ∞ |
| F2 staging token | 2 | 8 | +6 | 0 | 0 |

For the scalar model we use conservative finite severity proxy for exposed but normally recoverable play states rather than letting every raw-token loss force `KS=1`: `KS = 0.25`. Any actual irreplaceable loss during playtest is logged separately as catastrophic exposure.

Derived:
- `S_min = 0` for Bonus sources, but most critical infrastructure has `+1..+3`;
- for target tuning use effective `S_min = +1` because the raw-source contact estimate is highly spawn-dependent;
- `Q ≈ 0.25`;
- `OR ≈ 0.20`;
- `N_A = 1 + floor((7-3)/3) = 2`;
- `AP ≈ 0.67`;
- `SN = (2-1)/5 = 0.20`.

`AT ≈ 29.0`.

`D ≈ 32.2`.

## Ожидаемый тест

Проверяем:
- считывается ли фиксированный SORT lane как часть пазла;
- воспринимается ли освобождение `=SORT(key)` через CUT естественно;
- заставляет ли REF менять порядок двух независимых целей;
- не становится ли единственная SORT formula искусственным bottleneck вместо осмысленного ресурса.
