using Newtonsoft.Json;
using SPK.GoogleApi.v2;
using SPKHelperPackage.IO;
using SPKHelperPackage.Logs;
using System;
using System.Globalization;

namespace SPK.GoogleApi.DriveAgent
{
    public interface IDriveFileAsyncable
    {
        DriveFolder ReadDriveFolder();

        void Upload(string path, string clientId, string clientSecret, string projectId, string parentFolderId);
    }

    public class DriveFileAsync : IDriveFileAsyncable
    {
        private const string LOG_UPLOAD = "DRIVEFILEASYNC/UPLOAD";
        public int LimitUpload { get; set; }

        public DriveFileAsync()
        {
            LimitUpload = 10;
        }

        public DriveFolder ReadDriveFolder()
        {
            // Keep folder Id.
            var assyLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var storeFolderIdPath = System.IO.Path.Combine(assyLocation, ".dataStore/folderId.json");

            if (System.IO.File.Exists(storeFolderIdPath))
            {
                var driveFolderContent = System.IO.File.ReadAllText(storeFolderIdPath);
                return JsonConvert.DeserializeObject<DriveFolder>(driveFolderContent);
            }

            return null;
        }

        private DriveFolder CreateDriveFolder(string folderId, string parentId, string period)
        {
            var assyLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var storeFolderIdPath = System.IO.Path.Combine(assyLocation, ".dataStore/folderId.json");

            var driveFolder = new DriveFolder();
            driveFolder.Id = folderId;
            driveFolder.ParentId = parentId;
            driveFolder.Period = period;

            var serializeContent = JsonConvert.SerializeObject(driveFolder);

            if (System.IO.File.Exists(storeFolderIdPath))
            {
                System.IO.File.Delete(storeFolderIdPath);
            }

            var fileObj = new AsFile();
            fileObj.WriteFile(storeFolderIdPath, serializeContent);

            return driveFolder;
        }

        public void Upload(string path, string clientId, string clientSecret, string projectId, string parentFolderId)
        {
            try
            {
                var uploadCtrl = new UploadController(clientId, clientSecret, projectId);
                var folderName = DateTime.Now.ToString("yyyy-MM-dd", new CultureInfo("en-US"));

                Logged.Event(LOG_UPLOAD, "CHECK FOLDER", folderName);

                var driveFolder = ReadDriveFolder();

                if (driveFolder == null || (driveFolder.Period != folderName))
                {
                    driveFolder = new DriveFolder();
                    driveFolder.Id = uploadCtrl.CreateFolder(folderName, parentFolderId);
                    driveFolder.ParentId = parentFolderId;
                    driveFolder.Period = folderName;
                }

                Logged.Event(LOG_UPLOAD, "FOLDER ID", driveFolder.Id);

                var directoryObj = new AsDirectory();
                var files = directoryObj.GetFiles(path);

                if (files.Count == 0) return;

                Logged.Event(LOG_UPLOAD, "BEGIN UPLOAD", folderName);

                for (int i = 0; i < files.Count; i++)
                {
                    var fileInfo = files[i];
                    Logged.Event(LOG_UPLOAD, "UPLOADING...", fileInfo.Name);

                    if (uploadCtrl.Upload(driveFolder.Id, fileInfo.FullName, "application/zip"))
                    {
                        uploadCtrl.Transh(fileInfo.FullName);
                        Logged.Event(LOG_UPLOAD, "SUCCESS", fileInfo.Name);
                    }

                    if (i == LimitUpload) break;
                }

                Logged.Event(LOG_UPLOAD, "END UPLOAD", folderName);
            }
            catch (Exception ex)
            {
                Logged.Error(LOG_UPLOAD, ex.Message);
            }
        }
    }
}