using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LeaderboardRowView : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text rankText;
    public TMP_Text nameText;
    public TMP_Text scoreText;
    public GameObject highlightRoot;

    public void Bind(int rank1Based, string playerName, int score, bool highlight)
    {
        if (rankText != null) rankText.text = rank1Based.ToString();
        if (nameText != null) nameText.text = playerName;
        if (scoreText != null) scoreText.text = score.ToString();

        if (highlightRoot != null) highlightRoot.SetActive(highlight);
    }
}
