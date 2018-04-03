using SPKHelperPackage.Logs;
using System;
using System.Configuration;
using System.ServiceProcess;

namespace SPK.LocalDriveAgent
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
            Logged.Event("Start Service");
        }

        protected override void OnStop()
        {
            timer1.Stop();
            Logged.Event("Stop Service");
        }

        private void timer1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer1.Enabled = false;

            try
            {
                var host = ConfigurationManager.AppSettings["host"];
                var username = ConfigurationManager.AppSettings["username"];
                var password = ConfigurationManager.AppSettings["password"];
                var path = ConfigurationManager.AppSettings["path"];
                var dataPath = ConfigurationManager.AppSettings["dataPath"];
                var maxSize = Convert.ToInt32(ConfigurationManager.AppSettings["maxSize"]);
                var serverName = ConfigurationManager.AppSettings["serverName"];
                var keepDays = Convert.ToInt32(ConfigurationManager.AppSettings["keepDays"]);

                var bakCtrl = new BakController(host, username, password, path, dataPath, serverName, maxSize);
                bakCtrl.Go();
                bakCtrl.ClearArchive(keepDays);
            }
            catch (Exception ex)
            {
                Logged.Error("SERVICE", ex.Message);
            }
            timer1.Enabled = true;
        }
    }
}