using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class EngineFade
    {
        public class Settings
        {
            public float fadeInStart;
            public float fadeOutStart;
            public float fadeInDuration = 2f;
            public float fadeOutDuration = 1f;
        }

        private static readonly Dictionary<LocoTrainAudio, Settings> settings = new Dictionary<LocoTrainAudio, Settings>();

        private static Settings GetDefaultSettings(LocoTrainAudio audio)
        {
            if (audio is LocoAudioShunter audioShunter)
            {
                return new Settings
                {
                    fadeInStart = audioShunter.engineOnClip.length * 0.15f,
                    fadeOutStart = audioShunter.engineOffClip.length * 0.10f,
                };
            }
            else if (audio is LocoAudioDiesel audioDiesel)
            {
                return new Settings
                {
                    fadeInStart = audioDiesel.engineOnClip.length * 0.15f,
                    fadeOutStart = audioDiesel.engineOffClip.length * 0.10f,
                };
            }
            else
            {
                throw new System.Exception($"{audio.GetType().Name} received by EngineFade");
            }
        }

        private static Settings GetSettings(LocoTrainAudio audio)
        {
            return settings.ContainsKey(audio) ? settings[audio] : GetDefaultSettings(audio);
        }

        public static float GetFadeInStart(LocoTrainAudio audio) => GetSettings(audio).fadeInStart;
        public static float GetFadeOutStart(LocoTrainAudio audio) => GetSettings(audio).fadeOutStart;
        public static float GetFadeInDuration(LocoTrainAudio audio) => GetSettings(audio).fadeInDuration;
        public static float GetFadeOutDuration(LocoTrainAudio audio) => GetSettings(audio).fadeOutDuration;

        public static void SetFadeSettings(LocoTrainAudio audio, Settings fadeSettings)
        {
            settings[audio] = fadeSettings;
        }

        [HarmonyPatch]
        public static class EngineAudioHandlePatch
        {
            public static IEnumerable<CodeInstruction> GenerateNewMethod(MethodBase original, List<CodeInstruction> instructions)
            {
                var index = instructions.FindIndex(ci => ci.Is(OpCodes.Ldfld, AccessTools.Field(original.DeclaringType, "engineTurnedOn")));
                index = instructions.FindIndex(index + 1, ci => ci.Is(OpCodes.Ldfld, AccessTools.Field(original.DeclaringType, "engineTurnedOn")));

                index += 3;
                instructions.RemoveRange(index, 4);
                instructions.Insert(index, CodeInstruction.Call(typeof(EngineFade), nameof(EngineFade.GetFadeOutStart)));

                index += 3;
                instructions.RemoveRange(index, 4);
                instructions.Insert(index, CodeInstruction.Call(typeof(EngineFade), nameof(EngineFade.GetFadeInStart)));

                index = instructions.FindIndex(index, ci => ci.Is(OpCodes.Ldfld, AccessTools.Field(original.DeclaringType, "engineTurnedOn")));

                index += 2;
                instructions[index].opcode = OpCodes.Ldloc_1;
                instructions[index].operand = null;
                instructions.Insert(index + 1,  CodeInstruction.Call(typeof(EngineFade), nameof(EngineFade.GetFadeOutDuration)));

                index += 3;
                instructions[index].opcode = OpCodes.Ldloc_1;
                instructions[index].operand = null;
                instructions.Insert(index + 1, CodeInstruction.Call(typeof(EngineFade), nameof(EngineFade.GetFadeInDuration)));

                return instructions;
            }

            public static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions)
            {
                var before = new List<CodeInstruction>(instructions);
                var after = GenerateNewMethod(original, before).ToList();
                // Main.DebugLog(() => $"\nBefore:\n{string.Join("\n", before)}\n\nAfter:\n{string.Join("\n", after)}");
                return after;
            }

            public static IEnumerable<MethodBase> TargetMethods()
            {
                return typeof(LocoAudioDiesel).Assembly.GetTypes()
                    .Where(t => t.FullName.Contains("+<EngineAudioHandle>"))
                    .Select(t => t.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic));
            }
        }
    }
}