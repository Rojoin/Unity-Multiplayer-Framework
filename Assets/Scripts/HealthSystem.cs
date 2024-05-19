using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class HealthSystem : MonoBehaviour
{
    [Header("HealthValues")] 
    [SerializeField] protected int maxHealthPoints;
    [SerializeField] protected float _currentHealth;
    
    [Header("Unity Events")]
    public UnityEvent onDeath;
    public UnityEvent onHit;



    /// <summary>
    /// Value for the current health of the character
    /// Every time its sets Raises the FillUI Channel
    /// </summary>
    public float CurrentHealth
    {
        get => _currentHealth;
        set
        {
            _currentHealth = value;
        }
    }
    private void Start()
    {
        CurrentHealth = maxHealthPoints;
    }
    public virtual void ReceiveDamage(float damage)
    {
        CurrentHealth -= damage;
        onHit.Invoke();
    }

    public bool IsAlive()
    {
        return (CurrentHealth > 0);
    }
    public abstract void Deactivate();

    public float GetCurrentFillValue()
    {
        return CurrentHealth;
    }

    public float GetMaxFillValue()
    {
        return maxHealthPoints;
    }
    
}