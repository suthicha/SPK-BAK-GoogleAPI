using SPK.FtpClient;
using SPK.IonicZip;
using System;
using System.Windows.Forms;

namespace SPK.TestZip
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var zipCtrl = new ZipController();
            zipCtrl.ZipAndSplitFile(25, @"D:\demo\CTITBKK8_backup_2018_03_02_100002_1249116.bak", @"D:\demo\CTITBKK8_20180302.zip");

            MessageBox.Show("OK");

            //using (ZipFile zip = new ZipFile())
            //{
            //    zip.UseZip64WhenSaving = Zip64Option.Always;
            //    zip.AddFile(@"D:\demo\CTITBKK8_backup_2018_03_02_100002_1249116.bak", @"\");
            //    zip.MaxOutputSegmentSize = 20 * 1024 * 1024;
            //    zip.Save(@"D:\demo\CTITBKK8_20180302.zip");
            //}
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var host = @"ftp://198.1.1.1";
            var username = "ftpadmin";
            var password = "ftp2018!@";

            var ftpCtrl = new FtpController(host, username, password);

            var state = ftpCtrl.Upload("backup", @"d:\demo\CTITBKK8_20180302.z01");

            MessageBox.Show(state.ToString());
        }
    }
}