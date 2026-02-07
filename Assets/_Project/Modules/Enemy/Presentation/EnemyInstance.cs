using UnityEngine;

public sealed class EnemyInstance : ICellLayoutOccupant
{
    private readonly ConfigScriptableObject _config;
    private readonly Enemy _model;
    private readonly GameObject _prefab;
    private readonly FieldRoot _fieldRoot;

    private EnemyMover _mover;

    public Enemy EntityModel => _model;
    public Entity Entity => _model;
    public GameObject View { get; private set; }

    public EnemyInstance(
        ConfigScriptableObject config,
        Enemy model,
        GameObject prefab,
        FieldRoot fieldRoot
    )
    {
        _config = config;
        _model = model;
        _prefab = prefab;
        _fieldRoot = fieldRoot;
    }

    public void Spawn(Cell cell)
    {
        if (cell == null)
        {
            Debug.LogError("[EnemyInstance] Cannot spawn enemy because cell is null.");
            return;
        }

        if (_prefab == null)
        {
            Debug.LogError("[EnemyInstance] Cannot spawn enemy because prefab is missing.");
            return;
        }

        _model.SetCell(cell);
        Transform parent = _fieldRoot?.EnsureRoot();
        View = parent == null
            ? Object.Instantiate(_prefab)
            : Object.Instantiate(_prefab, parent);

        if (View == null)
        {
            Debug.LogError("[EnemyInstance] Enemy prefab instantiation failed.");
            return;
        }

        View.name = _model.Name;
        _mover = View.GetComponent<EnemyMover>();
        if (_mover == null)
        {
            _mover = View.AddComponent<EnemyMover>();
        }

        View.transform.position = GetPosition(cell);
    }

    public bool MoveTo(Cell targetCell)
    {
        if (targetCell == null || _model.CurrentCell == null)
        {
            return false;
        }

        if (!_model.CurrentCell.CanMoveTo(targetCell))
        {
            return false;
        }

        _model.SetCell(targetCell);
        MoveToPosition(GetPosition(targetCell), _config.CHARACTER_SPEED);
        return true;
    }

    public void MoveToPosition(Vector3 targetPosition, float speed)
    {
        if (_mover != null)
        {
            _mover.MoveToPosition(targetPosition, speed);
            return;
        }

        if (View != null)
        {
            View.transform.position = targetPosition;
        }
    }

    public void Destroy()
    {
        if (View != null)
        {
            Object.Destroy(View);
            View = null;
            _mover = null;
        }

        _model.SetCell(null);
    }

    private Vector3 GetPosition(Cell cell)
    {
        return new Vector3(
            cell.X * _config.CELL_SIZE,
            _config.CHARACTER_HEIGHT,
            cell.Y * _config.CELL_SIZE
        );
    }
}
