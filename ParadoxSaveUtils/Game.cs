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

        public Game(string sGameName, string sFileExtensionName, string sURI, string sProcessName)
        {
            System.Diagnostics.Debug.Assert(!String.IsNullOrWhiteSpace(sGameName));
            System.Diagnostics.Debug.Assert(!String.IsNullOrWhiteSpace(sFileExtensionName));
            System.Diagnostics.Debug.Assert(!String.IsNullOrWhiteSpace(sURI));
            System.Diagnostics.Debug.Assert(!String.IsNullOrWhiteSpace(sProcessName));

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
            ICollection<string> keys = this.pools.Keys;
            if (keys.Count > 0)
            {
                // TODO sort save files by last modification time
                string[] range = new string[keys.Count];
                keys.CopyTo(range, 0);
                comboBox.Items.AddRange(range);
                comboBox.SelectedIndex = 0;
            }
        }

        public void updateUI_version(string sSaveName, ComboBox comboBox)
        {
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
            set { this.selectedFile = value; }
        }

        public bool ContainsSave(string sSaveName)
        {
            Dictionary<string, BackupPool> pools = this.pools;
            bool result = pools.ContainsKey(sSaveName);
            return result;
        }
    }
}
