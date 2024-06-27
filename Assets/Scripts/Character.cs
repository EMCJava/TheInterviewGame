using UnityEngine;

public class Character : MonoBehaviour
{
    public CardDockManager cardDockManager;
    public CharacterManager characterManager;

    private int _maxHealth;
    private int _health;

    public delegate void HealthListener(int newHealth, float newHealthPercentage);

    private HealthListener _healthListener;
    private static readonly int AnimatorDead = Animator.StringToHash("Dead");

    public void SetHealthListener(HealthListener healthListener)
    {
        _healthListener = healthListener;
    }

    private void Start()
    {
        var gameManager = GameObject.FindGameObjectWithTag("GameManager")!.GetComponent<MainGameManager>();
        characterManager = gameManager.GetCharacterManager();
        cardDockManager = gameManager.GetCardDockManager();

        SetHealth(_maxHealth = 100);
    }

    public int GetHealth()
    {
        return _health;
    }

    private void SetHealth(int newHealth)
    {
        _health = Mathf.Clamp(newHealth, 0, _maxHealth);

        if (_health <= 0)
        {
            GetComponent<Animator>().SetBool(AnimatorDead, true);
            characterManager.DisableHealthBarOfCharacter(gameObject);
        }

        _healthListener?.Invoke(_health, (float)_health / _maxHealth);
    }

    // Called when user click on sprite
    private void OnMouseDown()
    {
        cardDockManager.ReportCharacterClicked(gameObject);
    }

    public void ChangeHealth(int deltaHealth)
    {
        SetHealth(_health + deltaHealth);
    }
}