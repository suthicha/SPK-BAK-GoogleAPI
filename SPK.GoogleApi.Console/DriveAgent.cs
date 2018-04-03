using Newtonsoft.Json;
using SPK.GoogleApi.v2;
using SPKHelperPackage.IO;
using SPKHelperPackage.Logs;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SPK.GoogleApi.Console
{
    public interface IDriveFileAsyncable
    {
        DriveFolder ReadDriveFolder(string name);

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

        public DriveFolder ReadPeriodFolder()
        {
            var storeFolderIdPath = Path.Combine(Logged.AssemblyPath, ".datastore/period.json");

            if (File.Exists(storeFolderIdPath))
            {
                var driveFolderContent = File.ReadAllText(storeFolderIdPath);
                return JsonConvert.DeserializeObject<DriveFolder>(driveFolderContent);
            }

            return null;
        }

        public DriveFolder CreatePeriodFolder(string folderId, string parentId, string name)
        {
            var dataStorePath = Path.Combine(Logged.AssemblyPath, ".datastore");
            if (!Directory.Exists(dataStorePath))
            {
                Directory.CreateDirectory(dataStorePath);
            }

            var storeFolderIdPath = Path.Combine(Logged.AssemblyPath, string.Format(".datastore/period.json", name));

            var driveFolder = new DriveFolder();
            driveFolder.Id = folderId;
            driveFolder.ParentId = parentId;
            driveFolder.Period = name;

            var serializeContent = JsonConvert.SerializeObject(driveFolder);
            Logged.Event(LOG_UPLOAD, "CREATE STORE FOLDER", serializeContent);

            System.Console.ForegroundColor = ConsoleColor.DarkGreen;
            System.Console.WriteLine(Logged.LastMessage);
            System.Console.ForegroundColor = ConsoleColor.White;

            if (File.Exists(storeFolderIdPath))
            {
                File.Delete(storeFolderIdPath);
            }

            var fileObj = new AsFile();
            fileObj.WriteFile(storeFolderIdPath, serializeContent);

            return driveFolder;
        }

        public DriveFolder ReadDriveFolder(string name)
        {
            var storeFolderIdPath = Path.Combine(Logged.AssemblyPath, string.Format(".datastore/{0}.json", name));

            if (File.Exists(storeFolderIdPath))
            {
                var driveFolderContent = File.ReadAllText(storeFolderIdPath);
                return JsonConvert.DeserializeObject<DriveFolder>(driveFolderContent);
            }

            return null;
        }

        private DriveFolder CreateDriveFolder(string folderId, string parentId, string name)
        {
            var dataStorePath = Path.Combine(Logged.AssemblyPath, ".datastore");
            if (!Directory.Exists(dataStorePath))
            {
                Directory.CreateDirectory(dataStorePath);
            }

            var storeFolderIdPath = Path.Combine(Logged.AssemblyPath, string.Format(".datastore/{0}.json", name));

            var driveFolder = new DriveFolder();
            driveFolder.Id = folderId;
            driveFolder.ParentId = parentId;
            driveFolder.Period = name;

            var serializeContent = JsonConvert.SerializeObject(driveFolder);
            Logged.Event(LOG_UPLOAD, "CREATE STORE FOLDER", serializeContent);

            System.Console.ForegroundColor = ConsoleColor.DarkGreen;
            System.Console.WriteLine(Logged.LastMessage);
            System.Console.ForegroundColor = ConsoleColor.White;

            if (File.Exists(storeFolderIdPath))
            {
                File.Delete(storeFolderIdPath);
            }

            var fileObj = new AsFile();
            fileObj.WriteFile(storeFolderIdPath, serializeContent);

            return driveFolder;
        }

        private void WriteMessage(DbMessage message)
        {
            try
            {
                var dataStorePath = Path.Combine(Logged.AssemblyPath, ".message");
                if (!Directory.Exists(dataStorePath))
                {
                    Directory.CreateDirectory(dataStorePath);
                }

                var storeFolderIdPath = Path.Combine(Logged.AssemblyPath, ".message/" + Guid.NewGuid().ToString() + ".json");
                var serialContent = JsonConvert.SerializeObject(message);
                File.WriteAllText(storeFolderIdPath, serialContent);
            }
            catch { }
        }

        public void Upload(string path, string clientId, string clientSecret, string projectId, string parentFolderId)
        {
            try
            {
                var uploadCtrl = new UploadController(clientId, clientSecret, projectId);
                var periodFolderName = DateTime.Now.ToString("yyyy-MM-dd", new CultureInfo("en-US"));

                var periodFolder = ReadPeriodFolder();

                if (periodFolder == null || periodFolder.Period != periodFolderName)
                {
                    Logged.Event(LOG_UPLOAD, "CHECK PERIOD FOLDER", periodFolderName);
                    System.Console.WriteLine(Logged.LastMessage);

                    periodFolder = new DriveFolder();
                    periodFolder.Id = uploadCtrl.CreateFolder(periodFolderName, parentFolderId);
                    periodFolder.ParentId = parentFolderId;
                    periodFolder.Period = periodFolderName;

                    periodFolder = CreatePeriodFolder(periodFolder.Id, periodFolder.ParentId, periodFolderName);
                    Logged.Event(LOG_UPLOAD, "CREATE STORE PERIOD FOLDER", periodFolderName);
                    System.Console.WriteLine(Logged.LastMessage);
                }

                if (periodFolder.Id == "")
                {
                    Logged.Event(LOG_UPLOAD, "CANNOT CREATE PERIOD FOLDER", periodFolderName);
                    return;
                }

                // var directoryObj = new AsDirectory();
                //var files = directoryObj.GetFiles(path);

                var di = new DirectoryInfo(path);
                FileInfo[] files = new String[] { "*.zip", "*.z*" }
                    .SelectMany(i => di.GetFiles(i, SearchOption.AllDirectories))
                    .ToArray();

                if (files.Length == 0) return;

                Logged.Event(LOG_UPLOAD, "BEGIN UPLOAD");
                System.Console.WriteLine(Logged.LastMessage);

                for (int i = 0; i < files.Length; i++)
                {
                    var fileInfo = files[i];
                    var periodFolderId = periodFolder.Id;

                    // Check directory.
                    var folderName = fileInfo.Name.Replace(fileInfo.Extension, "");
                    var driveFolder = ReadDriveFolder(folderName);

                    if (driveFolder == null || (driveFolder.Period != folderName))
                    {
                        Logged.Event(LOG_UPLOAD, "CHECK FOLDER", folderName);
                        System.Console.WriteLine(Logged.LastMessage);

                        driveFolder = new DriveFolder();
                        driveFolder.Id = uploadCtrl.CreateFolder(folderName, periodFolderId);
                        driveFolder.ParentId = periodFolderId;
                        driveFolder.Period = folderName;

                        driveFolder = CreateDriveFolder(driveFolder.Id, driveFolder.ParentId, folderName);
                        Logged.Event(LOG_UPLOAD, "CREATE STORE FOLDER", folderName);
                        System.Console.WriteLine(Logged.LastMessage);
                    }

                    // Upload file.
                    var dbMessage = new DbMessage();
                    dbMessage.Period = driveFolder.Period;
                    dbMessage.Size = fileInfo.Length;
                    dbMessage.FileName = fileInfo.Name;
                    dbMessage.BeginUpload = DateTime.Now;

                    Logged.Event(LOG_UPLOAD, "UPLOADING...", fileInfo.Name);

                    System.Console.ForegroundColor = ConsoleColor.White;
                    System.Console.WriteLine(Logged.LastMessage);

                    if (uploadCtrl.Upload(driveFolder.Id, fileInfo.FullName, "application/zip"))
                    {
                        // copy data to archive folder.
                        Archive(fileInfo.FullName);

                        // uploadCtrl.Transh(fileInfo.FullName);
                        Logged.Event(LOG_UPLOAD, "SUCCESS", fileInfo.Name);
                        System.Console.ForegroundColor = ConsoleColor.DarkGreen;
                        System.Console.WriteLine(Logged.LastMessage);
                        dbMessage.EndUpload = DateTime.Now;
                        WriteMessage(dbMessage);
                    }

                    if (i == LimitUpload) break;
                }

                Logged.Event(LOG_UPLOAD, "END UPLOAD");
                System.Console.ForegroundColor = ConsoleColor.White;
                System.Console.WriteLine(Logged.LastMessage);
            }
            catch (Exception ex)
            {
                Logged.Error(LOG_UPLOAD, ex.Message);
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine(Logged.LastMessage);
            }
        }

        private void Archive(string filePath)
        {
            var path = Path.Combine(Logged.AssemblyPath, "archive");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var fileInfo = new FileInfo(filePath);
            var destinationFile = Path.Combine(path, fileInfo.Name);

            File.Copy(filePath, destinationFile, true);
            File.Delete(filePath);
        }

        public void ClearArchive(int days)
        {
            var archiveFolder = Path.Combine(Logged.AssemblyPath, "archive");
            var currDate = DateTime.Now.AddDays(-days);
            var asDir = new AsDirectory();
            var files = asDir.GetFiles(archiveFolder);

            if (files.Count == 0) return;

            for (int i = 0; i < files.Count; i++)
            {
                var objFile = files[i];

                if (objFile.CreationTimeUtc <= currDate)
                {
                    File.Delete(objFile.FullName);
                    Logged.Event("DRIVE_AGENT", "ClearArchive", objFile.FullName);
                }
            }
        }
    }
}