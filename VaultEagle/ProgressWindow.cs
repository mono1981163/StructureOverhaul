using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Common.DotNet.Extensions;

namespace VaultEagle
{
    public partial class ProgressWindow : Form, IProgressWindow
    {
        private Extension extension;
        private System.IO.StreamWriter logFileTextWriter;
        private Timer abortTimer;
        
        public ProgressWindow(Extension extension, string logFilePath = null)
        {
            this.extension = extension;
            InitializeComponent();
            closeButton.Select();
            try
            {
                if (logFilePath != null)
                    this.logFileTextWriter = new System.IO.StreamWriter(logFilePath, false, Encoding.UTF8);
            }
            catch (Exception) { }
        }

        public void Initialize()
        {
            // bind control to ui thread even though it's not visible yet
            var dummy = Handle;
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Hide();
        }

        public void Log(string text, string detailed = null)
        {
            if (string.IsNullOrEmpty(detailed))
                detailed = text;
            DoThreadSafe(() =>
            {
                string text2 = text.Replace("\r\n", "\n").Replace("\n", "\r\n");
                textBox1.AppendText(text2 + "\r\n");
                if (logFileTextWriter != null)
                {
                    string detailed2 = detailed.Replace("\r\n", "\n").Replace("\n", "\r\n");
                    logFileTextWriter.WriteLine("["+DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss z") + "] " + detailed2);
                    logFileTextWriter.Flush();
                }
            });
        }

        public void LogWithProgress(string text, int progress)
        {
            DoThreadSafe(() =>
            {
                if(text != null)
                    Log(text);
                progressBar1.Style = ProgressBarStyle.Blocks;
                progressBar1.Value = progress;
            });
        }
        
        public void LogDone(Boolean failed = false)
        {
            DoThreadSafe(() =>
            {
                progressBar1.Value = failed ? progressBar1.Minimum : progressBar1.Maximum;
                progressBar1.Style = ProgressBarStyle.Blocks;
                abortButton.Enabled = false;
                if (logFileTextWriter != null)
                {
                    logFileTextWriter.Close();
                    logFileTextWriter.Dispose();
                    logFileTextWriter = null;
                }
            });
        }

        public new void Show()
        {
            DoThreadSafe(base.Show);
        }

        private void abortButton_Click(object sender, EventArgs e)
        {
            abortButton.Enabled = false;
            if (extension.thread != null)
            {
                var thread = extension.thread.Stop();
                if (!thread.IsAlive)
                {
                    abortTimer = new Timer(); // timer to avoid deadlock with thread.Join and Invoke
                    abortTimer.Interval = 100;
                    abortTimer.Tick += (o, args) =>
                        {
                            if (!thread.IsAlive)
                            {
                                abortTimer.Stop();
                                abortTimer.DisposeUnlessNull(ref abortTimer);
                                progressBar1.Style = ProgressBarStyle.Blocks;
                            }
                        };
                    abortTimer.Start();
                    return;
                }
            }
            progressBar1.Style = ProgressBarStyle.Blocks;
        }

        public void DoThreadSafe(Action action)
        {
            if (InvokeRequired)
                BeginInvoke(action); // execute async, Control.BeginInvoke executes calls in-order, queuing items on the message-pump
            else
                action();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                    components.Dispose();
                if (logFileTextWriter != null)
                {
                    logFileTextWriter.Dispose();
                    logFileTextWriter = null;
                }
            }
            base.Dispose(disposing);
        }

    }
}
