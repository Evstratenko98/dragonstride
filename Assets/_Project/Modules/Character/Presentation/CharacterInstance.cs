using System.Threading.Tasks;
using UnityEngine;

public class CharacterInstance
    : ICellLayoutOccupant
{
    private readonly ConfigScriptableObject _config;
    public Character Model { get; private set; }
    public Entity Entity => Model;
    public CharacterView View { get; private set; }
    public CharacterClass CharacterClass => Model?.Class;
    public bool IsMoving { get; private set; }
    public string Name { get; private set; }

    private readonly IEventBus _eventBus;
    private readonly FieldRoot _fieldRootService;

    private CharacterView _prefab;

    public CharacterInstance(
        ConfigScriptableObject config,
        Character model,
        CharacterView prefab,
        string name,
        IEventBus eventBus,
        FieldRoot fieldRootService
    )
    {
        _config = config;
        _prefab = prefab;
        _eventBus = eventBus;
        _fieldRootService = fieldRootService;

        Model = model;
        Name = name;
    }

    public Vector3 GetCoordinatesForCellView(int x, int y)
    {
        return new Vector3(x * _config.CELL_SIZE, _config.CHARACTER_HEIGHT, y * _config.CELL_SIZE);
    }

    public void Spawn(Cell cell)
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
        Transform parent = _fieldRootService?.EnsureRoot();
        View = parent == null
            ? GameObject.Instantiate(_prefab)
            : GameObject.Instantiate(_prefab, parent);
        if (View == null)
        {
            Debug.LogError($"[CharacterInstance] Failed to instantiate prefab for character '{Name}'.");
            return;
        }

        View.Bind(Model, Name);
        View.SetPosition(GetCoordinatesForCellView(cell.X, cell.Y));

        var clickHandler = View.gameObject.GetComponent<CharacterClickHandler>();
        if (clickHandler == null)
        {
            clickHandler = View.gameObject.AddComponent<CharacterClickHandler>();
        }

        clickHandler.Initialize(this, _eventBus);
    }

    public void Respawn(Character model, Cell startCell)
    {
        if (model == null)
        {
            Debug.LogError("[CharacterInstance] Cannot respawn character because model is missing.");
            return;
        }

        if (View != null)
        {
            UnityEngine.Object.Destroy(View.gameObject);
            View = null;
        }

        Model = model;
        Spawn(startCell);
    }

    public async Task MoveTo(Cell nextTarget)
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

        _eventBus.Publish(new CharacterMoved(this));
    }

    public void Destroy()
    {
        if (View != null)
        {
            UnityEngine.Object.Destroy(View.gameObject);
            View = null;
        }

        Model = null;
    }

    public void MoveToPosition(Vector3 targetPosition, float speed)
    {
        View?.MoveToPosition(targetPosition, speed);
    }
}
