using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public TextAsset cardConfig;

    private List<Components.CardData> _cardList;

    private void Awake()
    {
        LoadCardAssert();
    }

    private void LoadCardAssert()
    {
        _cardList = JsonUtility.FromJson<Components.CardDataList>(cardConfig.text).config
            .Select(
                card =>
                {
                    card.LoadActor();
                    return card;
                }).ToList();
    }

    public int GetCardIndexByName(string cardName)
    {
        return _cardList.FindIndex(card => card.name == cardName);
    }

    public List<Components.CardData> GetAvailableCardsByCharacter(string characterName)
    {
        return _cardList.FindAll(card => card.characterName == characterName).ToList();
    }

    public Components.CardData GetCardDateAt(int index)
    {
        Debug.Assert(index < _cardList.Count);
        return _cardList[index];
    }
}