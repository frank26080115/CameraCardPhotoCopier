using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CameraCardPhotoCopier
{
    /// <summary>
    /// Create a New INI file to store or load data
    /// </summary>

    public class IniFile
    {
        public string FilePath;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(
            string section, string key, string val,
            string filePath
        );

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(
            string section, string key,
            string def, StringBuilder retVal, int size,
            string filePath
        );

        /// <summary>
        /// INIFile Constructor.
        /// </summary>
        /// <PARAM name="filePath">File path to .ini file</PARAM>
        public IniFile(string filePath)
        {
            FilePath = filePath;
        }

        /// <summary>
        /// Write data to .ini file
        /// </summary>
        /// <param name="Section">Section to write to</param>
        /// <param name="Key">Key to write to</param>
        /// <param name="Value">Value to write</param>
        /// <returns>True if successful</returns>
        public bool Write(string Section, string Key, string Value)
        {
            if (WritePrivateProfileString(Section, Key, Value, this.FilePath) != 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Read data from .ini file
        /// </summary>
        /// <param name="Section">Section to read from</param>
        /// <param name="Key">Key to read</param>
        /// <returns>String value of key, or null if failed</returns>
        public string Read(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);

            int i = GetPrivateProfileString(
                Section, Key,
                "", temp, 255,
                this.FilePath
            );

            if (i != 0)
                return temp.ToString();
            else
                return null;
        }

        public int ReadInt(string section, string key, int defval)
        {
            string s = Read(section, key);
            int res;
            if (int.TryParse(s, out res))
            {
                return res;
            }
            return defval;
        }

        public double ReadFloat(string section, string key, double defval)
        {
            string s = Read(section, key);
            double res;
            if (double.TryParse(s, out res))
            {
                return res;
            }
            return defval;
        }

        public string ReadStr(string section, string key, string defval)
        {
            string s = Read(section, key);
            if (s == null)
            {
                return defval;
            }
            if (s.Trim().Length <= 0)
            {
                return defval;
            }
            return s.TrimEnd();
        }

        public bool Exists
        {
            get { return File.Exists(FilePath); }
        }
    }
}
