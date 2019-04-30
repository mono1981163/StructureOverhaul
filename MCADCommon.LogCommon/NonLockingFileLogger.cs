using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MCADCommon.LogCommon
{
    public class NonLockingFileLogger : Logger
    {
        private string LogFilePath;
        private Utils.LogLevel Level;

        private object Lock = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////
        public NonLockingFileLogger(string logFilePath, Utils.LogLevel level)
        {
            LogFilePath = logFilePath;
            Level = level;

            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public void Trace(string message)
        {
            Log(message, Utils.LogLevel.Trace);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public void Info(string message)
        {
            Log(message, Utils.LogLevel.Info);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public void Warning(string message)
        {
            Log(message, Utils.LogLevel.Warning);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public void Error(string message)
        {
            Log(message, Utils.LogLevel.Error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        private void Log(string message, Utils.LogLevel level)
        {
            if (level < Level)
                return;

            lock (Lock)
            {
                using (TextWriter writer = new StreamWriter(LogFilePath))
                {
                    writer.WriteLine(DateTime.Now.ToString() + " " + level.ToString().ToUpper() + " " + message);
                    writer.Flush();
                }
            }
        }
    }
}
