using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Player { Player1, Player2 }

public class HealthManaUI : MonoBehaviour
{
    public Player player;

    [Header("Mana Stats")]
    [SerializeField] private int maxMana = 10;
    [SerializeField] private float currentMana = 0;
    [SerializeField] private float currentFill;

    [Header("Mana Settings")]
    [SerializeField] private Image manaFill;
    [SerializeField] private Image manaBackground;

    [Header("Health Stats")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int currentHealth = 5;

    [Header("Health Settings")]
    [SerializeField] private List<GameObject> healthList = new();

    private void Start()
    {
        if (!manaFill) 
            manaFill = GameObject.Find("HP Fill").GetComponentInChildren<Image>();

        if (!manaBackground) 
            manaBackground = GameObject.Find("HP Background").GetComponentInChildren<Image>();

        currentFill = currentMana / maxMana;
        UpdateVisual(currentFill);
    }

    private void Update()
    {
        currentFill = currentMana / maxMana;
        UpdateVisual(currentFill);
    }

    private void UpdateVisual(float fillAmount)
    {
        if (!manaFill) return;

        manaFill.rectTransform.anchorMax = new(fillAmount, 1);
    }

    public void SetMana(float newMana)
    {
        if (newMana <= maxMana) currentMana = newMana;
    }

    public void SetHealth(int newHealth)
    {
        if (newHealth <= maxHealth) currentHealth = newHealth;

        for (int i = 0; i < healthList.Count; i++)
        {
            if (i < currentHealth)
                healthList[i].SetActive(true);
            else
                healthList[i].SetActive(false);
        }
    }
}