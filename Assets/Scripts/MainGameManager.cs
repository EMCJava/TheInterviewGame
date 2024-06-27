using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class MainGameManager : MonoBehaviour
{
    public CardDockManager cardDockManager;
    public CharacterManager characterManager;
    public CardManager cardManager;

    public GameObject enemyAI;
    public GameObject aimDot;

    private int _frameRate;
    private bool _isPlayerTurn;

    public CardDockManager GetCardDockManager()
    {
        return cardDockManager;
    }

    public CharacterManager GetCharacterManager()
    {
        return characterManager;
    }

    public void SetIsPlayerTurn(bool isPlayer)
    {
        _isPlayerTurn = isPlayer;
        if (_isPlayerTurn)
        {
            aimDot.SetActive(true);
            aimDot.GetComponent<SpriteRenderer>().DOFade(1, 0.15f);
            enemyAI.SetActive(false);
        }
        else
        {
            enemyAI.SetActive(true);
        }
    }

    public bool IsPlayerTurn()
    {
        return _isPlayerTurn;
    }

    // Start is called before the first frame update
    private void Start()
    {
        // Place characters
        characterManager.LoadAlly();
        characterManager.LoadEnemy();

        // Get available cards
        List<string> cardNameList = new();
        characterManager.GetAllAllyObject().ForEach(ally =>
            cardManager.GetAvailableCardsByCharacter(ally.name).ForEach(card =>
                cardNameList.Add(card.name)));

        // Eight card for each
        cardNameList.AddRange(cardNameList);
        cardNameList.AddRange(cardNameList);
        cardNameList.AddRange(cardNameList);

        // Place cards
        SetIsPlayerTurn(true);
        cardDockManager.SpawnCards(cardNameList.OrderBy(_ => Random.value).ToList());
    }
}