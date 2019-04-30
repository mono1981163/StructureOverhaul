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
using static VaultEagleConsole.ServerUtils;
using VaultEagleLib;

namespace VaultEagleConsole
{
    
    class Program
    {
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
                                    retryDelay = FileUtils.ParseTimeSpan(v);
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
                            ServerUtils.ParseVaultUrl(url.Get(), ref userName, ref password, ref serverName, ref vaultName, ref path);
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
                        FileUtils.ShowHelp(p);
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

                        bool serverCheckResult = ServerUtils.CheckServers(settings.ServerList);
                       
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
                                
                                    FileUtils.RemoveOldLogFiles(settings.configPath, logName, settings.SavedLogCount);
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
                                    // synchronize with server
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
                    ServerUtils.DoWithRetry(logger, execute, retry ? retries : 0, retryDelay.ToNullable());

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
        
    }
}
