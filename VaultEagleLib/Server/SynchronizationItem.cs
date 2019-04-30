using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.DotNet.Extensions;
using Autodesk.Connectivity.WebServices;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Connections;
using System.Text.RegularExpressions;
namespace VaultEagleLib
{
    public class SynchronizationItem
    {
        bool DownloadToTemp = false;
        bool Force = false;
        private string TempPath;
        public SynchronizationItem() { }

        //Tacton constructor
        public SynchronizationItem(Model.TacTon.Component input, string path, List<string> excludes, string[] patternsToSynchronize, bool writable, bool force, bool tempDownload, string output)
        {
            TempFilesAndFinalPath = new List<Tuple<string, string>>();
            TacTonPath = "";
            SearchPath = input.Properties.First().Value;
            VaultPath = path;
            RemoveDoubleDots();
            Excludes = excludes;

            PatternsToSynchronize = patternsToSynchronize;
            DownloadOnState = new List<string>();
            InvalidStates = new string[] { };
            SkipFolders = new string[] { };
            Writable = writable;
            Recursive = false;
            Force = force;//FetchBool(item, "-f|");
            DownloadToTemp = tempDownload;
            if (DownloadToTemp)
                TempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());

            Output = output;

            RunPatterns = new string[] { };
            Deletes = new string[] { };
            // syncItem.LockedFiles = FetchItems(item, "-lf|").ToArray();
            Mirrors = new string[] { };
            FolderMappings = new Tuple<string, string>[] { };
            MirrorsToFiles = new List<Tuple<string, List<File>>> { };

            GetChildren = true; //Tacton children should be downloaded.

            RecursiveMirrors = new List<string>();
            ComponentName = input.Name.AsOption();

        }

        public Option<string> ComponentName { get; private set; }
        public string[] Deletes { get; private set; }
        public List<string> DownloadOnState { get; private set; }
        public Tuple<string, string>[] FolderMappings { get; private set; }
        public bool GetChildren { get; private set; }
        public string[] InvalidStates { get; private set; }
        // public string[] LockedFiles { get; private set; }
        public string[] Mirrors { get; private set; }

        public List<Tuple<string, List<File>>> MirrorsToFiles { get; private set; }
        public string Output { get; private set; }
        public string[] PatternsToSynchronize { get; private set; }
        public bool Recursive { get; private set; }
        public List<string> RecursiveMirrors { get; private set; }
        public string[] RunPatterns { get; private set; }
        public string SearchPath { get; private set; }
        public string[] SkipFolders { get; private set; }
        public string TacTonPath { get; private set; }
        public List<Tuple<string, string>> TempFilesAndFinalPath { get; private set; }
        public string VaultPath { get; private set; }
        public bool Writable { get; private set; }
        List<string> Excludes { get; set; }
        List<Folder> FoldersToCreate { get; set; }
        // -i $/Designs -e Customers/customer1,test.txt -p .ipt,.iam,.txt -s all -w -d $/Designs/Customers/Block Customer/Blocks.iam -lf test.exe,test2.exe -fm source=$designs/test;target=E:\project,source=$designs/test2;target=E:\project2 
        public static SynchronizationItem ReadString(string item)
        {
            SynchronizationItem syncItem = new SynchronizationItem();
            syncItem.TempFilesAndFinalPath = new List<Tuple<string, string>>();
            syncItem.TacTonPath = FetchString(item, "-TacF|");
            syncItem.VaultPath = FetchString(item, "-i|");

            if (String.IsNullOrWhiteSpace(syncItem.VaultPath))
                syncItem.VaultPath = "$";

            syncItem.Excludes = new List<string>();
            /*syncItem.Excludes*/List<string> tempExcludes = FetchItems(item, "-e|");
            foreach (string tempExclude in tempExcludes)
                syncItem.Excludes.Add(Regex.Replace(tempExclude, @"\t|\n|\r", ""));

            syncItem.PatternsToSynchronize = FetchItems(item, "-p|").ToArray();
            syncItem.DownloadOnState = FetchItems(item, "-s|");//FetchDownloadOnState(item);
            syncItem.InvalidStates = FetchItems(item, "-is|").ToArray();
            syncItem.SkipFolders = FetchItems(item, "-sf|").ToArray();
            syncItem.Writable = FetchBool(item, "-w|");
            syncItem.Recursive = FetchBool(item, "-r|");
            syncItem.Force = FetchBool(item, "-f|");
            syncItem.DownloadToTemp = FetchBool(item, "-lck|");
            if (syncItem.DownloadToTemp)
                syncItem.TempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            
            syncItem.Output = Environment.ExpandEnvironmentVariables(FetchString(item, "-o|"));
            if (!String.IsNullOrWhiteSpace(syncItem.Output) && !syncItem.Output.EndsWith("\\"))
                syncItem.Output += "\\";
            syncItem.RunPatterns = FetchItems(item, "-run|").ToArray();
            syncItem.Deletes = AddVaultRootToStrings(FetchItems(item, "-d|").ToList(), syncItem.VaultPath).ToArray();
           // syncItem.LockedFiles = FetchItems(item, "-lf|").ToArray();
            syncItem.Mirrors = AddVaultRootToStrings(FetchItemsWithRoot(item, "-m|", syncItem.VaultPath).ToList(), syncItem.VaultPath).ToArray();
            syncItem.FolderMappings = FetchFolderMappings(item, syncItem.VaultPath);
            syncItem.MirrorsToFiles = new List<Tuple<string, List<File>>> { };

            foreach (string mirror in syncItem.Mirrors)
                syncItem.MirrorsToFiles.Add(new Tuple<string, List<File>>(mirror, new List<File>()));

            syncItem.RecursiveMirrors = new List<string>();
            if (syncItem.Recursive)
                syncItem.RecursiveMirrors.AddRange(syncItem.Mirrors);

            syncItem.SearchPath = syncItem.VaultPath;
            syncItem.ComponentName = Option.None;
            return syncItem;
        }

        public static SynchronizationItem[] UpdateListWithTactonItems(SynchronizationItem[] currentItems)
        {
            List<SynchronizationItem> items = new List<SynchronizationItem>();
            foreach (SynchronizationItem item in currentItems)
            {
                List<Model.TacTon.Component> tactonFiles = item.ReadTactonPath();
                if (tactonFiles.Count > 0)
                {
                    foreach (Model.TacTon.Component component in tactonFiles)
                        items.Add(new SynchronizationItem(component, item.VaultPath, item.Excludes.ToList(), item.PatternsToSynchronize.ToArray(), item.Writable, item.Force, item.DownloadToTemp, item.Output));

                }
                else
                    items.Add(item);
            }
            return items.ToArray();

        }

        public void AddMirrorFolders(List<Folder> folders)
        {
            FoldersToCreate = folders;
        }

        public void CreateEmptyFolders(string localPath, int retries)
        {
            if (Mirrors.Count() > 0)
            {
                foreach (Folder folder in FoldersToCreate)
                {
                    if (VaultEagle.VaultUtils.HandleNetworkErrors(() => !System.IO.Directory.Exists(GetLocalFolder(folder.FullName, localPath)), retries))
                        VaultEagle.VaultUtils.HandleNetworkErrors(() => System.IO.Directory.CreateDirectory(GetLocalFolder(folder.FullName, localPath)), retries);
                }
            }
        }

        public void DeleteFiles(string localPath, int retries)
        {
            foreach (string delete in Deletes)
            {
                try
                {
                    string localPathN = localPath;
                    if (!String.IsNullOrWhiteSpace(Output))
                    {
                        localPathN = Output;
                        if (!localPathN.EndsWith("\\"))
                            localPathN += "\\";
                    }
                    if (VaultEagle.VaultUtils.HandleNetworkErrors(() => System.IO.File.Exists(localPathN + delete), retries))
                        DeleteFile(localPathN + delete, retries);
                    else if (VaultEagle.VaultUtils.HandleNetworkErrors(() => System.IO.Directory.Exists(localPathN + delete), retries))
                        DeleteFolder(localPathN + delete, retries);
                    else if (VaultEagle.VaultUtils.HandleNetworkErrors(() => System.IO.File.Exists(delete), retries))
                        DeleteFile(delete, retries);
                    else if (VaultEagle.VaultUtils.HandleNetworkErrors(() => System.IO.Directory.Exists(delete), retries))
                        DeleteFolder(delete, retries);

                }
                catch { }
            }
        }

        public Model.DataStructures.DownloadItem[] GetFilesAndPathsForItem(Connection connection, List<Tuple<FileFolder, Option<string>>> fileAndFolders, List<File> addedFiles, string vaultRoot, bool allowChildren, int networkRetries)
        {
            List<string> skipFolder = new List<string>();
            VaultEagle.VaultCommunication com = new VaultEagle.VaultCommunication();
            com.InitializeFromConnection(connection);
            //  List<Tuple<File, string, bool, bool>> filesAndPaths = new List<Tuple<File, string, bool, bool>>();
            List<Model.DataStructures.DownloadItem> filesToDownload = new List<Model.DataStructures.DownloadItem>();
            foreach (Tuple<FileFolder, Option<string>> fileAndFolderComponent in fileAndFolders)
            {
                FileFolder fileAndFolder = fileAndFolderComponent.Item1;
                foreach (string fp in SkipFolders)
                {
                    if ((!skipFolder.Contains(fileAndFolder.Folder.FullName)) && (fileAndFolder.Folder.Cat.CatName.Equals(fp)))
                        skipFolder.Add(fileAndFolder.Folder.FullName);
                }
                if ((AbhereSynchronizationRules(fileAndFolder, Recursive, allowChildren)) && (!skipFolder.Contains(fileAndFolder.Folder.FullName)))
                {
                    if ((Mirrors.Where(m => fileAndFolder.Folder.FullName.ToLower().Contains(m.ToLower())).Count() > 0) && !(RecursiveMirrors.Where(m => fileAndFolder.Folder.FullName.Equals(m, StringComparison.InvariantCultureIgnoreCase)).Count() == 1))
                    {
                        RecursiveMirrors.Add(fileAndFolder.Folder.FullName);
                        MirrorsToFiles.Add(new Tuple<string, List<File>>(fileAndFolder.Folder.FullName, new List<File>()));
                    }
                    foreach (Tuple<string, List<File>> mirrorItem in MirrorsToFiles)
                    {
                        if (mirrorItem.Item1.Equals(fileAndFolder.Folder.FullName, StringComparison.InvariantCultureIgnoreCase))
                            mirrorItem.Item2.Add(fileAndFolder.File);
                    }
                    string localFilePath = GetLocalFolder(fileAndFolder.Folder.FullName + "/" + fileAndFolder.File.Name, vaultRoot);
                    // if (System.IO.Path.
                    string localPath = System.IO.Path.GetDirectoryName(localFilePath);
                    File file = null;
                    if ((DownloadOnState.Count > 0)/* && (DownloadOnState.Get().Equals("Released", StringComparison.InvariantCultureIgnoreCase))*/)
                    {
                        File releasedFile = null;
                        VaultEagle.VaultUtils.FileState state = VaultEagle.VaultUtils.GetLastRelevantFileState(fileAndFolder.File.VerNum, fileAndFolder.File.MasterId, connection, ref releasedFile, DownloadOnState, InvalidStates.ToList());
                        if (state == VaultEagle.VaultUtils.FileState.Ok)
                            file = releasedFile;
                        else if (state == VaultEagle.VaultUtils.FileState.Obsolete)
                        {
                            DeleteFile(localFilePath, networkRetries);
                            //Delete file?
                        }
                    }
                    else
                        file = fileAndFolder.File;
                    if (file == null)
                        continue;
                    if (VaultEagle.VaultUtils.HandleNetworkErrors(() => System.IO.File.Exists(localPath + "/" + fileAndFolder.File.Name), networkRetries))
                    {
                        if (file != null)
                        {
                            if (Force || file.Cksum != VaultEagle.VaultUtils.HandleNetworkErrors(() => { return VaultEagle.VaultCommunication.CalculateCrc32(localPath + "/" + fileAndFolder.File.Name); }, networkRetries))
                            {
                                if (!addedFiles.Contains(file))
                                {
                                    if (DownloadToTemp)
                                    {
                                        bool run = RunFile(file);
                                        TempFilesAndFinalPath.Add(new Tuple<string, string>(System.IO.Path.Combine(TempPath, file.Name), System.IO.Path.Combine(localPath, file.Name)));
                                        //filesAndPaths.Add(new Tuple<File, string, bool, bool>(PatternsToSynchronize.Count() == 1 && PatternsToSynchronize[0].Equals("/") ? null : file, TempPath, Writable, run));
                                        filesToDownload.Add(GetDownloadItem(TempPath, file, Writable, run, fileAndFolderComponent.Item2));
                                    }
                                    else
                                    {
                                        bool run = RunFile(file);
                                        //filesAndPaths.Add(new Tuple<File, string, bool, bool>(PatternsToSynchronize.Count() == 1 && PatternsToSynchronize[0].Equals("/") ? null : file, localPath, Writable, run));
                                        filesToDownload.Add(GetDownloadItem(localPath, file, Writable, run, fileAndFolderComponent.Item2));
                                    }
                                    addedFiles.Add(file);
                                }
                            }
                        }
                    }
                    else if (file != null)
                    {
                        if (!addedFiles.Contains(file))
                        {
                            if (DownloadToTemp)
                            {
                                bool run = RunFile(file);
                                TempFilesAndFinalPath.Add(new Tuple<string, string>(System.IO.Path.Combine(TempPath, file.Name), System.IO.Path.Combine(localPath, file.Name)));
                                //  filesAndPaths.Add(new Tuple<File, string, bool, bool>(PatternsToSynchronize.Count() == 1 && PatternsToSynchronize[0].Equals("/") ? null : file, TempPath, Writable, run));
                                filesToDownload.Add(GetDownloadItem(TempPath, file, Writable, run, fileAndFolderComponent.Item2));
                            }
                            else
                            {
                                bool run = RunFile(file);
                                //filesAndPaths.Add(new Tuple<File, string, bool, bool>(PatternsToSynchronize.Count() == 1 && PatternsToSynchronize[0].Equals("/") ? null : file, localPath, Writable, run));
                                filesToDownload.Add(GetDownloadItem(localPath, file, Writable, run, fileAndFolderComponent.Item2));
                            }
                            addedFiles.Add(file);
                        }
                    }


                }
                else if (!Recursive && fileAndFolder.Folder.FullName.Equals(VaultPath))
                    filesToDownload.Add(new Model.DataStructures.DownloadFolder(GetLocalFolder(VaultPath, vaultRoot)));
                //filesAndPaths.Add(new Tuple<File, string, bool, bool>(null, GetLocalFolder(VaultPath, vaultRoot), false, false));

            }
            UpdateMirrorToFiles(vaultRoot);
            return filesToDownload.ToArray();

        }

        public /*Tuple<File, string, bool, bool>*/Model.DataStructures.DownloadItem[] GetFoldersAndPathsForItem(List<Folder> folders, string vaultRoot)
        {
            // List<Tuple<File, string, bool, bool>> localFolders = new List<Tuple<File, string, bool, bool>>();
            List<Model.DataStructures.DownloadItem> localFolders = new List<Model.DataStructures.DownloadItem>();
            foreach (Folder f in folders)
            {
                string localFolder = GetLocalFolder(f.FullName, vaultRoot);
                localFolders.Add(new Model.DataStructures.DownloadFolder(localFolder));//new Tuple<File, string, bool, bool>(null, localFolder, false, false));

            }
            return localFolders.ToArray();
        }

        public void HandleLockedFiles(List<Tuple<File, string, bool, bool>> files, int retries, Option<MCADCommon.LogCommon.DisposableFileLogger> logger)
        {
            foreach (Tuple<File, string, bool, bool> file in files)
            {
                try
                {
                    // If an file already exist, remove it.
                    if (System.IO.File.Exists(System.IO.Path.Combine(file.Item2, file.Item1.Name)))
                    {
                        logger.IfSomeDo(l => l.Trace("Deleting old file."));

                        VaultEagle.VaultUtils.HandleNetworkErrors(() => { System.IO.File.SetAttributes(System.IO.Path.Combine(file.Item2, file.Item1.Name), System.IO.FileAttributes.Normal); }, retries);
                        string newLocalFilePath = System.IO.Path.Combine(VaultEagle.VaultUtils.HandleNetworkErrors(() => System.IO.Path.GetDirectoryName(System.IO.Path.Combine(file.Item2, file.Item1.Name)), retries), "To_Be_Deleted_" + file.Item1.Name);

                        if (new System.IO.FileInfo(newLocalFilePath).Exists)
                        {
                            VaultEagle.VaultUtils.HandleNetworkErrors(() => { System.IO.File.SetAttributes(newLocalFilePath, System.IO.FileAttributes.Normal); }, retries);
                            VaultEagle.VaultUtils.HandleNetworkErrors(() => { System.IO.File.Delete(newLocalFilePath); }, retries);
                        }

                        VaultEagle.VaultUtils.HandleNetworkErrors(() => { System.IO.File.Move(System.IO.Path.Combine(file.Item2, file.Item1.Name), newLocalFilePath); }, retries);
                        VaultEagle.VaultUtils.HandleNetworkErrors(() => { System.IO.File.Delete(newLocalFilePath); }, retries);
                        logger.IfSomeDo(l => l.Trace("Finished deleting old file."));
                    }
                }
                catch { }
            }
        }

        public bool HasIllegalItems(string vaultRoot)
        {
            if ((Mirrors.Count() == 0) && (Deletes.Count() == 0))
                return false;

            if (IsStringIllegal(vaultRoot))
                return true;

            foreach (string mirror in Mirrors)
            {
                string local = GetLocalFolder(mirror, vaultRoot);
                if (IsStringIllegal(local))
                    return true;
            }

            foreach (string delete in Deletes)
            {
                string local = delete;//GetLocalFolder(delete, vaultRoot);
                if (IsStringIllegal(local))
                    return true;
            }

            return false;
        }

        public void MirrorFolders(string localPath, int retries)
        {
            foreach (Tuple<string, List<File>> mirrorItem in MirrorsToFiles)
            {
                if (VaultEagle.VaultUtils.HandleNetworkErrors(() => System.IO.Directory.Exists(GetLocalFolder(mirrorItem.Item1, localPath)), retries))
                {
                    foreach (System.IO.FileInfo file in new System.IO.DirectoryInfo(GetLocalFolder(mirrorItem.Item1, localPath)).GetFiles())
                    {
                        if (!mirrorItem.Item2.Any(i => i.Name.Equals(file.Name, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            if (VaultEagle.VaultUtils.HandleNetworkErrors(() => System.IO.File.Exists(file.FullName), retries))
                                VaultEagle.VaultCommunication.DeleteFileWithRetries(file.FullName, retries);
                        }
                    }
                    foreach (System.IO.DirectoryInfo directory in new System.IO.DirectoryInfo(GetLocalFolder(mirrorItem.Item1, localPath)).GetDirectories())
                    {
                        if (!MirrorsToFiles.Any(i => GetLocalFolder(i.Item1, localPath).Equals(directory.FullName, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            if (VaultEagle.VaultUtils.HandleNetworkErrors(() => System.IO.Directory.Exists(directory.FullName), retries))
                                VaultEagle.VaultCommunication.DeleteFolder(directory.FullName, retries);
                        }
                    }
                }
            }
        }

        public List<Model.TacTon.Component> ReadTactonPath()
        {
            List<Model.TacTon.Component> extraFiles = new List<Model.TacTon.Component>();
            if (!String.IsNullOrWhiteSpace(TacTonPath))
                extraFiles.AddRange(Model.TacTon.TactonConfiguration.ReadVaultFilesFromTactonFile(TacTonPath, VaultPath));

            List<Model.TacTon.Component> drawings = extraFiles.Where(e => e.Properties.ContainsKey("da_drawing")).ToList();

            List<Model.TacTon.Component> assemblies = extraFiles.Where(e => e.Properties.ContainsKey("da_assembly")).ToList();
            List<Model.TacTon.Component> others = extraFiles.Where(e => e.Properties.ContainsKey("da_document")).ToList();
            List<Model.TacTon.Component> tactonFiles = new List<Model.TacTon.Component>();
            tactonFiles.AddRange(drawings);
            tactonFiles.AddRange(assemblies);
            tactonFiles.AddRange(others);
            return tactonFiles;
        }

        public void RunFiles(string vaultRoot, List<VaultEagleLib.Model.DataStructures.DownloadFile> filesAndFolders, int retries)
        {
            foreach (VaultEagleLib.Model.DataStructures.DownloadFile fileAndFolder in filesAndFolders)
            {
                if (fileAndFolder.Run)
                {
                    string extension = System.IO.Path.GetExtension(fileAndFolder.File.Name);
                    if (RunPatterns.Contains(extension))
                    {
                        VaultEagle.VaultUtils.HandleNetworkErrors(() =>
                        {
                            System.Diagnostics.Process proc = new System.Diagnostics.Process();
                            proc.StartInfo.FileName = fileAndFolder.LocalFileName;
                            proc.Start();
                            proc.WaitForExit();
                        }, retries);
                    }
                }
            }
        }

        private static List<string> AddVaultRootToStrings(List<string> strings, string input)
        {
            List<string> stringsNew = new List<string>();
            foreach (string str in strings)
            {
                string source = str;
                /* if ((source.Length > 0) && (!source[0].Equals('$')))
                 {
                     if (input[input.Length - 1].Equals('/') && source[0].Equals('/'))
                         source = input.Substring(0, input.Length - 1) + '/' + source.Substring(1);
                     else if (input[input.Length - 1].Equals('/'))
                         source = input.Substring(0, input.Length - 1) + '/' + source;
                     else if (source[0].Equals('/'))
                         source = input + '/' + source.Substring(1);
                     else
                         source = input + '/' + source;    
                 }*/
                stringsNew.Add(Environment.ExpandEnvironmentVariables(source));
            }
            return stringsNew;
        }

        private static bool FetchBool(string item, string tag)
        {
            int indexOfWritable = item.IndexOf(tag);
            if (indexOfWritable >= 0)
                return true;
            else
                return false;
        }

        private static Option<string> FetchDownloadOnState(string item)
        {
            int indexOfDownloadOnState = item.IndexOf("-s|");
            if (indexOfDownloadOnState >= 0)
            {
                string subString = item.Substring(indexOfDownloadOnState + 3);
                int endIndexOfDownloadOnState = subString.IndexOf("|");
                string downloadOnState;
                if (endIndexOfDownloadOnState >= 0)
                {
                    string subString2 = subString.Substring(0, endIndexOfDownloadOnState);
                    int lastIndexOfDownloadOnState = subString2.LastIndexOf('-');

                    if (lastIndexOfDownloadOnState >= 0)
                        downloadOnState = subString.Substring(0, /*endIndexOfDownloadOnState*/ lastIndexOfDownloadOnState);
                    else
                        downloadOnState = subString.Substring(0);

                }
                else
                    downloadOnState = subString.Substring(0);
                downloadOnState = downloadOnState.Trim();
                return downloadOnState.AsOption();
            }
            else
                return Option.None;

        }

        private static Tuple<string, string>[] FetchFolderMappings(string item, string input)
        {
            int indexOfFolderMappings = item.IndexOf("-fm|");
            if (indexOfFolderMappings >= 0)
            {
                List<Tuple<string, string>> folderMappings = new List<Tuple<string, string>>();
                List<string> folderMappingsStrings = FetchItems(item, "-fm|");
                foreach (string mapping in folderMappingsStrings)
                {
                    string[] sourceAndTarget = mapping.Split(';');
                    string source = sourceAndTarget[0].Replace("source=", "").Trim();
                    if ((source.Length > 0) && (!source[0].Equals('$')))
                    {
                        if (input[input.Length - 1].Equals('/') && source[0].Equals('/'))
                            source = input.Substring(0, input.Length - 1) + '/' + source.Substring(1);
                        else if (input[input.Length - 1].Equals('/'))
                            source = input.Substring(0, input.Length - 1) + '/' + source;
                        else if (source[0].Equals('/'))
                            source = input + '/' + source.Substring(1);
                        else
                            source = input + '/' + source;
                    }
                    folderMappings.Add(new Tuple<string, string>(source, Environment.ExpandEnvironmentVariables(sourceAndTarget[1].Replace("target=", "").Trim())));
                }
                return folderMappings.ToArray();
            }
            else
                return new Tuple<string, string>[] { };
        }

        private static List<string> FetchItems(string item, string tag)
        {
            int index = item.IndexOf(tag);
            if (index >= 0)
            {
                string subString = item.Substring(index + tag.Length);
                int endIndex = subString.IndexOf('|');
                string syncItem;
                if (endIndex >= 0)
                {
                    string subString2 = subString.Substring(0, endIndex);
                    int lastIndex = subString2.LastIndexOf('-');
                    if (lastIndex >= 0)
                        syncItem = subString.Substring(0, /*endIndex*/lastIndex);
                    else
                        syncItem = subString.Substring(0);

                }
                else
                    syncItem = subString.Substring(0);
                syncItem = syncItem.Trim();
                return syncItem.Split(',').ToList();
            }
            else
                return new List<string>();
        }

        private static List<string> FetchItemsWithRoot(string item, string tag, string root)
        {
            int index = item.IndexOf(tag);
            if (index >= 0)
            {
                string subString = item.Substring(index + tag.Length);
                int endIndex = subString.IndexOf('|');
                string syncItem;
                if (endIndex >= 0)
                {
                    string subString2 = subString.Substring(0, endIndex);
                    int lastIndex = subString2.LastIndexOf('-');
                    if (lastIndex >= 0)
                        syncItem = subString.Substring(0, /*endIndex*/lastIndex);
                    else
                        syncItem = subString.Substring(0);

                }
                else
                    syncItem = subString.Substring(0);
                syncItem = syncItem.Trim();
                //   if (String.IsNullOrWhiteSpace(syncItem))
                // syncItem = root;
                List<string> strings = syncItem.Split(',').ToList();
                List<string> strings2 = new List<string>();
                foreach (string str in strings)
                {
                    if (String.IsNullOrWhiteSpace(str) && !strings2.Contains(root))
                        strings2.Add(root);
                    else
                        strings2.Add(str);
                }
                return strings2;
            }
            else
                return new List<string>();
        }

        private static string FetchString(string item, string tag)
        {
            int indexOfInclude = item.IndexOf(tag);
            if (indexOfInclude >= 0)
            {
                string subString = item.Substring(indexOfInclude + tag.Length);
                int endIndexOfInclude = subString.IndexOf('|');
                string includePath;
                if (endIndexOfInclude >= 0)
                {
                    string substring2 = subString.Substring(0, endIndexOfInclude);
                    int lastIndex = substring2.LastIndexOf('-');
                    if (lastIndex >= 0)
                        includePath = subString.Substring(0, /*endIndexOfInclude*/lastIndex);
                    else
                        includePath = subString.Substring(0);
                }
                else
                    includePath = subString.Substring(0);
                return includePath.Trim();
            }
            else
                return "";
        }

        private static bool IsStringIllegal(string path)
        {
            try
            {
                Uri PathUri = new Uri(path);
                if (path.Equals(Environment.ExpandEnvironmentVariables("%AppData%")) || path.Equals(Environment.ExpandEnvironmentVariables("%ProgramData%")) || path.Equals(Environment.ExpandEnvironmentVariables("%LocalAppData%")) || path.Equals(Environment.ExpandEnvironmentVariables("%ProgramFiles%")) || path.Equals(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%")) || path.EndsWith(":\\") || PathUri.IsUnc)
                    return true;
                else
                    return false;
            }
            catch
            {
                if (path.Equals(Environment.ExpandEnvironmentVariables("%AppData%")) || path.Equals(Environment.ExpandEnvironmentVariables("%ProgramData%")) || path.Equals(Environment.ExpandEnvironmentVariables("%LocalAppData%")) || path.Equals(Environment.ExpandEnvironmentVariables("%ProgramFiles%")) || path.Equals(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%")) || path.EndsWith(":\\"))
                    return true;
                else
                    return false;
            }
        }

        private static string RemoveDoubleDot(string input)
        {

            return input;
        }
        /************************************************************************************************************/
        /************************************************************************************************************/
        private bool AbhereSynchronizationRules(FileFolder file, bool recursive, bool children)
        {
            if (!children && !recursive && !(file.Folder.FullName.Equals(VaultPath, StringComparison.InvariantCultureIgnoreCase) || (file.Folder.FullName + "/" + file.File.Name).Equals(VaultPath, StringComparison.InvariantCultureIgnoreCase)))
                return false;

            if (!children && !(file.Folder.FullName + "/" + file.File.Name).ToLower().Contains(VaultPath.ToLower()))
                return false;

            if (((PatternsToSynchronize.Count() > 0) && (!PatternsToSynchronize.Any(s => file.File.Name.ToLower().EndsWith(s.ToLower())))) && !((PatternsToSynchronize.Count() == 1) && (PatternsToSynchronize[0].Equals("/", StringComparison.InvariantCultureIgnoreCase))))
                return false;

            if ((Excludes.Count() > 0) && (Excludes.Any(s => (file.Folder.FullName + "/" + file.File.Name).ToLower().Contains(s.ToLower()))))
                return false;

            return true;
        }

        private void DeleteFile(string fileToDelete, int networkRetries, string localFolder = "")
        {
            string filePath;
            if (fileToDelete.StartsWith("$"))
                filePath = System.IO.Path.Combine(localFolder, System.IO.Path.GetFileName(fileToDelete));
            else
                filePath = fileToDelete;

            if (VaultEagle.VaultUtils.HandleNetworkErrors(() => System.IO.File.Exists(filePath), networkRetries))
                VaultEagle.VaultCommunication.DeleteFileWithRetries(filePath, networkRetries);
        }

        private void DeleteFolder(string folder, int networkRetries, string localFolder = "")
        {
            System.IO.DirectoryInfo dir;
            if (String.IsNullOrWhiteSpace(localFolder))
                dir = new System.IO.DirectoryInfo(folder);
            else
                dir = new System.IO.DirectoryInfo(localFolder);

            foreach (System.IO.FileInfo file in VaultEagle.VaultUtils.HandleNetworkErrors(() => { return dir.GetFiles(); }, networkRetries))
                DeleteFile(file.FullName, networkRetries, System.IO.Path.GetDirectoryName(file.FullName));

            foreach (System.IO.DirectoryInfo directory in VaultEagle.VaultUtils.HandleNetworkErrors(() => { return dir.GetDirectories(); }, networkRetries))
                DeleteFolder(directory.FullName, networkRetries);

            VaultEagle.VaultUtils.HandleNetworkErrors(() => { System.IO.Directory.Delete(dir.FullName); }, networkRetries);
        }

        private Model.DataStructures.DownloadItem GetDownloadItem(string downloadPath, File file, bool writable, bool run, Option<string> componentName)
        {
            if (PatternsToSynchronize.Count() == 1 && PatternsToSynchronize[0].Equals("/"))
                return new Model.DataStructures.DownloadFolder(downloadPath);
            else if (componentName.IsSome)
                return new Model.DataStructures.TacTon.TactonDownloadFile(file, downloadPath, writable, run, componentName.Get());
            else
                return new Model.DataStructures.DownloadFile(file, downloadPath, writable, run);
        }

        private string GetLocalFolder(string vaultFolderPath, string localPath)
        {
            string local = localPath;
            string vaultFolder;
            if (!String.IsNullOrWhiteSpace(Output))
            {
                local = Output;
                string subFolder;
                if (vaultFolderPath.Equals(VaultPath, StringComparison.InvariantCultureIgnoreCase))
                    subFolder = "";//System.IO.Path.GetFileName(vaultFolderPath);
                else if (VaultPath.EndsWith("/"))
                    subFolder = vaultFolderPath.Substring(VaultPath.Length).Replace('/', '\\');
                else
                    subFolder = vaultFolderPath.Substring(VaultPath.Length + 1).Replace('/', '\\');
                vaultFolder = System.IO.Path.Combine(Output, subFolder);
            }
            else
                vaultFolder = local + System.IO.Path.Combine(vaultFolderPath.Substring(1)).Replace('/', '\\');
            foreach (Tuple<string, string> folderMapping in FolderMappings)
            {
                if (folderMapping.Item1.Equals(vaultFolderPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    vaultFolder = folderMapping.Item2;
                    if (!vaultFolder.EndsWith("\\"))
                        vaultFolder += "\\";
                }
                else if (vaultFolderPath.StartsWith(folderMapping.Item1, StringComparison.InvariantCultureIgnoreCase))
                    vaultFolder = folderMapping.Item2 + "\\" + vaultFolderPath.Substring(folderMapping.Item1.Count() + 1).Split("/").StringJoin("\\");
            }

            return vaultFolder;
        }

        private void RemoveDoubleDots()
        {
            if (SearchPath.Contains("/.."))
            {
                string beforeDoubleDots = SearchPath.Substring(0, SearchPath.IndexOf("/.."));
                int numberOfPossibleStepsUp = Regex.Matches(beforeDoubleDots, "/").Count;
                int numberOfDoubleDots = Regex.Matches(SearchPath, @"/\.\.").Count;
                if (numberOfPossibleStepsUp <= numberOfDoubleDots)
                {
                    string afterDoubleDots = SearchPath.Substring(SearchPath.LastIndexOf("/..") + 3);
                    while (numberOfDoubleDots > 0)
                    {
                        if (beforeDoubleDots.LastIndexOf('/') >= 0)
                            beforeDoubleDots = beforeDoubleDots.Substring(0, beforeDoubleDots.LastIndexOf("/"));
                        else
                            break;
                        numberOfDoubleDots--;
                    }
                    VaultPath = beforeDoubleDots;
                    SearchPath = VaultPath + afterDoubleDots;
                }
            }
        }

        private bool RunFile(File file)
        {
            string extension = System.IO.Path.GetExtension(file.Name);
            bool run = false;
            if (RunPatterns.Contains(extension))
                run = true;
            return run;
        }      

        /****************************************************************************************************************************/
        /****************************************************************************************************************************/
        private void UpdateMirrorToFiles(string vaultRoot)
        {
            //string localRoot = GetLocalFolder(VaultPath, vaultRoot);
            List<string> emptyMirrorFolders = new List<string>();
            foreach (Tuple<string, List<File>> mirrorFolders in MirrorsToFiles)
            {
                string folderName = /*GetLocalFolder(*/mirrorFolders.Item1/*, vaultRoot)*/;
                while (true)
                {
                    if (!(MirrorsToFiles.Exists(t => folderName.Equals(t.Item1))) && (folderName.Length > VaultPath.Length) && !emptyMirrorFolders.Contains(folderName))
                        emptyMirrorFolders.Add(folderName);

                    folderName = System.IO.Path.GetDirectoryName(folderName).Replace("\\", "/");
                    if (String.IsNullOrWhiteSpace(folderName))
                        break;
                }
            }
            foreach (string emptyMirrorFolder in emptyMirrorFolders)
                MirrorsToFiles.Add(new Tuple<string, List<File>>(emptyMirrorFolder, new List<File>() { }));
        }

        /****************************************************************************************************************************/
        /*************************************************************************************************************************/
        /***********************************************************************************************************************/
        /***************************************************************************************************/
        /***************************************************************************************************/
        /***************************************************************************************************/
        /***************************************************************************************************/
        /***************************************************************************************************/
        /***************************************************************************************************/
        /******************************* Static Functions **************************************************/
        /************************************************************************************************/
    }
}
