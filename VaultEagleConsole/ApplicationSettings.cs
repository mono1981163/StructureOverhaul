using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.DotNet.Extensions;
using MCADCommon.MailCommon;
using System.IO;
using System.Xml;
using System.Reflection;
namespace VaultEagleConsole
{
    class ApplicationSettings
    {
        private static FileInfo ConfigurationFile { get { return new FileInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "applicationsettings.config")); } }
        public string configPath { get; private set; }
        public Option<MCADCommon.LogCommon.Utils.LogLevel> LogLevel { get; private set; }
        public int SavedLogCount { get; private set; }
        //public bool UseLastSyncDate { get; private set; }
        public bool UseNotifications { get; private set; }
        public bool ReadOnly { get; private set; }
        //public DateTime LastSyncTime { get; private set; }

        public Option<MailSettings> MailInfo { get; private set; }
        public string MailLog { get; private set; }
        public int NetworkRetries { get; private set; }

        private static string PassPhrase = "Hades";

        public  List<string> ServerList { get; private set; }

        public static ApplicationSettings Read()
        {
            ApplicationSettings configuration = new ApplicationSettings();

            if (ConfigurationFile.Exists)
            {
                XmlDocument document = new XmlDocument();
                document.Load(ConfigurationFile.FullName);

                configuration.ServerList = getServerList();


                configuration.configPath = Environment.ExpandEnvironmentVariables(MCAD.XmlCommon.XmlTools.ReadString(document.DocumentElement, "configuration_path"));
                configuration.LogLevel = MCAD.XmlCommon.XmlTools.SafeReadString(document.DocumentElement, "log_level").IsSome ? MCADCommon.LogCommon.Utils.ParseLogLevel(MCAD.XmlCommon.XmlTools.ReadString(document.DocumentElement, "log_level")).AsOption() : Option.None;

                Option<XmlElement> mailElement = MCAD.XmlCommon.XmlTools.SafeGetElement(document.DocumentElement, "mail_settings");
                configuration.MailInfo = mailElement.IsSome ? MailSettings.Read(mailElement.Get(), PassPhrase).AsOption() : Option.None;
                configuration.MailLog = MCAD.XmlCommon.XmlTools.SafeReadString(document.DocumentElement, "mail_log").IsSome ? MCAD.XmlCommon.XmlTools.ReadString(document.DocumentElement, "mail_log") : "Never";

             //   configuration.UseLastSyncDate = MCAD.XmlCommon.XmlTools.SafeReadString(document.DocumentElement, "use_last_sync_date").IsSome ? MCAD.XmlCommon.XmlTools.ReadString(document.DocumentElement, "use_last_sync_date").Equals("True", StringComparison.InvariantCultureIgnoreCase) : false;

                configuration.UseNotifications = MCAD.XmlCommon.XmlTools.SafeReadString(document.DocumentElement, "use_notifications").IsSome ? MCAD.XmlCommon.XmlTools.ReadString(document.DocumentElement, "use_notifications").Equals("True", StringComparison.InvariantCultureIgnoreCase) : false;

                configuration.NetworkRetries = MCAD.XmlCommon.XmlTools.SafeReadString(document.DocumentElement, "network_retries").IsSome ? Convert.ToInt32(MCAD.XmlCommon.XmlTools.ReadString(document.DocumentElement, "network_retries")) : 5;
                configuration.ReadOnly = MCAD.XmlCommon.XmlTools.SafeReadString(document.DocumentElement, "login_mode").IsSome ? MCAD.XmlCommon.XmlTools.ReadString(document.DocumentElement, "login_mode").Equals("read", StringComparison.InvariantCultureIgnoreCase) : false;
              /*  if ((configuration.UseLastSyncDate) && (VaultEagle.VaultUtils.HandleNetworkErrors(() => File.Exists(Path.Combine(configuration.configPath, "last_sync_time.txt")), configuration.NetworkRetries)))
                {
                    try { configuration.LastSyncTime = DateTime.ParseExact(VaultEagle.VaultUtils.HandleNetworkErrors(() => File.ReadLines(Path.Combine(configuration.configPath, "last_sync_time.txt")).First(), configuration.NetworkRetries), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture); }
                    catch { configuration.LastSyncTime = DateTime.ParseExact("1990-01-01 00:00:00", "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture); }
                }
                else
                    configuration.LastSyncTime = DateTime.ParseExact("1990-01-01 00:00:00", "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                */
                try{
                    configuration.SavedLogCount = MCAD.XmlCommon.XmlTools.SafeReadString(document.DocumentElement, "saved_log_count").IsSome ? Convert.ToInt32(MCAD.XmlCommon.XmlTools.ReadString(document.DocumentElement, "saved_log_count")) : 60;
                } catch{
                    configuration.SavedLogCount = 60;
                }
                return configuration;
            }
            throw new ErrorMessageException("Could not find file: " + ConfigurationFile.FullName + ".");
        }

        public static List<string> getServerList()
        {
            List<string> serverLists = new List<string>();
            List<string> serverList = new List<string>();

            if (ConfigurationFile.Exists)
            {
                XmlDocument document = new XmlDocument();
                document.Load(ConfigurationFile.FullName);

                serverLists = (List<string>)MCAD.XmlCommon.XmlTools.ReadStringValues(document.DocumentElement, "check_online_servers", "check_online_server", false).Select(p => Environment.ExpandEnvironmentVariables(p)).ToList();

                for (int i = 0; i < serverLists.Count; i++)
                {
                    if (serverLists[i].Contains(';'))
                    {
                        String[] splittedText = serverLists[i].Split(';');

                        for (int x = 0; x < splittedText.Length; x++)
                        {
                            if (splittedText[x].Contains('@'))
                            {
                                serverList.Add(splittedText[x].Substring(splittedText[x].LastIndexOf('@')+1));
                            }
                        }
                    }
                    else
                    {
                        serverList.Add(serverLists[i]);
                    }
                }
            }
            return serverList;
        }
    }
}
