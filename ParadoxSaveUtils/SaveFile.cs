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

    public class SaveFile
    {
        private Game game;
        private string sSaveName;
        private int iVersion;
        private string sFileName;
        // LinkedList
        private SaveFile last;
        private SaveFile next;

        // Split full file name such as "Poland (2).eu4"
        // into three parts: "Poland", "2" and ".eu4";
        // then create instance `SaveFile(game, sSaveName="Poland", iVersion=4)`
        public SaveFile(Game game, string sSaveName, int iVersion)
        {
            System.Diagnostics.Debug.Assert(game != null);
            System.Diagnostics.Debug.Assert(!String.IsNullOrWhiteSpace(sSaveName));
            System.Diagnostics.Debug.Assert(iVersion > 0);

            this.game = game;
            this.sSaveName = sSaveName;
            this.iVersion = iVersion;
            this.sFileName = recreateFileName();
            //
            this.last = this.next = null;
        }

        private string recreateFileName()
        {
            string sGameSaveExtensionName = game.FileExtensionName;
            string sSaveName = this.SaveName;
            int iVersion = this.Version;

            string sFullFileName = string.Format("{0} ({1}){2}",
                sSaveName,
                iVersion,
                sGameSaveExtensionName);

            System.Diagnostics.Trace.Assert(!string.IsNullOrWhiteSpace(sFullFileName),
                String.Format(@"Invalid output from 'SaveFile.FileName'!
                        Input is (sGameSaveExtensionName={0}, sSaveName={1}, iVersion={2}).",
                    sGameSaveExtensionName, sSaveName, iVersion));
            System.Diagnostics.Debug.WriteLine(
                String.Format("generateFullFileName(sSaveName='{0}', iVersion={1}) => '{2}';",
                sSaveName, iVersion, sFullFileName));

            return sFullFileName;
        }

        public DateTime LastWriteTimeUtc
        {
            get
            {
                string path = this.AbsolutePath;
                DateTime result = System.IO.File.GetLastWriteTimeUtc(path);
                return result;
            }
        }

        public string AbsolutePath
        {
            get
            {
                Game game = this.game;
                string sPathBack = game.PathBack;
                string sFileName = this.FileName;
                string result = System.IO.Path.Combine(
                    sPathBack,
                    sFileName);
                return result;
            }
        }

        public string SaveName
        {
            get { return this.sSaveName; }
        }

        public int Version
        {
            get { return this.iVersion; }
        }

        public string FileName
        {
            get { return sFileName; }
        }

        public SaveFile Last
        {
            get { return this.last; }
            set { this.last = value; }
        }

        public SaveFile Next
        {
            get { return this.next; }
            set { this.next = value; }
        }

        public override string ToString() => string.Format(@"SaveFile(game='{0}', save='{1}', version={2})",
                game.GameName,
                this.sSaveName,
                this.iVersion);
    }
}