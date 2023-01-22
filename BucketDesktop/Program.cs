using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BucketDesktop
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        public static string FormatFileSize(long[] fileSizes)
        {
            for (int i = 3; i >= 0; i--)
            {
                string str = "";
                var divider = Math.Pow(1024, i);
                bool can_use = false;
                for (int j = 0; j < fileSizes.Length; j++)
                {
                    double x = Convert.ToDouble(fileSizes[j]) / Convert.ToDouble(divider);
                    if ((x >= 1 && i < 3) || x >= 10)
                    {
                        can_use = true;
                        break;
                    }
                }
                if (can_use)
                {
                    for (int j = 0; j < fileSizes.Length; j++)
                    {
                        double x = Convert.ToDouble(fileSizes[j]) / Convert.ToDouble(divider);
                        str += x.ToString("0.0").TrimEnd('0').TrimEnd('.');
                        if (j != fileSizes.Length - 1)
                        {
                            str += " / ";
                        }
                    }

                    switch (i)
                    {
                        case 0:
                            str += " bytes";
                            break;
                        case 1:
                            str += " KB";
                            break;
                        case 2:
                            str += " MB";
                            break;
                        case 3:
                            str += " GB";
                            break;
                    }
                    while (str.Contains("  "))
                    {
                        str = str.Replace("  ", " ");
                    }
                    return str;
                }
            }
            return string.Empty;
        }

        public static string FormatFileSize(long fileSize)
        {
            long[] fileSizes = new long[1];
            fileSizes[0] = fileSize;
            return FormatFileSize(fileSizes);
        }

        public static string FormatFileSize(long numer, long denom)
        {
            long[] fileSizes = new long[2];
            fileSizes[0] = numer;
            fileSizes[1] = denom;
            return FormatFileSize(fileSizes);
        }

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr memcmp(byte[] b1, byte[] b2, IntPtr count);
        public static int MemcmpCompare(byte[] b1, byte[] b2, int len)
        {
            IntPtr retval = memcmp(b1, b2, new IntPtr(len));
            return retval.ToInt32();
        }

        public static string DestPath = null;
        public static int FtpFileCnt = 0;
        public static long FtpFileSize = 0;

        public static IniFile Ini = null;

        public static string CardRootDir
        {
            get
            {
                return Ini.GetIniVal("Paths", "CardRootDir", "DCIM");
            }
        }

        public static string CardFilePrefix
        {
            get
            {
                return Ini.GetIniVal("Paths", "CardFilePrefix", "DSC");
            }
        }

        public static string HomePath
        {
            get
            {
                return Ini.GetIniVal("Paths", "HomePath", "Pictures");
            }
        }

        public static string DestDirPrefix
        {
            get
            {
                return Ini.GetIniVal("Paths", "DestDirPrefix", "Photography");
            }
        }

        public static string KeeperFolderName
        {
            get
            {
                return Ini.GetIniVal("Paths", "KeeperFolderName", "good");
            }
        }

        public static bool FtpEnabled
        {
            get
            {
                return Ini.GetIniValBool("FTP", "FtpEnabled", false);
            }
        }

        public static int FtpPortNum
        {
            get
            {
                return Ini.GetIniValInt("FTP", "FtpPortNum", 345);
            }
        }

        public static string FtpHomeDir
        {
            get
            {
                return Ini.GetIniVal("FTP", "FtpHomeDir", "");
            }
        }

        public static string FtpUsername
        {
            get
            {
                return Ini.GetIniVal("FTP", "FtpUsername", "");
            }
        }

        public static string FtpPassword
        {
            get
            {
                return Ini.GetIniVal("FTP", "FtpPassword", "");
            }
        }
    }
}
