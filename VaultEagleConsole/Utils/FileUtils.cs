using Common.DotNet.Extensions;
using NDesk.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaultEagle;
using VaultEagleLib;
using Option = Common.DotNet.Extensions.Option;

namespace VaultEagleConsole
{
    public static class FileUtils
    {
        public static DateTime ParseLogTime(string logFileName, string logName, string parseString)
        {
            string dateTimeText = logFileName.Substring(logName.Length + 1, 19);
            return DateTime.ParseExact(dateTimeText, parseString, new CultureInfo("en-US"));
        }

        public static Option<TimeSpan> ParseTimeSpan(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return Option.None;
            if (System.Text.RegularExpressions.Regex.IsMatch(s, @"^[0-9]+\s*s?$"))
                return s.TrimStringAtEnd("s").Trim().OptionParseDouble().Transform(TimeSpan.FromSeconds);
            if (System.Text.RegularExpressions.Regex.IsMatch(s, @"^[0-9]+\s*(m|min)$"))
                return s.TrimStringAtEnd("min").TrimStringAtEnd("m").Trim().OptionParseDouble().Transform(TimeSpan.FromMinutes);
            if (System.Text.RegularExpressions.Regex.IsMatch(s, @"^[0-9]+\s*h$"))
                return s.TrimStringAtEnd("h").Trim().OptionParseDouble().Transform(TimeSpan.FromHours);
            return Option.None;
        }

        public static void RemoveOldLogFiles(string path, string logFileName, int allowedOldLogs)
        {
            List<FileInfo> oldLogFiles = new List<FileInfo>();
            foreach (FileInfo fileInLogPath in new DirectoryInfo(path).GetFiles())
                if (fileInLogPath.Name.StartsWith(logFileName))
                    oldLogFiles.Add(fileInLogPath);

            oldLogFiles.OrderBy(fileInfo => ParseLogTime(fileInfo.Name, logFileName, "yyyy-MM-dd HH-mm-ss"));

            while (oldLogFiles.Count >= allowedOldLogs)
            {
                FileInfo oldestLogFile = oldLogFiles.First();
                oldLogFiles.Remove(oldestLogFile);
                using (FileAttributeHandler attributeHandler = new FileAttributeHandler(oldestLogFile.FullName))
                    System.IO.File.Delete(oldestLogFile.FullName);
            }
        }
        public static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: VaultEagleConsole [OPTIONS]+ USER:PASS@SERVER/VAULT[/$/VAULTPATH/]");
            Console.WriteLine("Vault Eagle updates subscribed files from Vault. It checks if files are up to date");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }
    }

    public class ConsoleProgressReporter : IProgressWindow
    {
        public void Log(string text, string detailed)
        {
            lock (this)
            {
                if (detailed == null)
                    Console.WriteLine(text);
                else
                    Console.WriteLine(detailed);
            }
        }

        public void LogDone(bool failed) { }

        public void LogWithProgress(string text, int progress)
        {
            lock (this)
            {
                if (text != null)
                    Console.WriteLine("{0} ({1}%)", text, progress);
            }
        }
        public void Show() { }
    }

    public class DummyProgressReporter : IProgressWindow
    {
        public void Log(string text, string detailed) { }
        public void LogDone(bool failed) { }

        public void LogWithProgress(string text, int progress) { }
        public void Show() { }
    }

    public class DummySysTrayNotifyIconService : ISysTrayNotifyIconService
    {
        public void ShowIfSlow(string s) { }

        public void ShowNow(string s, bool ignoreMinimumDisplayTime) { }

        public void Start() { }
    }
    class FileAttributeHandler : IDisposable
    {
        private FileAttributes Attributes;
        private string Path = null;

        public FileAttributeHandler(string path)
        {
            if (System.IO.File.Exists(path))
            {
                Path = path;
                Attributes = System.IO.File.GetAttributes(path);
                System.IO.File.SetAttributes(path, FileAttributes.Normal);
            }
        }
        public void Dispose()
        {
            if (Path != null && System.IO.File.Exists(Path))
                System.IO.File.SetAttributes(Path, Attributes);
        }
    }
}
