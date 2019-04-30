using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Common.DotNet.Extensions;

namespace VaultEagle
{
    public enum SyncState { Exclude, Include, IncludeOnlyFolders, IncludeOnlyFiles, IncludeOnlyDirectChildFolders, IncludeSingleFolder, FromParent }

    public class SynchronizationTree
    {
        public class SyncInfo
        {
            public SyncState State;
            public long LastVaultId = -1;
            public string LocalPath = null;
            public static bool IsFolder(string path)
            {
                return path.EndsWith("/");
            }
        }

        public class VaultEagleConfig
        {
            public Dictionary<string, SortedDictionary<string, SyncInfo>> Vaults = EmptyForest;
            public string ConfigVersion = "2.0";
            public bool OverwriteLocallyModifiedFiles = true;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this).Replace('"','\'');
        }

        private static SyncState[] specialFolderSyncStates = 
                    {
                        SyncState.IncludeOnlyFolders, SyncState.IncludeOnlyFolders, SyncState.IncludeSingleFolder, SyncState.IncludeOnlyFiles, SyncState.IncludeOnlyDirectChildFolders, 
                    };

        public enum ImageIndex
        {
            FileExplicitlyExclude, // red cross
            FileExplicitlyInclude, // bright add
            FolderExplicitlyExclude, // red cross
            FolderExplicitlyInclude, // bright add
            FolderImplicitlyExclude, // gray
            FolderImplicitlyInclude, // bright
            FolderExplicitlyIncludeSpecial, // bright
            FileError, // bright
            FolderError, // bright
        }

        public Dictionary<string, SyncInfo> ExplicitPaths = new Dictionary<string, SyncInfo>(StringComparer.OrdinalIgnoreCase);

        public string VaultName = "";
        public string VaultURI = "";
        public VaultEagleConfig Config = new VaultEagleConfig();

        public SynchronizationTree()
        {
        }

        public SynchronizationTree(string vaultName, string vaultUri)
        {
            VaultName = vaultName;
            VaultURI = vaultUri;
        }

        public bool IsEmpty()
        {
            return ExplicitPaths.Count == 0;
        }

        public static SynchronizationTree ReadTree(string vaultName, string vaultURI, bool tryHarder = false)
        {
            var newTree = new SynchronizationTree(vaultName, vaultURI);

            string vaultId = GetVaultId(vaultName, vaultURI);
            VaultEagleConfig config = ReadConfig(vaultName, vaultURI);
            newTree.Config = config;
            var treeByVaultMap = config.Vaults;
            try
            {
                if (!treeByVaultMap.ContainsKey(vaultId))
                    if (tryHarder)
                        vaultId = TryToFindMovedOrRenamedVaultId(vaultName, vaultURI, treeByVaultMap) ?? vaultId;
            }
            catch (Exception) { }
            if (treeByVaultMap.ContainsKey(vaultId))
                newTree.ExplicitPaths = treeByVaultMap[vaultId].ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

            return newTree;
        }

        public static VaultEagleConfig ReadConfig(string vaultName, string vaultURI, bool getEmptyIfFailed = false)
        {
            string treePath = FilesAndFolders.GetConfigPath();
            if (!File.Exists(treePath))
                return new VaultEagleConfig();

            try
            {
                using (var stream = new FileStream(treePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(stream, Encoding.UTF8))
                    return ParseForest(sr, vaultName, vaultURI) ?? new VaultEagleConfig();
            }
            catch (Exception ex)
            {
                if (getEmptyIfFailed)
                    return new VaultEagleConfig();
#if DEBUG
                throw new Exception("Failed to read configuration file at: '" + treePath + "'. Error: " + ex.Message, ex);
#else
                throw new Exception("Failed to read configuration file at: '" + treePath + "'.", ex);
#endif
            }
        }

        private static Dictionary<string, SortedDictionary<string, SyncInfo>> EmptyForest
        {
            get { return new Dictionary<string, SortedDictionary<string, SyncInfo>>(); }
        }

        public static VaultEagleConfig ParseForest_2_0(JObject input)
        {
            return input.ToObject<VaultEagleConfig>(new JsonSerializer());
        }
        
        public static VaultEagleConfig ParseForest(TextReader input, string vaultName, string vaultURI)
        {
            var data = input.ReadToEnd();
            var stringReader = new StringReader(data);
            var configConversionContext = new Configs.ConfigConversionContext {VaultName = vaultName, VaultURI = vaultURI};
            if (data.StartsWith("{") || data.Contains("{")) // possibly json
            {
                var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(data);
                var configVersion = (string)jsonObject["ConfigVersion"];

                switch (configVersion)
                {
                    case null:
                        var configV10 = Configs.VaultEagleConfigV1_0.ParseForest_1_0(new StringReader(data));
                        if (configV10 == null)
                            return null;
                        return configV10.UpgradeToLatest(configConversionContext);
                        //return new VaultEagleConfig { Vaults = forest10.ToDictionary(vaultKv => vaultKv.Key, vaultKv => vaultKv.Value.ToDictionary(kv => kv.Key, kv => new SyncInfo { State = kv.Value["State"] }).ToSortedDictionary(), StringComparer.OrdinalIgnoreCase) };
                    case "1.1":
                        var forest11 = Configs.VaultEagleConfigV1_1.ParseForest_1_1(jsonObject).UpgradeToLatest(configConversionContext);
                        return forest11;
                    case "2.0":
                        var forest20 = ParseForest_2_0(jsonObject);
                        return forest20;
                }
            }
            if (data.Contains("+$") || data.Contains("-$")) // possibly first format
            {
                var configV05 = Configs.VaultEagleConfigV0_5.ParseTree_Old(stringReader);
                return configV05.UpgradeToLatest(configConversionContext);
            }
            return new VaultEagleConfig();
        }

        private static string TryToFindMovedOrRenamedVaultId(string vaultName, string vaultURI, Dictionary<string, SortedDictionary<string, SyncInfo>> treeByVaultMap)
        {
            var matchingVaultIdsByURI = treeByVaultMap.Keys.Where(vId => vId.EndsWith('@'+vaultURI.ToLowerInvariant()));
            var matchingVaultIdsByVaultName = treeByVaultMap.Keys.Where(vId => vId.StartsWith(vaultName.ToLowerInvariant()+'@'));
            return matchingVaultIdsByURI.Concat(matchingVaultIdsByVaultName).FirstOrDefault();
        }

        public bool WriteTree()
        {
            string configFile = FilesAndFolders.GetConfigPath(createFolder: true);

            var sDict = ExplicitPaths.ToSortedDictionary();

            var config = new VaultEagleConfig();

            try
            {
                config = ReadConfig(VaultName, VaultURI);
            }
            catch (Exception) { }

            string vaultId = GetVaultId(VaultName, VaultURI);
            config.Vaults[vaultId] = sDict;

            var jsonSettings = new JsonSerializerSettings() { Converters = new [] { new StringEnumConverter() } };
            var json = JsonConvert.SerializeObject(config, Formatting.Indented, jsonSettings);
            try
            {
                using (var writer = new StreamWriter(System.IO.File.Open(configFile, FileMode.Create, FileAccess.Write)))
                    writer.WriteLine(json);
            } catch{
                return false;
            }
            return true;
        }

        public static string GetVaultId(string vaultName, string vaultURI)
        {
            return vaultName.ToLowerInvariant() + "@" + vaultURI.ToLowerInvariant();
        }

        public void WriteTree_Old(TextWriter writer)
        {
            Dictionary<SyncState, string> syncStateMapping = new Dictionary<SyncState, string>()
            {
                {SyncState.Include, "+"},
                {SyncState.Exclude, "-"}
            };

            foreach (var path in ExplicitPaths.Keys.ToList().Sorted())
            {
                var val = ExplicitPaths[path];
                string line = syncStateMapping[val.State] + path;
                writer.WriteLine(line);
            }
        }

        public void WriteTree_Old()
        {
            string configFile = FilesAndFolders.GetConfigPath(createFolder: true);

            using (var writer = new StreamWriter(System.IO.File.Open(configFile, FileMode.Create, FileAccess.Write)))
                WriteTree_Old(writer);
        }

        private static List<string> SplitPath(string path)
        {
            return new List<string>(path.Split(new char[] { '/' }));
        }

        public SyncState? GetExplicitStateOfPath(string path)
        {
            if (ExplicitPaths.ContainsKey(path))
                return ExplicitPaths[path].State;
            return null;
        }

        public Option<string> GetLocalPathOfPath(string path)
        {
            foreach (string p in SubPaths(path))
            {
                if (ExplicitPaths.ContainsKey(p))
                {
                    var syncInfo = ExplicitPaths[p];

                    if (!string.IsNullOrWhiteSpace(syncInfo.LocalPath))
                        return Option.GetSome(Path.GetFullPath(Path.Combine(Environment.ExpandEnvironmentVariables(syncInfo.LocalPath), Utils.GetPrefixRelativePath(path, p))));
                }
            }
            return Option.None;
        }

        public SyncState GetImplicitStateOfPath(string path)
        {
            int count = 0;
            foreach (string p in SubPaths(path))
            {
                count++;
                if (ExplicitPaths.ContainsKey(p))
                {
                    var syncInfo = ExplicitPaths[p];

                    if(syncInfo.State == SyncState.FromParent)
                        continue;

                    if (specialFolderSyncStates.Contains(syncInfo.State))
                    {
                        if (count == 1 && syncInfo.State == SyncState.IncludeOnlyDirectChildFolders && SyncInfo.IsFolder(path)) // direct parent
                            return SyncState.IncludeOnlyFolders;
                        if (count == 1 && SyncInfo.IsFolder(path)) // explicit path
                            return syncInfo.State;
                        if (syncInfo.State == SyncState.IncludeOnlyFolders && SyncInfo.IsFolder(path))
                            return SyncState.IncludeOnlyFolders;
                        if (count == 2 && syncInfo.State == SyncState.IncludeOnlyDirectChildFolders && SyncInfo.IsFolder(path)) // direct parent
                            return SyncState.IncludeSingleFolder;
                        if (count == 2 && syncInfo.State == SyncState.IncludeOnlyFiles && !SyncInfo.IsFolder(path)) // direct parent
                            return SyncState.Include;
                        return SyncState.Exclude;
                    }

                    return syncInfo.State;
                }
            }
            return SyncState.Exclude;
        }

        public bool PathIsIncluded(string path)
        {
            return GetImplicitStateOfPath(path) != SyncState.Exclude;
        }

        public void Include(string path)
        {
            SetState(path, SyncState.Include);
        }

        public void Exclude(string path)
        {
            SetState(path, SyncState.Exclude);
        }

        public void SetState(string path, SyncState state)
        {
            SetSyncInfo(path, new SyncInfo() {State = state});
        }

        public void SetSyncInfo(string path, SyncInfo syncInfo)
        {
            ExplicitPaths[path] = syncInfo;
        }

        public void Reset(string path)
        {
            ExplicitPaths.Remove(path);
        }

        //public static string Normalize(string s)
        //{
        //    // return s.ToUpperInvariant(); // More correct but uglier: http://msdn.microsoft.com/en-us/library/bb386042.aspx
        //    return s.ToLowerInvariant();
        //}

        /// <summary>
        /// "$/Designs/Padlock/Dial.ipt" -> ["$/Designs/Padlock/Dial.ipt", "$/Designs/Padlock/", "$/Designs/", "$/"]
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IEnumerable<string> SubPaths(string path)
        {
            yield return path;

            const char SEPARATOR = '/';
            int pos = path.EndsWith(""+SEPARATOR) ? path.Length-1 : path.Length;
            pos = path.LastIndexOf(SEPARATOR, pos - 1);
            while (pos > 0)
            {
                yield return path.Substring(0,pos+1);
                pos = path.LastIndexOf(SEPARATOR, pos - 1);
            }
        }

        public static string GetParentFromPath(string path)
        {
            string folder, child;
            TrySplitPathIntoFolderAndChild(path, out folder, out child);
            return folder;
        }

        public static string GetNameFromPath(string path)
        {
            string folder, child;
            TrySplitPathIntoFolderAndChild(path, out folder, out child);
            return child;
        }


        public static bool TrySplitPathIntoFolderAndChild(string path, out string folder, out string child)
        {
            int startpos = path.EndsWith("/") ? path.Length-1 : path.Length;
            int i = path.LastIndexOf('/', startpos-1);
            if (i < 0)
            {
                folder = child = null;
                return false;
            }
            else
            {
                folder = path.Substring(0, i+1);
                child = path.Substring(i + 1);
                return true;
            }
        }

        public bool IsFolder(string path)
        {
            return SyncInfo.IsFolder(path);
        }

        public bool IsPathImplicit(string path)
        {
            return ExplicitPaths.ContainsKey(path);
        }

        public bool IsExcluded(string path)
        {
            return GetImplicitStateOfPath(path) == SyncState.Exclude;
        }

        public ImageIndex GetIconIndexForPath(string path)
        {
            if (ExplicitPaths.ContainsKey(path))
            {
                // Ok, it's explicitly set
                SyncInfo info = ExplicitPaths[path];
                if (specialFolderSyncStates.Contains(info.State))
                    return ImageIndex.FolderExplicitlyIncludeSpecial;
                if (info.State == SyncState.Include)
                    if (SyncInfo.IsFolder(path))
                        return ImageIndex.FolderExplicitlyInclude;
                    else
                        return ImageIndex.FileExplicitlyInclude;
                else
                    if (SyncInfo.IsFolder(path))
                        return ImageIndex.FolderExplicitlyExclude;
                    else
                        return ImageIndex.FileExplicitlyExclude;
            }
            else
                if (PathIsIncluded(path))
                    return ImageIndex.FolderImplicitlyInclude;
                else
                    return ImageIndex.FolderImplicitlyExclude;
        }

        public List<string> ExplicitlyIncludedFolders
        {
            get
            {
                return (from kv in ExplicitPaths
                        let syncInfo = kv.Value
                        let path = kv.Key
                        where SyncInfo.IsFolder(path)
                        where syncInfo.State != SyncState.Exclude
                        select kv.Key).ToList();
            }
        }

        public List<string> FoldersWithExplicitlyIncludedFiles
        {
            get
            {
                return ExplicitlyIncludedFiles
                    .Select(file => GetParentFromPath(file))
                    .Distinct()
                    .ToList();
            }
        }

        public List<string> ExplicitlyIncludedFiles
        {
            get
            {
                return (from kv in ExplicitPaths
                        let syncInfo = kv.Value
                        let path = kv.Key
                        where !SyncInfo.IsFolder(path)
                        where syncInfo.State == SyncState.Include
                        select kv.Key)
                        .ToList();
            }
        }
    }

    public static class TreeUtils
    {
        public static IEnumerable<T> Flatten<T>(T item, Func<T, IEnumerable<T>> next)
        {
            yield return item;
            foreach (T child in next(item))
                foreach (T flattenedChild in Flatten(child, next))
                    yield return flattenedChild;
        }
    }
}
