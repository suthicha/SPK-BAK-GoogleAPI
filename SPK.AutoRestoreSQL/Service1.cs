using SPK.BackUpSql;
using SPK.IonicZip;
using SPKHelperPackage.Logs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceProcess;

namespace SPK.AutoRestoreSQL
{
    public partial class Service1 : ServiceBase
    {
        private const string NAME = "AUTO_RESOTRE_SQL_APP";

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            timer1.Interval = 3000;
            try
            {
                timer1.Interval = Convert.ToInt32(ConfigurationManager.AppSettings["interval"]);
            }
            catch
            {
            }

            timer1.Start();
            Logged.Event(NAME, "Start Service");
        }

        protected override void OnStop()
        {
            timer1.Stop();
            Logged.Event(NAME, "Stop Service");
        }

        private int currentTimeString()
        {
            return Convert.ToInt32(DateTime.Now.ToString("HH", new System.Globalization.CultureInfo("en-US")));
        }

        private void writeStatus(string statusText)
        {
            var fileName = Path.Combine(Logged.AssemblyPath, "status.txt");
            if (File.Exists(fileName)) File.Delete(fileName);

            File.WriteAllText(fileName, statusText);
        }

        private string readStatus()
        {
            var fileName = Path.Combine(Logged.AssemblyPath, "status.txt");
            if (!File.Exists(fileName))
                return "";
            else
                return File.ReadAllText(Path.Combine(Logged.AssemblyPath, "status.txt"));
        }

        private void timer1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer1.Enabled = false;

            try
            {
                var dbConnection = ConfigurationManager.AppSettings["dbConnection"];
                var copyFromDataPath = ConfigurationManager.AppSettings["CopyFromDataPath"];
                var dbNames = ConfigurationManager.AppSettings["DbNames"];
                var triggerTime = Convert.ToInt32(ConfigurationManager.AppSettings["triggerTime"]);

                if (triggerTime == currentTimeString())
                {
                    if (readStatus() != "A")
                    {
                        // Copy data from ftp temp.
                        var databases = dbNames.Split(',');

                        for (int i = 0; i < databases.Length; i++)
                        {
                            var databaseName = databases[i];
                            Logged.Event(NAME, "CopyFromDataPath", "CHECK", copyFromDataPath);

                            var dbFiles = GetFiles(databaseName, copyFromDataPath);
                            if (dbFiles.Count == 0) continue;

                            Logged.Event(NAME, "CopyFromDataPath", "COPY", copyFromDataPath);
                            CopyFile(dbFiles.ToArray());

                            UnZip();

                            Restore(databaseName, dbConnection);

                            writeStatus("A");

                            ClearFiles(TempPath);
                        }
                    }
                }
                else
                {
                    writeStatus("O");
                }
            }
            catch (Exception ex)
            {
                Logged.Error(NAME, "Timer", ex.Message);
            }
            timer1.Enabled = true;
        }

        private List<FileInfo> GetFiles(string dbName, string directoryName)
        {
            List<FileInfo> seekFiles = new List<FileInfo>();
            var di = new DirectoryInfo(directoryName);
            var fiZip = di.GetFiles("*.zip");

            if (fiZip.Length == 0) return null;

            List<FileInfo> seekZipFiles = new List<FileInfo>();

            for (int i = 0; i < fiZip.Length; i++)
            {
                var file = fiZip[i];
                var splitFileName = file.Name.Split('_');
                if (splitFileName.Length > 0)
                {
                    if (splitFileName[0] == dbName)
                        seekZipFiles.Add(file);
                }
                else
                {
                    if (file.Name.Contains(dbName))
                        seekZipFiles.Add(file);
                }
            }

            if (seekZipFiles.Count == 0) return null;

            var selectZipFile = seekZipFiles.OrderByDescending(d => d.CreationTimeUtc).First();
            di = new DirectoryInfo(directoryName);
            var files = di.GetFiles("*.*");
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                if (file.Name.Replace(file.Extension, "") == selectZipFile.Name.Replace(selectZipFile.Extension, ""))
                {
                    seekFiles.Add(file);
                }
            }

            return seekFiles;
        }

        private void CopyFile(params FileInfo[] files)
        {
            ClearFiles(TempPath);

            for (int i = 0; i < files.Length; i++)
            {
                FileInfo fi = files[i];
                string destFileName = Path.Combine(TempPath, fi.Name);
                File.Copy(fi.FullName, destFileName, true);
                Logged.Event(NAME, "CopyFile", fi.FullName, destFileName);
            }
        }

        private void UnZip()
        {
            Logged.Event(NAME, "Unzip", "BEGIN");

            ClearFiles(ZipPath);

            var di = new DirectoryInfo(TempPath);
            var fi = di.GetFiles("*.zip");

            if (fi.Length == 0)
            {
                Logged.Event(NAME, "Unzip", "NOT FOUND");
                return;
            }

            var zipCtrl = new ZipController();
            zipCtrl.ExtractFileToDirectory(fi[0].FullName, ZipPath);
            Logged.Event(NAME, "Unzip", "SUCCESS");
        }

        private void Restore(string dbName, string sqlConnectionString)
        {
            Logged.Event(NAME, "Restore SQL", "BEGIN");

            var outputDirectory = ZipPath;

            var di = new DirectoryInfo(outputDirectory);
            var fi = di.GetFiles("*.bak");

            if (fi.Length == 0)
            {
                Logged.Event(NAME, "Restore SQL", "NOT FOUND");
                return;
            }

            Logged.Event(NAME, "Restore SQL", "PROCESS", fi[0].FullName);

            var restDb = new RestDatabase(sqlConnectionString);
            restDb.Go(dbName, fi[0].FullName);

            Logged.Event(NAME, "Restore SQL", "SUCCESS", fi[0].FullName);

            ClearFiles(ZipPath);
        }

        private void ClearFiles(string directoryName)
        {
            var di = new DirectoryInfo(directoryName);
            var fi = di.GetFiles("*.*");

            for (int i = 0; i < fi.Length; i++)
            {
                try
                {
                    File.Delete(fi[i].FullName);
                }
                catch { }
            }
        }

        private string TempPath
        {
            get
            {
                var tempPath = Path.Combine(Logged.AssemblyPath, "temp");
                if (!Directory.Exists(tempPath))
                    Directory.CreateDirectory(tempPath);

                return tempPath;
            }
        }

        private string ZipPath
        {
            get
            {
                var outputDirectory = Path.Combine(Logged.AssemblyPath, "zip");
                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                return outputDirectory;
            }
        }
    }
}