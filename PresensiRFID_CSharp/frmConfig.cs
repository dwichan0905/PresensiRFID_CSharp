using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PresensiRFID {
    public partial class frmConfig : Form {
        public frmConfig () {
            InitializeComponent();
        }

        private void frmConfig_Load (object sender, EventArgs e) {
            txtServer.Text = Properties.Settings.Default.Server;
        }

        private void button2_Click (object sender, EventArgs e) {
            Properties.Settings.Default.Server = txtServer.Text;
            Properties.Settings.Default.Save();
            MessageBox.Show(this, "Berhasil disimpan!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        private void button1_Click (object sender, EventArgs e) {
            this.Close();
        }
    }
}
