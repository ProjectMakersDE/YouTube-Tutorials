using System;
using System.IO;
using System.Runtime.CompilerServices;
using PM.Core;
using UnityEngine;

namespace PM.Service
{
    public class Log
    {
        private readonly LogLevel _logLevel = GetLogLevel();
        private readonly string _path = App.Config.GetValue("Debugging", "LogPath", "/Logs") ?? "/Logs";
        private readonly string _title = App.Config.GetValue("General", "AppTitle", "App") ?? "App";

        private static LogLevel GetLogLevel()
        {
            var logLevelString = App.Config.GetValue<string>("Debugging", "LogLevel") ?? "Info";
            UnityEngine.Debug.Log($"logLevelString: {logLevelString}");
            return Enum.TryParse(logLevelString, out LogLevel logLevel) ? logLevel : LogLevel.Info;
        }

        public void Debug(string message, ErrorCode code = ErrorCode.None, int maxLength = 1000, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "")
        {
            WriteLog(message, LogLevel.Debug, code, maxLength, caller, filePath);
        }

        public void Info(string message, ErrorCode code = ErrorCode.Ok, int maxLength = 1000, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "")
        {
            WriteLog(message, LogLevel.Info, code, maxLength, caller, filePath);
        }

        public void Warning(string message, ErrorCode code = ErrorCode.None, int maxLength = 1000, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "")
        {
            WriteLog(message, LogLevel.Warn, code, maxLength, caller, filePath);
        }

        public void Error(string message, ErrorCode code = ErrorCode.UnknownError, int maxLength = 1000, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "")
        {
            WriteLog(message, LogLevel.Error, code, maxLength, caller, filePath);
        }

        public void Fatal(string message, ErrorCode code = ErrorCode.UnknownError, int maxLength = 1000, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "")
        {
            WriteLog(message, LogLevel.Fatal, code, maxLength, caller, filePath);
        }

        private void WriteLog(string log, LogLevel type, ErrorCode code, int maxLength, string caller, string filePath)
        {
            if (_logLevel > type) return;

            string content = $"{DateTime.UtcNow:dd-hh:mm:ss} [{type}] [{(int)code}] [{GetShortFilePath(filePath)}.{caller}] {log}";

            if (content.Length > maxLength)
                content = content[..maxLength] + "...";

            WriteToConsole(content, type);
            WriteToFile(content);
        }

        private static void WriteToConsole(string content, LogLevel type)
        {
            var colorPrefix = type switch
            {
                LogLevel.Debug => "<color=white>",
                LogLevel.Info => "<color=white>",
                LogLevel.Warn => "<color=yellow>",
                LogLevel.Error => "<color=#DE523D>",
                LogLevel.Fatal => "<color=#DE523D>",
                _ => "<color=white>"
            };

            UnityEngine.Debug.Log($"{colorPrefix}{content}</color>");
        }

        private void WriteToFile(string content)
        {
            var fileName = $"{DateTime.UtcNow:yyyy-MM}.log";
            var path = Path.Combine(Application.streamingAssetsPath, _path, fileName);

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_IOS
    path = "file://" + path;
#endif

            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                using StreamWriter sw = File.CreateText(path);

                sw.WriteLine($"{_title} - Log[{DateTime.UtcNow:yyyy-MM}]");
                sw.WriteLine();
            }

            using (StreamWriter sw = File.AppendText(path))
                sw.WriteLine(content);
        }

        private static string GetShortFilePath(string filePath)
        {
            var path = filePath.AsSpan();

            int dotIndex = path.LastIndexOf('.');
            int firstSeparator = path.IndexOf("Scripts") + 8;

            if (firstSeparator == -1 || dotIndex == -1 || firstSeparator >= dotIndex)
                return "Unknown";

            var result = path.Slice(firstSeparator, dotIndex - firstSeparator);
            return result.ToString().Replace('/', '.').Replace('\\', '.');
        }
    }
}