using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LobbyScpSelector.ScpSelection;
using LobbyScpSelector.Utils;
using PlayerRoles.RoleAssign;

namespace LobbyScpSelector.Patches
{
    /// <summary>
    /// 独立 Harmony 补丁，负责开局队列、SCP 溢出名额及已自选玩家优先抽选。
    /// </summary>
    public static class LobbyHarmonyPatches
    {
        private const string PatchId = "lobby.scpselector.labapi";
        private static Harmony _harmony;

        internal static bool ResolveAllowScpOverflow(bool originalValue)
        {
            return LobbyScpSelectorPlugin.Instance?.Config?.AllowScpOverflow == true || originalValue;
        }

        [HarmonyPatch(typeof(RoleAssigner), "OnRoundStarted")]
        public static class ScpOverflowPatch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool readingOverflow = false;
                bool patched = false;

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Ldstr &&
                        instruction.operand as string == "allow_scp_overflow")
                        readingOverflow = true;

                    yield return instruction;
                    if (readingOverflow && instruction.operand is MethodInfo method &&
                        method.Name == "GetBool" && method.ReturnType == typeof(bool) &&
                        (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt))
                    {
                        yield return new CodeInstruction(
                            OpCodes.Call,
                            AccessTools.Method(typeof(LobbyHarmonyPatches), nameof(ResolveAllowScpOverflow)));
                        readingOverflow = false;
                        patched = true;
                    }
                }

                if (!patched)
                    throw new InvalidOperationException("未找到开局 allow_scp_overflow 读取，无法注入 SCP 溢出分配逻辑");
            }
        }

        [HarmonyPatch(typeof(RoleAssigner), "OnRoundStarted")]
        public static class StartQueuePatch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool readingQueue = false;
                bool patched = false;

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Ldstr &&
                        instruction.operand as string == "team_respawn_queue")
                        readingQueue = true;

                    yield return instruction;
                    if (readingQueue && instruction.operand is MethodInfo method &&
                        method.Name == "GetString" && method.ReturnType == typeof(string) &&
                        (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt))
                    {
                        yield return new CodeInstruction(
                            OpCodes.Call,
                            AccessTools.Method(typeof(ScpSelectionManager), nameof(ScpSelectionManager.PrepareStartQueue)));
                        readingQueue = false;
                        patched = true;
                    }
                }

                if (!patched)
                    throw new InvalidOperationException("未找到 team_respawn_queue 读取，无法注入开局队列与大厅准备逻辑");
            }
        }

        [HarmonyPatch(typeof(ScpPlayerPicker), "GenerateList")]
        public static class SelectedPlayerPriorityPatch
        {
            public static void Postfix(List<ReferenceHub> ___ScpsToSpawn)
            {
                List<ReferenceHub> selected = ScpSelectionManager.PrioritizeSelectedPlayers(___ScpsToSpawn);
                if (ReferenceEquals(selected, ___ScpsToSpawn))
                    return;

                ___ScpsToSpawn.Clear();
                ___ScpsToSpawn.AddRange(selected);
            }
        }

        public static void OnEnable()
        {
            if (_harmony != null)
                return;

            _harmony = new Harmony(PatchId);
            try
            {
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                PluginLog.Info("Harmony 补丁已应用。");
            }
            catch
            {
                OnDisable();
                throw;
            }
        }

        public static void OnDisable()
        {
            _harmony?.UnpatchAll(PatchId);
            _harmony = null;
        }
    }
}
