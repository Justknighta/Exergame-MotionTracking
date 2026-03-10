using UnityEngine;

public class HUDController : MonoBehaviour
{
    public void SetHeartsVisible(bool visible) { }
    public void SetHearts(int hearts) { }
    public void SetScore(int score) { }
    public void ShowBossHP(int hp) { }
    public void SetBossHP(int hp) { }
    public void HideBossHP() { }

    public void ShowResultFeedback(object grade, int dmg, float delay) { }
}