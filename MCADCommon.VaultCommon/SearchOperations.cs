using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ADSK = Autodesk.Connectivity;
using AWS = Autodesk.Connectivity.WebServices;
using Vault = Autodesk.DataManagement.Client.Framework.Vault;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

namespace MCADCommon.VaultCommon
{
    public class SearchOperations
    {
        public static List<AWS.File> FindFilesByConditions(AWS.SrchCond[] conditions, ADSK.WebServicesTools.WebServiceManager webService, long[] folderIDs = null)
        {
            string bookmark = string.Empty;
            AWS.SrchStatus status = null;
            List<AWS.File> totalResults = new List<AWS.File>();
            while (status == null || totalResults.Count < status.TotalHits)
            {
                AWS.File[] results = webService.DocumentService.FindFilesBySearchConditions(
                conditions,
                    null, folderIDs, true, true, ref bookmark, out status);

                if (results != null)
                    totalResults.AddRange(results);
                else
                    break;
            }
            return totalResults;
        }

        public static AWS.SrchCond GetAllCheckedInTodaySrchCond(ADSK.WebServicesTools.WebServiceManager webService)
        {
            long checkedInId = webService.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE").First(p => p.DispName == "Checked In").Id;
            DateTime oneDayAgo = DateTime.Today.Subtract(new TimeSpan(24, 0, 0));
            string onCorrectForm = oneDayAgo.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            return new AWS.SrchCond()
            {
                PropDefId = checkedInId,
                PropTyp = AWS.PropertySearchType.SingleProperty,
                SrchOper = 6, // is Greater than
                SrchRule = AWS.SearchRuleType.Must,
                SrchTxt = onCorrectForm
            };
        }

        public static AWS.SrchCond GetAllCheckedInSrchCond(ADSK.WebServicesTools.WebServiceManager webService)
        {
            long checkedOutById = webService.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE").First(p => p.DispName == "Checked Out By").Id;
            return  new AWS.SrchCond
                    {
                        PropDefId = checkedOutById,
                        PropTyp = AWS.PropertySearchType.SingleProperty,
                        SrchOper = 4, // is empty
                        SrchRule = AWS.SearchRuleType.Must
                    };
        }

        public static AWS.SrchCond GetAllWithPlotDateSrchCond(ADSK.WebServicesTools.WebServiceManager webService)
        {
            long plotDateId = webService.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE").First(p => p.DispName == "Date Exported").Id;
            return new AWS.SrchCond()
            {
                PropDefId = plotDateId,
                PropTyp = AWS.PropertySearchType.SingleProperty,
                SrchOper = 6, // is Greater than
                SrchRule = AWS.SearchRuleType.Must,
                SrchTxt = "01/01/1900 00:00:01"
            };
        }

        public static List<AWS.File> RemoveWithOlderPlotDateThenCheckedInDate(List<AWS.File> totalResults, ADSK.WebServicesTools.WebServiceManager webService, Regex toMatch)
        {
            List<AWS.File> filesThatMeetsCriteria = new List<AWS.File>();
            long checkedInId = webService.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE").First(p => p.DispName == "Checked In").Id;
            long plotDateId = webService.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE").First(p => p.DispName == "Date Exported").Id;
            foreach (AWS.File file in totalResults)
            {
                if (toMatch.IsMatch(file.Name))
                {
                    AWS.PropInst[] props = webService.PropertyService.GetProperties("FILE", new long[] { file.Id }, new long[] { checkedInId, plotDateId });
                    DateTime checkInDate = (DateTime)props[0].Val;
                    DateTime plotDate = (DateTime)props[1].Val;
                    if (DateTime.Compare(checkInDate.Subtract(new TimeSpan(0, 30, 0)), plotDate) > 0)
                        filesThatMeetsCriteria.Add(file);
                }
            }
            return filesThatMeetsCriteria;
        }
    }
}
