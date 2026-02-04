using UnityEngine;
using UnityEngine.UI;

public class CharacterCardView : MonoBehaviour
{
    [SerializeField] private Text infoText;

    public void SetCharacter(CharacterInstance character)
    {
        if (character == null || infoText == null)
        {
            return;
        }

        var model = character.Model;
        infoText.text =
            $"Имя: {model.Name}\n" +
            $"Здоровье: {model.Health}\n" +
            $"Атака: {model.Attack}\n" +
            $"Защита: {model.Armor}\n" +
            $"Шанс уклонения: {model.DodgeChance:P0}\n" +
            $"Инициатива: {model.Initiative}\n" +
            $"Скорость: {model.Speed}\n" +
            $"Удача: {model.Luck:P0}";
    }
}
