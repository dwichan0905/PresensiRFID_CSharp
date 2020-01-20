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

namespace ArduinoTest {
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
                serialPort.Write("Disconnected.");
                serialPort.Close();
            } catch (Exception ex) {

            } finally {
                btnDisconnect.Enabled = false;
                btnConnect.Enabled = true;
                tmrSearch.Enabled = false;
                tmrComm.Enabled = false;
                portUsed = "";
                txtOut.Text += "\r\nTerputus.";
            }
        }

        private void btnClose_Click (object sender, EventArgs e) {
            this.Close();
        }

        private void tmrSearch_Tick (object sender, EventArgs e) {
            String s = serialPort.ReadExisting();
            if (s != "") {
                txtOut.Text += "\r\nDevice ID " + s + " terdeteksi! Mencari ketersediaan perangkat...";
                //string json = "{\"code\":\"404\",\"result\":\"Device 1\"}";
                var json = "";
                using (WebClient wc = new WebClient()) {
                    json = wc.DownloadString("http://localhost/PresensiRFID_Web/devs/cek_dev.json?device_id=" + s);
                }
                serialPort.Write(json);
                dynamic ss = JsonConvert.DeserializeObject(json);
                if (ss.code == 200) {
                    txtOut.Text += "\r\nDevice ID " + s + " terdaftar dengan nama " + ss.result + "!";
                } else if (ss.code == 404) {
                    txtOut.Text += "\r\nDevice ID " + s + " tidak terdaftar!";
                } else {
                    txtOut.Text += "\r\nERROR!";
                }
            }
        }
    }
}
