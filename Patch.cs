using HarmonyLib;

[HarmonyPatch]
public static class Patch
{
    [HarmonyPatch(typeof(PlayerFileManager), "Load"), HarmonyPostfix]
    public static void Postfix(PlayerFileManager __instance)
    {
        LeaderboardManager.Instance.Logging(__instance.fileName);
    }

    [HarmonyPatch(typeof(HighScoreManager), "Load"), HarmonyPostfix]
    public static void Postfix(HighScoreManager __instance)
    {
        LeaderboardManager.PrintBase("HighScoreManager is LOADING data AND I'm tring to UPLOADING them!");
        LeaderboardManager.Instance.UploadScore(__instance);
    }

    [HarmonyPatch(typeof(HighScoreManager), "Save"), HarmonyPostfix]
    public static void Postfix0(HighScoreManager __instance)
    {
        LeaderboardManager.PrintBase("HighScoreManager is SAVING data AND I'm tring to UPLOADING them!");
        LeaderboardManager.Instance.UploadScore(__instance);
    }

    [HarmonyPatch(typeof(EndlessMapOverview), "LoadScores"), HarmonyPostfix]
    public static void Postfix(EndlessMapOverview __instance)
    {
        LeaderboardManager.PrintBase("EndlessMapOverview is LoadScores AND I'm tring to LOADING them!");
        EndlessScoreUpdater updater = __instance.GetComponent<EndlessScoreUpdater>();
        if (!updater)
        {
            updater = __instance.gameObject.AddComponent<EndlessScoreUpdater>();
        }
        updater.Set(__instance);
    }

    [HarmonyPatch(typeof(MinigameHighScoreTable), "UpdateScores"), HarmonyPostfix]
    public static void Postfix(MinigameHighScoreTable __instance, MinigameName minigame, string nameKey)
    {
        LeaderboardManager.PrintBase("MinigameHighScoreTable is LoadScores AND I'm tring to LOADING them!");
        MgmScoreUpdater updater = __instance.GetComponent<MgmScoreUpdater>();
        if (!updater)
        {
            updater = __instance.gameObject.AddComponent<MgmScoreUpdater>();
        }
        updater.Set(minigame, __instance);
    }
}