using Common.DotNet.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WS = Autodesk.Connectivity.WebServices;

namespace MCADCommon.VaultCommon
{
    public static class NumberingSchemesUtils
    {
        /*****************************************************************************************/
        public static List<string> GetActiveSchemes(WS.DocumentService documentService)
        {
            List<string> numberingSchemes = new List<string>();

            WS.NumSchm[] schemes = documentService.GetNumberingSchemesByType(WS.NumSchmType.Activated);
            if (schemes != null)
                foreach (WS.NumSchm scheme in schemes)
                    numberingSchemes.Add(scheme.Name);

            return numberingSchemes;
        }

        /*****************************************************************************************/
        public static Option<string> GetNumber(WS.DocumentService documentService, string numberingSchemeName, string[] fields = null)
        {
            fields = fields ??  new string[] { "" };

            WS.NumSchm[] schemes = documentService.GetNumberingSchemesByType(WS.NumSchmType.Activated);
            if (schemes == null)
                return Option.None;

            foreach (WS.NumSchm scheme in schemes)
                if (scheme.Name.Equals(numberingSchemeName, StringComparison.InvariantCultureIgnoreCase))
                    return documentService.GenerateFileNumber(scheme.SchmID, fields).AsOption();

            return Option.None;
        }
    }
}
