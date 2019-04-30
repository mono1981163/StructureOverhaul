using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
//using Autodesk.Connectivity.WebServicesTools;
//using Autodesk.Connectivity.WebServices;
using Autodesk.DataManagement.Client.Framework.Currency;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Connections;
using Autodesk.DataManagement.Client.Framework.Vault.Results;
using NDesk.Options;
using System.Management;
using VaultEagle;

using Common.DotNet.Extensions;
using Option = Common.DotNet.Extensions.Option;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Diagnostics;

namespace VaultEagleConsole
{
    public class DummyProgressReporter : IProgressWindow
    {
        public void Log(string text, string detailed) { }
        public void LogWithProgress(string text, int progress) { }
        public void LogDone(bool failed) { }
        public void Show() { }
    }
    
    public class DummySysTrayNotifyIconService : ISysTrayNotifyIconService
    {
        public void Start() { }
        public void ShowIfSlow(string s) { }
        public void ShowNow(string s, bool ignoreMinimumDisplayTime) { }
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

        public void LogWithProgress(string text, int progress)
        {
            lock (this)
            {
                if (text != null)
                    Console.WriteLine("{0} ({1}%)", text, progress);
            }
        }

        public void LogDone(bool failed) { }
        public void Show() { }
    }

    class Program
    {
        // return - true : one of server is valid or ServerList is empty, false : no server is valid
        private static bool CheckServers(List<string> ServerList)
        {
            if (ServerList.Count == 0)
                return true;

            List<string> serverList = ServerList;
            List<Task<PingReply>> pingTasks = new List<Task<PingReply>>();
            foreach (var server in serverList)
            {
                pingTasks.Add(PingAsync(server));
            }

            Task.WaitAll(pingTasks.ToArray());

            foreach (var pingTask in pingTasks)
            {
                if (pingTask.Result != null)
                {
                    return true;
                }

            }

            return false;
        }

        // return - response from url
        static Task<PingReply> PingAsync(string address)
        {
            var tcs = new TaskCompletionSource<PingReply>();
            Ping ping = new Ping();
            ping.PingCompleted += (obj, sender) =>
            {
                tcs.SetResult(sender.Reply);
            };
            ping.SendAsync(address, new object());
            return tcs.Task;
        }

        static int Main(string[] args)
        {
/*#if DEBUG
                using (var streamWriter = new System.IO.StreamWriter(System.IO.Path.Combine(Utils.GetDirectoryOfExecutingAssembly().FullName, "debug.log"), true))
                {
                    streamWriter.AutoFlush = true;
                    streamWriter.WriteLine(DateTime.Now.ToString("s").Replace('T',' ') + ": Logging begun...");
                    var textWriterTraceListener = new System.Diagnostics.TextWriterTraceListener(streamWriter);
                    System.Diagnostics.Debug.Listeners.Add(textWriterTraceListener);
#endif*/
            try
            {
                try
                {
                    //var rand = new Random();
                    //Console.WriteLine(string.Concat("beta beta beta beta beta beta beta beta".Select(x => rand.Next() % 2 == 0 ? x : char.ToUpper(x))));
                    //Console.WriteLine(string.Concat("beta beta beta beta beta beta beta beta".Select(x => rand.Next() % 2 == 0 ? x : char.ToUpper(x))));
                    //Console.WriteLine(string.Concat("beta beta beta beta beta beta beta beta".Select(x => rand.Next() % 2 == 0 ? x : char.ToUpper(x))));
                    //Console.WriteLine(string.Concat("beta beta beta beta beta beta beta beta".Select(x => rand.Next() % 2 == 0 ? x : char.ToUpper(x))));
                    //Console.WriteLine(string.Concat("beta beta beta beta beta beta beta beta".Select(x => rand.Next() % 2 == 0 ? x : char.ToUpper(x))));
                    //Console.WriteLine(string.Concat("beta beta beta beta beta beta beta beta".Select(x => rand.Next() % 2 == 0 ? x : char.ToUpper(x))));
                    //Console.WriteLine(string.Concat("beta beta beta beta beta beta beta beta".Select(x => rand.Next() % 2 == 0 ? x : char.ToUpper(x))));
                    //Console.WriteLine(string.Concat("beta beta beta beta beta beta beta beta".Select(x => rand.Next() % 2 == 0 ? x : char.ToUpper(x))));
                    //Console.WriteLine(string.Concat("beta beta beta beta beta beta beta beta".Select(x => rand.Next() % 2 == 0 ? x : char.ToUpper(x))));
                    //Console.WriteLine(string.Concat("beta beta beta beta beta beta beta beta".Select(x => rand.Next() % 2 == 0 ? x : char.ToUpper(x))));

                    //string serverName = "mcaddmcps01";
                    //string vaultName = "CPSVault";
                    //string userName = "Administrator";
                    //string password = "";

                    string serverName = null;
                    string vaultName = null;
                    string userName = null;
                    string password = null;
                    string configuration = null;

                    bool useWindowsAuth = false;
                    bool silent = false;

                    bool encryptedPassword = false;

                    bool dontUpdateConfig = false;
                    string logFile;

                    string logName = null;
                    string logUser = null;

                    bool retry = false;
                    int retries = 5;
                    Option<TimeSpan> retryDelay = null;


                    string downloadTarget = null;

                    bool showHelpAndExit = false;
                    var p = new OptionSet
                    {
                        {
                            "vu=",
                            "the Vault {USERNAME}.",
                            v => userName = v
                        },
                        {
                            "vp:",
                            "the Vault {PASSWORD}.",
                            v => password = v
                        },
                        {
                            "sn=",
                            "the Vault {SERVER} Name.",
                            v => serverName = v
                        },
                        {
                            "n=",
                            "the name of the {VAULT}.",
                            v => vaultName = v
                        },
                        {
                            "wa",
                            "Use Windows Authentication",
                            v => { useWindowsAuth = true; /*= v != null; throw new NotImplementedException("Windows Authentication not implemented yet");*/ }
                        },
                        {
                            "silent",
                            "Suppress console messages.",
                            v => { silent = true;}
                        },
                        {
                            "retry:", 
                            "Retry on error, " +retries+ " {TIMES} by default.",
                            v =>
                                {
                                    retry = true;
                                    v.AsOption().Bind(s => s.OptionParseInt()).IfSomeDo(r => retries = r);
                                }
                        },
                        {
                            "retry-delay=", 
                            "Delay between each retry {DELAY}. Eg. 60s",
                            v =>
                                {
                                    retry = true;
                                    retryDelay = ParseTimeSpan(v);
                                }
                        },
                        {
                            "k|keep-config", 
                            "Keep current config file unchanged, not attempting to update it to a later version.",
                            v => dontUpdateConfig = v != null
                        },
                        {
                            "c|configuration=",
                            "Run command with configuration file.",
                            v => configuration = v
                        },
                        {
                            "l|log=",
                            "Log information to a {LOGFILE}",
                            v => { logFile = v; throw new NotImplementedException("Logging not implemented yet"); }
                        },
                        {
                            "lu=",
                            "Which user uses the log, system or user {LOGUSER}",
                            v => logUser = v
                        },
                        {
                            "ep",
                            "Enrypted Password.",
                            v => encryptedPassword = true 
                        },
                        {
                            "t|target=",
                            "Target {LOCALPATH} when downloading a specific file",
                            v => downloadTarget = v
                        },
                        {
                            "h|help", 
                            "show this message and exit",
                            v => showHelpAndExit = v != null
                        },
                    };

                    List<string> extra;
                    string path = null;
                    // parse arguments
                    try
                    {
                        extra = p.Parse(args);
                        password = password ?? "";

                        var url = extra.OptionSingle();
                        if (url.IsSome)
                            ParseVaultUrl(url.Get(), ref userName, ref password, ref serverName, ref vaultName, ref path);
                        else if (extra.Any())
                            showHelpAndExit = true;

                        if (path != null)
                            dontUpdateConfig = true;

                        if (new[] { serverName, vaultName, userName, password, }.Any(s => s == null) && String.IsNullOrWhiteSpace(configuration))
                            showHelpAndExit = true;
                    }
                    catch (Exception e)
                    {
                        if (!silent)
                        {
                            Console.Write("VaultEagleConsole: ");
                            Console.WriteLine(e.Message);
                            Console.WriteLine();
                        }
                        showHelpAndExit = true;
                    }

                    var connectionManager = Autodesk.DataManagement.Client.Framework.Vault.Library.ConnectionManager;
                    if (showHelpAndExit)
                    {
                        ShowHelp(p);
                        return 1;
                    }
                    IProgressWindow logger = new ConsoleProgressReporter();
                    if (!silent)
                        VaultEagleSynchronizer.LogVersion(logger, System.Reflection.Assembly.GetExecutingAssembly());

                    if (string.IsNullOrWhiteSpace(downloadTarget))
                    {
                        downloadTarget = null;
                    }
                    else
                    {
                        downloadTarget = System.IO.Path.GetFullPath(downloadTarget);
                    }

                    
                    Action execute;
                    if (!String.IsNullOrEmpty(configuration))
                    {
                       
                        ApplicationSettings settings = ApplicationSettings.Read();
                        List<Tuple<string, string>> filesToMove = new List<Tuple<string, string>>();

                        bool serverCheckResult = CheckServers(settings.ServerList);
                       
                        if (serverCheckResult && (!String.IsNullOrWhiteSpace(serverName) && !String.IsNullOrWhiteSpace(vaultName)) && ((!String.IsNullOrWhiteSpace(userName)) || (useWindowsAuth)))
                        {
                            execute = () =>
                            {
                                // create config folder
                                if (!System.IO.Directory.Exists(settings.configPath))
                                {
                                    try
                                    {
                                        System.IO.Directory.CreateDirectory(settings.configPath);
                                    }
                                    catch{  }
                                }

                                // create log file
                                Option<MCADCommon.LogCommon.DisposableFileLogger> fileLogger = Option.None;
                                logName = "VaultEagle" + logUser;
                                if (settings.LogLevel.IsSome)
                                {
                                    fileLogger = new MCADCommon.LogCommon.DisposableFileLogger(MCADCommon.LogCommon.DisposableFileLogger.CreateLogFilePath(settings.configPath, logName), settings.LogLevel.Get()).AsOption();
                                
                                    RemoveOldLogFiles(settings.configPath, logName, settings.SavedLogCount);
                                }

                                //  using (MCADCommon.LogCommon.DisposableFileLogger logger2 = new MCADCommon.LogCommon.DisposableFileLogger(MCADCommon.LogCommon.DisposableFileLogger.CreateLogFilePath(settings.configPath, logName), settings.LogLevel))
                                {
                                    fileLogger.IfSomeDo(fl => fl.Trace("Started Logging."));
                                    LogInResult result;

                                    fileLogger.IfSomeDo(fl => fl.Trace("Before login."));
                                    if (!String.IsNullOrWhiteSpace(userName))
                                    {
                                        // login by userName
                                        AuthenticationFlags authFlag;
                                        if (settings.ReadOnly)
                                            authFlag = AuthenticationFlags.ReadOnly;
                                        else
                                            authFlag = AuthenticationFlags.Standard;

                                        string pass = encryptedPassword ? MCADCommon.EncryptionCommon.SimpleEncryption.DecryptString(password, "Hades") : password;
                                        
                                        if (!silent)
                                            result = connectionManager.LogIn(serverName, vaultName, userName, pass, authFlag, (message, states) => { Console.WriteLine(message); return true; });
                                        else
                                            result = connectionManager.LogIn(serverName, vaultName, userName, pass, authFlag, null);
                                    }
                                    else // login by windows authentication
                                    {
                                        if (!silent)
                                            result = connectionManager.LogIn(serverName, vaultName, null, null, AuthenticationFlags.WindowsAuthentication, (message, states) => { Console.WriteLine(message); return true; });
                                        else
                                            result = connectionManager.LogIn(serverName, vaultName, null, null, AuthenticationFlags.WindowsAuthentication, null);
                                    }

                                    fileLogger.IfSomeDo(fl => fl.Trace("After loggin."));
                                    if (result.Success)
                                        fileLogger.IfSomeDo(fl => fl.Trace("Login successful."));
                                    else
                                    {
                                        foreach (KeyValuePair<LogInResult.LogInErrors, string> error in result.ErrorMessages)
                                            fileLogger.IfSomeDo(fl => fl.Trace("Login message: " + error.Value + "."));
                                    }

                                    var connection = result.Connection;
                                    List<string> logErrorList = new List<string>();
                                    try
                                    {
                                        Option<string> mailSender = Option.None;
                                        VaultEagleSynchronizer synchronizer = new VaultEagleSynchronizer(connection, new SynchronizationTree());
                                        filesToMove.AddRange(synchronizer.Synchronize(settings.configPath, settings.NetworkRetries, configuration, settings.UseNotifications, fileLogger, ref mailSender));
                                        if (mailSender.IsSome)
                                            settings.MailInfo.Get().SetSender(mailSender.Get());
                                        logErrorList = synchronizer.errors;
                                    }
                                    finally
                                    {
                                        /* Process current = Process.GetCurrentProcess();
                                         ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + current.Id);
                                         ManagementObjectCollection moc = searcher.Get();
                                         foreach (ManagementObject mo in moc)
                                         {
                                             try
                                             {
                                                 Process child = Process.GetProcessById(Convert.ToInt32(mo["ProcessId"]));
                                                 child.Kill();
                                             }
                                             catch { }
                                         }*/

                                        if (connection != null)
                                            connectionManager.LogOut(connection);
                                        /*   connectionManager.*/
                                        // connectionManager.
                                        foreach (Tuple<string, string> fileToMove in filesToMove)
                                        {
                                            try
                                            {
                                                 if (new FileInfo(fileToMove.Item2).Exists)
                                                 {
                                                     VaultEagle.VaultUtils.HandleNetworkErrors(() => { System.IO.File.SetAttributes(fileToMove.Item2, System.IO.FileAttributes.Normal); }, retries);
                                                     if (new FileInfo(fileToMove.Item2 + ".old").Exists)
                                                     {
                                                         VaultEagle.VaultUtils.HandleNetworkErrors(() => { System.IO.File.SetAttributes(fileToMove.Item2 + ".old", FileAttributes.Normal); }, retries);
                                                         VaultEagle.VaultUtils.HandleNetworkErrors(() => { System.IO.File.Delete(fileToMove.Item2 + ".old"); }, retries);
                                                     }
                                                     VaultEagle.VaultUtils.HandleNetworkErrors(() => { System.IO.File.Move(fileToMove.Item2, fileToMove.Item2 + ".old"); }, retries);
                                                     VaultEagle.VaultUtils.HandleNetworkErrors(() => { System.IO.File.SetAttributes(fileToMove.Item2 + ".old", FileAttributes.Normal); }, retries);
                                                 }
                                                 Directory.CreateDirectory(new FileInfo(fileToMove.Item2).DirectoryName);
                                                 VaultEagle.VaultUtils.HandleNetworkErrors(() => { System.IO.File.Move(fileToMove.Item1, fileToMove.Item2); }, retries);
                                                 if (new FileInfo(fileToMove.Item2 + ".old").Exists)
                                                 {
                                                     VaultEagle.VaultUtils.HandleNetworkErrors(() => { System.IO.File.SetAttributes(fileToMove.Item2 + ".old", FileAttributes.Normal); }, retries);
                                                     VaultEagle.VaultUtils.HandleNetworkErrors(() => { System.IO.File.Delete(fileToMove.Item2 + ".old"); }, retries);
                                                 }
                                            }
                                            catch (Exception ex) { fileLogger.IfSomeDo(fl => fl.Error(ex.Message)); }
                                        }
                                    }

                                    //Checking if the log file should be sent.
                                    if (!settings.MailLog.EqualsIgnoreCase("Never"))
                                    {
                                        string mailTitle = "Vault Eagle Synchronization";
                                        string mailBody = "";

                                        if (settings.MailLog.EqualsIgnoreCase("Error") && fileLogger.IsSome && fileLogger.Get().ErrorEncountered)
                                        {
                                            if (settings.MailInfo.IsSome)
                                            {
                                                mailTitle += " Failed";

                                                if (logErrorList.Count > 0)
                                                    mailBody = logErrorList.StringJoin("\r\n");
                                                else
                                                    mailBody = Environment.ExpandEnvironmentVariables("%computername%");
                                                
                                                //To avoid an empty body in the mail, if the computername variable does not exist or is empty.
                                                if (mailBody == string.Empty)
                                                    mailBody = "Vault Eagle";

                                                MCADCommon.MailCommon.Mailer mailer = new MCADCommon.MailCommon.Mailer(settings.MailInfo.Get());

                                                mailer.Mail(mailTitle, mailBody, new List<string> { fileLogger.Get().getLogFilePath() });
                                            }
                                        }


                                        else if (settings.MailLog.EqualsIgnoreCase("Always") && fileLogger.IsSome)
                                        {
                                            if (settings.MailInfo.IsSome)
                                            {
                                                mailTitle += " Successful";
                                                mailBody = Environment.ExpandEnvironmentVariables("%computername%");
                                                //To avoid an empty body in the mail, if the computername variable does not exist or is empty.
                                                if (mailBody == string.Empty)
                                                    mailBody = "Vault Eagle";

                                                MCADCommon.MailCommon.Mailer mailer = new MCADCommon.MailCommon.Mailer(settings.MailInfo.Get());
                                                mailer.Mail(mailTitle, mailBody, new List<string> { fileLogger.Get().getLogFilePath() });
                                             }
                                         }       
                                    }
                                }
                                if (fileLogger.IsSome)
                                    fileLogger.Get().Dispose();
                             };      
                        }
                                
                        else
                        {
                            return 1;
                        }  
                    }
                    
                    else
                    {
                        execute = () =>
                        {
                            if (!silent)
                            {
                                Console.WriteLine();
                                Console.WriteLine("Logging in...");
                            }
                            
                            var result = connectionManager.LogIn(serverName, vaultName, userName, password, AuthenticationFlags.Standard, (message, states) => { Console.WriteLine(message); return true; });
                            var connection = result.Connection;
                            try
                            {
                                if (!result.Success)
                                {
                                    var msg = string.Join("\r\n", result.ErrorMessages.Select(
                                        errorMessage =>
                                        string.Format("({0}) {1}", errorMessage.Key, errorMessage.Value)));
                                    if (result.ErrorMessages.Any(x => x.Key == LogInResult.LogInErrors.AuthenticationError))
                                        throw new SimpleException<DoNotRetry>(msg, new ErrorMessageException(msg));
                                    throw new ErrorMessageException(msg);
                                }

                                if (!silent)
                                    Console.WriteLine();

                                SynchronizationTree tree = null;
                                if (path != null)
                                {
                                    path = path.Replace('\\', '/');
                                    if (!path.EndsWith("/") || path == "$")
                                    {
                                        var vaultFolder = connection.WebServiceManager.DocumentService.FindFoldersByPaths(new[] { path }).OptionSingle().Where(x => x.Id != -1);
                                        if (vaultFolder.IsSome)
                                            path = path + '/';
                                    }

                                    bool isVaultFolder = path.EndsWith("/") || path == "$";
                                    if (!isVaultFolder)
                                    {
                                        var vaultFile = connection.WebServiceManager.DocumentService.FindLatestFilesByPaths(new[] { path }).OptionSingle().Where(x => x.Id != -1);
                                        string msg = string.Format("{0} not found in Vault {1}!", path, connection.Vault);
                                        if (vaultFile.IsNone)
                                            throw new SimpleException<DoNotRetry>(msg, new ErrorMessageException(msg));
                                    }
                                    if (downloadTarget != null && !isVaultFolder && (System.IO.Directory.Exists(downloadTarget) || downloadTarget.EndsWith("" + System.IO.Path.DirectorySeparatorChar)))
                                        downloadTarget = System.IO.Path.Combine(downloadTarget, System.IO.Path.GetFileName(path));

                                    tree = new SynchronizationTree(connection.Vault, connection.Server);
                                    tree.SetSyncInfo(path, new SynchronizationTree.SyncInfo { State = SyncState.Include, LocalPath = downloadTarget });
                                }
                                if (!silent && path != null)
                                    Console.WriteLine("Download Mode: Downloading " + path + (downloadTarget == null ? "" : " to " + downloadTarget) + "...");

                                var synchronizer = tree == null ? new VaultEagleSynchronizer(connection) : new VaultEagleSynchronizer(connection, tree);
                                synchronizer.logWindow = logger;
                                synchronizer.sysTray = new DummySysTrayNotifyIconService();
                                synchronizer.dontUpdateConfig = dontUpdateConfig;
                                synchronizer.Synchronize();

                                string errorMsg = string.Join("\r\n", synchronizer.errors.Prepend("Errors occured during download"));
                                if (synchronizer.failedDownloads.Any())
                                    throw new ErrorMessageException(errorMsg);
                                if (synchronizer.errors.Any())
                                    throw new SimpleException<DoNotRetry>(errorMsg, new ErrorMessageException(errorMsg));
                            }
                            finally
                            {
                                if (connection != null)
                                    connectionManager.LogOut(connection);
                            }
                        };
                    }
                    DoWithRetry(logger, execute, retry ? retries : 0, retryDelay.ToNullable());

                    return 0;
                }
                catch (SimpleException<DoNotRetry> ex)
                {
                    if (ex.InnerException != null)
                        throw ex.InnerException;
                    throw new ErrorMessageException(ex.Message);
                }
            }
            catch (ErrorMessageException ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Unhandled exception " + VaultServerException.WrapException(ex).ToString());
                return 1;
            }
/*#if DEBUG
                }
#endif*/
        }

        private static void RemoveOldLogFiles(string path, string logFileName, int allowedOldLogs)
        {
            List<FileInfo> oldLogFiles = new List<FileInfo>();
            foreach (FileInfo fileInLogPath in new DirectoryInfo(path).GetFiles())
                if (fileInLogPath.Name.StartsWith(logFileName))
                    oldLogFiles.Add(fileInLogPath);

            oldLogFiles.OrderBy(fileInfo => ParseLogTime(fileInfo.Name, logFileName,"yyyy-MM-dd HH-mm-ss"));

            while (oldLogFiles.Count >= allowedOldLogs)
            {
                FileInfo oldestLogFile = oldLogFiles.First();
                oldLogFiles.Remove(oldestLogFile);
                using (FileAttributeHandler attributeHandler = new FileAttributeHandler(oldestLogFile.FullName))
                    System.IO.File.Delete(oldestLogFile.FullName);
            }
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

        private static DateTime ParseLogTime(string logFileName, string logName,string parseString)
        {
            string dateTimeText = logFileName.Substring(logName.Length + 1, 19);
            return DateTime.ParseExact(dateTimeText, parseString, new CultureInfo("en-US"));
        }
        private static Option<TimeSpan> ParseTimeSpan(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return Option.None;
            if(System.Text.RegularExpressions.Regex.IsMatch(s, @"^[0-9]+\s*s?$"))
                return s.TrimStringAtEnd("s").Trim().OptionParseDouble().Transform(TimeSpan.FromSeconds);
            if(System.Text.RegularExpressions.Regex.IsMatch(s, @"^[0-9]+\s*(m|min)$"))
                return s.TrimStringAtEnd("min").TrimStringAtEnd("m").Trim().OptionParseDouble().Transform(TimeSpan.FromMinutes);
            if(System.Text.RegularExpressions.Regex.IsMatch(s, @"^[0-9]+\s*h$"))
                return s.TrimStringAtEnd("h").Trim().OptionParseDouble().Transform(TimeSpan.FromHours);
            return Option.None;
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: VaultEagleConsole [OPTIONS]+ USER:PASS@SERVER/VAULT[/$/VAULTPATH/]");
            Console.WriteLine("Vault Eagle updates subscribed files from Vault. It checks if files are up to date");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }
        
        private static void ParseVaultUrl(string vaultUrl, ref string user, ref string pass, ref string server, ref string vault, ref string path)
        {
            try
            {
                
                if (!string.IsNullOrEmpty(vaultUrl))
                {
                    Uri sourceUri;
                    if (vaultUrl.Contains("://"))
                        sourceUri = new Uri(vaultUrl);
                    else
                        sourceUri = new Uri("http://" + vaultUrl);

                    if (!string.IsNullOrEmpty(sourceUri.UserInfo))
                    {
                        string userInfo = sourceUri.UserInfo;
                        if (userInfo.Contains(":"))
                        {
                            var split = userInfo.Split(new[] { ':' }, 2, StringSplitOptions.None);
                            SetIfNotNullOrEmpty(ref user, split[0]);
                            pass = split[1]; // can be String.Empty
                        }
                        else
                            SetIfNotNullOrEmpty(ref user, userInfo);
                    }

                    SetIfNotNullOrEmpty(ref server, sourceUri.Host);

                    var absolutePath = sourceUri.AbsolutePath;
                    // "/Vault2/$/Designs" => {"", "Vault2", "$/Designs"}
                    var split2 = absolutePath.Split(new[] { '/' }, 3, StringSplitOptions.None);
                    if (split2.Length > 1)
                        SetIfNotNullOrEmpty(ref vault, split2[1]);
                    if (split2.Length > 2)
                        SetIfNotNullOrEmpty(ref path, split2[2]);
                }
            }
            catch (UriFormatException)
            {
                //Print("Couldn't parse: " + vaultUrl, "Error");
                //PrintUsageAndExit();
                throw new Exception("Couldn't parse: " + vaultUrl);
            }
        }

        private static void SetIfNotNullOrEmpty(ref string target, string s)
        {
            if (!string.IsNullOrEmpty(s))
                target = Uri.UnescapeDataString(s);
        }

        public class DoNotRetry { }

        private static void DoWithRetry(IProgressWindow logger, Action action, int numberOfRetries, TimeSpan? retryDelay)
        {
            var retryDelayInSecondsSchedule =
                retryDelay.HasValue
                    ? new[] {retryDelay.Value}.Cycle()
                    : new[]
                        {
                            10,
                            30,
                            60,
                            5*60,
                            10*60,
                            15*60,
                            60*60,
                            2*60*60,
                        }.Concat(new[] {2*60*60}.Cycle())
                         .Select(x => TimeSpan.FromSeconds(x));

            foreach (var delay in retryDelayInSecondsSchedule.Take(numberOfRetries))
            {
                try
                {
                    action();
                    return;
                }
                catch (SimpleException<DoNotRetry> ex)
                {
                    if(ex.InnerException != null)
                        throw ex.InnerException;
                    throw new ErrorMessageException(ex.Message);
                }
                catch (ErrorMessageException ex)
                {
                    logger.Log("Error: " + ex.Message);
                    logger.Log("Waiting to retry... (" + delay.ToPrettyFormat() + ")");
                    System.Threading.Thread.Sleep(delay);
                }
                catch (Exception ex)
                {
                    logger.Log(VaultServerException.WrapException(ex).ToString());
                    logger.Log("Waiting to retry... (" + delay.ToPrettyFormat() + ")");
                    System.Threading.Thread.Sleep(delay);
                }
            }
            action();
        }
    }
}
