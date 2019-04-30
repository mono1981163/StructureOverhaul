using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCADCommon.LogCommon
{
    public interface Logger
    {
        void Trace(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message);
    }
}
