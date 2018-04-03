using System;

namespace SPK.IonicZip
{
    public class FileReference
    {
        public int TrxNo { get; set; }
        public string Host { get; set; }
        public string FileType { get; set; }
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public double Size { get; set; }
        public DateTime CreateDateTime { get; set; }
    }
}