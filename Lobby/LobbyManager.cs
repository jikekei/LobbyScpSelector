using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using LobbyScpSelector.Config;
using LobbyScpSelector.ScpSelection;
using LobbyScpSelector.Utils;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace LobbyScpSelector.Lobby
{
    /// <summary>
    /// 开始大厅管理模块：玩家入厅传送、倒计时状态 HUD 刷新与开局角色重置。
    /// </summary>
    internal static class LobbyManager
    {
        private sealed class TransientMessage
        {
            public string Text;
            public float Until;
        }

        private static readonly Vector3 FallbackLobbySpawnPosition = new Vector3(-0.09920526f, 295.3805f, -8.435011f);
        private static readonly Dictionary<Player, TransientMessage> Messages = new Dictionary<Player, TransientMessage>();
        private static CoroutineHandle _lobbyLoop;
        private static bool _registered;
        private static int _generation;

        public static bool IsInLobby { get; private set; }
        public static bool IsLobby => IsInLobby && !Round.IsRoundStarted;

        public static void ShowMessage(Player player, string message, float duration)
        {
            if (player == null || player.IsDestroyed)
                return;

            Messages[player] = new TransientMessage
            {
                Text = message,
                Until = Time.realtimeSinceStartup + Math.Max(0.5f, duration)
            };

            if (IsLobby)
                ShowLobbyHint(player);
            else
                player.SendHint(message, duration);
        }

        public static void ClearMessage(Player player)
        {
            if (player == null)
                return;

            Messages.Remove(player);
        }

        private static void ShowLobbyHint(Player player)
        {
            if (player == null || player.IsDestroyed || GameCore.RoundStart.singleton == null)
                return;

            TranslationConfig translations = LobbyScpSelectorPlugin.Instance?.Translations ?? new TranslationConfig();
            translations.EnsureDefaults();

            int timer = GameCore.RoundStart.singleton.NetworkTimer;
            string state = timer == -2
                ? translations.LobbyStatePaused
                : timer == -1
                    ? translations.LobbyStateStarting
                    : translations.LobbyStateCountdown.Replace("{timer}", timer.ToString());

            string transient = string.Empty;
            if (Messages.TryGetValue(player, out TransientMessage message))
            {
                if (Time.realtimeSinceStartup < message.Until)
                    transient = "\n\n" + message.Text;
                else
                    Messages.Remove(player);
            }

            string header = translations.LobbyHeaderFormat
                .Replace("{count}", Player.Count.ToString())
                .Replace("{state}", state);

            string text = header + "\n" +
                          ScpSelectionManager.GetSelectionStatusText(player) + "\n" +
                          translations.LobbyTipText +
                          transient;

            player.SendHint(text, 1.3f);
        }

        private static IEnumerator<float> LobbyLoop()
        {
            while (_registered && IsLobby)
            {
                foreach (Player player in Player.ReadyList)
                {
                    if (player != null && !player.IsDestroyed)
                        ShowLobbyHint(player);
                }

                yield return Timing.WaitForSeconds(1f);
            }
        }

        private static void OnJoin(PlayerJoinedEventArgs ev)
        {
            int generation = _generation;
            Timing.CallDelayed(0.5f, () =>
            {
                if (!_registered || generation != _generation || !IsLobby ||
                    ev?.Player == null || ev.Player.IsDestroyed || GameCore.RoundStart.singleton == null)
                    return;

                int timer = GameCore.RoundStart.singleton.NetworkTimer;
                if (!(timer > 1 || timer == -2))
                    return;

                Vector3 spawn = LobbyScpSelectorPlugin.Instance?.Config?.LobbySpawnPosition ?? FallbackLobbySpawnPosition;
                if (!CoordinateHelper.IsFinite(spawn))
                {
                    PluginLog.Warn("大厅出生坐标无效，已回退至默认坐标。");
                    spawn = FallbackLobbySpawnPosition;
                }

                ev.Player.ClearBroadcasts();
                ev.Player.SetRole(RoleTypeId.Tutorial, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.All);
                ev.Player.Position = spawn;
            });
        }

        private static void OnPlayerLeft(PlayerLeftEventArgs ev)
        {
            if (ev?.Player != null)
                ClearMessage(ev.Player);
        }

        private static void WaitingPlayers()
        {
            _generation++;
            Timing.KillCoroutines(_lobbyLoop);
            Messages.Clear();
            ClearAllHints();
            IsInLobby = true;

            GameObject startRound = GameObject.Find("StartRound");
            if (startRound != null)
                startRound.transform.localScale = Vector3.zero;

            _lobbyLoop = Timing.RunCoroutine(LobbyLoop());
        }

        internal static void PreparePlayersForRound()
        {
            _generation++;
            IsInLobby = false;
            Timing.KillCoroutines(_lobbyLoop);
            Messages.Clear();
            ClearAllHints();

            foreach (Player player in Player.ReadyList)
            {
                if (player == null || player.IsDestroyed)
                    continue;

                if (player.Role != RoleTypeId.Overwatch && player.IsAlive)
                    player.SetRole(RoleTypeId.Spectator, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.All);
            }
        }

        private static void EndLoop(RoundEndedEventArgs ev) => CleanupLobbyState();

        private static void OnRoundRestarted() => CleanupLobbyState();

        private static void CleanupLobbyState()
        {
            _generation++;
            IsInLobby = false;
            Timing.KillCoroutines(_lobbyLoop);
            Messages.Clear();
            ClearAllHints();
        }

        private static void ClearAllHints()
        {
            foreach (Player player in Player.ReadyList)
            {
                if (player != null && !player.IsDestroyed)
                    player.SendHint(string.Empty, 0.1f);
            }
        }

        public static void Register()
        {
            if (_registered)
                return;

            _registered = true;
            ServerEvents.WaitingForPlayers += WaitingPlayers;
            ServerEvents.RoundEnded += EndLoop;
            ServerEvents.RoundRestarted += OnRoundRestarted;
            PlayerEvents.Joined += OnJoin;
            PlayerEvents.Left += OnPlayerLeft;
        }

        public static void Unregister()
        {
            if (!_registered)
                return;

            _registered = false;
            ServerEvents.WaitingForPlayers -= WaitingPlayers;
            ServerEvents.RoundEnded -= EndLoop;
            ServerEvents.RoundRestarted -= OnRoundRestarted;
            PlayerEvents.Joined -= OnJoin;
            PlayerEvents.Left -= OnPlayerLeft;
            CleanupLobbyState();
        }
    }
}
