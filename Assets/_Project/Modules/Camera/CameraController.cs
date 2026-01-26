using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class CameraController : ITickable, IPostInitializable, IDisposable
{
    private readonly CameraService _cameraService;
    private readonly Camera _camera;
    private readonly IEventBus _eventBus;
    private readonly float _panSpeed;
    private readonly float _edgeThreshold;
    private IDisposable _subscription;
    private IDisposable _gameStateSub;
    private IDisposable _followSub;

    private float smooth = 3f;
    private Vector3 offset = new Vector3(0, 15, -15);

    public CameraController(CameraService cameraService, Camera camera, IEventBus eventBus, ConfigScriptableObject config)
    {
        _cameraService = cameraService;
        _camera = camera;
        _eventBus = eventBus;
        _panSpeed = config.CAMERA_PAN_SPEED;
        _edgeThreshold = config.CAMERA_EDGE_THRESHOLD;
    }

    public void PostInitialize()
    {
        _subscription = _eventBus.Subscribe<TurnStateChangedMessage>(FocusCameraForCharacter);
        _gameStateSub = _eventBus.Subscribe<GameStateChangedMessage>(OnStateGame);
        _followSub = _eventBus.Subscribe<CameraFollowToggledMessage>(OnCameraFollowToggled);
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
            if (target == null) return;

            Vector3 targetPos = target.View.transform.position + offset;

            _camera.transform.position = Vector3.Lerp(
                _camera.transform.position,
                targetPos,
                Time.deltaTime * smooth);
            return;
        }

        Vector3 mousePosition = Input.mousePosition;
        Vector3 panDirection = Vector3.zero;

        if (mousePosition.x <= _edgeThreshold)
            panDirection.x -= 1f;
        else if (mousePosition.x >= Screen.width - _edgeThreshold)
            panDirection.x += 1f;

        if (mousePosition.y <= _edgeThreshold)
            panDirection.z -= 1f;
        else if (mousePosition.y >= Screen.height - _edgeThreshold)
            panDirection.z += 1f;

        if (panDirection.sqrMagnitude > 0f)
            _camera.transform.position += panDirection.normalized * _panSpeed * Time.deltaTime;
    }

    public void Dispose()
    {
        _subscription?.Dispose();
        _gameStateSub?.Dispose();
        _followSub?.Dispose();
    }

    private void OnStateGame(GameStateChangedMessage msg)
    {
        if(msg.State == GameState.Finished)
        {
            _cameraService.SetTarget(null);
        }
    }

    private void OnCameraFollowToggled(CameraFollowToggledMessage msg)
    {
        _cameraService.SetFollowEnabled(msg.IsEnabled);
    }
}
