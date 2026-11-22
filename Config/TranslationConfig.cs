using System.ComponentModel;

namespace LobbyScpSelector.Config
{
    /// <summary>
    /// In-game player-facing localization strings stored in translations.yml alongside config.yml.
    /// </summary>
    public sealed class TranslationConfig
    {
        [Description("Round state text shown when the lobby countdown is paused.")]
        public string LobbyStatePaused { get; set; } = "<color=#FACC15>Round Paused</color>";

        [Description("Round state text shown when the round is about to start.")]
        public string LobbyStateStarting { get; set; } = "<color=#4ADE80>Round Starting</color>";

        [Description("Round state text shown during countdown. Placeholders: {timer}.")]
        public string LobbyStateCountdown { get; set; } = "<color=#38BDF8>Starting in {timer}s</color>";

        [Description("Top lobby HUD line. Placeholders: {count}, {state}.")]
        public string LobbyHeaderFormat { get; set; } =
            "<size=34><b><color=#7CC6FE>{count}</color> <color=#F8FAFC>Players Connected</color>  <color=#64748B>|</color>  {state}</b></size>";

        [Description("Bottom lobby HUD instruction line.")]
        public string LobbyTipText { get; set; } =
            "<size=26><color=#93C5FD>Pick up a coin in front of a role to select that SCP</color></size>";

        [Description("Selection status line when the player has selected an SCP. Placeholders: {scp}, {count}.")]
        public string SelectionCurrentFormat { get; set; } =
            "<size=30><color=#E2E8F0>Selected SCP: </color><b><color=#FF6B6B>{scp}</color></b>  <color=#64748B>|</color>  <color=#CBD5E1>Same Pick: </color><b><color=#FDE047>{count}</color></b></size>";

        [Description("Selection status line when the player has not selected any SCP.")]
        public string SelectionNoneText { get; set; } =
            "<size=30><color=#CBD5E1>Selected SCP: </color><color=#94A3B8>None</color></size>";

        [Description("Hint shown when picking up the SCP-079 coin below the minimum player requirement. Placeholders: {min_players}.")]
        public string Scp079MinPlayersMessage { get; set; } =
            "<size=32><b><color=#F87171>SCP-079 cannot be selected with fewer than {min_players} players</color></b></size>";

        [Description("Hint shown when picking up the coin for the SCP the player has already selected. Placeholders: {scp}.")]
        public string AlreadySelectedMessage { get; set; } =
            "<size=32><b><color=#FACC15>You have already selected {scp}</color></b></size>";

        [Description("Hint shown when a player successfully selects an SCP. Placeholders: {scp}, {count}.")]
        public string SelectedSuccessMessage { get; set; } =
            "<size=32><b><color=#4ADE80>Selected {scp}</color></b></size>\n<size=26><color=#FDE047>Players selecting this SCP: {count}</color></size>";

        [Description("Default TextToy label template above each coin when coin_label.text_format in config.yml is empty. Placeholders: {scp}, {role}.")]
        public string CoinLabelTextFormat { get; set; } = "-SCP SELECT-\r\n{scp}";

        /// <summary>
        /// Ensures no translation field remains null after YAML deserialization.
        /// </summary>
        public void EnsureDefaults()
        {
            LobbyStatePaused ??= "<color=#FACC15>Round Paused</color>";
            LobbyStateStarting ??= "<color=#4ADE80>Round Starting</color>";
            LobbyStateCountdown ??= "<color=#38BDF8>Starting in {timer}s</color>";
            LobbyHeaderFormat ??= "<size=34><b><color=#7CC6FE>{count}</color> <color=#F8FAFC>Players Connected</color>  <color=#64748B>|</color>  {state}</b></size>";
            LobbyTipText ??= "<size=26><color=#93C5FD>Pick up a coin in front of a role to select that SCP</color></size>";
            SelectionCurrentFormat ??= "<size=30><color=#E2E8F0>Selected SCP: </color><b><color=#FF6B6B>{scp}</color></b>  <color=#64748B>|</color>  <color=#CBD5E1>Same Pick: </color><b><color=#FDE047>{count}</color></b></size>";
            SelectionNoneText ??= "<size=30><color=#CBD5E1>Selected SCP: </color><color=#94A3B8>None</color></size>";
            Scp079MinPlayersMessage ??= "<size=32><b><color=#F87171>SCP-079 cannot be selected with fewer than {min_players} players</color></b></size>";
            AlreadySelectedMessage ??= "<size=32><b><color=#FACC15>You have already selected {scp}</color></b></size>";
            SelectedSuccessMessage ??= "<size=32><b><color=#4ADE80>Selected {scp}</color></b></size>\n<size=26><color=#FDE047>Players selecting this SCP: {count}</color></size>";
            CoinLabelTextFormat ??= "-SCP SELECT-\r\n{scp}";
        }
    }
}
