using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultEagleLib
{
    public interface IProgressWindow
    {
        void Log(string text, string detailed = null);
        void LogDone(Boolean failed = false);

        void LogWithProgress(string text, int progress);
        void Show();
    }

    public interface ISysTrayNotifyIconService
    {
        void ShowIfSlow(string s);

        void ShowNow(string s, bool ignoreMinimumDisplayTime = false);

        void Start();
    }

    [Serializable]
    public class StopThreadException : Exception
    {
        public StopThreadException() : base("Synchronization interrupted.") { }
    }

    public class StopThreadSwitch
    {
        // boolean read/write is atomic
        // (must lock though if more data is passed between
        // threads, and a memory barrier needed)
        public bool ShouldStop = false;
    }
}
