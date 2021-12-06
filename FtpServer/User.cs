using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

using CameraCardPhotoCopier;

namespace SharpFtpServer
{
    [Serializable]
    public class User
    {
        [XmlAttribute("username")]
        public string Username { get; set; }

        [XmlAttribute("password")]
        public string Password { get; set; }

        //[XmlAttribute("homedir")]
        //public string HomeDir { get; set; }
        public string HomeDir
        {
            get
            {
                return Program.FtpHomeDir;
            }
        }
    }
}
