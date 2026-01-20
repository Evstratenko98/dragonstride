using System.Threading.Tasks;
using UnityEngine;

public class CharacterInstance
{
    private readonly ConfigScriptableObject _config;
    public CharacterModel Model { get; private set; }
    public CharacterView View { get; private set; }
    public bool IsMoving { get; private set; }
    public string Name { get; private set; }

    private readonly IEventBus _eventBus;

    private CharacterView _prefab;

    public CharacterInstance(
        ConfigScriptableObject config,
        CharacterModel model,
        CharacterView prefab,
        string name,
        IEventBus eventBus
    )
    {
        _config = config;
        _prefab = prefab;
        _eventBus = eventBus;

        Model = model;
        Name = name;
    }

    public Vector3 GetCoordinatesForCellView(int x, int y)
    {
        return new Vector3(x * _config.CELL_SIZE, _config.CHARACTER_HEIGHT, y * _config.CELL_SIZE);
    }

    public void Spawn(CellModel cell)
    {
        if (cell == null)
        {
            Debug.LogError("[CharacterInstance] Cannot spawn character because start cell is null.");
            return;
        }

        if (Model == null)
        {
            Debug.LogError("[CharacterInstance] Cannot spawn character because model is missing.");
            return;
        }

        if (_prefab == null)
        {
            Debug.LogError($"[CharacterInstance] Cannot spawn character '{Name}' because prefab is missing.");
            return;
        }

        Model.SetCell(cell);
        View = GameObject.Instantiate(_prefab);
        if (View == null)
        {
            Debug.LogError($"[CharacterInstance] Failed to instantiate prefab for character '{Name}'.");
            return;
        }

        View.SetPosition(GetCoordinatesForCellView(cell.X, cell.Y));
    }

    public async Task MoveTo(CellModel nextTarget)
    {
        if (IsMoving) return;
        IsMoving = true;

        Vector3 start = View.transform.position;
        Vector3 end = GetCoordinatesForCellView(nextTarget.X, nextTarget.Y);

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * _config.CHARACTER_SPEED;
            View.transform.position = Vector3.Lerp(start, end, t);
            await Task.Yield();
        }

        View.transform.position = end;
        Model.SetCell(nextTarget);
        IsMoving = false;

        _eventBus.Publish(new CharacterMovedMessage(this));
    }

    public void Destroy()
    {
        // Удаляем визуальную часть
        if (View != null)
        {
            UnityEngine.Object.Destroy(View.gameObject);
            View = null;
        }

        // Чистим модель
        Model = null;
    }
}
