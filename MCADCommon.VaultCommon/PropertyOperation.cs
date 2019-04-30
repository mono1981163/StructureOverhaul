using Common.DotNet.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ADSK = Autodesk.Connectivity;
using AWS = Autodesk.Connectivity.WebServices;
using ADMC = Autodesk.DataManagement.Client;

namespace MCADCommon.VaultCommon
{
    public static class PropertyOperations
    {
        /*****************************************************************************************/
        public static Option<string> GetPropertyString(ADMC.Framework.Vault.Currency.Connections.Connection connection, AWS.File file, string propertyName)
        {
            Option<AWS.PropDef> propertyDefinition = GetPropertyDefinition(connection, propertyName);
            if (propertyDefinition.IsNone)
                return Option.None;

            AWS.PropInst[] properties = connection.WebServiceManager.PropertyService.GetProperties("FILE", new long[] { file.Id }, new long[] { propertyDefinition.Get().Id });
            if (properties[0].Val == null)
                return Option.None;
            
            return properties[0].Val.ToString().AsOption();
        }

        /*****************************************************************************************/
        public static Option<AWS.PropDef> GetPropertyDefinition(ADMC.Framework.Vault.Currency.Connections.Connection connection, string name)
        {
            foreach (AWS.PropDef definition in connection.WebServiceManager.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE"))
                if (definition.DispName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    return definition.AsOption();

            return Option.None;
        }

        /*****************************************************************************************/
        public static bool IsPlotDateBeforeCheckedInDate(ADSK.WebServicesTools.WebServiceManager webService, AWS.File file)
        {
            long plotDateId = webService.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE").First(p => p.DispName == "Date Exported").Id;
            long checkedInId = webService.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE").First(p => p.DispName == "Checked In").Id;

            AWS.PropInst[] props = webService.PropertyService.GetProperties("FILE", new long[] { file.Id }, new long[] { checkedInId, plotDateId });
            DateTime checkInDate = (DateTime)props[0].Val;
            Option<DateTime> plotDate = Option.None;
            try
            { plotDate = ((DateTime)props[1].Val).AsOption(); }
            catch
            { plotDate = Option.None; }

            if ((plotDate.IsNone) ||((plotDate.IsSome) && ((DateTime.Compare(checkInDate.Subtract(new TimeSpan(0, 30, 0)), plotDate.Get()) > 0))))
                return true;

            return false;
        }

        /**********************************************************************************************************************************************/
        public static List<AWS.FileFolder> GetFilesThatContainsName(ADSK.WebServicesTools.WebServiceManager webService, string name)
        {
            List<AWS.FileFolder> files = new List<AWS.FileFolder>();
            AWS.SrchStatus status = null;
            string bookmark = string.Empty;
            /******************************************************************* Find files with same document name *********************************************************/
            AWS.PropDef[] filePropertyDefinitions = webService.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE");
            AWS.PropDef fileNameProperty = filePropertyDefinitions.Single(n => n.SysName == "ClientFileName");
            AWS.SrchCond fileNameCondition = new AWS.SrchCond
            {
                PropDefId = fileNameProperty.Id,
                PropTyp = AWS.PropertySearchType.SingleProperty,
                SrchOper = 1,
                SrchRule = AWS.SearchRuleType.Must,
                SrchTxt = name
            };

            /*****************************************************************************************************************************************************************/
            while (status == null || files.Count < status.TotalHits)
            {
                AWS.FileFolder[] filesWithSameDocumentName = webService.DocumentService.FindFileFoldersBySearchConditions(new AWS.SrchCond[] { fileNameCondition }, null, null, true, true, ref bookmark, out status);
                if (filesWithSameDocumentName != null)
                    files.AddRange(filesWithSameDocumentName);
            }
            return files;
        }

        /**********************************************************************************************************************************************/
        public static List<AWS.FileFolder> GetFilesInFolder (ADSK.WebServicesTools.WebServiceManager webService, long id)
        {
            List<AWS.FileFolder> files = new List<AWS.FileFolder>();
            AWS.SrchStatus status = null;
            string bookmark = string.Empty;

            while (status == null || files.Count < status.TotalHits)
            {
                AWS.FileFolder[] results = webService.DocumentService.FindFileFoldersBySearchConditions(new AWS.SrchCond[] { }, null, new long[] { id }, true, true, ref bookmark, out status);
                if (results != null)
                    files.AddRange(results);
            }

            return files;
        }
    }
}
