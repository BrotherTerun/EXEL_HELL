# MVP 0.5 Level 03 — Промежуточные итоги

Роль: проверить `=SUM(n)` как обычный aggregate token внутри формульной оболочки и осмысленность повторного использования SUM-coordinate.

## Параметры

| Параметр | Значение |
|---|---:|
| Поле | 8×8 |
| Turn budget `B` | 12 |
| First outbreak `F` | 3 |
| Active outbreak delay `A` | 3 |
| Respawn delay | 2 |
| Цели | Salary Total, Bonus Total |
| Salary Total | 312 |
| Bonus Total | 27 |

Dataset:

| Сотрудник | Часы | Зарплата | Переработка | Премия |
|---|---:|---:|---:|---:|
| Иванов | 38 | 63 | 1 | 7 |
| Петров | 42 | 54 | 4 | 2 |
| Сидоров | 35 | 71 | 6 | 5 |
| Волкова | 47 | 58 | 3 | 9 |
| Ким | 39 | 66 | 2 | 4 |

## Стартовое поле

|   | A | B | C | D | E | F | G | H |
|---|---|---|---|---|---|---|---|---|
| 1 | K:H | K:SAL | K:OT | · | K:B | · | · | REP |
| 2 | · | · | Σ work | · | S↓ Bonus | · | · | Σ Salary |
| 3 | 63 | R:Иванов | · | K:H | · | 7 | · | Σ Bonus |
| 4 | 54 | R:Петров | · | 1 | · | 2 | · | · |
| 5 | 71 | R:Сидоров | · | 6 | · | 58 | · | · |
| 6 | · | R:Волкова | · | 3 | · | 66 | · | · |
| 7 | 38 | 42 | 35 | 47 | · | 39 | · | · |
| 8 | 9 | 5 | 4 | 2 | · | · | · | · |

Пять Salary-токенов намеренно разбиты на два чистых кластера:
- `A3:A5 = 63,54,71`;
- `F5:F6 = 58,66`.

Прямоугольник, охватывающий оба кластера, содержит ключи/другие числовые данные и поэтому не является валидным прямым SUM без дополнительной дорогой расчистки.

Formula cells:
- `C2 = SUM`, workspace formula, будет использована дважды;
- `E2 = SORT`, down, для Bonus;
- `H2 = SUM`, report Salary Total;
- `H3 = SUM`, report Bonus Total.

Staging cell для первого salary subtotal: `D2`.

## Минимальная легальная последовательность

Salary chain:
1. `A3:A5 -> C2 =SUM(188)`.
2. `CUT C2` — забрать aggregate `188`, оставить `=SUM()`.
3. `PASTE D2`.
4. `F5:F6 -> C2 =SUM(124)`.
5. `C2:D2 -> H2 =SUM(312)`.

Bonus chain:
6. `K:B -> E2 =SORT()`.
7. `E3:E7 -> H3 =SUM(27)`.

`C0 = 7`.

Ключевой тест: формульное поле `C2` переиспользуется как инфраструктура, а его aggregate-token сначала отделяется через CUT и становится обычным staging data.

## Функция balance model

`PL = 100*(0.45*C_n + 0.20*FL + 0.15*SP + 0.10*DI + 0.10*WP)`

`AT = 100*(0.30*SN + 0.25*Q + 0.15*AP + 0.15*KS + 0.15*OR)`

`D = 0.55*PL + 0.45*AT`

### Puzzle metrics

| Метрика | Значение | Причина |
|---|---:|---|
| `C0` | 7 | 5-action subtotal chain + Bonus SORT/SUM |
| `B` | 12 | 5 turns reserve |
| `R` | 5 | normal |
| `F_need` | 5 | SUM A, SUM B, final SUM, SORT Bonus, SUM Bonus |
| `F_reuse` | 1 | workspace C2 reused |
| `FL` | 0.20 | 1/5 |
| `SP` | 0 | Bonus spill initially clear |
| `DI` | 0 | final goals use independent raw fields |
| `OC` | 1 | salary subtotal A must be removed before subtotal B in same formula coordinate |
| `WP` | 0.05 | one required staging cell |

`C_n = 7/12 = 0.583`.

`PL ≈ 30.8`.

`OC` is kept as a diagnostic rather than added again to PL, because its action cost is already present in `C0`/`FL`.

### Critical lifetime estimate

| Сущность | `U_e` | `T_e` | `S_e` | `O_e` | Recovery `K_e` |
|---|---:|---:|---:|---:|---:|
| C2 workspace SUM | 4 | 5 | +1 | 1 | ∞ if destroyed before action 4 |
| subtotal 188 at D2 | 5 | 5 | 0 | 0 | 3 |
| subtotal 124 at C2 | 5 | 5 | 0 | 0 | 1–2 |
| H2 report SUM | 5 | 6 | +1 | 1 | ∞ |
| E2 Bonus SORT | 6 | 7 | +1 | 1 | ∞ |
| Bonus sources | 7 | 7 | 0 | 0 | ∞ |
| H3 report SUM | 7 | 8 | +1 | 1 | ∞ |

Authoring values used for the scalar estimate:
- `S_min = 0`;
- `Q = 0.40`;
- `OR = 0.20`;
- `N_A = 1 + floor((7-3)/3) = 2`;
- `AP = 0.67`;
- `KS = 0.35`;
- `SN = (2-0)/5 = 0.40`.

`AT ≈ 40.3`.

`D ≈ 35.0`.

## Почему этот уровень нужен отдельно

Он изолирует вопрос, которого нет в Level 02:

> Является ли `field -> token -> CUT -> field remains formula` понятной и полезной моделью, или игрок воспринимает повторное использование formula cell как техническую возню?

## Ожидаемый тест

- понимает ли игрок после CUT, что `C2` снова `=SUM()`;
- воспринимает ли `188` после PASTE как обычный aggregate-token;
- естественно ли складывать два subtotal в final report SUM;
- создаёт ли угроза subtotal реальное решение «сохранить/ускориться», а не только случайный проигрыш.
