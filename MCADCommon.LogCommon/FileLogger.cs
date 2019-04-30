using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MCADCommon.LogCommon
{
    public class FileLogger : IDisposable, Logger
    {
        private string LogFilePath;
        private Utils.LogLevel Level;

        private TextWriter _Writer;
        private TextWriter Writer { get { if (_Writer == null) _Writer = InitWriter(LogFilePath); return _Writer; } }

        public static string CreateLogFilePath(string folderPath, string applicationName)
        {
            string fileName = applicationName + "_" + string.Format("{0:yyyyMMdd}", DateTime.Now.Date) + ".log";

            if (folderPath.Equals("%temp%", StringComparison.InvariantCultureIgnoreCase) ||
                folderPath.Length < 1 ||
                !Directory.Exists(folderPath))
                return Path.Combine(Path.GetTempPath(), fileName);
            else
                return Path.Combine(folderPath, fileName);
        }

        public FileLogger(string logFilePath, Utils.LogLevel level)
        {
            LogFilePath = logFilePath;
            Level = level;
        }

        private static TextWriter InitWriter(string path)
        {
            string directoryPath = Path.GetDirectoryName(path);
            if (directoryPath.Length > 0)
                Directory.CreateDirectory(directoryPath);

            return new StreamWriter(path, true);
        }

        public void Trace(string message)
        {
            Log(message, Utils.LogLevel.Trace);
        }

        public void Info(string message)
        {
            Log(message, Utils.LogLevel.Info);
        }

        public void Warning(string message)
        {
            Log(message, Utils.LogLevel.Warning);
        }

        public void Error(string message)
        {
            Log(message, Utils.LogLevel.Error);
        }

        private void Log(string message, Utils.LogLevel level)
        {
            if (level < Level)
                return;

            Writer.WriteLine(DateTime.Now.ToString() + " " + level.ToString().ToUpper() + " " + message);
        }

        public void Dispose()
        {
            if (_Writer != null)
            {
                _Writer.Flush(); 

                _Writer.Dispose();
            }
        }

        public static void LogError(string logFilePath, string message)
        {
            LogMessage(logFilePath, message, Utils.LogLevel.Error);
        }

        public static void LogWarning(string logFilePath, string message)
        {
            LogMessage(logFilePath, message, Utils.LogLevel.Warning);
        }

        public static void LogInfo(string logFilePath, string message)
        {
            LogMessage(logFilePath, message, Utils.LogLevel.Info);
        }

        public static void LogTrace(string logFilePath, string message)
        {
            LogMessage(logFilePath, message, Utils.LogLevel.Trace);
        }

        private static object Lock = new object();
        public static void LogMessage(string logFilePath, string message, Utils.LogLevel level)
        {
            lock (Lock)
            {
                using (FileLogger logger = new FileLogger(logFilePath, level))
                    logger.Log(message, level);
            }
        }
    }
}
