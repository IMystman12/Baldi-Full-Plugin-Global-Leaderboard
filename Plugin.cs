using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using BepInEx;
using HarmonyLib;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

[BepInPlugin("baldifull.global.leaderboard", "Baldi Full Plugin Global Leaderboard", "1.0")]
public class BasePlugin : BaseUnityPlugin
{
    public void Awake()
    {
        PlayFabSettings.staticSettings.TitleId = Config.Bind<string>(new BepInEx.Configuration.ConfigDefinition("", "TitleId"), "1DDBDA").Value;
        new Harmony("baldifull.global.leaderboard").PatchAll();
        //PlayFabSettings.staticSettings.DeveloperSecretKey = Config.Bind<string>(new BepInEx.Configuration.ConfigDefinition("", "DeveloperSecretKey"), "4PJQZ8YKUTUKTEQD5WKYB35YWPZ7TSCCNR9K1AZOTQ6J6EX4XU").Value;
    }
}

[HarmonyPatch]
public static class Patch
{
    [HarmonyPatch(typeof(PlayerFileManager), "Load"), HarmonyPostfix]
    public static void Postfix(PlayerFileManager __instance)
    {
        __instance.StartCoroutine(Logging());
    }
    public static IEnumerator Logging()
    {
        Stopwatch sw = Stopwatch.StartNew();
        Debug.Log($"{PlayerFileManager.Instance.fileName} is logging!");
        yield return PlayFabClientAPI.LoginWithCustomIDAsync(new LoginWithCustomIDRequest() { CustomId = PlayerFileManager.Instance.fileName, CreateAccount = true, });
        yield return PlayFabClientAPI.UpdateUserTitleDisplayNameAsync(new UpdateUserTitleDisplayNameRequest() { DisplayName = PlayerFileManager.Instance.fileName });
        Debug.Log($"Logging:{sw.ElapsedMilliseconds}");
    }
    [HarmonyPatch(typeof(MinigameHighScoreTable), "UpdateScores"), HarmonyPostfix]
    public static void Postfix(MinigameHighScoreTable __instance, MinigameName minigame, string nameKey)
    {
        if (!__instance.GetComponent<MgmScoreUpdater>())
        {
            MgmScoreUpdater mgmScoreUpdater = __instance.gameObject.AddComponent<MgmScoreUpdater>();
            mgmScoreUpdater.scoreTable = __instance;
            mgmScoreUpdater.minigame = minigame;
        }
        __instance.GetComponent<MgmScoreUpdater>().AddScore();
    }
    [HarmonyPatch(typeof(EndlessMapOverview), "LoadScores"), HarmonyPostfix]
    public static void Postfix(EndlessMapOverview __instance)
    {
        if (!__instance.GetComponent<EndlessScoreUpdater>())
        {
            EndlessScoreUpdater updater = __instance.gameObject.AddComponent<EndlessScoreUpdater>();
            updater.SetOverview(__instance);
        }
        __instance.GetComponent<EndlessScoreUpdater>().AddScore();
    }
}

public class MgmScoreUpdater : ScoreUpdater
{
    protected override string nameKey => minigame.ToString();
    public MinigameName minigame;
    public MinigameHighScoreTable scoreTable;
    public override void UpdateScore()
    {
        if (previous == null)
        {
            Debug.Log($"No leaderboard_{nameKey} found!");
            return;
        }
        Traverse traverse = Traverse.Create(scoreTable);
        TMP_Text[] nameTmp = traverse.Field("nameTmp").GetValue<TMP_Text[]>();
        TMP_Text[] valueTmp = traverse.Field("valueTmp").GetValue<TMP_Text[]>();
        for (int i = 0; i < Mathf.Min(nameTmp.Length, previous.Result.Leaderboard.Count); i++)
        {
            nameTmp[i].text = previous.Result.Leaderboard[i].DisplayName;
            valueTmp[i].text = previous.Result.Leaderboard[i].StatValue.ToString();
        }
        if (previous.Result.Leaderboard.Count < nameTmp.Length)
        {
            for (int i = previous.Result.Leaderboard.Count; i < nameTmp.Length; i++)
            {
                nameTmp[i].text = Singleton<LocalizationManager>.Instance.GetLocalizedText("Name_Baldi");
                valueTmp[i].text = i.ToString();
            }
        }
    }
    public override void AddScore()
    {
        StatisticUpdate update = new StatisticUpdate() { StatisticName = nameKey, Value = 0 };
        for (int i = 0; i < Singleton<HighScoreManager>.Instance.tripNames.GetLength(1); i++)
        {
            if (Singleton<HighScoreManager>.Instance.tripNames[(int)minigame, i] == PlayerFileManager.Instance.fileName)
            {
                update.Value = Mathf.Max(update.Value, Singleton<HighScoreManager>.Instance.tripScores[(int)minigame, i]);
            }
        }
        PlayerFileManager.Instance.StartCoroutine(UploadScore(update));
        UpdateScore();
    }
}

public class EndlessScoreUpdater : ScoreUpdater
{
    protected override string nameKey => field.GetValue<string>();
    EndlessMapOverview overview;
    Traverse traverse, field;
    public void SetOverview(EndlessMapOverview overview)
    {
        this.overview = overview;
        traverse = Traverse.Create(overview);
        field = traverse.Field("levelId");
    }
    public override void UpdateScore()
    {
        if (previous == null)
        {
            Debug.Log($"No leaderboard_{nameKey} found!");
            return;
        }
        for (int i = 0; i < Mathf.Min(overview.listings.Length, previous.Result.Leaderboard.Count); i++)
        {
            overview.listings[i].name.text = previous.Result.Leaderboard[i].DisplayName;
            overview.listings[i].score.text = previous.Result.Leaderboard[i].StatValue.ToString();
        }
        if (previous.Result.Leaderboard.Count < overview.listings.Length)
        {
            for (int i = previous.Result.Leaderboard.Count; i < overview.listings.Length; i++)
            {
                overview.listings[i].name.text = Singleton<LocalizationManager>.Instance.GetLocalizedText("Name_Baldi");
                overview.listings[i].score.text = i.ToString();
            }
        }
    }
    public override void AddScore()
    {
        StatisticUpdate update = new StatisticUpdate() { StatisticName = nameKey, Value = 0 };
        foreach (var item in Singleton<HighScoreManager>.Instance.GetSortedScoresForLevel(nameKey))
        {
            if (item.name == PlayerFileManager.Instance.fileName)
            {
                update.Value = Mathf.Max(update.Value, item.score);
            }
        }
        PlayerFileManager.Instance.StartCoroutine(UploadScore(update));
        UpdateScore();
    }
}

public class ScoreUpdater : MonoBehaviour
{
    protected virtual string nameKey => "Rank";
    protected PlayFabResult<GetLeaderboardResult> previous; PlayerLeaderboardEntry a, b; WaitUntil wait;
    public IEnumerator Start()
    {
        while (gameObject)
        {
            bool flag = false;
            var task = PlayFabClientAPI.GetLeaderboardAsync(new GetLeaderboardRequest() { StatisticName = nameKey, MaxResultsCount = 10 });
            wait = new WaitUntil(() => { return task.IsCompleted; });
            yield return wait;
            if (task.IsCompletedSuccessfully)
            {
                if (previous != null || (previous != null && previous.Result.Leaderboard.Count == task.Result.Result.Leaderboard.Count))
                {
                    for (int i = 0; i < task.Result.Result.Leaderboard.Count; i++)
                    {
                        a = task.Result.Result.Leaderboard[i];
                        b = previous.Result.Leaderboard[i];
                        if (a.DisplayName != b.DisplayName || a.PlayFabId != b.PlayFabId)
                        {
                            UpdateScore();
                            previous = task.Result;
                        }
                    }
                }
                else
                {
                    UpdateScore();
                    previous = task.Result;
                }
            }
            if (task.Result.Error != null)
            {
                Debug.Log(task.Result.Error.ErrorMessage);
                yield return new WaitForSeconds(60f);
            }
        }
    }
    public virtual void AddScore()
    {
        UpdateScore();
    }
    public virtual void UpdateScore()
    {

    }
    public IEnumerator UploadScore(StatisticUpdate update)
    {
        Stopwatch sw = Stopwatch.StartNew();
        yield return PlayFabClientAPI.UpdatePlayerStatisticsAsync(new UpdatePlayerStatisticsRequest() { Statistics = new List<StatisticUpdate>() { update } });
        Debug.Log($"UploadScore:{sw.ElapsedMilliseconds}");
    }
}