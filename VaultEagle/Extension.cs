using Autodesk.Connectivity.Explorer.Extensibility;
using Autodesk.Connectivity.Extensibility.Framework;
using Common.DotNet.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

/*
 * TODO: Subscribe to folder structure, without files
 *
 * TODO: Handle paths missing/moved in Vault. (Id isn't
 * permanent. Still, cache file/folder id, then perform search for id.)
 *
 * TODO: Add download selection dialog.
 *
 * TODO: Handle log-in from Inventor plugin too.
 *
 * TODO: Run periodocally/triggered by event.
 *
 * Use Case: Install
 * Run installer, show location of plugin and config. When starting for first time show guide(?), do config.
 *
 * Use Case: Nothing changed
 * User starts Vault, all files are up to date.
 *
 * Use Case: Watched files out of date
 * User starts Vault, some files needs updating.
 *
 */

[assembly: ApiVersion("12.0")]
[assembly: ExtensionId("3BDBF088-F58A-44EC-95EB-414774B40F85"/*"e9f6702c-8422-4f88-bd15-8f39b2251443"*/)]

namespace VaultEagle
{
    public class Extension : IExplorerExtension
    {
        public const string STR_cacheFolder = @"C:\Workspace\VS2010.hg\vaulteagle\VaultEagleTests\VaultJsonCache";
        public static Extension _ExtensionHookForTests = null;
        public IApplication application;
        public Control invokeControl;
        public SynchronizationThread thread = null;
        private const string STR_MSAB_RunTests = "MSAB_RunTests";
        private const string STR_MSAB_ShowSubscriptionCommand = "MSAB_ToolShowSubscriptionCommand";
        private VaultCommunication vaultCom;
        // this is our hook to access the UI thread
#if DEBUG
#endif

        //    string pluginRoot = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        //    string assemblyPath = Path.Combine(pluginRoot, "VaultEagleTests.dll");
        //    new NUnitLite.Runner.TextUI(sw).Execute(new string[] { assemblyPath, "-out=" + tempfile });
        //    using (var sr = new StreamReader(tempfile))
        //    {
        //        sw.Write(sr.ReadToEnd());
        //    }
        //    System.IO.File.Delete(tempfile);
        //    return sw.ToString();
        //}
        //#endif
        public static bool DidAnyTestsFail(string data)
        {
            return !data.Contains(" : 0 Failures, 0 Errors, ");
        }

        public static string[] GetPathsFromSelections(VaultCommunication vaultCom, ISelection[] selSet)
        {
            long[] selectedFileIds = (from x in selSet where x.TypeId == SelectionTypeId.FileVersion select x.Id).ToArray();
            long[] selectedMasterFileIds = (from x in selSet where x.TypeId == SelectionTypeId.File select x.Id).ToArray();
            long[] selectedFolderIds = (from x in selSet where x.TypeId == SelectionTypeId.Folder select x.Id).ToArray();
            return vaultCom.GetPathsFromSelections(selectedFileIds, selectedMasterFileIds, selectedFolderIds);
        }

        public static void WriteTimeStampFile()
        {
            string timestamp = System.IO.Path.Combine(STR_cacheFolder, "timestamp.txt");
            using (var writer = new StreamWriter(System.IO.File.Open(timestamp, FileMode.Create, FileAccess.Write)))
                writer.WriteLine(DateTime.Now.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        public IEnumerable<CommandSite> CommandSites()
        {
            string folderMenuRoot = "Vault Eagle [MCAD]";
            string description = "(Vault Eagle updates subscribed files from Vault)";
            string folderSubscribe = "Subscribe to contained files and folders";
            string folderExclude = "Exclude, if subscribed to parent";
            string showSubscriptions = "Show Subscriptions...";

            string fileMenuRoot = "Vault Eagle [MCAD]";
            //     description       = "(Vault Eagle updates subscribed files from Vault)";
            string fileSubscribe = "Subscribe to this file";
            string fileExclude = "Exclude, if subscribed to parent";
            //     showSubscriptions = "Show Subscriptions...";

            string toolShowSubsc = "Show Subscriptions... [MCAD]";

            CommandItem descriptionCommand = new CommandItem("MSAB_DescriptionCommand", description)
            {
                MultiSelectEnabled = false,
                NavigationTypes = new SelectionTypeId[] { SelectionTypeId.Other },
            };
            descriptionCommand.Execute += ShowSubscription;

            CommandItem subscribeToFolderCommand = new CommandItem("MSAB_SubscribeToFolderCommand", folderSubscribe)
            {
                MultiSelectEnabled = true,
                NavigationTypes = new SelectionTypeId[] { SelectionTypeId.Folder, SelectionTypeId.File }
            };
            subscribeToFolderCommand.Execute += HandleSubscribe;

            CommandItem excludeFolderCommand = new CommandItem("MSAB_UnSubscribeFromFolderCommand", folderExclude)
            {
                MultiSelectEnabled = true,
                NavigationTypes = new SelectionTypeId[] { SelectionTypeId.Folder, SelectionTypeId.File },
            };
            excludeFolderCommand.Execute += HandleUnsubscribe;

            CommandItem showSubscriptionsCommand = new CommandItem("MSAB_ShowSubscriptionsCommand", showSubscriptions)
            {
                MultiSelectEnabled = true,
                NavigationTypes = new SelectionTypeId[] { SelectionTypeId.Folder, SelectionTypeId.File },
            };
            showSubscriptionsCommand.Execute += ShowSubscription;

            CommandItem subscribeToFileCommand = new CommandItem("MSAB_SubscribeToFileCommand", fileSubscribe)
            {
                MultiSelectEnabled = true,
                NavigationTypes = new SelectionTypeId[] { SelectionTypeId.File, SelectionTypeId.Folder }
            };
            subscribeToFileCommand.Execute += HandleSubscribe;

            CommandItem excludeFileCommand = new CommandItem("MSAB_UnSubscribeFromFileCommand", fileExclude)
            {
                MultiSelectEnabled = true,
                NavigationTypes = new SelectionTypeId[] { SelectionTypeId.File, SelectionTypeId.Folder }
            };
            excludeFileCommand.Execute += HandleUnsubscribe;

            CommandItem showSubscriptionCommand = new CommandItem(STR_MSAB_ShowSubscriptionCommand, toolShowSubsc);
            showSubscriptionCommand.Execute += ShowSubscription;

            CommandSite mcadToolMenuSite = new CommandSite("MSAB_ToolMenu", "VaultEagleToolMenu")
            {
                Location = CommandSiteLocation.ToolsMenu,
                DeployAsPulldownMenu = false
            };
#if DEBUG
            CommandItem runTestsCommand = new CommandItem(STR_MSAB_RunTests, "Run all tests... [MCAD]");
            runTestsCommand.Image = Properties.Resources.accept_sharp_edge.ToBitmap();
            runTestsCommand.Execute += RunTests;
            mcadToolMenuSite.AddCommand(runTestsCommand);
#endif
            mcadToolMenuSite.AddCommand(showSubscriptionCommand);

            CommandSite mcadFolderContextMenuSite = new CommandSite("MSAB_FolderContextMenu", folderMenuRoot)
            {
                DeployAsPulldownMenu = true,
                Location = CommandSiteLocation.FolderContextMenu
            };

            mcadFolderContextMenuSite.AddCommand(descriptionCommand);
            mcadFolderContextMenuSite.AddCommand(subscribeToFolderCommand);
            mcadFolderContextMenuSite.AddCommand(excludeFolderCommand);
            mcadFolderContextMenuSite.AddCommand(showSubscriptionsCommand);

            CommandSite mcadFileContextMenuSite = new CommandSite("MSAB_FileContextMenu", fileMenuRoot)
            {
                DeployAsPulldownMenu = true,
                Location = CommandSiteLocation.FileContextMenu
            };
            mcadFileContextMenuSite.AddCommand(descriptionCommand);
            mcadFileContextMenuSite.AddCommand(subscribeToFileCommand);
            mcadFileContextMenuSite.AddCommand(excludeFileCommand);
            mcadFileContextMenuSite.AddCommand(showSubscriptionsCommand);

            List<CommandSite> sites = new List<CommandSite>();
            sites.Add(mcadFolderContextMenuSite);
            sites.Add(mcadFileContextMenuSite);
            sites.Add(mcadToolMenuSite);
            return sites;
        }

        public IEnumerable<CustomEntityHandler> CustomEntityHandlers()
        {
            return null;
        }

        public IEnumerable<DetailPaneTab> DetailTabs()
        {
            return null;
        }
        public IEnumerable<string> HiddenCommands()
        {
            return null;
        }

        public void OnLogOff(IApplication application)
        {
            // Careful. This is called before first login. And also where obvious just before shutdown.
            System.Diagnostics.Debug.WriteLine("OnLogOff");
        }

        public void OnLogOn(IApplication application)
        {
            vaultCom = new VaultCommunication();
            this.application = application;

            invokeControl.DoThreadSafeAsync(() =>
            {
                if (application.Connection != null)
                {
                    InitializeFromConnectionOnLogon();
                }
                else
                {
                    CallInitializeLater();
                }
            });
        }

        public void OnShutdown(IApplication application)
        {
            if (thread != null)
            {
                thread.Stop();
                thread = null;
            }
        }

        public void OnStartup(IApplication application)
        {
            CreateInvokeControl();
            System.Diagnostics.Debug.WriteLine("OnStartup");

            // deletes Menus.xml after install, so our commands appear in the Main Menu
            string[] commandsToLookFor =
            {
            STR_MSAB_ShowSubscriptionCommand,
#if DEBUG
            STR_MSAB_RunTests,
#endif
            };

#if DEBUG
            _ExtensionHookForTests = this;
#endif
        }

        public void RunTests(bool alwaysShowLogWindows)
        {
            var showLog = RunTestsButDontShow(alwaysShowLogWindows);
            if (showLog != null)
                showLog();
        }

        public Action RunTestsButDontShow(bool alwaysShowLogWindows)
        {
            //System.Diagnostics.Debug.WriteLine("========================================");
            //System.Diagnostics.Debug.WriteLine("[Running tests...]");
            //var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            //string result = RunTestTextUI();
            //stopwatch.Stop();
            //System.Diagnostics.Debug.WriteLine(result);
            //System.Diagnostics.Debug.WriteLine(string.Format("[Done after {0} ms]", stopwatch.ElapsedMilliseconds));
            //WriteTimeStampFile();
            //System.Diagnostics.Debug.WriteLine("========================================");
            //if (DidAnyTestsFail(result) || alwaysShowLogWindows)
            //    return () =>
            //        {
            //            var log = new ProgressWindow(this);
            //            log.Log(result);
            //            log.Log(string.Format("[Done after {0} ms]", stopwatch.ElapsedMilliseconds));
            //            log.Show();
            //            log.LogDone();
            //        };
            //else
            return null;
        }

        public void StartSyncThread(bool showProgressWindow)
        {
            if (thread != null)
            {
                thread.Stop();
            }
            thread = new SynchronizationThread(application, invokeControl);
            var log = new ProgressWindow(this, FilesAndFolders.GetLogPath());
            log.Initialize();

            VaultEagleSynchronizer.LogVersion(log, System.Reflection.Assembly.GetExecutingAssembly());

            if (showProgressWindow)
                log.Show();
            thread.logWindow = log;
            thread.Start();
        }

        private void CallInitializeLater()
        {
            var timer = new Timer();
            timer.Interval = 1500;
            timer.Tag = 10; // number of attempts
            timer.Tick += (sender, args) =>
                {
                    System.Diagnostics.Debug.WriteLine("timer");
                    var attempts = (int)timer.Tag;
                    timer.Tag = attempts - 1;
                    System.Diagnostics.Debug.WriteLine("timer.Tag: " + attempts);

                    bool stop = attempts <= 1; // give up after this try
                    if (this.application.Connection != null)
                    {
                        stop = true;
                        InitializeFromConnectionOnLogon();
                    }

                    if (stop)
                    {
                        timer.Stop();
                        timer.DisposeUnlessNull(ref timer);
                    }
                };
            timer.Start();
        }

        private void CreateInvokeControl()
        {
            if (invokeControl == null)
            {
                invokeControl = new Control();
                var dummy = invokeControl.Handle;
            }
        }

        private void HandleSubscribe(object s, CommandItemEventArgs e)
        {
            SubscribeOrUnsubscribe(e.Context.CurrentSelectionSet, subscribe: true);
        }

        private void HandleUnsubscribe(object s, CommandItemEventArgs e)
        {
            SubscribeOrUnsubscribe(e.Context.CurrentSelectionSet, subscribe: false);
        }

        private void InitializeFromConnectionOnLogon()
        {
            vaultCom.InitializeFromConnection(application.Connection);
            vaultCom.Log = s => System.Diagnostics.Debug.WriteLine(s);

            StartSyncThread(false);

            System.Diagnostics.Debug.WriteLine("OnLogOn");
        }
        //#if DEBUG
        private void RunTests(object s, CommandItemEventArgs e)
        {
            //RunTests(true);
        }

        private List<string> SeparateFolderPath(string path)
        {
            return new List<string>(path.Split(new char[] { '/' }));
        }

        private void ShowSubscription(object s, CommandItemEventArgs e)
        {
            var connection = e.Context.Application.Connection;
            if (vaultCom.connection == null)
                vaultCom.InitializeFromConnection(application.Connection);
            string vaultName = connection.Vault, vaultUri = connection.Server;

            SynchronizationTree tree = new SynchronizationTree(vaultName, vaultUri);
            try
            {
                tree = SynchronizationTree.ReadTree(vaultName, vaultUri);

                if (tree.IsEmpty())
                {
                    var result = MessageBox.Show("No subscriptions found for this vault. Try harder?", "Try harder?", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                        tree = SynchronizationTree.ReadTree(vaultName, vaultUri, tryHarder: true);
                }
            }
            catch (Exception ex)
            {
                var result = MessageBox.Show("[ERROR] " + ex.Message + "\r\nDo you want to continue?", "Error", MessageBoxButtons.OKCancel);
                if (result == DialogResult.Cancel)
                    return;
            }

            ShowSubscriptionsForm form = new ShowSubscriptionsForm(tree, StartSyncThread, vaultCom);
            form.ShowDialog();
        }

        private void SubscribeOrUnsubscribe(IEnumerable<ISelection> selectionSet, bool subscribe)
        {
            try
            {
                if (vaultCom.connection == null)
                    vaultCom.InitializeFromConnection(application.Connection);

                var selSet = selectionSet.ToArray();
                var paths = GetPathsFromSelections(vaultCom, selSet);

                var connection = application.Connection;
                SynchronizationTree tree = SynchronizationTree.ReadTree(connection.Vault, connection.Server);
                foreach (var path in paths)
                    if (subscribe)
                        tree.Include(path);
                    else
                        tree.Exclude(path);

                bool didWrite = tree.WriteTree();
                if (!didWrite)
                {
                    var readOnly = new ReadOnlyForm();
                    readOnly.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("[ERROR] " + ex.Message, "Error");
            }
        }
        //private static string RunTestTextUI()
        //{
        //    var sw = new StringWriter();

        //    //new NUnitLite.Runner.TextUI(sw).Execute(new string[0]);
        //    var tempfile = Path.GetTempFileName();
    }
}