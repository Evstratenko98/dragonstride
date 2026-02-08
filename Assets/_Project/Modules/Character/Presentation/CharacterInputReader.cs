using System;
using UnityEngine.InputSystem;
using UnityEngine;

public class CharacterInputReader : IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly InputSystem.InputSystem _actions;
    private IDisposable _gameStateSub;

    private Vector2Int _dir;
    public Vector2Int Dir => _dir;

    private Vector2 _move;
    public Vector2 Move => _move;

    private GameState _gameState = GameState.Init;

    public CharacterInputReader(IEventBus eventBus)
    {
        _eventBus = eventBus;

        _actions = new InputSystem.InputSystem();

        _actions.Character.Move.performed += OnMove;
        _actions.Character.Move.canceled  += OnMoveCanceled;
        _actions.Character.EndTurn.performed += OnEndTurn;

        _actions.Character.Enable();
    }
    public void StartListening()
    {
        _gameStateSub = _eventBus.Subscribe<GameStateChanged>(OnStateGame);
    }

    private void OnStateGame(GameStateChanged msg)
    {
        _gameState = msg.State;
        Debug.Log(_gameState);
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        _move = ctx.ReadValue<Vector2>();

        if      (_move.y >  0.5f) _dir = Vector2Int.up;
        else if (_move.y < -0.5f) _dir = Vector2Int.down;
        else if (_move.x < -0.5f) _dir = Vector2Int.left;
        else if (_move.x >  0.5f) _dir = Vector2Int.right;
        else                      _dir = Vector2Int.zero;
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        _move = Vector2.zero;
        _dir = Vector2Int.zero;
    }

    private void OnEndTurn(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            _eventBus.Publish(new EndTurnRequested());
        }
    }

    public void Dispose()
    {
        _gameStateSub?.Dispose();

        _actions.Character.Move.performed -= OnMove;
        _actions.Character.Move.canceled  -= OnMoveCanceled;
        _actions.Character.EndTurn.performed -= OnEndTurn;

        _actions.Character.Disable();
        _actions.Dispose();
    }
}
