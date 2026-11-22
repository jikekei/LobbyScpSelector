using System;
using System.Collections.Generic;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using LobbyScpSelector.Config;
using LobbyScpSelector.Lobby;
using LobbyScpSelector.Patches;
using LobbyScpSelector.ScpSelection;
using LobbyScpSelector.Utils;

namespace LobbyScpSelector
{
    /// <summary>
    /// 开始大厅 + SCP 自选独立 LabAPI 插件入口。
    /// </summary>
    public sealed class LobbyScpSelectorPlugin : Plugin<PluginConfig>
    {
        public static LobbyScpSelectorPlugin Instance { get; private set; }

        public override string Name => "LobbyScpSelector";
        public override string Description => "独立开始大厅与 SCP 自选插件，支持指令配置出生点及 SCP 自选硬币位置并自动写入配置文件";
        public override string Author => "LobbyScpSelector";
        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredApiVersion => new Version(1, 1, 7);

        public string ConfigPath => ConfigurationLoader.GetConfigPath(this, ConfigFileName, false);

        private readonly Stack<Action> _cleanup = new Stack<Action>();

        private void RegisterModule(Action start, Action stop)
        {
            _cleanup.Push(stop);
            start();
        }

        public override void Enable()
        {
            if (Instance == this)
                return;

            if (Config == null)
                LoadConfigs();

            if (Config == null)
            {
                PluginLog.Error("无法加载插件配置，插件启动已中止。");
                return;
            }

            Config.EnsureDefaults();
            if (!Config.IsEnabled)
            {
                PluginLog.Info("LobbyScpSelector 插件已在配置中禁用。");
                return;
            }

            Instance = this;
            try
            {
                RegisterModule(LobbyHarmonyPatches.OnEnable, LobbyHarmonyPatches.OnDisable);
                RegisterModule(LobbyManager.Register, LobbyManager.Unregister);
                RegisterModule(ScpSelectionManager.Register, ScpSelectionManager.Unregister);
                PluginLog.Info($"LobbyScpSelector 插件已启用（配置文件: {ConfigPath}）。");
            }
            catch
            {
                Disable();
                throw;
            }
        }

        public override void Disable()
        {
            if (Instance == this)
                Instance = null;

            while (_cleanup.Count > 0)
            {
                try
                {
                    _cleanup.Pop()();
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"关闭模块时出错，将继续清理其余模块: {ex}");
                }
            }

            PluginLog.Info("LobbyScpSelector 插件已关闭。");
        }

        /// <summary>
        /// 将内存中的最新配置写入配置文件。
        /// </summary>
        public bool TrySaveConfig(out string configPath, out string error)
        {
            configPath = string.Empty;
            error = null;

            if (Config == null)
            {
                error = "插件配置实例为空。";
                return false;
            }

            Config.EnsureDefaults();
            try
            {
                configPath = ConfigPath;
                if (ConfigurationLoader.TrySaveConfig(this, Config, ConfigFileName, false))
                {
                    PluginLog.Debug($"配置已写入: {configPath}");
                    return true;
                }

                SaveConfig();
                PluginLog.Debug($"配置已通过 SaveConfig 写入: {configPath}");
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                PluginLog.Error($"保存配置文件失败 ({configPath}): {ex}");
                return false;
            }
        }
    }
}
