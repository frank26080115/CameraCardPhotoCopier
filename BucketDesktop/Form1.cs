using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Taskbar;
using SharpFtpServer;
using LogUtility;

namespace BucketDesktop
{
    public partial class Form1 : Form
    {
        string destPath;
        string srcPath;
        string lastCopyDir;

        bool ftpStarted = false;

        bool startCopy = false;
        bool doneAnalysis = false;
        bool threadDone = false;
        Dictionary<string, string> copyQueue = new Dictionary<string, string>();
        int copied_cnt = 0;
        long fsize_copied = 0;
        long fsize_total = 0;
        int fexist_cnt = 0;
        int hasErrors = 0;
        DateTime? signalNewDisk = null;

        private FtpServer ftpServer = null;

        System.Windows.Forms.ContextMenu menu;
        System.Windows.Forms.MenuItem quitItem;
        System.Windows.Forms.MenuItem infoItem;
        System.Windows.Forms.MenuItem ejectItem;

        public Form1()
        {
            InitializeComponent();
            notifyIcon.Icon = this.Icon;
            this.Text = Application.ProductName;
            txtDestination.Text = Program.DestPath = lastCopyDir = destPath = GetDestDir();
            if (destPath == null)
            {
                return;
            }

            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            menu = new System.Windows.Forms.ContextMenu();
            infoItem = new System.Windows.Forms.MenuItem("Info");
            infoItem.Enabled = false;
            ejectItem = new System.Windows.Forms.MenuItem("Eject");
            ejectItem.Click += EjectItem_Click;
            quitItem = new System.Windows.Forms.MenuItem("Quit");
            quitItem.Click += QuitItem_Click;
            menu.MenuItems.Add(infoItem);
            menu.MenuItems.Add(ejectItem);
            menu.MenuItems.Add(quitItem);
            notifyIcon.ContextMenu = menu;

            ReEvaluate();
        }

        void ResetAllVars()
        {
            startCopy = false;
            doneAnalysis = false;
            threadDone = false;
            copyQueue = new Dictionary<string, string>();
            copied_cnt = 0;
            fsize_copied = 0;
            fsize_total = 0;
            fexist_cnt = 0;
            hasErrors = 0;
            grpProgress.Text = "Progress";
        }

        void ReEvaluate()
        {
            ResetAllVars();

            string src = GetSourceDir();
            if (src.Length <= 0)
            {
                srcPath = null;
                txtSource.Text = "FTP";
                btnCopy.Enabled = false;
                btnEject.Enabled = false;
                chkAutoEject.Enabled = false;
                if (ftpServer == null)
                {
                    ftpServer = new FtpServer(Program.FtpPortNum);
                    ftpServer.Start();
                }
                ejectItem.Visible = false;
                infoItem.Text = "FTP Mode";
            }
            else
            {
                txtSource.Text = src;
                srcPath = src;
                btnCopy.Enabled = true;
                btnEject.Enabled = true;
                chkAutoEject.Enabled = true;
                ejectItem.Visible = true;
                infoItem.Text = "Info";
                ejectItem.Text = "Eject";
                QuitWaitBkgndThread();
                ResetAllVars();
                while (true)
                {
                    try
                    {
                        bkgndWorker.RunWorkerAsync();
                        break;
                    }
                    catch
                    {
                        try
                        {
                            bkgndWorker.CancelAsync();
                        }
                        catch { }
                        Thread.Sleep(1000);
                    }
                }
            }
        }

        private const int WM_DEVICECHANGE = 0x219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int DBT_DEVTYP_VOLUME = 0x00000002;
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            switch (m.Msg)
            {
                case WM_DEVICECHANGE:
                    switch ((int)m.WParam)
                    {
                        case DBT_DEVICEARRIVAL:
                            if ((startCopy != false && copyQueue.Count > 0 && threadDone) || (startCopy == false && doneAnalysis && copyQueue.Count <= 0) || ftpServer != null)
                            {
                                signalNewDisk = DateTime.Now;
                            }
                            break;

                        case DBT_DEVICEREMOVECOMPLETE:
                            break;

                    }
                    break;
            }
        }

        string GetDestDir()
        {
            DriveInfo[] drvNfos = DriveInfo.GetDrives();

            string destIniFile = ":" + Path.DirectorySeparatorChar + "bucket.ini";

            foreach (DriveInfo drv in drvNfos)
            {
                if (drv.IsReady == false)
                {
                    continue;
                }
                string iniFilePath = drv.Name[0] + destIniFile;
                if (File.Exists(iniFilePath))
                {
                    Program.Ini = new IniFile(iniFilePath);
                    string destDirName = Program.DestDirPrefix + DateTime.Now.Year;
                    string destHomeName = Program.HomePath;
                    string destHomeDir = drv.Name[0] + ":" + Path.DirectorySeparatorChar + destHomeName;
                    string destDir = destHomeDir + Path.DirectorySeparatorChar + destDirName;
                    return destDir;
                }
            }
            return null;
        }

        string GetSourceDir()
        {
            DriveInfo[] drvNfos = DriveInfo.GetDrives();
            string result = String.Empty;
            foreach (DriveInfo drv in drvNfos)
            {
                if (drv.IsReady == false)
                {
                    continue;
                }
                string dcimPath = drv.Name + "DCIM";
                if (result == String.Empty && Directory.Exists(dcimPath))
                {
                    result = dcimPath;
                }
                if (result.Length > 0 && Directory.Exists(dcimPath) && (Directory.Exists(drv.Name + "SONY") || Directory.Exists(drv.Name + "M4ROOT")))
                {
                    result = dcimPath;
                }
            }
            return result;
        }

        private void bkgndWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            threadDone = false;
            doneAnalysis = false;

            DateTime now = DateTime.Now;

            Dictionary<string, string> toBeCopied = new Dictionary<string, string>();

            if (Directory.Exists(Program.DestPath) == false)
            {
                Directory.CreateDirectory(Program.DestPath);
            }

            DirectoryInfo srcRoot = new DirectoryInfo(txtSource.Text);
            DirectoryInfo destRoot = new DirectoryInfo(Program.DestPath);
            var srcDirs = srcRoot.GetDirectories();
            var destDirs = destRoot.GetDirectories();
            foreach (var srcDir in srcDirs)
            {
                if (e.Cancel || bkgndWorker.CancellationPending)
                {
                    break;
                }

                var fileList = srcDir.GetFiles();
                foreach (var file in fileList)
                {
                    if (e.Cancel || bkgndWorker.CancellationPending)
                    {
                        break;
                    }

                    string justName = Path.GetFileName(file.Name);
                    string nameTail = justName.Substring(Math.Max(0, justName.Length - 9));
                    if (toBeCopied.ContainsKey(nameTail) == false)
                    {
                        toBeCopied.Add(nameTail, file.FullName);
                    }
                }

                foreach (var destDir in destDirs)
                {
                    if (e.Cancel || bkgndWorker.CancellationPending)
                    {
                        break;
                    }

                    if (destDir.Name.EndsWith(srcDir.Name))
                    {
                        var destFiles = destDir.GetFiles();
                        foreach (var df in destFiles)
                        {
                            if (e.Cancel || bkgndWorker.CancellationPending)
                            {
                                break;
                            }

                            string alreadyCopiedFilePath = destDir.FullName + Path.DirectorySeparatorChar + "already_copied.txt";
                            if (File.Exists(alreadyCopiedFilePath))
                            {
                                try
                                {
                                    using (StreamReader sr = new StreamReader(alreadyCopiedFilePath))
                                    {
                                        string srLine = sr.ReadLine();
                                        while (srLine != null)
                                        {
                                            srLine = srLine.Trim();
                                            if (toBeCopied.ContainsKey(srLine))
                                            {
                                                toBeCopied.Remove(srLine);
                                                fexist_cnt += 1;
                                            }
                                            srLine = sr.ReadLine();
                                        }
                                    }
                                }
                                catch
                                {

                                }

                            }

                            string nameTail = df.Name.Substring(Math.Max(0, df.Name.Length - 9));
                            if (toBeCopied.ContainsKey(nameTail))
                            {
                                toBeCopied.Remove(nameTail);
                                fexist_cnt += 1;
                            }
                        }
                    }
                }

                string copyDest = Program.DestPath + Path.DirectorySeparatorChar + now.ToString("yyMMdd") + "-" + Path.GetFileName(srcDir.Name);
                foreach (KeyValuePair<string, string> i in toBeCopied)
                {
                    string justName = Path.GetFileName(i.Value);
                    string justNewName = justName.Replace(Program.CardFilePrefix, Program.CardFilePrefix + now.ToString("yyMMdd"));
                    string destPath = Path.Combine(copyDest, justNewName);
                    copyQueue.Add(i.Value, destPath);
                    fsize_total += new FileInfo(i.Value).Length;
                }
                toBeCopied.Clear();
            }

            doneAnalysis = true;
            while (startCopy == false)
            {
                Thread.Sleep(100);
                if (e.Cancel || bkgndWorker.CancellationPending)
                {
                    break;
                }
            }

            bool deletePartial = false;

            foreach (KeyValuePair<string, string> i in copyQueue)
            {
                if (e.Cancel || bkgndWorker.CancellationPending || startCopy == false)
                {
                    break;
                }

                try
                {
                    string destDirPath = Path.GetDirectoryName(i.Value);
                    string alreadyCopiedFilePath = destDirPath + Path.DirectorySeparatorChar + "already_copied.txt";
                    if (Directory.Exists(destDirPath) == false)
                    {
                        Directory.CreateDirectory(destDirPath);
                        Directory.CreateDirectory(destDirPath + Path.DirectorySeparatorChar + Program.KeeperFolderName);
                    }

                    lastCopyDir = Path.GetDirectoryName(i.Value);

                    int chunk = 1024 * 1024 * 25;

                    using (FileStream inStream = File.OpenRead(i.Key))
                    {
                        using (FileStream outStream = File.OpenWrite(i.Value))
                        {
                            if (e.Cancel || bkgndWorker.CancellationPending)
                            {
                                deletePartial = true;
                                break;
                            }

                            byte[] buffer = new byte[chunk];
                            int r = 1;
                            while (r > 0 && inStream.Position < inStream.Length && deletePartial == false)
                            {
                                if (inStream.Position >= inStream.Length)
                                {
                                    break;
                                }
                                r = inStream.Read(buffer, 0, buffer.Length);
                                outStream.Write(buffer, 0, r);
                                outStream.Flush();
                                fsize_copied += r;
                            }
                        }
                    }

                    if (deletePartial == false)
                    {
                        // compare the byte contents of both files
                        // only compare a small portion to catch bad transfers while maintaining copy speed
                        using (FileStream inStream = File.OpenRead(i.Key))
                        {
                            using (FileStream outStream = File.OpenRead(i.Value))
                            {
                                byte[] buffer1 = new byte[chunk];
                                byte[] buffer2 = new byte[chunk];
                                int r1 = 1, r2 = 1;
                                while (r1 > 0 && r2 > 0 && inStream.Position < (inStream.Length / 20))
                                {
                                    r1 = inStream.Read(buffer1, 0, buffer1.Length);
                                    r2 = outStream.Read(buffer2, 0, buffer2.Length);
                                    if (Program.MemcmpCompare(buffer1, buffer2, r1) != 0 || r1 != r2)
                                    {
                                        hasErrors += 1;
                                        Logger.Log(LogLevel.Error, "During Copy \"" + i.Key + "\" to \"" + i.Value + "\", content mismatch detected");
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (deletePartial && File.Exists(i.Value))
                    {
                        File.Delete(i.Value);
                    }
                    else
                    {
                        try
                        {
                            File.AppendAllText(alreadyCopiedFilePath, Path.GetFileName(i.Key) + Environment.NewLine);
                        }
                        catch
                        {
                        }
                    }
                    copied_cnt += 1;
                }
                catch (Exception ex)
                {
                    hasErrors += 1;
                    Logger.Log(LogLevel.Error, "During Copy \"" + i.Key + "\" to \"" + i.Value + "\", exception: " + ex.Message);
                }

                Thread.Sleep(0);
            }
            threadDone = true;
            if (startCopy && hasErrors <= 0 && chkAutoEject.Checked && !(e.Cancel || bkgndWorker.CancellationPending))
            {
                Thread.Sleep(2000);
            }
        }

        private void bkgndWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            SetProgress(e.ProgressPercentage);
        }

        private void bkgndWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            threadDone = true;

            if (srcPath != null && chkAutoEject.Checked && hasErrors <= 0 && startCopy)
            {
                EjectDisk();
            }
            if (hasErrors > 0)
            {
                MessageBox.Show("Please see log file: " + Logger.LogFilePath, "Bucket Errors Encountered", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (startCopy && threadDone && copyQueue.Count > 0)
            {
                notifyIcon.ShowBalloonTip(3000, "Bucket Done", "Bucket has finished copying files", ToolTipIcon.Info);
            }
        }

        void QuitWaitBkgndThread()
        {
            if (bkgndWorker.IsBusy)
            {
                bkgndWorker.CancelAsync();
                for (int i = 0; i < 30 && bkgndWorker.IsBusy && threadDone == false; i++)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        void QuitWaitEjectThread()
        {
            if (ejectorThread.IsBusy)
            {
                ejectorThread.CancelAsync();
                for (int i = 0; i < 30 && ejectorThread.IsBusy; i++)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        void EjectDisk()
        {
            ejectItem.Enabled = false;
            btnEject.Enabled = false;
            btnCopy.Enabled = false;
            chkAutoEject.Enabled = false;
            QuitWaitBkgndThread();
            ejectorThread.RunWorkerAsync();
        }

        private void ejectorThread_DoWork(object sender, DoWorkEventArgs e)
        {
            string drive = Path.GetPathRoot(txtSource.Text).Substring(0, 1);
            while (Directory.Exists(Path.GetPathRoot(txtSource.Text)))
            {
                if (e.Cancel || ejectorThread.CancellationPending)
                {
                    return;
                }
                DiskUtils.Eject(drive);
                Thread.Sleep(500);
            }
            ejectorThread.ReportProgress(1);
        }

        private void ejectorThread_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
            {
                txtSource.Text = "Ejected";
                ejectItem.Text = "Already Ejected";
                btnEject.Enabled = false;
            }
        }

        private void SetProgress(int x)
        {
            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke((MethodInvoker)delegate
                {
                    SetProgress(x);
                });
                return;
            }
            if (x >= 0)
            {
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Maximum = 101;
                progressBar.Value = x + 1;
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal, this.Handle);
                TaskbarManager.Instance.SetProgressValue(x + 10, 110, this.Handle);
            }
            else
            {
                progressBar.Style = ProgressBarStyle.Marquee;
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Indeterminate, this.Handle);
            }
        }

        private void SetProgressText(string x)
        {
            if (grpProgress.InvokeRequired)
            {
                grpProgress.Invoke((MethodInvoker)delegate
                {
                    SetProgressText(x);
                });
            }
            else
            {
                grpProgress.Text = x;
                infoItem.Text = x;
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (signalNewDisk.HasValue)
            {
                var dt = DateTime.Now - signalNewDisk.Value;
                if (dt.TotalSeconds >= 3)
                {
                    ReEvaluate();
                    signalNewDisk = null;
                }
            }

            if (ftpStarted == false && Program.FtpFileCnt > 0)
            {
                ftpStarted = true;
                SetProgress(-1);
                notifyIcon.ShowBalloonTip(3000, "Bucket FTP", "Bucket FTP has started transfering", ToolTipIcon.Info);
            }
            else if (ftpStarted)
            {
                SetProgressText("FTP Transfered: " + Program.FtpFileCnt.ToString() + "; Size = " + Program.FormatFileSize(Program.FtpFileSize));
            }
            else if (srcPath != null && startCopy == false && doneAnalysis && copyQueue.Count <= 0)
            {
                SetProgressText("No files to copy" + (fexist_cnt > 0 ? " (" + fexist_cnt.ToString() + " existing)": ""));
                btnCopy.Enabled = false;
            }
            else if (srcPath != null && startCopy == false && copyQueue.Count > 0)
            {
                SetProgressText(copyQueue.Count.ToString() + " Files Found; Size = " + Program.FormatFileSize(fsize_total));
            }
            else if (srcPath != null && startCopy != false && copyQueue.Count > 0)
            {
                string errtxt = "";
                if (hasErrors > 0)
                {
                    errtxt = "; " + hasErrors.ToString() + " errors";
                }
                if (threadDone == false)
                {
                    SetProgressText("Copied " + copied_cnt.ToString() + " / " + copyQueue.Count.ToString() + " Files; " + Program.FormatFileSize(fsize_copied, fsize_total) + errtxt);
                    int percentage = Convert.ToInt32(Math.Round(Convert.ToDouble(fsize_copied) * 100.0d / Convert.ToDouble(fsize_total)));
                    SetProgress(percentage);
                }
                else
                {
                    SetProgressText("Done copying " + copied_cnt.ToString() + " / " + copyQueue.Count.ToString() + " files; " + Program.FormatFileSize(fsize_copied, fsize_total) + errtxt);
                    int percentage = Convert.ToInt32(Math.Round(Convert.ToDouble(fsize_copied) * 100.0d / Convert.ToDouble(fsize_total)));
                    SetProgress(percentage);
                    btnEject.Enabled = true;
                }
            }
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
        }

        private void EjectItem_Click(object sender, EventArgs e)
        {
            EjectDisk();
        }

        private void QuitItem_Click(object sender, EventArgs e)
        {
            QuitWaitBkgndThread();
            if (ftpServer != null)
            {
                ftpServer.Stop();
            }
            notifyIcon.Visible = false;
            QuitWaitEjectThread();
            this.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            if (destPath == null)
            {
                MessageBox.Show("No Destination Disk", "Bucket Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                notifyIcon.Visible = true;
                this.Hide();
            }
            else
            {
                notifyIcon.Visible = false;
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            notifyIcon.Visible = false;
            if (ftpServer != null)
            {
                ftpServer.Stop();
            }
            if (bkgndWorker.IsBusy)
            {
                bkgndWorker.CancelAsync();
            }
        }

        private void btnEject_Click(object sender, EventArgs e)
        {
            EjectDisk();
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            btnCopy.Enabled = false;
            btnEject.Enabled = false;
            if (srcPath != null)
            {
                SetProgress(0);
            }
            else
            {
                SetProgress(-1);
            }
            startCopy = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            notifyIcon.Visible = false;
            QuitWaitBkgndThread();
            QuitWaitEjectThread();
        }

        private void txtDestination_DoubleClick(object sender, EventArgs e)
        {
            if (lastCopyDir != null)
            {
                if (Directory.Exists(lastCopyDir))
                {
                    Process fileopener = new Process();
                    fileopener.StartInfo.FileName = "explorer";
                    fileopener.StartInfo.Arguments = "\"" + lastCopyDir + "\"";
                    fileopener.Start();
                }
            }
        }

        private void txtDestination_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //txtDestination_DoubleClick(sender, e);
        }
    }
}
