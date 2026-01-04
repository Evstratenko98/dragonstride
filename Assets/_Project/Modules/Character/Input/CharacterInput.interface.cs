using UnityEngine;

public interface ICharacterInput
{
    Vector2 Move { get; }
    Vector2Int Dir { get; }
}
