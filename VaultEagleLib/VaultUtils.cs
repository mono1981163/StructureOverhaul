using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Common.DotNet.Extensions;
using Vault = Autodesk.DataManagement.Client.Framework.Vault;
using AWS = Autodesk.Connectivity.WebServices;
namespace VaultEagle
{
    public class VaultUtils
    {

        public static string GetLocalFolder(string vaultFolderPath, string localPath, List<Tuple<string, string>> folderMappings)
        {
            string vaultFolder = localPath + System.IO.Path.Combine(vaultFolderPath.Substring(1)).Replace('/', '\\');
            foreach (Tuple<string, string> folderMapping in folderMappings)
            {
                if (folderMapping.Item1.Equals(vaultFolderPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    string server = GetServerName(localPath);
                    vaultFolder = /*server + "\\" +*/ folderMapping.Item2;
                }
                else if (vaultFolderPath.StartsWith(folderMapping.Item1, StringComparison.InvariantCultureIgnoreCase))
                {
                    string server = GetServerName(localPath);
                    vaultFolder = /*server + "\\" +*/ folderMapping.Item2+ "\\" + vaultFolderPath.Substring(folderMapping.Item1.Count()+1).Split("/").StringJoin("\\");
                }
            }
            return vaultFolder;
        }

        public static string GetServerName(string path)
        {
            string serverName;

            if (path.IndexOf("\\") == 0) //unc path
                serverName = path.Substring(0, path.IndexOf("\\", 3));
            else // not unc path
                serverName = path.Substring(0, path.IndexOf("\\", 0));
            return serverName;
        }

        public static bool IsShared(Vault.Currency.Connections.Connection connection, AWS.File file)
        {
            return connection.WebServiceManager.DocumentService.GetFoldersByFileMasterId(file.MasterId).Length > 1;
        }


        public enum FileState { Ok, Obsolete, None }

        public static FileState GetLastRelevantFileState(int fileVersion, long fileMasterId, Vault.Currency.Connections.Connection connection, ref AWS.File releasedFile, List<string> states, List<string> invalidStates)
        {
            AWS.File file = connection.WebServiceManager.DocumentService.GetFileByVersion(fileMasterId, fileVersion);

            foreach (string state in states)
            {
                if (file.FileLfCyc.LfCycStateName.Equals(state, StringComparison.CurrentCultureIgnoreCase))
                {
                    releasedFile = file;
                    return FileState.Ok;
                }
               // else if (file.FileLfCyc.LfCycStateName.Equals("Obsolete", StringComparison.CurrentCultureIgnoreCase))
                  //  return FileState.Obsolete;
            }
            foreach (string state in invalidStates)
            {
                if (file.FileLfCyc.LfCycStateName.Equals(state, StringComparison.CurrentCultureIgnoreCase))
                    return FileState.Obsolete;
            }
            try
            {
                if (string.IsNullOrWhiteSpace(file.FileLfCyc.LfCycStateName))
                    return FileState.None;
                return fileVersion == 1 ? FileState.None : GetLastRelevantFileState(fileVersion - 1, fileMasterId, connection, ref releasedFile, states, invalidStates);
            }
            catch
            {
                return FileState.None;
            }
        }

        /*************************************************************************************/
        public static T HandleNetworkErrors<T>(Func<T> function, int retries)
        {
            Option<string> error = Option.None;
            for (int i = 0; i < retries; ++i)
            {
                T result = default(T);
                try
                {
                    /*result = */return function();
                  //  return result;
                }
                catch (Exception ex)
                {
                    //  Matrix.VaultCore.LogHandler.TraceLog("Network error, remaining retries: " + (retries - i - 1).ToString() + ". Message: " + ex.Message);
                    error = ex.Message.AsOption(); ;
                    System.Threading.Thread.Sleep(1000);
                }
            }
            if (error.IsSome)
                throw new Exception(error.Get());
            else
                throw new Exception("Too many network errors.");
        }

        /******************************************************************************************/
        public static void HandleNetworkErrors(Action action, int retries)
        {
            HandleNetworkErrors(() => { action(); return 0; }, retries);

        }

        /********************************************************************************************/
        public static bool CheckIfShared(Vault.Currency.Connections.Connection connection, AWS.File file)
        {
           AWS.Folder[] folders = connection.WebServiceManager.DocumentService.GetFoldersByFileMasterId(file.MasterId);

            return (folders.Length > 1);
        }


    }
}
