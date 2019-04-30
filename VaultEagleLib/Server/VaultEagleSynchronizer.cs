using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Windows.Forms;
using System.Drawing;
using Autodesk.Connectivity.WebServicesTools;
using Autodesk.Connectivity.Explorer.ExtensibilityTools;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Connections;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties;

using ADSK = Autodesk.Connectivity.WebServices;
using VDF = Autodesk.DataManagement.Client.Framework;
using Autodesk.Connectivity.WebServices;

using Common.DotNet.Extensions;
using System.Text.RegularExpressions;
using MCADCommon.MailCommon;

//using Windows.UI.Notifications;
//using Microsoft.Toolkit.Uwp.Notifications;
//using Microsoft.QueryStringDotNET;

namespace VaultEagle
{
    public interface IProgressWindow
    {
        void Log(string text, string detailed = null);
        void LogDone(Boolean failed = false);

        void LogWithProgress(string text, int progress);
        void Show();
    }

    public interface ISysTrayNotifyIconService
    {
        void ShowIfSlow(string s);

        void ShowNow(string s, bool ignoreMinimumDisplayTime = false);

        void Start();
    }

    [Serializable]
    public class StopThreadException : Exception
    {
        public StopThreadException() : base("Synchronization interrupted.") { }
    }

    public class StopThreadSwitch
    {
        // boolean read/write is atomic
        // (must lock though if more data is passed between
        // threads, and a memory barrier needed)
        public bool ShouldStop = false;
    }

    public class VaultEagleSynchronizer
    {
        public Connection connection;

        public bool dontUpdateConfig;

        public List<string> errors = new List<string>();

        public List<string> failedDownloads = new List<string>();

        //   private ToastNotificationManager
        public IProgressWindow logWindow;

        public StopThreadSwitch StopThread = new StopThreadSwitch();

        public ISysTrayNotifyIconService sysTray;

        private SynchronizationTree syncTree;

        public VaultEagleSynchronizer(Connection connection)
            : this(connection, SynchronizationTree.ReadTree(connection.Vault, connection.Server))
        {
        }

        public VaultEagleSynchronizer(Connection connection, SynchronizationTree syncTree)
        {
            this.connection = connection;
            this.syncTree = syncTree;
        }

        public static void GetVersion(System.Reflection.Assembly executingAssembly, out string company, out string product, out Version version)
        {
            company = executingAssembly.GetAssemblyAttribute<System.Reflection.AssemblyCompanyAttribute>().Company;
            product = executingAssembly.GetAssemblyAttribute<System.Reflection.AssemblyProductAttribute>().Product;
            version = executingAssembly.GetName().Version;
        }
        public static void LogVersion(IProgressWindow logger, System.Reflection.Assembly executingAssembly)
        {
            string company, product;
            Version version;
            GetVersion(executingAssembly, out company, out product, out version);

            var shortVer = string.Format("{0} {1}.{2} running...", product, version.Major, version.Minor);
            var longVer = string.Format("{0} {1} running...", product, version.ToString());
            logger.Log(shortVer, longVer);
        }

        public IEnumerable<VaultEagleLib.Model.DataStructures.DownloadItem/*FileFolder*/> GetBatch(int pageNumber, List<VaultEagleLib.Model.DataStructures.DownloadItem> files, int batchSize)
        {
            return files.Skip(pageNumber * batchSize).Take(batchSize);
        }

        public void StopCheck()
        {
            if (StopThread.ShouldStop)
                throw new StopThreadException();
        }
        public void Synchronize()
        {
#if DEBUG
            var watch = System.Diagnostics.Stopwatch.StartNew();
#endif
            System.Diagnostics.Debug.WriteLine("SynchronizationThread.Run()");
            StopCheck();

            if (syncTree.IsEmpty())
            {
                logWindow.Log("Not subscribed to any files, nothing to do.");
                logWindow.LogDone();
                return;
            }

            using (VaultCommunication vaultCom = new VaultCommunication()
            {
                connection = connection,
                manager = connection.WebServiceManager,
                UserId = connection.UserID,
                cache = new FunctionJsonCache(),
                Log = s => logWindow.Log(s),
                stopThread = StopThread
            })
            {
                vaultCom.ResetWorkingFoldersIfNecessary();

                StopCheck();
                logWindow.Log("Getting list of files from Vault...");
                sysTray.Start();
                sysTray.ShowIfSlow("Getting info from Vault...           ");
                StopCheck();
                List<string> missingFiles = new List<string>(), missingFolders = new List<string>();
                var foldersToCreate = new List<Folder>();
                var files = new List<FileFolder>();
                Action getFilesToSynchronize = () =>
                    files = vaultCom.GetFilesToSynchronize(syncTree, missingFolders: missingFolders, missingFiles: missingFiles, watchedFolders: foldersToCreate);
                getFilesToSynchronize();
                StopCheck();
                if (missingFolders.Any() || missingFiles.Any())
                {
                    logWindow.Log("Missing folder/files!");
                    if (vaultCom.TryRepairSynchronizationTree(syncTree))
                    {
                        getFilesToSynchronize();
                        logWindow.Log("Getting list of files from Vault again...");
                    }
                }

                //foreach (var fileFolder in files)
                //{
                //    Console.WriteLine(fileFolder.Folder.FullName + "/" + fileFolder.File.VerName);
                //}

#if DEBUG
                logWindow.Log("Time elapsed " + watch.ElapsedMilliseconds + "ms");
#endif

                logWindow.Log("");
                logWindow.Log("Checking if files are up to date (" + files.Count + " files)...");
                sysTray.ShowIfSlow("Getting info from Vault (watching " + files.Count + " files)...");

                StopCheck();
                var fileStatuses = files
                    .Select(filefolder =>
                        {
                            StopCheck();
                            var localPath = syncTree.GetLocalPathOfPath(VaultCommunication.GeneratePathFromFolderAndFile(filefolder));
                            Dbg.Trace("before vaultCom.QuickLocalFileStatus(filefolder.File, ...");
                            return new
                                {
                                    localPath,
                                    filefolder,
                                    status = localPath.IsSome
                                                ? vaultCom.QuickLocalFileStatus(filefolder.File, localPath.Get())
                                                : vaultCom.QuickLocalFileStatus(filefolder.File, filefolder.Folder),
                                };
                        })
                    .ToList();
                StopCheck();
                Dbg.Trace("after var fileStatuses = ...");

                //logWindow.Log(string.Join("\r\n",fileStatuses.Select(x => x.file.VerName + ": " + x.status.ToString()).ToArray()));

                var partition = fileStatuses.PartitionBy(x => x.status.VersionState == EntityStatus.VersionStateEnum.Unknown);
                var unknownFiles = partition[true];
                fileStatuses = partition[false];

                partition = fileStatuses.PartitionBy(x => x.status.CheckoutState == EntityStatus.CheckoutStateEnum.CheckedOutByCurrentUser);
                var checkedOutByCurrentUserFiles = partition[true];
                fileStatuses = partition[false];

                logWindow.Log(checkedOutByCurrentUserFiles.Count + " files checked out by current user (skipped)");

                partition = fileStatuses.PartitionBy(x => x.status.VersionState == EntityStatus.VersionStateEnum.MatchesLatestVaultVersion && x.status.LocalEditsState == EntityStatus.LocalEditsStateEnum.DoesNotHaveLocalEdits);
                var filesUpToDate = partition[true];
                fileStatuses = partition[false];

                logWindow.Log(filesUpToDate.Count + " files up to date");

                if (!syncTree.Config.OverwriteLocallyModifiedFiles)
                {
                    partition = fileStatuses.PartitionBy(x => x.status.LocalEditsState == EntityStatus.LocalEditsStateEnum.HasLocalEdits);
                    var locallyModified = partition[true];
                    fileStatuses = partition[false];

                    logWindow.Log(locallyModified.Count + " files locally modified:");

                    foreach (var item in locallyModified)
                        logWindow.Log("  " + item.filefolder.File.VerName);
                }

                var filesToUpdate = fileStatuses.ToList();

                logWindow.Log("");
                Dbg.Trace("before if (filesToUpdate.Count != 0)");
                int numberOfErrors = missingFiles.Count + missingFolders.Count + unknownFiles.Count;
                if (filesToUpdate.Count != 0)
                {
                    logWindow.Log("Downloading " + filesToUpdate.Count + " files...");
                    sysTray.ShowNow("Getting info from Vault (watching " + files.Count + " files)...\r\n" + filesToUpdate.Count + " files out of date. Downloading (1/" + filesToUpdate.Count + ")...");
                    logWindow.LogWithProgress(null, 0);
                    int count = 0;
                    var failedDownloadsQueue = new List<string>();
                    Dbg.Trace("before vaultCom.DownloadFiles(...");
                    vaultCom.DownloadFiles(filesToUpdate.Select(x => Tuple.Create(x.filefolder, x.localPath)).ToArray(), failedDownloadsQueue, afterDownload: () =>
                    {
                        Dbg.Trace("vaultCom.DownloadFiles, afterDownload");
                        count++;
                        sysTray.ShowIfSlow("Getting info from Vault (watching " + files.Count + " files)...\r\n" + filesToUpdate.Count + " files out of date. Downloading (" + count + "/" + filesToUpdate.Count + ")...");
                        int progress = (int)Math.Round(100.0 * count / filesToUpdate.Count);
                        logWindow.LogWithProgress(null, progress);
                        StopCheck();
                    });
                    failedDownloads = failedDownloadsQueue.ToList();
                    numberOfErrors += failedDownloads.Count;
                    sysTray.ShowIfSlow("Getting info from Vault (watching " + files.Count + " files)...\r\n" + filesToUpdate.Count + " files out of date. Downloaded " + (filesToUpdate.Count - failedDownloads.Count) + " files, checking folders...");
                }

                logWindow.Log("Ensuring folder structure up to date...");
                try
                {
                    foreach (var folder in foldersToCreate)
                    {
                        StopCheck();
                        string localPath = syncTree.GetLocalPathOfPath(folder.FullName + '/').Else(() => vaultCom.GetLocalPathFromFolder(folder));
                        if (!System.IO.Directory.Exists(localPath))
                        {
                            System.IO.Directory.CreateDirectory(localPath);
                            logWindow.Log("  created " + folder.FullName + " locally");
                        }
                    }
                }
                catch (StopThreadException)
                {
                    throw;
                }
                catch { }

                if (!dontUpdateConfig)
                {
                    logWindow.Log("Updating config file...");
                    vaultCom.UpdateLastVaultId(syncTree);
                }
                StopCheck();

                if (filesToUpdate.Count == 0)
                {
                    logWindow.Log("");
                    logWindow.Log("No file to download");

                    if (numberOfErrors == 0)
                        sysTray.ShowNow("Getting info from Vault (watching " + files.Count + " files)...\r\nAll up to date!", ignoreMinimumDisplayTime: true);
                    else
                        sysTray.ShowNow("Getting info from Vault (watching " + files.Count + " files)...\r\nNothing to download.\r\nErrors occurred.", ignoreMinimumDisplayTime: true);
                }
                else
                {
                    sysTray.ShowNow("Getting info from Vault (watching " + files.Count + " files)...\r\n" + filesToUpdate.Count + " files out of date. Downloaded " + (filesToUpdate.Count - failedDownloads.Count) + " files!" + (numberOfErrors == 0 ? "" : "\r\nErrors occurred."), ignoreMinimumDisplayTime: true);
                }

                logWindow.Log("Done.");
                logWindow.LogDone();
#if DEBUG
                            logWindow.Log("Time elapsed " + watch.ElapsedMilliseconds + "ms");
#endif

                if (numberOfErrors > 0)
                {
                    logWindow.Log("");
                    logWindow.Log("Errors:");
                    logWindow.Show();
                }

                // errors
                foreach (var item in missingFiles)
                    errors.Add("ERROR: Couldn't find file in Vault: " + item);
                foreach (var item in missingFolders)
                    errors.Add("ERROR: Couldn't find folder in Vault: " + item);
                foreach (var item in unknownFiles)
                    errors.Add("ERROR: Couldn't get status for " + item.filefolder.File.VerName);
                foreach (var item in failedDownloads)
                    errors.Add("ERROR: Couldn't download " + item);

                foreach (var error in errors)
                    logWindow.Log(error);
            }
        }
        /**********************************************************************************************************************/
        public List<Tuple<string, string>> Synchronize(string configPath, int networkRetries, string synchronizerConfigurationPath, bool useNotification, Option<MCADCommon.LogCommon.DisposableFileLogger> logger, ref Option<string> mailFrom)
        {
            bool disposeLog = false;
            List<Tuple<string, string>> filesToMove = new List<Tuple<string, string>>();
            try
            {
                
                NotifyIcon trayIcon = new NotifyIcon();
                trayIcon.Text = "Fetching files to synchronize.";
                trayIcon.Icon = VaultEagleLib.Properties.Resources.Eagle;
                trayIcon.BalloonTipText = "Fetching files to synchronize.";

                ContextMenu trayMenu = new ContextMenu();

                trayIcon.ContextMenu = trayMenu;
                trayIcon.Visible = true;
                if (useNotification)
                    trayIcon.ShowBalloonTip(4000);
                errors = new List<string>();
                // Fetch configuration file, mail on failure.
                Option<ADSK.File> configurationFile = Option.None;
                string path;
                logger.IfSomeDo(l => l.Trace("Before downloading configuration."));
                if (synchronizerConfigurationPath.StartsWith("$"))
                {
                    configurationFile = DownloadConfiguration(configPath, networkRetries, synchronizerConfigurationPath, logger, errors);
                    if (configurationFile.IsNone)
                        return filesToMove;
                    path = Path.Combine(configPath, configurationFile.Get().Name);
                }
                else
                    path = Environment.ExpandEnvironmentVariables(synchronizerConfigurationPath);
                logger.IfSomeDo(l => l.Trace("After downloading configuration."));


                VaultEagleLib.SynchronizerSettings configuration = VaultEagleLib.SynchronizerSettings.Read(networkRetries,logger,path);
                if (logger.IsNone && configuration.LogFile.IsSome)
                {
                    logger = new MCADCommon.LogCommon.DisposableFileLogger(MCADCommon.LogCommon.DisposableFileLogger.CreateLogFilePath(configuration.configPath, configuration.LogFile.Get()), configuration.LogLevel).AsOption();
                    disposeLog = true;
                }

                //   Option<Logg
                //  if (configuration.LogFile.IsSome)
                logger.IfSomeDo(l => l.Info("Synchronizing vault files."));

                // Fetch files to sync, mail on failure.
                //  List<FileFolder> filesAndFoldersToUpdate = /*new List<FileFolder>();*/FetchingFilesFromVaultToSynchronize(ref mailInfo, lastSyncTime, logger, errors, configuration);
                if (configuration.OverrideMailSender.IsSome)
                    mailFrom = configuration.OverrideMailSender;
                /*List<Tuple<ADSK.File, string, bool, bool>>*/List<VaultEagleLib.Model.DataStructures.DownloadItem> filesToUpdate = FindFilesChangedSinceLastSync(connection, configuration.UseLastSync,configuration.LastSyncTime, configuration.VaultRoot, networkRetries, trayIcon,configuration.Items.ToArray(), logger);

                foreach (VaultEagleLib.SynchronizationItem item in configuration.Items)
                {
                    foreach (Tuple<string, string> tempFileAndFinalPath in item.TempFilesAndFinalPath)
                        filesToMove.Add(tempFileAndFinalPath);
                }
                
                string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                logger.IfSomeDo(l => l.Info("Synchronizing: " + filesToUpdate.Count + " files."));
                //  if (filesToUpdate.Count > 0)
                {
                    trayIcon.Text = "Downloaded 0 of " + filesToUpdate.Count + " files.";
                    
                    try
                    {
                            
                        errors.AddRange(SynchronizeFiles(networkRetries, logger, errors, configuration, filesToUpdate, trayIcon));
                        List<VaultEagleLib.Model.DataStructures.DownloadFile> downloadedFiles = filesToUpdate.OfType<VaultEagleLib.Model.DataStructures.DownloadFile>().ToList();
                        foreach (string getAndRun in configuration.GetAndRun)
                        {
                            // string test1 = filesToUpdate[filesToUpdate.Count - 1].Item2.Replace(configuration.VaultRoot, "$").Replace("\\", "/") + "/" + filesToUpdate[filesToUpdate.Count - 1].Item1.Name;
                            if (downloadedFiles.Any(f => getAndRun.Equals(f.VaultFileName(configuration.VaultRoot))))
                            {
                                VaultEagleLib.Model.DataStructures.DownloadFile file = downloadedFiles.Where(f => getAndRun.Equals(f.VaultFileName(configuration.VaultRoot))).First();
                                VaultEagle.VaultUtils.HandleNetworkErrors(() =>
                                {
                                    System.Diagnostics.Process proc = new System.Diagnostics.Process();
                                    proc.StartInfo.FileName = file.LocalFileName;
                                    proc.Start();
                                    proc.WaitForExit();
                                }, networkRetries);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.IfSomeDo(l => l.Error(ex.Message));
                        errors.Add("Failure synchronizing files.");
                    }

                    if (errors.Count == 0 && configuration.LastSyncFile.IsSome)
                    {
                        if (!VaultUtils.HandleNetworkErrors(() => System.IO.Directory.Exists(configuration.configPath), networkRetries))
                            VaultUtils.HandleNetworkErrors(() => System.IO.Directory.CreateDirectory(configuration.configPath), networkRetries);

                        if (VaultUtils.HandleNetworkErrors(() => System.IO.File.Exists(Path.Combine(configuration.configPath, configuration.LastSyncFile.Get()+".txt")), networkRetries))
                            VaultUtils.HandleNetworkErrors(() => System.IO.File.Delete(Path.Combine(configuration.configPath, configuration.LastSyncFile.Get()+".txt")), networkRetries);

                        try
                        {
                            using (TextWriter tw = new StreamWriter(VaultUtils.HandleNetworkErrors(() => System.IO.File.Create(Path.Combine(configuration.configPath, configuration.LastSyncFile.Get()+".txt")), networkRetries)))
                                tw.WriteLine(now);
                        }
                        catch { }
                    }
                    if (useNotification)
                    {
                        if (errors.Count > 0)
                        {
                            trayIcon.BalloonTipText = "Errors synchronizing files.";
                            if (useNotification)
                                trayIcon.ShowBalloonTip(2000);
                        }
                        else
                        {
                            trayIcon.BalloonTipText = "Finished synchronizing files.";
                            if (useNotification)
                                trayIcon.ShowBalloonTip(2000);
                        }
                    }
                }
                trayIcon.DisposeUnlessNull(ref trayIcon);
            }
            catch (Exception ex) 
            {
                logger.IfSomeDo(l => l.Error(ex.Message));
            }
            logger.IfSomeDo(l => l.Trace("Synchronizing complete."));
            if (disposeLog && logger.IsSome)
                logger.Get().Dispose();


            return filesToMove;
        }
        private Option<ADSK.File> DownloadConfiguration(string configPath, int networkRetries, string synchronizerConfigurationPath, Option<MCADCommon.LogCommon.DisposableFileLogger> logger, List<string> errors)
        {
            ADSK.File[] configurationFiles = connection.WebServiceManager.DocumentService.FindLatestFilesByPaths(new String[] { synchronizerConfigurationPath });
            Option<ADSK.File> configurationFile = Option.None;
            if (configurationFiles.Count() > 0)
            {
                VaultCommunication.DownloadFilesWithChecksum(connection, new List<VaultEagleLib.Model.DataStructures.DownloadFile>() { new VaultEagleLib.Model.DataStructures.DownloadFile(configurationFiles[0], configPath, false, false) }, logger, networkRetries);/*MCADCommon.VaultCommon.FileOperations.DownloadFile(connection, configurationFiles[0], path: configPath);*/
                if (VaultUtils.HandleNetworkErrors(() => !System.IO.File.Exists(Path.Combine(configPath, configurationFiles[0].Name)), networkRetries))
                {
                    logger.IfSomeDo(l => l.Error("Failed to download configuration file."));
                    errors.Add("Failed to download configuration file.");
                    //if (mailInfo.IsSome)
                    //{
                    //    MCADCommon.MailCommon.Mailer mailer = new MCADCommon.MailCommon.Mailer(mailInfo.Get());
                    //    mailer.Mail("Vault synchronizer failed", errors.StringJoin("\n\r"), new List<string>());
                    //}
                }
                configurationFile = configurationFiles[0].AsOption();

            }
            else
            {
                logger.IfSomeDo(l => l.Error("Could not find configuration file."));
                errors.Add("Could not find configuration file.");
                //if (mailInfo.IsSome)
                //{
                //    MCADCommon.MailCommon.Mailer mailer = new MCADCommon.MailCommon.Mailer(mailInfo.Get());
                //    mailer.Mail("Vault synchronizer failed", errors.StringJoin("\n\r"), new List<string>());
                //}
            }
            return configurationFile;
        }

        private List</*Tuple<ADSK.File, string, bool, bool>*/VaultEagleLib.Model.DataStructures.DownloadItem> FindFilesChangedSinceLastSync(VDF.Vault.Currency.Connections.Connection connection, bool useLastSyncTime, DateTime lastSyncTime, string vaultRoot, int retries, NotifyIcon trayIcon, VaultEagleLib.SynchronizationItem[] items, Option<MCADCommon.LogCommon.DisposableFileLogger> logger)
        {
            string lastSyncTag = lastSyncTime.ToUniversalTime().ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            logger.IfSomeDo(l => l.Trace("Fetching files with date modified after: " + lastSyncTag + "."));

            long modifiedId = connection.WebServiceManager.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE").First(p => p.DispName == "Date Modified").Id;

            SrchCond filesCreatedAfterLastSyncTime = new SrchCond { SrchTxt = lastSyncTag, SrchRule = SearchRuleType.Must, SrchOper = 7, PropDefId = modifiedId, PropTyp = PropertySearchType.SingleProperty };


            // logger.Trace("Fetched: " + filesAndFolders.Count + " files before filtering.");

            //List<Tuple<ADSK.File, string, bool, bool>> fileAndPath = new List<Tuple<ADSK.File, string, bool, bool>>();
            List<VaultEagleLib.Model.DataStructures.DownloadItem> filesToDownload = new List<VaultEagleLib.Model.DataStructures.DownloadItem>();
            List<ADSK.File> addedFiles = new List<ADSK.File>();
            items = VaultEagleLib.SynchronizationItem.UpdateListWithTactonItems(items);
            List<string> Checked = new List<string>();
            int i = 0;
            trayIcon.Text = "0 of " + items.Count() + " download items to iterate over.";
            foreach (VaultEagleLib.SynchronizationItem item in items)
            {
                if (Checked.Contains(item.SearchPath.ToLower()))
                    continue;
                try
                {
                    Option<Folder> folder = Option.None;
                    Option<string> fileName = Option.None;

                    if (!System.IO.Path.HasExtension(item.SearchPath))
                    {
                        try
                        {
                            folder = connection.WebServiceManager.DocumentService.GetFolderByPath(item.SearchPath).AsOption();
                        }
                        catch { }
                    }
                    else
                    {
                        try
                        {
                            if (item.SearchPath.Contains('.'))
                            {
                                int k = item.SearchPath.LastIndexOf('/');
                                if (k > 0 && connection.WebServiceManager.DocumentService.FindFoldersByPaths(new string[] { item.SearchPath.Substring(0, k) }).Count() > 0)
                                {
                                    folder = connection.WebServiceManager.DocumentService.GetFolderByPath(item.SearchPath.Substring(0, k)).AsOption();
                                    fileName = item.SearchPath.Substring(k + 1).AsOption();
                                }
                            }
                        }
                        catch { }
                    }
                    if (folder.IsSome && folder.Get().Id > 0)
                    {
                        List<Tuple<FileFolder, Option<string>>> FileFolderAndComponentName = new List<Tuple<FileFolder, Option<string>>>();
                        List<FileFolder> filesAndFolders = new List<FileFolder>();
                        List<Folder> folders = new List<Folder>();
                        SrchStatus status = null;
                        string bookmark = string.Empty;
                        bool singleFile = false;
                        while (status == null || filesAndFolders.Count < status.TotalHits)
                        {
                            FileFolder[] results;
                            ADSK.File[] files = connection.WebServiceManager.DocumentService.GetLatestFilesByFolderId(folder.Get().Id, false).Where(f => f.Name.Equals(fileName.Get(), StringComparison.InvariantCultureIgnoreCase)).ToArray();
                            if (fileName.IsSome && files.Count() > 0)
                            {
                                singleFile = true;
                                ADSK.File file = files.First();
                                ADSK.FileFolder fileFolder = new FileFolder();
                                fileFolder.File = file;
                                fileFolder.Folder = folder.Get();
                                results = new FileFolder[] { fileFolder };

                            }
                            else if (item.ComponentName.IsSome)
                            {
                                logger.IfSomeDo(l => l.Info("Could not find file: " + fileName + " for component: " + item.ComponentName.Get()));
                                break;
                            }
                            else if (item.Mirrors.Count() > 0 || !useLastSyncTime)
                                results = connection.WebServiceManager.DocumentService.FindFileFoldersBySearchConditions(new SrchCond[] { }, null, new long[] { folder.Get().Id }, item.Recursive, true, ref bookmark, out status);
                            else
                                results = connection.WebServiceManager.DocumentService.FindFileFoldersBySearchConditions(new SrchCond[] { filesCreatedAfterLastSyncTime }, null, new long[] { folder.Get().Id }, item.Recursive, true, ref bookmark, out status);


                            List<FileFolder> allChildren = new List<FileFolder>();
                            //**********************************************************************************************/
                            if (item.GetChildren)
                            {

                                foreach (FileFolder ff in results)
                                {
                                    foreach (ADSK.File childFile in MCADCommon.VaultCommon.FileOperations.GetAllChildren(ff.File.MasterId, new List<ADSK.File>(), connection))
                                    {
                                        FileFolder child = new FileFolder();

                                        child.File = childFile;
                                        child.Folder = connection.WebServiceManager.DocumentService.GetFolderById(childFile.FolderId);
                                        allChildren.Add(child);
                                        Checked.Add((child.Folder.FullName + "/" + child.File.Name).ToLower());
                                    }
                                }

                            }

                            //***************************************************************************************************/

                            if (results != null)
                            {
                                filesAndFolders.AddRange(results);
                                FileFolderAndComponentName.AddRange(results.Select(r => new Tuple<FileFolder, Option<string>>(r, item.ComponentName)));
                            }
                            if (allChildren.Count > 0)
                            {
                                filesAndFolders.AddRange(allChildren);
                                FileFolderAndComponentName.AddRange(allChildren.Select(r => new Tuple<FileFolder, Option<string>>(r, item.ComponentName)));
                            }
                            Checked.Add(item.SearchPath.ToLower());
                            if (singleFile)
                                break;
                        }
                        status = null;
                        if (item.PatternsToSynchronize.Contains("/") || item.Mirrors.Count() > 0)
                        {
                            while (status == null || folders.Count < status.TotalHits)
                            {
                                Folder[] folderResults = connection.WebServiceManager.DocumentService.FindFoldersBySearchConditions(new SrchCond[] { }, null, new long[] { folder.Get().Id }, item.Recursive, ref bookmark, out status);

                                if (folderResults != null)
                                    folders.AddRange(folderResults);
                            }
                        }
                        FileFolderAndComponentName = FileFolderAndComponentName.OrderBy(f => f.Item1.Folder.FullName.Length).ToList();
                        FileFolderAndComponentName = FileFolderAndComponentName.Where(f => !f.Item1.File.Hidden).ToList();
                        filesToDownload.AddRange(item.GetFilesAndPathsForItem(connection, FileFolderAndComponentName, addedFiles, vaultRoot, item.GetChildren, retries));

                        if (item.PatternsToSynchronize.Contains("/"))
                            filesToDownload.AddRange(item.GetFoldersAndPathsForItem(folders, vaultRoot));

                        if (item.Mirrors.Count() > 0)
                            item.AddMirrorFolders(folders);

                        i++;
                        trayIcon.Text = i + " of " + items.Count() + " download items checked.";
                        // Console.WriteLine(i);
                    }
                    else
                    {
                        logger.IfSomeDo(l => l.Error("Incorrect input path: " + item.SearchPath + "."));
                    }
                }
                catch (Exception ex)
                {
                    logger.IfSomeDo(l => l.Error("Error with synchronization item: " + ex.Message + "."));
                }


            }
            //List<Tuple<ADSK.File, string, bool, bool>> filesAndPathsToDl = new List<Tuple<ADSK.File, string, bool, bool>>();
            List<VaultEagleLib.Model.DataStructures.DownloadItem> filesToDl = new List<VaultEagleLib.Model.DataStructures.DownloadItem>();
            /* filesAndPathsToDl = fileAndPath.Where(f => f.Item1 != null).ToList();
             filesAndPathsToDl = filesAndPathsToDl.DistinctBy(f => f.Item1.MasterId).ToList();
             filesAndPathsToDl.AddRange(fileAndPath.Where(f => f.Item1 == null).ToList()); */
            filesToDl = filesToDownload.Where(f => f is VaultEagleLib.Model.DataStructures.DownloadFile).ToList();
            filesToDl = filesToDl.DistinctBy(f => ((VaultEagleLib.Model.DataStructures.DownloadFile)f).File.MasterId).ToList();
            filesToDl.AddRange(filesToDownload.Where(f => !(f is VaultEagleLib.Model.DataStructures.DownloadFile)));

            return /*removeExcludedPaths*/filesToDl;//filesAndPathsToDl;
        }

        private bool IsFileInServerLocalFolder(FileFolder fileFolder, string localPath)
        {
            string serverName = VaultUtils.GetServerName(localPath);

            return fileFolder.Folder.FullName.StartsWith("$/LocalDesign/" + serverName + "/", StringComparison.InvariantCultureIgnoreCase);
        }

        private List<string> SynchronizeFileBatches(List<VaultEagleLib.Model.DataStructures.DownloadItem> fileFolders, VaultEagleLib.SynchronizationItem[] items, Option<MCADCommon.LogCommon.DisposableFileLogger> logger, int retries)
        {
            // foreach (VaultEagleLib.SynchronizationItem item in items)
            //   item.HandleLockedFiles(fileFolders, retries, logger);

            List<VaultEagleLib.Model.DataStructures.DownloadFolder> folders = fileFolders.OfType<VaultEagleLib.Model.DataStructures.DownloadFolder>().ToList();//Where(f => f.Item1 == null).ToList();
            foreach (VaultEagleLib.Model.DataStructures.DownloadFolder folder in folders)
            {
                if (!VaultEagle.VaultUtils.HandleNetworkErrors(() => Directory.Exists(folder.DownloadPath), retries))
                    VaultEagle.VaultUtils.HandleNetworkErrors(() => Directory.CreateDirectory(folder.DownloadPath), retries);
            }
            return VaultCommunication.DownloadFilesWithChecksum(connection, fileFolders.OfType<VaultEagleLib.Model.DataStructures.DownloadFile>().ToList(), logger, retries);
        }

        private List<string> SynchronizeFiles(int networkRetries, Option<MCADCommon.LogCommon.DisposableFileLogger> logger, List<string> errors, VaultEagleLib.SynchronizerSettings configuration, List<VaultEagleLib.Model.DataStructures.DownloadItem> filesAndFoldersToUpdate, NotifyIcon notify)
        {
            List<string> failedFiles = new List<string>();
            if (filesAndFoldersToUpdate.Count > 0)
            {
                int batchSize = 1000;
                int maxPageNumber = Convert.ToInt32(Math.Floor(filesAndFoldersToUpdate.Count / (batchSize + 0.0)));
                for (int i = 0; i <= maxPageNumber; i++)
                {
                    IEnumerable<VaultEagleLib.Model.DataStructures.DownloadItem> filesToSynchronize = GetBatch(i, filesAndFoldersToUpdate, batchSize);
                    logger.IfSomeDo(l => l.Trace("Fetched: " + filesToSynchronize.Count() + " files to download."));

                    failedFiles.AddRange(SynchronizeFileBatches(filesToSynchronize.ToList(), configuration.Items.ToArray(), logger, networkRetries));

                    notify.Text = "downloaded " + (i * batchSize).ToString() + " of " + filesAndFoldersToUpdate.Count + " files.";// +" synchronized files of " + filesAndFoldersToUpdate.Count.ToString() + ".";
                    logger.IfSomeDo(l => l.Info(filesToSynchronize.Count() + " files downloaded."));
                }
            }
            foreach (VaultEagleLib.SynchronizationItem item in configuration.Items)
            {
                item.DeleteFiles(configuration.VaultRoot, networkRetries);
                item.MirrorFolders(configuration.VaultRoot, networkRetries);
                item.RunFiles(configuration.VaultRoot, filesAndFoldersToUpdate.OfType<VaultEagleLib.Model.DataStructures.DownloadFile>().ToList(), networkRetries);
                item.CreateEmptyFolders(configuration.VaultRoot, networkRetries);
            }
            return failedFiles;
        }

        /*********************************************************************************************/
        /*********************************************************************************************/
        /***********************************************************************************************************************/
        /***********************************************************************************************************************/
    }
}
