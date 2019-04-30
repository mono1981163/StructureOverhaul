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
    public class SynchronizerSettings
    {
        public string configPath { get; private set; }
        public List<string> GetAndRun { get; private set; }
        public List<SynchronizationItem> Items { get; private set; }
        public Option<string> LastSyncFile { get; private set; }
        public DateTime LastSyncTime { get; private set; }
        public Option<string> LogFile { get; private set; }
        public MCADCommon.LogCommon.Utils.LogLevel LogLevel { get; private set; }
        public Option<string> OverrideMailSender { get; private set; }
        public int SavedLogCount { get; private set; }
        public bool UseLastSync { get; private set; }
        public string VaultRoot { get; private set; }
        private static FileInfo ConfigurationFile { get { return new FileInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SynchronizerSettings.config")); } }
        public static SynchronizerSettings Read( int networkRetries, Option<MCADCommon.LogCommon.DisposableFileLogger> logger,string configurationPath = "")
        {
            SynchronizerSettings configuration = new SynchronizerSettings();
            FileInfo configurationFile;
            if (!String.IsNullOrWhiteSpace(configurationPath))
                configurationFile = new FileInfo(configurationPath);
            else
                configurationFile = ConfigurationFile;

            if (!configurationFile.Exists)
                configurationFile = new FileInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), configurationPath));

            if (configurationFile.Exists)
            {
                XmlDocument document = new XmlDocument();
                document.Load(configurationFile.FullName);

                configuration.configPath = Environment.ExpandEnvironmentVariables(MCAD.XmlCommon.XmlTools.ReadString(document.DocumentElement, "configuration_path"));

                try
                {
                    configuration.GetAndRun = MCAD.XmlCommon.XmlTools.ReadStringValues(document.DocumentElement, "run_files", "get_and_run");
                }
                catch
                {
                    configuration.GetAndRun = new List<string>();
                }
                configuration.VaultRoot = Environment.ExpandEnvironmentVariables(MCAD.XmlCommon.XmlTools.ReadString(document.DocumentElement, "vault_root"));
                List<SynchronizationItem> items = new List<SynchronizationItem>();
                foreach (string syncItem in MCAD.XmlCommon.XmlTools.ReadStringValues(document.DocumentElement, "synchronization_commands", "synchronization_command"))
                {
                    try
                    {
                        items.Add(SynchronizationItem.ReadString(syncItem));
                    }
                    catch (Exception ex) { logger.IfSomeDo(l => l.Error(ex.Message)); }
                }

                List<SynchronizationItem> RemoveItemsNotLegal = new List<SynchronizationItem>();
                foreach (SynchronizationItem item in items)
                {
                    try
                    {
                        if (!item.HasIllegalItems(configuration.VaultRoot))
                            RemoveItemsNotLegal.Add(item);
                    }
                    catch (Exception ex) { logger.IfSomeDo(l => l.Error(ex.Message)); }
                }

                

                configuration.Items = RemoveItemsNotLegal;

                configuration.LastSyncFile = MCAD.XmlCommon.XmlTools.SafeReadString(document.DocumentElement, "last_sync_file");
                configuration.UseLastSync = false;
                if ((configuration.LastSyncFile.IsSome) && (VaultEagle.VaultUtils.HandleNetworkErrors(() => File.Exists(Path.Combine(configuration.configPath, configuration.LastSyncFile.Get() + ".txt")), networkRetries)))
                {
                    configuration.UseLastSync = true;
                    try { configuration.LastSyncTime = DateTime.ParseExact(VaultEagle.VaultUtils.HandleNetworkErrors(() => File.ReadLines(Path.Combine(configuration.configPath, configuration.LastSyncFile.Get() + ".txt")).First(), networkRetries), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture); }
                    catch { configuration.LastSyncTime = DateTime.ParseExact("1990-01-01 00:00:00", "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture); }
                }
                else 
                    configuration.LastSyncTime = DateTime.ParseExact("1990-01-01 00:00:00", "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

                configuration.OverrideMailSender = MCAD.XmlCommon.XmlTools.SafeReadString(document.DocumentElement, "override_mail_sender");
                configuration.LogLevel = MCADCommon.LogCommon.Utils.ParseLogLevel(MCAD.XmlCommon.XmlTools.SafeReadString(document.DocumentElement, "log_level").IsSome ? MCAD.XmlCommon.XmlTools.ReadString(document.DocumentElement, "log_level") : "Info");
                configuration.LogFile = MCAD.XmlCommon.XmlTools.SafeReadString(document.DocumentElement, "log_name");
                try
                {
                    configuration.SavedLogCount = MCAD.XmlCommon.XmlTools.SafeReadString(document.DocumentElement, "saved_log_count").IsSome ? Convert.ToInt32(MCAD.XmlCommon.XmlTools.ReadString(document.DocumentElement, "saved_log_count")) : 60;
                }
                catch
                {
                    configuration.SavedLogCount = 60;
                }
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
                folderMappings.Add(new Tuple<string, string>(MCAD.XmlCommon.XmlTools.ReadString(folderMapping, "source"), Environment.ExpandEnvironmentVariables(MCAD.XmlCommon.XmlTools.ReadString(folderMapping, "target"))));

            return folderMappings;
        }
    }
}