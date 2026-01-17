
using HarmonyLib;

public class EndlessScoreUpdater : ScoreUpdater
{
    protected override string nameKey => field.GetValue<string>();
    EndlessMapOverview overview;
    Traverse traverse, field;
    public void Set(EndlessMapOverview overview)
    {
        this.overview = overview;
        traverse = Traverse.Create(overview);
        field = traverse.Field("levelId");
        LoadScore();
    }

    protected override void SetLoadingScreen()
    {
        for (int i = 0; i < overview.listings.Length; i++)
        {
            overview.listings[i].name.text = "LOADING!";
            overview.listings[i].score.text = "99";
        }
    }

    protected override void UpdateScore()
    {
        for (int i = 0; i < overview.listings.Length; i++)
        {
            if (i < result.Result.Leaderboard.Count)
            {
                overview.listings[i].name.text = result.Result.Leaderboard[i].DisplayName;
                overview.listings[i].score.text = result.Result.Leaderboard[i].StatValue.ToString();
                if (LeaderboardManager.playfabId == result.Result.Leaderboard[i].PlayFabId)
                {
                    overview.listings[i].name.color = UnityEngine.Color.green;
                    overview.listings[i].score.color = UnityEngine.Color.green;
                }
            }
            else
            {
                overview.listings[i].name.text = "!Unassigned";
                overview.listings[i].score.text = i.ToString();
                overview.listings[i].name.color = UnityEngine.Color.red;
                overview.listings[i].score.color = UnityEngine.Color.red;
            }
        }
    }
}
