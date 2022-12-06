using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BucketDesktop
{
    public class FtpFilePathMap
    {
        private static char dest_drive = '\0';
        public static string GetRoot()
        {
            return Program.DestPath;
        }

        public static string WasHere(string oripath)
        {
            string holderfile = oripath;
            if (holderfile.EndsWith(".washere") == false)
            {
                holderfile += ".washere";
            }
            if (File.Exists(holderfile) == false)
            {
                // placeholder not found, so just report back the original file path
                return oripath;
            }

            // placeholder file exists, the text inside indicates where the file was moved to
            string finalpath = File.ReadAllText(holderfile);
            if (finalpath.Contains(Path.PathSeparator))
            {
                // strip out any metadata after the path
                finalpath = finalpath.Substring(0, finalpath.IndexOf(Path.PathSeparator));
            }
            if (File.Exists(finalpath))
            {
                // the file's final resting place has been found
                return finalpath;
            }

            // didn't find it, but maybe it moved to another drive?
            string drv = GetRoot().Substring(0, 3);
            finalpath = drv + finalpath.Substring(3);
            if (File.Exists(finalpath))
            {
                return finalpath;
            }
            return oripath;
        }

        public static string GetNewPath(string pathname)
        {
            // rename both the destination directory and the photo file name with a date prefix
            string dtstr = DateTime.Now.ToString("yyMMdd");
            pathname = pathname.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            string lowerpathname = pathname.ToLower();
            string needle = Path.DirectorySeparatorChar + Program.CardRootDir + Path.DirectorySeparatorChar;
            if (lowerpathname.Contains(needle.ToLower()))
            {
                int folder_name_idx = lowerpathname.IndexOf(needle.ToLower()) + needle.Length;
                string folder_name = pathname.Substring(folder_name_idx);
                if (folder_name.Contains(Path.DirectorySeparatorChar))
                {
                    folder_name = folder_name.Substring(0, folder_name.IndexOf(Path.DirectorySeparatorChar));
                }
                folder_name = dtstr + "-" + folder_name;
                string folder_path = GetRoot() + Path.DirectorySeparatorChar + folder_name;
                if (Directory.Exists(folder_path) == false)
                {
                    Directory.CreateDirectory(folder_path);
                    Directory.CreateDirectory(folder_path + Path.DirectorySeparatorChar + Program.KeeperFolderName);
                }
                string nfilename = Path.GetFileName(pathname).Replace(Program.CardFilePrefix, Program.CardFilePrefix + dtstr);
                string final_file_path = folder_path + Path.DirectorySeparatorChar + nfilename;
                return final_file_path;
            }
            return pathname;
        }

        public static void WriteWasHere(string pathname, string destpath)
        {
            if (pathname.ToLower() == destpath.ToLower())
            {
                // no placeholder needed
                return;
            }
            // write placeholder file with new path inside the file
            File.WriteAllText(pathname + ".washere", destpath);
        }

        public static void SpoolUp()
        {
            // forces a file write so a sleeping HDD spins up
            File.WriteAllText(GetRoot() + Path.DirectorySeparatorChar + "spoolup.txt", DateTime.Now.ToLongTimeString());
        }
    }
}
