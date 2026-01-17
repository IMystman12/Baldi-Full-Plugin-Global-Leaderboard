
using HarmonyLib;
using TMPro;

public class MgmScoreUpdater : ScoreUpdater
{
    protected override string nameKey => minigame.ToString();
    MinigameName minigame;
    MinigameHighScoreTable scoreTable;
    public void Set(MinigameName minigameNew, MinigameHighScoreTable scoreTableNew)
    {
        minigame = minigameNew;
        scoreTable = scoreTableNew;
        LoadScore();
    }
    protected override void UpdateScore()
    {
        Traverse traverse = Traverse.Create(scoreTable);
        TMP_Text[] nameTmp = traverse.Field("nameTmp").GetValue<TMP_Text[]>();
        TMP_Text[] valueTmp = traverse.Field("valueTmp").GetValue<TMP_Text[]>();
        for (int i = 0; i < nameTmp.Length; i++)
        {
            if (i < result.Result.Leaderboard.Count)
            {
                nameTmp[i].text = result.Result.Leaderboard[i].DisplayName;
                valueTmp[i].text = result.Result.Leaderboard[i].StatValue.ToString();
                if (LeaderboardManager.playfabId == result.Result.Leaderboard[i].PlayFabId)
                {
                    nameTmp[i].color = UnityEngine.Color.green;
                    valueTmp[i].color = UnityEngine.Color.green;
                }
            }
            else
            {
                nameTmp[i].text = "!Unassigned";
                valueTmp[i].text = i.ToString();
                nameTmp[i].color = UnityEngine.Color.red;
                valueTmp[i].color = UnityEngine.Color.red;
            }
        }
    }

    protected override void SetLoadingScreen()
    {
        Traverse traverse = Traverse.Create(scoreTable);
        TMP_Text[] nameTmp = traverse.Field("nameTmp").GetValue<TMP_Text[]>();
        TMP_Text[] valueTmp = traverse.Field("valueTmp").GetValue<TMP_Text[]>();
        for (int i = 0; i < nameTmp.Length; i++)
        {
            nameTmp[i].text = "LOADING!";
            valueTmp[i].text = "99";
        }
    }
}
