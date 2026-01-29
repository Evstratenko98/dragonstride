# Clean Architecture правила

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

## Суффиксы и роли

- `*Service` — оркестрация/бизнес-процессы Application.
- `*UseCase` — конкретный сценарий (например, `GenerateMazeUseCase`, `StartTurnUseCase`).
- `*Presenter` — связывает события Application с View.
- `*View` — `MonoBehaviour`/UI.
- `*Controller` — Unity-компонент, принимает input/Unity-события и делегирует в Application.

## Именование

- PascalCase для папок/файлов/классов.
- Без camelCase в путях.
