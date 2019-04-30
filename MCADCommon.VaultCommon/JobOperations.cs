using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WS = Autodesk.Connectivity.WebServices;

namespace MCADCommon.VaultCommon
{
    public static class JobOperations
    {
        ///////////////////////////////////////////////////////////////////////////////////////////
        public static void CreateJob(WS.JobService service, string type, string description, Dictionary<string, string> parameters, int priority)
        {
            List<WS.JobParam> jobParameters = new List<WS.JobParam>();
            foreach (KeyValuePair<string, string> parameter in parameters)
                jobParameters.Add(new WS.JobParam { Name = parameter.Key, Val = parameter.Value });

            // if two different parts/assemblies have the same .idw (I dunno, because reason) this will 
            // prevent SoapException 237 - the second .pdf will just overwrite the first which is okey
            //if(type != "mcadraccoon.transmittalnote.exportbom")
                
              //  jobParameters.Add(new WS.JobParam { Name = "GuidToPreventSoap237", Val = Guid.NewGuid().ToString() });

            service.AddJob(type, description, jobParameters.ToArray(), priority);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public static bool JobExists(WS.JobService jobService, string jobType, string message, List<WS.JobParam> jobParams, int priority)
        {
            DateTime date = DateTime.Today.AddDays(-10);
            WS.Job[] jobs = jobService.GetJobsByDate(50, date);
            if (jobs == null)
                return false;
            foreach (WS.Job job in jobs)
            {
                if (!string.Equals(job.Typ, jobType, StringComparison.InvariantCultureIgnoreCase))
                    continue;
                if (!string.Equals(job.Descr, message, StringComparison.InvariantCultureIgnoreCase))
                    continue;
                if (priority != job.Priority)
                    continue;

                int i = 0;
                bool equals = true;
                if (!(job.ParamArray.Count() == jobParams.Count))
                    continue;
                foreach (WS.JobParam param in job.ParamArray)
                {
                    if (!string.Equals(param.Name, jobParams[i].Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        equals = false;
                        break;
                    }
                    if (!string.Equals(param.Val, jobParams[i].Val, StringComparison.InvariantCultureIgnoreCase))
                    {
                        equals = false;
                        break;
                    }
                    i++;
                }

                if (equals)
                    return true;
            }

            return false;
        }
    }
}
