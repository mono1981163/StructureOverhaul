using Common.DotNet.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCADCommon
{
    public interface ExtensionBase
    {
        void AddToContext(Standard.Context context);
    }
}
