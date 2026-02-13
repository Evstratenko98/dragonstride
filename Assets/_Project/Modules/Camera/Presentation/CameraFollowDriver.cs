using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class CameraFollowDriver : ITickable, IPostInitializable, IDisposable
{
    private readonly CameraFocusState _cameraFocusState;
    private readonly Camera _camera;
    private readonly IEventBus _eventBus;
    private readonly ConfigScriptableObject _config;
    private IDisposable _subscription;
    private IDisposable _gameStateSub;
    private IDisposable _followToggleSub;

    private float smooth = 3f;
    private Vector3 offset = new Vector3(0, 15, -15);
    private float _zoomDistance;
    private Vector3 _zoomDirection;
    private float _zoomSpeed;
    private float _zoomMinDistance;
    private float _zoomMaxDistance;
    private float _minX;
    private float _maxX;
    private float _minZ;
    private float _maxZ;

    public CameraFollowDriver(CameraFocusState cameraFocusState, Camera camera, IEventBus eventBus, ConfigScriptableObject config)
    {
        _cameraFocusState = cameraFocusState;
        _camera = camera;
        _eventBus = eventBus;
        _config = config;
    }

    public void PostInitialize()
    {
        _subscription = _eventBus.Subscribe<TurnPhaseChanged>(FocusCameraForCharacter);
        _gameStateSub = _eventBus.Subscribe<GameStateChanged>(OnStateGame);
        _followToggleSub = _eventBus.Subscribe<CameraFollowToggled>(OnFollowToggled);

        InitializeZoom();
        UpdateFieldBounds();
    }

    private void FocusCameraForCharacter(TurnPhaseChanged msg)
    {
        if (msg.State != TurnState.RollDice)
        {
            return;
        }

        _cameraFocusState.SetTarget(msg.Actor as CharacterInstance);
    }

    public void Tick()
    {
        HandleZoomInput();

        if (_cameraFocusState.FollowEnabled)
        {
            var target = _cameraFocusState.CurrentTarget;
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

    private void OnStateGame(GameStateChanged msg)
    {
        if(msg.State == GameState.Finished)
        {
            _cameraFocusState.SetTarget(null);
        }
    }

    private void OnFollowToggled(CameraFollowToggled msg)
    {
        _cameraFocusState.SetFollowEnabled(msg.IsEnabled);
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

    private void HandleZoomInput()
    {
        var scrollDelta = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(scrollDelta, 0f))
        {
            return;
        }

        _zoomDistance -= scrollDelta * _zoomSpeed * Time.deltaTime;
        _zoomDistance = Mathf.Clamp(_zoomDistance, _zoomMinDistance, _zoomMaxDistance);
        offset = _zoomDirection * _zoomDistance;
    }

    private void InitializeZoom()
    {
        _zoomDirection = offset.normalized;
        _zoomDistance = offset.magnitude;

        _zoomSpeed = _config.CAMERA_ZOOM_SPEED > 0f ? _config.CAMERA_ZOOM_SPEED : 120f;
        _zoomMinDistance = _config.CAMERA_ZOOM_MIN_DISTANCE > 0f ? _config.CAMERA_ZOOM_MIN_DISTANCE : 10f;
        _zoomMaxDistance = _config.CAMERA_ZOOM_MAX_DISTANCE > _zoomMinDistance ? _config.CAMERA_ZOOM_MAX_DISTANCE : _zoomMinDistance + 1f;
        _zoomDistance = Mathf.Clamp(_zoomDistance, _zoomMinDistance, _zoomMaxDistance);
        offset = _zoomDirection * _zoomDistance;
    }

    private void UpdateFieldBounds()
    {
        var cellSize = _config.CellDistance;
        _minX = 0f;
        _minZ = -(_config.FIELD_HEIGHT);
        _maxX = Mathf.Max(0f, (_config.FIELD_WIDTH - 1) * cellSize);
        _maxZ = Mathf.Max(0f, (_config.FIELD_HEIGHT - 1) * (cellSize / 2));
    }
}
