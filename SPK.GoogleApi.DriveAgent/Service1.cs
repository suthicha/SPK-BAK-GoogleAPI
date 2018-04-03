using SPKHelperPackage.Logs;
using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;

namespace SPK.GoogleApi.DriveAgent
{
    public partial class Service1 : ServiceBase
    {
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
            catch { }

            timer1.Start();
            Logged.Event("SERVICE", "Start Service...");
        }

        protected override void OnStop()
        {
            timer1.Stop();
            Logged.Event("SERVICE", "Stop Service...");
        }

        private void timer1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer1.Enabled = false;
            try
            {
                var clientId = ConfigurationManager.AppSettings["clientId"];
                var clientSecret = ConfigurationManager.AppSettings["clientSecret"];
                var projectId = ConfigurationManager.AppSettings["projectId"];
                var parentId = ConfigurationManager.AppSettings["parentId"];
                var path = ConfigurationManager.AppSettings["path"];
                var limitUpload = Convert.ToInt32(ConfigurationManager.AppSettings["limit"]);

                var driveAgent = new DriveFileAsync();
                driveAgent.LimitUpload = limitUpload;
                driveAgent.Upload(path, clientId, clientSecret, projectId, parentId);
            }
            catch (Exception ex)
            {
                Logged.Error("SERVICE", ex.Message);
            }
            timer1.Enabled = true;
        }
    }
}