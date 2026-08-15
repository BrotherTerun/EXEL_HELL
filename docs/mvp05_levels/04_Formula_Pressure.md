# MVP 0.5 Level 04 — Формулы под давлением

Роль: поздний pressure-test новой модели — semantic subset + две независимые суммы, две переиспользуемые SORT-coordinate и ранний `#REF!`.

## Параметры

| Параметр | Значение |
|---|---:|
| Поле | 8×8 |
| Turn budget `B` | 18 |
| First outbreak `F` | 2 |
| Active outbreak delay `A` | 3 |
| Respawn delay | 2 |
| Цели | Salaries where Hours < 40, Overtime Total, Bonus Total |
| Salaries where Hours < 40 | 218 |
| Overtime Total | 18 |
| Bonus Total | 29 |

Dataset:

| Сотрудник | Часы | Зарплата | Переработка | Премия |
|---|---:|---:|---:|---:|
| Иванов | 36 | 67 | 4 | 5 |
| Петров | 45 | 56 | 2 | 8 |
| Сидоров | 39 | 73 | 6 | 3 |
| Волкова | 48 | 61 | 1 | 7 |
| Ким | 34 | 78 | 5 | 6 |

Low-hours salary: `67 + 73 + 78 = 218`.

## Стартовое поле

|   | A | B | C | D | E | F | G | H |
|---|---|---|---|---|---|---|---|---|
| 1 | K:H | · | K:SAL | · | K:OT | K:B | · | REP |
| 2 | · | S↓ A | · | S↓ B | · | · | · | Σ LowHours |
| 3 | 36 | · | 67 | · | 4 | 5 | R:Иванов | Σ OT |
| 4 | 45 | · | 56 | · | 2 | 8 | R:Петров | Σ Bonus |
| 5 | 39 | · | 73 | · | 6 | 3 | R:Сидоров | · |
| 6 | 48 | · | 61 | · | 1 | 7 | R:Волкова | · |
| 7 | 34 | · | 78 | · | 5 | 6 | R:Ким | · |
| 8 | · | · | · | · | · | · | · | · |

Formula cells:
- `B2 = SORT`, down. Используется Hours -> Salary;
- `D2 = SORT`, down. Используется Overtime -> Bonus;
- `H2 = SUM`, report LowHours Salary;
- `H3 = SUM`, report Overtime Total;
- `H4 = SUM`, report Bonus Total.

Staging cells для исключённых salary: `F2`, `G2`.

## Минимальная легальная последовательность

### Low-hours Salary

1. `K:H -> B2 =SORT()` → `B3:B7 = 36,45,39,48,34`.
2. `CUT B2` — освободить formula coordinate, запомнив qualifying records: Иванов, Сидоров, Ким.
3. `K:SAL -> B2 =SORT()` → `B3:B7 = 67,56,73,61,78`.
4. `CUT B4` (`56`, Петров).
5. `PASTE F2`.
6. `CUT B6` (`61`, Волкова).
7. `PASTE G2`.
8. `B3:B7 -> H2 =SUM(218)`; пустые B4/B6 игнорируются.

### Overtime

9. `K:OT -> D2 =SORT()`.
10. `D3:D7 -> H3 =SUM(18)`.

### Bonus

11. `CUT D2` — освободить `=SORT(Overtime)`.
12. `K:B -> D2 =SORT()`.
13. `D3:D7 -> H4 =SUM(29)`.

`C0 = 13`.

## Функция balance model

`PL = 100*(0.45*C_n + 0.20*FL + 0.15*SP + 0.10*DI + 0.10*WP)`

`AT = 100*(0.30*SN + 0.25*Q + 0.15*AP + 0.15*KS + 0.15*OR)`

`D = 0.55*PL + 0.45*AT`

### Puzzle metrics

| Метрика | Значение | Причина |
|---|---:|---|
| `C0` | 13 | semantic chain 8 + OT 2 + SORT reuse + Bonus 2 |
| `B` | 18 | 5 turns recovery reserve |
| `R` | 5 | нормальный запас при длинной базе |
| `F_need` | 6 | 3 SORT + 3 SUM activations |
| `F_reuse` | 2 | B2 и D2 reuse |
| `FL` | 0.333 | 2/6 |
| `SP` | 0 | оба lane чисты на старте; сложность приходит после проекций |
| `DI` | 0.05 | цели почти независимы; LowHours использует Hours+Salary semantics |
| `OC` | 2 | Hours before Salary; OT before Bonus only because D2 reused |
| `WP` | 0.10 | две staging cells для salary exclusions |

`C_n = min(1, 13/12) = 1`.

`PL ≈ 53.2`.

### Critical lifetime estimate

| Сущность | `U_e` | `T_e` | `S_e` | `O_e` | Recovery `K_e` |
|---|---:|---:|---:|---:|---:|
| B2 SORT A | 3 | 4 | +1 | 1 | ∞ |
| qualifying Hours information | 3 | 4 | +1 | 0 | 2+ |
| low-hours Salary tokens | 8 | 6 | **-2** | 0 | ∞ |
| H2 SUM | 8 | 7 | **-1** | 1 | ∞ |
| D2 SORT B | 12 | 9 | **-3** | 1 | ∞ |
| Overtime sources | 10 | 8 | **-2** | 0 | ∞ |
| H3 SUM | 10 | 9 | -1 | 1 | ∞ |
| Bonus sources | 13 | 10 | **-3** | 0 | ∞ |
| H4 SUM | 13 | 11 | **-2** | 1 | ∞ |
| excluded Salary staging F2/G2 | 8 | 8 | 0 | 0 | 1–2 |

Это deliberately severe layout. Точные `T_e` зависят от actual spawn candidate и route; таблица задаёт гипотезу, которую smoke-test должен опровергнуть/уточнить.

Scalar authoring values:
- `S_min = -2` (не используем одиночный -3 инфраструктурный worst-case как единственное число, чтобы не переоценивать случайный прямой маршрут);
- `Q = 0.55`;
- `OR = 0.20`;
- `N_A = 1 + floor((13-2)/3) = 4`, поэтому `AP = 1`;
- `KS = 0.50`;
- `SN = (2 - (-2))/5 = 0.80`.

`AT ≈ 63.2`.

`D ≈ 57.7`.

## Почему уровень не просто «REF ближе»

Высокая угроза возникает одновременно из:
- длинного `C0`;
- нескольких anomaly opportunities (`N_A≈4`);
- поздно нужных formula coordinates;
- повторного использования двух SORT-полей;
- высокой цены потери source set после того, как одна проекция уже была собрана;
- двух staging values;
- отрицательного slack у нескольких разных типов critical entity, а не только у ближайшего raw token.

## Ожидаемый тест

- меняет ли игрок порядок трёх целей из-за telegraph;
- решает ли эвакуировать данные или карантинить REF вместо слепого следования оптимальной цепочке;
- не становится ли разрушение единственной нужной formula coordinate безальтернативным случайным поражением;
- достаточно ли `R=5` для 1–2 осмысленных defensive actions;
- ощущается ли pressure как конфликт планов, а не как «таймер случайно съел нужную кнопку».
