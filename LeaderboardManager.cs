using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using BepInEx;
using HarmonyLib;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

[BepInPlugin("baldifull.global.leaderboard", "Baldi Full Plugin Global Leaderboard", "1.1")]
public class LeaderboardManager : BaseUnityPlugin
{
    public static LeaderboardManager Instance => instance;
    private static LeaderboardManager instance;
    WaitForSecondsRealtime waitGeneral = new WaitForSecondsRealtime(60f);
    static bool log;
    public static ScoreUpdater scoreUpdater;
    UpdatePlayerStatisticsRequest requestSetter = new UpdatePlayerStatisticsRequest() { Statistics = new List<StatisticUpdate>() };
    public static string playfabId { get; private set; }

    #region "Basics"
    public void Awake()
    {
        name = "";
        instance = this;
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Baldifull TitleId")))
        {
            Environment.SetEnvironmentVariable("Baldifull TitleId", "1DDBDA");
        }
        PlayFabSettings.staticSettings.TitleId = Environment.GetEnvironmentVariable("Baldifull TitleId");

        log = Config.Bind<bool>(new BepInEx.Configuration.ConfigDefinition("", "Debug Log"), false).Value;
        new Harmony("baldifull.global.leaderboard").PatchAll();

        StartCoroutine(Getter());
    }

    IEnumerator Getter()
    {
        Stopwatch sw;
        while (true)
        {
            while (!PlayFabClientAPI.IsClientLoggedIn() || requestSetter.Statistics.Count == 0)
            {
                yield return null;
            }
            PrintBase($"Name:{name} uploading all scores to servers");
            foreach (var stat in requestSetter.Statistics)
            {
                PrintBase($"stat:{stat.StatisticName}:{stat.Value}");
            }
            sw = Stopwatch.StartNew();
            using (var task = PlayFabClientAPI.UpdatePlayerStatisticsAsync(requestSetter))
            {
                disposables.Add(task);
                while (!task.IsCompleted && !task.IsCompletedSuccessfully)
                {
                    yield return task;
                }
                disposables.Remove(task);
                if (task.Result.Error != null)
                {
                    PrintBase($"UpdatePlayerStatisticsAsync:{task.Result.Error.Error.ToString()}");
                }
            }
            PrintBase($"Name:{name} uploading successfully Used Time:{sw.ElapsedMilliseconds}");
            requestSetter.Statistics.Clear();
            scoreUpdater?.LoadScore();
            yield return waitGeneral;
        }
    }

    public void Logging(string displayName)
    {
        if (name != displayName)
        {
            name = displayName;

            PrintBase($"Name:{name} is logging");

            //PU:Previous User
            //stop auto setter to set new one to PU
            requestSetter.Statistics.Clear();
            //stop PU logging
            if (logger != null)
            {
                StopCoroutine(logger);
            }

            foreach (var item in disposables)
            {
                item?.Dispose();
            }

            //set up a new logger
            logger = LoggerBase();
            StartCoroutine(logger);
        }
    }
    IEnumerator logger, uploader;
    List<IDisposable> disposables = new List<IDisposable>();
    public static void PrintBase(string logNew)
    {
        if (log)
        {
            UnityEngine.Debug.Log($"GLM:{logNew}");
        }
    }
    #endregion

    #region"Logging"
    IEnumerator LoggerBase()
    {
        Stopwatch sw = Stopwatch.StartNew();

        //login to server
        using (var task = PlayFabClientAPI.LoginWithCustomIDAsync(new LoginWithCustomIDRequest() { CustomId = name, CreateAccount = true }))
        {
            disposables.Add(task);
            while (!task.IsCompleted && !task.IsCompletedSuccessfully)
            {
                yield return task;
            }
            disposables.Remove(task);
            playfabId = task.Result.Result.PlayFabId;
            if (task.Result.Error != null)
            {
                PrintBase($"LoginWithCustomIDAsync:{task.Result.Error.Error.ToString()}");
            }
        }

        //set display name
        using (var task = PlayFabClientAPI.UpdateUserTitleDisplayNameAsync(new UpdateUserTitleDisplayNameRequest() { DisplayName = name.Length < 3 ? $"{name}_{playfabId}" : name }))
        {
            disposables.Add(task);
            while (!task.IsCompleted && !task.IsCompletedSuccessfully)
            {
                yield return task;
            }
            disposables.Remove(task);
            if (task.Result.Error != null)
            {
                PrintBase($"UpdateUserTitleDisplayNameAsync:{task.Result.Error.Error.ToString()}");
            }
        }

        UploadScore(HighScoreManager.Instance);
        PrintBase($"Name:{name} logged successfully! Used Time:{sw.ElapsedMilliseconds} and I'm tring to UPLOADING scores this account!");
    }
    #endregion

    #region "Upload"
    public void UploadScore(HighScoreManager highScoreManager)
    {
        if (uploader != null)
        {
            StopCoroutine(uploader);
        }
        uploader = UploadScoreBase(highScoreManager);
        StartCoroutine(uploader);
    }

    IEnumerator UploadScoreBase(HighScoreManager highScoreManager)
    {
        while (!PlayFabClientAPI.IsClientLoggedIn())
        {
            yield return null;
        }

        MinigameName minigame;
        int a = highScoreManager.tripNames.GetLength(0), b = highScoreManager.tripNames.GetLength(1), c;
        for (int i = 0; i < a; i++)
        {
            c = 0;
            try
            {
                minigame = (MinigameName)i;
                for (int j = 0; j < b; j++)
                {
                    if (highScoreManager.tripNames[i, j] == name)
                    {
                        c = Mathf.Max(c, highScoreManager.tripScores[i, j]);
                    }
                }
                if (c > 0)
                {
                    requestSetter.Statistics.Add(new StatisticUpdate() { StatisticName = minigame.ToString(), Value = c });
                }
            }
            catch
            {
                PrintBase($"Invaild Minigame_{i}");
            }
        }

        List<string> ids = new List<string>();
        foreach (var item in highScoreManager.endlessScores)
        {
            if (!string.IsNullOrEmpty(item.levelId) && !ids.Contains(item.levelId))
            {
                ids.Add(item.levelId);
            }
        }

        foreach (var item in ids)
        {
            c = 0;
            foreach (var item1 in highScoreManager.GetSortedScoresForLevel(item))
            {
                if (item1.name == name)
                {
                    c = Mathf.Max(c, item1.score);
                }
            }
            if (c > 0)
            {
                requestSetter.Statistics.Add(new StatisticUpdate() { StatisticName = item, Value = c });
            }
        }
    }
    #endregion
}