using HarmonyLib;
using BepInEx;
using UnityEngine;

[BepInPlugin("baldifull.template", "Baldi Full Template", "1.0")]
public class BasePlugin : BaseUnityPlugin
{
    public void Awake()
    {
        new Harmony("baldifull.template").PatchAll();
        Debug.Log("Awaking!");
    }
}
[HarmonyPatch]
public static class Patch
{
    [HarmonyPatch(typeof(NameManager), "Awake"), HarmonyPostfix]
    public static void Postfix()
    {
        Debug.Log("Load your assets or modify base game here!");
    }
}