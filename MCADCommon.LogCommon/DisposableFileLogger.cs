using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MCADCommon.LogCommon
{
    public class DisposableFileLogger : IDisposable, Logger
    {
        private string LogFilePath;
        private Utils.LogLevel Level;
        private Boolean _errorEncountered;

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

        public DisposableFileLogger(string logFilePath, Utils.LogLevel level)
        {
            LogFilePath = logFilePath;
            Level = level;
            ErrorEncountered = false;

            string directoryPath = Path.GetDirectoryName(LogFilePath);
            if (directoryPath.Length > 0)
                Directory.CreateDirectory(directoryPath);

        }

        public Boolean ErrorEncountered
        {
            get { return _errorEncountered; }
            set { _errorEncountered = value; }
        }

        public string getLogFilePath()
        {
            return this.LogFilePath;
        }

        public void Dispose()
        {
            
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
            ErrorEncountered = true;
            Log(message, Utils.LogLevel.Error);
        }

        private void Log(string message, Utils.LogLevel level)
        {
            if (level < Level)
                return;

            WriteToLog(DateTime.Now.ToString() + " " + level.ToString().ToUpper() + " " + message);
        }

        private void WriteToLog(string textToLog)
        {
            using(StreamWriter sw = new StreamWriter(LogFilePath, true))
            {
                sw.WriteLine(textToLog);
                sw.Close();
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
                using (DisposableFileLogger logger = new DisposableFileLogger(logFilePath, level))
                    logger.Log(message, level);
            }
        }
    }
}
