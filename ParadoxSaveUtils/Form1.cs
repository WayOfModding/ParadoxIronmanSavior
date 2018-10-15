using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace ParadoxSaveUtils
{
    public partial class Form1 : Form
    {
        enum SaveMode
        {
            PUSH,
            POP,
            PEEK
        }

        enum PSUStatus
        {
            RUNNING,
            FAILURE,
            SUCCESS
        }

        private Regex rgxBack;

        /**
         * Mapping between 'Save Name' and a list of 'Version' of each save
         */
        private Dictionary<string, List<int>> dSaves = new Dictionary<string, List<int>>();
        /**
         * Mapping between 'Game Name' and 'File Extension Name'
         */
        private Dictionary<string, string> dExtensions = new Dictionary<string, string>();
        /**
         * Mapping between 'Game Name' and 'Game URL'
         */
        private Dictionary<string, string> dUrls = new Dictionary<string, string>();
        /**
         * Mapping between 'Game Name' and 'Process Name'
         */
        private Dictionary<string, string> dProcesses = new Dictionary<string, string>();
        private string sPathSave;
        private string sPathBack;
        private PSUStatus status;

        public Form1()
        {
            const string patternBack = @"^(?<name>\w+) \((?<version>\d+)\)\.(?:eu4|ck2|hoi4)$";
            this.rgxBack = new Regex(patternBack, RegexOptions.Compiled | RegexOptions.ECMAScript);
            this.sPathSave = null;
            this.sPathBack = null;

            this.dExtensions["Europa Universalis IV"] = ".eu4";
            this.dExtensions["Crusader Kings II"] = ".ck2";

            this.dUrls["Europa Universalis IV"] = "steam://rungameid/236850";
            this.dUrls["Crusader Kings II"] = "steam://rungameid/203770";

            this.dProcesses["Europa Universalis IV"] = "eu4";
            this.dProcesses["Crusader Kings II"] = "CK2game";

            // UI initialization
            InitializeComponent();
            LoadResources();

            if (this.comboBox1.Items.Count > 0)
                this.comboBox1.SelectedIndex = 0;
        }

        private void createFileSystemWatcher(string sPath, string sExtensionName)
        {
            System.IO.FileSystemWatcher watcher = new System.IO.FileSystemWatcher();

            watcher.Path = sPath;
            watcher.NotifyFilter = System.IO.NotifyFilters.LastAccess
                | System.IO.NotifyFilters.LastWrite
                | System.IO.NotifyFilters.FileName
                | System.IO.NotifyFilters.DirectoryName;
            watcher.Filter = "*" + sExtensionName;
            watcher.Changed += new System.IO.FileSystemEventHandler(this.onChange);
            watcher.Created += new System.IO.FileSystemEventHandler(this.onChange);
            watcher.Deleted += new System.IO.FileSystemEventHandler(this.onChange);

            watcher.EnableRaisingEvents = true;
        }

        private void onChange(object source, System.IO.FileSystemEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("onChange(source={0}, args={1});", source, args);
        }

        // Push
        private void button1_Click(object sender, EventArgs e)
        {
            this.setStatus(PSUStatus.RUNNING);

            /////////////////

            string sSaveName = this.getSaveName();
            int iVersion = this.getVersioni();

            // handle file
            handleFiles(SaveMode.PUSH);

            // handle field `dSaves`
            List<int> versions = this.dSaves[sSaveName];
            versions.Insert(0, ++iVersion);

            // adjust UI
            this.comboBox3.Items.Insert(0, iVersion);
            this.comboBox3.SelectedItem = iVersion;

            /////////////////

            this.setStatus(PSUStatus.SUCCESS);
        }

        // Pop
        private void button2_Click(object sender, EventArgs e)
        {
            this.setStatus(PSUStatus.RUNNING);

            /////////////////

            string sSaveName = this.getSaveName();
            int iVersion = this.getVersioni();

            // handle file
            handleFiles(SaveMode.POP);

            // handle field `dSaves`
            List<int> versions = this.dSaves[sSaveName];
            versions.Remove(iVersion);

            // adjust UI
            this.comboBox3.Items.Remove(iVersion);
            --iVersion;
            this.comboBox3.SelectedItem = iVersion;

            /////////////////

            this.setStatus(PSUStatus.SUCCESS);
        }

        // Peek
        private void button3_Click(object sender, EventArgs e)
        {
            this.setStatus(PSUStatus.RUNNING);

            /////////////////

            handleFiles(SaveMode.PEEK);

            /////////////////

            this.setStatus(PSUStatus.SUCCESS);
        }

        // Restart Game
        private void button4_Click(object sender, EventArgs e)
        {
            this.setStatus(PSUStatus.RUNNING);

            /////////////////

            this.killGame();
            this.bootGame();

            /////////////////

            this.setStatus(PSUStatus.SUCCESS);
        }

        private void recyle(string path)
        {
            if (String.IsNullOrWhiteSpace(path)
                || !System.IO.File.Exists(path))
                return;

            string sRecycle = System.IO.Path.Combine(
                this.sPathSave,
                "recycle");

            if (System.IO.Directory.Exists(sRecycle))
                System.IO.Directory.Delete(sRecycle, true);
            System.IO.Directory.CreateDirectory(sRecycle);

            string sNewPath = System.IO.Path.Combine(
                sRecycle,
                System.IO.Path.GetFileName(path));

            if (System.IO.File.Exists(sNewPath))
                System.IO.File.Delete(sNewPath);

            System.IO.File.Move(path, sNewPath);
        }

        private void clean()
        {
            this.setStatus(PSUStatus.RUNNING);

            /////////////////

            string sSaveName = this.getSaveName();

            if (String.IsNullOrWhiteSpace(sSaveName))
            {
                this.setStatus(PSUStatus.FAILURE);
                return;
            }

            string[] backups = System.IO.Directory.GetFiles(this.sPathBack);

            foreach (string backup in backups)
            {
                string fileName = System.IO.Path.GetFileName(backup);
                Match match = rgxBack.Match(fileName);

                if (!match.Success)
                    continue;

                string name = match.Groups["name"].Value;

                if (!sSaveName.Equals(name))
                    continue;

                // remove all the matching files
                try
                {
                    //System.IO.File.Delete(backup);
                    this.recyle(backup);
                }
                catch (Exception)
                {
                    this.setStatus(PSUStatus.FAILURE);
                    throw;
                }
            }

            // clean up field `dSaves`
            List<int> list = this.dSaves[sSaveName];
            list.Clear();

            // clean up UI
            this.comboBox3.Items.Clear();

            /////////////////

            this.setStatus(PSUStatus.SUCCESS);
        }

        // Clean
        private void button5_Click(object sender, EventArgs e)
        {
            this.clean();
        }

        private void handleFiles(SaveMode mode)
        {
            if (mode == SaveMode.PUSH)
            {
                Tuple<string, string, string> tuple = this.getFiles(1);

                string dst = tuple.Item1;
                string src = tuple.Item2;

                if (System.IO.File.Exists(src))
                {
                    if (System.IO.File.Exists(dst))
                        System.IO.File.Delete(dst);

                    System.IO.File.Copy(src, dst);
                }
                else
                {
                    this.setStatus(PSUStatus.FAILURE);
                }
            }
            else
            {
                Tuple<string, string, string> tuple = getFiles();

                string src = tuple.Item1;
                string dst = tuple.Item2;
                string bkp = tuple.Item3;

                if (System.IO.File.Exists(bkp))
                    System.IO.File.Delete(bkp);

                if (System.IO.File.Exists(src))
                {
                    if (System.IO.File.Exists(dst))
                        System.IO.File.Delete(dst);

                    switch (mode)
                    {
                        case SaveMode.POP:
                            System.IO.File.Move(src, dst);
                            break;
                        case SaveMode.PEEK:
                            System.IO.File.Copy(src, dst);
                            break;
                        default:
                            this.setStatus(PSUStatus.FAILURE);
                            break;
                    }
                }
                else
                {
                    this.setStatus(PSUStatus.FAILURE);
                }
            }
        }

        private Tuple<string, string, string> getFiles(int iVersionOffset = 0)
        {
            string sGameName = this.getGameName();
            string sSaveName = this.getSaveName();
            int iVersion = this.getVersioni() + iVersionOffset;

            string sExtensionName = this.dExtensions[sGameName];
            string src = System.IO.Path.Combine(this.sPathBack,
                String.Format("{0} ({1}){2}",
                    sSaveName, iVersion, sExtensionName));
            string dst = System.IO.Path.Combine(this.sPathSave,
                String.Format("{0}{1}",
                    sSaveName, sExtensionName));
            string bkp = System.IO.Path.Combine(this.sPathSave,
                String.Format("{0}_Backup{1}",
                    sSaveName, sExtensionName));

            return new Tuple<string, string, string>(src, dst, bkp);
        }

        private string getGameName()
        {
            return this.comboBox1.SelectedItem.ToString();
        }

        private string getSaveName()
        {
            return this.comboBox2.SelectedItem.ToString();
        }

        private int getVersioni()
        {
            try
            {
                return (int)this.comboBox3.SelectedItem;
            }
            catch (System.NullReferenceException)
            {
                return 1;
            }
        }

        private string getVersions()
        {
            return this.comboBox3.SelectedItem.ToString();
        }

        private static string getPathSave(string sGameName)
        {
            string result = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                "Paradox Interactive",
                sGameName,
                "save games");
            System.Diagnostics.Debug.Assert(System.IO.Directory.Exists(result));
            return result;
        }

        private string getPathBack()
        {
            string result = System.IO.Path.Combine(
                this.sPathSave,
                "backup");
            if (!System.IO.File.Exists(result))
            {
                System.IO.Directory.CreateDirectory(result);
            }
            return result;
        }

        private void onSelectGame()
        {
            string sGameName = getGameName();
            string sGameSaveExtensionName = this.dExtensions[sGameName];
            this.sPathSave = Form1.getPathSave(sGameName);
            this.sPathBack = this.getPathBack();

            this.createFileSystemWatcher(this.sPathSave, sGameSaveExtensionName);

            string[] actives = System.IO.Directory.GetFiles(this.sPathSave);

            foreach (string active in actives)
            {
                string sExtensionName = System.IO.Path.GetExtension(active);
                if (!sGameSaveExtensionName.Equals(sExtensionName))
                {
                    continue;
                }
                string sFileName = System.IO.Path.GetFileNameWithoutExtension(active);

                List<int> list = null;

                try
                {
                    list = this.dSaves[sFileName];
                }
                catch (System.Collections.Generic.KeyNotFoundException)
                {
                    list = new List<int>();
                    this.dSaves[sFileName] = list;
                }
                finally
                {
                    System.Diagnostics.Debug.Assert(list != null);
                }
            }

            string[] backups = System.IO.Directory.GetFiles(this.sPathBack);

            foreach (string backup in backups)
            {
                string fileName = System.IO.Path.GetFileName(backup);
                Match match = rgxBack.Match(fileName);

                if (!match.Success)
                    continue;

                string sName = match.Groups["name"].Value;
                string sVersion = match.Groups["version"].Value;

                List<int> list = null;

                try
                {
                    list = this.dSaves[sName];
                }
                catch (System.Collections.Generic.KeyNotFoundException)
                {
                    list = new List<int>();
                    this.dSaves[sName] = list;
                }
                finally
                {
                    System.Diagnostics.Debug.Assert(list != null);
                    list.Add(int.Parse(sVersion));
                }
            }

            // sort lists
            foreach (KeyValuePair<string, List<int>> kvp in this.dSaves)
            {
                kvp.Value.Sort((int x, int y) => y - x);
            }

            ICollection<string> keys = this.dSaves.Keys;
            if (keys.Count > 0)
            {
                string[] range = new string[keys.Count];
                keys.CopyTo(range, 0);
                this.comboBox2.Items.AddRange(range);
                this.comboBox2.SelectedIndex = 0;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            onSelectGame();
        }

        private void onSelectSave()
        {
            string sSaveName = this.getSaveName();
            List<int> list = this.dSaves[sSaveName];
            if (list.Count > 0)
            {
                object[] range = new object[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    range[i] = list[i];
                }
                this.comboBox3.Items.AddRange(range);
                this.comboBox3.SelectedIndex = 0;
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            onSelectSave();
        }

        // @see https://stackoverflow.com/questions/5901679/kill-process-tree-programmatically-in-c-sharp
        private static void KillProcessTree(int pid)
        {
            if (pid == 0) return;

            string query = string.Format(
                    @"Select *
                    From Win32_Process
                    Where ParentProcessID={0}",
                    pid);
            System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher(query);
            System.Management.ManagementObjectCollection moc = searcher.Get();
            foreach (System.Management.ManagementObject mo in moc)
            {
                int pidChild = Convert.ToInt32(mo["ProcessID"]);
                KillProcessTree(pidChild);
            }
            try
            {
                System.Diagnostics.Process proc = System.Diagnostics.Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }

        private void killGame()
        {
            string sGameName = this.getGameName();
            string processName = this.dProcesses[sGameName];
            System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcessesByName(processName);
            int nProcess = processes.Count();
            if (nProcess == 1)
                foreach (System.Diagnostics.Process process in processes)
                {
                    int pid = process.Id;
                    System.Diagnostics.Debug.WriteLine(string.Format("Kill Game(pid={0}) ...", pid));
                    KillProcessTree(pid);
                    //process.CloseMainWindow();
                }
            else if (nProcess == 0)
                return;
            else
                throw new Exception("Too many processes!");
        }

        private void bootGame()
        {
            string sGameName = this.getGameName();
            string url = this.dUrls[sGameName];

            System.Diagnostics.Process.Start(url);
        }

        private bool setStatus(PSUStatus status)
        {
            if (PSUStatus.SUCCESS.Equals(this.status) && PSUStatus.FAILURE.Equals(status)
                || PSUStatus.FAILURE.Equals(this.status) && PSUStatus.SUCCESS.Equals(status))
            {
                // invalid status transition
                return false;
            }
            this.status = status;
            switch (status)
            {
                case PSUStatus.RUNNING:
                    this.label6.Image = global::ParadoxSaveUtils.Properties.icon.led_yellow;
                    break;
                case PSUStatus.FAILURE:
                    this.label6.Image = global::ParadoxSaveUtils.Properties.icon.led_red;
                    break;
                case PSUStatus.SUCCESS:
                    this.label6.Image = global::ParadoxSaveUtils.Properties.icon.led_green;
                    break;
            }
            return true;
        }
    }
}
