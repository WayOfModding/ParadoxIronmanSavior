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
    private class DateTimeComparer : IComparer<DateTime>
    {
        int IComparer<DateTime>.Compare(DateTime x, DateTime y)
        {
            return y.CompareTo(x);
        }
    }

    public static IComparer<DateTime> dateTimeComparer = new DateTimeComparer();

    public class BackupPool
    {
        // list of all backup saves
        private Dictionary<int, SaveFile> dictSaves =
            new Dictionary<int, SaveFile>();
        private SortedList<DateTime, SaveFile> listSaves =
            new SortedList<DateTime, SaveFile>(dateTimeComparer);

        public Dictionary<int, SaveFile> Dict { get { return this.dictSaves; } }
        public SortedList<DateTime, SaveFile> List { get { return this.listSaves; } }
    }
}