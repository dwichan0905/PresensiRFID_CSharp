using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using System.IO.Ports;
using System.Threading;
using System.Net;
using Newtonsoft.Json;

namespace PresensiRFID {
    public partial class frmMain : Form {
        private SerialPort serialPort = new SerialPort();
        private string portUsed = "";

        public frmMain () {
            InitializeComponent();
        }

        private bool AutodetectArduinoPort () {
            SerialPort tmp;
            foreach (string str in SerialPort.GetPortNames())
            {
                tmp = new SerialPort(str);
                if (tmp.IsOpen == false)
                {
                    try
                    {
                        //open serial port
                        serialPort.PortName = str;
                        serialPort.Open();
                        serialPort.BaudRate = 9600;
                        serialPort.WriteTimeout = 10;
                        serialPort.ReadTimeout = 10;
                        serialPort.Write(str);
                        txtOut.Text += "\r\nPerangkat terhubung ke komputer via Komunikasi Serial dengan Port " + str + "!";
                        portUsed = str;
                        tmrSearch.Enabled = true;
                        tmrComm.Enabled = false;
                        return true;
                    }
                    catch (TimeoutException)
                    {
                        serialPort.Close();
                        return false;
                    }
                    catch (Exception ex)
                    {
                        tmrComm.Enabled = false;
                        MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return true;
                    }
                }
            }
            return false;
        }

        private void btnConnect_Click (object sender, EventArgs e) {
            if (btnConnect.Text == "Stop") {
                tmrComm.Enabled = false;
                btnConnect.Text = "Connect";
                txtOut.AppendText("\r\nKoneksi dibatalkan.");
            } else {
                if (cmbMode.SelectedItem == null)
                    MessageBox.Show(this, "Pilih mode yang akan digunakan!", "Choose Mode", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else {
                    txtOut.Text = "Mencari port yang akan digunakan...";
                    tmrSearch.Enabled = false;
                    tmrComm.Enabled = true;
                    btnConnect.Text = "Stop";
                }
            }
            
        }

        private void tmrComm_Tick (object sender, EventArgs e) {
            if (AutodetectArduinoPort()) {
                //serialPort.Write(portUsed);
                btnDisconnect.Enabled = true;
                btnConnect.Text = "Connect";
                btnConnect.Enabled = false;
            } 
        }

        private void btnDisconnect_Click (object sender, EventArgs e) {
            try {
                serialPort.Write("DC");
                serialPort.Close();
            } catch (Exception ex) {

            } finally {
                btnDisconnect.Enabled = false;
                btnConnect.Enabled = true;
                tmrSearch.Enabled = false;
                tmrComm.Enabled = false;
                portUsed = "";
                txtOut.AppendText("\r\nTerputus.");
            }
        }

        private void btnClose_Click (object sender, EventArgs e) {
            this.Close();
        }

        private void tmrSearch_Tick (object sender, EventArgs e) {
            try {
                string s = serialPort.ReadExisting();
                string[] str = s.Split('=');
                if (str[0] == "DEVID") {
                    if (str[1] != "") {
                        txtOut.AppendText("\r\nDevice ID " + str[1].Substring(0, str[1].Length - 1) + " terdeteksi! Mencari ketersediaan perangkat...");
                        var json = "";
                        using (WebClient wc = new WebClient()) {
                            json = wc.DownloadString(PresensiRFID.Properties.Settings.Default.Server + "/devs/cek_dev.json?device_id=" + str[1].Substring(0, str[1].Length - 1));
                        }
                        serialPort.Write(json);
                        Thread.Sleep(50);
                        dynamic ss = JsonConvert.DeserializeObject(json);
                        if (ss.code == 200) {
                            txtOut.AppendText("\r\nDevice ID " + str[1].Substring(0, str[1].Length - 1) + " terdaftar dengan nama " + ss.result + "!");
                        } else if (ss.code == 404) {
                            txtOut.AppendText("\r\nDevice ID " + str[1].Substring(0, str[1].Length - 1) + " tidak terdaftar!");
                        } else {
                            txtOut.AppendText("\r\nERROR!");
                        }
                        Thread.Sleep(500);
                        txtOut.AppendText("\r\n\r\nMenunggu data masuk... Silakan tempelkan kartu pada perangkat!");
                    }
                    } else if (str[0] == "UID") {
                        string[] spl = s.Split(';');
                        string uid = spl[0].Split('=')[1];
                        string devid = spl[1].Split('=')[1];
                        txtOut.AppendText("\r\nCard ID " + uid + " telah masuk via Device ID " + devid);
                        var json = "";
                        using (WebClient wc = new WebClient()) {
                            if (cmbMode.SelectedItem.ToString() == "PRESENSI") {
                                json = wc.DownloadString(Properties.Settings.Default.Server + "/devs/presensi.json?device_id=" + devid + "&card_id=" + uid);
                                dynamic ss = JsonConvert.DeserializeObject(json);
                                if (ss.code == 200) {
                                    string sqs = ss.result;
                                    string[] sss = sqs.Split(';');
                                    txtOut.AppendText("\r\nTerdaftar Mahasiswa bernama " + sss[1] + " dengan NIM " + sss[0] + "!\r\n");
                                } else if (ss.code == 404) {
                                    txtOut.AppendText("\r\nMahasiswa tidak ditemukan." + "!\r\n");
                                } else {
                                    txtOut.AppendText("\r\nERROR!" + "!\r\n");
                                }
                            } else if (cmbMode.SelectedItem.ToString() == "TAMBAH KARTU") {
                                json = wc.DownloadString(Properties.Settings.Default.Server + "/devs/tambah_kartu.json?device_id=" + devid + "&card_id=" + uid);
                                dynamic ss = JsonConvert.DeserializeObject(json);
                                if (ss.code == 2) {
                                    txtOut.AppendText("\r\nKartu Mahasiswa dengan Card ID " + uid + " berhasil ditambahkan!\r\n");
                                } else if (ss.code == 1) {
                                    txtOut.AppendText("\r\nKartu Mahasiswa dengan Card ID " + uid + " sudah pernah ditambahkan!\r\n");
                                } else {
                                    txtOut.AppendText("\r\nERROR!" + "!\r\n");
                                }
                            }

                        }
                        //txtOut.AppendText("\r\n" + json);
                        serialPort.Write(json);
                        txtOut.AppendText("\r\nMenunggu data masuk... Silakan tempelkan kartu pada perangkat!");
                    }
            } catch (WebException ex) {
                txtOut.AppendText("\r\nGagal terhubung dengan server: "  + Properties.Settings.Default.Server + "!\r\nAlasan:\r\n" + ex.ToString() + "\r\n");
            } catch (InvalidOperationException ex) {
                txtOut.AppendText("\r\nPerangkat diputus secara paksa! Menutup koneksi...");
                try {
                    serialPort.Write("DC");
                    serialPort.Close();
                } catch (Exception ey) {

                } finally {
                    btnDisconnect.Enabled = false;
                    btnConnect.Enabled = true;
                    tmrSearch.Enabled = false;
                    tmrComm.Enabled = false;
                    portUsed = "";
                    txtOut.AppendText("\r\nTerputus.");
                }
            }
        }

        private void button1_Click (object sender, EventArgs e) {
            new frmAbout().ShowDialog(this);
        }

        private void btnConfig_Click (object sender, EventArgs e) {
            new frmConfig().ShowDialog(this);
        } 
            
    }
}
