using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;
using LogUtility;
using System.Security.Permissions;

namespace CameraCardPhotoCopier
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        static void Main()
        {
            Process[] processes = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            foreach (Process p in processes)
            {
                if (p.Id != Process.GetCurrentProcess().Id)
                {
                    //p.Kill();
                    MessageBox.Show("Error: another instance of this app \"" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + "\" is already running.", "Error Occured", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            try
            {
                //Logger.AcceptedLogLevel = LogLevel.Debug;
                //StartThreadMonitor();

                Program.IniFile = new CameraCardPhotoCopier.IniFile(Path.GetDirectoryName(Application.ExecutablePath) + Path.DirectorySeparatorChar + "settings.ini");

                Application.ThreadException += Application_ThreadException;
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MyApp());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + "\" has encountered a fatal error. Please see the log file.", "Fatal Error Occured", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logger.Log(LogLevel.Error, ex);
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            Logger.Log(LogLevel.Error, "CurrentDomain_UnhandledException");
            Logger.Log(LogLevel.Error, ex);
            MessageBox.Show("Error (CurrentDomain_UnhandledException): " + ex.Message, "Fatal Error Occured", MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (AppNotifyIcon != null)
            {
                AppNotifyIcon.Visible = false;
            }
            Application.Exit();
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Exception ex = (Exception)e.Exception;
            Logger.Log(LogLevel.Error, "Application_ThreadException");
            Logger.Log(LogLevel.Error, ex);
            MessageBox.Show("Error (Application_ThreadException): " + ex.Message, "Fatal Error Occured", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static NotifyIcon AppNotifyIcon = null;
        private static Thread monitoringThread = null;

        private static void StartThreadMonitor()
        {
            monitoringThread = new Thread(new ThreadStart(ThreadMonitor));
            monitoringThread.Start();
        }

        private static void ThreadMonitor()
        {
            int prev_cnt = 0;
            while (true)
            {
                ProcessThreadCollection currentThreads = Process.GetCurrentProcess().Threads;
                int cnt = currentThreads.Count;
                if (prev_cnt != cnt)
                {
                    Logger.Log(LogLevel.Debug, "thread cnt changed " + cnt);
                    prev_cnt = cnt;
                }
                Thread.Sleep(3000);
            }
        }

        public static CameraCardPhotoCopier.IniFile IniFile = null;
        private static Dictionary<string, string> IniDict = new Dictionary<string, string>();

        private static string GetIniVal(string sect, string name, string defval)
        {
            string s;
            try
            {
                if (IniDict.ContainsKey(sect + "-" + name))
                {
                    return IniDict[sect + "-" + name];
                }
                s = Program.IniFile.Read(sect, name);
                if (string.IsNullOrWhiteSpace(s))
                {
                    if (string.IsNullOrWhiteSpace(defval) == false)
                    {
                        try
                        {
                            Program.IniFile.Write(sect, name, defval);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(LogLevel.Error, "Cannot write \"" + name + "\" to ini file: " + ex.ToString());
                        }
                    }
                    return defval;
                }
                else
                {
                    IniDict.Add(sect + "-" + name, s);
                    return s;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "Cannot read \"" + name + "\" from ini file: " + ex.ToString());
                return defval;
            }
        }

        private static int GetIniValInt(string sect, string name, int defval)
        {
            string s = GetIniVal(sect, name, defval.ToString());
            try
            {
                return Convert.ToInt32(s);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "Cannot read integer \"" + name + "\" from ini file: " + ex.ToString());
                return defval;
            }
        }

        private static bool GetIniValBool(string sect, string name, bool defval)
        {
            string s = GetIniVal(sect, name, defval.ToString().ToLower().Trim()).ToLower().Trim();
            try
            {
                if (s == "true" || s == "yes" || s == "y")
                {
                    return true;
                }
                else if (s == "false" || s == "no" || s == "n")
                {
                    return false;
                }
                int i = Convert.ToInt32(s);
                return i == 0 ? false : true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "Cannot read bool \"" + name + "\" from ini file: " + ex.ToString());
                return defval;
            }
        }

        private static int threadSleepTime = -1;
        public static int ThreadSleepTime
        {
            get
            {
                if (threadSleepTime < 0)
                {
                    threadSleepTime = GetIniValInt("Settings", "ThreadSleepTime", 500);
                }
                return threadSleepTime;
            }
        }

        public static string CardRootDir
        {
            get
            {
                return GetIniVal("Paths", "CardRootDir", "DCIM");
            }
        }

        public static string CardFilePrefix
        {
            get
            {
                return GetIniVal("Paths", "CardFilePrefix", "DSC");
            }
        }

        public static string HomePath
        {
            get
            {
                return GetIniVal("Paths", "HomePath", "Pictures");
            }
        }

        public static string DestDirPrefix
        {
            get
            {
                return GetIniVal("Paths", "DestDirPrefix", "Photography");
            }
        }

        public static string KeeperFolderName
        {
            get
            {
                return GetIniVal("Paths", "KeeperFolderName", "good");
            }
        }

        public static bool FtpEnabled
        {
            get
            {
                return GetIniValBool("FTP", "FtpEnabled", false);
            }
        }

        public static int FtpPortNum
        {
            get
            {
                return GetIniValInt("FTP", "FtpPortNum", 345);
            }
        }

        public static string FtpHomeDir
        {
            get
            {
                return GetIniVal("FTP", "FtpHomeDir", "");
            }
        }

        public static string FtpUsername
        {
            get
            {
                return GetIniVal("FTP", "FtpUsername", "");
            }
        }

        public static string FtpPassword
        {
            get
            {
                return GetIniVal("FTP", "FtpPassword", "");
            }
        }
    }
}
