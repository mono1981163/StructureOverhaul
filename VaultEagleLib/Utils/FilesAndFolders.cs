using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Common.DotNet.Extensions;

namespace VaultEagle
{
    public static class FilesAndFolders
    {
        public static string GetConfigPath(bool createFolder = false)
        {
            return Path.Combine(GetVaultEagleConfigFolder(createFolder), "vaulteagle.conf");
        }

        public static string GetDefaultFunctionJsonCachePath(bool createFolder = false)
        {
            return Path.Combine(GetVaultEagleConfigFolder(createFolder), "vaulteagle.cache");
        }

        public static string GetLogPath(bool createFolder = false)
        {
            return Path.Combine(GetVaultEagleConfigFolder(createFolder), "vaulteagle.log");
        }

        public static string GetResumeStatePath(bool createFolder = false)
        {
            return Path.Combine(GetVaultEagleConfigFolder(createFolder), "vaulteagle.resumestate");
        }

        public static string GetVaultEagleConfigFolder(bool createFolder = false)
        {
            VaultEagleLib.SynchronizerSettings appSet = VaultEagleLib.SynchronizerSettings.Read(5, Option.None);
            string folder;
            if (appSet.configPath.Equals(""))
            {
                folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MCAD", "VaultEagle");
            }
            else
            {
                folder = appSet.configPath;
            }
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }
    }
}
