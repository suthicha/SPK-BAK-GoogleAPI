using System;

namespace SPK.GoogleApi.v2
{
    public class DbMessage
    {
        public string Period { get; set; }
        public string FileName { get; set; }
        public Double Size { get; set; }
        public DateTime BeginUpload { get; set; }
        public DateTime EndUpload { get; set; }
    }
}