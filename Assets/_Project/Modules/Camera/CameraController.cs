using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class CameraController : ITickable, IPostInitializable, IDisposable
{
    private readonly CameraService _cameraService;
    private readonly Camera _camera;
    private readonly IEventBus _eventBus;
    private IDisposable _subscription;
    private IDisposable _gameStateSub;

    private float smooth = 3f;
    private Vector3 offset = new Vector3(0, 15, -15);

    public CameraController(CameraService cameraService, Camera camera, IEventBus eventBus)
    {
        _cameraService = cameraService;
        _camera = camera;
        _eventBus = eventBus;
    }

    public void PostInitialize()
    {
        _subscription = _eventBus.Subscribe<TurnStateChangedMessage>(FocusCameraForCharacter);
        _gameStateSub = _eventBus.Subscribe<GameStateChangedMessage>(OnStateGame);
    }

    private void FocusCameraForCharacter(TurnStateChangedMessage msg)
    {
        if(msg.State == TurnState.Start)
            _cameraService.SetTarget(msg.Character);
    }

    public void Tick()
    {
        var target = _cameraService.CurrentTarget;
        if (target == null) return;

        Vector3 targetPos = target.View.transform.position + offset;

        _camera.transform.position = Vector3.Lerp(
            _camera.transform.position,
            targetPos,
            Time.deltaTime * smooth);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
        _gameStateSub?.Dispose();
    }

    private void OnStateGame(GameStateChangedMessage msg)
    {
        if(msg.State == GameState.Finished)
        {
            _cameraService.SetTarget(null);
        }
    }
}
