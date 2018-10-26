using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParadoxSaveUtils
{
    partial class Form1
    {
        private void doPush()
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Form1:doPush()` invoked ..."));

            this.setStatus(PSUStatus.RUNNING);

            /////////////////

            Game game = this.selectedGame;
            // get save name
            string sSaveName = this.getSaveName();
            if (String.IsNullOrWhiteSpace(sSaveName))
            {
                this.setStatus(PSUStatus.FAILURE);
                return;
            }

            // add save file into the table
            game.pushSaveFile(sSaveName);

            /////////////////
            this.setStatus(PSUStatus.SUCCESS);
        }

        private void doPop()
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Form1:doPop()` invoked ..."));

            this.setStatus(PSUStatus.RUNNING);

            /////////////////

            Game game = this.selectedGame;
            // get save name
            string sSaveName = this.getSaveName();
            if (String.IsNullOrWhiteSpace(sSaveName))
            {
                this.setStatus(PSUStatus.FAILURE);
                return;
            }
            int version = this.getVersioni();
            if (version < 0)
            {
                this.setStatus(PSUStatus.FAILURE);
                return;
            }

            // remove save file
            game.popSaveFile(sSaveName);

            /////////////////

            this.setStatus(PSUStatus.SUCCESS);
        }

        private void doPeek()
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Form1:doPeek()` invoked ..."));

            this.setStatus(PSUStatus.RUNNING);

            /////////////////

            Game game = this.selectedGame;
            // get save name
            string sSaveName = this.getSaveName();
            if (String.IsNullOrWhiteSpace(sSaveName))
            {
                this.setStatus(PSUStatus.FAILURE);
                return;
            }
            int version = this.getVersioni();
            if (version < 0)
            {
                this.setStatus(PSUStatus.FAILURE);
                return;
            }

            game.peekSaveFile(sSaveName);

            /////////////////

            this.setStatus(PSUStatus.SUCCESS);
        }

        private void doRestart()
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Form1:doRestart()` invoked ..."));

            this.setStatus(PSUStatus.RUNNING);

            /////////////////

            this.killGame();
            this.bootGame();

            /////////////////

            this.setStatus(PSUStatus.SUCCESS);
        }

        private void recyle(string path)
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Form1:recyle()` invoked ..."));

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

        // remove all save files with the same save name
        private void doClean()
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Form1:doClean()` invoked ..."));

            this.setStatus(PSUStatus.RUNNING);

            /////////////////

            Game game = this.selectedGame;
            SaveFile saveFile = game.SelectedFile;
            if (saveFile == null)
            {
                System.Diagnostics.Debug.WriteLine(@"Warning! Action<Clean>. SelectedFile is null!");
                this.setStatus(PSUStatus.FAILURE);
                return;
            }
            string sSaveName = saveFile.SaveName;

            if (String.IsNullOrWhiteSpace(sSaveName))
            {
                this.setStatus(PSUStatus.FAILURE);
                return;
            }

            // clean up field `dSaves`
            game.clearSaveFiles(sSaveName);

            // clean up UI
            this.resetComboBox(this.comboBox3);

            /////////////////

            this.setStatus(PSUStatus.SUCCESS);
        }

        private void onSelectGame()
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Form1:onSelectGame()` invoked ..."));

            string sGameName = this.getGameName();
            System.Diagnostics.Debug.Assert(!String.IsNullOrWhiteSpace(sGameName));
            Game game = Game.DictGames[sGameName];
            this.selectedGame = game;

            this.activateWatcher(game);

            game.scanDirSave();
            game.scanDirBack();

            // sort lists
            this.updateUI_save();
        }

        private void onSelectSave()
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Form1:onSelectSave()` invoked ..."));

            // update `comboBox3`(Select Version)
            Game game = this.selectedGame;
            string sSaveName = this.getSaveName();

            if (sSaveName != null)
                this.updateUI_version(sSaveName);
        }

        // @see https://stackoverflow.com/questions/5901679/kill-process-tree-programmatically-in-c-sharp
        private static void KillProcessTree(int pid)
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Form1:KillProcessTree(pid={0})` invoked ...",
                    pid));

            if (pid == 0) return;

            string query = String.Format(
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
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Form1:killGame()` invoked ..."));

            Game game = this.selectedGame;
            string sGameName = game.GameName;
            string processName = game.ProcessName;
            System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcessesByName(processName);
            int nProcess = processes.Count();
            if (nProcess == 1)
                foreach (System.Diagnostics.Process process in processes)
                {
                    int pid = process.Id;
                    System.Diagnostics.Debug.WriteLine(
                        String.Format(
                            "Kill Game(pid={0}) ...",
                            pid));
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
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Form1:bootGame()` invoked ..."));

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

        private void openExplorer()
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Form1:openExplorer()` invoked ..."));

            Game game = this.selectedGame;
            string path = game.PathSave;
            System.Diagnostics.Process.Start(path);
        }

        private void resetComboBox(System.Windows.Forms.ComboBox comboBox)
        {
            if (comboBox.InvokeRequired)
            {
                comboBox.Invoke(new Action(() =>
                {
                    comboBox.SelectedItem = null;
                    comboBox.Items.Clear();
                }));
            }
            else
            {
                comboBox.SelectedItem = null;
                comboBox.Items.Clear();
            }
        }

        private void setComboBoxRange(System.Windows.Forms.ComboBox comboBox, object[] range)
        {
            if (comboBox.InvokeRequired)
            {
                comboBox.Invoke(new Action(() =>
                {
                    comboBox.Items.AddRange(range);
                    comboBox.SelectedIndex = 0;
                }));
            }
            else
            {
                comboBox.Items.AddRange(range);
                comboBox.SelectedIndex = 0;
            }
        }

        private void updateUI_save()
        {
            this.resetComboBox(this.comboBox2);
            this.resetComboBox(this.comboBox3);

            Game game = this.selectedGame;
            if (game == null)
                return;

            ICollection<string> keys = game.Saves;
            string sPathSave = game.PathSave;
            string sFileExtensionName = game.FileExtensionName;
            if (keys.Count > 0)
            {
                // sort save files by last modification time
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
                IList<string> list = sldts.Values;
                list.CopyTo(range, 0);
                this.setComboBoxRange(this.comboBox2, range);
            }
        }

        public void updateUI_version(string sSaveName)
        {
            this.resetComboBox(this.comboBox3);

            Game game = this.selectedGame;
            IList<SaveFile> list = game.getVersionList(sSaveName);
            int count = list.Count;
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `updateUI_version` counted {0} items ...",
                    count));
            if (count > 0)
            {
                object[] range = new object[count];
                for (int i = 0; i < count; i++)
                {
                    int version = list[i].Version;
                    range[i] = version;
                    System.Diagnostics.Debug.WriteLine(
                        String.Format(
                            @"Function `updateUI_version` added version `{0}` into comboBox ...",
                            version));
                }
                this.setComboBoxRange(this.comboBox3, range);
                // select save file
                SaveFile saveFile = list[0];
                game.SelectedFile = saveFile;
            }
        }

        private void onChangeDirSave(object source, System.IO.FileSystemEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("onChangeDirSave(source={0}, args={1});", source, args);

            Game game = this.selectedGame;
            if (game == null)
                return;

            game.scanDirSave();
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Save Count: {0}.",
                    game.Saves.Count));
            this.updateUI_save();
        }

        protected void onChangeDirBack(object source, System.IO.FileSystemEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("onChangeDirBack(source={0}, args={1});", source, args);

            Game game = this.selectedGame;
            if (game == null)
                return;

            game.scanDirBack();

            SaveFile saveFile = game.SelectedFile;
            if (saveFile == null)
                return;
            string sSaveName = saveFile.SaveName;

            this.updateUI_version(sSaveName);
        }

        public void activateWatcher(Game game)
        {
            if (game.isWatcherActivated)
                return;

            string sPathSave = game.PathSave;
            string sPathBack = game.PathBack;
            // get extension name of game save file
            string sGameSaveExtensionName = game.FileExtensionName;

            // watch `save games/` folder for any change
            game.createFileSystemWatcher(sPathSave, sGameSaveExtensionName, this.onChangeDirSave);
            // watch `save games/backup/` folder for any change
            game.createFileSystemWatcher(sPathBack, sGameSaveExtensionName, this.onChangeDirBack);

            game.isWatcherActivated = true;
        }
    }
}
