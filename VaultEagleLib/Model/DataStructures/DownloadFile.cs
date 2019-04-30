using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Connectivity.WebServices;
using Common.DotNet.Extensions;
using System.IO;
namespace VaultEagleLib.Model.DataStructures
{
    public class DownloadFile : DownloadItem
    {
        public DownloadFile(Autodesk.Connectivity.WebServices.File file, string downloadPath, bool writable, bool run)
            : base(downloadPath)
        {
            File = file;
            Writable = writable;
            Run = run;
        }

        public Autodesk.Connectivity.WebServices.File File { get; private set; }
        public string LocalFileName
        {
            get { return Path.Combine(DownloadPath, File.Name); }
        }
        public bool Run { get; private set; }

        public bool Writable { get; private set; }

        public virtual void Log(Option<MCADCommon.LogCommon.DisposableFileLogger> logger)
        {
            logger.IfSomeDo(l => l.Info("Synchronizing: " + DownloadPath + "\\" + File.Name));
        }

        public string VaultFileName(string vaultRoot)
        {
            return DownloadPath.Replace(vaultRoot, "$").Replace("\\", "/") + "/" + File.Name;
        }
    }
}
