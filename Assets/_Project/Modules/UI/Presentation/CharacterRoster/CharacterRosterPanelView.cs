using System.Collections.Generic;
using UnityEngine;

public class CharacterRosterPanelView : MonoBehaviour
{
    [SerializeField] private CharacterCardView cardTemplate;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private float cardSpacing = 12f;

    private readonly List<CharacterCardView> _cards = new();

    private void Awake()
    {
        if (cardTemplate != null)
        {
            cardTemplate.gameObject.SetActive(false);
        }
    }

    public void SetCharacters(IReadOnlyList<CharacterInstance> characters)
    {
        ClearCards();

        if (cardTemplate == null || contentRoot == null || characters == null)
        {
            return;
        }

        float yOffset = 0f;
        for (int i = 0; i < characters.Count; i++)
        {
            CharacterCardView card = Instantiate(cardTemplate, contentRoot);
            card.gameObject.SetActive(true);
            card.SetCharacter(characters[i]);

            if (card.TryGetComponent(out RectTransform rect))
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -yOffset);
                yOffset += rect.sizeDelta.y + cardSpacing;
            }

            _cards.Add(card);
        }

        contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, yOffset);
    }

    private void ClearCards()
    {
        foreach (CharacterCardView card in _cards)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }

        _cards.Clear();
    }
}
