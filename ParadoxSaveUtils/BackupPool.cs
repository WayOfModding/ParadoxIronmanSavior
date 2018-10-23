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
        private Dictionary<int, SaveFile> dictSaves =
            new Dictionary<int, SaveFile>();
        private SortedList<DateTime, SaveFile> listSaves =
            new SortedList<DateTime, SaveFile>(dateTimeComparer);

        private Game game;
        private string sSaveName;

        public BackupPool(Game game, string sSaveName)
        {
            this.game = game;
            this.sSaveName = sSaveName;
        }

        public bool add(SaveFile saveFile)
        {
            System.Diagnostics.Debug.Write(
                String.Format(
                    @"Function `BackupPool:add(saveFile={0})` invoked ... ",
                    saveFile));

            SaveFile challenge;
            string sPathSave, sPathChallenge;
            DateTime time;
            int iVersion = saveFile.Version;
            bool result = false;

            // Add an existing file into the pool:
            if (System.IO.File.Exists(saveFile.AbsolutePath))
            {
                sPathSave = saveFile.AbsolutePath;
                time = saveFile.LastWriteTimeUtc;
                challenge = null;
                if (listSaves.ContainsKey(time))
                {
                    challenge = listSaves[time];
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

                bool containsVersion = dictSaves.ContainsKey(iVersion);
                if (containsVersion)
                {
                    System.Diagnostics.Debug.Write(String.Format(
                        @"Dictionary `dictSaves` contains key: (version={0}) ... ",
                        iVersion));
                }
                result &= !containsVersion;
            }
            // No challenge, no comparison, auto-success
            else
            {
                result = true;
            }

            if (result)
            {
                // on success
                listSaves[time] = saveFile;
                dictSaves[iVersion] = saveFile;
            }
#if ! DEBUG
            else
            {
                // remove duplicate save file
                string sFilePath = saveFile.AbsolutePath;
                if (System.IO.File.Exists(sFilePath))
                    System.IO.File.Delete(sFilePath);
            }
#endif

            System.Diagnostics.Debug.WriteLine(
                result ? "Success" : "Failure");
            System.Diagnostics.Debug.Assert(dictSaves.Count == listSaves.Count,
                String.Format("Assertion failed (dictSaves.Count={0}, listSaves.Count={1})",
                    dictSaves.Count,
                    listSaves.Count));

            return result;
        }

        public bool del(SaveFile saveFile)
        {
            int iVersion = saveFile.Version;
            DateTime time = saveFile.LastWriteTimeUtc;
            System.Diagnostics.Debug.Assert(time != null);
            // remove
            bool result = dictSaves.Remove(iVersion);
            result &= listSaves.Remove(time);

            return result;
        }

        public void clear()
        {
            dictSaves.Clear();
            listSaves.Clear();
        }

        public int getMaxVersion()
        {
            int iMaxVersion = dictSaves.Keys.Max();
            int iVersion = iMaxVersion + 1;
            return iVersion;
        }

        private SaveFile getNewest()
        {
            if (listSaves.Count > 0)
            {
                KeyValuePair<DateTime, SaveFile> kvpNewest = listSaves.First();
                return kvpNewest.Value;
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
                IList<SaveFile> list = listSaves.Values;
                return list[1];
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
                KeyValuePair<DateTime, SaveFile> kvpOldest = listSaves.Last();
                return kvpOldest.Value;
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
                System.Diagnostics.Debug.Assert(dictSaves.Count == listSaves.Count,
                    String.Format("Assertion failed (dictSaves.Count={0}, listSaves.Count={1})",
                        dictSaves.Count,
                        listSaves.Count));
                return listSaves.Count;
            }
        }

        public IList<SaveFile> Values
        {
            get
            {
                return listSaves.Values;
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
