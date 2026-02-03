using UnityEngine;
using UnityEngine.UI;

public class CharacterCardView : MonoBehaviour
{
    [SerializeField] private Image portraitImage;
    [SerializeField] private Text infoText;

    public void SetCharacter(CharacterInstance character)
    {
        if (character == null || infoText == null)
        {
            return;
        }

        var model = character.Model;
        infoText.text =
            $"Имя: {character.Name}\n" +
            $"Здоровье: {model.Health}\n" +
            $"Атака: {model.Attack}\n" +
            $"Защита: {model.Armor}\n" +
            $"Шанс уклонения: {model.DodgeChance:P0}\n" +
            $"Инициатива: {model.Initiative}\n" +
            $"Скорость: {model.Speed}\n" +
            $"Удача: {model.Luck:P0}";
    }

    public void SetPortrait(Sprite sprite)
    {
        if (portraitImage == null)
        {
            return;
        }

        portraitImage.sprite = sprite;
    }
}
