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
        private static readonly Dictionary<string, Game> games = new Dictionary<string, Game>();

        private static void defineGame(string sGameName, string sFileExtensionName, string sURI, string sProcessName)
        {
            System.Diagnostics.Debug.Assert(sGameName != null);
            System.Diagnostics.Debug.Assert(sFileExtensionName != null);
            System.Diagnostics.Debug.Assert(sURI != null);
            System.Diagnostics.Debug.Assert(sProcessName != null);

            Game game = new Game(sGameName, sFileExtensionName, sURI, sProcessName);
            string sPathSave = game.PathSave;
            if (!System.IO.Directory.Exists(sPathSave))
            {
                System.Diagnostics.Debug.WriteLine(
                    String.Format(
                        @"Fail to locate the path of save files for game '{0}'({1})!",
                        sGameName,
                        sPathSave));
                return;
            }

            games[sGameName] = game;
        }

        public static void init()
        {
            defineGame("Europa Universalis IV", ".eu4", "steam://rungameid/236850", "eu4");
            defineGame("Crusader Kings II", ".ck2", "steam://rungameid/203770", "CK2game");
            defineGame("Hearts of Iron IV", ".hoi4", "steam://rungameid/394360", "hoi4");

            // TODO add support for other Paradox games
            //defineGame("Stellaris", ".stellaris", "steam://rungameid/281990", "stellaris");

            System.Diagnostics.Debug.Assert(games.Keys.Count > 0,
                @"Fail to detect games! Did you install any Paradox Game?");
        }

        public static Dictionary<string, Game> DictGames
        {
            get
            {
                return games;
            }
        }

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
        public volatile bool isWatcherActivated;

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
            if (sFileName.Contains("temp"))
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

        public BackupPool initBackupPool(string sSaveName)
        {
            BackupPool result = new BackupPool(this, sSaveName);
            this.pools[sSaveName] = result;
            return result;
        }

        public bool addSaveFile(SaveFile saveFile)
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Game:addSaveFile(saveFile={0})` invoked ...",
                    saveFile));

            string sSaveName = saveFile.SaveName;
            BackupPool pool = this.pools[sSaveName];
            bool result = pool.add(saveFile);
            return result;
        }

        public bool removeSaveFile(SaveFile saveFile)
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Game:removeSaveFile(saveFile={0})` invoked ...",
                    saveFile));

            string sSaveName = saveFile.SaveName;
            BackupPool pool = this.pools[sSaveName];
            bool result = pool.del(saveFile);
            return result;
        }

        public void clearSaveFiles(string sSaveName)
        {
            BackupPool pool = this.pools[sSaveName];
            pool.clear();

            // handle file system

            // @see `Game:scanDirBack`
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

                // delete file
                System.IO.File.Delete(backup);
            }
        }

        public bool pushSaveFile(string sSaveName)
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Game:pushSaveFile(sSaveName={0})` invoked ...",
                    sSaveName));

            // calculate `iVersion`
            BackupPool pool = this.pools[sSaveName];
            int iVersion = pool.getMaxVersion();
            // create object `saveFile`
            SaveFile saveFile = new SaveFile(this, sSaveName, iVersion);
            System.Diagnostics.Debug.WriteLine(String.Format(
                @"Select `pushSaveFile({0})` ...",
                saveFile));
            // add `saveFile`
            bool success = this.addSaveFile(saveFile);
            if (success)
            {
                // select `saveFile`
                this.SelectedFile = saveFile;

                // handle file system

                // relocate save file
                string sPathFrom = pool.FileSave;
                string sPathTo = saveFile.AbsolutePath;
                System.Diagnostics.Debug.Assert(System.IO.File.Exists(sPathFrom));
                System.Diagnostics.Debug.WriteLine(
                    String.Format(
                        @"Relocate file '{0}' to '{1}' ...",
                        sPathFrom,
                        sPathTo));
                if (System.IO.File.Exists(sPathTo))
                    System.IO.File.Delete(sPathTo);
                System.IO.File.Copy(sPathFrom, sPathTo);
            }
            return success;
        }

        public bool popSaveFile(string sSaveName, bool keep=false)
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Game:popSaveFile(sSaveName={0})` invoked ...",
                    sSaveName));

            BackupPool pool = this.pools[sSaveName];
            SaveFile saveFile = this.SelectedFile;
            SaveFile lastFile = pool.getSecondNewest();
            // remove `saveFile`
            bool success = keep || this.removeSaveFile(saveFile);
            if (success)
            {
                if (!keep)
                {
                    System.Diagnostics.Debug.Write(
                        String.Format(
                            @"Function `Game:popSaveFile` changed `SelectedFile` to ({0}) ... ",
                            lastFile));
                    // select `lastSaveFile`
                    this.SelectedFile = lastFile;
                }

                // handle file system

                // remove default backup file
                string sPathBack = pool.FileBack;
                if (System.IO.File.Exists(sPathBack))
                    System.IO.File.Delete(sPathBack);

                // relocate backup file
                string sPathTo = pool.FileSave;
                string sPathFrom = saveFile.AbsolutePath;
                System.Diagnostics.Debug.Assert(System.IO.File.Exists(sPathFrom));
                System.Diagnostics.Debug.WriteLine(
                    String.Format(
                        @"Relocate file '{0}' to '{1}' ...",
                        sPathFrom,
                        sPathTo));
                if (System.IO.File.Exists(sPathTo))
                    System.IO.File.Delete(sPathTo);
                if (keep)
                    System.IO.File.Copy(sPathFrom, sPathTo);
                else
                    System.IO.File.Move(sPathFrom, sPathTo);
            }
            return success;
        }

        public bool peekSaveFile(string sSaveName)
        {
            return popSaveFile(sSaveName, true);
        }

        public static ICollection<string> Games
        {
            get
            {
                ICollection<string> keys = games.Keys;
                return keys;
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
            // destroy pools
            this.pools.Clear();

            string sPathSave = this.PathSave;

            // get the list of all files in `save games/` folder
            string[] actives = System.IO.Directory.GetFiles(sPathSave);
            // traverse all files in `save games/` folder
            foreach (string active in actives)
            {
                System.Diagnostics.Debug.WriteLine(String.Format(
                    @"Function `scanDirSave` handling file (sPath='{0}') ...",
                    active));

                if (!this.isIronMode(active))
                    continue;
                System.Diagnostics.Debug.WriteLine(String.Format(
                    @"File (sPath='{0}') is not ironman save, skip ...",
                    active));

                string sFileName = System.IO.Path.GetFileNameWithoutExtension(active);
                // create a dictionary of file name mapping to a list of file names
                this.initBackupPool(sFileName);
            }

            this.scanDirBack();
        }

        public void scanDirBack()
        {
            // clean up pools
            foreach (BackupPool pool in pools.Values.AsEnumerable())
            {
                pool.clear();
            }

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

        public System.IO.FileSystemWatcher createFileSystemWatcher(
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

        public ICollection<string> Saves
        {
            get
            {
                ICollection<string> result = this.pools.Keys;
                return result;
            }
        }

        public IList<SaveFile> getVersionList(string sSaveName)
        {
            IList<SaveFile> result = null;
            if (!this.pools.TryGetValue(sSaveName, out BackupPool pool))
            {
                pool = this.initBackupPool(sSaveName);
            }
            result = pool.Values;
            System.Diagnostics.Debug.Assert(result != null, "Variable `result` should not be NULL!");
            return result;
        }
    }
}
