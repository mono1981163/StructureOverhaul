using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

using Autodesk.Connectivity.WebServicesTools;
using Autodesk.Connectivity.Explorer.Extensibility;
using Autodesk.Connectivity.Explorer.ExtensibilityTools;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties;

using ADSK = Autodesk.Connectivity.WebServices;
using Newtonsoft.Json;
using System.Windows.Forms;
using Autodesk.Connectivity.WebServices;

using Common.DotNet.Extensions;

namespace VaultEagle
{
    public class SynchronizationThread
    {
        Thread thread;

        public ProgressWindow logWindow;
        public Control invokeControl;
        private VaultEagleSynchronizer synchronizer;

        public SynchronizationThread(IApplication application, Control invokeControl)
        {
            thread = new Thread(Run);
            thread.Name = "SynchronizationThread";

            this.invokeControl = invokeControl;

            synchronizer = new VaultEagleSynchronizer(application.Connection);
        }

        public void Start()
        {
            thread.Start();
        }

        public Thread Stop()
        {
            synchronizer.StopThread.ShouldStop = true;

            // don't wait on the ui thread, deadlock together with invoke!
            //if (thread.IsAlive)
            //    thread.Join();
            return thread;
        }

        private void Run()
        {
            try
            {
#if DEBUG
                using (var streamWriter = new System.IO.StreamWriter(System.IO.Path.Combine(FilesAndFolders.GetVaultEagleConfigFolder(), "debug.log"), true))
                {
                    streamWriter.AutoFlush = true;
                    streamWriter.WriteLine(DateTime.Now.ToString("s").Replace('T',' ') + ": Logging begun...");
                    var textWriterTraceListener = new TextWriterTraceListener(streamWriter);
                    System.Diagnostics.Debug.Listeners.Add(textWriterTraceListener);
#endif
                try
                {
                    using (var sysTray = new SysTrayNotifyIconService(invokeControl))
                    {


                        sysTray.BalloonTipClicked += (object sender, EventArgs e) => logWindow.Show();

                        synchronizer.logWindow = logWindow;
                        synchronizer.sysTray = sysTray;
                        synchronizer.Synchronize();
                    }

//#if DEBUG
//                    var showTestLog = Extension._ExtensionHookForTests.RunTestsButDontShow(false);
//                    if (showTestLog != null)
//                        invokeControl.DoThreadSafeAsync(() =>
//                            showTestLog()
//                        );
//#endif

                }
                catch (System.Web.Services.Protocols.SoapException ex)
                {
                    var wrappedEx = VaultServerException.WrapException(ex);
                    HandleException(wrappedEx);
                    System.Diagnostics.Debug.WriteLine("ex.Detail.InnerXml: " + ex.Detail.InnerXml);
                }
                catch (Exception ex)
                {
                    HandleException(ex);
                }
#if DEBUG
                System.Diagnostics.Debug.Listeners.Remove(textWriterTraceListener);
                }
#endif
            }
            catch
            {
                // This is a pokemon catch. We should never end up here.
                // If we do, things are bad!
            }
        }

        private void HandleException(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            logWindow.Log("Halted.");
            logWindow.Log("Exception: " + ex.Message, detailed: "Exception: " + ex);
            logWindow.LogDone(failed: true);
            invokeControl.DoThreadSafeAsync(() =>
                logWindow.Show()
            );
        }
    }

    public class OneShotStoppableTimer : IDisposable
    {
        public readonly object TimerRunningLock = new object();
        private System.Threading.Timer timer;
        private bool stopped;

        public OneShotStoppableTimer(TimerCallback callback)
        {
            timer = new System.Threading.Timer(x => 
            {
                lock (TimerRunningLock)
                {
                    if (stopped)
                        return;
                    stopped = true;
                    callback(x);
                }
            });
        }

        public void Start(long dueTime)
        {
            lock(TimerRunningLock) {
                if (stopped)
                    return;
                timer.Change(dueTime, Timeout.Infinite);
            }
        }

        public void StopAndWait()
        {
            lock (TimerRunningLock)
            {
                if (stopped)
                    return;
                stopped = true;
                timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public bool Stopped()
        {
            lock (TimerRunningLock)
            {
                return stopped;
            }
        }

        public void Dispose()
        {
            timer.DisposeUnlessNull(ref timer);
        }
    }

    public class SysTrayNotifyIconService : IDisposable, ISysTrayNotifyIconService
    {
        public static readonly int DISPLAY_DELAY = 4000;
        public static readonly int MIN_DISPLAY_TIME = 1000;

        private NotifyIcon notifyIcon;
        private Control invokeControl;
        private int uiThreadNumActive = 0;
        private bool disposed = false;

        private DateTime started;
        private DateTime lastShowed;
        private OneShotStoppableTimer timer;
        private string nextMessage;

        public SysTrayNotifyIconService(Control invokeControl)
        {
            this.invokeControl = invokeControl;
        }

        public void Start()
        { 
            started = DateTime.Now;
        }

        private void StartTimer(string message, double dueTime)
        {
            nextMessage = message;
            timer = new OneShotStoppableTimer(TimerElapsed);
            timer.Start((long)dueTime);
        }

        public void TimerElapsed(object o)
        {
            //lock (timer.TimerRunningLock) // already locked

            ShowOnUIThread(nextMessage);

            timer.DisposeUnlessNull(ref timer);
        }

        public void ShowIfSlow(string s)
        {
            if (timer == null)
            {
                AddToNumActive(1);
                DateTime now = DateTime.Now;
                if (started != null && now < started.AddMilliseconds(DISPLAY_DELAY - 50))
                    StartTimer(s, (started.AddMilliseconds(DISPLAY_DELAY) - now).TotalMilliseconds);
                else if (lastShowed != null && now < lastShowed.AddMilliseconds(MIN_DISPLAY_TIME - 50))
                    StartTimer(s, (lastShowed.AddMilliseconds(MIN_DISPLAY_TIME) - now).TotalMilliseconds);
                else
                    ShowOnUIThread(s);
            }
            else
            {
                lock (timer.TimerRunningLock)
                {
                    nextMessage = s;
                }
            }
        }

        private void AddToNumActive(int k)
        {
            Action action = () => uiThreadNumActive += k;
            invokeControl.BeginInvoke(action);
        }

        public void ShowNow(string s, bool ignoreMinimumDisplayTime = false)
        {
            if (timer != null)
            {
                //AddToNumActive(-1);
                timer.StopAndWait();
                timer.DisposeUnlessNull(ref timer);
                //AddToNumActive(1);
            }
            else
                AddToNumActive(1);

            DateTime now = DateTime.Now;
            if (!ignoreMinimumDisplayTime && lastShowed != null && now < lastShowed.AddMilliseconds(MIN_DISPLAY_TIME - 50))
            {
                StartTimer(s, (lastShowed.AddMilliseconds(MIN_DISPLAY_TIME) - now).TotalMilliseconds);
            }
            else
                ShowOnUIThread(s);
        }

        private void ShowOnUIThread(string s)
        {
            lastShowed = DateTime.Now;

            Action action = () =>
                {
                    if (notifyIcon == null)
                    {
                        string company, product;
                        Version version;
                        VaultEagleSynchronizer.GetVersion(System.Reflection.Assembly.GetExecutingAssembly(), out company, out product, out version);
                        string shortVersion = version.Major + "." + version.Minor;

                        string STR_MCAD_Vault_Eagle_1_0___Workspace_Sync = "{0} {1} {2} - Workspace Sync";
                        string title = string.Format(STR_MCAD_Vault_Eagle_1_0___Workspace_Sync, company, product, shortVersion);

                        notifyIcon = new NotifyIcon() {
                            Text = title,
                            Icon = Properties.Resources.MCAD_,
                            BalloonTipIcon = ToolTipIcon.Info,
                            BalloonTipTitle = title
                        };

                        // forward event
                        notifyIcon.BalloonTipClicked += (sender, e) => BalloonTipClicked(sender, e);

                        //notifyIcon.BalloonTipShown += (sender, e) => active = true;
                        EventHandler onClosed = (sender, e) =>
                        {
                            System.Diagnostics.Debug.WriteLine("notifyIcon.BalloonTipClosed");
                            uiThreadNumActive--;
                            
                            if (disposed && uiThreadNumActive == 0)
                                notifyIcon.DisposeUnlessNull(ref notifyIcon);
                        };
                        notifyIcon.BalloonTipClosed += onClosed;
                        notifyIcon.BalloonTipClicked += onClosed;
                        notifyIcon.Visible = true;
                    }

                    notifyIcon.BalloonTipText = s;
                    notifyIcon.ShowBalloonTip(10000);
                };
            invokeControl.BeginInvoke(action);
        }

        public void Dispose()
        {
            Action action = () =>
                {
                    disposed = true;
                    if (uiThreadNumActive == 0)
                        notifyIcon.DisposeUnlessNull(ref notifyIcon);
                };
            invokeControl.BeginInvoke(action);
        }

        public event EventHandler BalloonTipClicked = delegate { };

        //// forward event
        //public event EventHandler BalloonTipClicked
        //{
        //    add    { notifyIcon.BalloonTipClicked += value; }
        //    remove { notifyIcon.BalloonTipClicked -= value; }
        //}

    }

    public class DisposableActionWrapper : IDisposable
    {
        public Action disposeAction;
        public void Dispose()
        {
            if (disposeAction != null)
                disposeAction();
        }
    }
}
