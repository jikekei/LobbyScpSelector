using System.Collections.Generic;
using System.ComponentModel;
using PlayerRoles;
using UnityEngine;

namespace LobbyScpSelector.Config
{
    /// <summary>
    /// Configuration for the pre-round lobby and SCP selection plugin.
    /// </summary>
    public sealed class PluginConfig
    {
        [Description("Whether the LobbyScpSelector plugin is enabled.")]
        public bool IsEnabled { get; set; } = true;

        [Description("Whether to output debug logs.")]
        public bool Debug { get; set; } = false;

        [Description("World coordinates for the lobby spawn point; updated and saved automatically via the lobbyspawn set command.")]
        public Vector3 LobbySpawnPosition { get; set; } = new Vector3(-0.09920526f, 295.3805f, -8.435011f);

        [Description("Scale multiplier for SCP selection coins (must be greater than 0).")]
        public float CoinScale { get; set; } = 17f;

        [Description("Minimum number of connected players required to select or assign SCP-079.")]
        public int MinPlayersForScp079 { get; set; } = 7;

        [Description("Whether to enable SCP overflow slot assignment at round start.")]
        public bool AllowScpOverflow { get; set; } = true;

        [Description("Whether to reorder the vanilla start team queue so SCP slots are placed first.")]
        public bool UseCustomStartQueue { get; set; } = true;

        [Description("World coordinates for SCP selection coins; updated and saved automatically via scpcoin <scp>. Remove an entry to disable that coin.")]
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

        [Description("Custom Euler rotations (in degrees) for the TextToy labels above coins; recorded automatically to match player facing when placed via scpcoin. Roles omitted here use coin_label fallback rules.")]
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

        [Description("Settings for the TextToy labels spawned automatically above each coin.")]
        public CoinLabelConfig CoinLabel { get; set; } = new CoinLabelConfig();

        /// <summary>
        /// Ensures dictionaries and nested settings are non-null after YAML deserialization.
        /// </summary>
        public void EnsureDefaults()
        {
            ScpSelectionCoinPositions ??= new Dictionary<RoleTypeId, Vector3>();
            ScpSelectionCoinLabelRotations ??= new Dictionary<RoleTypeId, Vector3>();
            CoinLabel ??= new CoinLabelConfig();
        }
    }

    /// <summary>
    /// Configuration for automatic TextToy labels above SCP selection coins.
    /// </summary>
    public sealed class CoinLabelConfig
    {
        [Description("Whether to automatically spawn a TextToy label above each coin.")]
        public bool Enabled { get; set; } = true;

        [Description("Position offset relative to the coin coordinates (default 1.2m directly above).")]
        public Vector3 Offset { get; set; } = new Vector3(0f, 1.2f, 0f);

        [Description("Text scale (x/y/z); each component must be greater than 0.")]
        public Vector3 Scale { get; set; } = new Vector3(0.5f, 0.5f, 0.5f);

        [Description("Text display box size (x = width, y = height); both must be greater than 0.")]
        public Vector2 DisplaySize { get; set; } = new Vector2(1000f, 1000f);

        [Description("Whether to horizontally orient unrotated labels toward LobbySpawnPosition.")]
        public bool FaceLobbySpawn { get; set; } = true;

        [Description("Fallback Euler rotation (in degrees) when auto-facing is disabled or aligned with the spawn.")]
        public Vector3 DefaultRotation { get; set; } = Vector3.zero;

        [Description("Optional override template for coin labels (supports rich text and {scp}/{role}). Leave empty to use coin_label_text_format from translations.yml.")]
        public string TextFormat { get; set; } = string.Empty;
    }
}
