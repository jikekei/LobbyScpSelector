using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using LobbyScpSelector.Config;
using LobbyScpSelector.Lobby;
using LobbyScpSelector.Utils;
using PlayerRoles;
using PlayerRoles.RoleAssign;
using UnityEngine;

namespace LobbyScpSelector.ScpSelection
{
    /// <summary>
    /// SCP 角色自选核心模块：管理硬币生成、上方 TextToy 标签同步、拾取选择、开局优先分配以及实时放置替换。
    /// </summary>
    internal static class ScpSelectionManager
    {
        internal const string DefaultStartTeamQueue = "4014314031441404134041434414";

        private static readonly Dictionary<Player, RoleTypeId> SelectedRoles = new Dictionary<Player, RoleTypeId>();
        private static readonly Dictionary<RoleTypeId, int> RoleSelectionCounts = new Dictionary<RoleTypeId, int>();
        private static readonly Dictionary<ushort, RoleTypeId> ConfirmedCoinRoles = new Dictionary<ushort, RoleTypeId>();
        private static readonly Dictionary<RoleTypeId, Pickup> ConfirmedCoinsByRole = new Dictionary<RoleTypeId, Pickup>();
        private static readonly Dictionary<RoleTypeId, TextToy> ConfirmedLabelsByRole = new Dictionary<RoleTypeId, TextToy>();
        private static readonly HashSet<RoleTypeId> AssignedScpsThisRound = new HashSet<RoleTypeId>();

        internal static readonly List<ReferenceHub> RoundStartPlayerOrder = new List<ReferenceHub>();

        private static int _roundStartScpSlots;
        private static bool _registered;

        public static string GetSelectionStatusText(Player player)
        {
            if (player == null)
                return string.Empty;

            TranslationConfig translations = LobbyScpSelectorPlugin.Instance?.Translations ?? new TranslationConfig();
            translations.EnsureDefaults();

            if (SelectedRoles.TryGetValue(player, out RoleTypeId selectedRole))
            {
                int playerCount = RoleSelectionCounts.TryGetValue(selectedRole, out int count) ? count : 0;
                return translations.SelectionCurrentFormat
                    .Replace("{scp}", ScpRoleParser.FormatDisplayName(selectedRole))
                    .Replace("{count}", playerCount.ToString());
            }

            return translations.SelectionNoneText;
        }

        private static void OnPickingUpItem(PlayerPickingUpItemEventArgs ev)
        {
            try
            {
                if (ev?.Pickup == null || ev.Player == null)
                    return;

                if (!ConfirmedCoinRoles.TryGetValue(ev.Pickup.Serial, out RoleTypeId targetRole))
                    return;

                ev.IsAllowed = false;
                if (!LobbyManager.IsLobby)
                    return;

                TranslationConfig translations = LobbyScpSelectorPlugin.Instance?.Translations ?? new TranslationConfig();
                translations.EnsureDefaults();

                int min079 = GetMinPlayersForScp079();
                if (targetRole == RoleTypeId.Scp079 && Player.Count < min079)
                {
                    LobbyManager.ShowMessage(
                        ev.Player,
                        translations.Scp079MinPlayersMessage.Replace("{min_players}", min079.ToString()),
                        4f);
                    return;
                }

                if (SelectedRoles.TryGetValue(ev.Player, out RoleTypeId currentRole))
                {
                    if (currentRole == targetRole)
                    {
                        LobbyManager.ShowMessage(
                            ev.Player,
                            translations.AlreadySelectedMessage.Replace("{scp}", ScpRoleParser.FormatDisplayName(targetRole)),
                            3f);
                        return;
                    }

                    if (RoleSelectionCounts.TryGetValue(currentRole, out int oldCount) && oldCount > 0)
                        RoleSelectionCounts[currentRole] = oldCount - 1;
                }

                if (!RoleSelectionCounts.TryGetValue(targetRole, out int curCount))
                    curCount = 0;

                RoleSelectionCounts[targetRole] = curCount + 1;
                SelectedRoles[ev.Player] = targetRole;

                LobbyManager.ShowMessage(
                    ev.Player,
                    translations.SelectedSuccessMessage
                        .Replace("{scp}", ScpRoleParser.FormatDisplayName(targetRole))
                        .Replace("{count}", (curCount + 1).ToString()),
                    5f);
            }
            catch (Exception ex)
            {
                PluginLog.Error($"处理玩家拾取 SCP 自选硬币时出错: {ex}");
            }
        }

        private static void OnChangingRole(PlayerChangingRoleEventArgs ev)
        {
            try
            {
                if (ev?.Player == null || !ev.IsAllowed)
                    return;

                if (ev.ChangeReason != RoleChangeReason.RoundStart)
                    return;

                if (PlayerRolesUtils.GetTeam(ev.NewRole) != Team.SCPs)
                    return;

                if (ev.NewRole == RoleTypeId.Scp0492)
                    return;

                bool allow079 = Player.Count >= GetMinPlayersForScp079();
                RoleTypeId? selectedRole = !ScpPlayerPicker.IsOptedOutOfScp(ev.Player.ReferenceHub) &&
                                          SelectedRoles.TryGetValue(ev.Player, out RoleTypeId selection)
                    ? selection
                    : (RoleTypeId?)null;

                RoleTypeId targetRole = ChooseScpRole(selectedRole, allow079, _roundStartScpSlots);
                if (selectedRole == targetRole)
                    PluginLog.Debug($"[SCP自选] 玩家 {ev.Player.Nickname} 获得预选角色 {targetRole}");

                ev.NewRole = targetRole;
            }
            catch (Exception ex)
            {
                PluginLog.Error($"[SCP自选] 处理开局 SCP 角色分配时出错: {ex}");
            }
        }

        internal static RoleTypeId ChooseScpRole(RoleTypeId? selectedRole, bool allow079, int scpSlots)
        {
            allow079 = allow079 && scpSlots > 1 && !AssignedScpsThisRound.Contains(RoleTypeId.Scp079);

            var candidates = new List<RoleTypeId>(ScpRoleParser.SelectableRoles.Length);
            foreach (RoleTypeId role in ScpRoleParser.SelectableRoles)
            {
                if (role == RoleTypeId.Scp079 && !allow079)
                    continue;

                if (!AssignedScpsThisRound.Contains(role))
                    candidates.Add(role);
            }

            if (candidates.Count == 0)
            {
                foreach (RoleTypeId role in ScpRoleParser.SelectableRoles)
                {
                    if (role == RoleTypeId.Scp079 && !allow079)
                        continue;

                    candidates.Add(role);
                }
            }

            RoleTypeId targetRole = selectedRole.HasValue && candidates.Contains(selectedRole.Value)
                ? selectedRole.Value
                : candidates[UnityEngine.Random.Range(0, candidates.Count)];

            AssignedScpsThisRound.Add(targetRole);
            return targetRole;
        }

        internal static string PrepareStartQueue(string configuredQueue)
        {
            AssignedScpsThisRound.Clear();
            _roundStartScpSlots = 0;
            RoundStartPlayerOrder.Clear();
            LobbyManager.PreparePlayersForRound();

            List<Player> players = Player.ReadyList
                .Where(p => p != null && !p.IsDestroyed && RoleAssigner.CheckPlayer(p.ReferenceHub))
                .ToList();

            var preSelectors = players.Where(p => SelectedRoles.ContainsKey(p)).ToList();
            var others = players.Where(p => !SelectedRoles.ContainsKey(p)).ToList();
            Shuffle(preSelectors);
            Shuffle(others);

            RoundStartPlayerOrder.AddRange(preSelectors.Select(p => p.ReferenceHub));
            RoundStartPlayerOrder.AddRange(others.Select(p => p.ReferenceHub));

            bool useCustomQueue = LobbyScpSelectorPlugin.Instance?.Config?.UseCustomStartQueue ?? true;
            if (!useCustomQueue)
            {
                string effectiveQueue = string.IsNullOrEmpty(configuredQueue) ? DefaultStartTeamQueue : configuredQueue;
                for (int i = 0; i < players.Count; i++)
                {
                    Team team = (Team)(effectiveQueue[i % effectiveQueue.Length] - '0');
                    if (team == Team.SCPs)
                        _roundStartScpSlots++;
                }

                PluginLog.Debug($"[SCP自选] 使用原配置队列：{players.Count} 名玩家，{_roundStartScpSlots} 个 SCP 名额");
                return configuredQueue;
            }

            List<Team> queue = BuildDefaultStartTeamQueue(players.Count);
            _roundStartScpSlots = queue.Count(team => team == Team.SCPs);
            PluginLog.Debug($"[SCP自选] 自定义队列：{players.Count} 名玩家，{_roundStartScpSlots} 个 SCP 名额");
            return queue.Count == 0 ? DefaultStartTeamQueue : string.Concat(queue.Select(team => ((int)team).ToString()));
        }

        internal static List<ReferenceHub> PrioritizeSelectedPlayers(List<ReferenceHub> original)
        {
            _roundStartScpSlots = original?.Count ?? 0;
            if (original == null || original.Count == 0)
                return original;

            var result = new List<ReferenceHub>(original.Count);
            foreach (ReferenceHub hub in RoundStartPlayerOrder)
            {
                Player player = Player.Get(hub);
                if (player == null || player.IsDestroyed || !RoleAssigner.CheckPlayer(hub) ||
                    ScpPlayerPicker.IsOptedOutOfScp(hub) || !SelectedRoles.ContainsKey(player))
                    continue;

                if (!result.Contains(hub))
                    result.Add(hub);

                if (result.Count == original.Count)
                    return result;
            }

            foreach (ReferenceHub hub in original)
            {
                if (!result.Contains(hub))
                    result.Add(hub);

                if (result.Count == original.Count)
                    break;
            }

            return result;
        }

        internal static List<Team> BuildDefaultStartTeamQueue(int playerCount)
        {
            var scps = new List<Team>();
            var humans = new List<Team>();
            for (int i = 0; i < playerCount; i++)
            {
                Team team = (Team)(DefaultStartTeamQueue[i % DefaultStartTeamQueue.Length] - '0');
                if (team == Team.SCPs)
                    scps.Add(team);
                else
                    humans.Add(team);
            }

            scps.AddRange(humans);
            return scps;
        }

        private static void OnPlayerLeft(PlayerLeftEventArgs ev)
        {
            try
            {
                if (ev?.Player == null)
                    return;

                LobbyManager.ClearMessage(ev.Player);
                if (!SelectedRoles.TryGetValue(ev.Player, out RoleTypeId role))
                    return;

                SelectedRoles.Remove(ev.Player);
                if (RoleSelectionCounts.TryGetValue(role, out int count) && count > 0)
                    RoleSelectionCounts[role] = count - 1;
            }
            catch (Exception ex)
            {
                PluginLog.Error($"处理玩家离开时清理 SCP 自选数据出错: {ex}");
            }
        }

        internal static bool PlaceOrReplaceCoin(
            RoleTypeId role,
            Vector3 position,
            out Pickup pickup,
            out bool spawnedImmediately)
        {
            DestroyConfirmedCoin(role);

            if (!LobbyManager.IsLobby)
            {
                pickup = null;
                spawnedImmediately = false;
                return true;
            }

            pickup = CreateConfirmedCoin(role, position);
            spawnedImmediately = pickup != null;
            return spawnedImmediately;
        }

        internal static void RemoveCoin(RoleTypeId role) => DestroyConfirmedCoin(role);

        internal static void RefreshActiveLabels()
        {
            PluginConfig config = LobbyScpSelectorPlugin.Instance?.Config;
            CoinLabelConfig labelConfig = config?.CoinLabel;
            if (config == null || labelConfig == null)
                return;

            foreach (KeyValuePair<RoleTypeId, TextToy> entry in ConfirmedLabelsByRole)
            {
                TextToy toy = entry.Value;
                if (toy == null || toy.IsDestroyed)
                    continue;

                toy.Rotation = CoinTextLabelHelper.ComputeRotation(entry.Key, toy.Position, config, labelConfig);
            }
        }

        internal static int ReloadConfirmedCoins()
        {
            DestroyConfirmedCoins();

            Dictionary<RoleTypeId, Vector3> positions = LobbyScpSelectorPlugin.Instance?.Config?.ScpSelectionCoinPositions;
            if (positions == null)
            {
                PluginLog.Warn("SCP 自选硬币坐标配置为空，未生成选择硬币。");
                return 0;
            }

            int created = 0;
            foreach (KeyValuePair<RoleTypeId, Vector3> entry in positions)
            {
                if (!ScpRoleParser.SelectableRoleSet.Contains(entry.Key) || !CoordinateHelper.IsFinite(entry.Value))
                {
                    PluginLog.Warn($"跳过无效的 SCP 自选硬币配置: {entry.Key}, {entry.Value}");
                    continue;
                }

                if (CreateConfirmedCoin(entry.Key, entry.Value) != null)
                    created++;
            }

            return created;
        }

        private static Pickup CreateConfirmedCoin(RoleTypeId roleType, Vector3 position)
        {
            try
            {
                DestroyConfirmedLabel(roleType);

                Pickup pickup = CreateCoinPickup(position);
                if (pickup != null)
                {
                    ConfirmedCoinRoles[pickup.Serial] = roleType;
                    ConfirmedCoinsByRole[roleType] = pickup;

                    TextToy label = CoinTextLabelHelper.CreateLabel(roleType, position);
                    if (label != null)
                        ConfirmedLabelsByRole[roleType] = label;

                    PluginLog.Debug($"成功创建 {roleType} 选择硬币，位置: {position}");
                }
                else
                {
                    PluginLog.Warn($"创建 {roleType} 选择硬币失败，位置: {position}");
                }

                return pickup;
            }
            catch (Exception ex)
            {
                PluginLog.Error($"创建 {roleType} 选择硬币时出错: {ex}");
                return null;
            }
        }

        private static Pickup CreateCoinPickup(Vector3 position)
        {
            float scale = LobbyScpSelectorPlugin.Instance?.Config?.CoinScale ?? 17f;
            if (!CoordinateHelper.IsFinite(scale) || scale <= 0f)
                scale = 17f;

            return Pickup.Create(ItemType.Coin, position, Quaternion.identity, Vector3.one * scale, true);
        }

        private static void WaitingPlayers()
        {
            try
            {
                DestroyConfirmedCoins();
                AssignedScpsThisRound.Clear();
                RoundStartPlayerOrder.Clear();
                _roundStartScpSlots = 0;
                SelectedRoles.Clear();
                RoleSelectionCounts.Clear();

                int count = ReloadConfirmedCoins();
                PluginLog.Debug($"SCP 自选系统初始化完成，共生成 {count} 枚选择硬币。");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"初始化 SCP 自选系统时出错: {ex}");
            }
        }

        private static void OnRoundStarted() => DestroyConfirmedCoins();

        private static void OnRoundEnded(RoundEndedEventArgs ev) => DestroyConfirmedCoins();

        private static void OnRoundRestarted() => DestroyConfirmedCoins();

        private static void DestroyConfirmedLabel(RoleTypeId role)
        {
            if (!ConfirmedLabelsByRole.TryGetValue(role, out TextToy label))
                return;

            ConfirmedLabelsByRole.Remove(role);
            CoinTextLabelHelper.DestroySafely(label);
        }

        private static void DestroyAllConfirmedLabels()
        {
            foreach (TextToy label in ConfirmedLabelsByRole.Values)
                CoinTextLabelHelper.DestroySafely(label);

            ConfirmedLabelsByRole.Clear();
        }

        private static void DestroyConfirmedCoin(RoleTypeId role)
        {
            DestroyConfirmedLabel(role);

            if (!ConfirmedCoinsByRole.TryGetValue(role, out Pickup existing))
                return;

            ConfirmedCoinsByRole.Remove(role);
            if (existing == null)
                return;

            ConfirmedCoinRoles.Remove(existing.Serial);
            if (!existing.IsDestroyed)
                existing.Destroy();
        }

        private static void DestroyConfirmedCoins()
        {
            DestroyAllConfirmedLabels();

            foreach (Pickup coin in ConfirmedCoinsByRole.Values)
            {
                if (coin != null && !coin.IsDestroyed)
                    coin.Destroy();
            }

            ConfirmedCoinsByRole.Clear();
            ConfirmedCoinRoles.Clear();
        }

        private static int GetMinPlayersForScp079() =>
            Math.Max(0, LobbyScpSelectorPlugin.Instance?.Config?.MinPlayersForScp079 ?? 7);

        private static void Shuffle<T>(List<T> list)
        {
            if (list == null || list.Count <= 1)
                return;

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        public static void Register()
        {
            if (_registered)
                return;

            _registered = true;
            ServerEvents.WaitingForPlayers += WaitingPlayers;
            ServerEvents.RoundStarted += OnRoundStarted;
            ServerEvents.RoundEnded += OnRoundEnded;
            ServerEvents.RoundRestarted += OnRoundRestarted;
            PlayerEvents.PickingUpItem += OnPickingUpItem;
            PlayerEvents.ChangingRole += OnChangingRole;
            PlayerEvents.Left += OnPlayerLeft;
        }

        public static void Unregister()
        {
            if (!_registered)
                return;

            _registered = false;
            ServerEvents.WaitingForPlayers -= WaitingPlayers;
            ServerEvents.RoundStarted -= OnRoundStarted;
            ServerEvents.RoundEnded -= OnRoundEnded;
            ServerEvents.RoundRestarted -= OnRoundRestarted;
            PlayerEvents.PickingUpItem -= OnPickingUpItem;
            PlayerEvents.ChangingRole -= OnChangingRole;
            PlayerEvents.Left -= OnPlayerLeft;

            DestroyConfirmedCoins();
            RoundStartPlayerOrder.Clear();
            AssignedScpsThisRound.Clear();
            _roundStartScpSlots = 0;
            SelectedRoles.Clear();
            RoleSelectionCounts.Clear();
            PluginLog.Debug("SCP 自选系统事件已注销。");
        }
    }
}
