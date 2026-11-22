using LabApi.Features.Console;

namespace LobbyScpSelector.Utils
{
    /// <summary>
    /// 统一日志输出工具。
    /// </summary>
    internal static class PluginLog
    {
        internal static void Debug(object message)
        {
            bool enabled = LobbyScpSelectorPlugin.Instance?.Config?.Debug == true;
            Logger.Debug(message, enabled);
        }

        internal static void Debug(object message, bool enabled) => Logger.Debug(message, enabled);
        internal static void Info(object message) => Logger.Info(message);
        internal static void Warn(object message) => Logger.Warn(message);
        internal static void Error(object message) => Logger.Error(message);
    }
}
