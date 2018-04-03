using System;
using System.Configuration;
using System.Globalization;
using System.Threading;

namespace SPK.GoogleApi.Console
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            System.Console.Title = "SPK Google.Apis.Drive.v2";
            System.Console.ForegroundColor = ConsoleColor.DarkGreen;
            System.Console.WriteLine("====================================================================");
            System.Console.WriteLine("==== SPK Google.Apis.Drive.v2 ");
            System.Console.WriteLine("==== Build 01.03.2018");
            System.Console.WriteLine("==== Upload zip file via google drive api.");
            System.Console.WriteLine("====================================================================");
            System.Console.ForegroundColor = ConsoleColor.White;
            System.Console.WriteLine("Start Service ...");

            var interval = Convert.ToInt32(ConfigurationManager.AppSettings["interval"]);

            using (new Timer(timerExecuteEveryTenMinute, null, TimeSpan.FromSeconds(interval), TimeSpan.FromSeconds(30)))
            {
                while (true)
                {
                    if (System.Console.ReadLine() == "exit")
                    {
                        System.Console.WriteLine("Exit Service ...");
                        break;
                    }
                }
            }
        }

        private static void timerExecuteEveryTenMinute(object state)
        {
            var _cultureInfo = new CultureInfo("en-US");

            System.Console.WriteLine("{0} : Begin start process.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", _cultureInfo));

            var clientId = ConfigurationManager.AppSettings["clientId"];
            var clientSecret = ConfigurationManager.AppSettings["clientSecret"];
            var projectId = ConfigurationManager.AppSettings["projectId"];
            var parentId = ConfigurationManager.AppSettings["parentId"];
            var path = ConfigurationManager.AppSettings["path"];
            var limitUpload = Convert.ToInt32(ConfigurationManager.AppSettings["limit"]);
            var archiveDays = Convert.ToInt32(ConfigurationManager.AppSettings["archiveDays"]);

            var driveAgent = new DriveFileAsync();
            driveAgent.LimitUpload = limitUpload;
            driveAgent.Upload(path, clientId, clientSecret, projectId, parentId);

            driveAgent.ClearArchive(archiveDays);

            System.Console.WriteLine("{0} : End process.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", _cultureInfo));
        }
    }
}