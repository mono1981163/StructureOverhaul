using Common.DotNet.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCADCommon.LogCommon
{
    public static class Utils
    {
        public enum LogLevel { Trace, Info, Warning, Error };

        public static List<LogLevel> AllLogLevels
        {
            get
            {
                return new List<LogLevel> { LogLevel.Trace, LogLevel.Info, LogLevel.Warning, LogLevel.Error };
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public static LogLevel ParseLogLevel(string text)
        {
            switch (text)
            {
                case "Trace": 
                    return LogLevel.Trace;
                case "Info": 
                    return LogLevel.Info;
                case "Warning": 
                    return LogLevel.Warning;
                case "Error": 
                    return LogLevel.Error;
                default:
                    throw new ErrorMessageException("Unknown log level: '" + text + "'.");
            }
        }
    }
}
