using Newtonsoft.Json;
using SPK.FtpClient;
using SPK.IonicZip;
using SPKHelperPackage.IO;
using SPKHelperPackage.Logs;
using System;
using System.Collections.Generic;
using System.IO;

namespace SPK.LocalDriveAgent
{
    public class BakController
    {
        private const string NAME = "BAKCONTROLLER";
        private readonly string _host;
        private readonly string _username;
        private readonly string _password;
        private readonly string _path;
        private readonly string _dataPath;
        private readonly int _maxSize;
        private readonly string _serverName;

        public BakController(string host,
            string username,
            string password,
            string path,
            string dataPath,
            string serverName,
            int maxSize)
        {
            _host = host;
            _username = username;
            _password = password;
            _path = path;
            _dataPath = dataPath;
            _maxSize = maxSize;
            _serverName = serverName;
        }

        private List<FileInfo> GetFiles(string path, string ext)
        {
            var asDir = new AsDirectory();
            return asDir.GetFiles(path, ext);
        }

        public void Go()
        {
            var bakFiles = GetFiles(_dataPath, "*.bak");
            if (bakFiles == null || bakFiles.Count == 0) return;

            for (int i = 0; i < bakFiles.Count; i++)
            {
                var fiInfo = bakFiles[i];

                // Split to zip.
                var zipStatus = ZipFile(fiInfo);

                if (zipStatus)
                {
                    // Ftp
                    UploadFtp(fiInfo);

                    // Copy to archive data.
                    Archive(fiInfo);
                }
            }
        }

        private void UploadFtp(FileInfo fileInfo)
        {
            var asDir = new AsDirectory();
            var files = asDir.GetFiles(TempPath);

            var ftpCtrl = new FtpController(_host, _username, _password);
            var fileRefs = new List<FileReference>();

            for (int i = 0; i < files.Count; i++)
            {
                try
                {
                    var objFile = files[i];
                    Logged.Event(NAME, "UploadFtp", "Begin Upload", objFile.Name, objFile.Name);

                    var uploadState = ftpCtrl.Upload(_path, objFile.FullName);

                    Logged.Event(NAME, "UploadFtp", objFile.Name, uploadState.ToString());

                    if (uploadState)
                    {
                        fileRefs.Add(new FileReference
                        {
                            TrxNo = i + 1,
                            Host = _serverName,
                            FileName = objFile.Name,
                            FullPath = objFile.FullName,
                            Size = objFile.Length,
                            FileType = objFile.Extension,
                            CreateDateTime = objFile.CreationTimeUtc
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logged.Error("BackController", "UploadFtp", ex.Message);
                }
            }

            var serialContent = JsonConvert.SerializeObject(fileRefs);
            var destFileName = Path.Combine(StorePath, Guid.NewGuid().ToString() + ".json");
            File.WriteAllText(destFileName, serialContent);

            ftpCtrl.Upload(_path, destFileName);
            Logged.Event(NAME, "UploadFtp", "JSON", destFileName);

            File.Delete(destFileName);
        }

        private string TempPath
        {
            get
            {
                var path = Path.Combine(Logged.AssemblyPath, "temp");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            }
        }

        private string StorePath
        {
            get
            {
                var path = Path.Combine(Logged.AssemblyPath, "store");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            }
        }

        private bool ZipFile(FileInfo fileInfo)
        {
            bool respState = false;

            try
            {
                var zipCtrl = new ZipController();

                var destinationFileName = Path.Combine(TempPath, fileInfo.Name.Replace(fileInfo.Extension, "") + ".zip");

                ClearTemp();

                Logged.Event(NAME, "ZipFile", fileInfo.Name, "Start");
                zipCtrl.ZipAndSplitFile(_maxSize, fileInfo.FullName, destinationFileName);
                Logged.Event(NAME, "ZipFile", fileInfo.Name, "End");

                respState = true;
            }
            catch (Exception ex)
            {
                Logged.Error(NAME, "ZipFile", ex.Message);
            }

            return respState;
        }

        private void ClearTemp()
        {
            try
            {
                var asDir = new AsDirectory();
                var files = asDir.GetFiles(TempPath);

                for (int i = 0; i < files.Count; i++)
                {
                    File.Delete(files[i].FullName);
                }
            }
            catch { }
        }

        private void Archive(FileInfo fileInfo)
        {
            try
            {
                var archiveFolder = Path.Combine(_dataPath, "Archive");
                if (!Directory.Exists(archiveFolder))
                {
                    Directory.CreateDirectory(archiveFolder);
                }

                var destFileName = Path.Combine(archiveFolder, fileInfo.Name);
                Logged.Event(NAME, "Archive", fileInfo.FullName, "Start");

                File.Copy(fileInfo.FullName, destFileName, true);

                File.Delete(fileInfo.FullName);

                Logged.Event(NAME, "Archive", destFileName, "End");
            }
            catch (Exception ex)
            {
                Logged.Error(NAME, "Archive", ex.Message);
            }
        }

        public void ClearArchive(int days)
        {
            var archiveFolder = Path.Combine(_dataPath, "Archive");
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
                    Logged.Event(NAME, "ClearArchive", objFile.FullName);
                }
            }
        }
    }
}