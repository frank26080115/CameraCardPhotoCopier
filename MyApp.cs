using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;

using SharpFtpServer;

using LogUtility;
using System.Reflection;
using System.Diagnostics;

namespace CameraCardPhotoCopier
{
    public partial class MyApp : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private BackgroundWorker bgworker;    // this worker checks for drive insertions
        private BackgroundWorker copyworker;  // this worker does the copying
        private MenuItem startItem;
        private MenuItem infoItem;
        private MenuItem ejectItem;
        private MenuItem openFolderItem;
        private DateTime now;
        private int err_cnt;
        private char dest_drive = '\0';
        private char src_drive = '\0';
        private string dest_folder = "";

        private string progressText = "";

        private FtpServer ftpServer;

        public MyApp()
        {
            ftpServer = new FtpServer(Program.FtpPortNum);
            ftpServer.Start();

            infoItem = new MenuItem("");
            infoItem.Enabled = false;

            trayIcon = new NotifyIcon()
            {
                Icon = Resources.microsd_icon,
                ContextMenu = new ContextMenu(new MenuItem[] {
                    (startItem = new MenuItem("Start", Start)),
                    infoItem,
                    (ejectItem = new MenuItem("Eject", Eject)),
                    (openFolderItem = new MenuItem("Open Folder", OpenFolder)),
                    new MenuItem("Exit", Exit), 
                }),
                Visible = true
            };

            startItem.Enabled = false;
            ejectItem.Enabled = false;
            openFolderItem.Enabled = false;

            bgworker = new BackgroundWorker();
            bgworker.DoWork += Bgworker_DoWork;
            bgworker.WorkerReportsProgress = true;
            bgworker.ProgressChanged += Bgworker_ProgressChanged;
            bgworker.RunWorkerAsync();
        }

        private void Copyworker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            infoItem.Text = progressText;
            trayIcon.Text = progressText;
        }

        private void Copyworker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<DirectoryInfo> srcDirs = new List<DirectoryInfo>();
            string destDir;
            if (GetDirs(out destDir, srcDirs) == false)
            {
                return;
            }

            UInt64 total_bytes = 0;
            UInt64 done_bytes = 0;
            UInt64 done_bytes100 = 0;
            int total_files = 0;
            int done_files = 0;
            err_cnt = 0;

            try
            {
                // these loops just count how many files and how many bytes for informational display
                foreach (DirectoryInfo dn in srcDirs)
                {
                    FileInfo[] allFilesHere = dn.GetFiles();
                    foreach (FileInfo fn in allFilesHere)
                    {
                        total_files += 1;
                        total_bytes += Convert.ToUInt64(fn.Length);
                    }
                }

                progressText = string.Format("0% (0 GB : 0 F / {0:G3} GB : {1} F)", Convert.ToDouble(total_bytes / 1024 / 1024) / 1024.0, total_files);
                copyworker.ReportProgress(0);

                // make the destination directory
                if (Directory.Exists(destDir) == false)
                {
                    try
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    catch (Exception ex)
                    {
                        err_cnt++;
                        string errmsg = "While creating dir \"" + destDir + "\", exception occured: " + ex.Message;
                        Logger.Log(LogLevel.Error, errmsg);
                        return;
                    }
                }

                // date code prefix for folders and files since the Sony 5 digits isn't really enough for a serious photographer
                string dtstr = now.ToString("yyMMdd");

                foreach (DirectoryInfo dn in srcDirs)
                {
                    string dname = Path.GetFileName(dn.Name);
                    string thisDestDir = destDir + Path.DirectorySeparatorChar + dtstr + "-" + dname; // renames the directory out of the camera to one with the date prefix

                    try
                    {
                        // this checks if the directory with a matching suffix already exists, which means that this is a continuation of that directory
                        string[] all_dests = Directory.GetDirectories(destDir);
                        foreach (string existing_dir in all_dests)
                        {
                            if (existing_dir.ToLower().EndsWith("-" + dname.ToLower()))
                            {
                                thisDestDir = Path.GetFullPath(existing_dir);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        string errmsg = "While checking older directories for \"" + dname + "\", exception occured: " + ex.Message;
                        Logger.Log(LogLevel.Error, errmsg);
                    }

                    // we use a text file to track which photos were already copied
                    // each line is a simply file name
                    // this is done so that we can cull a directory and not have it copied again
                    string recordFile = thisDestDir + Path.DirectorySeparatorChar + "already_copied.txt";
                    List<string> alreadyCopied = new List<string>();

                    try
                    {
                        if (File.Exists(recordFile))
                        {
                            alreadyCopied.AddRange(File.ReadAllLines(recordFile));
                        }
                    }
                    catch (Exception ex)
                    {
                        string errmsg = "While reading record file \"" + recordFile + "\", exception occured: " + ex.Message;
                        Logger.Log(LogLevel.Error, errmsg);
                    }

                    try
                    {
                        // make the target directory, the one with the date prefix
                        if (Directory.Exists(thisDestDir) == false)
                        {
                            Directory.CreateDirectory(thisDestDir);

                            // makes a "keeper" folder inside the dated folder for convenience
                            // TODO: parse file ratings and populate the keeper folder with rated files
                            string keeperFolder = thisDestDir + Path.DirectorySeparatorChar + Program.KeeperFolderName;
                            try
                            {
                                Directory.CreateDirectory(keeperFolder);
                            }
                            catch (Exception ex)
                            {
                                err_cnt += 1;
                                Logger.Log(LogLevel.Error, "While creating dir \"" + keeperFolder + "\", exception occured: " + ex.Message);
                            }
                        }

                        FileInfo[] allFilesHere = dn.GetFiles();
                        foreach (FileInfo fn in allFilesHere)
                        {
                            string oldFullPath = fn.FullName;
                            string fname = fn.Name;
                            string destName = fname;
                            try
                            {
                                if (fname.StartsWith(Program.CardFilePrefix))
                                {
                                    // if it is a photo file with the 5 digit suffix, prepend the date prefix
                                    destName = fname.Substring(0, 3) + dtstr + fname.Substring(3);
                                }
                                dest_folder = thisDestDir;
                                openFolderItem.Enabled = true;
                                string destPath = thisDestDir + Path.DirectorySeparatorChar + destName;
                                if (alreadyCopied.Contains(fname))
                                {
                                    Logger.Log(LogLevel.Info, "Skipped \"" + oldFullPath + "\" to \"" + destPath + "\"");
                                }
                                else if (File.Exists(destPath) == false)
                                {
                                    fn.CopyTo(destPath, true);
                                    Logger.Log(LogLevel.Info, "Copied new \"" + oldFullPath + "\" to \"" + destPath + "\"");
                                    File.AppendAllText(recordFile, fname + Environment.NewLine);
                                }
                                else
                                {
                                    FileInfo overwriteMe = new FileInfo(destPath);
                                    if (overwriteMe.Length != fn.Length)
                                    {
                                        Logger.Log(LogLevel.Info, "Copied overwrite \"" + oldFullPath + "\" to \"" + destPath + "\"");
                                        fn.CopyTo(destPath, true);
                                    }
                                    else
                                    {
                                        Logger.Log(LogLevel.Info, "Skipped \"" + oldFullPath + "\" to \"" + destPath + "\"");
                                    }
                                    File.AppendAllText(recordFile, fname + Environment.NewLine);
                                }
                                done_files += 1;
                                done_bytes += Convert.ToUInt64(fn.Length);
                                done_bytes100 = done_bytes * 100;
                                progressText = string.Format("{0}% ({1:G3} GB : {2} F / {3:G3} GB : {4} F)"
                                    , done_bytes100 / total_bytes
                                    , Convert.ToDouble(done_bytes / 1024 / 1024) / 1024.0
                                    , done_files
                                    , Convert.ToDouble(total_bytes / 1024 / 1024) / 1024.0
                                    , total_files
                                    );
                                copyworker.ReportProgress(0);
                            }
                            catch (Exception ex)
                            {
                                err_cnt += 1;
                                string destPath = thisDestDir + Path.DirectorySeparatorChar + destName;
                                Logger.Log(LogLevel.Error, "While copying \"" + fname + "\" to \"" + destPath + "\", exception occured: " + ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        err_cnt += 1;
                        Logger.Log(LogLevel.Error, "While creating dir \"" + thisDestDir + "\", exception occured: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                err_cnt += 1;
                Logger.Log(LogLevel.Error, "Fatal error!" + ex.Message);
            }

            progressText = string.Format("Done {0}% ({1:G3} GB : {2} F / {3:G3} GB : {4} F)"
                , total_bytes > 0 ? (done_bytes100 / total_bytes) : 0
                , Convert.ToDouble(done_bytes / 1024 / 1024) / 1024.0
                , done_files
                , Convert.ToDouble(total_bytes / 1024 / 1024) / 1024.0
                , total_files
                );
            copyworker.ReportProgress(0);
        }

        private void Bgworker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
            {
                DialogResult res = MessageBox.Show("Photos found, start copying?", "New Photos", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    Start(sender, e);
                }
            }
        }

        private void Bgworker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool prevResult = false;
            while (true)
            {
                try
                {
                    Thread.Sleep(500); // don't hog CPU

                    if (ftpServer.new_client_flag == FtpClientStatus.Transfering)
                    {
                        ftpServer.new_client_flag = FtpClientStatus.None; // clear the flag so we don't constantly show annoying notifications
                        FtpFilePathMap.SpoolUp();
                        trayIcon.ShowBalloonTip(0, "FTP Photo Transfer", "New FTP connection detected!", ToolTipIcon.Info);
                    }

                    if (copyworker != null)
                    {
                        if (copyworker.IsBusy)
                        {
                            continue;
                        }
                    }

                    // enable/disable/show/hide UI elements based on state

                    if (GetDirs())
                    {
                        if (prevResult == false) // do not repeatedly show notification if user dismissed previous notification
                        {
                            startItem.Enabled = copyworker != null ? (copyworker.IsBusy ? false : true) : true;
                            bgworker.ReportProgress(1); // dirty hack just to make the main thread show messages/UI
                        }
                        prevResult = true;
                    }
                    else
                    {
                        prevResult = false;
                        startItem.Enabled = false;
                    }

                    openFolderItem.Enabled = dest_folder.Length > 0;
                    if (src_drive != '\0')
                    {
                        ejectItem.Enabled = copyworker != null ? (copyworker.IsBusy ? false : true) : true; // do not allow eject if copying is in progress
                        ejectItem.Text = "Eject \"" + src_drive + ":\\\"";
                    }
                    else
                    {
                        ejectItem.Enabled = false;
                        ejectItem.Text = "Eject (none)";
                    }
                }
                catch (ThreadAbortException ex)
                {
                    string errmsg = "BG thread abort occured: " + ex.Message;
                    Logger.Log(LogLevel.Error, errmsg);
                }
                catch (Exception ex)
                {
                    string errmsg = "BG thread exception occured: " + ex.Message;
                    Logger.Log(LogLevel.Error, errmsg);
                    MessageBox.Show("Error: " + errmsg, "Errors Occured", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        bool GetDirs()
        {
            string dummy;
            List<DirectoryInfo> dummy2 = new List<DirectoryInfo>();
            return GetDirs(out dummy, dummy2);
        }

        bool GetDirs(out string destDirPath, List<DirectoryInfo> srcDirs)
        {
            now = DateTime.Now;
            DriveInfo[] drvNfos = DriveInfo.GetDrives();
            List<DriveInfo> drivesThatMatter = new List<DriveInfo>();
            foreach (DriveInfo d in drvNfos)
            {
                if (d.Name.ToUpper()[0] != 'C') // ignore C drive, which is internal, this app is only meant for externa drives
                {
                    drivesThatMatter.Add(d);
                }
            }

            string destDirName = Program.DestDirPrefix + now.Year;
            string destHomeName = Program.HomePath;
            string destHomeDir = ":" + Path.DirectorySeparatorChar + destHomeName;
            bool destFound = false;

            // try to find the destination drive
            // we know it's the correct destination drive if it contains the directory structure we are looking for

            if (dest_drive == '\0') // no previous drive was cached
            {
                foreach (DriveInfo d in drivesThatMatter)
                {
                    // does this drive contain the folder structure we are looking for
                    if (Directory.Exists(d.Name[0] + destHomeDir))
                    {
                        dest_drive = d.Name[0];
                        destHomeDir = dest_drive + destHomeDir;
                        destFound = true;
                        break;
                    }
                }
            }
            else // previous drive letter was cached, use it
            {
                foreach (DriveInfo d in drivesThatMatter)
                {
                    if (d.Name[0] == dest_drive)
                    {
                        // as long as the drive exists, we don't actually check if the directory exists, we just assume it does
                        // this is because external drives go to sleep and we'd like it to stay asleep
                        destFound = true;
                        destHomeDir = dest_drive + destHomeDir;
                        break;
                    }
                }
            }

            if (destFound == false)
            {
                dest_drive = '\0';
                destDirPath = "";
                return false;
            }

            destDirPath = destHomeDir + Path.DirectorySeparatorChar + destDirName;

            // find the SD card
            // it should contain the directory structure we expect ("DCIM" for Sony)

            string srcHomeName = ":" + Path.DirectorySeparatorChar + Program.CardRootDir;
            bool has_card = false;
            foreach (DriveInfo drv in drivesThatMatter)
            {
                if (drv.Name[0] != dest_drive)
                {
                    if (drv.IsReady)
                    {
                        if (Directory.Exists(drv.Name[0] + srcHomeName))
                        {
                            src_drive = drv.Name[0];
                            has_card = true;
                            break;
                        }
                    }
                }
            }

            if (has_card == false)
            {
                src_drive = '\0';
                return false;
            }

            // check for new directories on the SD card
            // this is a bit messy because the folder names on the SD card will not match the names on the destination external drive
            // because we prepended a date onto the name

            Dictionary<string, string> existing_prefixes = new Dictionary<string, string>();
            Dictionary<string, string> existing_suffixes = new Dictionary<string, string>();
            if (Directory.Exists(destDirPath))
            {
                dest_folder = destDirPath;
                string[] destDatedDirs = Directory.GetDirectories(destDirPath);
                foreach (string d in destDatedDirs)
                {
                    string dn = Path.GetFileName(d);
                    string[] parts = dn.Split(new char[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    existing_prefixes.Add(d, parts[0]);
                    if (parts.Length >= 2)
                    {
                        existing_suffixes.Add(d, parts[1]);
                    }
                }
            }
            else
            {
                dest_folder = "";
            }

            foreach (DriveInfo drv in drivesThatMatter)
            {
                if (drv.Name[0] != dest_drive)
                {
                    if (drv.IsReady)
                    {
                        if (Directory.Exists(drv.Name[0] + srcHomeName)) // we think this is a SD card
                        {
                            string srcHomeDir = drv.Name[0] + srcHomeName;
                            string[] cardSubDirs = Directory.GetDirectories(srcHomeDir);
                            foreach (string subdir in cardSubDirs)
                            {
                                string n = Path.GetFileName(subdir);
                                if (existing_suffixes.ContainsValue(n) == false && existing_prefixes.ContainsValue(n) == false)
                                {
                                    // new folder that doesn't exist on the destination drive
                                    srcDirs.Add(new DirectoryInfo(subdir));
                                }
                                else
                                {
                                    // folder already exists on the destination drive
                                    string dtstr = now.ToString("yyMMdd");
                                    foreach (KeyValuePair<string, string> x in existing_prefixes)
                                    {
                                        // figure out the relationship of the folders between the drives
                                        if (x.Key.ToLower().EndsWith(n.ToLower()) && x.Value == dtstr)
                                        {
                                            if (Directory.Exists(x.Key) && Directory.Exists(subdir))
                                            {
                                                // found the folder pair match, count the files and see if we need to copy

                                                var srcFileList = Directory.GetFiles(subdir);//.Where(file => file.ToLower().EndsWith(".jpg") || file.ToLower().EndsWith(".arw")).ToList();
                                                var destFileList = Directory.GetFiles(x.Key);//.Where(file => file.ToLower().EndsWith(".jpg") || file.ToLower().EndsWith(".arw")).ToList();
                                                if (srcFileList.Length != destFileList.Length)
                                                {
                                                    srcDirs.Add(new DirectoryInfo(subdir));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (srcDirs.Count > 0)
            {
                return true;
            }
            return false;
        }

        private void Start(object sender, EventArgs e)
        {
            if (GetDirs())
            {
                copyworker = new BackgroundWorker();
                copyworker.DoWork += Copyworker_DoWork;
                copyworker.WorkerReportsProgress = true;
                copyworker.ProgressChanged += Copyworker_ProgressChanged;
                copyworker.RunWorkerCompleted += Copyworker_RunWorkerCompleted;
                startItem.Text = "Busy...";
                startItem.Enabled = false;
                err_cnt = 0;
                copyworker.RunWorkerAsync();
            }
            else
            {
                MessageBox.Show("Error: cannot copy photos", "Errors Occured", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Eject(object sender, EventArgs e)
        {
            if (src_drive != '\0')
            {
                try
                {
                    if (DiskUtils.Eject(src_drive + ""))
                    {
                        trayIcon.ShowBalloonTip(5000, "Eject", "Ejected \"" + src_drive + ":\\\"", ToolTipIcon.Info);
                    }
                    else
                    {
                        string errmsg = "Cannot eject \"" + src_drive + ":\\\"";
                        Logger.Log(LogLevel.Error, errmsg);
                        trayIcon.ShowBalloonTip(5000, "Cannot Eject", errmsg, ToolTipIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    string errmsg = "While ejecting \"" + src_drive + ":\\\", exception occured: " + ex.Message;
                    Logger.Log(LogLevel.Error, errmsg);
                    MessageBox.Show("Error: " + errmsg, "Errors Occured", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("No drive to eject.", "Errors Occured", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenFolder(object sender, EventArgs e)
        {
            if (Directory.Exists(dest_folder))
            {
                Process.Start("explorer.exe", dest_folder);
            }
            else
            {
                MessageBox.Show("Error: folder \"" + dest_folder + "\" does not exist.", "Errors Occured", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Copyworker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (err_cnt > 0)
            {
                MessageBox.Show("There were errors during photo file copying!" + Environment.NewLine + "Please see the log file:" + Environment.NewLine + Logger.LogFilePath, "Errors Occured", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                trayIcon.ShowBalloonTip(0, "Done!", "Finished copying photo files!", ToolTipIcon.Info);
            }
            startItem.Text = "Start";
        }

        private void Exit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            trayIcon.Visible = false;
            base.Dispose(disposing);
        }

        private delegate void SetControlPropertyThreadSafeDelegate(
            Control control,
            string propertyName,
            object propertyValue);

        public static void SetControlPropertyThreadSafe(
            Control control,
            string propertyName,
            object propertyValue)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(new SetControlPropertyThreadSafeDelegate
                (SetControlPropertyThreadSafe),
                new object[] { control, propertyName, propertyValue });
            }
            else
            {
                control.GetType().InvokeMember(
                    propertyName,
                    BindingFlags.SetProperty,
                    null,
                    control,
                    new object[] { propertyValue });
            }
        }
    }
}
