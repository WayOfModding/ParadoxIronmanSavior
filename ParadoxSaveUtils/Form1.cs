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

            this.updateUI_game();
        }

        private void updateUI_game()
        {
            comboBox1.Items.Clear();

            ICollection<string> games = Game.Games;
            int count = games.Count;
            string[] range = new string[count];
            games.CopyTo(range, 0);
            comboBox1.Items.AddRange(range);
            if (count > 0)
                this.comboBox1.SelectedIndex = 0;
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
                return -1;
            return (int)item;
        }
    }
}
