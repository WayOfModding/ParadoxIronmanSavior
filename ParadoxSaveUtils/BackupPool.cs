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

using MD5 = System.Security.Cryptography.MD5;

namespace ParadoxSaveUtils
{
    public class BackupPool
    {
        private class DateTimeComparer : IComparer<DateTime>
        {
            int IComparer<DateTime>.Compare(DateTime x, DateTime y)
            {
                return y.CompareTo(x);
            }
        }

        public static IComparer<DateTime> dateTimeComparer = new DateTimeComparer();

        // list of all backup saves
        private SortedList<DateTime, SortedList<int, SaveFile>> listSaves =
            new SortedList<DateTime, SortedList<int, SaveFile>>(dateTimeComparer);

        private Game game;
        private string sSaveName;
        private volatile bool dirty;
        private IList<SaveFile> cache;

        public BackupPool(Game game, string sSaveName)
        {
            this.game = game;
            this.sSaveName = sSaveName;
            this.dirty = false;
            this.cache = null;
        }

        public bool add(SaveFile saveFile)
        {
            System.Diagnostics.Debug.Write(
                String.Format(
                    @"Function `BackupPool:add(saveFile={0})` invoked ... ",
                    saveFile));

            this.dirty = true;

            SaveFile challenge;
            string sPathSave, sPathChallenge;
            DateTime time;
            int iVersion = saveFile.Version;
            bool result = false;
            SaveFile sfToBeRemoved = null;

            // Add an existing file into the pool:
            if (System.IO.File.Exists(saveFile.AbsolutePath))
            {
                sPathSave = saveFile.AbsolutePath;
                time = saveFile.LastWriteTimeUtc;
                challenge = null;
                if (listSaves.ContainsKey(time))
                {
                    challenge = get(time);
                }
                else
                {
                    challenge = this.getNewest();
                }
            }
            // Allocate disk space for a new file
            else
            {
                sPathSave = this.FileSave;
                time = System.IO.File.GetLastWriteTimeUtc(sPathSave);
                challenge = this.getNewest();
            }

            // Challenge got
            // compare it with the items already in the pool
            if (challenge != null)
            {
                sPathChallenge = challenge.AbsolutePath;

                // compare MD5 hash of two files
                using (var fs1 = System.IO.File.OpenRead(sPathSave))
                using (var fs2 = System.IO.File.OpenRead(sPathChallenge))
                {
                    var hash1 = MD5.Create().ComputeHash(fs1);
                    var hash2 = MD5.Create().ComputeHash(fs2);

                    result = !hash1.SequenceEqual(hash2);
#if DEBUG
                    string sHash1 = BitConverter.ToString(hash1).Replace("-", "").ToLowerInvariant();
                    string sHash2 = BitConverter.ToString(hash2).Replace("-", "").ToLowerInvariant();
                    System.Diagnostics.Debug.Write(String.Format(
                        @"Comparing conflict files: file/ver[{0}]({2}) - file/ver[{1}]({3}) ... ",
                        iVersion,
                        challenge.Version,
                        sHash1.Substring(0, 7),
                        sHash2.Substring(0, 7)));
#endif
                }
            }
            // No challenge, no comparison, auto-success
            else
            {
                result = true;
            }

            {
                // on success
                SortedList<int, SaveFile> dict = null;
                if (!listSaves.TryGetValue(time, out dict))
                {
                    dict = new SortedList<int, SaveFile>
                    {
                        [iVersion] = saveFile
                    };
                    listSaves[time] = dict;
                }
                else
                {
                    bool containsVersion = dict.ContainsKey(iVersion);
                    if (containsVersion)
                    {
                        System.Diagnostics.Debug.Write(String.Format(
                            @"Dictionary `dict` contains key: (version={0}) ... ",
                            iVersion));
                    }
                    result &= !containsVersion;

                    if (!containsVersion)
                    {
                        dict[iVersion] = saveFile;

                        System.Diagnostics.Debug.Write(String.Format(
                            @"Dictionary `dict` contains ({0}) pairs; start cleaning ... ",
                            dict.Count));

                        // clean up mess
                        // among files with the same `LastWriteTimeUtc`  attribute
                        // choose the one with the smallest `Version` attribute
                        while (dict.Count > 1)
                        {
                            var kvp = dict.Last();
                            int ver = kvp.Key;
                            dict.Remove(ver);

#if !DEBUG
                            sfToBeRemoved = kvp.Value;
#endif

                            System.Diagnostics.Debug.Write(String.Format(
                                @"Entry (saveFile={0}) removed ... ",
                                kvp.Value));

                            if (ver == iVersion)
                            {
                                result = false;
                            }
                            else
                            {
                                result = true;
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.Assert(dict.Count == 1,
                    "Dictionary `dict` should have only one entry!");
            }
#if !DEBUG
            if (sfToBeRemoved != null) {
                // remove duplicate save file
                string sFilePath = sfToBeRemoved.AbsolutePath;
                if (System.IO.File.Exists(sFilePath))
                    System.IO.File.Delete(sFilePath);
            }
#endif

            System.Diagnostics.Debug.WriteLine(
                result ? "Success" : "Failure");

            return result;
        }

        public bool del(SaveFile saveFile)
        {
            System.Diagnostics.Debug.Write(
                String.Format(
                    @"Function `BackupPool:del(saveFile={0})` invoked ... ",
                    saveFile));

            this.dirty = true;

            int iVersion = saveFile.Version;
            DateTime time = saveFile.LastWriteTimeUtc;
            System.Diagnostics.Debug.Assert(time != null);
            // remove
            bool result = listSaves.Remove(time);

            return result;
        }

        private SaveFile get(DateTime time)
        {
            var dict = listSaves[time];
            return get(dict);
        }

        private SaveFile get(SortedList<int, SaveFile> dict)
        {
            System.Diagnostics.Debug.Assert(dict.Count == 1,
                "Dictionary `dict` should have only one entry!");
            KeyValuePair<int, SaveFile> kvp = dict.First();
            SaveFile result = kvp.Value;
            return result;
        }

        private SaveFile get(KeyValuePair<DateTime, SortedList<int, SaveFile>> kvp)
        {
            var dict = kvp.Value;
            return get(dict);
        }

        private bool containsKey(int iVersion)
        {
            foreach (var kvp in this.listSaves)
            {
                var val = kvp.Value;
                if (val.ContainsKey(iVersion))
                    return true;
            }
            return false;
        }

        public void clear()
        {
            System.Diagnostics.Debug.Write(
                @"Function `BackupPool:clear()` invoked ... ");

            this.dirty = true;

            // FIXME
            listSaves.Clear();
        }

        public int getMaxVersion()
        {
            int iMaxVersion = 0;
            int iVersion = 0;
            foreach (var kvp in this.listSaves)
            {
                var val = kvp.Value;
                iVersion = val.Keys.Max();
                iMaxVersion = Math.Max(iMaxVersion, iVersion);
            }
            iVersion = iMaxVersion + 1;
            return iVersion;
        }

        private SaveFile getNewest()
        {
            if (listSaves.Count > 0)
            {
                var kvpNewest = listSaves.First();
                SaveFile result = get(kvpNewest);
                return result;
            }
            else
            {
                return null;
            }
        }

        public SaveFile getSecondNewest()
        {
            if (listSaves.Count > 1)
            {
                var list = listSaves.Values;
                var dict = list[1];
                SaveFile result = get(dict);
                return result;
            }
            else
            {
                return null;
            }
        }

        private SaveFile getOldest()
        {
            if (listSaves.Count > 0)
            {
                var kvpNewest = listSaves.Last();
                SaveFile result = get(kvpNewest);
                return result;
            }
            else
            {
                return null;
            }
        }

        public int Count
        {
            get
            {
                return listSaves.Count;
            }
        }

        public IList<SaveFile> Values
        {
            get
            {
                if (dirty)
                {
                    this.cache = new List<SaveFile>();
                    foreach (var kvp in this.listSaves)
                    {
                        SaveFile saveFile = get(kvp);
                        this.cache.Add(saveFile);
                    }
                }
                return this.cache;
            }
        }

        public string FileSave
        {
            get
            {
                Game game = this.game;
                string sPathSave = game.PathSave;
                string sExtensionName = game.FileExtensionName;
                string sFileName = String.Format(
                    "{0}{1}",
                    sSaveName,
                    sExtensionName);
                string result = System.IO.Path.Combine(
                    sPathSave,
                    sFileName);
                return result;
            }
        }

        public string FileBack
        {
            get
            {
                Game game = this.game;
                string sPathSave = game.PathSave;
                string sExtensionName = game.FileExtensionName;
                string sFileName = String.Format(
                    "{0}_Backup{1}",
                    sSaveName,
                    sExtensionName);
                string result = System.IO.Path.Combine(
                    sPathSave,
                    sFileName);
                return result;
            }
        }
    }
}
