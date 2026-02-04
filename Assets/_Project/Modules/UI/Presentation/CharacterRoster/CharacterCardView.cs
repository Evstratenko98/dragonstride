using UnityEngine;
using UnityEngine.UI;

public class CharacterCardView : MonoBehaviour
{
    [SerializeField] private Text infoText;
    private CharacterInstance _character;

    public void SetCharacter(CharacterInstance character)
    {
        if (_character != null && _character.Model != null)
        {
            _character.Model.StatsChanged -= UpdateInfo;
        }

        _character = character;

        if (_character != null && _character.Model != null)
        {
            _character.Model.StatsChanged += UpdateInfo;
        }

        UpdateInfo();
    }

    private void OnDisable()
    {
        if (_character != null && _character.Model != null)
        {
            _character.Model.StatsChanged -= UpdateInfo;
        }
    }

    private void UpdateInfo()
    {
        if (_character == null || _character.Model == null || infoText == null)
        {
            return;
        }

        var model = _character.Model;
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
