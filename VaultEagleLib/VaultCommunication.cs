using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Autodesk.Connectivity.WebServicesTools;
using Autodesk.DataManagement.Client.Framework.Currency;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Connections;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Entities;
using Autodesk.DataManagement.Client.Framework.Vault.Results;
using Autodesk.DataManagement.Client.Framework.Vault.Settings;
using ADSK = Autodesk.Connectivity.WebServices;
using Vault = Autodesk.DataManagement.Client.Framework.Vault;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties;
using Newtonsoft.Json;

using Common.DotNet.Extensions;

namespace VaultEagle
{
    public class VaultCommunication : IDisposable
    {

        private Dictionary<string, FolderPathAbsolute> _allWorkingFoldersCache;

        public StopThreadSwitch stopThread;
        public FunctionJsonCache cache;
        public Connection connection;

        public WebServiceManager manager;
        public long UserId;

        public Action<string> Log = (s) => System.Diagnostics.Debug.WriteLine(s);


        public void StopCheck()
        {
            Dbg.Trace();
            if (ShouldStop)
                throw new StopThreadException();
        }

        private bool ShouldStop
        {
            get { return stopThread != null && stopThread.ShouldStop; }
        }

        /// <summary>
        /// This will get a list of Folders from the Vault server, starting
        /// at <paramref name="root"/>, and filtered by <paramref name="acceptFunc"/>.
        /// </summary>
        /// <param name="root">the root</param>
        /// <param name="acceptFunc">a function to decide if a folder should be traversed/included or not.</param>
        /// <returns></returns>
        public List<ADSK.Folder> FetchFoldersWithSubfoldersFromRoot(ADSK.Folder root, Func<ADSK.Folder, bool> acceptFunc)
        {
            if (root == null)
                throw new ArgumentNullException();
            return FetchFoldersWithSubfoldersFromRoots(new List<ADSK.Folder>() { root }, acceptFunc);
        }

        public class MemCachingBatchProcessor<TArg, TResult>
        {
            public Dictionary<TArg, TResult> cache = new Dictionary<TArg, TResult>();

            private Func<TArg[], TResult[]> batchFunc;
            public MemCachingBatchProcessor(Func<TArg[], TResult[]> batchFunc)
            {
                this.batchFunc = batchFunc;
            }

            HashSet<TArg> queue = new HashSet<TArg>();
            public void Enqueue(TArg arg)
            {
                queue.Add(arg);
            }

            public void PerformQueuedCalls()
            {
                if (queue.Count == 0)
                    return; // no work
                TArg[] queueArray = this.queue.ToArray();
                TResult[] results = batchFunc(queueArray);
                foreach (var item in queueArray.Zip(results, Tuple.Create))
                {
                    cache[item.Item1] = item.Item2;
                }
                queue.Clear();
            }

            public bool EmptyQueue()
            {
                return queue.Count == 0;
            }
        }

        public List<ADSK.Folder> FetchFoldersWithSubfoldersFromRoots(IEnumerable<ADSK.Folder> roots, Func<ADSK.Folder, bool> acceptFunc, Func<ADSK.Folder, bool> recurseFunc = null)
        {
            if (recurseFunc == null)
                recurseFunc = acceptFunc;
            var getChildrenBatchProcessor = new MemCachingBatchProcessor<long, ADSK.Folder[]>(
                folderIds => manager.DocumentService.GetFoldersByParentIds(folderIds, false)
                                                    .Select(x => x.Folders ?? new ADSK.Folder[0])
                                                    .ToArray()
            );
            StopCheck();

            //long[] cachedIds = cache == null ? new long[0] : cache.SimpleGet<long[]>("foldersQueriedInLastSync") ?? new long[0];
            //foreach (var folderId in cachedIds)
            //    getChildrenBatchProcessor.Enqueue(folderId);
            ////getChildrenBatchProcessor.PerformQueuedCalls();
            //StopCheck();

            var result = new Dictionary<string, ADSK.Folder>();

            FetchFoldersWithSubfoldersFromRootsRecursive(roots, acceptFunc, recurseFunc, getChildrenBatchProcessor, result);
            while (!getChildrenBatchProcessor.EmptyQueue())
            {
                getChildrenBatchProcessor.PerformQueuedCalls();
                StopCheck();

                FetchFoldersWithSubfoldersFromRootsRecursive(roots, acceptFunc, recurseFunc, getChildrenBatchProcessor, result);
            }

            List<ADSK.Folder> folders = result.Values.ToList();
            //if (cache != null) cache.SimpleSet("foldersQueriedInLastSync", folders.Select(f => f.Id).ToArray());
            return folders;
        }

        private void FetchFoldersWithSubfoldersFromRootsRecursive(IEnumerable<ADSK.Folder> roots, Func<ADSK.Folder, bool> acceptFunc, Func<ADSK.Folder, bool> recurseFunc, MemCachingBatchProcessor<long, ADSK.Folder[]> getChildrenBatchProcessor, Dictionary<string, ADSK.Folder> result)
        {
            foreach (var root in roots)
            {
                if(acceptFunc(root))
                    result[root.FullName] = root;

                if (recurseFunc(root))
                {
                    var folderId = root.Id;
                    var cache = getChildrenBatchProcessor.cache;
                    if (cache.ContainsKey(folderId))
                    {
                        ADSK.Folder[] children = cache[folderId];
                        FetchFoldersWithSubfoldersFromRootsRecursive(children, acceptFunc, recurseFunc, getChildrenBatchProcessor, result);
                    }
                    else
                    {
                        // enqueue piece of work (and later retry)
                        getChildrenBatchProcessor.Enqueue(folderId);
                    }
                }
            }
        }

        public Func<ADSK.Folder, bool> GetFolderRecurseFilter(SynchronizationTree t)
        {
            return (f) =>
                {
                    SyncState state = t.GetImplicitStateOfPath(f.FullName + "/");
                    return state != SyncState.Exclude && state != SyncState.IncludeSingleFolder && state != SyncState.IncludeOnlyFiles;
                };
        }

        public Func<ADSK.Folder, bool> GetFolderAcceptFilter(SynchronizationTree t)
        {
            return (f) => t.PathIsIncluded(f.FullName + "/");
        }

        public Func<ADSK.Folder, ADSK.File, bool> GetFileFilter(SynchronizationTree t)
        {
            return (folder, file) => t.PathIsIncluded(folder.FullName + "/" + GetCurrentNameOfFile(file));
        }

        public static string GetCurrentNameOfFile(ADSK.File file)
        {
            return file.VerName;
        }

        public ADSK.Folder GetRootFolder()
        {
            return manager.DocumentService.GetFolderRoot();
        }

        public ADSK.Folder[] FetchFoldersFromPaths(IEnumerable<string> paths)
        {
            var paths2 = paths.ToArray();
            if (!paths2.Any())
                return new ADSK.Folder[0];

            string[] vaultPaths = (from p in paths2
                                   where p.EndsWith("/")
                                   select p.TrimEnd('/')).ToArray();
            return manager.DocumentService.FindFoldersByPaths(vaultPaths);
        }

        public ADSK.File[] FetchLatestFilesFromPaths(IEnumerable<string> paths)
        {
            var paths2 = paths.ToArray();
            if (!paths2.Any())
                return new ADSK.File[0];

            string[] vaultPaths = (from p in paths2
                                   where !p.EndsWith("/")
                                   select p).ToArray();
            return manager.DocumentService.FindLatestFilesByPaths(vaultPaths);
        }

        public List<ADSK.FileFolder> GetFilesToSynchronize(SynchronizationTree syncTree, List<string> missingFolders = null, List<string> missingFiles = null, List<ADSK.Folder> watchedFolders = null)
        {
            // a = get folders roots + implicit (for path names)
            // b = get folders recursively from a1
            // get list of files in folders in b
            // get list of explicit files

            missingFiles = missingFiles ?? new List<string>();
            missingFolders = missingFolders ?? new List<string>();

            List<string> explIncFoldersPaths = syncTree.ExplicitlyIncludedFolders;
            List<string> fileFoldersPaths = syncTree.FoldersWithExplicitlyIncludedFiles;

            // Vault web call
            var merged = explIncFoldersPaths.Concat(fileFoldersPaths).ToList();
            var mergedResult = FetchFoldersFromPaths(merged);
            StopCheck();
            List<ADSK.Folder> explIncFolders = mergedResult.Take(explIncFoldersPaths.Count).ToList();
            List<ADSK.Folder> fileFolders = mergedResult.Skip(explIncFoldersPaths.Count).ToList();

            FilterErrors(ref explIncFolders, ref explIncFoldersPaths, missingFolders, x => x.Id == -1);
            FilterErrors(ref fileFolders, ref fileFoldersPaths, new List<string>(), x => x.Id == -1); // these are implict folders, only show files as missing

            // Vault web call
            List<ADSK.Folder> includedFolders = FetchFoldersWithSubfoldersFromRoots(explIncFolders, GetFolderAcceptFilter(syncTree), GetFolderRecurseFilter(syncTree));
            StopCheck();

            if (watchedFolders != null)
                watchedFolders.AddRange(includedFolders);
            var includedFoldersToGetFilesFrom = includedFolders.Where(x => new[]{SyncState.Include, SyncState.IncludeOnlyFiles, }.Contains(syncTree.GetImplicitStateOfPath(x.FullName + "/"))).ToList();

            // Vault web call, quite slow, most time in call is spent here
            var filesPerFolder = FetchFilesFromFolders(includedFoldersToGetFilesFrom)
                .Zip(includedFoldersToGetFilesFrom, (files, folder) => new { files, folder });
            StopCheck();

            var fileFilter = GetFileFilter(syncTree);
            var result = new List<ADSK.FileFolder>();
            foreach (var item in filesPerFolder)
            {
                ADSK.Folder folder = item.folder;
                var includedFiles = item.files.Where(file => fileFilter(folder, file));
                foreach (var file in includedFiles)
                    result.Add(new ADSK.FileFolder { File = file, Folder = folder });
            }

            List<string> filePaths = syncTree.ExplicitlyIncludedFiles;
            // Vault web call
            var expFiles = FetchLatestFilesFromPaths(filePaths).Zip(filePaths, (f, p) => new { f, p }).ToList();
            StopCheck();

            FilterErrors(ref expFiles, ref filePaths, missingFiles, x => x.f.Id == -1);

            Dictionary<string, ADSK.Folder> pathTofileFoldersDict = fileFoldersPaths.Zip(fileFolders, (p, f) => new { p, f }).ToDictionary(pf => pf.p, pf => pf.f);

            foreach (var filePath in expFiles)
            {
                string parentPath = SynchronizationTree.GetParentFromPath(filePath.p);
                ADSK.File file = filePath.f;
                ADSK.Folder folder = pathTofileFoldersDict[parentPath];
                result.Add(new ADSK.FileFolder() { File = file, Folder = folder });
            }

            return result.Distinct(ff => GeneratePathFromFolderAndFile(ff.Folder, ff.File)).ToList();
        }

        private static void FilterErrors<TResult, TArg>(ref List<TResult> result, ref List<TArg> args, List<TArg> failedAcc, Func<TResult, bool> failFunc)
        {
            var part = args
                       .Zip(result, (arg, res) => new { arg, res })
                       .PartitionBy(x => failFunc(x.res));
            failedAcc.AddRange(part[true].Select(x => x.arg));
            args = part[false].Select(x => x.arg).ToList();
            result = part[false].Select(x => x.res).ToList();
        }

        public static string GeneratePathFromFolderAndFile(ADSK.FileFolder fileFolder)
        {
            return GeneratePathFromFolderAndFile(fileFolder.Folder, fileFolder.File);
        }

        public static string GeneratePathFromFolderAndFile(ADSK.Folder folder, ADSK.File file)
        {
            return folder.FullName + "/" + GetCurrentNameOfFile(file);
        }

        /// <summary>
        /// For each folder, get the Files it contains.
        /// </summary>
        /// <param name="folders"></param>
        /// <returns></returns>
        public ADSK.File[][] FetchFilesFromFolders(IEnumerable<ADSK.Folder> folders)
        {
            var folders2 = folders.ToArray();
            if (!folders2.Any())
                return new ADSK.File[0][];

            return manager.DocumentService
                .GetLatestFilesByFolderIds(folders2.Select(f => f.Id).ToArray(), includeHidden: false)
                .Select(fa => fa.Files ?? new ADSK.File[0])
                .ToArray();
        }

        public string GetLocalPathFromFolder(ADSK.Folder folder)
        {
            Dbg.Trace("before connection.WorkingFoldersManager.GetWorkingFolder(folder.FullName).FullPath");
            return connection.WorkingFoldersManager.GetWorkingFolder(folder.FullName).FullPath;
        }

        private string GetLocalPathFromFileFolder(ADSK.File file, ADSK.Folder folder)
        {
            string workingFolder = GetLocalPathFromFolder(folder);
            System.Diagnostics.Debug.WriteLine(string.Format("GetWorkingFolder({0}) = {1}", folder.FullName, workingFolder));
            return GetLocalPathFromFileFolder(file, workingFolder);
        }

        private static string GetLocalPathFromFileFolder(ADSK.File file, string workingFolder)
        {
            string localPath = Path.Combine(workingFolder, GetCurrentNameOfFile(file));
            System.Diagnostics.Debug.WriteLine(string.Format("Using {0} as local path for {1}", localPath, file.VerName));
            return localPath;
        }

        public static int CalculateCrc32(string filename)
        {
            using (var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var crcStream = new Ionic.Crc.CrcCalculatorStream(fileStream))
            {
                var buf = new byte[64 * 1024];
                while (crcStream.Read(buf, 0, buf.Length) != 0)
                { }
                int crc = crcStream.Crc;
                return crc;
            }
        }

        public VaultFileStatus QuickLocalFileStatus(ADSK.File vaultFile, string localPath, bool clearCache = false)
        {
            Dbg.Trace("before connection.WebServiceManager.DocumentService.GetFolderById(vaultFile.FolderId)");
            var folder = connection.WebServiceManager.DocumentService.GetFolderById(vaultFile.FolderId);
            string workingFolder = GetLocalPathFromFolder(folder);
            var localFileStatus = QuickLocalFileStatus(vaultFile, localPath, workingFolder, folder, clearCache);
            System.Diagnostics.Debug.WriteLine("Status of " + localPath + ": " + localFileStatus);
            return localFileStatus;
        }

        public VaultFileStatus QuickLocalFileStatus(ADSK.File lastFile, ADSK.Folder folder, bool clearCache = false)
        {
            string workingFolder = GetLocalPathFromFolder(folder);
            string localPath = GetLocalPathFromFileFolder(lastFile, workingFolder);
            var localFileStatus = QuickLocalFileStatus(lastFile, localPath, workingFolder, folder, clearCache);
            System.Diagnostics.Debug.WriteLine("Status of " + localPath + ": " + localFileStatus);

            return localFileStatus;
        }

        public class VaultFileStatus
        {
            public EntityStatus.VersionStateEnum VersionState;
            public EntityStatus.LocalEditsStateEnum LocalEditsState;
            public EntityStatus.CheckoutStateEnum CheckoutState;

            public VaultFileStatus(EntityStatus.VersionStateEnum versionState, EntityStatus.LocalEditsStateEnum localEditsState, EntityStatus.CheckoutStateEnum checkoutState)
            {
                VersionState = versionState;
                CheckoutState = checkoutState;
                LocalEditsState = localEditsState;
            }

            public override string ToString()
            {
                return string.Format("VersionState: {0}, LocalEditsState: {1}, CheckoutState: {2}", VersionState, LocalEditsState, CheckoutState);
            }
        }

        public VaultFileStatus QuickLocalFileStatus(ADSK.File vaultFile, string localPath, string workingFolder, ADSK.Folder folder, bool clearCache)
        {
            Dbg.Trace();

            // VersionState.MatchesLatestVaultVersion -> up to date -> do nothing
            // VersionState.MatchesNoVaultVersion -> possibly locally modified -> do nothing
            // VersionState.Unknown -> do nothing

            // VersionState.InVaultButNotOnDisk -> download
            // VersionState.MatchesOlderVaultVersion -> out-of-date -> download

            // VersionState.OnDiskButNotInVault -> how would this happen? no vaultFile -> do nothing. delete?

            var checkoutState = !vaultFile.CheckedOut
                                     ? EntityStatus.CheckoutStateEnum.NotCheckedOut
                                     : vaultFile.CkOutUserId == UserId
                                           ? EntityStatus.CheckoutStateEnum.CheckedOutByCurrentUser
                                           : EntityStatus.CheckoutStateEnum.CheckedOutByOtherUser;

            FileInfo fileInfo = new FileInfo(localPath);
            if (!fileInfo.Exists)
                return new VaultFileStatus(EntityStatus.VersionStateEnum.InVaultButNotOnDisk, EntityStatus.LocalEditsStateEnum.Unknown, checkoutState);

            StopCheck();


            var func = Utils.GetFunc(
                (string localPath2, long localFileSize, DateTime localLastWriteTimeUtc, DateTime localCreationTimeUtc, string vaultVerNum) =>
                {
                    // max 5MB, presumably slow if too big
                    if (vaultFile.FileSize == localFileSize && localFileSize < 5*1024*1024)
                    {
                        try
                        {
                            if (fileInfo.CreationTime == vaultFile.CreateDate)
                                Dbg.Trace("before if (CalculateCrc32(localPath) == vaultFile.Cksum)");
                            if (fileInfo.CreationTime == vaultFile.CreateDate)
                                if (CalculateCrc32(localPath) == vaultFile.Cksum)
                                    return Tuple.Create(EntityStatus.VersionStateEnum.MatchesLatestVaultVersion, EntityStatus.LocalEditsStateEnum.DoesNotHaveLocalEdits);
                        }
                        catch (Exception) { }
                    }

                    bool resetWorkingFolder = false;
                    try
                    {
                        string localPathDirectory = Path.GetDirectoryName(Path.GetFullPath(localPath));
                        if (
                            !workingFolder.TrimEnd(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                                          .Equals(localPathDirectory, StringComparison.OrdinalIgnoreCase))
                        {
                            resetWorkingFolder = true;
                            SetWorkingFolderTemporarily(folder.FullName, new FolderPathAbsolute(localPathDirectory));
                        }

                        Dbg.Trace("before new FileIteration(connection, vaultFile);");
                        var fileIteration = new FileIteration(connection, vaultFile);
                        Dbg.Trace("before connection.PropertyManager.GetPropertyValue(fileIteration, vaultStatusPropertyDefinition, null) as EntityStatusImageInfo");
                        var entityStatusImageInfo = connection.PropertyManager.GetPropertyValue(fileIteration, vaultStatusPropertyDefinition, null) as EntityStatusImageInfo;
                        if (entityStatusImageInfo == null)
                            return Tuple.Create(EntityStatus.VersionStateEnum.Unknown, EntityStatus.LocalEditsStateEnum.Unknown);
                        var vaultFileStatus = Tuple.Create(entityStatusImageInfo.Status.VersionState, entityStatusImageInfo.Status.LocalEditsState);
                        entityStatusImageInfo.DisposeUnlessNull(ref entityStatusImageInfo);
                        return vaultFileStatus;
                    }
                    finally
                    {
                        if(resetWorkingFolder)
                            ResetWorkingFoldersIfNecessary();
                    }
                });

            var cachingStatusFunction = cache.GetCachingFunction(func, "explorer.GetLocalFileStatus(file)", skipCachePredicate: vState => vState.Item1 == EntityStatus.VersionStateEnum.Unknown);

            var verNums = Enumerable.Range(0, 50).TakeWhile(i => i < vaultFile.VerNum).Select(i => vaultFile.VerNum - i);
            var argsPerVerNum = verNums.Select(verNum => Tuple.Create(localPath, fileInfo.Length, fileInfo.LastWriteTimeUtc, fileInfo.CreationTimeUtc, string.Format("{0}:{1}:{2}:{3}", connection.Server, connection.Vault, vaultFile.MasterId, verNum))).ToArray();
            string mainKey = cachingStatusFunction.GetKey(argsPerVerNum.First());
            if (!cachingStatusFunction.IsCallCached(mainKey))
            {
                if (!clearCache)
                {
                    foreach (var args in argsPerVerNum.Skip(1))
                    {
                        var key = cachingStatusFunction.GetKey(args);
                        if (cachingStatusFunction.IsCallCached(key))
                        {
                            var oldVersionReturnValue = cachingStatusFunction.PerformCall(argsPerVerNum.First(), key);
                            StopCheck();
                            if (oldVersionReturnValue.Item1 == EntityStatus.VersionStateEnum.MatchesLatestVaultVersion && oldVersionReturnValue.Item2 == EntityStatus.LocalEditsStateEnum.DoesNotHaveLocalEdits)
                                return new VaultFileStatus(EntityStatus.VersionStateEnum.MatchesOlderVaultVersion, EntityStatus.LocalEditsStateEnum.DoesNotHaveLocalEdits, checkoutState);
                            break;
                        }
                    }
                }
            }

            var returnValue = cachingStatusFunction.PerformCall(argsPerVerNum.First(), mainKey, clearCache);
            StopCheck();
            return new VaultFileStatus(returnValue.Item1, returnValue.Item2, checkoutState);
        }

        private PropertyDefinition _vaultStatusPropertyDefinition;
        private PropertyDefinition vaultStatusPropertyDefinition
        {
            get
            {
                if (_vaultStatusPropertyDefinition != null)
                    return _vaultStatusPropertyDefinition;
                Dbg.Trace("before connection.PropertyManager.GetPropertyDefinitions(EntityClassIds.Files, null, PropertyDefinitionFilter.IncludeAll);");
                PropertyDefinitionDictionary props = connection.PropertyManager.GetPropertyDefinitions(EntityClassIds.Files, null, PropertyDefinitionFilter.IncludeAll);
                PropertyDefinition statusProp = props[PropertyDefinitionIds.Client.VaultStatus];
                return _vaultStatusPropertyDefinition = statusProp;
            }
        }

        public void DownloadFiles(Tuple<ADSK.FileFolder, Option<string>>[] filesToDownload, List<string> failedDownloads, Action afterDownload)
        {
            var downloadSettings = new AcquireFilesSettings(connection, updateFileReferences: false);

            downloadSettings.OptionsResolution.OverwriteOption = AcquireFilesSettings.AcquireFileResolutionOptions.OverwriteOptions.ForceOverwriteAll;
            downloadSettings.OptionsResolution.SyncWithRemoteSiteSetting = AcquireFilesSettings.SyncWithRemoteSite.Always;

            downloadSettings.OptionsExtensibility.PreFileAcquire += (sender, args) =>
                { 
                    if (ShouldStop)
                    {
                        args.PreFileAcquisitionResult = AcquireFilesSettings.PreFileAcquisitionResult.Abort;
                        return;
                    }
                    Log("Downloading: " + args.FileOp.File.EntityName); 
                };

            var perFileInfo = new Dictionary<long, string>();

            var @lock = new object();

            downloadSettings.OptionsExtensibility.PostFileAcquire += (sender, args) =>
            {
                lock (@lock)
                {
                    DownloadedFile(args, failedDownloads, perFileInfo.OptionGetValue(args.FileResult.File.EntityIterationId));
                    afterDownload();
                }
            };

            foreach (var fileToDownload in filesToDownload)
            {
                var fileFolder = fileToDownload.Item1;
                var specialLocalPath = fileToDownload.Item2;
                bool isVaultEagleZipFile = GetCurrentNameOfFile(fileFolder.File).ToLowerInvariant().EndsWith("_unzipped_by_vaulteagle.zip");
                var fileIteration = new FileIteration(connection, new Folder(connection, fileFolder.Folder), fileFolder.File);

                if (isVaultEagleZipFile)
                {
                    specialLocalPath.IfSomeDo(p => perFileInfo[fileFolder.File.Id] = p);
                    var tempPath = DownloadUnzipAndRenamePreDownload(fileIteration);
                    downloadSettings.AddFileToAcquire(fileIteration,
                                                      AcquireFilesSettings.AcquisitionOption.Download, new FilePathAbsolute(tempPath));
                }
                else
                {
                    if(specialLocalPath.IsSome)
                        downloadSettings.AddFileToAcquire(fileIteration, AcquireFilesSettings.AcquisitionOption.Download, new FilePathAbsolute(specialLocalPath.Get()));
                    else
                        downloadSettings.AddFileToAcquire(fileIteration, AcquireFilesSettings.AcquisitionOption.Download);
                }
            }

            try
            {
                AcquireFilesResults results = connection.FileManager.AcquireFiles(downloadSettings);
           }
            catch (AggregateException aggregate)
            {
                var flattened = aggregate.Flatten();
                if (flattened.InnerExceptions.All(ex => ex is StopThreadException || ex is OperationCanceledException))
                {
                    throw new StopThreadException();
                }
                else
                    throw;
            }
        }

        private void DownloadedFile(AcquireFilesSettings.AcquireFileExtensibilityOptions.PostFileAcquireEventArgs postFileAcquireEventArgs, List<string> failedDownloads, Option<string> specialLocalPath)
        {
            var file = postFileAcquireEventArgs.FileResult.File;
            var folder = file.Parent;
            if (postFileAcquireEventArgs.FileResult.Status != FileAcquisitionResult.AcquisitionStatus.Success)
            {
                if (failedDownloads != null)
                    failedDownloads.Add(GeneratePathFromFolderAndFile(folder, file));
                if (postFileAcquireEventArgs.FileResult.Status == FileAcquisitionResult.AcquisitionStatus.Exception)
                {
                    var ex = postFileAcquireEventArgs.FileResult.Exception;
                    Log("  Exception: " + ex.Message);
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
            else
            {
                var localPath = postFileAcquireEventArgs.FileResult.LocalPath.FullPath;
                bool isVaultEagleZipFile = localPath.ToLowerInvariant().EndsWith("_unzipped_by_vaulteagle.zip");
                if(isVaultEagleZipFile)
                {
                    DownloadUnzipAndRenamePostDownload(file, localPath, specialLocalPath);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Downloaded: " + file.EntityName);
                }
                
                QuickLocalFileStatus(file, folder, clearCache: true); // update cache
            }
        }

        private string DownloadUnzipAndRenamePreDownload(FileIteration vaultEagleZipFile)
        {
            string filename = vaultEagleZipFile.EntityName;
            System.Diagnostics.Debug.WriteLine("Downloading: " + filename);

            var tempFolder = System.IO.Path.GetTempPath();
            var tempPath = System.IO.Path.Combine(tempFolder, filename);
            return tempPath;
        }

        private void DownloadUnzipAndRenamePostDownload(FileIteration vaultEagleZipFile, string tempPath, Option<string> specialLocalPath)
        {
            string filename = vaultEagleZipFile.EntityName;
            System.Diagnostics.Debug.WriteLine("Downloading: " + filename);
            //string targetPath;
            Log("Found Vault Eagle packed folder.");

            string targetPath = specialLocalPath.Else(() => GetLocalPathFromFileFolder(vaultEagleZipFile, vaultEagleZipFile.Parent));

            StopCheck();
            System.Diagnostics.Debug.WriteLine("Downloaded: " + filename);

            string unpackDirectory = System.IO.Path.GetDirectoryName(targetPath);

            // delete any tmp-files created by zipfile
            using (var zip = Ionic.Zip.ZipFile.Read(tempPath))
                foreach (var zipEntry in zip)
                {
                    string f = System.IO.Path.Combine(unpackDirectory, zipEntry.FileName + ".tmp");
                    DeleteFile(targetPath);
                }

            using (var zipFile = Ionic.Zip.ZipFile.Read(tempPath))
                foreach (var zipEntry in zipFile)
                {
                    System.Diagnostics.Debug.Write("Extracting: " + zipEntry.FileName + "..");
                    Log("Extracting: " + zipEntry.FileName);
                    zipEntry.Extract(unpackDirectory, Ionic.Zip.ExtractExistingFileAction.OverwriteSilently);
                    System.Diagnostics.Debug.WriteLine(".");
                }

            System.Diagnostics.Debug.WriteLine("Finised extracting: " + filename);
            MoveFile(tempPath, targetPath);
        }

        private static void MoveFile(string source, string targetPath)
        {
            var creationDateUtc = File.GetCreationTimeUtc(source);
            DeleteFile(targetPath);
            System.IO.File.Move(source, targetPath); // sequence atomic on NTFS, apparently
            var targetInfo = new FileInfo(targetPath);

            var attributes = targetInfo.Attributes;
            targetInfo.Attributes = FileAttributes.Normal; // disable read only
            targetInfo.CreationTimeUtc = creationDateUtc; // preserve creation date. (note file system "tunneling")
            targetInfo.Attributes = attributes;
        }

        private static void DeleteFile(string targetPath)
        {
            var fileInfo = new System.IO.FileInfo(targetPath);
            if (!fileInfo.Exists)
                return;

            if(fileInfo.Attributes != FileAttributes.Normal)
                fileInfo.Attributes = FileAttributes.Normal;

            fileInfo.Delete();
        }



        public void Dispose()
        {
            cache.DisposeUnlessNull(ref cache);
            manager.DisposeUnlessNull(ref manager);
        }

        public void InitializeFromConnection(Connection connection)
        {
            manager = connection.WebServiceManager;
            this.connection = connection;
        }

        public string[] GetPathsFromSelections(long[] selectedFileIds, long[] selectedMasterFileIds, long[] selectedFolderIds)
        {
            var filesFromFileIds = new ADSK.File[0];
            if (selectedFileIds.Length > 0)
                filesFromFileIds = manager.DocumentService.GetFilesByIds(selectedFileIds);
            StopCheck();

            var filesFromMasterFileIds = new ADSK.File[0];
            if (selectedMasterFileIds.Length > 0)
                filesFromMasterFileIds = manager.DocumentService.GetLatestFilesByMasterIds(selectedMasterFileIds);
            StopCheck();

            var files = filesFromFileIds.Concat(filesFromMasterFileIds).Distinct(f => f.MasterId).ToArray();
            long[] masterFileIds = files.Select(f => f.MasterId).ToArray();

            // "[...] GetFoldersByFileMasterId returns an array of Folders even though
            // the array will always have 1 element in 2012."
            var foldersFromMasterFileIds = new ADSK.Folder[0];
            if (masterFileIds.Length > 0)
                foldersFromMasterFileIds = manager.DocumentService.GetFoldersByFileMasterIds(masterFileIds)
                    .Select(fa => fa.Folders.Single()) // Folders has length 1
                    .ToArray();
            StopCheck();

            var filePaths = foldersFromMasterFileIds.Zip(
                files,
                (folder, file) => GeneratePathFromFolderAndFile(folder, file)
            );

            var foldersFromFolderIds = new ADSK.Folder[0];
            if (selectedFolderIds.Length > 0)
                foldersFromFolderIds = manager.DocumentService.GetFoldersByIds(selectedFolderIds);
            StopCheck();

            var folderPaths = foldersFromFolderIds.Select(f => f.FullName + "/");

            return filePaths.Concat(folderPaths).ToArray();
        }

        public bool TryRepairSynchronizationTree(SynchronizationTree t)
        {
            var allPaths = t.ExplicitPaths.Select(kv => new {path = kv.Key, kv.Value.LastVaultId}).ToArray();

            var part = allPaths.PartitionBy(x => SynchronizationTree.SyncInfo.IsFolder(x.path));
            var folders = part[true];
            var files = part[false];

            var vaultFolders = (!folders.Any() ? new ADSK.Folder[0] : manager.DocumentService.FindFoldersByPaths(folders.Select(x => x.path.TrimEnd('/')).ToArray())).Zip(folders, (folder, x) => new {folder, x.path, x.LastVaultId});
            var vaultFiles = (!files.Any() ? new ADSK.File[0] : manager.DocumentService.FindLatestFilesByPaths(files.Select(x => x.path).ToArray())).Zip(files, (file, x) => new { file, x .path, x .LastVaultId }).ToArray();

            var missingVaultFolders = vaultFolders.Where(x => x.folder.Id == -1).ToArray();
            var missingVaultFiles = vaultFiles.Where(x => x.file.Id == -1).ToArray();

            bool didAnything = false;
            if (missingVaultFolders.Any())
            {
                Log("Missing folders detected, possibly moved, trying to recover...");
                var missingVaultFoldersWithOldId = missingVaultFolders.Where(x => x.LastVaultId != -1).ToArray();
                if (missingVaultFoldersWithOldId.Any())
                {
                    var foundNewFolders =
                        manager.DocumentService.FindFoldersByIds(missingVaultFoldersWithOldId.Select(x => x.LastVaultId).ToArray())
                                               .Zip(missingVaultFoldersWithOldId, (newFolder, x) => new {newFolder, x})
                                               .Where(x => x.newFolder.Id != -1)
                                               .ToArray();

                    if (foundNewFolders.Any())
                    {
                        bool moved = false;
                        foreach (var foundNewFolder in foundNewFolders)
                        {
                            var oldPath = foundNewFolder.x.path;
                            var newPath = foundNewFolder.newFolder.FullName + '/';
                            if(oldPath == "$/" || newPath == "$/")
                                continue;

                            var oldPathSplit = oldPath.TrimEnd('/').Split('/');
                            var oldFolderName = oldPathSplit.LastOrDefault();
                            var newPathSplit = newPath.TrimEnd('/').Split('/');
                            var newFolderName = newPathSplit.LastOrDefault();
                            var oldParentPath = string.Join("/", oldPathSplit.Take(oldPathSplit.Length-1));
                            var newParentPath = string.Join("/", newPathSplit.Take(newPathSplit.Length - 1)); ;

                            bool sameName = newFolderName == oldFolderName;
                            bool sameParent = oldParentPath == newParentPath && oldParentPath != "$";
                            if (!(sameName || sameParent))
                                continue;

                            if(!moved)
                            {
                                moved = true;
                                Log("Moved folders found...");
                            }

                            var temp = t.ExplicitPaths[oldPath];
                            t.ExplicitPaths.Remove(oldPath);
                            t.ExplicitPaths[newPath] = temp;
                            Log("  " + oldPath + " moved to " + newPath);
                        }
                        if(moved)
                            didAnything = true;
                    }
                }
            }
            if (missingVaultFiles.Any())
            {
                Log("Missing files detected, possibly moved, trying to recover...");
                var missingVaultFilesWithOldId = missingVaultFiles.Where(x => x.LastVaultId != -1).ToArray();
                if (missingVaultFilesWithOldId.Any())
                {
                    var foundNewFiles =
                        manager.DocumentService.FindLatestFilesByMasterIds(missingVaultFilesWithOldId.Select(x => x.LastVaultId).ToArray())
                                               .Zip(missingVaultFilesWithOldId, (newFile, x) => new {newFile, x})
                                               .Where(x => x.newFile.Id != -1)
                                               .ToArray();
                    if (foundNewFiles.Any())
                    {
                        var foundNewFileFolders =
                            manager.DocumentService.GetFoldersByFileMasterIds(foundNewFiles.Select(x => x.newFile.MasterId).ToArray())
                                                   .Zip(foundNewFiles, (f, x) => new {folder = f.Folders.Single(), x.newFile, x.x.path, x.x.file})
                                                   .ToArray();

                        bool moved = false;
                        foreach (var foundNewFileFolder in foundNewFileFolders)
                        {
                            var oldPath = foundNewFileFolder.path;
                            var newPath = GeneratePathFromFolderAndFile(foundNewFileFolder.folder,
                                                                        foundNewFileFolder.newFile);
                            var oldPathSplit = oldPath.Split('/');
                            var oldFolderName = oldPathSplit.LastOrDefault();
                            var newPathSplit = newPath.Split('/');
                            var newFolderName = newPathSplit.LastOrDefault();
                            var oldParentPath = string.Join("/", oldPathSplit.Take(oldPathSplit.Length - 1));
                            var newParentPath = string.Join("/", newPathSplit.Take(newPathSplit.Length - 1)); ;

                            bool sameName = newFolderName == oldFolderName;
                            bool sameParent = oldParentPath == newParentPath;
                            if (!(sameName || sameParent))
                                continue;

                            if(!moved)
                            {
                                moved = true;
                                Log("Moved files found...");
                            }

                            var temp = t.ExplicitPaths[oldPath];
                            t.ExplicitPaths.Remove(oldPath);
                            t.ExplicitPaths[newPath] = temp;
                            Log("  " + oldPath + " moved to " + newPath);
                        }
                        if(moved)
                            didAnything = true;
                    }
                }
            }
            return didAnything;
        }

        public void UpdateLastVaultId(SynchronizationTree tree)
        {
            HashSet<string> missingFolders;
            HashSet<string> missingFiles;
            UpdateLastVaultId(tree, out missingFolders, out missingFiles);
        }

        public void UpdateLastVaultId(SynchronizationTree tree, out HashSet<string> missingFolders,
                                           out HashSet<string> missingFiles)
        {
            var paths = tree.ExplicitPaths.Keys.ToArray();
            var folderPaths = paths.Where(p => tree.IsFolder(p)).ToArray();
            var filePaths = paths.Where(p => !tree.IsFolder(p)).ToArray();

            StopCheck();
            var folders = FetchFoldersFromPaths(folderPaths).Zip(folderPaths, (folder, path) => new { folder, path }).ToArray();
            StopCheck();
            var files = FetchLatestFilesFromPaths(filePaths).Zip(filePaths, (file, path) => new { file, path }).ToArray();
            StopCheck();

            foreach (var folder in folders.Where(x => x.folder.Id != -1))
                tree.ExplicitPaths[folder.path].LastVaultId = folder.folder.Id;

            foreach (var file in files.Where(x => x.file.Id != -1))
                tree.ExplicitPaths[file.path].LastVaultId = file.file.MasterId;

            missingFolders = folders.Where(x => x.folder.Id == -1).Select(x => x.path).ToSet();
            missingFiles = files.Where(x => x.file.Id == -1).Select(x => x.path).ToSet();

            StopCheck();
            tree.WriteTree();
        }


        #region public class ResumeState

        public void ResetWorkingFoldersIfNecessary()
        {
            Dbg.Trace();
            var resumeState = ReadAllResumeStates().GetValueOrDefault(GetKeyFromServerAndVault(), null);
            if (resumeState == null)
                return;

            _allWorkingFoldersCache = null;
            foreach (var mapping in resumeState.ModifiedWorkingFoldersMappings)
                if (mapping.Value == null)
                    connection.WorkingFoldersManager.ClearWorkingFolder(mapping.Key);
                else
                    connection.WorkingFoldersManager.SetWorkingFolder(mapping.Key, new FolderPathAbsolute(mapping.Value));

            WriteResumeState(null);
        }

        private void SetWorkingFolderTemporarily(string vaultFolder, FolderPathAbsolute folderPathAbsolute)
        {
            Dbg.Trace();
            vaultFolder = vaultFolder.TrimEnd('/');
            var allWorkingFolders = _allWorkingFoldersCache ?? (_allWorkingFoldersCache = connection.WorkingFoldersManager.GetAllWorkingFolders());

            bool writeResumeState = true;
            var resumeState = ReadAllResumeStates().GetValueOrDefault(GetKeyFromServerAndVault(), null);
            string existingMappingKey = null;
            if (resumeState != null)
            {
                existingMappingKey = resumeState.ModifiedWorkingFoldersMappings.Select(x => x.Key).Where(x => vaultFolder.Equals(x, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (existingMappingKey != null)
                    writeResumeState = false;
            }

            if (writeResumeState)
            {
                resumeState = resumeState ?? new ResumeState();
                //if (existingMappingKey != null)
                //    resumeState.ModifiedWorkingFoldersMappings.Remove(existingMappingKey);
                var modifiedWorkingFoldersMapping = allWorkingFolders.GetValueOrDefault(vaultFolder, null);
                resumeState.ModifiedWorkingFoldersMappings[vaultFolder] = modifiedWorkingFoldersMapping == null ? null : modifiedWorkingFoldersMapping.FullPath;
                WriteResumeState(resumeState);
            }

            connection.WorkingFoldersManager.SetWorkingFolder(vaultFolder, folderPathAbsolute);
        }

        private void WriteResumeState(ResumeState resumeState)
        {
            var allResumeStates = ReadAllResumeStates();
            try
            {
                var key = GetKeyFromServerAndVault();
                if (resumeState == null || resumeState.IsEmpty)
                    allResumeStates.Remove(key);
                else
                    allResumeStates[key] = resumeState;
                if (allResumeStates.Count == 0)
                    Utils.DeleteFile(FilesAndFolders.GetResumeStatePath());
                else
                    using (Stream stream = new FileStream(FilesAndFolders.GetResumeStatePath(createFolder: true), FileMode.Create))
                    using (var writer = new StreamWriter(stream))
                        new JsonSerializer { Formatting = Formatting.Indented }.Serialize(writer, allResumeStates);
            }
            catch { }
        }

        private string GetKeyFromServerAndVault()
        {
            return connection.Server + "/" + connection.Vault;
        }

        public class ResumeState
        {
            public Dictionary<string, string> ModifiedWorkingFoldersMappings = new Dictionary<string, string>();

            public bool IsEmpty
            {
                get { return ModifiedWorkingFoldersMappings.Count == 0; }
            }
        }

        private static Dictionary<string,ResumeState> ReadAllResumeStates()
        {
            Dictionary<string, ResumeState> resumeState = null;
            try
            {
                if (File.Exists(FilesAndFolders.GetResumeStatePath()))
                    using (Stream stream = new FileStream(FilesAndFolders.GetResumeStatePath(), FileMode.Open))
                    using (var reader = new StreamReader(stream))
                    using (var jr = new JsonTextReader(reader))
                        resumeState = new JsonSerializer().Deserialize<Dictionary<string, ResumeState>>(jr);
            }
            catch { }
            return resumeState ?? new Dictionary<string, ResumeState>();
        }

        /*****************************************************************************************/
        public static void DeleteFolder(string dirPath,  int networkRetries)
        {
            try
            {
                string[] subFolderPaths = VaultUtils.HandleNetworkErrors(() => { return Directory.GetDirectories(dirPath); }, networkRetries);

                if (subFolderPaths != null)
                    foreach (string subFolderPath in subFolderPaths)
                        DeleteFolder(subFolderPath, networkRetries);

                string[] filePaths = VaultUtils.HandleNetworkErrors(() => { return Directory.GetFiles(dirPath); }, networkRetries);
                if (filePaths != null)
                    foreach (string filePath in filePaths)
                        DeleteFileWithRetries(filePath, networkRetries);

                VaultUtils.HandleNetworkErrors(() => Directory.Delete(dirPath), networkRetries);
            }
            catch (Exception ex)
            { }
        }

        /********************************************************************************************************/
        public static void DeleteSpecialVaultFolder(string path, int networkRetries)
        {
            string vaultSpecialFolder = Path.Combine(path, "_V");
            bool vaultSpecialFolderExists = VaultUtils.HandleNetworkErrors(() => { return Directory.Exists(vaultSpecialFolder); }, networkRetries);

            if (vaultSpecialFolderExists)
                DeleteFolder(vaultSpecialFolder, networkRetries);
        }

        /*****************************************************************************************/
        public static void DeleteFileWithRetries(string fullFilePath, int networkRetries)
        {
            try
            {
                VaultUtils.HandleNetworkErrors(() => File.SetAttributes(fullFilePath, FileAttributes.Normal), networkRetries);

                string newFilePath = Path.Combine(Path.GetDirectoryName(fullFilePath), "To_Be_deleted_" + Path.GetFileName(fullFilePath));

                VaultUtils.HandleNetworkErrors(() => File.Move(fullFilePath, newFilePath), networkRetries);
                VaultUtils.HandleNetworkErrors(() => File.Delete(newFilePath), networkRetries);
            }
            catch (Exception ex)
            {  }
        }

        /******************************************************************************/
        public static List<string> DownloadFilesWithChecksum(Vault.Currency.Connections.Connection connection, List<VaultEagleLib.Model.DataStructures.DownloadFile> files/*, string filePath, bool removeReadOnly*/, Option<MCADCommon.LogCommon.DisposableFileLogger> logger, int networkRetries)
        {
            foreach (VaultEagleLib.Model.DataStructures.DownloadFile filePathReadOnly in files)
                filePathReadOnly.Log(logger);//logger.IfSomeDo(l => l.Info("Synchronizing: " + filePathReadOnly.Item2 + "\\" + filePathReadOnly.Item1.Name));
            
            List<string> failedFiles = DownloadFilesWithRetries(connection, files, networkRetries);
            foreach (string failedFile in failedFiles)
                logger.IfSomeDo(l => l.Error("Failed to download: " + failedFile));

            foreach (VaultEagleLib.Model.DataStructures.DownloadFile filePathReadOnly in files)
            {
                if (VaultUtils.HandleNetworkErrors(() => { return File.Exists(filePathReadOnly.LocalFileName); }, networkRetries))
                {
                    try
                    {
                        FileInfo f = new FileInfo(filePathReadOnly.LocalFileName);
                        System.Security.AccessControl.FileSecurity fs = f.GetAccessControl();

                        fs.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(@".\Users", System.Security.AccessControl.FileSystemRights.ReadAndExecute, System.Security.AccessControl.AccessControlType.Allow));
                        f.SetAccessControl(fs);

                    }
                    catch { }
                    if ((filePathReadOnly.Writable))
                    {
                        try
                        {
                            VaultUtils.HandleNetworkErrors(() =>
                            {
                                FileInfo fileInfo = new FileInfo(filePathReadOnly.LocalFileName);
                                fileInfo.IsReadOnly = false;
                            }, networkRetries);
                        }
                        catch (Exception ex)
                        {
                            failedFiles.Add(filePathReadOnly.File.Name);
                            int i = 0;
                            //  throw ex;
                        }
                    }
                }
            }
            logger.IfSomeDo(l => l.Trace("Finished downloading " + files.Count + " files."));
          //  foreach (Tuple<ADSK.File, string, bool, bool> fileToDownload in files)
               // DeleteSpecialVaultFolder(fileToDownload.Item2, networkRetries);
            logger.IfSomeDo(l => l.Trace("Finished deleting special vault folder."));

            return failedFiles;
        }
        /***********************************************************************************************/
        public static List<string> DownloadFilesWithRetries(Vault.Currency.Connections.Connection connection, List<VaultEagleLib.Model.DataStructures.DownloadFile> files, int networkRetries)
        {
            List<string> failedFiles = new List<string>();
            for (int i = 0; i < networkRetries; ++i)
            {
                AcquireFilesResults results;
                try
                {
                    results = MCADCommon.VaultCommon.FileOperations.DownloadFilesToPaths(connection, files.Select(f => new Tuple<Autodesk.Connectivity.WebServices.File, string, bool, bool>(f.File, f.DownloadPath, f.Writable, f.Run)).ToList());
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                foreach (Vault.Results.FileAcquisitionResult result in results.FileResults)
                {
                    if (result.Status != FileAcquisitionResult.AcquisitionStatus.Success)
                    {
                        failedFiles.Add(result.Exception.Message +": "+result.File.EntityName+".");
                      //  if (result.Exception != null)
                           // throw result.Exception;
                    }
                }
                if (i == networkRetries - 1)
                    break;
                else if (failedFiles.Count > 0)
                {
                    Thread.Sleep(1000);
                    failedFiles.Clear();
                }
                else
                    break;
            }
            return failedFiles;

        }


        /*****************************************************************************************/
        #endregion
    }

    public class JsonCache<T> : IDisposable
    {
        protected string fileName;
        public bool consumeOnlyMode = true;
        protected bool suppressDispose = false;
        protected string name;
        protected Dictionary<string, T> cache = new Dictionary<string, T>();

        protected void WriteToFile()
        {
            using (var writer = new StreamWriter(System.IO.File.Open(fileName, FileMode.Create, FileAccess.Write)))
                new JsonSerializer() { Formatting = Formatting.Indented }
                    .Serialize(writer, cache);
        }

        protected void LoadFromFile()
        {
            try
            {
                using (var reader = new StreamReader(System.IO.File.Open(fileName, FileMode.Open, FileAccess.Read)))
                using (var jr = new JsonTextReader(reader))
                {
                    var c = new JsonSerializer().Deserialize<Dictionary<string, T>>(jr);
                    if (c != null)
                        cache = c;
                }
            }
            catch (FileNotFoundException) { } // leave cache empty
            catch (Exception) { } // leave cache empty
        }

        public void Clear() {
            cache.Clear();
        }

        public void Dispose()
        {
            if (suppressDispose)
                return;
            if (!consumeOnlyMode)
                WriteToFile();
        }
    }



    public class FunctionJsonCacheEntry
    {
        public string returnValue;
        public DateTime accessTime;
    }

    public class FunctionJsonCache : JsonCache<FunctionJsonCacheEntry>, IDisposable
    {
        public static JsonSerializerSettings settings = new JsonSerializerSettings() 
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Error,
            Converters = new JsonConverter[] 
            {
                new Newtonsoft.Json.Converters.StringEnumConverter()
            }
        };

        public FunctionJsonCache(string fileName = null)
        {
            consumeOnlyMode = false;
            this.fileName = fileName ?? FilesAndFolders.GetDefaultFunctionJsonCachePath();
            LoadFromFile();

            var invalidEntries = (from entry in cache
                                  where entry.Value == null || string.IsNullOrWhiteSpace(entry.Key) || Math.Abs((DateTime.Now - entry.Value.accessTime).TotalDays) > 60
                                  select entry).ToArray();
            foreach (var entry in invalidEntries)
                cache.Remove(entry.Key);

            var expiredEntries = (from entry in cache
                                  where (DateTime.Now - entry.Value.accessTime).TotalDays > 7
                                  select entry).ToArray();
            foreach (var entry in expiredEntries)
                cache.Remove(entry.Key);
        }

        private bool ContainsKey(string key)
        {
            return cache.ContainsKey(key);
        }

        private TResult GetFromCache<F, TResult>(F f, Func<F, TResult> performCall, string key, bool skipCache = false, Func<TResult, bool> dontCachePredicate = null)
        {
            if (!skipCache && cache.ContainsKey(key))
            {
                var entry = cache[key];
                entry.accessTime = DateTime.Now;
                try
                {
                    return JsonConvert.DeserializeObject<TResult>(entry.returnValue, settings);
                }
                catch { }
            }

            var returnValue = performCall(f);
            if (!(dontCachePredicate != null && dontCachePredicate(returnValue)))
            {
                cache[key] = new FunctionJsonCacheEntry()
                    {
                        accessTime = DateTime.Now,
                        returnValue = JsonConvert.SerializeObject(returnValue, settings)
                    };
            }

            return returnValue;
        }

        private static string GetKey(string name, object[] args)
        {
            string key = JsonConvert.SerializeObject(new {name, args}, settings).Replace("'", "''").Replace('"', '\'');
            return key;
        }

        public class CachedFunction<T, TResult>
        {
            private FunctionJsonCache cache;
            private Func<T, TResult> funcToCache;
            private string name;
            private Func<TResult, bool> dontCachePredicate;

            public CachedFunction(FunctionJsonCache cache, Func<T, TResult> funcToCache, string name, Func<TResult, bool> dontCachePredicate = null)
            {
                this.cache = cache;
                this.funcToCache = funcToCache;
                this.name = name;
                this.dontCachePredicate = dontCachePredicate;
            }

            public TResult PerformCall(T t, string key = null, bool skipCache = false)
            {
                key = key ?? GetKey(t);
                return cache.GetFromCache(funcToCache, f => f(t), key, skipCache, dontCachePredicate);
            }

            public bool IsCallCached(string key)
            {
                return cache.ContainsKey(key);
            }

            public string GetKey(T t)
            {
                return FunctionJsonCache.GetKey(name, new object[] { t });
            }
        }

        public Func<T, TResult> GetCachedCall<T, TResult>(Func<T, TResult> funcToCache, string name)
        {
            return a => GetFromCache(funcToCache, f => f(a), GetKey(name, new object[] { a }));
        }

        public Func<T, TResult> GetCachingFunction<T, TResult>(Func<T, TResult> funcToCache, string name)
        {
            return a => GetFromCache(funcToCache, f => f(a), GetKey(name, new object[] { a }));
        }

        public CachedFunction<Tuple<T1, T2, T3, T4, T5>, TResult> GetCachingFunction<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> funcToCache, string name, Func<TResult, bool> skipCachePredicate = null)
        {
            return new CachedFunction<Tuple<T1, T2, T3, T4, T5>, TResult>(this, x => funcToCache(x.Item1, x.Item2, x.Item3, x.Item4, x.Item5), name, skipCachePredicate);
        }

        public T SimpleGet<T>(string key) where T : class
        {
            string encodedKey = JsonConvert.SerializeObject(new { key }, settings).Replace("'", "''").Replace('"', '\'');
            if (!cache.ContainsKey(encodedKey))
                return null;
            return JsonConvert.DeserializeObject<T>(cache[encodedKey].returnValue);
        }

        public void SimpleSet<T>(string key, T val) where T : class
        {
            string encodedKey = JsonConvert.SerializeObject(new { key }, settings).Replace("'", "''").Replace('"', '\'');
            cache[encodedKey] = new FunctionJsonCacheEntry()
            {
                accessTime = DateTime.Now,
                returnValue = JsonConvert.SerializeObject(val, settings)
            };
        }
    }

}
