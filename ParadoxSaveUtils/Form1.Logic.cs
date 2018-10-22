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
            SaveFile saveFile = game.SelectedFile;
            // get save name
            string sSaveName = saveFile.SaveName;
            // get version number
            int iVersion = saveFile.Version;

            // add save file into the table
            game.pushSaveFile(sSaveName);

            // adjust UI
            saveFile = game.SelectedFile;
            ++iVersion;
            this.comboBox3.Items.Insert(0, iVersion);
            this.comboBox3.SelectedItem = iVersion;

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

            // remove save file
            game.popSaveFile(sSaveName);

            // adjust UI
            this.comboBox3.Items.Remove(iVersion);
            --iVersion;
            this.comboBox3.SelectedItem = iVersion;

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
            SaveFile saveFile = game.SelectedFile;
            // get save name
            string sSaveName = saveFile.SaveName;

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
            string sSaveName = saveFile.SaveName;

            if (String.IsNullOrWhiteSpace(sSaveName))
            {
                this.setStatus(PSUStatus.FAILURE);
                return;
            }

            string sPathBack = game.PathBack;

            // TODO
            throw new NotSupportedException("clean");

            // clean up field `dSaves`
            game.clearSaveFiles(sSaveName);

            // clean up UI
            this.comboBox3.Items.Clear();

            /////////////////

            this.setStatus(PSUStatus.SUCCESS);
        }

        private void onSelectGame()
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Form1:onSelectGame()` invoked ..."));

            string sGameName = this.getGameName();
            System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sGameName));
            Game game = games[sGameName];
            this.selectedGame = game;

            game.activateWatcher();

            game.scanDirSave();
            game.scanDirBack();

            // sort lists
            game.updateUI_save(this.comboBox2);
        }

        private void onSelectSave()
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Form1:onSelectSave()` invoked ..."));

            // update `comboBox3`(Select Version)
            Game game = this.selectedGame;
            string sSaveName = this.getSaveName();
            System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sSaveName));

            game.updateUI_version(sSaveName, this.comboBox3);
        }

        // @see https://stackoverflow.com/questions/5901679/kill-process-tree-programmatically-in-c-sharp
        private static void KillProcessTree(int pid)
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"Function `Form1:KillProcessTree(pid={0})` invoked ...",
                    pid));

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
    }
}
