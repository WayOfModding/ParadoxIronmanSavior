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
    public class BackupPool
    {
        private class DateTimeComparer : IComparer<DateTime>
        {
            int IComparer<DateTime>.Compare(DateTime x, DateTime y)
            {
                return y.CompareTo(x);
            }
        }

        static IComparer<DateTime> dateTimeComparer = new DateTimeComparer();

        // list of all backup saves
        private Dictionary<int, SaveFile> dictSaves =
            new Dictionary<int, SaveFile>();
        private SortedList<DateTime, SaveFile> listSaves =
            new SortedList<DateTime, SaveFile>(dateTimeComparer);

        public bool add(SaveFile saveFile)
        {
            int iVersion = saveFile.Version;
            DateTime time = saveFile.LastWriteTimeUtc;
            // avoid duplicate entries
            bool result1 = false;
            bool result2 = false;

            if (!listSaves.ContainsKey(time))
            {
                listSaves[time] = saveFile;
                result1 = true;
            }
            if (!dictSaves.ContainsKey(iVersion))
            {
                dictSaves[iVersion] = saveFile;
                result2 = true;
            }
            return result1 && result2;
        }

        public bool del(SaveFile saveFile)
        {
            int iVersion = saveFile.Version;
            DateTime time = saveFile.LastWriteTimeUtc;
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
    }
}