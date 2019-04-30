using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Logger
{
    public class Logger : IDisposable
    {
        private TextWriter Writer;
        private LogLevel Level;

        public enum LogLevel { Trace, Info, Error };

        public Logger(string path, LogLevel level)
        {
            Init(path, level);
        }

        private void Init(string path, LogLevel level)
        {
            string directoryPath = Path.GetDirectoryName(path);
            if (directoryPath.Length > 0)
                Directory.CreateDirectory(directoryPath);

            Writer = new StreamWriter(path, true);

            Level = level;
        }

        public void Trace(string message)
        {
            Log(message, LogLevel.Trace);
        }
        public void Info(string message)
        {
            Log(message, LogLevel.Info);
        }
        public void Error(string message)
        {
            Log(message, LogLevel.Error);
        }

        private void Log(string message, LogLevel level)
        {
            if (level < Level)
                return;

            Writer.WriteLine(DateTime.Now.ToString() + " " + message);
        }

        public void Dispose()
        {
            Writer.Flush();

            Writer.Dispose();
        }

        public static void LogError(string message, string path)
        {
            using (Logger logger = new Logger(path, LogLevel.Error))
                logger.Log(message, LogLevel.Error);
        }
    }
}
