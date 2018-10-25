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
    public partial class Form1 : Form
    {
        enum SaveMode
        {
            PUSH,
            POP,
            PEEK
        }

        enum PSUStatus
        {
            RUNNING,
            FAILURE,
            SUCCESS
        }

        private PSUStatus status;

        private Game selectedGame;

        static Form1()
        {
            Game.init();
        }

        public Form1()
        {
            this.selectedGame = null;

            // UI initialization
            InitializeComponent();
            LoadResources();

            Game.updateUI_game(this.comboBox1);

            if (this.comboBox1.Items.Count > 0)
                this.comboBox1.SelectedIndex = 0;
#if DEBUG
            Task task = Task.Delay(1000).ContinueWith(t =>
            {
                System.Diagnostics.Debug.Assert(this.comboBox1.Items.Count > 0, "Invalid UI initialization: comboBox1");
                System.Diagnostics.Debug.Assert(this.comboBox2.Items.Count > 0, "Invalid UI initialization: comboBox2");
                System.Diagnostics.Debug.Assert(this.comboBox3.Items.Count > 0, "Invalid UI initialization: comboBox3");
            });
#endif
        }

        // Push
        private void button1_Click(object sender, EventArgs e) => doPush();

        // Pop
        private void button2_Click(object sender, EventArgs e) => doPop();

        // Peek
        private void button3_Click(object sender, EventArgs e) => doPeek();

        // Restart Game
        private void button4_Click(object sender, EventArgs e) => doRestart();

        // Clean
        private void button5_Click(object sender, EventArgs e) => doClean();

        // Open Folder
        private void button6_Click(object sender, EventArgs e) => openExplorer();

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) => onSelectGame();

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e) => onSelectSave();

        private string getGameName()
        {
            var item = this.comboBox1.SelectedItem;
            if (item == null)
                return null;
            return item.ToString();
        }

        private string getSaveName()
        {
            var item = this.comboBox2.SelectedItem;
            if (item == null)
                return null;
            return item.ToString();
        }

        private int getVersioni()
        {
            var item = this.comboBox3.SelectedItem;
            if (item == null)
                return 1;
            return (int)item;
        }
    }
}
