using TMPro;
using UnityEngine;

/// <summary>Attached to the LeaderboardItemPrefab. Pure view: just fills its 3 texts.</summary>
public class LeaderboardItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI scoreText;

    public void SetData(int rank, string username, int score)
    {
        if (rankText     != null) rankText.text     = $"#{rank}";
        if (usernameText != null) usernameText.text = username;
        if (scoreText    != null) scoreText.text    = $"{ScoreRules.Format(score)} Pts";
    }
}