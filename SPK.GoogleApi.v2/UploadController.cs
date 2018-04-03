using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using SPKHelperPackage.IO;
using SPKHelperPackage.Logs;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SPK.GoogleApi.v2
{
    public interface IUploadable
    {
        string CreateFolder(string folderName, string parentId);

        bool Upload(string folderId, string fileName, string mimeType);

        void Transh(string fileName);
    }

    public class UploadController : BaseClient, IUploadable
    {
        private const string LOG_AUTHENTICATED = "UPLOADCONTROLLER/AUTHENTICATED";
        private const string LOG_CREATE_SERVICE = "UPLOADCONTROLLER/CREATE_SERVICE";
        private const string LOG_CREATE_FOLDER = "UPLOADCONTROLLER/CREATE_FOLDER";
        private const string LOG_TRANSH = "UPLOADCONTROLLER/TRANSH";
        private const string LOG_UPLOAD = "UPLOADCONTROLLER/UPLOAD";

        public UploadController(string clientId, string clientSecret, string projectId) :
            base(clientId, clientSecret, projectId)
        {
        }

        public string CreateFolder(string folderName, string parentId)
        {
            string folderId = string.Empty;
            var service = CreateService();
            if (service == null) return folderId;

            try
            {
                Logged.Event(LOG_CREATE_FOLDER, "Initial Folder Metadata");
                var folderMetadata = new Google.Apis.Drive.v2.Data.File()
                {
                    Title = folderName,
                    MimeType = "application/vnd.google-apps.folder",
                    Parents = new List<ParentReference> {
                    new ParentReference { Id = parentId }
                }
                };

                Logged.Event(LOG_CREATE_FOLDER, "DriveService Insert");
                var reqService = service.Files.Insert(folderMetadata);
                reqService.Fields = "id";
                var file = reqService.Execute();

                folderId = file.Id;
                Logged.Event(LOG_CREATE_FOLDER, "SUCCESS", folderId);
            }
            catch (Exception ex)
            {
                Logged.Error(LOG_CREATE_FOLDER, ex.Message);
            }

            return folderId;
        }

        public void Transh(string fileName)
        {
            try
            {
                var fileObj = new AsFile();
                fileObj.Delete(fileName);
                Logged.Event(LOG_TRANSH, "DELETED", fileName);
            }
            catch (Exception ex)
            {
                Logged.Error(LOG_TRANSH, "DELETED FAIL", fileName, ex.Message);
            }
        }

        public bool Upload(string folderId, string fileName, string mimeType)
        {
            var service = CreateService();
            if (service == null) return false;

            bool result = false;

            try
            {
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(fileName);
                string fileLocalName = fileInfo.Name;

                Logged.Event(LOG_UPLOAD, "Instant File Metadata");
                var fileMetadata = new Google.Apis.Drive.v2.Data.File()
                {
                    Title = fileLocalName,
                    Parents = new List<ParentReference> { new ParentReference() {
                    Id= folderId} }
                };

                Logged.Event(LOG_UPLOAD, "Begin Upload", folderId, fileLocalName);
                FilesResource.InsertMediaUpload reqService;

                using (var stream = new System.IO.FileStream(fileName,
                                        System.IO.FileMode.Open))
                {
                    reqService = service.Files.Insert(
                        fileMetadata, stream, mimeType);
                    reqService.Fields = "id";
                    reqService.Upload();
                }

                Logged.Event(LOG_UPLOAD, "End Upload");
                var respFile = reqService.ResponseBody;

                if (respFile != null)
                {
                    Logged.Event(LOG_UPLOAD, "Upload Status", respFile.Id);
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Logged.Error(LOG_UPLOAD, folderId, fileName, ex.Message);
            }

            return result;
        }

        internal override UserCredential Authenticated()
        {
            string[] scopes = new string[] {
                DriveService.Scope.Drive,
                DriveService.Scope.DriveFile};

            Logged.Event(LOG_AUTHENTICATED, "Scopes", string.Join(",", scopes));

            // string credPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string credPath = Logged.AssemblyPath;

            credPath = System.IO.Path.Combine(credPath, ".credentials/drive-dotnet-backup.json");

            Logged.Event(LOG_AUTHENTICATED, "Credential Path", credPath);

            Logged.Event(LOG_AUTHENTICATED, "Initial credential", "Start...");
            var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets
            {
                ClientId = ClientId,
                ClientSecret = ClientSecret
            }, scopes, Environment.UserName, CancellationToken.None, new FileDataStore(credPath, true)).Result;

            Logged.Event(LOG_AUTHENTICATED, "Credential Success");

            return credential;
        }

        internal override DriveService CreateService()
        {
            DriveService service = null;
            try
            {
                var credential = Authenticated();

                Logged.Event(LOG_CREATE_SERVICE, "Initial DriveService");
                service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ProjectId,
                });

                Logged.Event(LOG_CREATE_SERVICE, "DriveService Success");
            }
            catch (Exception ex)
            {
                Logged.Error(LOG_CREATE_SERVICE, ex.Message);
            }

            return service;
        }
    }
}