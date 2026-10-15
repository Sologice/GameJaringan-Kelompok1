using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
	[SerializeField] private float maxHealth = 10f;
	[SerializeField] private float currentHealth = 10f;

	public event Action<float, float> OnHealthChanged;

	public float CurrentHealth => currentHealth;
	public float MaxHealth => maxHealth;

	private void Awake()
	{
		if (float.IsNaN(maxHealth) || float.IsInfinity(maxHealth))
			maxHealth = 0f;
		else
			maxHealth = Mathf.Max(0f, maxHealth);

		currentHealth = float.IsNaN(currentHealth) || float.IsInfinity(currentHealth)
			? maxHealth
			: Mathf.Clamp(currentHealth, 0f, maxHealth);
	}

	public void TakeDamage(float amount)
	{
		if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;

		SetHealth(currentHealth - amount);
	}

	public void Heal(float amount)
	{
		if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;

		SetHealth(currentHealth + amount);
	}

	public void SetHealth(float newHealth)
	{
		if (float.IsNaN(newHealth) || float.IsInfinity(newHealth)) return;

		float clampedHealth = Mathf.Clamp(newHealth, 0f, maxHealth);
		if (Mathf.Approximately(clampedHealth, currentHealth)) return;

		currentHealth = clampedHealth;
		OnHealthChanged?.Invoke(currentHealth, maxHealth);
	}
}
