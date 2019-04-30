using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Services.Protocols;
using System.Xml;

using Common.DotNet.Extensions;

namespace VaultEagle
{
    [Serializable]
    public class VaultServerException : Exception
    {
        public string ServerStackTrace;
        const string exceptionClassName = "VaultServerException";
        private const string vaultExceptionSignatureString = "http://streamline.autodesk.com/faultschema";

        public VaultServerException(SoapException soapEx)
            : base(FormatVaultExceptionMessage(soapEx), soapEx)
        {
            ServerStackTrace = GetServerStackTrace(soapEx) ?? "<no server stacktrace>";
        }
        public override string StackTrace
        {
            get
            {
                return base.StackTrace + Environment.NewLine + (ServerStackTrace??"").Trim();
            }
        }

        public static string FormatException(Exception exception)
        {
            return FormatVaultException(exception as SoapException)
                   ?? exception.ToString();
        }

        public static string FormatVaultException(SoapException ex)
        {
            if (ex == null)
                return null;
            return "Vault Server Error:\r\n\r\n" + GetVaultServerErrorInfo(ex) + "\r\n"
                   + ex.ToString() + "\r\n"
                   + (GetServerStackTrace(ex) ?? "<no server stacktrace>") + "\r\n";
        }

        public static string GetNameAndDescription(SoapException ex)
        {
            string nameAndDescription = null;
            try
            {
                var errorCode = ex.Detail["sl:sldetail"]["sl:errorcode"].InnerText;
                nameAndDescription = VaultErrorCodes.errorCodes[errorCode];
            }
            catch (Exception) { }
            return nameAndDescription;
        }

        public static SortedDictionary<int, string> GetParams(SoapException ex)
        {
            var paramsDict = new SortedDictionary<int, string>();
            try
            {
                var nsmgr = XmlIncantations(ex.Detail, "sl");
                foreach (var xmlNode in ex.Detail.SelectNodes("sl:sldetail/sl:param", nsmgr).AsEnumerable())
                {
                    int pid = -paramsDict.Keys.FirstOrDefault() + 1;
                    try
                    {
                        pid = int.Parse(xmlNode.Attributes["sl:pid"].InnerText);
                    }
                    catch (Exception) { }
                    paramsDict[-pid] = xmlNode.InnerText;
                }
            }
            catch (Exception) { }

            return paramsDict.ToDictionary(kv => -kv.Key, kv => kv.Value).ToSortedDictionary();
        }

        public static string GetRestrictions(SoapException ex)
        {
            var resultLines = new List<string>();
            try
            {
                var nsmgr = XmlIncantations(ex.Detail, "sl");
                XmlNode restrictions = ex.Detail.SelectSingleNode("sl:sldetail/sl:restrictions", nsmgr);
                if (restrictions == null)
                    return null;

                foreach (var restrictionXmlNode in restrictions.SelectNodes("sl:restriction", nsmgr).AsEnumerable())
                {
                    try
                    {
                        string restrictionCode = restrictionXmlNode.Attributes["sl:code"].InnerText;
                        string[] paramsInfo = VaultErrorCodes.restrictionCodesAndParams.GetValueOrDefault(restrictionCode, new[] { "" });
                        string restrictionDetails = paramsInfo[0];

                        resultLines.Add(string.Format("   Restriction (code={0}) {1}", restrictionCode, restrictionDetails));

                        var paramNames = paramsInfo.Skip(1).Select((name, i) => new { i, name }).ToDictionary(x => x.i.ToString(), x => x.name);

                        var givenParams = new HashSet<string>();
                        foreach (var paramXmlNode in restrictionXmlNode.SelectNodes("sl:param", nsmgr).AsEnumerable())
                        {
                            try
                            {
                                string paramNumberString = paramXmlNode.Attributes["sl:index"].InnerText;
                                string paramValue = paramXmlNode.InnerText;
                                string paramName = paramNames.GetValueOrDefault(paramNumberString, "");
                                givenParams.Add(paramName);
                                resultLines.Add(string.Format("      param[{0}] {1}: {2}", paramNumberString, paramName, paramValue));
                            }
                            catch (Exception) { }
                        }
                        foreach (var missingParam in paramNames.Where(kv => !givenParams.Contains(kv.Value)))
                            resultLines.Add(string.Format("      (param[{0}] {1} not found)", missingParam.Key, missingParam.Value));
                    }
                    catch (Exception) { }
                }
                return string.Join("\r\n", resultLines.ToArray()); ;
            }
            catch (Exception) { }

            return null;
        }

        public static string GetServerStackTrace(SoapException ex)
        {
            string stackTrace = null;
            try
            {
                stackTrace = ex.Detail["sl:sldetail"]["sl:stack"].InnerText.Replace("\r\n", "\n").Replace("\n", "\r\n");
            }
            catch (Exception) { }
            return stackTrace;
        }

        public static string GetVaultServerErrorInfo(SoapException ex)
        {
            string restrictionsString = GetRestrictions(ex);
            var paramsString = GetParamsString(ex);
            return string.Join(Environment.NewLine,
                                   new[]{ GetNameAndDescription(ex),
                                       paramsString,
                                       restrictionsString}.Where(s => s != null).ToArray());
        }

        public static Exception WrapException(Exception ex)
        {
            var soapEx = ex as SoapException;
            if (soapEx != null)
            {
                if (soapEx.Detail != null && soapEx.Detail.InnerXml.Contains(vaultExceptionSignatureString))
                    return WrapVaultException(soapEx);
            }
            return ex;
        }

        public static Exception WrapVaultException(SoapException soapEx)
        {
            return new VaultServerException(soapEx);
        }

        private static string FormatVaultExceptionMessage(SoapException ex)
        {
            return "\r\n\r\n" + GetVaultServerErrorInfo(ex).Trim() + Environment.NewLine + Environment.NewLine;
        }
        private static string GetParamsString(SoapException ex)
        {
            var @params = GetParams(ex);
            if (@params.Count == 0)
                return null;

            var paramsString =
                (string.Join("",
                             @params.Select(
                                 pidValue => string.Format("   param[{0}]: {1}\r\n", pidValue.Key, pidValue.Value)).ToArray()));

            return paramsString;
        }

        private static XmlNamespaceManager XmlIncantations(XmlNode node, string prefix)
        {
            var xmlDocument = node as System.Xml.XmlDocument;
            if (xmlDocument == null)
                xmlDocument = node.OwnerDocument;
            var nsmgr = new XmlNamespaceManager(xmlDocument.NameTable);
            nsmgr.AddNamespace(prefix, node.FirstChild.NamespaceURI);
            return nsmgr;
        }
        #region Test exceptions

        //VaultErrorCodes.ThrowDatabaseExistsVaultException();
        //VaultErrorCodes.ThrowRestrictionVaultException();

        public static void ThrowDatabaseExistsVaultException()
        {
            XmlDocument doc = new XmlDocument();
            string xmlData =
                "<detail><sl:sldetail xmlns:sl=\"http://streamline.autodesk.com/faultschema\">          <sl:errorcode>102</sl:errorcode>          <sl:mesg-id>632374075548091123</sl:mesg-id>          <sl:param sl:pid=\"1\">Vault</sl:param>          <sl:stack>Server stack trace:    at Connectivity.Core.Services.KnowledgeVaultService.AddKnowledgeVault(String vaultName, String filestore, Category categoryId)   at System.Runtime.Remoting.Messaging.Message.Dispatch(Object target, Boolean fExecuteInContext)   at System.Runtime.Remoting.Messaging.StackBuilderSink.SyncProcessMessage(IMessage msg, Int32 methodPtr, Boolean fExecuteInContext)Exception rethrown at [0]:    at System.Runtime.Remoting.Proxies.RealProxy.HandleReturnMessage(IMessage reqMsg, IMessage retMsg)   at System.Runtime.Remoting.Proxies.RealProxy.PrivateInvoke(MessageData msgData, Int32 type)   at Connectivity.Core.Services.KnowledgeVaultService.AddKnowledgeVault(String vaultName, String filestore, Category categoryId)   at Connectivity.Web.Services.KnowledgeVaultService.AddKnowledgeVault(String vaultName, Category category)</sl:stack>        </sl:sldetail></detail>";
            doc.LoadXml(xmlData);
            var soapException = new SoapException("a", XmlQualifiedName.Empty, "abc", doc.FirstChild);
            throw soapException;
        }

        public static void ThrowRestrictionVaultException()
        {
            XmlDocument doc = new XmlDocument();
            string xmlData =
                "<detail><sl:sldetail xmlns:sl=\"http://streamline.autodesk.com/faultschema\">  <sl:errorcode>1387</sl:errorcode>  <sl:mesg-id>632521940579843750</sl:mesg-id><sl:stack>\nServer stack trace:  at Connectivity.Product.Services.ItemService.CheckRestrictionsByItemIterationIds(ProductRestrictionCodes[] restrictionCodes, Int64[] itemIterationIds) in H:\\Server\\Product\\Services\\ItemService.cs:line 114 at Connectivity.Product.Services.ItemService.CheckRestrictionsByItemRevisionIds(ProductRestrictionCodes[] restrictionCodes, Int64[] itemRevisionIds) in H:\\Server\\Product\\Services\\ItemService.cs:line 97 at Connectivity.Product.Services.ItemService.EditItemRevisionInternal(Int64 itemRevId, Boolean bypassWIPRestriction) in H:\\Server\\Product\\Services\\ItemService.cs:line 1149 at Connectivity.Product.Services.ItemService.EditItemRevisionInternal(Int64 itemRevId) in H:\\Server\\Product\\Services\\ItemService.cs:line 1128 at Connectivity.Product.Services.ItemService.EditItemRevision(Int64 itemRevId) in H:\\Server\\Product\\Services\\ItemService.cs:line 1124 at System.Runtime.Remoting.Messaging.Message.Dispatch(Object target, Boolean fExecuteInContext) at System.Runtime.Remoting.Messaging.StackBuilderSink.SyncProcessMessage(IMessage msg, Int32 methodPtr, Boolean fExecuteInContext)Exception rethrown at [0]:  at System.Runtime.Remoting.Proxies.RealProxy.HandleReturnMessage(IMessage reqMsg, IMessage retMsg) at System.Runtime.Remoting.Proxies.RealProxy.PrivateInvoke(MessageData&amp; msgData, Int32 type) at Connectivity.Product.Services.ItemService.EditItemRevision(Int64 itemRevId) in H:\\Server\\Product\\Services\\ItemService.cs:line 1124 at Connectivity.Web.Services.ItemService.EditItemRevision(Int64 itemRevisionId) in H:\\Server\\Web\\Services\\ItemService.asmx.cs:line 780</sl:stack><sl:restrictions>  <sl:restriction sl:code=\"2001\">    <sl:param sl:index=\"0\">000001</sl:param>    <sl:param sl:index=\"1\">Test Item</sl:param>    <sl:param sl:index=\"2\">2</sl:param>    <sl:param sl:index=\"3\">Released</sl:param>  </sl:restriction></sl:restrictions></sl:sldetail></detail>";
            doc.LoadXml(xmlData);
            var soapException = new SoapException("a", XmlQualifiedName.Empty, "abc", doc.FirstChild);
            throw soapException;
        }
        #endregion
    }

    class VaultErrorCodes
    {
        #region public static Dictionary<string, string> errorCodes = ...

        // Vault 2012
        public static Dictionary<string, string> errorCodes = new Dictionary<string, string>
            {
{"0","UnspecifiedSystemException[0]: Is this used when the error code is invalid, or does it have another purpose as well?  (ExceptionEncoder: 29)"},
{"100","CreateKnowledgeVaultDatabase[100]: Error creating the knowledge vault."},
{"102","DatabaseExists[102]: Database already exists."},
{"106","TransactionInvalidPrincipal[106]: An example would be making a call without being logged into a vault for methods that require a vault"},
{"108","TransactionManagementError[108]: Cannot create database connection and/or transaction"},
{"109","DatabaseError[109]: Generic error for unexpected database issues."},
{"111","BadResourceRelativePath[111]: General error for failures to store or access a resource in the file store"},
{"114","CreateSystemMasterDatabase[114]: could not create KnowledgeVaultMaster Database"},
{"130","UnknownVersion[130]: Unable to determine the version of a KnowledgeVault or Master"},
{"131","InvalidAdminDbLogin[131]: The database admin login is invalid"},
{"132","DirectoryNotEmpty[132]: The directory is not empty"},
{"133","KnowledgeVaultDoesNotExist[133]: The Knowledge Vault referenced doesn't exist"},
{"134","KnowledgeVaultsAttached[134]: There are Knowledge Vaults still attached."},
{"137","IllegalInputParam[137]: One of the inputs to the service call is incorrect."},
{"138","IllegalDatabaseName[138]: Database name is not allowed. Most likely due to illegal characters."},
{"139","IllegalPath[139]: Specified folder is illegal."},
{"140","DuplicatePath[140]: Specified folder is already in use."},
{"142","CorruptedDatabase[142]: The database {0} is corrupted due to previous migration failure."},
{"143","UserAlreadyExists[143]: Duplicate User Name"},
{"144","DbFileAlreadyExists[144]: Database error because an MDF or LDF file with that name already exists."},
{"146","MigrationPathNotFound[146]: Cannot determine the migration steps."},
{"147","PathTooLong[147]: The specified path is too long."},
{"148","UnsupportedProduct[148]: The vault has a product installed that is not installed on the server"},
{"150","KnowledgeVaultMasterDoesNotExist[150]: The KnowledgeVaultMaster referenced doesn't exist"},
{"152","IllegalRestoreDBLocation[152]: Cannot restore db files to a remote location"},
{"153","InvalidBackupDirectory[153]: Selected directory does not contain a valid backup structure."},
{"154","InvalidUserId[154]: The user ID is not valid"},
{"155","IllegalNullParam[155]: A null value was passed in where a null value is not allowed."},
{"157","AdministratorCannotBeRemoved[157]"},
{"158","CircularReference[158]"},
{"160","BadId[160]"},
{"164","MigrationXmlError[164]: migrations.xml identifies the .sql scripts and C# code that need to be executed for the different migration paths (e.g., R2 to R3, etc.)"},
{"165","KnowledgeLibraryDoesNotExist[165]: Occurs during restore"},
{"167","AttachWrongDatabaseType[167]"},
{"171","DuplicateLibraryGuid[171]: There is already a KnowledgeLibrary with the same GUID"},
{"173","ReadOnlyFile[173]: Trying to perform a write operation on a read-only file"},
{"174","InvalidDatabaseCollation[174]: Trying to create a KVM with an invalid database collation (eg case sensitive)"},
{"175","InsufficientFilePermissions[175]: The user {1} does not have permission to the path {0}"},
{"176","GroupDoesNotExist[176]"},
{"179","IOError[179]: IOException has been thrown. IOException.Message={0}"},
{"180","FileDoesNotExist[180]"},
{"185","IncrementTurnedOff[185]"},
{"186","IncRestoreInEligibleForAdminDataDirty[186]"},
{"187","IncRestoreInEligibleForUserDataDirty[187]"},
{"188","IncRestoreInEligibleForBadStateId[188]"},
{"189","IncBackupInEligibleForAdminDataDirty[189]"},
{"190","IncBackupInEligibleForUserDataNotdirty[190]"},
{"191","IncBackupInEligibleForFullBackupUndone[191]"},
{"192","MissingBackupPackages[192]"},
{"193","ContentfileWrongFormatted[193]"},
{"194","IncRestoreInEligibleForWrongIncrement[194]"},
{"196","LibraryPartitionDoesNotExist[196]"},
{"197","LibraryPartitionUpdateDoesNotExist[197]"},
{"198","DatabaseServerNotCompatible[198]"},
{"199","SystemTypeNotEditable[199]"},
{"200","NonmemberSite[200]"},
{"201","DatabaseLocked[201]: {0} is the name of the locking operation. {1} [currently unused] is the name of the database"},
{"203","ResourcePartInvalidByteRange[203]"},
{"204","InvalidSiteName[204]"},
{"205","CouldNotReplicate[205]"},
{"207","NoFilestore[207]"},
{"208","InvalidVaultSite[208]"},
{"211","VaultDisabled[211]"},
{"212","FileStoreMismatch[212]: There is a sentinal file with a vaultguid in the root of the filestore. This value needs to match the guid identity of the vault"},
{"213","OrphanedMetaData[213]"},
{"214","RoleDoesNotExist[214]"},
{"215","InvalidSystemDbLogin[215]: The system user [vaultsys] either doesn't exist or doesn't match the web.config password"},
{"216","GroupAlreadyExists[216]: Duplicate Group Name"},
{"222","ChecksumValidationFailure[222]"},
{"223","RestoringUnsupportedProducts[223]: Restore failed due to product difference between the server and the backup."},
{"224","InvalidServiceExtensionConfig[224]: At least one type or method referenced in the ServiceExtenstions.xml file could not be resolved."},
{"226","LuceneSearchError[226]: Wrap any errors thrown by Lucene when searching."},
{"227","LuceneIndexingError[227]: Wrap any errors thrown by Lucene when indexing."},
{"228","InvalidServiceExtensionMethod[228]: Wrap any errors thrown when invoking a service extension method."},
{"230","PropertyParsingFailed[230]: Search failed to parse a property value."},
{"231","DatabaseDeadlock[231]: Generic error for unexpected database issues."},
{"232","ConfigurationError[232]: Error occured calling a config section handler"},
{"233","DatabaseLogFull[233]"},
{"234","ArraysOfDifferentSizes[234]: Input arrays were of different lengths."},
{"236","UnsupportedParameterType[236]"},
{"237","DuplicateQueuedEvent[237]"},
{"238","InvalidEventClass[238]"},
{"239","UnreserveEventFailed[239]"},
{"240","JobQueueDisabled[240]"},
{"241","UnsupportedOperation[241]"},
{"242","DuplicatePropertiesCannotBeToSamePropertyDef[242]"},
{"243","PropertiesCannotHaveTheSamePriority[243]"},
{"244","FailedToLoadPropertyProviderForProp[244]"},
{"245","InvalidPropertyForPropertyProvider[245]"},
{"246","IncompatiblePropertyDataTypes[246]"},
{"247","CannotCreateNewForProperty[247]"},
{"248","PropertyDoesNotSupportMappingDir[248]"},
{"249","InvalidEntityClassId[249]"},
{"250","CannotFindPropertyDefBySystemName[250]"},
{"251","CannotCreatePropertyDef[251]"},
{"252","CannotCreatePropertyDef_DisplayNameExists[252]"},
{"253","PropertyDefIdDoesNotExist[253]"},
{"255","PropertyDefIsNotMappedToEntityClass[255]"},
{"256","SystemPropertyDefsCannotChangeThierEnitityClassMappings[256]"},
{"257","CannotDeleteMappingsWhichAreInUseByEnts[257]"},
{"258","PropertyDefDisplayNameExist[258]"},
{"259","ContentSourcePropertyDefIdDoesNotExist[259]"},
{"260","SystemPropertyDefsCannotBeDeleted[260]"},
{"261","PropertyDefsCannotBeDeletedWithEntityRefs[261]"},
{"262","CtntSrcPropProviderNotFound[262]"},
{"263","BadNumberingSchemeId[263]"},
{"264","NumberingSchemeIsDefault[264]"},
{"265","DuplicateNumberSchemeName[265]"},
{"266","NumberingSchemeInUse[266]"},
{"267","LastNumberingSchemeCannotBeRemoved[267]"},
{"268","GetNumberingSchemesBySchemeStatusFailed[268]"},
{"269","ActivateNumberingSchemeFailed[269]"},
{"270","DeactivateNumberingSchemeFailed[270]"},
{"271","SetDefaultNumberingSchemeFailed[271]"},
{"272","AddNumberingSchemeFailed[272]"},
{"273","UpdateNumberingSchemeFailed[273]"},
{"274","DeleteNumberingSchemeFailed[274]"},
{"275","DeleteNumberingSchemeUnconditionalFailed[275]"},
{"276","MethodNotSupportedWithBaseVaultServer[276]"},
{"277","PropertyDefRequiresEntityClassMapping[277]"},
{"278","PropertyDefDefaultValuesNotSupported[278]"},
{"279","InvalidEntityClassName[279]"},
{"280","OnlyReadMappingsSupported[280]"},
{"281","CreateNewPropertyMappingsNotSupported[281]"},
{"282","RestrictionsOccurred[282]"},
{"283","BadEntityId[283]"},
{"284","EntityClassDoesNotSupportMapping[284]"},
{"285","EntityClassDoesNotSupportMappingToCSType[285]"},
{"286","MigrationInProgress[286]"},
{"287","EntityClassDoesNotSupportLinks[287]"},
{"288","AddLinkFailed[288]"},
{"289","CreateNewAndDefaultMappingTypeNotSupported[289]"},
{"290","UserDefinedPropertyListValuesNotSupported[290]"},
{"291","UserDefinedPropertyConstraintsNotSupported[291]"},
{"292","UserDefinedPropertyWithoutCSMappingsNotSupported[292]"},
{"293","UserDefinedPropertyInitialValuesNotSupported[293]"},
{"300","BadAuthenticationToken[300]: This can happen when the web services are restarted."},
{"301","InvalidUserPassword[301]: Username and/or Password is invalid, so user cannot be authenticated."},
{"302","UserNotVaultMember[302]: User is not a member of the vault"},
{"303","PermissionDenied[303]: Invalid permissions for transaction"},
{"304","UserIsDisabled[304]: User is disabled"},
{"306","IncompatibleKnowledgeVault[306]: The compatibility of the Vault doesn't match the server version"},
{"307","IncompatibleKnowledgeMaster[307]: The compatibility of the Knowledge Master doesn't match the server version"},
{"308","RestrictionsOccurred[308]"},
{"309","FeatureNotAvailable[309]"},
{"310","IncompatibleKnowledgeLibrary[310]"},
{"311","InvalidAuthType[311]: Attempted to login through WinAuth login user, but user is of Auth Type Vault."},
{"312","WinAuthUserNotFound[312]: could be auto create is disabled or could not find a valid AD group"},
{"313","WinAuthAnonymousIdentity[313]: Identity was unauthenticated or anonymous"},
{"318","WinAuthFailed[318]: Unknown WinAuth error occured."},
{"319","LicensingError[319]"},
{"320","PermissionTamperingDetected[320]"},
{"321","WorkgroupDoesNotHaveAdminOwnership[321]"},
{"322","WorkgroupDoesNotHaveObjectOwnership[322]"},
{"323","WorkgroupIsSubscriber[323]"},
{"400","DuplicateRoutingName[400]: The routing name already exists."},
{"401","IncompleteRouting[401]: Must be at least one user assigned to each role."},
{"402","DeactivateRoutingFailed[402]: Can't deactivate the last active routing, or the default routing."},
{"403","DeleteRoutingFailed[403]: Can't delete a routing in use, the last active routing, or the default routing."},
{"404","SetDefaultRoutingFailed[404]: Cannot set a deactive routing to the default."},
{"405","ActionDenied[405]: The user does not have the appropriate routing role to perform the activity."},
{"406","ActionAlreadyPerformed[406]: The user has already performed the activity since the change order last entered the current state."},
{"407","BadRoutingName[407]: Routing name contains illegal characters"},
{"408","BadRoutingNameLength[408]"},
{"501","FailureToLoadEmailHandler[501]"},
{"502","ErrorSendingEmail[502]"},
{"503","ErrorInitializeEmailhandler[503]"},
{"504","EmailIsConfiguredAsDisabled[504]"},
{"505","InvalidAttachmentStream[505]"},
{"506","InvalidAttachmentName[506]"},
{"600","JobConfigurationError[600]"},
{"601","FailureToLoadJobHandler[601]"},
{"602","DuplicateJobHandlerIdFound[602]"},
{"603","JobIdNotFound[603]"},
{"1000","BadFolderId[1000]"},
{"1001","GetLatestVersionFailed[1001]"},
{"1002","GetVersionsFailed[1002]"},
{"1003","BadFileId[1003]"},
{"1004","CheckoutFailed[1004]: Checkout latest file version failed."},
{"1005","CheckinFailed[1005]: Error checking in file version into database."},
{"1006","UndoCheckoutFailed[1006]: Error undoing check out of file version."},
{"1007","BadVersionId[1007]: Bad version id when getting file version dependents or dependencies by version id."},
{"1008","AddFileExists[1008]: Cannot add file because file exists."},
{"1009","AddFileFailed[1009]: Cannot add file (unspecified failure)"},
{"1010","NOT USED ANYMORE.[1010]: NOT USED ANYMORE."},
{"1011","AddFolderExists[1011]: Cannot add folder because folder exists."},
{"1012","AddFailedCreateFolder[1012]: Cannot add folder (unable to create/make new folder)."},
{"1013","GetFileFailed[1013]: Cannot get file (file id is invalid)."},
{"1014","MakeVersionFailed[1014]: Cannot create/make version in database."},
{"1015","DeleteFileWithDependencies[1015]: Only have file id, not name"},
{"1016","UndoCheckoutWrongUser[1016]: Cannot undo checkout because user is not the same as user who checked out file."},
{"1017","UndoCheckoutWrongFolder[1017]: Cannot undo checkout because passed in folder id is not the same folder that the file was checked out from."},
{"1018","CheckinNotCheckedOut[1018]: Cannot check in file because the file is not currently checked out"},
{"1019","CheckinWrongUser[1019]: Cannot check in file because the file is not currently checked out by the same user."},
{"1020","CheckinWrongFolder[1020]: Cannot check in file because passed in folder id is not the same folder that the file was checked out from."},
{"1021","CheckoutAlreadyCheckedOut[1021]: Cannot check out the file because it is already checked out."},
{"1022","SelfDependency[1022]: Only have file version id, not name"},
{"1023","MakeFolderFailed[1023]: Cannot create folder in database."},
{"1024","GetFolderFailed[1024]: Occurs in these cases:\r\n: Cannot get folder based on path.\r\n: Cannot get folder based on folder id."},
{"1025","GetRootFailed[1025]: Cannot get root folder from the database."},
{"1026","LibraryProjectExistsForFileId[1026]: File belongs to a library folder."},
{"1027","LibraryProjectExistsForId[1027]: Folder is a library folder."},
{"1028","MoveFileFailed[1028]: Cannot move file."},
{"1029","MoveFileExists[1029]: Only have file id, not name"},
{"1030","ShareFileExists[1030]: Only have file id, not name"},
{"1031","DuplicatePropertyDefName[1031]"},
{"1034","RenameFailed[1034]: Cannot rename the file because there was some other unexpected error."},
{"1035","MakeDefinitionFailed[1035]: Could not create property definition in database."},
{"1036","GetAllPropertyDefinitionsFailed[1036]: Not used yet?"},
{"1037","GetAllPropertyDefinitinsExtendedFailed[1037]: Not used yet?"},
{"1038","**GetFileVersionsByPropertySearchConditionsFailed[1038]"},
{"1041","GetPropertiesFailed[1041]"},
{"1042","AddFolderChildRootInvalid[1042]: Create folder rule-check failed:  parent must exist, for all but root"},
{"1043","AddFolderLibraryRelationshipInvalid[1043]: Create folder rule-check failed:  libs can only have non lib parent if that parent is root.  libs cannot have non lib children."},
{"1044","ConcurrentShareFailed[1044]: Request to share a file to a folder fails because of a concurrent request to share the file to the same folder."},
{"1045","ConcurrentMoveFailed[1045]: Request to move a file to a folder fails because of a concurrent request to move the file to the same folder or because of a concurrent request to move the file to another folder"},
{"1046","FolderCharacterLengthInvalid[1046]: Request to create a folder fails because the folder name is longer that 80 characters."},
{"1047","DependentExistsAttachmentFailed[1047]: Unused"},
{"1048","RenameFailedDependentParentItemsLinked[1048]: Unused"},
{"1049","RenameFailedDependentParentItemsAttached[1049]: Unused"},
{"1050","DeleteFileFailedRestrictions[1050]: Request to conditionally delete a file fails because there are delete restrictions (file has dependent parents files, file is checked out, or file is linked or attached to an item)"},
{"1051","DeleteFileFailedUnconditionalRestrictions[1051]: Request to unconditionally delete a file fails because the are delete restrictions that cannot be overridden (file is linked or attached to an item)"},
{"1052","DeleteFileFailed[1052]: Request to delete a file failed for an unspecified reason."},
{"1053","DeleteFolderFailedRestrictions[1053]: Request to conditionally delete a folder fails because there are delete restrictions on one or more child files (file has dependent parents files, file is checked out, or file is linked or attached to an item)"},
{"1054","DeleteFolderFailedUnconditionalRestrictions[1054]: Request to unconditionally delete a folder fails because the are delete restrictions that cannot be overridden on one or more child files (file is linked or attached to an item)"},
{"1055","DeleteFolderFailed[1055]: Request to delete a folder fails due to an unspecified reason"},
{"1056","PurgeBadParam[1056]: The keep count must be >= 1, and the minimum age must be >= 0"},
{"1057","PurgeFailed[1057]: Occurs when something goes wrong while purging file iterations from the database, or while deleting files from the file store"},
{"1058","UniqueFileNameRequiredViolated[1058]: If the Unique File Name Required Vault option is ON, a request to Add or Checkin a file with the same name as a file already existing in the Vault will fail with this error."},
{"1059","UpdateFolderFailed[1059]: Occurs when an attempt to update a Folder fails for an unspecified reason."},
{"1060","UpdateFolderExists[1060]: Occurs when an attempt to update a Folder Name fails because another Folder with that name exists in the parent."},
{"1061","BadLabelId[1061]: Label ID is invalid"},
{"1062","BadLabelName[1062]: Label Name contains invalid characters"},
{"1063","DuplicateLabel[1063]: Label Name already exists in vault"},
{"1064","MakeLabelFailed[1064]: Cannot create label in database."},
{"1065","GetAllFilesFailed[1065]"},
{"1066","BadPropertyGroupId[1066]"},
{"1067","PropertyGroupExists[1067]"},
{"1069","PropertyGroupEmpty[1069]"},
{"1072","DeletePropertyGroupFailed[1072]"},
{"1073","MoveFolderFailed[1073]: Now used as: documentRestriction_11; documentRestriction_12; documentRestriction_13 in document world."},
{"1074","MoveFolderExists[1074]: Folder with the same name already exists in the destination folder"},
{"1075","MoveFolderDescendentCheckedOut[1075]: Folder begin moved has descendent files that are checked out."},
{"1076","MoveFolderChildRootInvalid[1076]: Move folder rule-check failed:  parent must exist, for all but root"},
{"1077","MoveFolderLibraryRelationshipInvalid[1077]: Move folder rule-check failed:  libs can only have non lib parent if that parent is root.  libs cannot have non lib children."},
{"1078","FolderNameInvalid[1078]: A null path or path will illegal characters has been passed in."},
{"1079","FolderFullNameTooLong[1079]"},
{"1080","IllegalNullParam[1080]: A null value has been passed in where null values are not allowed."},
{"1081","BadDate[1081]: The date is out of range for the DB.  The date should be between 1-1-1753 and 12-31-9999"},
{"1082","ArraysOfDifferentSizes[1082]: The input arrays were not of the same size"},
{"1083","LabelNameLengthInvalid[1083]"},
{"1084","UndoCheckoutNotCheckedOut[1084]: Cannot undo check out of the file because the file is not currently checked out."},
{"1085","BadSearchOperator[1085]: The search operator is not valid."},
{"1086","EmptyFolder[1086]: The folder being operated on is empty."},
{"1087","IllegalEmptyString[1087]: A empty string has been passed in where its is not allowed."},
{"1088","UnknownBOMVersion[1088]: ####################################"},
{"1089","InvalidBOMXml[1089]: ####################################"},
{"1090","BadPropertyDefId[1090]: Property Def Id is invalid."},
{"1091","CheckoutFolderInvalid[1091]: The file being checked out does not live in the specified folder, therefore it cannot be used as the checkout folder."},
{"1092","RestrictionsOccurred[1092]: Restrictions have occurred. More information available in SoapException detail."},
{"1093","GetRestrictionsFailed[1093]: Failed trying to determine restrictions."},
{"1094","PropertyGroupPropertyDefMinCount[1094]: Property groups must include a minimum of 2 property definitions."},
{"1098","MoveFileLocked[1098]: The file cannot be moved because it is locked"},
{"1099","DownloadFilePartInvalidByteRange[1099]"},
{"1100","UploadFileDoesNotExist[1100]"},
{"1101","DownloadFileSizeExceedsServerLimit[1101]"},
{"1102","UpdatePropertyDefinitionFailed[1102]"},
{"1103","CheckinFailedAssociatedFileCheckedout[1103]"},
{"1104","SetFileStatusFailed[1104]"},
{"1106","FullContentSearchContentIndexingDisabled[1106]: The client has submitted a search against file content but the Vault Content Indexing is disabled."},
{"1107","DimeAttachmentExpected[1107]: This error will be raised if AddFile or UploadFilePart is called without including a DIME attachment."},
{"1108","BadFileName[1108]"},
{"1109","ComponentCustomPropertyDefExists[1109]"},
{"1110","ParamNameInvalid[1110]"},
{"1111","DuplicateFileNamingSchemeExists[1111]"},
{"1112","AddFileNamingSchemeFailed[1112]"},
{"1113","UpdateFileNamingSchemeFailed[1113]"},
{"1114","FileExistsRemotely[1114]"},
{"1115","FileDoesNotExists[1115]"},
{"1116","SaveFilterConfigFailed[1116]: Raised when the Filter Config information cannot be saved to disk."},
{"1117","BadFileNamingScheme[1117]"},
{"1118","RollbackFileNamingDescriptionsFailed[1118]"},
{"1119","ReserveFileNamingDescriptionsFailed[1119]"},
{"1120","FolderFileNameCollision[1120]: AddFile, CheckinFile, MoveFile, ShareFile, AddFolder"},
{"1121","CreateUserDefinedPropertyDefinitionsFailed[1121]"},
{"1122","BadPropertyReindexError[1122]"},
{"1123","IdentifyFilesForPropertyReindexFailed[1123]"},
{"1124","AddDesignVisualizationAttachmentBadFileClassification[1124]"},
{"1125","AddDesignVisualizationAttachmentBadAttachmentOrder[1125]"},
{"1126","AddDesignVisualizationAttachmentExists[1126]"},
{"1127","SetDesignVisualizationStatusFileCheckedOut[1127]"},
{"1128","SetDesignVisualizationStatusInvalidStatus[1128]"},
{"1129","GenerateFileNumberFailed[1129]"},
{"1130","GenerateFileNumberFailedAutoFieldNumberUsedUp[1130]"},
{"1131","FileRenameReleasedParent[1131]"},
{"1132","WarningThresholdGreaterThanMaximumThreshold[1132]"},
{"1133","ThresholdOutOfRange[1133]"},
{"1134","ExceedBulkFileMaximumThreshold[1134]"},
{"1135","FailureToSaveFileForProviderUse[1135]"},
{"1136","UpdateFilePropertiesNotCheckedOut[1136]"},
{"1200","SerializeNullObject[1200]: When would this occur?"},
{"1300","BadId[1300]: Occurs when item revision ID is bad, user ID is bad"},
{"1301","BadUserId[1301]: The user ID couldn't be used to create an item revision"},
{"1302","BadItemRevision[1302]: The item revision couldn't be used to create the new item revision"},
{"1303","BadMasterItemId[1303]: The master item ID didn't return the tip item revision"},
{"1304","BadRevisionNumber[1304]: Item revision is less than or equal to current revision"},
{"1305","BadRevisionNumberFormat[1305]: Item revision isn't in current revision scheme"},
{"1306","UpdateItemsFailed[1306]: The item or unpinned item iteration associations could not be updated"},
{"1307","UpdateItemFailed[1307]: The item, its attachments, or unpinned item iteration associations could not be updated"},
{"1308","DeleteItemsFailed[1308]: Item could not be deleted"},
{"1309","GetTipItemRevisionsFailed[1309]: Tip item revisions could not be retrieved"},
{"1310","GetItemRevisionHistoryFailed[1310]: Item revision history could not be retrieved"},
{"1311","GetTipItemRevisionFailed[1311]: Item revision could not be retrieved. Please refresh and try again"},
{"1312","GetRolledUpBOMFailed[1312]: Rolled up BOM could not be retrieved"},
{"1313","GetAllBOMLinksAndRevisionsFailed[1313]: BOM links and revisions could not be retrieved"},
{"1314","BOMCompareFailed[1314]: The selected BOMs could not be compared"},
{"1315","GetNextRevisionOptionsFailed[1315]: Next revision options could not be retrieved"},
{"1316","PromoteFileIterationsFailed[1316]: File iterations could not be promoted"},
{"1317","UpdateItemsFromFilesFailed[1317]: Could not update items from files"},
{"1318","GetPromoteUpdateRestrictionsFailed[1318]: Could not get restrictions for promote and udpate"},
{"1319","GetItemUpdateRestrictionsFailed[1319]"},
{"1320","CreateItemRevisionFailed[1320]: Could not create an item revision"},
{"1321","EditItemRevisionFailed[1321]: Could not get item revision to edit"},
{"1322","DeleteItemIterationsFailed[1322]: Could not delete item iterations"},
{"1323","GetLifeCycleDefsFailed[1323]: Could not get lifecycle definitions"},
{"1324","GetItemIterationAttachmentsFailed[1324]: Could not get item iteration attachments"},
{"1325","GetItemFileLinksFailed[1325]: Could not get file links for item"},
{"1326","GetLifeCycleStateChangeRestrictionsFailed[1326]: Could not get restrictions for lifecycle state changes"},
{"1327","BulkChangeLifeCycleFailed[1327]: Could not perform bulk lifecycle change"},
{"1328","GetItemRevisionsForFileFailed[1328]"},
{"1329","GetUserDefinedPropertyDefinitionsFailed[1329]"},
{"1330","CreateUserDefinedPropertyDefinitionsFailed[1330]"},
{"1333","DeleteUnitOfMeasureFailed[1333]"},
{"1334","CreateUnitOfMeasureFailed[1334]"},
{"1335","EditUnitOfMeasureFailed[1335]"},
{"1336","GetUnitOfMeasureFailed[1336]"},
{"1337","GetAllUnitsOfMeasureFailed[1337]"},
{"1338","GetBaseUnitsOfMeasureFailed[1338]"},
{"1339","GetUnitOfMeasureFamilyFailed[1339]"},
{"1341","CreateItemNumberFailed[1341]"},
{"1344","DeleteUnusedItemNumbersFailed[1344]"},
{"1349","GetEditItemRevisionRestrictionsFailed[1349]"},
{"1350","GetLatestItemRevisionByItemNumberFailed[1350]"},
{"1351","GetChangeItemNumberRestrictionsFailed[1351]"},
{"1352","GetChangeItemRevisionNumberRestrictionsFailed[1352]"},
{"1353","GetDeleteUserDefinedPropertyDefinitionRestrictionsFailed[1353]"},
{"1354","DeleteUserDefinedPropertyDefinitionsFailed[1354]"},
{"1355","GetAllPropertyDefMappingsFailed[1355]"},
{"1356","GetUpdatePropertyDefMappingRestrictionsFailed[1356]"},
{"1357","UpdatePropertyDefMappingsFailed[1357]"},
{"1358","BadUnitOfMeasure[1358]"},
{"1359","SetItemRevisionFormatFailed[1359]"},
{"1360","GetSetItemRevisionFormatRestrictionsFailed[1360]"},
{"1361","GetDefaultRevisionFormatFailed[1361]"},
{"1362","PromoteFileIterationsConcurrencyFailed[1362]: Could not promote files due to concurrency issues."},
{"1364","InvalidLock[1364]: An Invalid Lock has occurred.  You were not able to obtain a lock for this operation or your lock expired."},
{"1365","GetDeleteItemRestrictionsFailed[1365]"},
{"1366","GetAllBOMStructureTypesFailed[1366]"},
{"1367","DuplicateName[1367]"},
{"1368","DuplicateId[1368]"},
{"1369","EntityInUse[1369]"},
{"1370","InvalidArrayLength[1370]"},
{"1371","GetItemTypeByIdFailed[1371]"},
{"1372","GetAllItemTypesFailed[1372]"},
{"1373","AddItemTypeFailed[1373]"},
{"1374","UpdateItemTypeFailed[1374]"},
{"1375","DeleteItemTypeFailed[1375]"},
{"1376","AddRevisionSequenceSchemeFailed[1376]"},
{"1377","AddRevisionFormatFailed[1377]"},
{"1378","GetAllRevisionFormatsFailed[1378]"},
{"1379","GetAllRevisionSequenceSchemesFailed[1379]"},
{"1380","AddItemIterationBOMLinksFailed[1380]"},
{"1381","UpdateItemIterationBOMLinksFailed[1381]: Update BOM failed."},
{"1382","ReorderItemIterationBOMLinksFailed[1382]"},
{"1383","DeleteItemIterationBOMLinksFailed[1383]"},
{"1384","NotEditable[1384]"},
{"1385","SetItemLifeCycleStateFailed[1385]"},
{"1386","GetItemIterationsByIterationIdsFailed[1386]"},
{"1387","RestrictionsOccurred[1387]"},
{"1388","GetRestrictionsFailed[1388]"},
{"1389","IncompatibleDataType[1389]"},
{"1390","EntityDeleted[1390]"},
{"1391","CircularReference[1391]"},
{"1392","DuplicatePriority[1392]"},
{"1393","DuplicateMapping[1393]"},
{"1394","GetMappablePropertyDefsFailed[1394]"},
{"1396","UpdateItemEffectivityFailed[1396]"},
{"1398","GetRestorableItemRevisionsFailed[1398]"},
{"1399","RestoreItemFailed[1399]"},
{"1400","GetItemPropertiesFailed[1400]"},
{"1401","GetAllItemPropertyDefinitionsFailed[1401]"},
{"1402","DuplicateLabel[1402]"},
{"1403","IntrinsicPropertyNameCollision[1403]"},
{"1404","GetItemRevisionByItemIterationIDFailed[1404]"},
{"1405","BadItemIterationId[1405]: Item version identified incorrectly."},
{"1406","ItemNumberInUse[1406]"},
{"1407","RevisionSequenceSchemeLengthGreaterThan16[1407]"},
{"1408","GetRollbackItemLifeCycleStatesInfoFailed[1408]: Could not retreive rollback information."},
{"1409","RollbackItemLifeCycleStatesFailed[1409]"},
{"1410","RollbackItemLifeCycleStatesCancelFailed[1410]"},
{"1411","GetItemRefDesPropertiesFailed[1411]"},
{"1412","GetAllItemRefDesPropertyDefinitionsFailed[1412]"},
{"1413","DuplicateBomSchemeName[1413]"},
{"1414","GetItemDuplicateCandidatesFailed[1414]"},
{"1415","ReassignComponentsToDifferentItemsFailed[1415]"},
{"1416","GetReleasedRevisionsFailed[1416]"},
{"1417","GetBOMFailedNothingEffective[1417]"},
{"1418","GetDWFWatermarksByItemIdFailed[1418]"},
{"1419","CreateDWFWatermarkDefinitionFailed[1419]"},
{"1420","GetAllDWFWatermarkDefinitionsFailed[1420]"},
{"1421","GetDWFWatermarkByFileIterationId[1421]"},
{"1422","GetEnableDWFWatermarkingFailed[1422]"},
{"1423","SetEnableDWFWatermarkingFailed[1423]"},
{"1424","UpdateDWFWatermarkDefinitionsFailed[1424]"},
{"1425","BadFileIterationId[1425]: File version identified incorrectly."},
{"1426","WatermarkRetrievalFailed[1426]"},
{"1427","DuplicateConstraintDefForProperty[1427]: Could not create a property constraint for the property because one of the some type already exists."},
{"1428","InvalidStringLength[1428]"},
{"1429","IllegalUseOfNull[1429]"},
{"1430","PropertyDefinitionDoesNotExist[1430]"},
{"1431","ItemTypeDoesNotExist[1431]"},
{"1432","InvalidConstraintExpression[1432]"},
{"1433","GetItemPackAndGoInfoFailed[1433]"},
{"1434","InvalidPropertyConstraintEntityTypeId[1434]"},
{"1435","GetRestrictLifeCycleStateChangeToChangeOrderFailed[1435]"},
{"1436","SetRestrictLifeCycleStateChangeToChangeOrderFailed[1436]"},
{"1437","BadUnitOfMeasureId[1437]"},
{"1442","GetEnablementConfigurationFailed[1442]"},
{"1443","SetEnablementConfigurationFailed[1443]"},
{"1445","GetItemRevisionByItemNumberAndRevisionNumberFailed[1445]"},
{"1446","GetRestrictAssignDesignFilesFailed[1446]"},
{"1447","SetRestrictAssignDesignFilesFailed[1447]"},
{"1448","GetItemBOMLinksFailed[1448]"},
{"1500","System_Error[1500]"},
{"1501","MSMQSendError[1501]"},
{"1502","Export_Get_Configuration_File_Error[1502]"},
{"1503","GetERPPackageError[1503]"},
{"1504","GetDWFPackageError[1504]"},
{"1505","ExportERPPackageError[1505]"},
{"1506","ExportDWFError[1506]"},
{"1507","CreateDWFPackageError[1507]"},
{"1508","ItemServiceError[1508]"},
{"1509","CopyFileError[1509]"},
{"1510","GetRevisionFromIterationIDError[1510]"},
{"1511","GetChildRevisionsError[1511]"},
{"1512","DeleteObsoletFilesError[1512]"},
{"1513","NoValidItemSelected[1513]"},
{"1514","Export_Save_Configuration_File_Error[1514]"},
{"1515","CheckStateError[1515]"},
{"1520","GetImportSystemFailed[1520]"},
{"1521","GetExportSystemFailed[1521]"},
{"1522","ImportFileNotFound[1522]"},
{"1523","ImportFileNotIntegrated[1523]"},
{"1524","InvalidImportFileFormat[1524]"},
{"1525","NoItemExists[1525]"},
{"1526","InvalidItemExists[1526]"},
{"1527","InvalidBOMStructure[1527]"},
{"1528","ReadCSVFileFailed[1528]"},
{"1529","ReadTDLFileFailed[1529]"},
{"1530","ReadXmlFileFailed[1530]"},
{"1531","ReadDwfFileFailed[1531]"},
{"1532","WriteCSVFileFailed[1532]"},
{"1533","WriteTDLFileFailed[1533]"},
{"1534","WriteXmlFileFailed[1534]"},
{"1535","WriteDwfFileFailed[1535]"},
{"1536","AttachmentNotFound[1536]"},
{"1537","AddERPFileToStoreFailed[1537]"},
{"1538","GetERPFileFromStoreFailed[1538]"},
{"1539","InvalidMappingInfo[1539]"},
{"1540","CreateTempItemsAndBOMFailed[1540]"},
{"1541","UpdateTempItemsAndBOMFailed[1541]"},
{"1542","CommitItemsAndBOMFailed[1542]"},
{"1543","GetItemPropertiesFailed[1543]"},
{"1544","GetExportItemInfoFailed[1544]"},
{"1545","GetERPPackageFailed[1545]"},
{"1546","ExportToERPFailed[1546]"},
{"1547","GetImportJobsFailed[1547]"},
{"1548","GetFileFromJobFailed[1548]"},
{"1549","InvalidFileType[1549]"},
{"1550","DirectoryNotExist[1550]"},
{"1551","BomStructureEmpty[1551]"},
{"1552","ItemStructureEmpty[1552]"},
{"1553","DataMapEmpty[1553]"},
{"1554","SendResultsEmailFailed[1554]"},
{"1555","SendResultsEmailAttachmentsError[1555]"},
{"1600","StateHasChanged[1600]: The state has changed since the last refresh so the action is not valid."},
{"1601","ActionDenied[1601]: OBSOLETE: use 405 instead"},
{"1602","UpdateDenied[1602]"},
{"1603","BadApproveDeadline[1603]: The approval deadline is in the past"},
{"1604","BadChangeOrderId[1604]: No such change order exists with the specified ID"},
{"1605","BadNumberingSchemeId[1605]: No such numbering scheme exists for change orders with the specified ID"},
{"1606","BadRoutingId[1606]: No routing exists for the change order process with the specified ID"},
{"1607","ChangeOrderNumberExists[1607]: Cannot add a change order since the change order number already exists."},
{"1608","GetChangeOrderFailed[1608]: Could not find the specified change order"},
{"1609","AddChangeOrderFailed[1609]: Unable to add change order"},
{"1610","MissingMasterItemId[1610]"},
{"1611","ChangeOrderLocked[1611]: The change order is locked by another user"},
{"1612","ItemOnAnotherChangeOrder[1612]: The item is being managed by another change order"},
{"1613","AddChangeOrderTypeFailed[1613]: A change order type defines a set of properties that should be attached to a change order."},
{"1614","DuplicateNumberSchemeName[1614]"},
{"1615","NumberingSchemeInUse[1615]"},
{"1616","GetAllChangeOrderTypesFailed[1616]: Could not get change order types"},
{"1617","UpdateChangeOrderTypeFailed[1617]"},
{"1618","GetChangeOrderNumberFailed[1618]: Could not get a number for a change order"},
{"1619","GetNumberingSchemesFailed[1619]: Could not get change order numbering schemes"},
{"1620","SetDefaultNumberingSchemeFailed[1620]"},
{"1621","ActivateNumberingSchemeFailed[1621]"},
{"1622","DeactivateNumberingSchemeFailed[1622]"},
{"1623","AddNumberingSchemeFailed[1623]"},
{"1624","UpdateNumberingSchemeFailed[1624]"},
{"1625","DeleteNumberingSchemeFailed[1625]"},
{"1626","DeleteUserDefinedPropertyDefinitionsFailed[1626]"},
{"1627","GetUserDefinedPropertyDefinitionIdsByChangeOrderTypeIdFailed[1627]"},
{"1628","CannotEditItem[1628]: Cannot make item on change order editable"},
{"1629","SetChangeOrderItemEditableFailed[1629]"},
{"1630","OwnerMustBeChangeRequestor[1630]: The change order creator cannot have change requestor role revoked."},
{"1631","NonChangeRequestorDenied[1631]: You must have the Change Requestor role on this change order's routing to perform the operation."},
{"1632","NumberingSchemeIsDefault[1632]"},
{"1633","RestrictionsOccurred[1633]: Restrictions have occurred. More information available in SoapException detail."},
{"1634","ItemObsolete[1634]: You cannot add obsolete items to a change order"},
{"1635","GetChangeOrderNumberSchemeStartFailed[1635]: used as IDS_NUMBERINGSCHEME_GET_STARTNUMBER_FAILED in Change Order world."},
{"1636","GetChangeOrderNumberSchemeStartFailedProviderDoesNotSupport[1636]: used as IDS_NUMBERINGSCHEME_GET_STARTNUMBER_PROVIDER_ERROR in Change Order world."},
{"1637","SetChangeOrderNumberSchemeStartFailed[1637]: used as IDS_NUMBERINGSCHEME_SET_STARTNUMBER_FAILED in Change Order world."},
{"1638","SetChangeOrderNumberSchemeStartFailedProviderDoesNotSupport[1638]: used as IDS_NUMBERINGSCHEME_SET_STARTNUMBER_PROVIDER_ERROR in Change Order world."},
{"1639","SetChangeOrderNumberSchemeStartFailedStartNumberMustBeGreaterThanCurrent[1639]: used as IDS_NUMBERINGSCHEME_SET_STARTNUMBER_LESS_ERROR in Change Order world."},
{"1640","GetChangeOrderNumberFailedAutoFieldNumberUsedUp[1640]: used as IDS_CHANGEORDER_CREATE_FAILED in Change Order world."},
{"1641","GetRollbackItemLifeCycleStatesInfoFailed[1641]: Could not retreive rollback information."},
{"1642","RollbackItemLifeCycleStatesFailed[1642]"},
{"1643","RollbackItemLifeCycleStatesCancelFailed[1643]"},
{"1644","ChangeOrderNotActive[1644]"},
{"1645","ItemNotOnChangeOrder[1645]"},
{"1646","GetUserDefinedPropertyDefinitionIdsByChangeOrderTypeIdFailed[1646]: Could not get item related user defined property definitions for change order type."},
{"1647","DeleteChangeOrderFailed[1647]"},
{"1648","NonResponsibleEngineerDenied[1648]"},
{"1649","GetAllPropertyDefinitionsFailed[1649]"},
{"1652","AddCustomPropertyDefFailed[1652]"},
{"1653","AddItemUserDefinedPropertyDefinitionsFailed[1653]"},
{"1654","GetPropertiesByChangeOrderIdsFailed[1654]"},
{"1655","GetItemPropertiesByChangeOrderIdsAndItemIdsFailed[1655]"},
{"1656","InappropriateRouting[1656]"},
{"1657","IllegalNullParam[1657]"},
{"1658","GetMarkupFolderFailed[1658]"},
{"1659","NoMarkupFolder[1659]"},
{"1660","InvalidMarkupFolderID[1660]"},
{"1661","SetMarkupFolderFailed[1661]"},
{"1664","GetRequireReviewLifeCycleStateBeforeChangeOrderReviewFailed[1664]"},
{"1665","SetRequireReviewLifeCycleStateBeforeChangeOrderReviewFailed[1665]"},
{"1666","GetDefaultWorkflowFailed[1666]"},
{"1667","SetDefaultWorkflowFailed[1667]"},
{"1668","GetAllActiveWorkflowsFailed[1668]"},
{"1669","GetWorkflowInfoFailed[1669]"},
{"1670","GetChangeOrderMarkupFolderIdFailed[1670]"},
{"1671","SetChangeOrderMarkupFolderIdFailed[1671]"},
{"1674","NoDefaultWorkflow[1674]"},
{"1675","BadWorkflowID[1675]"},
{"1676","GetInitialWorkflowFailed[1676]"},
{"1677","BadWorkflowRoleID[1677]"},
{"1678","BadWorkflowActiveID[1678]"},
{"1679","ItemlessChangeOrder[1679]"},
{"1680","AddCommentDenied[1680]"},
{"1681","AddCommentFailed[1681]"},
{"1682","GetChangeOrderMarkupFolderConfigurationFailed[1682]"},
{"1683","SetChangeOrderMarkupFolderConfigurationFailed[1683]"},
{"1684","AddMarkupFailed[1684]"},
{"1685","AddMarkupDenied[1685]"},
{"1686","NullMsgComponents[1686]"},
{"1687","BadDate[1687]"},
{"1688","CheckoutFileToChangeOrder[1688]"},
{"1689","FileOnAnotherChangeOrder[1689]"},
{"1690","ChangeOrderWithCheckoutFileCannotCloseOrCancel[1690]"},
{"1691","DeleteItemUserDefinedPropertyDefinitionsFailed[1691]"},
{"1809","ReportWriteFailure[1809]"},
{"1810","GenerateReportFailed[1810]"},
{"1811","GetTemplatePropertiesFailed[1811]"},
{"1812","InvalidParentChildInstanceProperty[1812]"},
{"1813","MissingPageReference[1813]"},
{"1814","MissingPageDetailsReference[1814]"},
{"1815","MissingDetailReference[1815]"},
{"1816","MissingCoverPageReference[1816]"},
{"1817","InequableAmountOfColumns[1817]"},
{"1818","InvalidPageSize[1818]"},
{"1819","MissingGroupHeader[1819]"},
{"1820","MissingGroupFooter[1820]"},
{"1821","InvalidData[1821]"},
{"1822","MissingPropertiesReference[1822]"},
{"1823","InvalidAmountOfColumnsInPropsSection[1823]"},
{"1824","PropertyLabelMustBeUnique[1824]"},
{"1825","InvalidPropertyDataType[1825]"},
{"1826","InvalidPropertySource[1826]"},
{"1827","InvalidReportProperty[1827]"},
{"1828","InvalidSystemProperty[1828]"},
{"1829","InvalidGroupDetail[1829]"},
{"1830","CellOutOfRangeError[1830]"},
{"1831","SectionHasTooManyRows[1831]"},
{"1832","InvalidInstanceProperty[1832]"},
{"1833","InvalidCalculatedProperty[1833]"},
{"1834","InvalidPropertyQualifier[1834]"},
{"1835","CircularReference[1835]"},
{"1836","PropertyLabelEmptyError[1836]"},
{"2000","PublishPackageFailed[2000]"},
{"2001","PublishDataAlreadyExists[2001]: An attempt to publish new data failed because it would overwrite existing data"},
{"2002","PublishOutOfSyncObject[2002]: An attempt to re-publish data failed because it has become out of date."},
{"2003","PublishLinkToSelf[2003]"},
{"2010","WriteLibraryInfoFailed[2010]: An error occurred while trying to write LibraryInfo object to library."},
{"2011","ReadLibraryInfoFailed[2011]: An error occurred while trying to read LibraryInfo object from library."},
{"2012","LibraryIdNotFound[2012]: The specified library ID was not found."},
{"2019","InvalidFamilyAspect[2019]: The specified aspect name is either not supported or is being used improperly."},
{"2020","ObjectDataNotFound[2020]: An attempt to get or delete the specified object data, or to get its version failed because no such object exists."},
{"2021","AddCategory_ParentNotFound[2021]: An attempt to add a child category failed because its parent does not exist."},
{"2022","UpdateCategory_CategoryNotFound[2022]"},
{"2023","AttachLibrary_GeneralFailure[2023]: Unused"},
{"2024","AttachLibrary_CheckFileExistence[2024]"},
{"2025","AttachLibrary_DataFileNotFound[2025]: Unused"},
{"2026","AttachLibrary_InitializeRolesAndPermissions[2026]: Unused"},
{"2027","AttachLibrary_AttachKnowledgeVault[2027]"},
{"2028","AttachLibrary_ReadLibraryInfo[2028]: Unused"},
{"2029","AttachLibrary_MissingLibraryInfo[2029]: Unused"},
{"2030","AttachLibrary_PrepareKnowledgeVaultMetaData[2030]: Unused"},
{"2031","AttachLibrary_WriteKnowledgeVaultMetaData[2031]"},
{"2032","AttachLibrary_UpdateLibraryUsers[2032]: Unused"},
{"2033","CreateLibrary_GeneralFailure[2033]: Unused"},
{"2034","CreateLibrary_InitializeRolesAndPermissions[2034]: Unused"},
{"2035","CreateLibrary_AddKnowledgeVault[2035]: Unused"},
{"2036","CreateLibrary_PrepareLibraryInfo[2036]: Unused"},
{"2037","CreateLibrary_WriteLibraryInfo[2037]"},
{"2038","CreateLibrary_PrepareKnowledgeVaultMetaData[2038]"},
{"2039","CreateLibrary_WriteKnowledgeVaultMetaData[2039]"},
{"2040","CreateLibrary_UpdateLibraryUsers[2040]: Unused"},
{"2041","UpdateLibraryUsers_MissingUsers[2041]: Unused"},
{"2042","UpdateLibraryUsers_AddFailed[2042]: Unused"},
{"2043","DetachLibrary_DatabaseNotFound[2043]: Unused"},
{"2044","TableOfContents_NoLanguageSpecified[2044]: A null or empty lang string was passed to GetTableOfContents"},
{"2045","CreateLibrary_AlreadyExists[2045]: Unused"},
{"2050","InvalidSchema_NotCompiled[2050]"},
{"2051","InvalidSchema_NoNamespace[2051]"},
{"2052","InvalidSchema_UnsupportedRestriction[2052]"},
{"2053","InvalidSchema_UnsupportedSimpleType[2053]"},
{"2054","InvalidSchema_UnsupportedRestrictionType[2054]: {0}"},
{"2055","InvalidSchema_UnsupportedDataType[2055]"},
{"2056","InvalidQuery_MissingRootNode[2056]: Search was not called with proper query xml"},
{"2057","InvalidQuery_NoReturnValues[2057]: Search did not specify anything to return"},
{"2058","InvalidQuery_MissingFieldSpecifier[2058]"},
{"2059","InvalidQuery_UnsupportedDataType[2059]"},
{"2060","InvalidQuery_MissingReturnProperty[2060]: Search did not fully specify a return value"},
{"2061","InvalidQuery_DuplicateReference[2061]: Search specified a duplicate reference in a return value"},
{"2062","InvalidQuery_InvalidFieldReference[2062]: Search referenced a return value that does not exist"},
{"2063","InvalidQuery_MissingOperator[2063]: Search constraint did not specifiy an operator"},
{"2064","InvalidQuery_MissingSearchCriterion[2064]: Search constraint did not specify a value"},
{"2065","InvalidQuery_MissingPropertyRelation[2065]"},
{"2066","InvalidQuery_UnsupportedSystemProperty[2066]: Search returns or is constrained by a a property that does not exist"},
{"2067","InvalidQuery_InvalidSchemaNamespace[2067]"},
{"2068","InvalidQuery_InvalidProperty[2068]"},
{"2069","InvalidQuery_InvalidRelation[2069]"},
{"2070","Resource_MissingResourceID[2070]"},
{"2071","Resource_StringsNotFound[2071]"},
{"2072","Resource_LocaleNotFound[2072]"},
{"2073","InvalidResource_MissingRootNode[2073]"},
{"2074","InvalidResource_LocaleMismatch[2074]"},
{"2075","InvalidResource_MissingLocale[2075]: {0}, library Id"},
{"2076","QueryExecutionError[2076]: Search resulted in a SQL error"},
{"2077","InternalError[2077]"},
{"2078","InvalidQuery_UnknownCategoryParameter[2078]: Search contained an unknown category parameter"},
{"2079","InvalidQuery_InvalidNumberOfValues[2079]"},
{"3002","FailedToFindPropertyDefinition[3002]"},
{"3003","InvalidParameterInput[3003]"},
{"3004","InternalError[3004]"},
{"3005","UnknownEntityClass[3005]"},
{"3006","UnknownEntityClassId[3006]"},
{"3007","UnknownPropertyDefinitionId[3007]"},
{"3008","InvalidPropertyDefDataTypeMapping[3008]"},
{"3009","EntityDataCreationFailed[3009]"},
{"3100","BadLifecycleDefinitionId[3100]"},
{"3101","BadStateId[3101]"},
{"3102","BadTransitionId[3102]"},
{"3103","BadEntityId[3103]"},
{"3104","AddLifeCycleStateTransitionFailed[3104]"},
{"3105","AddLifeCycleStateFailed[3105]"},
{"3106","AddLifeCycleDefinitionFailed[3106]"},
{"3107","AddLifeCycleStateTransitionACLFailed[3107]"},
{"3108","InvalidUserName[3108]"},
{"3109","CannotChangeDefinitionToItself[3109]"},
{"3110","InvalidDefinitionChange[3110]"},
{"3111","InvalidStateTransition[3111]"},
{"3115","DuplicatedStateDisplayName[3115]"},
{"3116","DuplicatedDefinitionDisplayName[3116]"},
{"3120","LifecycleDefinitionAlreadyExists[3120]"},
{"3121","LifecycleStateAlreadyExists[3121]"},
{"3122","LifecycleStateTransitionAlreadyExists[3122]"},
{"3123","RulePropDefDoesNotExist[3123]"},
{"3124","LifecycleDefinitionBeyondMaxLength[3124]"},
{"3125","TransitionSourceStateNotExist[3125]"},
{"3126","TransitionDestinationStateNotExist[3126]"},
{"3127","TransitionCrossLifecycleDefinition[3127]"},
{"3300","RevisionSequenceInUseCannotBeRemoved[3300]"},
{"3304","RevisionSequenceInUseCannotBeUpdated[3304]"},
{"3306","BadRevisionDefinitionId[3306]"},
{"3307","BadRevisionSequenceId[3307]"},
{"3308","RevisionSequenceDuplicateName[3308]"},
{"3309","RevisionDefinitionDuplicateName[3309]"},
{"3310","SeparatorNotValid[3310]"},
{"3311","SeparatorExistInRevisionSequence[3311]"},
{"3312","InvalidLabel[3312]"},
{"3313","RevisionLabelIsNullOrEmpty[3313]"},
{"3314","RevisionLabelBeyondMaxLength[3314]"},
{"3315","RevisionLabelDuplicate[3315]"},
{"3316","BadStartLabel[3316]"},
{"3320","BadEntityId[3320]"},
{"3321","InvalidDefinitionChange[3321]"},
{"3322","InvalidRevisionNumber[3322]"},
{"3330","GetRevisionDefinitionIdsByMasterIdsFailed[3330]"},
{"3331","GetNextRevisionNumbersByMasterIdsFailed[3331]"},
{"3333","SetRevisionNumberFailed[3333]"},
{"3334","SetRevisionNumbersFailed[3334]"},
{"3335","SetRevisionDefinitionAndNumbersFailed[3335]"},
{"3336","GetAllRevisionDefinitionInfoFailed[3336]"},
{"3337","GetRevisionDefinitionInfoByIdsFailed[3337]"},
{"3338","AddRevisionDefinitionFailed[3338]"},
{"3339","UpdateRevisionDefinitionFailed[3339]"},
{"3340","DeleteRevisionDefinitionFailed[3340]"},
{"3341","AddRevisionSequenceFailed[3341]"},
{"3342","UpdateRevisionSequenceFailed[3342]"},
{"3343","DeleteRevisionSequenceFailed[3343]"},
{"3344","SystemRevisionSequenceNotExist[3344]"},
{"3345","ImportRevisionDefinitionFailed[3345]"},
{"3346","RevisionDefinitionAlreadyExists[3346]"},
{"3500","BehaviorClassDoesNotSupportRules[3500]"},
{"3501","UnknownBehaviorClass[3501]"},
{"3502","UnknownBehaviorClassId[3502]"},
{"3503","FailureToGetBehaviorType[3503]"},
{"3504","FailureToCreateBehaviorClassInstance[3504]"},
{"3505","FailureToGetPropertySetId[3505]"},
{"3506","BehaviorClassFailedToProvideBehaviorView[3506]"},
{"3507","UnknownBehavior[3507]"},
{"3508","BehaviorCannotBeAssignedAsDefault[3508]"},
{"3509","BehaviorAlreadyAssocToEntityClass[3509]"},
{"3510","UnknownEntityAssocTable[3510]"},
{"3511","InvalidBehaviorsForAssocToEntityClass[3511]"},
{"3512","TooManyDefaultBehavoirsAssigned[3512]"},
{"3513","ZeroDefaultBehaviorsNoAllowed[3513]"},
{"3514","UnknownBehaviorId[3514]"},
{"3515","CannotDeleteBehavior_InUseByEntity[3515]"},
{"3516","CannotDeleteBehavior_InUseByAnotherBehavior[3516]"},
{"3517","CannotDeleteBehavior_ItIsAnEntityClassDefault[3517]"},
{"3518","CannotDeleteBehavior_ReasonUnknown[3518]"},
{"3519","UnknownBehaviorIdOrIsNotAssocToEntityClass[3519]"},
{"3700","UnknownCategoryId[3700]"},
{"3701","CategoryToCopyNotAssocToEntityClass[3701]"},
{"3702","FailureToFindCategoryRuleSet[3702]"},
{"3703","FailureToFindCategoryForEntity[3703]"},
{"3704","CategoryIdNotAssocToEntityClass[3704]"},
{"3705","CategoryAlreadyExists[3705]"},
{"3706","UnknownCategory[3706]"},
{"3707","EntityClassDoesNotSupportCategoryRules[3707]"},
{"3708","EntityIdDoesNotMatchEntityClass[3708]"},
{"3709","CategoryCfgCopyFailed[3709]"},
{"3710","CategoryUpdateFailed[3710]"},
{"3900","UpdatePropertyDefMappingsFailed[3900]"},
{"3901","DuplicateMapping[3901]"},
{"3902","NotUserDefinedPropertyDef[3902]"},
{"3903","SelfMapping[3903]"},
{"3904","NotFileIterationPropertyDef[3904]"},
{"3905","NotIndexedFileProperty[3905]"},
{"3906","GetUserDefinedPropertyDefinitionsFailed[3906]"},
{"3907","UpdateUserDefinedPropertyDefFailed[3907]"},
{"3908","UpdatePropertyValuesFailed[3908]"},
{"3909","GetPropertyDefDeleteRestrictionsFailed[3909]"},
{"3910","DeletePropertyDefsFailed[3910]"},
{"3911","AddOrRemovePropertyFailed[3911]"},
{"3912","AddPropertyConstraintsFailed[3912]"},
{"3913","UpdatePropertyConstraintsFailed[3913]"},
{"3914","DeletePropertyConstraintsFailed[3914]"},
{"3915","GetPropertyConstraintFailuresFailed[3915]"},
{"3916","CreateUserDefinedPropertyDefinitionsFailed[3916]"},
{"3917","InvalidOperationOnSpecificConstraintType[3917]"},
{"3918","DuplicateDisplayName[3918]"},
{"3919","GetPropertyDefinitionsFailed[3919]"},
{"3920","EntityClassIdDoesNotExist[3920]"},
{"3921","UserDefinedPropertyDefIdDoesNotExist[3921]"},
{"3922","UserDefinedPropertyDefIdHasAnAssignedEntityClass[3922]"},
{"3923","DisplayNameCollision[3923]"},
{"3924","InvalidMappingTarget_TOPropertyIsReadOnly[3924]"},
{"3925","InvalidMapping_DifferentDatatypes[3925]"},
{"3926","CheckCompliancesFailed[3926]"},
{"3929","InvalidMappingTarget_FROMProperty[3929]: Not all property definitions can be used as from mapping targets."},
{"3930","InvalidMappingTarget_TOProperty[3930]: Not all property definitions can be used as to mapping targets."},
{"3931","NonCompliantConstraintWithDefaultValue[3931]: New constraint can't be added or new default vaule can't be updated if the default value is not compliant with it."},
{"3932","UpdatedValueNotIncludedInList[3932]: The updated property value must be one of the value list for property definition of list type."},
{"3933","UpdatePropertiesFailed[3933]"},
{"4000","InputConfigSectionCanNotBeNull[4000]"},
{"4001","InvalidPropertySetGUID[4001]"},
{"4002","UDPDefintionNameCanNotBeEmpty[4002]"},
{"4003","UserDefinedPropertyDefaultValueRequired[4003]"},
{"4004","ValueExpressionNotParseable[4004]"},
{"4400","NoSitesInWorkgroupToRequestOwnership[4400]"},
{"4401","LeaseNotUp[4401]"},
{"4402","ErrorsWhileTransferringOwnership[4402]"},
{"4403","UnknownWorkgroup[4403]"},
{"4405","WorkgroupNotEntityOwner[4405]"},
{"4406","UnknownEntity[4406]"},
{"4407","WorkgroupNotAdminOwner[4407]"},
{"4408","WorkgroupIsPublisher[4408]"},
{"4410","WorkgroupExists[4410]"},
{"4411","NotFullSQL[4411]"},
{"4412","ConfigurationError[4412]"},
{"4413","ReplicationEnabled[4413]"},
{"4414","SubscribingWorkgroups[4414]"},
{"4415","SubscriberCleanupError[4415]"},
{"4416","ReplicationNotEnabled[4416]"},
{"4417","SubscriptionNotActive[4417]"},
{"4418","ServerConnectionFailureOrChangesPending[4418]"},
{"4419","DatabaseReplicationEnabled[4419]"},
{"4420","WorkgroupIsSubscriber[4420]"},
{"4421","SubscriberSqlVersionMismatch[4421]"},
{"4422","MigrationStateNotSetViaReplication[4422]"},
{"4423","OtherEntityInGroupNotTransferrable[4423]"},
{"4424","DatabaseNotReplicated[4424]"},
            };
        #endregion


        #region public static Dictionary<string, string[]> restrictionCodesAndParams = ...

        // Vault 2012
        public static Dictionary<string, string[]> restrictionCodesAndParams = new Dictionary<string, string[]>
            {
{"1000",new[]{"FileDependantParents: Operation cannot be performed because the file has dependent parents.","FilePath","ChildFileName"}},
{"1001",new[]{"FileCheckedOut: Operation cannot be performed because the file is checked out.","FilePath","FileCheckedOutByUserName"}},
{"1002",new[]{"FileOldVersion: Operation cannot be performed because the file is an old version.","FilePath"}},
{"1003",new[]{"FileLinkedToItem: Operation cannot be performed because the file is linked to an item.","FilePath"}},
{"1004",new[]{"FileAttachedToItem: Operation cannot be performed because the file is attached to an item.","FilePath"}},
{"1006",new[]{"FileParentSameName: Operation cannot be performed because the file has a parent with the same name.","FilePath"}},
{"1007",new[]{"FolderIsRoot: Operation cannot be performed because the folder is the root.","FilePath"}},
{"1009",new[]{"BadFolderId: Folder id is invalid.","FolderId"}},
{"1010",new[]{"MoveFolderExists: Folder with the same name already exists in the destination folder","FolderId","FolderName"}},
{"1011",new[]{"MoveFolderDescendentCheckedOut: Folder begin moved has descendent files that are checked out.","FolderId"}},
{"1012",new[]{"MoveFolderChildRootInvalid: Move folder rule-check failed: parent must exist, for all but root","FolderId"}},
{"1013",new[]{"MoveFolderLibraryRelationshipInvalid: Move folder rule-check failed: libs can only have non lib parent if that parent is root. libs cannot have non lib children.","FolderId"}},
{"1014",new[]{"MoveFolderConcurrent: Cannot finish move to destination folder because the destination folder was already created due to a concurrent operation (add,move, or rename)","FolderId"}},
{"1016",new[]{"FileStatusTrackingDisabled: Option is not set for tracking file status","FileMasterId"}},
{"1017",new[]{"FileStatusChildNotUpToDate: Child must be set to up to date in order for parent to be set to up to date","FileMasterId","FileIterationIds[]"}},
{"1018",new[]{"MoveFolderDestinationIsDescendant: Cannot move a folder because we are trying to move it to a child folder","FolderId"}},
{"1019",new[]{"MoveFolderDestinationIsSelf: Cannot move a folder to itself","FolderId"}},
{"1020",new[]{"FileNotCheckedOut: The file has not been checked out","FilePath"}},
{"2000",new[]{"ItemLocked: Operation cannot be performed because item is locked by another user.","ItemNumber","ItemTitle","ItemLockedByUserName"}},
{"2001",new[]{"ItemStateMustBeWIP: Operation cannot be performed because item life cycle state must be Work In Progress.","ItemNumber","ItemTitle","CurrentLifeCycleState","CurrentLifeCycleStateName"}},
{"2002",new[]{"ItemStateCantBeObsolete: Operation cannot be performed because item life cycle state can't be Obsolete.","ItemNumber","ItemTitle","CurrentLifeCycleState","CurrentLifeCycleStateName"}},
{"2003",new[]{"ItemStateMustBeObsolete: Operation cannot be performed because item life cycle state must be Obsolete.","ItemNumber","ItemTitle","CurrentLifeCycleState","CurrentLifeCycleStateName"}},
{"2004",new[]{"ItemInvalidStateTransition: Operation cannot be performed because an invalid state transition would occur.","ItemNumber","ItemTitle","CurrentLifeCycleState","CurrentLifeCycleStateName"}},
{"2005",new[]{"ItemParentPreventsState: Operation cannot be performed because the parent prevents the transition of the child's life cycle to this state.","ItemNumber","ItemTitle","ChildItemMasterID"}},
{"2006",new[]{"ItemChildPreventsState: Operation cannot be performed because a child prevents the transition of the parent's life cycle to this state.","ItemNumber","ItemTitle","ChildItemMasterID"}},
{"2009",new[]{"ItemInvalidUnitOfMeasure: Operation cannot be performed because an invalid unit of measure was specified.","ItemNumber","ItemTitle"}},
{"2010",new[]{"ItemPartOfOngoingPromoteOrUpdate: Operation cannot be performed because the item is part of an on-going promote or update.","ItemNumber","ItemTitle","LockedBy"}},
{"2011",new[]{"ItemLinkPreventsDeletion: Operation cannot be performed because an item link prevents the deletion.","ItemNumber","ItemTitle","ParentItemNumber"}},
{"2012",new[]{"ItemRevisionFormatAlreadyInUse: Operation cannot be performed because a revision format is already in use.","CurrentRevisionFormatID"}},
{"2013",new[]{"ItemRevisionFormatCurrentAndNewTheSame: Operation cannot be performed because the current revision formant and the new revision format are the same.","CurrentRevisionFormatID"}},
{"2014",new[]{"ItemRevisionFormatDoesNotExist: Operation cannot be performed because a revision format does not exist.","RevisionFormatID"}},
{"2015",new[]{"ItemPreviouslyReleased: Operation cannot be performed because there is a previously released revision.","ItemNumber","ItemTitle"}},
{"2016",new[]{"ItemControlledByChangeOrder: Operation cannot be performed because the item is being controlled by a change order.","ItemNumber","ItemTitle"}},
{"2017",new[]{"ItemNoItemToReplaceAfterExpiration: Operation cannot be performed because the item is set to expire but there is no item to replace it once it does expire.","ItemNumber","ItemTitle"}},
{"2018",new[]{"ItemBOMLinkCircularReference: Operation cannot be performed because the item that is being added has a parent item that is the same causing a circular reference.","ItemNumber","ItemTitle"}},
{"2019",new[]{"ItemNotRestorable: Operation cannot be performed because the item not restorable.","ItemNumber","ItemTitle"}},
{"2020",new[]{"ItemEquivalencyConflict: Operation cannot be performed because it would result in an equivalency conflict.","ItemNumber","ItemTitle"}},
{"2021",new[]{"ItemChangeOrderLinkPreventsDeletion: Operation cannot be performed because the item belongs to a change order.","ItemNumber","ItemTitle"}},
{"2022",new[]{"ItemPromoteOfPhantomOrReference: Can not promote phantom or reference files","FileName","FileID"}},
{"2023",new[]{"ItemEffectivityStartMustBeImmediateOrFuture: Operation cannot be performed because the item effectivity start date/time must be immediate or greater than the current date/time","ItemNumber","ItemTitle"}},
{"2024",new[]{"ItemHasNoStateToRollbackTo: Operation cannot be performed because item has no state to rollback to.","ItemNumber","ItemTitle"}},
{"2025",new[]{"ItemCantRollbackItemHistoryOnAClosedChangeOrder: Operation cannot be performed because you can not rollback a lifecycle change for a version that is associated with a closed or cancelled change order.","ItemNumber","ItemTitle","ChangeOrderNumber"}},
{"2026",new[]{"ItemMustBeOnAnActiveChangeOrder: Operation cannot be performed because the item must be associated with an active change order.","ItemNumber","ItemTitle"}},
{"2027",new[]{"ItemEffectivityStartOfChildGreaterThanParent: Operation cannot be performed because the child item has a later effectivity start date/time than one of it's parents and there is no prior effective revision for the child that can be used.","ChildItemNumber","ChildItemTitle","ParentItemNumber","ParentItemTitle","ParentMinEffStart (the child's effectivity start must be less than or equal to this value)"}},
{"2028",new[]{"ItemCantRollbackItemBeforeCreationOfChangeOrder: Operation cannot be performed because you can not rollback a lifecycle change for a version that was created before the creation of the change order.","ItemNumber","ItemTitle","ChangeOrderNumber"}},
{"2029",new[]{"ItemIsObsolete: Operation cannot be performed when the destination object is obsolete","ItemMasterID","ItemNumber","ItemTitle"}},
{"2030",new[]{"ItemWasDeleted: Operation cannot be performed because the destination item has been deleted","ItemMasterID","ItemNumber","ItemTitle"}},
{"2031",new[]{"ItemDestinationWasSource: Operation cannot be performed because destination items cannot also be source items.","ItemMasterID","ItemNumber","ItemTitle"}},
{"2032",new[]{"ItemSourceNotNew: Operation cannot be performed on source items unless they created by the current promote or update operation","ItemMasterID","ItemNumber","ItemTitle"}},
{"2033",new[]{"ItemCantPromoteConfigurationFactory: Operation cannot be performed configuration factories cannot be assigned to an item.","FileName"}},
{"2035",new[]{"ItemStandardComponentsMustHaveEquivalenceValue: Operation cannot be performed because standard components must have an equivalence value.","FileMasterID","componentID","componentUID","componentName"}},
{"2036",new[]{"ItemCantPromoteDesignDocument: Inventor drawing files cannot be assigned to items. Associate the drawing to the part or assembly file and assign the item to these files.","FileName"}},
{"2037",new[]{"ItemNotEditable: Operation cannot be performed because the item is not in an editable state.","ItemMasterID","ItemNumber","ItemTitle"}},
{"3000",new[]{"ChangeOrderLocked: Operation cannot be performed because item is locked by another user.","ChangeOrderNumber","ChangeOrderTitle","LockedByUserName"}},
{"3003",new[]{"ChangeOrderActivityRequiresItemToBeInReview: Operation cannot be performed because the item is not in the 'In Review' life cycle state.","ChangeOrderNumber","Item Number","Item Title","Item Life Cycle State"}},
{"3004",new[]{"ItemManualLifeCycleStateChangeNotAllowed: Operation cannot be performed because manual life cycle state changes are not allowed.","Item Number","Item Title"}},
{"3007",new[]{"ChangeOrderNumberAlreadyInUse: Operation cannot be performed because there already exists a Change Order with the specified Change Order Number.","ChangeOrderNumber"}},
{"3100",new[]{"PropertyConstraintFailure: Transition out of WIP cannot be performed because of one or more property constraint violations.","ItemNumber","Number of constraint failures on the item"}},
{"4000",new[]{"LessRestrctiveThanParent: Warning:Child ACL gives a user more permissions than in parent ACL","ChildEntityId","UserId"}},
{"4001",new[]{"BadEntityId: Entity does not exist","EntityId"}},
{"4002",new[]{"UnsupportedEntityClass: Operation not supported for that type of entity","EntityId"}},
{"5001",new[]{"StateTransitionDenied: User does not have the permission to perform that state transition","EntityId","FromStateId","ToStateId"}},
{"5002",new[]{"ChildStateNotSync: The child state is not match with the state parent transforming to.","StateIdOfChild","ChildName","ChildIsClocked","StateIdOfParent","ParentName","ParentIsClocked"}},
{"5003",new[]{"CannotPassCriteria: Entity's particular property not match the criteria defined with that transition.","EntityId","FromStateId","ToStateId"}},
            };
        #endregion

        /*
import re, sys
sys.path.append('/cygdrive/c/Temp')
from BeautifulSoup import BeautifulSoup

from itertools import *
fst=lambda (a,b):a
snd=lambda (a,b):b
multidict=lambda x: dict([(key, map(snd, vals)) for (key, vals) in groupby(sorted(x), fst)])

a=open('/cygdrive/c/Temp/vault_2012_error_codes.html').read().decode('latin1') # from api
soup=BeautifulSoup(a, convertEntities=BeautifulSoup.HTML_ENTITIES)
#b=[[re.sub('\s+',' ',td.text) for td in tr.findAll('td')] for tr in soup.findAll('tr')]
b=[[re.sub('\s+',' ',td.text.replace('"',"'")) for td in tr.findAll('td')] for tr in soup.findAll('tr')]
#c=[(code, name if desc == '' else '%s: %s' % (name,desc)) for (code,name,desc) in [(x+['','','',''])[:4] for x in b[4:]]] # 2013?
c=[(code, name + ('' if code == '' else '[%s]'%code) if desc == '' else '%s%s: %s' % (name,('' if code == '' else '[%s]'%code),desc)) for (code,name,_,desc) in [(x+['','','',''])[:4] for x in b[4:]]]
d=[]
for x in c:
    if(x[0] == ''):
        d[-1] = (d[-1][0], d[-1][1] + r'\r\n' + x[1])
    else:
        d.append(x)

failed=False
for (x,y) in multidict([(x[0],x) for x in d]).iteritems():
   if len(y) > 1:
      failed=True
      print x,y

if not failed:
   print '\r\n'.join(['{"%s","%s"},' % x for x in d])









        
import re, sys
sys.path.append('/cygdrive/c/Temp')
from BeautifulSoup import BeautifulSoup

from itertools import *
fst=lambda (a,b):a
snd=lambda (a,b):b
multidict=lambda x: dict([(key, map(snd, vals)) for (key, vals) in groupby(sorted(x), fst)])

a=open('/cygdrive/c/Temp/vault_2012_restriction_codes.html').read().decode('latin1') # from api
soup=BeautifulSoup(a, convertEntities=BeautifulSoup.HTML_ENTITIES)
#b=[[re.sub('\s+',' ',td.text) for td in tr.findAll('td')] for tr in soup.findAll('tr')]
b=[[re.sub('\s+',' ',td.text.replace('"',"'")) for td in tr.findAll('td')] for tr in soup.findAll('tr')]
c=[(code, params, name if desc == '' else '%s: %s' % (name,desc)) for (code,name,params,desc) in [(x+['','','',''])[:4] for x in b[4:]]]
d=[]
for x in c:
    if x[0] == '':
        if len(x[2].strip()) > 0:
           d[-1] = (d[-1][0], d[-1][1] + r'\r\n' + x[2], d[-1][2])
    else:
        d.append((x[0], x[2], x[1]))

d=multidict([(x[0],x) for x in d])
for (x,y) in list(d.iteritems()):
   if len(y) > 1:
      if x == '3004':
         d[x]=[y[-1]] # choose ChangeOrderNoUserPrivDeleteNotAllowed
      else:
          del d[x]

e=[(code,','.join(['"%s"'%x.strip() for x in [desc]+params.split(',')])) for [(code,desc,params)] in sorted(d.values())]

print '\r\n'.join(['{"%s",new[]{%s}},' % x for x in e])


*/
    }
}
