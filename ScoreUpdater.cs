using System.Collections;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using static LeaderboardManager;

public class ScoreUpdater : MonoBehaviour
{
    protected virtual string nameKey => "Rank";
    protected PlayFabResult<GetLeaderboardResult> result;
    IEnumerator enumerator;
    static int callCount;
    public void LoadScore()
    {
        scoreUpdater = this;
        SetLoadingScreen();
        if (enumerator != null)
        {
            Instance.StopCoroutine(enumerator);
        }
        enumerator = Get(nameKey);
        Instance.StartCoroutine(enumerator);
    }
    IEnumerator Get(string key)
    {
        if (callCount > 2)
        {
            PrintBase($"Loading too many times and resting for 1 minute!");
            yield return new WaitForSecondsRealtime(1f);
            callCount = 0;
            PrintBase($"Reset done!");
        }
        callCount++;

        using (var task = PlayFabClientAPI.GetLeaderboardAsync(new GetLeaderboardRequest() { StatisticName = key }))
        {
            while (!task.IsCompleted && !task.IsCompletedSuccessfully)
            {
                yield return task;
            }
            result = task.Result;
        }

        if (result == null)
        {
            PrintBase($"No leaderboard_{nameKey} found!");
            yield break;
        }
        UpdateScore();
    }
    protected virtual void SetLoadingScreen()
    {
    }
    protected virtual void UpdateScore()
    {
    }
}