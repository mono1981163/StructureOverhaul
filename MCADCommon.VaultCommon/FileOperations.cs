using Common.DotNet.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AWS = Autodesk.Connectivity.WebServices;
using VDF = Autodesk.DataManagement.Client.Framework;
using ADSK = Autodesk.Connectivity;

namespace MCADCommon.VaultCommon
{
    public static class FileOperations
    {
        /*****************************************************************************************/
        public static Option<AWS.File> GetFile(VDF.Vault.Currency.Connections.Connection connection, string vaultPath)
        {
            AWS.File[] files = connection.WebServiceManager.DocumentService.FindLatestFilesByPaths(new string[] { vaultPath });
            if (files[0].Id == -1)
                return Option.None;

            return files[0].AsOption();
        }

        /*****************************************************************************************/
        public static void DeleteIfExisting(VDF.Vault.Currency.Connections.Connection connection, VDF.Vault.Currency.Entities.Folder folder, string fileName)
        {
            try
            {
                AWS.File[] awsFiles = connection.WebServiceManager.DocumentService.FindLatestFilesByPaths(new string[] { folder.FullName + "/" + fileName });

                if (awsFiles[0].Id == -1)
                    return;

                foreach (AWS.File awsFile in awsFiles)
                    connection.WebServiceManager.DocumentService.DeleteFileFromFolder(awsFile.MasterId, folder.Id);
            }
            catch
            {
                throw new ErrorMessageException("Could not delete file in Vault: " + fileName + ".");
            }
        }

        public static AWS.File RenameFile(VDF.Vault.Currency.Connections.Connection connection, AWS.File file, string folderPath, string newName)
        {
            string localPath = connection.WorkingFoldersManager.GetWorkingFolder(folderPath).FullPath;
            if (!Directory.Exists(localPath))
                Directory.CreateDirectory(localPath);

            VDF.Vault.Currency.Entities.FileIteration fileIteration = new VDF.Vault.Currency.Entities.FileIteration(connection, file);

            

            VDF.Vault.Settings.AcquireFilesSettings settings = new VDF.Vault.Settings.AcquireFilesSettings(connection);
            settings.OptionsRelationshipGathering.FileRelationshipSettings.VersionGatheringOption = VDF.Vault.Currency.VersionGatheringOption.Latest;
            settings.OptionsResolution.OverwriteOption = VDF.Vault.Settings.AcquireFilesSettings.AcquireFileResolutionOptions.OverwriteOptions.ForceOverwriteAll;
            settings.LocalPath = new VDF.Currency.FolderPathAbsolute(localPath);
            settings.AddFileToAcquire(fileIteration, VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download | VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Checkout);
            VDF.Vault.Results.AcquireFilesResults result = connection.FileManager.AcquireFiles(settings);
            AWS.File downloadedFile = connection.WebServiceManager.DocumentService.GetFileById(result.FileResults.SingleOrDefault().File.EntityIterationId);

            VDF.Vault.Currency.Entities.FileIteration newIteration = new VDF.Vault.Currency.Entities.FileIteration(connection, downloadedFile);

            Option<List<AWS.FileAssocLite>> oldAssociations = GetFileChildrenAssocsLite(connection, newIteration);
            if (oldAssociations.IsNone)
                throw new Exception("File: " + file.Name + " has no association.");
            string filePath = Path.Combine(localPath, downloadedFile.Name);

            List<AWS.FileAssocParam> newAssociations = UpdateFileAssociations(connection, newIteration, oldAssociations.Get());

            VDF.Vault.Currency.Entities.FileIteration ff = connection.FileManager.CheckinFile(newIteration, "Changing name on file.",
                false, newAssociations.ToArray(), null, true,
                newName, downloadedFile.FileClass,
                false, new VDF.Currency.FilePathAbsolute(filePath));

            return connection.WebServiceManager.DocumentService.GetFileById(ff.EntityIterationId);
        }

        public static AWS.File RenameFile(VDF.Vault.Currency.Connections.Connection connection, AWS.FileFolder fileFolder, string newName)
        {
            return RenameFile(connection, fileFolder.File, fileFolder.Folder.FullName, newName);
            //SetProperties(connection, properties);
        }

        /*****************************************************************************************/
        public static VDF.Vault.Results.FileAcquisitionResult DownloadFile(VDF.Vault.Currency.Connections.Connection connection, AWS.Folder folder, AWS.File file, bool useWorkspace = false, string tempGuid = "", string path = "", bool useFolderStructureAndPath = false)
        {
            VDF.Vault.Currency.Entities.FileIteration iteration = new VDF.Vault.Currency.Entities.FileIteration(connection, new VDF.Vault.Currency.Entities.Folder(connection, folder), file);
            VDF.Vault.Settings.AcquireFilesSettings settings = new VDF.Vault.Settings.AcquireFilesSettings(connection, true);
            if (!useWorkspace && !String.IsNullOrWhiteSpace(tempGuid))
                settings.LocalPath = new VDF.Currency.FolderPathAbsolute(Path.Combine(Path.GetTempPath(), tempGuid));
            if (!String.IsNullOrWhiteSpace(path) && useFolderStructureAndPath)
            {
                string[] splitName = folder.FullName.Split('/');
                foreach (string s in splitName)
                    path = Path.Combine(path, s);
            }
            if (!String.IsNullOrWhiteSpace(path))
                settings.LocalPath = new VDF.Currency.FolderPathAbsolute(path);
            settings.AddFileToAcquire(iteration, VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download);
            VDF.Vault.Results.AcquireFilesResults results = connection.FileManager.AcquireFiles(settings);
            return results.FileResults.First();
        }

        /*****************************************************************************************/
        public static VDF.Vault.Results.AcquireFilesResults DownloadFilesToPaths(VDF.Vault.Currency.Connections.Connection connection, List<Tuple<AWS.File, string, bool, bool>> filesAndPaths)
        {
            VDF.Vault.Settings.AcquireFilesSettings settings = new VDF.Vault.Settings.AcquireFilesSettings(connection, true);
            settings.OptionsResolution.SyncWithRemoteSiteSetting = VDF.Vault.Settings.AcquireFilesSettings.SyncWithRemoteSite.Always;
            settings.OptionsResolution.OverwriteOption = VDF.Vault.Settings.AcquireFilesSettings.AcquireFileResolutionOptions.OverwriteOptions.ForceOverwriteAll;
            foreach (Tuple<AWS.File, string, bool, bool> file in filesAndPaths)
            {
                try
                {
                    if (file.Item1 != null)
                    {
                        VDF.Vault.Currency.Entities.FileIteration iteration = new VDF.Vault.Currency.Entities.FileIteration(connection, file.Item1);
                        // settings.LocalPath = new VDF.Currency.FolderPathAbsolute(file.Item2);
                        VDF.Currency.FolderPathAbsolute path = new VDF.Currency.FolderPathAbsolute(file.Item2);
                        settings.AddFileToAcquire(iteration, VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download, path);
                    }
                }
                catch (Exception ex)
                {

                }
            }
            VDF.Vault.Results.AcquireFilesResults results = connection.FileManager.AcquireFiles(settings);
            return results;
        }

        /*****************************************************************************************/
        public static VDF.Vault.Results.FileAcquisitionResult DownloadAssembly(VDF.Vault.Currency.Connections.Connection connection, AWS.Folder folder, AWS.File file, bool useWorkspace, string tempGuid = "", string path = "", bool useFolderStructureAndPath = false)
        {
            VDF.Vault.Currency.Entities.FileIteration iteration = new VDF.Vault.Currency.Entities.FileIteration(connection, new VDF.Vault.Currency.Entities.Folder(connection, folder), file);
            VDF.Vault.Settings.AcquireFilesSettings settings = new VDF.Vault.Settings.AcquireFilesSettings(connection, true);
            if (!useWorkspace && !String.IsNullOrWhiteSpace(tempGuid))
                settings.LocalPath = new VDF.Currency.FolderPathAbsolute(Path.Combine(Path.GetTempPath(), tempGuid));
            if (!String.IsNullOrWhiteSpace(path) && useFolderStructureAndPath)
            {
                /*string[] splitName = folder.FullName.Split('/');
                for (int i = 0; i < splitName.Length-1;i++)
                    path = Path.Combine(path, splitName[i]);*/
                settings.OrganizeFilesRelativeToCommonVaultRoot = true;
                settings.OrganizeFilesRelativeToCommonVaultRoot = true;
                settings.LocalPath = new VDF.Currency.FolderPathAbsolute(path);
            }
            settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeChildren = true;
            settings.OptionsRelationshipGathering.FileRelationshipSettings.RecurseChildren = true;
            settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeLibraryContents = true;
            settings.OptionsResolution.OverwriteOption = VDF.Vault.Settings.AcquireFilesSettings.AcquireFileResolutionOptions.OverwriteOptions.ForceOverwriteAll;
            settings.OptionsResolution.SyncWithRemoteSiteSetting = VDF.Vault.Settings.AcquireFilesSettings.SyncWithRemoteSite.Always;
            settings.OptionsRelationshipGathering.FileRelationshipSettings.VersionGatheringOption = VDF.Vault.Currency.VersionGatheringOption.Latest;
            settings.AddFileToAcquire(iteration, VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download);
            VDF.Vault.Results.AcquireFilesResults results = connection.FileManager.AcquireFiles(settings);
            Option<VDF.Vault.Results.FileAcquisitionResult> result = Option.None;
            foreach (VDF.Vault.Results.FileAcquisitionResult resultIAM in results.FileResults)
            {
                if (String.Equals(resultIAM.File.EntityName, file.Name))
                    result = resultIAM.AsOption();
            }
            return result.Get();
        }


        /*****************************************************************************************/
        public static VDF.Vault.Results.FileAcquisitionResult DownloadAssemblyAndSetWorkingFolder(VDF.Vault.Currency.Connections.Connection connection, AWS.Folder folder, AWS.File file, string path)
        {
            VDF.Vault.Currency.Entities.FileIteration iteration = new VDF.Vault.Currency.Entities.FileIteration(connection, new VDF.Vault.Currency.Entities.Folder(connection, folder), file);
            VDF.Vault.Settings.AcquireFilesSettings settings = new VDF.Vault.Settings.AcquireFilesSettings(connection, true);

            connection.WorkingFoldersManager.SetWorkingFolder("$", new VDF.Currency.FolderPathAbsolute(Path.Combine(path, "$")));
            settings.OrganizeFilesRelativeToCommonVaultRoot = true;
            //settings.LocalPath = new VDF.Currency.FolderPathAbsolute(path);
            settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeChildren = true;
            settings.OptionsRelationshipGathering.FileRelationshipSettings.RecurseChildren = true;
            settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeLibraryContents = true;
            settings.OptionsResolution.SyncWithRemoteSiteSetting = VDF.Vault.Settings.AcquireFilesSettings.SyncWithRemoteSite.Always;
            settings.OptionsRelationshipGathering.FileRelationshipSettings.VersionGatheringOption = VDF.Vault.Currency.VersionGatheringOption.Latest;
            settings.AddFileToAcquire(iteration, VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download);
            VDF.Vault.Results.AcquireFilesResults results = connection.FileManager.AcquireFiles(settings);
            Option<VDF.Vault.Results.FileAcquisitionResult> result = Option.None;
            foreach (VDF.Vault.Results.FileAcquisitionResult resultIAM in results.FileResults)
            {
                if (String.Equals(resultIAM.File.EntityName, file.Name))
                    result = resultIAM.AsOption();
            }
            return result.Get();
        }

        /*****************************************************************************************/
        public static bool SetProperty(VDF.Vault.Currency.Connections.Connection connection, AWS.File file, string propertyName, object value, string tempPath)
        {
            /*   VDF.Vault.Currency.Entities.FileIteration iteration = new VDF.Vault.Currency.Entities.FileIteration(connection, new VDF.Vault.Currency.Entities.Folder(connection, folder), file);
               VDF.Vault.Settings.AcquireFilesSettings settings = new VDF.Vault.Settings.AcquireFilesSettings(connection, true);

               connection.WorkingFoldersManager.SetWorkingFolder("$", new VDF.Currency.FolderPathAbsolute(Path.Combine(path, "$")));
               settings.OrganizeFilesRelativeToCommonVaultRoot = true;
               //settings.LocalPath = new VDF.Currency.FolderPathAbsolute(path);
               settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeChildren = true;
               settings.OptionsRelationshipGathering.FileRelationshipSettings.RecurseChildren = true;
               settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeLibraryContents = true;
               settings.OptionsResolution.SyncWithRemoteSiteSetting = VDF.Vault.Settings.AcquireFilesSettings.SyncWithRemoteSite.Always;
               settings.OptionsRelationshipGathering.FileRelationshipSettings.VersionGatheringOption = VDF.Vault.Currency.VersionGatheringOption.Latest;
               settings.AddFileToAcquire(iteration, VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download);
               VDF.Vault.Results.AcquireFilesResults results = connection.FileManager.AcquireFiles(settings);
               Option<VDF.Vault.Results.FileAcquisitionResult> result = Option.None;
               foreach (VDF.Vault.Results.FileAcquisitionResult resultIAM in results.FileResults)
               {
                   if (String.Equals(resultIAM.File.EntityName, file.Name))
                       result = resultIAM.AsOption();
               }
               return result.Get(); */
            throw new NotImplementedException();
        }

        /*****************************************************************************************/
        public static bool SetProperty(VDF.Vault.Currency.Connections.Connection connection, AWS.File file, string propertyName, object value, string tempPath, bool forceDownload = false)
        {
            long userId = connection.UserID;
            if (((file.CheckedOut) && (userId != file.CkOutUserId)) || (file.Locked))
                return false;
            Option<AWS.PropDef> definition = PropertyOperations.GetPropertyDefinition(connection, propertyName);
            if (definition.IsNone)
                return false;

            VDF.Vault.Currency.Entities.FileIteration fileIteration = new VDF.Vault.Currency.Entities.FileIteration(connection, file);

            Option<List<AWS.FileAssocLite>> oldAssociations = GetFileChildrenAssocsLite(connection, fileIteration);
            if (oldAssociations.IsNone)
                return false;

            AWS.FileAssocArray[] files = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { file.MasterId }, AWS.FileAssociationTypeEnum.None, false, AWS.FileAssociationTypeEnum.Dependency, true, true, true, true);
            bool childCheckedOutByOther = false;
            foreach (AWS.FileAssocArray tempFiles in files)
            {
                try
                {
                    foreach (AWS.FileAssoc tempFile in tempFiles.FileAssocs)
                    {
                        if ((tempFile.CldFile.CheckedOut) && (tempFile.CldFile.CkOutUserId == userId))
                        {
                            VDF.Vault.Currency.Entities.FileIteration tempIteration = new VDF.Vault.Currency.Entities.FileIteration(connection, tempFile.CldFile);
                            connection.FileManager.UndoCheckoutFile(tempIteration);
                        }
                        else if ((tempFile.CldFile.CheckedOut))
                            childCheckedOutByOther = true;
                    }
                }
                catch { }

            }

            if (childCheckedOutByOther)
                return false;



            VDF.Vault.Settings.AcquireFilesSettings acquireFilesSettings = new VDF.Vault.Settings.AcquireFilesSettings(connection, true);
            acquireFilesSettings.OptionsResolution.OverwriteOption = VDF.Vault.Settings.AcquireFilesSettings.AcquireFileResolutionOptions.OverwriteOptions.ForceOverwriteAll;
            acquireFilesSettings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeAttachments = false;
            acquireFilesSettings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeChildren = false;  // true? 
            acquireFilesSettings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeRelatedDocumentation = true;
            acquireFilesSettings.OptionsRelationshipGathering.FileRelationshipSettings.RecurseChildren = false; // true?
            acquireFilesSettings.OptionsRelationshipGathering.FileRelationshipSettings.VersionGatheringOption = VDF.Vault.Currency.VersionGatheringOption.Latest; ;
            acquireFilesSettings.LocalPath = new VDF.Currency.FolderPathAbsolute(tempPath);
            VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption downloadSetting = forceDownload ? VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Checkout | VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download : file.CheckedOut ? VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download : VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Checkout;
            acquireFilesSettings.AddFileToAcquire(fileIteration, /*VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Checkout | VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download*/downloadSetting);

            try
            {
                VDF.Vault.Results.AcquireFilesResults acquireFilesResults = connection.FileManager.AcquireFiles(acquireFilesSettings);
                if (acquireFilesResults.FileResults.First().Status != VDF.Vault.Results.FileAcquisitionResult.AcquisitionStatus.Success)
                    return false;


                AWS.PropInstParam property = new AWS.PropInstParam { PropDefId = definition.Get().Id, Val = value };

                connection.WebServiceManager.DocumentService.UpdateFileProperties(new long[] { file.MasterId }, new AWS.PropInstParamArray[] { new AWS.PropInstParamArray { Items = new AWS.PropInstParam[] { property } } });
                List<AWS.FileAssocParam> newAssociations = UpdateFileAssociations(connection, fileIteration, oldAssociations.Get());

                connection.FileManager.CheckinFile(fileIteration,
                                                           comment: "Updated property: " + propertyName + " to " + value.ToString(),
                                                           keepCheckedOut: false,
                                                           associations: newAssociations.ToArray(),
                                                           bom: null,
                                                           copyBom: false,
                                                           newFileName: null,
                                                           classification: fileIteration.FileClassification,
                                                           hidden: false,
                                                           filePath: new VDF.Currency.FilePathAbsolute(tempPath + "/" + file.Name));

                return true;
            }
            catch (Exception ex)
            {
                connection.FileManager.UndoCheckoutFile(fileIteration);
                throw new ErrorMessageException("Child is checked out.");
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public static List<long> GetFileChildAssociations(VDF.Vault.Currency.Connections.Connection connection, long fileId)
        {
            return GetFileAssociations(connection, fileId, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public static List<long> GetFileParentAssociations(VDF.Vault.Currency.Connections.Connection connection, long fileId)
        {
            return GetFileAssociations(connection, fileId, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        private static List<long> GetFileAssociations(VDF.Vault.Currency.Connections.Connection connection, long fileId, bool children)
        {
            VDF.Vault.Settings.FileRelationshipGatheringSettings settings = new VDF.Vault.Settings.FileRelationshipGatheringSettings
            {
                IncludeAttachments = true,
                IncludeChildren = children,
                IncludeParents = !children,
                IncludeRelatedDocumentation = false,
                VersionGatheringOption = VDF.Vault.Currency.VersionGatheringOption.Latest
            };

            IEnumerable<AWS.FileAssocLite> associations = connection.FileManager.GetFileAssociationLites(new List<long> { fileId }, settings);

            if (associations == null)
                return new List<long>();

            if (children)
                return associations.Select(a => a.CldFileId).ToList();
            else
                return associations.Select(a => a.ParFileId).ToList();
        }

        /*****************************************************************************************/
        public static Option<List<AWS.FileAssocLite>> GetFileChildrenAssocsLite(VDF.Vault.Currency.Connections.Connection connection, VDF.Vault.Currency.Entities.FileIteration fileIteration)
        {
            VDF.Vault.Settings.FileRelationshipGatheringSettings fileRelationshipGatheringSettings = new VDF.Vault.Settings.FileRelationshipGatheringSettings();
            fileRelationshipGatheringSettings.IncludeAttachments = true;
            fileRelationshipGatheringSettings.IncludeChildren = true;
            fileRelationshipGatheringSettings.IncludeParents = false;
            fileRelationshipGatheringSettings.IncludeRelatedDocumentation = true;
            fileRelationshipGatheringSettings.VersionGatheringOption = VDF.Vault.Currency.VersionGatheringOption.Latest;

            IEnumerable<AWS.FileAssocLite> associations = connection.FileManager.GetFileAssociationLites(new long[] { fileIteration.EntityIterationId }, fileRelationshipGatheringSettings);
            if (associations == null)
                return Option.None;

            return associations.ToList().AsOption();
        }

        /*****************************************************************************************/
        public static List<AWS.FileAssocParam> UpdateFileAssociations(VDF.Vault.Currency.Connections.Connection connection,
                                                                 VDF.Vault.Currency.Entities.FileIteration fileIterationParent,
                                                                 List<AWS.FileAssocLite> fileAssocsLite)
        {
            List<AWS.FileAssocParam> fileAssocParams = new List<AWS.FileAssocParam>();

            foreach (AWS.FileAssocLite fileAssocLite in fileAssocsLite)
            {
                AWS.File childAsReferenced = connection.WebServiceManager.DocumentService.GetFileById(fileAssocLite.CldFileId);

                // of any reason, the parent is referenced by it self in the list, but we dont want that
                if (fileIterationParent.EntityMasterId == childAsReferenced.MasterId)
                    continue;

                AWS.File childLatest = connection.WebServiceManager.DocumentService.GetLatestFileByMasterId(childAsReferenced.MasterId);

                AWS.FileAssocParam fileAssocParam = new AWS.FileAssocParam();

                fileAssocParam.CldFileId = childLatest.Id;
                fileAssocParam.RefId = fileAssocLite.RefId;
                fileAssocParam.Source = fileAssocLite.Source;
                fileAssocParam.Typ = fileAssocLite.Typ;
                fileAssocParam.ExpectedVaultPath = fileAssocLite.ExpectedVaultPath;
                fileAssocParams.Add(fileAssocParam);
            }

            return fileAssocParams;
        }

        /*****************************************************************************************/
        public static List<string> GetSelectedFilesWithValidExtension(Autodesk.DataManagement.Client.Framework.Vault.Currency.Connections.Connection connection, IEnumerable<ADSK.Explorer.Extensibility.ISelection> currentSelectionSet, List<string> validExtensions)
        {
            List<string> currentSelectedFiles = new List<string>();
            foreach (ADSK.Explorer.Extensibility.ISelection selection in currentSelectionSet)
            {
                if (selection.TypeId.Equals(Autodesk.Connectivity.Explorer.Extensibility.SelectionTypeId.File))
                {
                    AWS.File file;
                    if (selection.TypeId.EntityClassId.Equals("File", StringComparison.InvariantCultureIgnoreCase) && selection.TypeId.SelectionContext.Equals("FileMaster", StringComparison.InvariantCultureIgnoreCase))
                        file = connection.WebServiceManager.DocumentService.GetLatestFileByMasterId(selection.Id);
                    else if (selection.TypeId.EntityClassId.Equals("File", StringComparison.InvariantCultureIgnoreCase) && selection.TypeId.SelectionContext.Equals("FileVersion", StringComparison.InvariantCultureIgnoreCase))
                        file = connection.WebServiceManager.DocumentService.GetLatestFilesByIds(new long[] { selection.Id })[0];
                    else
                        throw new ErrorMessageException("Incorrect selection type.");
                    AWS.Folder folder = connection.WebServiceManager.DocumentService.GetFolderById(file.FolderId);

                    string fileName = Path.Combine(folder.FullName, file.Name).Replace("\\", "/");
                    if (!IsValidFile(fileName, validExtensions))
                        continue;

                    currentSelectedFiles.Add(fileName);
                }
            }
            return currentSelectedFiles;
        }

        /*****************************************************************************************/
        private static bool IsValidFile(string fileName, List<string> validExtensions)
        {
            string fileNameExtension = Path.GetExtension(fileName).ToLower();
            return validExtensions.Contains(fileNameExtension);
        }

        /*****************************************************************************************/
        public static Tuple<List<long>, List<long>> GetCheckedOutAndLockedFiles(long masterId, VDF.Vault.Currency.Connections.Connection connection)
        {
            List<long> corruptFiles = new List<long>();
            List<long> relations = new List<long>();
            AWS.FileAssocArray[] files = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { masterId }, AWS.FileAssociationTypeEnum.None, false, AWS.FileAssociationTypeEnum.Dependency, true, false, false, false);
            foreach (AWS.FileAssocArray tempFiles in files)
            {
                try
                {
                    foreach (AWS.FileAssoc tempFile in tempFiles.FileAssocs)
                    {
                        relations.Add(tempFile.CldFile.MasterId);
                        if ((tempFile.CldFile.CheckedOut /*|| tempFile.CldFile.Locked*/) && !corruptFiles.Contains(tempFile.CldFile.MasterId))
                        {
                            corruptFiles.Add(tempFile.CldFile.MasterId);
                            relations.AddRange(GetParents(tempFile.CldFile.MasterId, connection));
                        }
                    }
                }
                catch { }

            }
            try
            {
                AWS.File file = connection.WebServiceManager.DocumentService.GetLatestFileByMasterId(masterId);
                if ((file.CheckedOut || file.Locked) && !corruptFiles.Contains(file.MasterId))
                    corruptFiles.Add(file.MasterId);

                relations.AddRange(GetParents(file.MasterId, connection));
            }
            catch { }
            return new Tuple<List<long>, List<long>>(corruptFiles, relations);
        }

        /*****************************************************************************************/
        private static List<long> GetParents(long masterId, VDF.Vault.Currency.Connections.Connection connection)
        {
            List<long> parents = new List<long>();
            try
            {
                AWS.FileAssocArray[] files2 = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { masterId }, AWS.FileAssociationTypeEnum.All, false, AWS.FileAssociationTypeEnum.None, false, false, false, false);
                foreach (AWS.FileAssocArray tempFiles in files2)
                {
                    foreach (AWS.FileAssoc tempFile in tempFiles.FileAssocs)
                    {
                        FileInfo file = new FileInfo(tempFile.CldFile.Name);
                        if (file.Extension.Equals(".idw", StringComparison.InvariantCultureIgnoreCase) || file.Extension.Equals(".dwg", StringComparison.InvariantCultureIgnoreCase))
                            parents.Add(tempFile.CldFile.MasterId);

                        FileInfo file2 = new FileInfo(tempFile.ParFile.Name);
                        if (file2.Extension.Equals(".idw", StringComparison.InvariantCultureIgnoreCase) || file2.Extension.Equals(".dwg", StringComparison.InvariantCultureIgnoreCase))
                            parents.Add(tempFile.ParFile.MasterId);
                    }

                }
            }
            catch { }

            return parents;
        }

        /*****************************************************************************************/
        public static List<long> GetActualParentsFirstLevel(long masterId, VDF.Vault.Currency.Connections.Connection connection)
        {
            List<long> parents = new List<long>();
            try
            {
                AWS.File[] files = connection.WebServiceManager.DocumentService.GetFilesByMasterId(masterId);
                //AWS.FileAssocArray[] files2 = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { masterId }, AWS.FileAssociationTypeEnum.All, false, AWS.FileAssociationTypeEnum.None, false, false, false, false);
                AWS.FileAssocArray[] files2 = connection.WebServiceManager.DocumentService.GetFileAssociationsByIds(new long[] { files[0].Id }, AWS.FileAssociationTypeEnum.All, false, AWS.FileAssociationTypeEnum.None, false, false, false);

                foreach (AWS.FileAssocArray tmp in files2)
                {
                    foreach (AWS.FileAssoc tmpfile in tmp.FileAssocs)
                    {
                        FileInfo file = new FileInfo(tmpfile.ParFile.Name);
                        if (file.Extension.Equals(".iam", StringComparison.InvariantCultureIgnoreCase) || file.Extension.Equals(".ipt", StringComparison.InvariantCultureIgnoreCase))
                            parents.Add(tmpfile.ParFile.MasterId);

                    }
                }

                /*foreach (AWS.FileAssocLite tempFiles in files2)
                {
                    AWS.File tmpFile = connection.WebServiceManager.DocumentService.GetFileById(tempFiles.CldFileId);
                    FileInfo file = new FileInfo(tmpFile.Name);
                    if (file.Extension.Equals(".iam", StringComparison.InvariantCultureIgnoreCase) || file.Extension.Equals(".ipt", StringComparison.InvariantCultureIgnoreCase))
                    {
                        parents.Add(tempFiles.ParFileId);
                    }

                }*/
            }
            catch (Exception e) { }

            return parents;
        }

        /*****************************************************************************************/
        public static void GetActualChildrenFirstLevel(long masterId, List<AWS.File> children, VDF.Vault.Currency.Connections.Connection connection)
        {
            AWS.FileAssocArray[] fileArray = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { masterId }, AWS.FileAssociationTypeEnum.None, false, AWS.FileAssociationTypeEnum.All, false, false, false, false);
            foreach (AWS.FileAssocArray tmp in fileArray)
            {
                foreach (AWS.FileAssoc tmpfile in tmp.FileAssocs)
                {
                    FileInfo file = new FileInfo(tmpfile.CldFile.Name);
                    if (file.Extension.Equals(".iam", StringComparison.InvariantCultureIgnoreCase) || file.Extension.Equals(".ipt", StringComparison.InvariantCultureIgnoreCase))
                        children.Add(tmpfile.CldFile);

                }
            }
        }

        /*****************************************************************************************/

        public static void GetActualChildren(long masterId, VDF.Vault.Currency.Connections.Connection connection, List<string[]> BOMinfo, bool recursive = true)
        {
            AWS.FileAssocArray[] fileArray = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { masterId }, AWS.FileAssociationTypeEnum.None, false, AWS.FileAssociationTypeEnum.All, false, false, false, false);

            foreach (AWS.FileAssocArray tmp in fileArray)
            {
                if (tmp.FileAssocs != null)
                {
                    foreach (AWS.FileAssoc tmpfile in tmp.FileAssocs)
                    {
                        AWS.FileAssocArray fa = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { tmpfile.CldFile.MasterId }, AWS.FileAssociationTypeEnum.None, false, AWS.FileAssociationTypeEnum.All, false, false, false, false)[0];

                        FileInfo file = new FileInfo(tmpfile.CldFile.Name);
                        if (file.Extension.Equals(".iam", StringComparison.InvariantCultureIgnoreCase) || file.Extension.Equals(".ipt", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string[] bom = new string[2];
                            bom[0] = tmpfile.CldFile.MasterId.ToString();
                            bom[1] = tmpfile.CldFile.Name;
                            BOMinfo.Add(bom);
                        }
                        if (fa.FileAssocs != null && recursive)
                        {
                            GetActualChildren(tmpfile.CldFile.MasterId, connection, BOMinfo);
                        }

                    }
                }
            }
        }

        /*****************************************************************************************/
        private static void GetChildren(long masterId, List<AWS.File> children, VDF.Vault.Currency.Connections.Connection connection, List<string[]> BOMinfo, bool recursive = true)
        {
            AWS.FileAssocArray[] fileArray = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { masterId }, AWS.FileAssociationTypeEnum.None, false, AWS.FileAssociationTypeEnum.All, false, false, false, false);


            foreach (AWS.FileAssocArray tmp in fileArray)
            {
                foreach (AWS.FileAssoc tmpfile in tmp.FileAssocs)
                {
                    AWS.FileAssocArray fa = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { tmpfile.CldFile.MasterId }, AWS.FileAssociationTypeEnum.None, false, AWS.FileAssociationTypeEnum.All, false, false, false, false)[0];

                    foreach (AWS.File f in GetParentsFiles(tmpfile.CldFile.MasterId, connection))
                    {
                        FileInfo file = new FileInfo(f.Name);
                        if (file.Extension.Equals(".idw", StringComparison.InvariantCultureIgnoreCase) || file.Extension.Equals(".dwg", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string[] bom = new string[2];
                            bom[0] = f.MasterId.ToString();
                            bom[1] = f.Name;
                            BOMinfo.Add(bom);
                            children.Add(f);
                        }
                    }
                    if (fa.FileAssocs != null && recursive)
                    {
                        GetChildren(tmpfile.CldFile.MasterId, children, connection, BOMinfo);
                    }

                }
            }

        }
        /*****************************************************************************************/
        public static List<AWS.File> GetDrawingsFromSelfAndClosestChildren(long masterId, VDF.Vault.Currency.Connections.Connection connection, bool excludeChildren = false)
        {
            List<string[]> BOMinfo = new List<string[]>();
            List<AWS.File> selfAndChildren = new List<AWS.File>();
            try
            {
                AWS.FileAssocArray[] par_files = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { masterId }, AWS.FileAssociationTypeEnum.All, false, AWS.FileAssociationTypeEnum.None, false, false, false, false);
                List<AWS.File> children = new List<AWS.File>();

                AWS.FileAssocArray fa = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { masterId }, AWS.FileAssociationTypeEnum.None, false, AWS.FileAssociationTypeEnum.All, false, false, false, false)[0];

                if (fa.FileAssocs != null && !excludeChildren)
                    GetChildren(masterId, children, connection, BOMinfo, false);

                if (par_files[0].FileAssocs != null)
                {
                    foreach (AWS.FileAssocArray tempFiles in par_files)
                    {
                        foreach (AWS.FileAssoc tempFile in tempFiles.FileAssocs)
                        {
                            FileInfo file = new FileInfo(tempFile.CldFile.Name);
                            if (file.Extension.Equals(".idw", StringComparison.InvariantCultureIgnoreCase) || file.Extension.Equals(".dwg", StringComparison.InvariantCultureIgnoreCase))
                                selfAndChildren.Add(tempFile.CldFile);

                            FileInfo file2 = new FileInfo(tempFile.ParFile.Name);
                            if (file2.Extension.Equals(".idw", StringComparison.InvariantCultureIgnoreCase) || file2.Extension.Equals(".dwg", StringComparison.InvariantCultureIgnoreCase))
                                selfAndChildren.Add(tempFile.ParFile);
                        }

                    }
                }
                foreach (AWS.File f in children)
                    selfAndChildren.Add(f);
            }
            catch { }
            return selfAndChildren;
        }

        /*****************************************************************************************/
        public static List<AWS.File> GetRelativesFiles(long masterId, VDF.Vault.Currency.Connections.Connection connection)
        {
            List<string[]> BOMinfo = new List<string[]>();
            List<AWS.File> relatives = new List<AWS.File>();
            try
            {
                AWS.FileAssocArray[] par_files2 = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { masterId }, AWS.FileAssociationTypeEnum.All, false, AWS.FileAssociationTypeEnum.None, false, false, false, false);
                List<AWS.File> children = new List<AWS.File>();

                AWS.FileAssocArray fa = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { masterId }, AWS.FileAssociationTypeEnum.None, false, AWS.FileAssociationTypeEnum.All, false, false, false, false)[0];

                if (fa.FileAssocs != null)
                {
                    GetChildren(masterId, children, connection, BOMinfo);
                }
                /*   foreach (AWS.FileAssocArray tmp in child_files2)
                   {
                       foreach (AWS.FileAssoc tmpfile in tmp.FileAssocs)
                       {
                       foreach (AWS.File f in GetParentsFiles(tmpfile.CldFile.MasterId, connection))
                       {
                           FileInfo file = new FileInfo(f.Name);
                           if (file.Extension.Equals(".idw", StringComparison.InvariantCultureIgnoreCase) || file.Extension.Equals(".dwg", StringComparison.InvariantCultureIgnoreCase))
                               relatives.Add(f);
                       }

                       }
                   }
                */
                if (par_files2[0].FileAssocs != null)
                {
                    foreach (AWS.FileAssocArray tempFiles in par_files2)
                    {
                        foreach (AWS.FileAssoc tempFile in tempFiles.FileAssocs)
                        {
                            FileInfo file = new FileInfo(tempFile.CldFile.Name);
                            if (file.Extension.Equals(".idw", StringComparison.InvariantCultureIgnoreCase) || file.Extension.Equals(".dwg", StringComparison.InvariantCultureIgnoreCase))
                                relatives.Add(tempFile.CldFile);

                            FileInfo file2 = new FileInfo(tempFile.ParFile.Name);
                            if (file2.Extension.Equals(".idw", StringComparison.InvariantCultureIgnoreCase) || file2.Extension.Equals(".dwg", StringComparison.InvariantCultureIgnoreCase))
                                relatives.Add(tempFile.ParFile);
                        }

                    }

                }
                foreach (AWS.File f in children)
                {
                    relatives.Add(f);
                }
            }
            catch { }

            return relatives;
        }

        /*****************************************************************************************/
        //need all childrens parents in order to find corresponding drawings
        private static List<AWS.File> GetParentsFiles(long masterId, VDF.Vault.Currency.Connections.Connection connection)
        {
            List<AWS.File> parents = new List<AWS.File>();
            try
            {
                AWS.FileAssocArray[] files2 = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { masterId }, AWS.FileAssociationTypeEnum.All, false, AWS.FileAssociationTypeEnum.None, false, false, false, false);
                foreach (AWS.FileAssocArray tempFiles in files2)
                {
                    foreach (AWS.FileAssoc tempFile in tempFiles.FileAssocs)
                    {
                        FileInfo file = new FileInfo(tempFile.CldFile.Name);
                        if (file.Extension.Equals(".idw", StringComparison.InvariantCultureIgnoreCase) || file.Extension.Equals(".dwg", StringComparison.InvariantCultureIgnoreCase))
                            parents.Add(tempFile.CldFile);

                        FileInfo file2 = new FileInfo(tempFile.ParFile.Name);
                        if (file2.Extension.Equals(".idw", StringComparison.InvariantCultureIgnoreCase) || file2.Extension.Equals(".dwg", StringComparison.InvariantCultureIgnoreCase))
                            parents.Add(tempFile.ParFile);
                    }

                }
            }
            catch { }

            return parents;
        }

        /*****************************************************************************************/
        public static AWS.File GetFile(VDF.Vault.Currency.Connections.Connection connection, string selectionType, ADSK.Explorer.Extensibility.ISelection selection)
        {
            if (selection.TypeId.EntityClassId.Equals("File", StringComparison.InvariantCultureIgnoreCase) && selectionType.Equals("FileMaster", StringComparison.InvariantCultureIgnoreCase))
                return connection.WebServiceManager.DocumentService.GetLatestFileByMasterId(selection.Id);
            else if (selection.TypeId.EntityClassId.Equals("File", StringComparison.InvariantCultureIgnoreCase) && selectionType.Equals("FileVersion", StringComparison.InvariantCultureIgnoreCase))
                return connection.WebServiceManager.DocumentService.GetLatestFilesByIds(new long[] { selection.Id })[0];
            else
                throw new ErrorMessageException("Incorrect selection type.");
        }

        /*****************************************************************************************/
        public static AWS.Folder GetActiveFolder(Autodesk.DataManagement.Client.Framework.Vault.Currency.Connections.Connection connection, IEnumerable<ADSK.Explorer.Extensibility.ISelection> navSelectionSet)
        {
            return GetActiveFolders(connection, navSelectionSet).First();
        }

        /*****************************************************************************************/
        public static AWS.Folder[] GetActiveFolders(Autodesk.DataManagement.Client.Framework.Vault.Currency.Connections.Connection connection, IEnumerable<ADSK.Explorer.Extensibility.ISelection> navSelectionSet)
        {
            List<AWS.Folder> currentSelectedFolders = new List<AWS.Folder>();
            foreach (ADSK.Explorer.Extensibility.ISelection selection in navSelectionSet)
            {
                if (selection.TypeId.Equals(Autodesk.Connectivity.Explorer.Extensibility.SelectionTypeId.Folder))
                    currentSelectedFolders.Add(connection.WebServiceManager.DocumentService.GetFolderById(selection.Id));
            }

            return currentSelectedFolders.ToArray();
        }

        /*****************************************************************************************/
        public static List<AWS.File> getAllActualChildren(long masterId, List<AWS.File> children, VDF.Vault.Currency.Connections.Connection connection)
        {
            try
            {
                AWS.FileAssocArray[] files2 = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { masterId }, AWS.FileAssociationTypeEnum.None, false, AWS.FileAssociationTypeEnum.All, true, false, false, false);
                foreach (AWS.FileAssocArray tempFiles in files2)
                {
                    foreach (AWS.FileAssoc tempFile in tempFiles.FileAssocs)
                    {
                        AWS.FileAssocArray fa = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { tempFile.CldFile.MasterId }, AWS.FileAssociationTypeEnum.None, false, AWS.FileAssociationTypeEnum.All, true, false, false, false)[0];

                        FileInfo file = new FileInfo(tempFile.CldFile.Name);
                        if (file.Extension.Equals(".idw", StringComparison.InvariantCultureIgnoreCase) || file.Extension.Equals(".iam", StringComparison.InvariantCultureIgnoreCase) || file.Extension.Equals(".ipt", StringComparison.InvariantCultureIgnoreCase))
                            children.Add(tempFile.CldFile);

                        if (fa.FileAssocs != null)
                        {
                            getAllActualChildren(tempFile.CldFile.MasterId, children, connection);
                        }
                    }

                }
            }
            catch { }

            return children;
        }

        public static List<AWS.File> GetAllChildren(long masterId, List<AWS.File> children, VDF.Vault.Currency.Connections.Connection connection)
        {
            try
            {
                AWS.FileAssocArray[] files2 = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { masterId }, AWS.FileAssociationTypeEnum.None, false, AWS.FileAssociationTypeEnum.All, true, false, false, false);
                foreach (AWS.FileAssocArray tempFiles in files2)
                {
                    foreach (AWS.FileAssoc tempFile in tempFiles.FileAssocs)
                    {
                        AWS.FileAssocArray fa = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { tempFile.CldFile.MasterId }, AWS.FileAssociationTypeEnum.None, false, AWS.FileAssociationTypeEnum.All, true, false, false, false)[0];

                        FileInfo file = new FileInfo(tempFile.CldFile.Name);
                        children.Add(tempFile.CldFile);
                    }
                }
            }
            catch { }
            return children;
        }

        /*****************************************************************************************/
        public static List<AWS.File> getAllActualParents(long masterId, List<AWS.File> parents, Autodesk.Connectivity.WebServicesTools.WebServiceManager webService)
        {
            try
            {
                AWS.FileAssocArray[] files2 = webService.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { masterId }, AWS.FileAssociationTypeEnum.All, false, AWS.FileAssociationTypeEnum.None, false, false, false, false);
                foreach (AWS.FileAssocArray tempFiles in files2)
                {
                    foreach (AWS.FileAssoc tempFile in tempFiles.FileAssocs)
                    {
                        AWS.FileAssocArray fa = webService.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { tempFile.ParFile.MasterId }, AWS.FileAssociationTypeEnum.All, false, AWS.FileAssociationTypeEnum.None, false, false, false, false)[0];

                        FileInfo file = new FileInfo(tempFile.ParFile.Name);
                        if (file.Extension.Equals(".idw", StringComparison.InvariantCultureIgnoreCase) || file.Extension.Equals(".iam", StringComparison.InvariantCultureIgnoreCase) || file.Extension.Equals(".ipt", StringComparison.InvariantCultureIgnoreCase))
                            parents.Add(tempFile.ParFile);

                        if (fa.FileAssocs != null)
                        {
                            getAllActualParents(tempFile.ParFile.MasterId, parents, webService);
                        }
                    }

                }
            }
            catch { }

            return parents;
        }


        /*****************************************************************************************/
        public static List<AWS.File> GetAllActualParentsFirstLevel(long masterId, List<AWS.File> parentFiles, Autodesk.Connectivity.WebServicesTools.WebServiceManager webService)
        {

            try
            {
                AWS.File[] files = webService.DocumentService.GetFilesByMasterId(masterId);
                //AWS.FileAssocArray[] files2 = connection.WebServiceManager.DocumentService.GetLatestFileAssociationsByMasterIds(new long[] { masterId }, AWS.FileAssociationTypeEnum.All, false, AWS.FileAssociationTypeEnum.None, false, false, false, false);
                AWS.FileAssocArray[] files2 = webService.DocumentService.GetFileAssociationsByIds(new long[] { files[0].Id }, AWS.FileAssociationTypeEnum.All, false, AWS.FileAssociationTypeEnum.None, false, false, false);

                foreach (AWS.FileAssocArray tmp in files2)
                {
                    foreach (AWS.FileAssoc tmpfile in tmp.FileAssocs)
                    {
                        FileInfo file = new FileInfo(tmpfile.ParFile.Name);
                        if (file.Extension.Equals(".iam", StringComparison.InvariantCultureIgnoreCase) || file.Extension.Equals(".ipt", StringComparison.InvariantCultureIgnoreCase) || file.Extension.Equals(".idw", StringComparison.InvariantCultureIgnoreCase))
                            parentFiles.Add(tmpfile.ParFile);

                    }
                }
            }
            catch (Exception e) { }

            return parentFiles;
        }
    }
}
