using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using MCAD.XmlCommon;
using MCADCommon.MailCommon;
using Common.DotNet.Extensions;
using System.Text.RegularExpressions;
namespace VaultEagleLib
{
    public class ApplicationSettings
    {
        private static FileInfo ConfigurationFile { get { return new FileInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "applicationsettings.config")); } }
        public string configPath { get; private set; }
        public MCADCommon.LogCommon.Utils.LogLevel LogLevel { get; private set; }
        public DateTime LastSyncTime { get; private set; }

        public Option<VaultSettings> VaultInfo { get; private set; }
        public Option<MailSettings> MailInfo { get; private set; }

        public List<string> ExcludePaths { get; private set; }
        public List<string> ExcludeFiles { get; private set; }
        public List<string> IncludePaths { get; private set; }
        public List<string> IncludeFiles { get; private set; } //Files in the root I believe.
        public List<Regex> IncludeFilePatterns { get; private set; } // *.iam, *.ipt
        public List<string> PossiblyLockedFiles { get; private set; } // Retries?
        public List<string> DeleteFiles { get; private set; } //Explicitly delete these.
        public List<Tuple<string, string>> FolderMappings {get; private set;}
        public string OutputPath { get; private set; }
      //  public string VaultRoot { get; private set; }
        public bool OnlyRecreateFolderStructure { get; private set; }
        public string VersionToDownload { get; private set; }
        public Option<string> DownloadOnState { get; private set; }
        public int NetworkRetries { get; private set; }

        private static string PassPhrase = "Hades";

        public static ApplicationSettings Read(string configurationPath = "")
        {
            ApplicationSettings configuration = new ApplicationSettings();
            FileInfo configurationFile;
            if (!String.IsNullOrWhiteSpace(configurationPath))
                configurationFile = new FileInfo(configurationPath);
            else
                configurationFile = ConfigurationFile;

            if (configurationFile.Exists)
            {
                XmlDocument document = new XmlDocument();
                document.Load(configurationFile.FullName);

                configuration.configPath = MCAD.XmlCommon.XmlTools.ReadString(document.DocumentElement, "configuration_path");
                configuration.LogLevel = MCADCommon.LogCommon.Utils.ParseLogLevel(MCAD.XmlCommon.XmlTools.SafeReadString(document.DocumentElement, "log_level").IsSome ? MCAD.XmlCommon.XmlTools.ReadString(document.DocumentElement, "log_level") : "info");

                Option<XmlElement> vaultElement = MCAD.XmlCommon.XmlTools.SafeGetElement(document.DocumentElement, "vault_settings");
                configuration.VaultInfo = vaultElement.IsSome ? VaultSettings.Read(vaultElement.Get()).AsOption() : Option.None;

                Option<XmlElement> mailElement = MCAD.XmlCommon.XmlTools.SafeGetElement(document.DocumentElement, "mail_settings");
                configuration.MailInfo = mailElement.IsSome ? MailSettings.Read(mailElement.Get(), PassPhrase).AsOption() : Option.None;


                configuration.ExcludePaths = MCAD.XmlCommon.XmlTools.ReadStringValues(document.DocumentElement, "exclude_paths", "path", false);
                configuration.IncludePaths = MCAD.XmlCommon.XmlTools.ReadStringValues(document.DocumentElement, "include_paths", "path", false);
                configuration.ExcludeFiles = MCAD.XmlCommon.XmlTools.ReadStringValues(document.DocumentElement, "exclude_files", "file", false);
                configuration.IncludeFiles = MCAD.XmlCommon.XmlTools.ReadStringValues(document.DocumentElement, "include_files", "file", false);
                configuration.IncludeFilePatterns = MCAD.XmlCommon.XmlTools.ReadStringValues(document.DocumentElement, "include_patterns", "expression", false).Select(e => new Regex(e+"$", RegexOptions.IgnoreCase)).ToList();
                configuration.PossiblyLockedFiles = MCAD.XmlCommon.XmlTools.ReadStringValues(document.DocumentElement, "possible_locked_files", "file", false);
                configuration.DeleteFiles = MCAD.XmlCommon.XmlTools.ReadStringValues(document.DocumentElement, "delete_files", "file", false);
                configuration.FolderMappings = GetFolderMappings(MCAD.XmlCommon.XmlTools.SafeGetElement(document.DocumentElement, "folder_mappings"));
                configuration.OutputPath = MCAD.XmlCommon.XmlTools.ReadString(document.DocumentElement, "output_path");
                configuration.OnlyRecreateFolderStructure = MCAD.XmlCommon.XmlTools.SafeReadString(document.DocumentElement, "only_recreate_folders").IsSome ? MCAD.XmlCommon.XmlTools.ReadString(document.DocumentElement, "only_recreate_folders").Equals("True", StringComparison.InvariantCultureIgnoreCase) : false;
                configuration.VersionToDownload = MCAD.XmlCommon.XmlTools.SafeReadString(document.DocumentElement, "version_to_download").IsSome ? MCAD.XmlCommon.XmlTools.ReadString(document.DocumentElement, "version_to_download") : "latest";
                configuration.DownloadOnState = MCAD.XmlCommon.XmlTools.SafeReadString(document.DocumentElement, "download_on_state");
                configuration.NetworkRetries = MCAD.XmlCommon.XmlTools.SafeReadString(document.DocumentElement, "network_retries").IsSome ? Convert.ToInt32(MCAD.XmlCommon.XmlTools.ReadString(document.DocumentElement, "network_retries")) : 5;

                if (VaultEagle.VaultUtils.HandleNetworkErrors(() => File.Exists(Path.Combine(configuration.configPath, "last_sync_time.txt")), configuration.NetworkRetries))
                {
                    try { configuration.LastSyncTime = DateTime.ParseExact(VaultEagle.VaultUtils.HandleNetworkErrors(() => File.ReadLines(Path.Combine(configuration.configPath, "last_sync_time.txt")).First(), configuration.NetworkRetries), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture); }
                    catch { configuration.LastSyncTime = DateTime.ParseExact("1990-01-01 00:00:00", "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture); }
                }
                else
                    configuration.LastSyncTime = DateTime.ParseExact("1990-01-01 00:00:00", "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                configuration.configPath = "";
            }

            return configuration;
        }

        private static List<Tuple<string, string>> GetFolderMappings(Option<XmlElement> element)
        {
            List<Tuple<string, string>> folderMappings = new List<Tuple<string, string>>();
            if (element.IsNone)
                return folderMappings;

            foreach (XmlElement folderMapping in MCAD.XmlCommon.XmlTools.GetElements(element.Get(), "folder_mapping"))
                folderMappings.Add(new Tuple<string, string>(MCAD.XmlCommon.XmlTools.ReadString(folderMapping, "source"), MCAD.XmlCommon.XmlTools.ReadString(folderMapping, "target")));

            return folderMappings;
        }
    }
}