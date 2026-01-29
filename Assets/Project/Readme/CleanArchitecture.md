# Clean Architecture правила

### Project
Корневая папка всего проектного кода и ассетов.  
Содержит **всё, что относится именно к этому проекту**, а не к Unity или сторонним пакетам.

## Bootstrap

**Назначение:**  
Bootstrap — это *composition root* проекта. Единственное место, где:
- создаются и связываются зависимости,
- инициализируются модули,
- подключаются конфиги и инфраструктура.

**Что здесь лежит:**
- `AppBootstrap` — старт приложения (EventBus, глобальные зависимости).
- `GameBootstrap` — инициализация игровой сессии.
- `UIScope` / `GameScope` — связывание UI и игровых модулей.

**Правила:**
- ❗ Только здесь разрешено “знать обо всех модулях”.
- ❌ Бизнес-логики быть не должно.
- ❌ Никаких UseCase / Domain-правил.
- Bootstrap **ничего не решает**, только *собирает*.

---

## Core

**Назначение:**  
Core — это общее “ядро”, не принадлежащее ни одному модулю.

**Что здесь лежит:**
- Общие интерфейсы (например `IEventBus`, `IRandomSource`).
- Общие базовые типы и состояния (`GamePhase`, `TurnPhase`).
- Общие утилиты и контракты, используемые разными модулями.

**Чего здесь НЕ должно быть:**
- Доменных сущностей конкретного модуля (Character, Item, Field и т.п.).
- Unity-зависимостей.
- Реализаций (только контракты).

**Правило:**
> Если файл нужен **более чем одному модулю** и он не про конкретную фичу — он кандидат в Core.

---

## Modules

Каждый модуль — **изолированная фича** со своей предметной областью.  
Модули не знают внутреннее устройство друг друга.

## Слои

- **Domain**: только чистые модели, value objects и бизнес-правила. Без Unity API и IO.
- **Application**: юзкейсы, оркестрация, порты/интерфейсы. Без Unity API.
- **Presentation**: `MonoBehaviour`/UI/рендер/ввод/анимации; подписки на события.
- **Infrastructure**: адаптеры к Unity и внешним системам (Random/Time/PlayerPrefs/Assets).

## Правила зависимостей

- **Domain** → никого.
- **Application** → Domain + Core интерфейсы.
- **Presentation** → Application + Unity.
- **Infrastructure** → Core интерфейсы + Unity.

## Именование и роли (без Controller/Service)

### Базовый принцип

Имя файла = **роль + предметная область**. Используем глаголы/роль, а не архитектурные слова.

Примеры:
- ❌ `CharacterService`
- ✅ `CharacterFactory`, `CharacterMovement`, `CharacterLifecycle`

### Domain — что есть (сущности и правила)

| Тип | Как называть | Примеры |
| --- | --- | --- |
| Сущность | Существительное | `Character`, `Cell`, `Inventory`, `Item` |
| Value Object | Существительное | `Stats`, `Position`, `CellIndex` |
| Enum | Существительное | `CellType`, `ItemRarity`, `TurnPhase` |
| Доменные правила | `*Rules`, `*Policy` | `MovementRules`, `InventoryPolicy` |
| Результаты | `*Result` | `MoveResult`, `AddItemResult` |

Если файл можно объяснить фразой “это часть мира игры” — он в Domain и без суффиксов.

### Application — что происходит (use-cases и процессы)

**Use-cases (одно действие):**
- `StartGameUseCase`
- `GenerateFieldUseCase`
- `RollDiceUseCase`
- `MoveCharacterUseCase`

**Долгоживущие процессы/оркестрация:**

| Назначение | Суффикс | Примеры |
| --- | --- | --- |
| Жизненный цикл | `Flow` | `GameFlow`, `TurnFlow` |
| Координация подсистем | `Coordinator` | `CharacterCoordinator` |
| Управление состоянием | `StateMachine` | `TurnStateMachine` |
| Создание объектов | `Factory` | `CharacterFactory`, `ItemFactory` |
| Генерация | `Generator` | `MazeGenerator`, `LootGenerator` |

### Presentation — как это выглядит и ощущается

**View (MonoBehaviour):**
- `*View` — чистая визуализация (`CellView`, `CharacterView`, `InventoryView`).

**Связка View ↔ Application:**
- `*Presenter`, `*Binder`, `*Driver`, `*Listener` (в зависимости от роли).

Примеры:
- `FieldPresenter`
- `CharacterMovementDriver`
- `TurnHudPresenter`
- `CameraFollowDriver`

**Input:**
- `*InputReader` (например `CharacterInputReader`, `PlayerInputReader`).

### Infrastructure — как мы говорим с Unity и миром

Технические имена, заменяемые при смене движка:
- `UnityRandomSource`
- `UnityTimeSource`
- `PlayerPrefsStorage`
- `PrefabProvider`
- `UnityCameraRig`

### Events — что уже произошло

Никаких `Event`/`Message`/`Notification` в названии:
- Было: `CharacterMovedMessage` → Стало: `CharacterMoved`
- Было: `TurnStateChangedMessage` → Стало: `TurnPhaseChanged`
- Было: `ResetButtonPressedMessage` → Стало: `ResetRequested`

### Общие правила

- PascalCase для папок/файлов/классов.
- Без camelCase в путях.