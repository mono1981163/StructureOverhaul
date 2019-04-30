using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.DotNet.Extensions;
using Autodesk.Connectivity.WebServices;
using MCADCommon.LogCommon;

namespace VaultEagleLib.Model.DataStructures.TacTon
{
    public class TactonDownloadFile : DownloadFile
    {
        string ComponentName;
        public TactonDownloadFile(File file, string downloadPath, bool writable, bool run, string componentName)
            : base(file, downloadPath, writable, run)
        {
            ComponentName = componentName;
        }

        public override void Log(Option<DisposableFileLogger> logger)
        {
            logger.IfSomeDo(l => l.Info("Synchronizing: " + DownloadPath + "\\" + File.Name + " Component: " + ComponentName));
        }
    }
}
