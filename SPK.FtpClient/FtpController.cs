using System;
using System.Net;

namespace SPK.FtpClient
{
    public class FtpController
    {
        private readonly string _username;
        private readonly string _password;
        private readonly string _host;

        public FtpController(string host, string username, string password)
        {
            _host = host;
            _username = username;
            _password = password;
        }

        public bool Upload(string path, string sourceFileName)
        {
            bool respState = false;
            try
            {
                var fiInfo = new System.IO.FileInfo(sourceFileName);
                var uri = string.Format("{0}/{1}", _host, path);
                var reqUri = string.Format("{0}/{1}", uri, fiInfo.Name);
                var request = (FtpWebRequest)WebRequest.Create(reqUri);

                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(_username, _password);
                request.UsePassive = true;
                request.UseBinary = true;
                request.KeepAlive = false;

                using (var fileStream = System.IO.File.OpenRead(fiInfo.FullName))
                {
                    using (var requestStream = request.GetRequestStream())
                    {
                        fileStream.CopyTo(requestStream);
                        requestStream.Close();
                    }
                }

                var response = (FtpWebResponse)request.GetResponse();
                response.Close();

                respState = true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return respState;
        }
    }
}