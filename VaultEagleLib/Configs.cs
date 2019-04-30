using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

using Common.DotNet.Extensions;

namespace VaultEagle
{
    public class Configs
    {
        public class ConfigConversionContext
        {
            public string VaultName = "";
            public string VaultURI = "";
        }

        public class VaultEagleConfigV0_5
        {
            public enum SyncStateV0_5 { Exclude, Include };

            public Dictionary<string, SyncStateV0_5> ExplicitPaths = new Dictionary<string, SyncStateV0_5>();

            public SynchronizationTree.VaultEagleConfig UpgradeToLatest(ConfigConversionContext context)
            {
                return UpgradeToNext(context).UpgradeToLatest(context);
            }

            public VaultEagleConfigV1_0 UpgradeToNext(ConfigConversionContext context)
            {
                return new VaultEagleConfigV1_0
                    {
                        Vaults = new[]{ExplicitPaths.ToDictionary(kv => kv.Key.ToLowerInvariant(),
                                                                   kv => new VaultEagleConfigV1_0.SyncInfoV1_0
                                                                       {
                                                                           State = (VaultEagleConfigV1_0.SyncStateV1_0) kv.Value,
                                                                           Path = kv.Key
                                                                       }).ToSortedDictionary()
                                      }.ToDictionary(x => SynchronizationTree.GetVaultId(context.VaultName, context.VaultURI))
                    };
            }

            public static VaultEagleConfigV0_5 ParseTree_Old(TextReader input)
            {
                // Match e.g.:
                // +$/Designs.ipj
                // -$/Designs/Padlock/Office
                var regex = new System.Text.RegularExpressions.Regex("^([+-])([$]/.*)$");

                var syncStateDict = new Dictionary<string, SyncStateV0_5>()
                {
                    {"-",SyncStateV0_5.Exclude},
                    {"+",SyncStateV0_5.Include}
                };

                var t = new VaultEagleConfigV0_5();
                foreach (var item in GetLines(input))
                {
                    if (item.StartsWith("#") || item.Trim().Length == 0)
                        continue;
                    var m = regex.Match(item);
                    if (m.Success)
                    {
                        var syncState = syncStateDict[m.Groups[1].Value];
                        var path = m.Groups[2].Value;
                        t.ExplicitPaths[path] = syncState;
                    }
                }
                return t;
            }
        }

        public class VaultEagleConfigV1_0
        {
            public enum SyncStateV1_0 { Exclude, Include, IncludeOnlyFolders };
            public class SyncInfoV1_0
            {
                public SyncStateV1_0 State;
                public string Path = "";
            }

            public Dictionary<string, SortedDictionary<string, SyncInfoV1_0>> Vaults = new Dictionary<string, SortedDictionary<string, SyncInfoV1_0>>(StringComparer.OrdinalIgnoreCase);

            public SynchronizationTree.VaultEagleConfig UpgradeToLatest(ConfigConversionContext context)
            {
                return UpgradeToNext(context).UpgradeToLatest(context);
            }

            public VaultEagleConfigV1_1 UpgradeToNext(ConfigConversionContext context)
            {
                return new VaultEagleConfigV1_1
                    {
                        Vaults = Vaults.ToDictionary(x => x.Key,
                                                     x => x.Value.ToDictionary(kv => kv.Value.Path,
                                                                          kv => new VaultEagleConfigV1_1.SyncInfoV1_1
                                                                              {
                                                                                  State = (VaultEagleConfigV1_1.SyncStateV1_1)kv.Value.State
                                                                              })
                                                            .ToSortedDictionary(),
                                                     StringComparer.OrdinalIgnoreCase),
                    };
            }

            public static VaultEagleConfigV1_0 ParseForest_1_0(TextReader input)
            {
                using (var jsonR = new JsonTextReader(input))
                    return new VaultEagleConfigV1_0
                        {
                            Vaults = new JsonSerializer().Deserialize<Dictionary<string, SortedDictionary<string, SyncInfoV1_0>>>(jsonR)
                        };
            }

        }

        public class VaultEagleConfigV1_1
        {
            public enum SyncStateV1_1 { Exclude, Include, IncludeOnlyFolders, IncludeOnlyFiles, IncludeOnlyDirectChildFolders, IncludeSingleFolder }

            public class SyncInfoV1_1
            {
                public SyncStateV1_1 State;
                public long LastVaultId = -1;

                public VaultEagleConfigV2_0.SyncInfoV2_0 UpgradeToNext()
                {
                    return new VaultEagleConfigV2_0.SyncInfoV2_0 { LastVaultId = LastVaultId, State = (VaultEagleConfigV2_0.SyncStateV2_0) State };
                }
            }

            public Dictionary<string, SortedDictionary<string, SyncInfoV1_1>> Vaults = new Dictionary<string, SortedDictionary<string, SyncInfoV1_1>>(StringComparer.OrdinalIgnoreCase);
            public string ConfigVersion = "1.1";
            public bool OverwriteLocallyModifiedFiles = true;

            public SynchronizationTree.VaultEagleConfig UpgradeToLatest(ConfigConversionContext context)
            {
                return UpgradeToNext(context).UpgradeToLatest(context);
            }

            public VaultEagleConfigV2_0 UpgradeToNext(ConfigConversionContext context)
            {
                return new VaultEagleConfigV2_0
                    {
                        Vaults = Vaults.ToDictionary(
                                    x => x.Key,
                                    x => x.Value.ToDictionary(kv => kv.Key, kv => kv.Value.UpgradeToNext()).ToSortedDictionary(),
                                    StringComparer.OrdinalIgnoreCase),
                    };
            }

            public static VaultEagleConfigV1_1 ParseForest_1_1(JObject input)
            {
                return input.ToObject<VaultEagleConfigV1_1>(new JsonSerializer());
            }
        }

        public class VaultEagleConfigV2_0
        {
            public enum SyncStateV2_0 { Exclude, Include, IncludeOnlyFolders, IncludeOnlyFiles, IncludeOnlyDirectChildFolders, IncludeSingleFolder, FromParent }

            public class SyncInfoV2_0
            {
                public SyncStateV2_0 State;
                public long LastVaultId = -1;
                public string LocalPath = null;

                public SynchronizationTree.SyncInfo UpgradeToLatest()
                {
                    return new SynchronizationTree.SyncInfo { LastVaultId = LastVaultId, State = (SyncState) State, LocalPath = LocalPath };
                }
            }

            public Dictionary<string, SortedDictionary<string, SyncInfoV2_0>> Vaults = new Dictionary<string, SortedDictionary<string, SyncInfoV2_0>>(StringComparer.OrdinalIgnoreCase);
            public string ConfigVersion = "2.0";
            public bool OverwriteLocallyModifiedFiles = true;

            public SynchronizationTree.VaultEagleConfig UpgradeToLatest(ConfigConversionContext context)
            {
                return new SynchronizationTree.VaultEagleConfig
                    {
                        Vaults = Vaults.ToDictionary(x => x.Key, x => x.Value.ToDictionary(kv => kv.Key, 
                                        kv => kv.Value.UpgradeToLatest()).ToSortedDictionary(), StringComparer.OrdinalIgnoreCase),
                    };
            }
        }
        public static IEnumerable<string> GetLines(TextReader reader)
        {
            using (reader)
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

    }
}
