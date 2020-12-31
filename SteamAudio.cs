using HarmonyLib;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class SteamAudio
    {
        public static void ResetAudio(LocoAudioSteam __instance)
        {
            AudioUtils.SetClip(
                "SH282 whistle",
                __instance.whistleAudio,
                Main.settings.steamWhistleSound,
                enabled: true,
                Main.settings.steamWhistlePitch);
        }

        public static void ResetAllAudio()
        {
            foreach (var audio in Component.FindObjectsOfType<LocoAudioSteam>())
                ResetAudio(audio);
        }

        [HarmonyPatch(typeof(LocoAudioSteam), nameof(LocoAudioSteam.SetupLocoLogic))]
        public static class SetupForCarPatch
        {
            public static void Postfix(LocoAudioSteam __instance)
            {
                ResetAudio(__instance);
            }
        }
    }
}