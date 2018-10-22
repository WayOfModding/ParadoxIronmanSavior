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
    public class Game
    {
        private Regex rgxSave;
        private Regex rgxBack;
        private Regex rgxCasl;

        private string sGameName;
        private string sFileExtensionName;
        private string sURI;
        private string sProcessName;
        private string sPathSave;
        private string sPathBack;
        private string sPathRecy;
        //
        private Dictionary<string, BackupPool> pools = new Dictionary<string, BackupPool>();
        //
        private SaveFile selectedFile;
        //
        private volatile bool isWatcherActivated;

        public Game(string sGameName, string sFileExtensionName, string sURI, string sProcessName)
        {
            System.Diagnostics.Debug.Assert(!String.IsNullOrWhiteSpace(sGameName));
            System.Diagnostics.Debug.Assert(!String.IsNullOrWhiteSpace(sFileExtensionName));
            System.Diagnostics.Debug.Assert(!String.IsNullOrWhiteSpace(sURI));
            System.Diagnostics.Debug.Assert(!String.IsNullOrWhiteSpace(sProcessName));

            const string patternSave = @"^(?<sname>\w+)$";
            const string patternBack = @"^(?<sname>\w+) \((?<version>\d+)\)$";
            const string patternCasl = @"\d{4}_\d{2}_\d{2}";
            this.rgxSave = new Regex(patternSave, RegexOptions.Compiled | RegexOptions.ECMAScript);
            this.rgxBack = new Regex(patternBack, RegexOptions.Compiled | RegexOptions.ECMAScript);
            this.rgxCasl = new Regex(patternCasl, RegexOptions.Compiled | RegexOptions.ECMAScript);

            this.sGameName = sGameName;
            this.sFileExtensionName = sFileExtensionName;
            this.sURI = sURI;
            this.sProcessName = sProcessName;

            // Absolute path of `save games/`
            string sPersonalFolder = System.Environment.GetFolderPath(
                System.Environment.SpecialFolder.Personal);
            this.sPathSave = System.IO.Path.Combine(
                sPersonalFolder,
                "Paradox Interactive",
                sGameName,
                "save games");
            System.Diagnostics.Debug.Assert(System.IO.Directory.Exists(this.sPathSave),
                String.Format("Directory '{0}' does not exist!", this.sPathSave));
            // Absolute path of `save games/backup/`
            this.sPathBack = System.IO.Path.Combine(
                this.sPathSave,
                "backup");
            // Create `save games/backup/` folder if it does not exist
            if (!System.IO.File.Exists(this.sPathBack))
            {
                System.IO.Directory.CreateDirectory(this.sPathBack);
            }
            System.Diagnostics.Debug.Assert(System.IO.Directory.Exists(this.sPathBack),
                String.Format("Directory '{0}' does not exist!", this.sPathBack));
            // Absolute path of `save games/recycle/`
            this.sPathRecy = System.IO.Path.Combine(
                this.sPathSave,
                "recycle");

            this.SelectedFile = null;
            this.isWatcherActivated = false;
        }

        public bool isIronMode(string path)
        {
            string sFileName = System.IO.Path.GetFileNameWithoutExtension(path);
            string sExtension = System.IO.Path.GetExtension(path);
            sFileName = sFileName.ToLower();
            sExtension = sExtension.ToLower();

            System.Diagnostics.Debug.Assert(!String.IsNullOrWhiteSpace(sFileName));
            System.Diagnostics.Debug.Assert(!String.IsNullOrWhiteSpace(sExtension));

            if (!this.sFileExtensionName.Equals(sExtension))
                return false;
            if (sFileName.Contains("autosave"))
                return false;
            if (sFileName.Contains("backup"))
                return false;
            // rule out files whose names contains date as "YYYY_MM_DD"
            Match match = rgxCasl.Match(sFileName);
            if (match.Success)
                return false;
            return true;
        }

        public string GameName
        {
            get { return this.sGameName; }
        }

        public string FileExtensionName
        {
            get { return this.sFileExtensionName; }
        }

        public string URI
        {
            get { return this.sURI; }
        }

        public string ProcessName
        {
            get { return this.sProcessName; }
        }

        public string PathSave
        {
            get { return this.sPathSave; }
        }

        public string PathBack
        {
            get { return this.sPathBack; }
        }

        public string PathRecy
        {
            get { return this.sPathRecy; }
        }

        public void initBackupPool(string sSaveName)
        {
            this.pools[sSaveName] = new BackupPool();
        }

        public bool addSaveFile(SaveFile saveFile)
        {
            string sSaveName = saveFile.SaveName;
            BackupPool pool = this.pools[sSaveName];
            bool result = pool.add(saveFile);
            if (result)
            {
                // link
                saveFile.Last = this.SelectedFile;
                if (this.SelectedFile != null)
                    this.SelectedFile.Next = saveFile;
            }
            return result;
        }

        public bool removeSaveFile(SaveFile saveFile)
        {
            string sSaveName = saveFile.SaveName;
            BackupPool pool = this.pools[sSaveName];
            bool result = pool.del(saveFile);
            if (result)
            {
                // unlink
                SaveFile lastFile = saveFile.Last;
                SaveFile nextFile = saveFile.Next;
                if (lastFile != null)
                    lastFile.Next = nextFile;
                if (nextFile != null)
                    nextFile.Last = lastFile;
            }
            return result;
        }

        public void clearSaveFiles(string sSaveName)
        {
            BackupPool pool = this.pools[sSaveName];
            pool.clear();
        }

        public void pushSaveFile(SaveFile saveFile)
        {
            string sSaveName = saveFile.SaveName;
            this.pushSaveFile(sSaveName);
        }

        public void pushSaveFile(string sSaveName)
        {
            // calculate `iVersion`
            BackupPool pool = this.pools[sSaveName];
            int iVersion = pool.getMaxVersion();
            // create object `saveFile`
            SaveFile saveFile = new SaveFile(this, sSaveName, iVersion);
            System.Diagnostics.Debug.WriteLine(String.Format(
                @"Select `pushSaveFile({0})` ...",
                saveFile));
            // add `saveFile`
            if (this.addSaveFile(saveFile))
                // select `saveFile`
                this.SelectedFile = saveFile;
        }

        public void popSaveFile(string sSaveName)
        {
            SaveFile saveFile = this.SelectedFile;
            SaveFile lastFile = saveFile.Last;
            // remove `saveFile`
            if (this.removeSaveFile(saveFile))
                // select `lastSaveFile`
                this.SelectedFile = lastFile;
        }

        public void updateUI_save(ComboBox comboBox)
        {
            comboBox.Items.Clear();

            ICollection<string> keys = this.pools.Keys;
            if (keys.Count > 0)
            {
                // TODO sort save files by last modification time
                SortedList<DateTime, string> sldts = new SortedList<DateTime, string>(BackupPool.dateTimeComparer);
                string[] range = new string[keys.Count];
                foreach (string sSaveName in keys)
                {
                    string path = System.IO.Path.Combine(sPathSave,
                        String.Format("{0}{1}",
                            sSaveName, sFileExtensionName));
                    DateTime dateTime = System.IO.File.GetLastWriteTimeUtc(path);
                    sldts[dateTime] = sSaveName;
                }
                IList<SaveFile> list = sldts.Values;
                list.CopyTo(range, 0);
                comboBox.Items.AddRange(range);
                comboBox.SelectedIndex = 0;
            }
        }

        public void updateUI_version(string sSaveName, ComboBox comboBox)
        {
            comboBox.Items.Clear();

            BackupPool pool = this.pools[sSaveName];
            IList<SaveFile> list = pool.Values;
            int count = pool.Count;
            if (count > 0)
            {
                object[] range = new object[count];
                for (int i = 0; i < count; i++)
                {
                    range[i] = list[i].Version;
                }
                comboBox.Items.AddRange(range);
                comboBox.SelectedIndex = 0;
                // select save file
                SaveFile saveFile = list[0];
                this.SelectedFile = saveFile;
            }
        }

        public SaveFile SelectedFile
        {
            get { return this.selectedFile; }
            set
            {
                System.Diagnostics.Debug.WriteLine(String.Format(
                    @"Update `SelectedFile = {0}`",
                    value));
                this.selectedFile = value;
            }
        }

        public bool ContainsSave(string sSaveName)
        {
            Dictionary<string, BackupPool> pools = this.pools;
            bool result = pools.ContainsKey(sSaveName);
            return result;
        }

        private bool isBackupFor(string sPath, string sSaveName)
        {
            string sFileName = System.IO.Path.GetFileNameWithoutExtension(sPath);
            Match match = rgxBack.Match(sFileName);
            return match.Success && sSaveName.Equals(sFileName);
        }

        public void scanDirSave()
        {
            string sPathSave = this.PathSave;

            // get the list of all files in `save games/` folder
            string[] actives = System.IO.Directory.GetFiles(sPathSave);
            // traverse all files in `save games/` folder
            foreach (string active in actives)
            {
                System.Diagnostics.Debug.WriteLine(String.Format(
                    @"Function `scanDirSave` handling file (sPath={0}) ...",
                    active));

                if (!this.isIronMode(active))
                    continue;
                string sFileName = System.IO.Path.GetFileNameWithoutExtension(active);
                // create a dictionary of file name mapping to a list of file names
                if (this.ContainsSave(sFileName))
                    continue;
                this.initBackupPool(sFileName);
            }
        }

        public void scanDirBack()
        {
            // get extension name of game save file
            string sPathBack = this.PathBack;

            // get the list of all files in `save games/` folder
            string[] backups = System.IO.Directory.GetFiles(sPathBack);
            // categorize
            foreach (string backup in backups)
            {
                System.Diagnostics.Debug.WriteLine(String.Format(
                    @"Function `scanDirBack` handling file (sPath={0}) ...",
                    backup));

                if (!this.isIronMode(backup))
                    continue;
                string sFileName = System.IO.Path.GetFileNameWithoutExtension(backup);
                Match match = rgxBack.Match(sFileName);

                if (!match.Success)
                    continue;

                string sSaveName = match.Groups["sname"].Value;
                string sVersion = match.Groups["version"].Value;
                int iVersion = int.Parse(sVersion);

                // add file name to the list
                if (!this.ContainsSave(sSaveName))
                {
                    this.initBackupPool(sSaveName);
                }
                // add file
                SaveFile saveFile = new SaveFile(this, sSaveName, iVersion);
                this.addSaveFile(saveFile);
                // delete file
                //System.IO.File.Delete(backup);
            }
        }

        private void onChangeDirSave(object source, System.IO.FileSystemEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("onChangeDirSave(source={0}, args={1});", source, args);
        }

        private void onChangeDirBack(object source, System.IO.FileSystemEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("onChangeDirBack(source={0}, args={1});", source, args);
        }

        private System.IO.FileSystemWatcher createFileSystemWatcher(
            string sPath, string sExtensionName,
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

            return watcher;
        }

        public void activateWatcher()
        {
            if (this.isWatcherActivated)
                return;

            string sPathSave = this.PathSave;
            string sPathBack = this.PathBack;
            // get extension name of game save file
            string sGameSaveExtensionName = this.FileExtensionName;

            // watch `save games/` folder for any change
            this.createFileSystemWatcher(sPathSave, sGameSaveExtensionName, this.onChangeDirSave);
            // watch `save games/backup/` folder for any change
            this.createFileSystemWatcher(sPathBack, sGameSaveExtensionName, this.onChangeDirBack);

            this.isWatcherActivated = true;
        }
    }
}
