using System;
using LabApi.Features.Wrappers;
using LobbyScpSelector.Config;
using LobbyScpSelector.Utils;
using PlayerRoles;
using UnityEngine;

namespace LobbyScpSelector.ScpSelection
{
    /// <summary>
    /// 负责在 SCP 自选硬币（正式硬币与测试硬币）上方自动创建、更新旋转与安全销毁 TextToy 文字标签。
    /// </summary>
    internal static class CoinTextLabelHelper
    {
        private static readonly Vector3 DefaultOffset = new Vector3(0f, 1.2f, 0f);
        private static readonly Vector3 DefaultScale = new Vector3(0.5f, 0.5f, 0.5f);
        private static readonly Vector2 DefaultDisplaySize = new Vector2(1000f, 1000f);

        internal static TextToy CreateLabel(RoleTypeId role, Vector3 coinPosition)
        {
            PluginConfig config = LobbyScpSelectorPlugin.Instance?.Config;
            CoinLabelConfig labelConfig = config?.CoinLabel;
            if (labelConfig == null || !labelConfig.Enabled || !CoordinateHelper.IsFinite(coinPosition))
                return null;

            Vector3 offset = CoordinateHelper.IsFinite(labelConfig.Offset) ? labelConfig.Offset : DefaultOffset;
            Vector3 scale = IsPositiveFinite(labelConfig.Scale) ? labelConfig.Scale : DefaultScale;
            Vector2 displaySize = IsPositiveFinite(labelConfig.DisplaySize) ? labelConfig.DisplaySize : DefaultDisplaySize;
            Vector3 labelPosition = coinPosition + offset;
            Quaternion rotation = ComputeRotation(role, labelPosition, config, labelConfig);

            string translationFormat = LobbyScpSelectorPlugin.Instance?.Translations?.CoinLabelTextFormat;
            string template = !string.IsNullOrEmpty(labelConfig.TextFormat)
                ? labelConfig.TextFormat
                : (!string.IsNullOrEmpty(translationFormat) ? translationFormat : "-SCP SELECT-\r\n{scp}");
            string text = template
                .Replace("{scp}", ScpRoleParser.FormatDisplayName(role))
                .Replace("{role}", role.ToString());

            TextToy toy = null;
            try
            {
                toy = TextToy.Create(labelPosition, rotation, scale, null, true);
                if (toy == null)
                {
                    PluginLog.Warn($"为 {role} 硬币创建 TextToy 失败：TextToy.Create 返回 null。");
                    return null;
                }

                toy.TextFormat = text;
                toy.DisplaySize = displaySize;
                return toy;
            }
            catch (Exception ex)
            {
                DestroySafely(toy);
                PluginLog.Error($"为 {role} 硬币创建上方文字时出错: {ex}");
                return null;
            }
        }

        internal static Vector3 GetTextToyEulerMatchingPlayerFacing(Player player)
        {
            if (player == null || player.IsDestroyed)
                return Vector3.zero;

            float playerYaw = player.Camera != null
                ? player.Camera.eulerAngles.y
                : player.Rotation.eulerAngles.y;

            if (!CoordinateHelper.IsFinite(playerYaw))
                return Vector3.zero;

            // SCP:SL 中 TextToy 可读正面朝向局部 -Z 轴，叠加 180° 使文字正面朝向与玩家水平朝向一致。
            float textYaw = ((playerYaw + 180f) % 360f + 360f) % 360f;
            return new Vector3(0f, textYaw, 0f);
        }

        internal static Quaternion ComputeRotation(
            RoleTypeId role,
            Vector3 labelPosition,
            PluginConfig config,
            CoinLabelConfig labelConfig)
        {
            if (config.ScpSelectionCoinLabelRotations != null &&
                config.ScpSelectionCoinLabelRotations.TryGetValue(role, out Vector3 customEuler) &&
                CoordinateHelper.IsFinite(customEuler))
            {
                return Quaternion.Euler(customEuler);
            }

            Vector3 defaultEuler = CoordinateHelper.IsFinite(labelConfig.DefaultRotation)
                ? labelConfig.DefaultRotation
                : Vector3.zero;

            if (labelConfig.FaceLobbySpawn && CoordinateHelper.IsFinite(config.LobbySpawnPosition))
            {
                // SCP:SL 中 TextToy 可读正面朝向局部 -Z 轴，因此局部 +Z 应指向背离出生点的方向。
                Vector3 away = new Vector3(
                    labelPosition.x - config.LobbySpawnPosition.x,
                    0f,
                    labelPosition.z - config.LobbySpawnPosition.z);

                if (away.sqrMagnitude > 0.0001f)
                    return Quaternion.LookRotation(away);
            }

            return Quaternion.Euler(defaultEuler);
        }

        internal static bool DestroySafely(TextToy toy)
        {
            if (toy == null || toy.IsDestroyed)
                return false;

            try
            {
                toy.Destroy();
                return true;
            }
            catch (Exception ex)
            {
                PluginLog.Error($"销毁硬币上方 TextToy 时出错: {ex}");
                return false;
            }
        }

        private static bool IsPositiveFinite(Vector3 value) =>
            CoordinateHelper.IsFinite(value) && value.x > 0f && value.y > 0f && value.z > 0f;

        private static bool IsPositiveFinite(Vector2 value) =>
            CoordinateHelper.IsFinite(value.x) && CoordinateHelper.IsFinite(value.y) &&
            value.x > 0f && value.y > 0f;
    }
}
