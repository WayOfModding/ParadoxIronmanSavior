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

            int iVersion = saveFile.Version;
            DateTime time = saveFile.LastWriteTimeUtc;
            // avoid duplicate entries
            bool result1 = false;
            bool result2 = false;
            bool result3 = false;

            if (listSaves.ContainsKey(time))
            {
                SaveFile conflict = listSaves[time];
                using (var fs1 = System.IO.File.OpenRead(saveFile.AbsolutePath))
                using (var fs2 = System.IO.File.OpenRead(conflict.AbsolutePath))
                {
                    var hash1 = MD5.Create().ComputeHash(fs1);
                    var hash2 = MD5.Create().ComputeHash(fs2);
                    result3 = !hash1.SequenceEqual(hash2);
#if DEBUG
                    string sHash1 = BitConverter.ToString(hash1).Replace("-", "").ToLowerInvariant();
                    string sHash2 = BitConverter.ToString(hash2).Replace("-", "").ToLowerInvariant();
                    System.Diagnostics.Debug.Write(String.Format(
                        @"Comparing conflict files: file/a({0}) - file/b({1}) ... ",
                        sHash1.Substring(0, 7),
                        sHash2.Substring(0, 7)));
#endif
                }
                result1 = false;
            }
            else
            {
                listSaves[time] = saveFile;
                result1 = true;
            }
            if (!dictSaves.ContainsKey(iVersion))
            {
                dictSaves[iVersion] = saveFile;
                result2 = true;
            }

            bool result = (result1 || result3) && result2;
            if (!result)
            {
                if (result1)
                    listSaves.Remove(time);
                if (result2)
                    dictSaves.Remove(iVersion);
#if !DEBUG
                // remove duplicate save file
                string sFilePath = saveFile.AbsolutePath;
                if (System.IO.File.Exists(sFilePath))
                    System.IO.File.Delete(sFilePath);
#endif
            }

            System.Diagnostics.Debug.WriteLine(
                String.Format(
                    @"{0}{1}",
                    result ? "Success" : "Failure",
                    result ? "" : String.Format(@"={0}+{1}",
                        result1,
                        result2)));

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
            KeyValuePair<DateTime, SaveFile> kvpNewest = listSaves.First();
            return kvpNewest.Value;
        }

        private SaveFile getOldest()
        {
            KeyValuePair<DateTime, SaveFile> kvpOldest = listSaves.Last();
            return kvpOldest.Value;
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
