using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Random = UnityEngine.Random;

public class EnemyAI : MonoBehaviour
{
    public MainGameManager gameManager;
    public CardManager cardManager;
    public CharacterManager characterManager;

    private bool _shouldInitializeAttack = true;

    private void OnEnable()
    {
        if (gameManager.IsPlayerTurn() || !_shouldInitializeAttack)
        {
            Debug.LogError("EnemyAI enabled when it should not be");
            return;
        }

        _shouldInitializeAttack = false;
        InitializeAttack().Forget();
    }

    private async UniTaskVoid InitializeAttack()
    {
        var listOfAvailableCharacterToAttack = characterManager.GetAllAllyObject();
        var listOfAvailableCharacter = characterManager.GetAllEnemyObject();
        foreach (var enemyObj in listOfAvailableCharacter.OrderBy(_ => Random.value))
        {
            if (enemyObj.GetComponent<Character>().GetHealth() <= 0) continue;

            var availableCards = cardManager.GetAvailableCardsByCharacter(enemyObj.name);
            var card = availableCards[Random.Range(0, availableCards.Count)];
            var target = listOfAvailableCharacterToAttack[Random.Range(0, listOfAvailableCharacterToAttack.Count)];

            await characterManager.CharacterAttackSequence(false, enemyObj, target, card);

            // Small delay after came back
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f), ignoreTimeScale: false);
        }

        _shouldInitializeAttack = true;
        gameManager.SetIsPlayerTurn(true);
    }
}