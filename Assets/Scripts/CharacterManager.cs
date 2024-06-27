using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DG.Tweening;
using Cysharp.Threading.Tasks;

public class CharacterManager : MonoBehaviour
{
    public List<GameObject> allyPresetList = new();
    public List<GameObject> enemyPresetList = new();
    public GameObject healthBarPrefab;

    public GameObject allyParent;
    public GameObject enemyParent;
    public GameObject characterCanvas;

    public int characterStartX;
    public float characterSpaceX;

    public int healthBarStartX;
    public float healthBarSpaceX;

    private bool _characterAnimationFinish = true;

    private readonly List<GameObject> _allyOnField = new();
    private readonly List<GameObject> _allyHealthBarOnField = new();
    private readonly List<GameObject> _enemyOnField = new();
    private readonly List<GameObject> _enemyHealthBarOnField = new();
    
    private static readonly int AnimatorRun = Animator.StringToHash("Run");

    public void NoticeCharacterAnimationFinish()
    {
        _characterAnimationFinish = true;
    }

    public List<GameObject> GetAllAllyObject()
    {
        return _allyOnField;
    }

    public List<GameObject> GetAllEnemyObject()
    {
        return _enemyOnField;
    }

    public GameObject FindCharacterWithName(bool isAlly, string characterName)
    {
        return isAlly ? FindAllyWithName(characterName) : FindEnemyWithName(characterName);
    }

    public GameObject GetAllyAtIndex(int index)
    {
        return _allyOnField[index];
    }

    public GameObject GetEnemyAtIndex(int index)
    {
        return _enemyOnField[index];
    }

    public int GetIndexOfEnemy(GameObject character)
    {
        return _enemyOnField.IndexOf(character);
    }

    public float GetCharacterPositionXAtIndex(int index, bool isAlly)
    {
        return (isAlly ? -1 : 1) * (characterStartX + characterSpaceX * index);
    }

    public GameObject FindAllyWithName(string allyName)
    {
        var allyIndex = _allyOnField.FindIndex(obj => obj.name == allyName);
        if (allyIndex != -1) return _allyOnField[allyIndex];

        Debug.Log("Unable to find ally with name: " + allyName);
        return null;
    }

    public GameObject FindEnemyWithName(string enemyName)
    {
        var enemyIndex = _enemyOnField.FindIndex(obj => obj.name == enemyName);
        if (enemyIndex != -1) return _enemyOnField[enemyIndex];

        Debug.Log("Unable to find enemy with name: " + enemyName);
        return null;
    }

    public void DisableHealthBarOfCharacter(GameObject character)
    {
        var characterIndex = _allyOnField.IndexOf(character);
        if (characterIndex >= 0)
        {
            _allyHealthBarOnField[characterIndex].SetActive(false);
            return;
        }

        characterIndex = _enemyOnField.IndexOf(character);
        if (characterIndex >= 0) _enemyHealthBarOnField[characterIndex].SetActive(false);
    }

    public async UniTask CharacterAttackSequence(bool isAllyAttacking, GameObject caster, GameObject target,
        Components.CardData castingCard)
    {
        caster.GetComponent<SpriteRenderer>().sortingOrder = 1;

        // Move character in-front to cast
        if (castingCard.Actor.CastInFrontOfTarget())
        {
            _characterAnimationFinish = false; // For run animation
            await MoveCharacterInFront(caster, target, isAllyAttacking, 0, 0.4f);
            await UniTask.WaitUntil(() => _characterAnimationFinish); // For run animation
        }

        // Cast spell
        _characterAnimationFinish = false;
        castingCard.Actor.Apply(isAllyAttacking, target);
        await UniTask.WaitUntil(() => _characterAnimationFinish);

        // Move character back
        if (castingCard.Actor.CastInFrontOfTarget())
        {
            _characterAnimationFinish = false; // For run animation
            await MoveCharacterBack(caster, isAllyAttacking, 0, 0.4f);
            await UniTask.WaitUntil(() => _characterAnimationFinish); // For run animation
        }

        caster.GetComponent<SpriteRenderer>().sortingOrder = 0;
    }

    public async UniTask MoveCharacterInFront(GameObject character, GameObject to, bool isAllyMoving,
        float startDelay = 0, float lastingTime = 0.1f)
    {

        var beginPosition = character.transform.position;
        var endPosition = to.transform.position - (isAllyMoving ? 1.2f : -1.2f) * new Vector3(characterSpaceX, 0);

        // Hide health bar when moving
        var characterIndex = isAllyMoving
            ? _allyOnField.IndexOf(character)
            : _enemyOnField.IndexOf(character);
        if (characterIndex >= 0)
        {
            var healthBar = isAllyMoving
                ? _allyHealthBarOnField[characterIndex]
                : _enemyHealthBarOnField[characterIndex];
            HealthBarFade(healthBar.GetComponent<CanvasGroup>(), false, 0, 0.3f).Forget();
        }

        await CharacterMove(character, beginPosition, endPosition, startDelay, lastingTime);
    }

    public async UniTask MoveCharacterBack(GameObject character, bool isAllyMoving,
        float startDelay = 0, float lastingTime = 0.1f)
    {

        var beginPosition = character.transform.position;
        var characterIndex = isAllyMoving ? _allyOnField.IndexOf(character) : _enemyOnField.IndexOf(character);
        if (characterIndex < 0)
        {
            Debug.LogError("CharacterManager::MoveCharacterBack Character not found in record");
            return;
        }

        var endPosition =
            (isAllyMoving ? -1 : 1) * new Vector3(characterStartX + characterSpaceX * characterIndex, 0);

        // Hide health bar when moving
        var healthBar = isAllyMoving
            ? _allyHealthBarOnField[characterIndex]
            : _enemyHealthBarOnField[characterIndex];
        HealthBarFade(healthBar.GetComponent<CanvasGroup>(), true, 0, 0.3f).Forget();

        var characterRenderer = character.GetComponent<SpriteRenderer>();
        characterRenderer.flipX = isAllyMoving;

        await CharacterMove(character, beginPosition, endPosition,
            startDelay, lastingTime, () => { characterRenderer.flipX = !isAllyMoving; });
    }

    private void LoadCharacters(List<GameObject> preset, GameObject parent,
        List<GameObject> characterSaves, List<GameObject> healthSaves, bool isAlly = true)
    {
        characterSaves.ForEach(Destroy);
        characterSaves.Clear();
        healthSaves.ForEach(Destroy);
        healthSaves.Clear();

        for (var i = 0; i < preset.Count; ++i)
        {
            /*
             *
             * Health bar setup
             *
             */
            var newHealthBar = Instantiate(healthBarPrefab, characterCanvas.transform, false);
            var healthBar = newHealthBar.GetComponent<RectTransform>();
            var healthBarPosition = healthBar.anchoredPosition;
            healthBarPosition.x = (isAlly ? -1 : 1) * (healthBarStartX + healthBarSpaceX * i);
            healthBar.anchoredPosition = healthBarPosition;
            healthSaves.Add(newHealthBar);

            /*
             *
             * Character setup
             *
             */
            var newCharacter = Instantiate(
                preset[i], new Vector3(GetCharacterPositionXAtIndex(i, isAlly), 0),
                Quaternion.identity);

            var newCharacterComponent = newCharacter.GetComponent<Character>();
            newCharacterComponent.SetHealthListener((_, newHealthPercentage) =>
            {
                newHealthBar.GetComponent<UnityEngine.UI.Slider>().value = newHealthPercentage;
            });

            // Flip X on opposite side
            if (!isAlly) newCharacter.GetComponent<SpriteRenderer>()!.flipX = true;

            newCharacter.name = preset[i].name;
            newCharacter.transform.parent = parent.transform;
            characterSaves.Add(newCharacter);
        }
    }

    private async UniTask HealthBarFade(CanvasGroup canvasToFade, bool fadeIn,
        float startDelay = 0, float lastingTime = 0.5f)
    {
        if (canvasToFade == null) return;
        if (startDelay != 0) await UniTask.Delay(TimeSpan.FromSeconds(startDelay), ignoreTimeScale: false);
        canvasToFade.DOKill();
        await canvasToFade.DOFade(fadeIn ? 1 : 0, lastingTime).AsyncWaitForCompletion();
    }

    delegate void CharacterMoveCallback();

    private async UniTask CharacterMove(GameObject character, Vector3 from, Vector3 to, float startDelay = 0,
        float lastingTime = 0.1f, CharacterMoveCallback callback = null)
    {
        if (startDelay != 0) await UniTask.Delay(TimeSpan.FromSeconds(startDelay), ignoreTimeScale: false);

        character.GetComponent<Animator>().SetBool(AnimatorRun, true);
        await character.transform.DOMove(to, lastingTime).AsyncWaitForCompletion();
        character.GetComponent<Animator>().SetBool(AnimatorRun, false);

        callback?.Invoke();
    }

    public void LoadAlly()
    {
        LoadCharacters(allyPresetList, allyParent, _allyOnField, _allyHealthBarOnField, true);
    }

    public void LoadEnemy()
    {
        LoadCharacters(enemyPresetList, enemyParent, _enemyOnField, _enemyHealthBarOnField, false);
    }
}