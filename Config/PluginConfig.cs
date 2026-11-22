using System.Collections.Generic;
using System.ComponentModel;
using PlayerRoles;
using UnityEngine;

namespace LobbyScpSelector.Config
{
    /// <summary>
    /// 开始大厅与 SCP 自选插件配置。
    /// </summary>
    public sealed class PluginConfig
    {
        [Description("是否启用开始大厅与 SCP 自选插件。")]
        public bool IsEnabled { get; set; } = true;

        [Description("是否输出调试日志。")]
        public bool Debug { get; set; } = false;

        [Description("大厅出生点世界坐标；可通过 lobbyspawn set 指令自动更新并保存。")]
        public Vector3 LobbySpawnPosition { get; set; } = new Vector3(-0.09920526f, 295.3805f, -8.435011f);

        [Description("SCP 自选硬币缩放倍率（必须大于 0）。")]
        public float CoinScale { get; set; } = 17f;

        [Description("允许自选及分配 SCP-079 所需的最低在线人数。")]
        public int MinPlayersForScp079 { get; set; } = 7;

        [Description("是否启用开局 SCP 溢出名额分配。")]
        public bool AllowScpOverflow { get; set; } = true;

        [Description("是否使用默认比例重排的开局阵营队列并优先放置 SCP 槽位。")]
        public bool UseCustomStartQueue { get; set; } = true;

        [Description("SCP 自选硬币位置；可通过 scpcoin <scp> 指令设置并自动覆盖保存，删除角色条目可禁用该硬币。")]
        public Dictionary<RoleTypeId, Vector3> ScpSelectionCoinPositions { get; set; } = new Dictionary<RoleTypeId, Vector3>
        {
            { RoleTypeId.Scp173, new Vector3(-2.656249f, 294.4705f, -12.48828f) },
            { RoleTypeId.Scp049, new Vector3(2.300781f, 294.7761f, -12.60156f) },
            { RoleTypeId.Scp096, new Vector3(8.140626f, 294.7f, -12.27734f) },
            { RoleTypeId.Scp079, new Vector3(3.214844f, 294.4705f, -3.140625f) },
            { RoleTypeId.Scp3114, new Vector3(-2.425783f, 294.4705f, -3.070313f) },
            { RoleTypeId.Scp106, new Vector3(-7.792969f, 294.4705f, -3.53125f) },
            { RoleTypeId.Scp939, new Vector3(7.660157f, 295.0731f, -3.160156f) },
        };

        [Description("SCP 自选硬币上方文字的自定义欧拉角旋转（度）；使用 scpcoin 指令放置时自动记录为与玩家朝向一致，未配置的角色使用 CoinLabel 默认策略。")]
        public Dictionary<RoleTypeId, Vector3> ScpSelectionCoinLabelRotations { get; set; } = new Dictionary<RoleTypeId, Vector3>
        {
            { RoleTypeId.Scp106, new Vector3(0f, 2.469223f, 0f) },
            { RoleTypeId.Scp3114, new Vector3(0f, 0.2279663f, 0f) },
            { RoleTypeId.Scp079, new Vector3(0f, 6.880295f, 0f) },
            { RoleTypeId.Scp939, new Vector3(0f, 7.347214f, 0f) },
            { RoleTypeId.Scp096, new Vector3(0f, 180.401f, 0f) },
            { RoleTypeId.Scp049, new Vector3(0f, 180.2856f, 0f) },
            { RoleTypeId.Scp173, new Vector3(0f, 182.7411f, 0f) },
        };

        [Description("硬币上方自动生成的 TextToy 文字标签配置。")]
        public CoinLabelConfig CoinLabel { get; set; } = new CoinLabelConfig();

        /// <summary>
        /// 防止配置反序列化后字典与嵌套配置对象为 null。
        /// </summary>
        public void EnsureDefaults()
        {
            ScpSelectionCoinPositions ??= new Dictionary<RoleTypeId, Vector3>();
            ScpSelectionCoinLabelRotations ??= new Dictionary<RoleTypeId, Vector3>();
            CoinLabel ??= new CoinLabelConfig();
        }
    }

    /// <summary>
    /// 硬币上方自动生成的 TextToy 文字配置。
    /// </summary>
    public sealed class CoinLabelConfig
    {
        [Description("是否在生成硬币时自动在硬币上方生成 TextToy 文字标签。")]
        public bool Enabled { get; set; } = true;

        [Description("文字相对硬币坐标的偏移量（默认正上方 1.2 米）。")]
        public Vector3 Offset { get; set; } = new Vector3(0f, 1.2f, 0f);

        [Description("文字缩放（x/y/z），各分量必须大于 0。")]
        public Vector3 Scale { get; set; } = new Vector3(0.5f, 0.5f, 0.5f);

        [Description("文字显示区域（x 为宽度，y 为高度），必须大于 0。")]
        public Vector2 DisplaySize { get; set; } = new Vector2(1000f, 1000f);

        [Description("是否自动将文字水平朝向大厅出生点 LobbySpawnPosition。")]
        public bool FaceLobbySpawn { get; set; } = true;

        [Description("关闭自动朝向或与出生点水平重合时使用的欧拉角旋转（度）。")]
        public Vector3 DefaultRotation { get; set; } = Vector3.zero;

        [Description("硬币上方显示的文字模板，支持富文本与 {scp} 占位符。")]
        public string TextFormat { get; set; } = "-SCP自选-\r\n{scp}";
    }
}
