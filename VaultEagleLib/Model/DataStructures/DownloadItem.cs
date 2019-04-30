using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultEagleLib.Model.DataStructures
{
    public class DownloadItem
    {
        public string DownloadPath { get; private set; }

        public DownloadItem(string downloadPath)
        {
            DownloadPath = downloadPath;
        }
    }
}
