using Ionic.Zip;
using SPKHelperPackage.Logs;
using System;
using System.Collections.Generic;
using System.IO;

namespace SPK.IonicZip
{
    public class ZipController
    {
        public void ZipAndSplitFile(int size, string sourceFileName, string destinationFileName, string password = "")
        {
            FileInfo fileInfo = new FileInfo(sourceFileName);
            double fileSize = (fileInfo.Length / 1024) / 1024;

            Logged.Event("ZipController", "ZipAndSplitFile", fileSize.ToString("N2") + "MB.");

            using (ZipFile zip = new ZipFile())
            {
                try
                {
                    //if (!string.IsNullOrEmpty(password))
                    //{
                    //    zip.Password = password;
                    //}

                    zip.UseZip64WhenSaving = Zip64Option.Always;

                    if (fileSize > size)
                    {
                        // zip.UseZip64WhenSaving = Zip64Option.Always;
                        Logged.Event("ZipController", "ZipAndSplitFile", "Split Zip");

                        zip.AddFile(sourceFileName, @"\");
                        zip.MaxOutputSegmentSize = size * 1024 * 1024;
                    }
                    else
                    {
                        Logged.Event("ZipController", "ZipAndSplitFile", "Single Zip");

                        zip.AddFile(sourceFileName, @"\");
                    }

                    zip.Save(destinationFileName);
                }
                catch (Exception ex)
                {
                    Logged.Error("ZipController", "ZipAndSplitFile", ex.Message);
                }
            }
        }

        public List<FileReference> GetFileReference(string path, string host = "")
        {
            List<FileReference> fileRefs = new List<FileReference>();

            var di = new DirectoryInfo(path);
            var fi = di.GetFiles("*.*");

            for (int i = 0; i < fi.Length; i++)
            {
                var fiInfo = fi[i];
                var hostName = host;

                if (host == "")
                {
                    try
                    {
                        var arryName = fiInfo.Name.Split('_');
                        hostName = arryName[0];
                    }
                    catch { }
                }

                fileRefs.Add(new FileReference
                {
                    TrxNo = i + 1,
                    Host = host,
                    FileType = fiInfo.Extension,
                    FileName = hostName,
                    FullPath = fiInfo.FullName,
                    Size = fiInfo.Length,
                    CreateDateTime = fiInfo.CreationTimeUtc
                });
            }

            return fileRefs;
        }

        public void ExtractFileToDirectory(string zipFileName, string outputDirectory)
        {
            try
            {
                ZipFile zip = ZipFile.Read(zipFileName);

                Directory.CreateDirectory(outputDirectory);

                foreach (ZipEntry e in zip)
                {
                    Logged.Event("ZipController", "ExtractFileToDirectory", "BEGIN", e.FileName);
                    e.Extract(outputDirectory, ExtractExistingFileAction.OverwriteSilently);
                    Logged.Event("ZipController", "ExtractFileToDirectory", "END", e.FileName);
                }
            }
            catch (Exception ex)
            {
                Logged.Error("ZipController", "ExtractFileToDirectory", ex.Message);
            }
        }
    }
}