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

        private Regex rgxSave;
        private Regex rgxBack;

        private PSUStatus status;

        private Dictionary<string, Game> games = new Dictionary<string, Game>();
        private Game selectedGame;

        public Form1()
        {
            const string patternSave = @"^(?<sname>\w+)\.(?:eu4|ck2|hoi4)$";
            const string patternBack = @"^(?<fname>(?<sname>\w+) \((?<version>\d+)\))\.(?:eu4|ck2|hoi4)$";
            this.rgxSave = new Regex(patternSave, RegexOptions.Compiled | RegexOptions.ECMAScript);
            this.rgxBack = new Regex(patternBack, RegexOptions.Compiled | RegexOptions.ECMAScript);

            this.games["Europa Universalis IV"] = new Game(
                "Europa Universalis IV", ".eu4", "steam://rungameid/236850", "eu4");
            this.games["Crusader Kings II"] = new Game(
                "Crusader Kings II", ".ck2", "steam://rungameid/203770", "CK2game");
            this.selectedGame = null;

            // UI initialization
            InitializeComponent();
            LoadResources();

            if (this.comboBox1.Items.Count > 0)
                this.comboBox1.SelectedIndex = 0;
        }

        private void createFileSystemWatcher(string sPath, string sExtensionName,
            Action<object, System.IO.FileSystemEventArgs> onChange)
        {
            System.IO.FileSystemWatcher watcher = new System.IO.FileSystemWatcher();

            watcher.Path = sPath;
            watcher.NotifyFilter = System.IO.NotifyFilters.LastAccess
                | System.IO.NotifyFilters.LastWrite
                | System.IO.NotifyFilters.FileName
                | System.IO.NotifyFilters.DirectoryName;
            watcher.Filter = "*" + sExtensionName;
            watcher.Changed += new System.IO.FileSystemEventHandler(onChange);
            watcher.Created += new System.IO.FileSystemEventHandler(onChange);
            watcher.Deleted += new System.IO.FileSystemEventHandler(onChange);

            watcher.EnableRaisingEvents = true;
        }

        private void onChangeDirSave(object source, System.IO.FileSystemEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("onChangeDirSave(source={0}, args={1});", source, args);
            this.scanDirSave();
        }

        private void onChangeDirBack(object source, System.IO.FileSystemEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("onChangeDirBack(source={0}, args={1});", source, args);
            this.scanDirBack();
        }

        // Push
        private void button1_Click(object sender, EventArgs e)
        {
            this.setStatus(PSUStatus.RUNNING);

            /////////////////

            Game game = this.selectedGame;
            SaveFile saveFile = game.SelectedFile;
            // get save name
            string sSaveName = saveFile.SaveName;
            // get version number
            int iVersion = saveFile.Version;

            // handle file
            handleFiles(SaveMode.PUSH);

            // add save file into the table
            game.pushSaveFile(sSaveName);

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

            Game game = this.selectedGame;
            SaveFile saveFile = game.SelectedFile;
            if (saveFile == null)
            {
                // TODO
                return;
            }
            // get save name
            string sSaveName = saveFile.SaveName;
            // get version number
            int iVersion = saveFile.Version;

            // handle file
            handleFiles(SaveMode.POP);

            // remove save file
            game.popSaveFile(sSaveName);

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

            Game game = this.selectedGame;
            string sRecycle = game.PathRecy;

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

            Game game = this.selectedGame;
            SaveFile saveFile = game.SelectedFile;
            string sSaveName = saveFile.SaveName;

            if (String.IsNullOrWhiteSpace(sSaveName))
            {
                this.setStatus(PSUStatus.FAILURE);
                return;
            }

            string sPathBack = game.PathBack;
            string[] backups = System.IO.Directory.GetFiles(sPathBack);

            foreach (string backup in backups)
            {
                string fileName = System.IO.Path.GetFileName(backup);
                Match match = rgxBack.Match(fileName);

                if (!match.Success)
                    continue;
                // get save name
                string sMatchSaveName = match.Groups["sname"].Value;
                // filter save names
                if (!sSaveName.Equals(sMatchSaveName))
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
            game.clearSaveFiles(sSaveName);

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
            Game game = this.selectedGame;
            SaveFile saveFile = game.SelectedFile;
            string sGameName = game.GameName;
            string sSaveName = saveFile.SaveName;
            int iVersion = saveFile.Version + iVersionOffset;

            string sPathSave = game.PathSave;
            string sPathBack = game.PathBack;
            string sExtensionName = game.FileExtensionName;
            string src = System.IO.Path.Combine(sPathBack,
                String.Format("{0} ({1}){2}",
                    sSaveName, iVersion, sExtensionName));
            string dst = System.IO.Path.Combine(sPathSave,
                String.Format("{0}{1}",
                    sSaveName, sExtensionName));
            string bkp = System.IO.Path.Combine(sPathSave,
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

        private void scanDirSave()
        {
            Game game = this.selectedGame;
            // get extension name of game save file
            string sGameSaveExtensionName = game.FileExtensionName;
            string sPathSave = game.PathSave;

            // get the list of all files in `save games/` folder
            string[] actives = System.IO.Directory.GetFiles(sPathSave);
            // traverse all files in `save games/` folder
            foreach (string active in actives)
            {
                if (active.Contains("_Backup"))
                    continue;
                // get extension name of the file
                string sExtensionName = System.IO.Path.GetExtension(active);
                // check if the extension name of this file equals
                // one that a game save file should have
                if (!sGameSaveExtensionName.Equals(sExtensionName))
                {
                    // skip this file which has invalid extension name
                    continue;
                }
                // get file name of the file
                string sFullFileName = System.IO.Path.GetFileName(active);
                Match match = this.rgxSave.Match(sFullFileName);
                if (!match.Success)
                    continue;
                string sSaveName = System.IO.Path.GetFileNameWithoutExtension(active);
                // create a dictionary of file name mapping to a list of file names
                if (game.ContainsSave(sSaveName))
                    continue;
                game.initBackupPool(sSaveName);
            }
        }

        private void scanDirBack()
        {
            Game game = this.selectedGame;
            // get extension name of game save file
            string sPathBack = game.PathBack;

            // get the list of all files in `save games/` folder
            string[] backups = System.IO.Directory.GetFiles(sPathBack);
            // categorize
            foreach (string backup in backups)
            {
                string fileName = System.IO.Path.GetFileName(backup);
                Match match = rgxBack.Match(fileName);

                if (!match.Success)
                    continue;

                string sSaveName = match.Groups["sname"].Value;
                string sFileName = match.Groups["fname"].Value;
                string sVersion = match.Groups["version"].Value;
                int iVersion = int.Parse(sVersion);

                // add file name to the list
                if (!game.ContainsSave(sSaveName))
                {
                    game.initBackupPool(sSaveName);
                }
                SaveFile saveFile = new SaveFile(game, sSaveName, iVersion);
                // add file
                if (game.addSaveFile(saveFile))
                {
                    continue;
                }
                // delete file
                System.IO.File.Delete(backup);
            }
        }

        private void onSelectGame()
        {
            string sGameName = this.getGameName();
            Game game = games[sGameName];
            this.selectedGame = game;
            System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sGameName));
            // get extension name of game save file
            string sGameSaveExtensionName = game.FileExtensionName;
            System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sGameSaveExtensionName));
            string sPathSave = game.PathSave;
            string sPathBack = game.PathBack;
            // watch `save games/` folder for any change
            this.createFileSystemWatcher(sPathSave, sGameSaveExtensionName, this.onChangeDirSave);
            // watch `save games/backup/` folder for any change
            this.createFileSystemWatcher(sPathBack, sGameSaveExtensionName, this.onChangeDirBack);

            this.scanDirSave();            

            this.scanDirBack();

            // sort lists
            game.updateUI_save(this.comboBox2);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            onSelectGame();
        }

        private void onSelectSave()
        {
            // update `comboBox3`(Select Version)
            Game game = this.selectedGame;
            string sSaveName = this.getSaveName();
            game.updateUI_version(sSaveName, this.comboBox3);
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
            Game game = this.selectedGame;
            string sGameName = game.GameName;
            string processName = game.ProcessName;
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
            Game game = this.selectedGame;
            string sGameName = game.GameName;
            string uri = game.URI;

            System.Diagnostics.Process.Start(uri);
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
