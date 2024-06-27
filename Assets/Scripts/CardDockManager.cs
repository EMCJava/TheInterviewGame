using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityEngine.Serialization;

public class CardDockManager : MonoBehaviour
{
    public GameObject cardCanvas;
    public GameObject cardPrefab;
    public GameObject aimDot;
    public CardManager cardManager;
    public CharacterManager characterManager;
    public MainGameManager gameManager;

    public int cardFireCount;

    public float cardSpacing;
    public float cardStartX;

    public float cardUnselectedY;
    public float cardSelectedY;

    private bool _fullDockAnimationComplete = true;
    private bool _finishAllCasting = true;

    private int _selectedTargetEnemyIndex = 0;

    private readonly List<GameObject> _cardDock = new();

    private struct SelectionRecord
    {
        public readonly GameObject SelectedCardObject;
        public int TargetEnemyIndex;

        public SelectionRecord(GameObject selectedCardObject, int targetEnemyIndex)
        {
            SelectedCardObject = selectedCardObject;
            TargetEnemyIndex = targetEnemyIndex;
        }
    }

    // private readonly HashSet<int> _selectedCard = new();
    private readonly List<SelectionRecord> _cardSelectionRecord = new();

    public void ReportCharacterClicked(GameObject character)
    {
        var clickedCharacter = characterManager.GetIndexOfEnemy(character);
        if (clickedCharacter < 0) return;

        // Target enemy selection
        var aimDotPosition = aimDot.transform.position;
        aimDotPosition.x = characterManager.GetCharacterPositionXAtIndex(clickedCharacter, false);
        aimDot.transform.position = aimDotPosition;

        _selectedTargetEnemyIndex = clickedCharacter;
    }

    private void ClearDock()
    {
        _cardDock.ForEach(Destroy);
        _cardDock.Clear();

        _cardSelectionRecord.Clear();
    }

    public void SpawnCards(List<string> cardNameList)
    {
        ClearDock();

        for (var i = 0; i < cardNameList.Count; i++)
        {
            var currentCardIndex = cardManager.GetCardIndexByName(cardNameList[i]);
            if (currentCardIndex == -1) continue;

            var newCardObj = Instantiate(cardPrefab, cardCanvas.transform);
            newCardObj.name = cardNameList[i];
            _cardDock.Add(newCardObj);

            // Modify position
            var cardRT = newCardObj.GetComponent<RectTransform>();
            cardRT.anchoredPosition = new Vector2(cardStartX - i * cardSpacing, cardUnselectedY);

            // Add click listener
            var cardBt = newCardObj.GetComponent<UnityEngine.UI.Button>();
            cardBt.onClick.AddListener(() => { OnCardInteract(newCardObj, currentCardIndex); });

            // Change card appearance
            var cardData = cardManager.GetCardDateAt(currentCardIndex);
            var tmpObject = newCardObj.transform.GetChild(0)?.gameObject;
            var cardText = tmpObject?.GetComponent<TMPro.TMP_Text>();
            if (cardText != null) cardText.text = cardData.displayName;

            if (ColorUtility.TryParseHtmlString(cardData.color, out var cardColor))
            {
                var cardImage = newCardObj.GetComponent<UnityEngine.UI.Image>();
                cardImage.color = cardColor;
            }
        }
    }

    private async UniTaskVoid FireSelected()
    {
        _finishAllCasting = false;

        aimDot.GetComponent<SpriteRenderer>().DOFade(0, 0.4f).OnComplete(() => { aimDot.SetActive(false); });

        // Remove selected from dock
        _cardDock.RemoveAll(card =>
            _cardSelectionRecord.Exists(selectCard =>
                selectCard.SelectedCardObject == card)
        );

        StartDockFillSpaceAnimation().Forget();

        // Move selected card to upper level for casting
        const float cardMoveToFireAreaTime = 0.2f;
        for (var i = _cardSelectionRecord.Count - 1; i >= 0; --i)
        {
            var selectedCard = _cardSelectionRecord[_cardSelectionRecord.Count - i - 1].SelectedCardObject;
            var cardRT = selectedCard.GetComponent<RectTransform>();
            var endPosition = new Vector2(cardStartX - i * cardSpacing, cardUnselectedY + 300);
            cardRT.DOAnchorPos(endPosition, 0.1f);

            await UniTask.Delay(TimeSpan.FromSeconds(cardMoveToFireAreaTime / 5), ignoreTimeScale: false);
        }

        // Wait for a second after all card finish moving
        await UniTask.Delay(TimeSpan.FromSeconds(cardMoveToFireAreaTime + 1f), ignoreTimeScale: false);

        foreach (var castRecord in _cardSelectionRecord)
        {
            var cardIndex = cardManager.GetCardIndexByName(castRecord.SelectedCardObject.name);
            Destroy(castRecord.SelectedCardObject);
            if (cardIndex < 0) continue;

            var castingCard = cardManager.GetCardDateAt(cardIndex)!;
            var caster = castingCard.Actor.GetCaster(true);
            await characterManager.CharacterAttackSequence(
                true, caster, characterManager.GetEnemyAtIndex(castRecord.TargetEnemyIndex), castingCard);

            // Small delay after came back
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f), ignoreTimeScale: false);
        }

        _cardSelectionRecord.Clear();

        gameManager.SetIsPlayerTurn(false);
        _finishAllCasting = true;
    }

    private async UniTaskVoid StartDockFillSpaceAnimation()
    {
        _fullDockAnimationComplete = false;
        for (var i = 0; i < _cardDock.Count; i++)
        {
            var cardRT = _cardDock[i].gameObject.GetComponent<RectTransform>();
            var endPosition = new Vector2(cardStartX - i * cardSpacing, cardUnselectedY);
            cardRT.DOKill();
            var cardAnim = cardRT.DOAnchorPos(endPosition, 0.1f);
            await UniTask.Delay(TimeSpan.FromSeconds(0.05f), ignoreTimeScale: false);

            if (i == _cardDock.Count - 1)
            {
                cardAnim.OnComplete(() => { _fullDockAnimationComplete = true; });
            }
        }
    }

    public void OnCardInteract(GameObject card, int cardIndex)
    {
        if (!gameManager.IsPlayerTurn()) return;
        if (!_finishAllCasting) return;
        if (!_fullDockAnimationComplete) return;

        var oldSelectionIndex = _cardSelectionRecord.FindIndex(selectedCard => selectedCard.SelectedCardObject == card);
        var selected = oldSelectionIndex >= 0;

        // Setup for "on/off" operation
        var finalPositionY = selected ? cardUnselectedY : cardSelectedY;

        // Toggle selection
        if (selected) _cardSelectionRecord.RemoveAt(oldSelectionIndex);
        else _cardSelectionRecord.Add(new SelectionRecord(card, _selectedTargetEnemyIndex));

        // Create new animation
        var cardRT = card.GetComponent<RectTransform>();
        cardRT.DOKill();
        cardRT.DOAnchorPosY(finalPositionY, 0.1f);

        if (_cardSelectionRecord.Count == cardFireCount)
        {
            FireSelected().Forget();
        }
    }
}