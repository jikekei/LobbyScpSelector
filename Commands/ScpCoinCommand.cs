using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using LobbyScpSelector.Config;
using LobbyScpSelector.Lobby;
using LobbyScpSelector.ScpSelection;
using LobbyScpSelector.Utils;
using PlayerRoles;
using UnityEngine;

namespace LobbyScpSelector.Commands
{
    /// <summary>
    /// SCP 自选硬币位置管理指令：放置或更新某 SCP 的硬币位置时自动删除旧位置、刷新大厅实体并自动保存配置。
    /// </summary>
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public sealed class ScpCoinCommand : ICommand
    {
        public string Command => "scpcoin";
        public string[] Aliases => new[] { "scpc", "scpfoot" };
        public string Description => "在玩家脚下或指定坐标放置 SCP 自选硬币并自动覆盖保存配置。用法: scpcoin <scp> [x y z] | remove <scp> | list | reload";

        private const string UsageText =
            "用法:\n" +
            "  scpcoin <scp>             - 在玩家脚下地面放置该 SCP 硬币（已有旧位置会自动删除覆盖并保存配置）\n" +
            "  scpcoin <scp> <x> <y> <z> - 在指定世界坐标放置该 SCP 硬币（已有旧位置会自动删除覆盖并保存配置）\n" +
            "  scpcoin remove <scp>      - 删除该 SCP 的硬币配置并移除大厅硬币（自动保存配置）\n" +
            "  scpcoin list              - 列出配置文件路径、当前出生点与全部硬币位置\n" +
            "  scpcoin reload            - 按当前配置重新加载大厅硬币与上方文字\n" +
            "支持的角色: 173, 049, 096, 079, 3114, 106, 939";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.HasPermission("scpselection.config"))
            {
                response = "缺少权限: scpselection.config";
                return false;
            }

            LobbyScpSelectorPlugin plugin = LobbyScpSelectorPlugin.Instance;
            PluginConfig config = plugin?.Config;
            if (plugin == null || config == null)
            {
                response = "LobbyScpSelector 插件尚未启用或配置未加载。";
                return false;
            }

            config.EnsureDefaults();

            if (arguments.Count == 0)
            {
                response = UsageText;
                return false;
            }

            string first = arguments.At(0).ToLowerInvariant();
            if (arguments.Count == 1)
            {
                switch (first)
                {
                    case "list":
                        return HandleList(plugin, config, out response);
                    case "reload":
                        return HandleReload(out response);
                    case "remove":
                        response = "用法: scpcoin remove <scp>";
                        return false;
                }

                if (ScpRoleParser.TryParse(first, out RoleTypeId role))
                    return PlaceAtPlayerFeet(sender, plugin, config, role, out response);

                response = UsageText;
                return false;
            }

            if (arguments.Count == 2 && first == "remove")
                return HandleRemove(arguments.At(1), plugin, config, out response);

            if (arguments.Count == 4 && ScpRoleParser.TryParse(first, out RoleTypeId targetRole))
            {
                if (!CoordinateHelper.TryParseVector3(arguments.At(1), arguments.At(2), arguments.At(3), out Vector3 position))
                {
                    response = "坐标必须为有限数字（小数使用英文句点），例如: scpcoin 173 133.6 295.33 -136.78";
                    return false;
                }

                Player player = Player.Get(sender);
                Vector3? labelEuler = player != null
                    ? CoinTextLabelHelper.GetTextToyEulerMatchingPlayerFacing(player)
                    : (Vector3?)null;

                return PlaceOrMoveCoin(plugin, config, targetRole, position, "指定坐标", labelEuler, out response);
            }

            response = UsageText;
            return false;
        }

        private static bool PlaceAtPlayerFeet(
            ICommandSender sender,
            LobbyScpSelectorPlugin plugin,
            PluginConfig config,
            RoleTypeId role,
            out string response)
        {
            Player player = Player.Get(sender);
            if (player == null)
            {
                response = $"从控制台执行且未提供坐标时无法获取玩家脚下位置，请使用: scpcoin {role} <x> <y> <z>";
                return false;
            }

            Vector3 feetPosition = CoordinateHelper.GetPlayerFeetPosition(player, out bool hitGround);
            string sourceLabel = hitGround ? "玩家脚下地面" : "玩家脚下估算坐标（未检测到下方地面）";
            Vector3 labelEuler = CoinTextLabelHelper.GetTextToyEulerMatchingPlayerFacing(player);
            return PlaceOrMoveCoin(plugin, config, role, feetPosition, sourceLabel, labelEuler, out response);
        }

        private static bool PlaceOrMoveCoin(
            LobbyScpSelectorPlugin plugin,
            PluginConfig config,
            RoleTypeId role,
            Vector3 targetPosition,
            string sourceLabel,
            Vector3? customLabelEuler,
            out string response)
        {
            if (!CoordinateHelper.IsFinite(targetPosition))
            {
                response = "目标坐标包含无效数值。";
                return false;
            }

            bool hadPreviousPos = config.ScpSelectionCoinPositions.TryGetValue(role, out Vector3 previousPosition);
            bool hadPreviousRot = config.ScpSelectionCoinLabelRotations.TryGetValue(role, out Vector3 previousRotation);

            config.ScpSelectionCoinPositions[role] = targetPosition;
            if (customLabelEuler.HasValue)
                config.ScpSelectionCoinLabelRotations[role] = customLabelEuler.Value;
            else
                config.ScpSelectionCoinLabelRotations.Remove(role);

            if (!plugin.TrySaveConfig(out string configPath, out string saveError))
            {
                if (hadPreviousPos)
                    config.ScpSelectionCoinPositions[role] = previousPosition;
                else
                    config.ScpSelectionCoinPositions.Remove(role);

                if (hadPreviousRot)
                    config.ScpSelectionCoinLabelRotations[role] = previousRotation;
                else
                    config.ScpSelectionCoinLabelRotations.Remove(role);

                response = $"硬币位置设置失败，配置文件写入失败: {saveError}";
                return false;
            }

            ScpSelectionManager.PlaceOrReplaceCoin(role, targetPosition, out Pickup pickup, out bool spawnedImmediately);
            string lobbyStatus = spawnedImmediately && pickup != null
                ? $"已在大厅实时生成硬币与上方文字 (Serial={pickup.Serial})"
                : "当前不在大厅阶段，新位置将在下次等待玩家时生效";

            string moveInfo = hadPreviousPos
                ? $"已自动删除旧位置 ({CoordinateHelper.Format(previousPosition)}) 并迁移至新位置"
                : "已新增硬币位置";

            string rotationInfo = customLabelEuler.HasValue
                ? $"文字朝向已设为与玩家一致 (Euler: {CoordinateHelper.Format(customLabelEuler.Value)})"
                : "文字朝向使用配置默认策略（控制台执行未读取玩家视角）";

            response =
                $"{moveInfo} [{role}]（来源: {sourceLabel}）: {CoordinateHelper.Format(targetPosition)}\n" +
                $"{rotationInfo}\n" +
                $"{lobbyStatus}\n" +
                $"配置已自动保存: {configPath}";
            return true;
        }

        private static bool HandleRemove(
            string roleInput,
            LobbyScpSelectorPlugin plugin,
            PluginConfig config,
            out string response)
        {
            if (!ScpRoleParser.TryParse(roleInput, out RoleTypeId role))
            {
                response = $"不支持的 SCP 角色 '{roleInput}'。支持的角色: {ScpRoleParser.SupportedNames}";
                return false;
            }

            if (!config.ScpSelectionCoinPositions.TryGetValue(role, out Vector3 oldPosition))
            {
                response = $"配置中不存在 {role} 的硬币位置。";
                return false;
            }

            bool hadOldRotation = config.ScpSelectionCoinLabelRotations.TryGetValue(role, out Vector3 oldRotation);
            config.ScpSelectionCoinPositions.Remove(role);
            config.ScpSelectionCoinLabelRotations.Remove(role);

            if (!plugin.TrySaveConfig(out string configPath, out string saveError))
            {
                config.ScpSelectionCoinPositions[role] = oldPosition;
                if (hadOldRotation)
                    config.ScpSelectionCoinLabelRotations[role] = oldRotation;

                response = $"删除 {role} 硬币位置失败，配置文件写入失败: {saveError}";
                return false;
            }

            ScpSelectionManager.RemoveCoin(role);
            response =
                $"已删除 {role} 的硬币配置（原坐标: {CoordinateHelper.Format(oldPosition)}）并移除对应实体。\n" +
                $"配置已自动保存: {configPath}";
            return true;
        }

        private static bool HandleList(
            LobbyScpSelectorPlugin plugin,
            PluginConfig config,
            out string response)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"配置文件路径: {plugin.ConfigPath}");
            sb.AppendLine($"大厅出生点 (LobbySpawnPosition): {CoordinateHelper.Format(config.LobbySpawnPosition)}");
            sb.AppendLine("SCP 自选硬币位置 (ScpSelectionCoinPositions):");

            if (config.ScpSelectionCoinPositions.Count == 0)
            {
                sb.AppendLine("  (空)");
            }
            else
            {
                foreach (RoleTypeId role in ScpRoleParser.SelectableRoles)
                {
                    if (!config.ScpSelectionCoinPositions.TryGetValue(role, out Vector3 pos))
                        continue;

                    string rotSuffix = config.ScpSelectionCoinLabelRotations.TryGetValue(role, out Vector3 rot)
                        ? $" (文字欧拉角: {CoordinateHelper.Format(rot)})"
                        : " (文字朝向: 默认策略)";
                    sb.AppendLine($"  - {role}: {CoordinateHelper.Format(pos)}{rotSuffix}");
                }
            }

            response = sb.ToString().TrimEnd();
            return true;
        }

        private static bool HandleReload(out string response)
        {
            if (!LobbyManager.IsLobby)
            {
                response = "当前不在开始大厅阶段，配置已保留，将在下次等待玩家时自动生成硬币。";
                return false;
            }

            int count = ScpSelectionManager.ReloadConfirmedCoins();
            response = $"已按当前配置重新加载大厅硬币与上方文字，共生成 {count} 枚。";
            return true;
        }
    }

    /// <summary>
    /// 获取当前玩家世界坐标的快捷指令。
    /// </summary>
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public sealed class ScpGetPositionCommand : ICommand
    {
        public string Command => "scpgetpos";
        public string[] Aliases => new[] { "scpcoord" };
        public string Description => "获取执行指令的玩家当前世界坐标。用法: scpgetpos";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.HasPermission("scpselection.position"))
            {
                response = "缺少权限: scpselection.position";
                return false;
            }

            if (arguments.Count != 0)
            {
                response = "用法: scpgetpos";
                return false;
            }

            Player player = Player.Get(sender);
            if (player == null)
            {
                response = "请由游戏内玩家在管理控制台执行此指令。";
                return false;
            }

            Vector3 position = player.Position;
            string formatted = CoordinateHelper.Format(position);
            response = string.Format(
                CultureInfo.InvariantCulture,
                "当前世界坐标: {0}\nYAML 坐标:\nx: {1:R}\ny: {2:R}\nz: {3:R}\n快捷指令示例:\n  scpcoin 173\n  scpcoin 173 {0}\n  lobbyspawn set {0}",
                formatted,
                position.x,
                position.y,
                position.z);
            return true;
        }
    }
}
