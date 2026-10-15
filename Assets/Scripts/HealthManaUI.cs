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

    [Header("Health Settings")]
    [SerializeField] private List<GameObject> healthList = new();

    private void Start()
    {
        if (!manaFill) 
            manaFill = GameObject.Find("HP Fill").GetComponentInChildren<Image>();

        if (!manaBackground) 
            manaBackground = GameObject.Find("HP Background").GetComponentInChildren<Image>();
        
        if (healthList.Count == 0)
        {
            GridLayoutGroup healthGrid = GetComponentInChildren<GridLayoutGroup>();
            if (healthGrid != null)
            {
                foreach (Image image in healthGrid.GetComponentsInChildren<Image>())
                    healthList.Add(image.gameObject);
            }
        }

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
        SetHealth((float)newHealth);
    }

    public void SetHealth(float newHealth)
    {
        float displayedHealth = Mathf.Clamp(newHealth, 0f, healthList.Count);
        UpdateHealthVisuals(displayedHealth);
    }

    private void UpdateHealthVisuals(float displayedHealth)
    {
        for (int i = 0; i < healthList.Count; i++)
        {
            if (i < displayedHealth)
                healthList[i].SetActive(true);
            else
                healthList[i].SetActive(false);
        }
    }
}