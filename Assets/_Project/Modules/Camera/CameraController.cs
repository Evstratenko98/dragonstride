using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class CameraController : ITickable, IPostInitializable, IDisposable
{
    private readonly CameraService _cameraService;
    private readonly Camera _camera;
    private readonly IEventBus _eventBus;
    private readonly ConfigScriptableObject _config;
    private IDisposable _subscription;
    private IDisposable _gameStateSub;
    private IDisposable _followToggleSub;

    private float smooth = 3f;
    private Vector3 offset = new Vector3(0, 15, -15);
    private float _minX;
    private float _maxX;
    private float _minZ;
    private float _maxZ;

    public CameraController(CameraService cameraService, Camera camera, IEventBus eventBus, ConfigScriptableObject config)
    {
        _cameraService = cameraService;
        _camera = camera;
        _eventBus = eventBus;
        _config = config;
    }

    public void PostInitialize()
    {
        _subscription = _eventBus.Subscribe<TurnStateChangedMessage>(FocusCameraForCharacter);
        _gameStateSub = _eventBus.Subscribe<GameStateChangedMessage>(OnStateGame);
        _followToggleSub = _eventBus.Subscribe<CameraFollowToggledMessage>(OnFollowToggled);

        UpdateFieldBounds();
    }

    private void FocusCameraForCharacter(TurnStateChangedMessage msg)
    {
        if(msg.State == TurnState.Start)
            _cameraService.SetTarget(msg.Character);
    }

    public void Tick()
    {
        if (_cameraService.FollowEnabled)
        {
            var target = _cameraService.CurrentTarget;
            if (target == null)
            {
                return;
            }

            Vector3 targetPos = target.View.transform.position + offset;

            _camera.transform.position = Vector3.Lerp(
                _camera.transform.position,
                targetPos,
                Time.deltaTime * smooth);

            return;
        }

        HandleEdgePanning();
    }

    public void Dispose()
    {
        _subscription?.Dispose();
        _gameStateSub?.Dispose();
        _followToggleSub?.Dispose();
    }

    private void OnStateGame(GameStateChangedMessage msg)
    {
        if(msg.State == GameState.Finished)
        {
            _cameraService.SetTarget(null);
        }
    }

    private void OnFollowToggled(CameraFollowToggledMessage msg)
    {
        _cameraService.SetFollowEnabled(msg.IsEnabled);
    }

    private void HandleEdgePanning()
    {
        var mousePosition = Input.mousePosition;
        var screenWidth = Screen.width;
        var screenHeight = Screen.height;
        var threshold = Mathf.Max(0f, _config.CAMERA_EDGE_THRESHOLD);

        var direction = Vector3.zero;

        if (mousePosition.x <= threshold)
        {
            direction.x -= 1f;
        }
        else if (mousePosition.x >= screenWidth - threshold)
        {
            direction.x += 1f;
        }

        if (mousePosition.y <= threshold)
        {
            direction.z -= 1f;
        }
        else if (mousePosition.y >= screenHeight - threshold)
        {
            direction.z += 1f;
        }

        if (direction.sqrMagnitude <= 0f)
        {
            return;
        }

        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        var currentPosition = _camera.transform.position;
        var nextPosition = currentPosition + direction * _config.CAMERA_PAN_SPEED * Time.deltaTime;
        nextPosition.x = Mathf.Clamp(nextPosition.x, _minX, _maxX);
        nextPosition.z = Mathf.Clamp(nextPosition.z, _minZ, _maxZ);
        _camera.transform.position = nextPosition;
    }

    private void UpdateFieldBounds()
    {
        var cellSize = _config.CELL_SIZE;
        _minX = 0f;
        _minZ = -(_config.FIELD_HEIGHT / 2);
        _maxX = Mathf.Max(0f, (_config.FIELD_WIDTH - 1) * cellSize);
        _maxZ = Mathf.Max(0f, (_config.FIELD_HEIGHT - 1) * (cellSize / 2));
    }
}
