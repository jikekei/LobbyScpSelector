using System;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using LobbyScpSelector.Config;
using LobbyScpSelector.Lobby;
using LobbyScpSelector.ScpSelection;
using LobbyScpSelector.Utils;
using UnityEngine;

namespace LobbyScpSelector.Commands
{
    /// <summary>
    /// 开始大厅出生点配置与校验指令，设置完成后自动写入配置文件。
    /// </summary>
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public sealed class LobbySpawnCommand : ICommand
    {
        public string Command => "lobbyspawn";
        public string[] Aliases => new[] { "setlobbyspawn", "lspawn" };
        public string Description => "查看或配置开始大厅出生点坐标并自动保存至配置文件。用法: lobbyspawn <get|set [x y z]|tp>";

        private const string UsageText =
            "用法:\n" +
            "  lobbyspawn [get]         - 查看当前配置的大厅出生点与配置文件路径\n" +
            "  lobbyspawn set [x y z]   - 将出生点设为当前玩家坐标或指定坐标，并自动写入配置文件\n" +
            "  lobbyspawn tp            - 将自己传送至当前配置的大厅出生点以验证位置";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.HasPermission("lobby.spawn"))
            {
                response = "缺少权限: lobby.spawn";
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

            if (arguments.Count == 0 || arguments.At(0).Equals("get", StringComparison.OrdinalIgnoreCase))
            {
                if (arguments.Count > 1)
                {
                    response = "用法: lobbyspawn [get]";
                    return false;
                }

                response =
                    $"当前大厅出生点: {CoordinateHelper.Format(config.LobbySpawnPosition)}\n" +
                    $"配置文件路径: {plugin.ConfigPath}";
                return true;
            }

            string subcommand = arguments.At(0).ToLowerInvariant();
            switch (subcommand)
            {
                case "set":
                    return HandleSet(arguments, sender, plugin, config, out response);
                case "tp":
                    return HandleTeleport(arguments, sender, config, out response);
                default:
                    response = UsageText;
                    return false;
            }
        }

        private static bool HandleSet(
            ArraySegment<string> arguments,
            ICommandSender sender,
            LobbyScpSelectorPlugin plugin,
            PluginConfig config,
            out string response)
        {
            if (arguments.Count != 1 && arguments.Count != 4)
            {
                response = "用法: lobbyspawn set [x y z]";
                return false;
            }

            Vector3 newSpawn;
            string sourceLabel;
            if (arguments.Count == 1)
            {
                Player player = Player.Get(sender);
                if (player == null)
                {
                    response = "从控制台执行且未提供坐标时无法获取玩家位置，请使用: lobbyspawn set <x> <y> <z>";
                    return false;
                }

                newSpawn = player.Position;
                sourceLabel = "当前玩家坐标";
            }
            else
            {
                if (!CoordinateHelper.TryParseVector3(arguments.At(1), arguments.At(2), arguments.At(3), out newSpawn))
                {
                    response = "坐标必须为有限数字（小数使用英文句点），例如: lobbyspawn set 142.659 295.230 -133.689";
                    return false;
                }

                sourceLabel = "指定坐标";
            }

            if (!CoordinateHelper.IsFinite(newSpawn))
            {
                response = "出生点坐标包含无效数值。";
                return false;
            }

            Vector3 previousSpawn = config.LobbySpawnPosition;
            config.LobbySpawnPosition = newSpawn;

            if (!plugin.TrySaveConfig(out string configPath, out string saveError))
            {
                config.LobbySpawnPosition = previousSpawn;
                response = $"大厅出生点设置失败，配置文件写入失败: {saveError}";
                return false;
            }

            if (LobbyManager.IsLobby && config.CoinLabel != null && config.CoinLabel.Enabled && config.CoinLabel.FaceLobbySpawn)
                ScpSelectionManager.RefreshActiveLabels();

            response =
                $"已更新大厅出生点（来源: {sourceLabel}）: {CoordinateHelper.Format(newSpawn)}\n" +
                $"配置文件已自动保存: {configPath}";
            return true;
        }

        private static bool HandleTeleport(
            ArraySegment<string> arguments,
            ICommandSender sender,
            PluginConfig config,
            out string response)
        {
            if (arguments.Count != 1)
            {
                response = "用法: lobbyspawn tp";
                return false;
            }

            Player player = Player.Get(sender);
            if (player == null)
            {
                response = "请由游戏内玩家执行 lobbyspawn tp。";
                return false;
            }

            if (!CoordinateHelper.IsFinite(config.LobbySpawnPosition))
            {
                response = "当前配置的大厅出生点坐标无效，请先使用 lobbyspawn set 重新设置。";
                return false;
            }

            if (!player.IsAlive)
            {
                response = "当前角色处于非存活状态，请切换为存活角色后再执行传送验证。";
                return false;
            }

            player.Position = config.LobbySpawnPosition;
            response = $"已将您传送至大厅出生点: {CoordinateHelper.Format(config.LobbySpawnPosition)}";
            return true;
        }
    }
}
