using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS = Autodesk.Connectivity.WebServices;
using VDF = Autodesk.DataManagement.Client.Framework;
using ADSK = Autodesk.Connectivity;
using Common.DotNet.Extensions;
using System.IO;

namespace MCADCommon.VaultCommon
{
    public class CheckInOutUtility
    {
        /***********************************************************************************************************/
        /***************** THIS DOES NOT CURRENTLY WORK BUT IT'S CLOSE - CONTINUE DEVELOPMENT HERE *****************/
        /***********************************************************************************************************/
        public static void publishDwf(VDF.Vault.Currency.Connections.Connection connection, AWS.File file, string localDirectoryPath, FileInfo actualDwfFile)
        {
            try
            {
                AWS.FilePathArray[] filePaths = connection.WebServiceManager.DocumentService.GetLatestAssociatedFilePathsByMasterIds(new long[] { file.MasterId },
                        AWS.FileAssociationTypeEnum.None, false, AWS.FileAssociationTypeEnum.Attachment, true, true, true, false);

                connection.WebServiceManager.DocumentService.SetTurnOffWarningForUserGeneratedDesignVisualization(true);
                

                VDF.Vault.Currency.Entities.Folder admcFolder = new VDF.Vault.Currency.Entities.Folder(connection, connection.WebServiceManager.DocumentService.GetFolderById(file.FolderId));


                Option<AWS.File> existingDWF = FileOperations.GetFile(connection, admcFolder.FullName + "/" + actualDwfFile.Name);


                if (existingDWF.IsSome)
                {
                    List<AWS.FileAssocLite> oldAssociations = new List<Autodesk.Connectivity.WebServices.FileAssocLite>();
                    AWS.FileAssocArray[] fass = connection.WebServiceManager.DocumentService.GetDesignVisualizationAttachmentsByFileMasterIds(new long[] { file.MasterId });

                    //if (fass.Length > 0)
                    //    return;


                    Option <VDF.Vault.Results.AcquireFilesResults> result = CheckOutFile(connection, existingDWF.Get(), localDirectoryPath, oldAssociations);
                    //string correctPath2 = result.Get().FileResults.First().LocalPath.FullPath;
                    //actualDwfFile.CopyTo(correctPath2, true);
                    //FileInfo newestDwfFile = new FileInfo(correctPath2);

                    //connection.WebServiceManager.DocumentService.DeleteFileFromFolderUnconditional(existingDWF.Get().MasterId, file.FolderId);
                    //connection.WebServiceManager.DocumentService.file
                    VDF.Vault.Currency.Entities.FileIteration fIter = CheckInFile(connection, existingDWF.Get(), "updated dwf", oldAssociations, admcFolder.FullName + "/" + actualDwfFile.Name, actualDwfFile);

                    /*AWS.FileAssocParam fileParam = new AWS.FileAssocParam();
                    fileParam.CldFileId = existingDWF.Get().Id;
                    fileParam.RefId = null;
                    fileParam.Typ = AWS.AssociationType.Attachment;
                    connection.WebServiceManager.DocumentService.AddDesignVisualizationFileAttachment(file.Id, fileParam);
                    */

                    //VDF.Vault.Currency.Entities.FileIteration fIter = connection.FileManager.AddFile(admcFolder, newDwfFile.Name, "added dwf", newDwfFile.LastWriteTime, null, null, AWS.FileClassification.DesignVisualization, true, stream);

                    //actualDwfFile.CopyTo(result.Get().FileResults.First().LocalPath.FullPath, true);
                    //CheckInFile(connection, existingDWF.Get(), "updated dwf", oldAssociations, admcFolder.FullName + "/" + existingDWF.Get().Name);
                    //VDF.Vault.Currency.Entities.FileIteration fIter = connection.FileManager.AddFile(admcFolder, newDwfFile.Name, "added dwf", newDwfFile.LastWriteTime, null, null, AWS.FileClassification.DesignVisualization, true, stream);

                    //AWS.BreakDesignVisualizationLinkCommandList blist = connection.WebServiceManager.DocumentService.GetBreakDesignVisualizationLinkCommandList();
                    //bool ya = connection.WebServiceManager.DocumentService.GetTurnOffWarningForUserGeneratedDesignVisualization();


                    //fass[0].FileAssocs[0].CldFile = connection.WebServiceManager.DocumentService.GetFileByVersion(fIter.EntityMasterId, fIter.VersionNumber);




                    AWS.FileAssocParam fileParam = new AWS.FileAssocParam();
                    fileParam.CldFileId = fIter.EntityIterationId;
                    fileParam.RefId = null;
                    fileParam.Typ = AWS.AssociationType.Attachment;
                    connection.WebServiceManager.DocumentService.AddDesignVisualizationFileAttachment(file.Id, fileParam);
                    //CheckInFile(connection, existingDWF.Get(), "updated dwf", oldAssociations, admcFolder.FullName + "/" + newDwfFile.Name);
                }
                else
                {

                    using (System.IO.FileStream stream = new FileStream(actualDwfFile.FullName, FileMode.Open))
                    {
                        VDF.Vault.Currency.Entities.FileIteration fIter = connection.FileManager.AddFile(admcFolder, actualDwfFile.Name, "added dwf", actualDwfFile.LastWriteTime, null, null, AWS.FileClassification.DesignVisualization, true, stream);

                        AWS.FileAssocParam fileParam = new AWS.FileAssocParam();
                        fileParam.CldFileId = fIter.EntityIterationId;
                        fileParam.RefId = null;
                        fileParam.Typ = AWS.AssociationType.Attachment;
                        connection.WebServiceManager.DocumentService.AddDesignVisualizationFileAttachment(file.Id, fileParam);
                    }
                }

            }
            catch
            {
                //errornstuff
            }
            //}
        }

        /*****************************************************************************************/
        public static Option<VDF.Vault.Results.AcquireFilesResults> CheckOutFile(VDF.Vault.Currency.Connections.Connection connection, AWS.File file, string localDirectoryPath, List<AWS.FileAssocLite> oldAssociations)
        {
            long userId = connection.UserID;
            if (((file.CheckedOut) && (userId != file.CkOutUserId)) || (file.Locked))
                return Option.None;

            VDF.Vault.Currency.Entities.FileIteration fileIteration = new VDF.Vault.Currency.Entities.FileIteration(connection, file);

            Option<List<AWS.FileAssocLite>> oldAssocs = FileOperations.GetFileChildrenAssocsLite(connection, fileIteration);
            if (oldAssocs.IsNone)
                return Option.None;

            oldAssociations.AddRange(oldAssocs.Get());

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
                return Option.None;

            VDF.Vault.Settings.AcquireFilesSettings acquireFilesSettings = new VDF.Vault.Settings.AcquireFilesSettings(connection, true);
            acquireFilesSettings.OptionsResolution.OverwriteOption = VDF.Vault.Settings.AcquireFilesSettings.AcquireFileResolutionOptions.OverwriteOptions.ForceOverwriteAll;
            acquireFilesSettings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeAttachments = false;
            acquireFilesSettings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeChildren = false;  // true? 
            acquireFilesSettings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeRelatedDocumentation = true;
            acquireFilesSettings.OptionsRelationshipGathering.FileRelationshipSettings.RecurseChildren = false; // true?
            acquireFilesSettings.OptionsRelationshipGathering.FileRelationshipSettings.VersionGatheringOption = VDF.Vault.Currency.VersionGatheringOption.Latest; ;
            acquireFilesSettings.LocalPath = new VDF.Currency.FolderPathAbsolute(localDirectoryPath);
            acquireFilesSettings.AddFileToAcquire(fileIteration, file.CheckedOut ? VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download : VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Checkout);

            try
            {
                VDF.Vault.Results.AcquireFilesResults acquireFilesResults = connection.FileManager.AcquireFiles(acquireFilesSettings);
                if (acquireFilesResults.FileResults.First().Status != VDF.Vault.Results.FileAcquisitionResult.AcquisitionStatus.Success)
                    return Option.None;

                return acquireFilesResults.AsOption();
            }
            catch
            {
                connection.FileManager.UndoCheckoutFile(fileIteration);
                throw new ErrorMessageException("Child is checked out.");
            }
        }















        /*****************************************************************************************/
        /*       public static bool setPropertyOnCheckedOutFile(string propertyName, object value)
               {
                   Option<AWS.PropDef> definition = PropertyOperations.GetPropertyDefinition(Connection, propertyName);
                   if (definition.IsNone)
                       return false;

                   try
                   {
                       AWS.PropInstParam property = new AWS.PropInstParam { PropDefId = definition.Get().Id, Val = value };

                       Connection.WebServiceManager.DocumentService.UpdateFileProperties(new long[] { CheckedOutFile.MasterId }, new AWS.PropInstParamArray[] { new AWS.PropInstParamArray { Items = new AWS.PropInstParam[] { property } } });
                       return true;
                   }
                   catch
                   {
                       return false;
                   }
               }*/

        /*****************************************************************************************/
        public static void updateDesignVisualizationOnCheckedOutFile()
        {

        }

        /*****************************************************************************************/
        public static VDF.Vault.Currency.Entities.FileIteration CheckInFile(VDF.Vault.Currency.Connections.Connection connection, AWS.File file, string comment, List<AWS.FileAssocLite> oldAssociations, string fullVaultPath, FileInfo localFile)
        {
            VDF.Vault.Currency.Entities.FileIteration fileIteration = new VDF.Vault.Currency.Entities.FileIteration(connection, file);
            return CheckInFile(connection, fileIteration, comment, oldAssociations, fullVaultPath, localFile);
        }
        public static VDF.Vault.Currency.Entities.FileIteration CheckInFile(VDF.Vault.Currency.Connections.Connection connection, VDF.Vault.Currency.Entities.FileIteration fileIteration, string comment, List<AWS.FileAssocLite> oldAssociations, string fullVaultPath, FileInfo localFile)
        {
            List<AWS.FileAssocParam> newAssociations = FileOperations.UpdateFileAssociations(connection, fileIteration, oldAssociations);
            try
            {
                return connection.FileManager.CheckinFile(fileIteration,
                                                           comment: comment,
                                                           keepCheckedOut: false,
                                                           associations: newAssociations.ToArray(),
                                                           bom: null,
                                                           copyBom: false,
                                                           newFileName: null,
                                                           classification: fileIteration.FileClassification,
                                                           hidden: false,
                                                           filePath: new VDF.Currency.FilePathAbsolute(localFile));
            }
            catch
            {
                return null;
            }
        }

        /*****************************************************************************************/
        public static bool SetProperty(VDF.Vault.Currency.Connections.Connection connection, AWS.File file, string propertyName, object value, string tempPath)
        {
            long userId = connection.UserID;
            if (((file.CheckedOut) && (userId != file.CkOutUserId)) || (file.Locked))
                return false;
            Option<AWS.PropDef> definition = PropertyOperations.GetPropertyDefinition(connection, propertyName);
            if (definition.IsNone)
                return false;

            VDF.Vault.Currency.Entities.FileIteration fileIteration = new VDF.Vault.Currency.Entities.FileIteration(connection, file);

            Option<List<AWS.FileAssocLite>> oldAssociations = FileOperations.GetFileChildrenAssocsLite(connection, fileIteration);
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
            acquireFilesSettings.AddFileToAcquire(fileIteration, /*VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Checkout | VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download*/file.CheckedOut ? VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download : VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Checkout);

            try
            {
                VDF.Vault.Results.AcquireFilesResults acquireFilesResults = connection.FileManager.AcquireFiles(acquireFilesSettings);
                if (acquireFilesResults.FileResults.First().Status != VDF.Vault.Results.FileAcquisitionResult.AcquisitionStatus.Success)
                    return false;


                AWS.PropInstParam property = new AWS.PropInstParam { PropDefId = definition.Get().Id, Val = value };

                connection.WebServiceManager.DocumentService.UpdateFileProperties(new long[] { file.MasterId }, new AWS.PropInstParamArray[] { new AWS.PropInstParamArray { Items = new AWS.PropInstParam[] { property } } });
                List<AWS.FileAssocParam> newAssociations = FileOperations.UpdateFileAssociations(connection, fileIteration, oldAssociations.Get());

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

        /***************************************************************************************/
        public static VDF.Vault.Currency.Entities.FileIteration AddNewFile(VDF.Vault.Currency.Connections.Connection connection, string vaultFolderPath, string localFile, AWS.FileAssocParam[] associations)
        {
            FileInfo file = new FileInfo(localFile);
            VDF.Vault.Currency.Entities.Folder vaultFolder = new VDF.Vault.Currency.Entities.Folder(connection, connection.WebServiceManager.DocumentService.GetFolderByPath(vaultFolderPath));
            return connection.FileManager.AddFile(
                parent: vaultFolder,
                comment: "Adding new file with name: " + file.Name + ".",
                associations: associations,
                bom: null,
                classification: AWS.FileClassification.None,
                hidden: false,
                localPathWithFileName: new VDF.Currency.FilePathAbsolute(file)
            );
        }


    }
}
