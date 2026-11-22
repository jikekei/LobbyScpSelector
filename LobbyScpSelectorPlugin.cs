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
    /// Standalone LabAPI plugin entry point for the pre-round lobby and SCP coin selector.
    /// </summary>
    public sealed class LobbyScpSelectorPlugin : Plugin<PluginConfig>
    {
        public const string TranslationFileName = "translations.yml";

        public static LobbyScpSelectorPlugin Instance { get; private set; }

        public override string Name => "LobbyScpSelector";
        public override string Description => "Standalone waiting lobby and coin-based SCP selection plugin with in-game coordinate commands and auto-saved configs.";
        public override string Author => "LobbyScpSelector";
        public override Version Version => new Version(0, 19, 0);
        public override Version RequiredApiVersion => new Version(1, 1, 7);

        public string ConfigPath => ConfigurationLoader.GetConfigPath(this, ConfigFileName, false);
        public string TranslationPath => ConfigurationLoader.GetConfigPath(this, TranslationFileName, false);

        public TranslationConfig Translations { get; private set; } = new TranslationConfig();

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

            LoadTranslations();

            Instance = this;
            try
            {
                RegisterModule(LobbyHarmonyPatches.OnEnable, LobbyHarmonyPatches.OnDisable);
                RegisterModule(LobbyManager.Register, LobbyManager.Unregister);
                RegisterModule(ScpSelectionManager.Register, ScpSelectionManager.Unregister);
                PluginLog.Info($"LobbyScpSelector 插件已启用（配置: {ConfigPath}，翻译: {TranslationPath}）。");
            }
            catch
            {
                Disable();
                throw;
            }
        }

        private void LoadTranslations()
        {
            try
            {
                if (ConfigurationLoader.TryLoadConfig(this, TranslationFileName, out TranslationConfig loaded, false) &&
                    loaded != null)
                {
                    loaded.EnsureDefaults();
                    Translations = loaded;
                    return;
                }
            }
            catch (Exception ex)
            {
                PluginLog.Warn($"加载翻译配置文件失败，将使用默认英文翻译并尝试重新生成 ({TranslationPath}): {ex.Message}");
            }

            Translations = new TranslationConfig();
            Translations.EnsureDefaults();

            try
            {
                ConfigurationLoader.TrySaveConfig(this, Translations, TranslationFileName, false);
            }
            catch (Exception ex)
            {
                PluginLog.Warn($"写入默认翻译配置文件失败 ({TranslationPath}): {ex.Message}");
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
