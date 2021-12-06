using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

namespace CameraCardPhotoCopier
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Process[] processes = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            foreach (Process p in processes)
            {
                if (p.Id != Process.GetCurrentProcess().Id)
                {
                    //p.Kill();
                    MessageBox.Show("Error: another instance of this app \"" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + "\" is already running.", "Errors Occured", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MyApp());
        }

        public static string CardRootDir = "DCIM";
        public static string CardFilePrefix = "DSC";
        public static string HomePath = "Pictures";
        public static string DestDirPrefix = "Photography";
        public static string KeeperFolderName = "good";

        public static int FtpPortNum = 345;
        public static string FtpHomeDir = "";
        public static string FtpUsername = "sonyalpha1";
        public static string FtpPassword = "123";
    }
}
