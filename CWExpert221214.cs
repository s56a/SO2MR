//=================================================================
// CWExpert.cs
//=================================================================
// Copyright (C) 2011 S56A YT7PWR
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//=================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections;
using System.Threading;
using AutoItHelper;
using System.Text.RegularExpressions;



namespace CWExpert
{
    public partial class CWExpert120817 : Form
    {
        #region variable definition


        MessageHelper msg;
        public Setup SetupForm;
        private int[,] Fxp = new int[2, 22];
        public CWDecode cwDecoder;

        #endregion

        #region properites

        public ListBox clbl = new ListBox();

        public ListBox clbr = new ListBox();

        private bool mrIsRunning = false;

        public int tx_focus = 0;

        public bool so2r = false;

        public bool MRIsRunning
        {
            get { return mrIsRunning; }
            set
            {
                if (mrIsRunning)
                    btnStopMR_Click(null, null);
                Thread.Sleep(100);
                mrIsRunning = value;
                if (value)
                    btnStartMR_Click(null, null);
            }
        }

        public void rx_lights()
        {
            if (tx_focus != 0)
            {
                txb.BackColor = Color.White;
                rxb.BackColor = Color.LightGreen;
                if (so2r) { rxa.BackColor = Color.LightGreen; }
            }
            else
            {
                txa.BackColor = Color.White;
                rxa.BackColor = Color.LightGreen;
                if (so2r) { rxb.BackColor = Color.LightGreen; }
            }
        }

        public void tx_lights()
        {
            if (tx_focus != 0)
            {
                txb.BackColor = Color.LightPink;
                rxb.BackColor = Color.White;
                if (so2r) { rxa.BackColor = Color.LightGreen; }
            }
            else
            {
                txa.BackColor = Color.LightPink;
                rxa.BackColor = Color.White;
                if (so2r) { rxb.BackColor = Color.LightGreen; }
            }
        }

        public string txtCALL
        {
            set { this.txtCall.Text = value; }
        }

        public string txtNR
        {
            set { this.txtNr.Text = value; }
        }

        public string txtRst
        {
            set { this.txtRST.Text = value; }
        }

        public string txtrCALL
        {
            set { this.txtrCall.Text = value; }
        }

        public string txtrNR
        {
            set { this.txtrNbr.Text = value; }
        }

        public string txtrRst
        {
            set { this.txtrRST.Text = value; }
        }

        public void tx_change()
        {
            if (tx_focus == 0)
            {
                tx_focus = 1;
                msg.bringAppToFront(Fxp[1, 0]);
                btncall.BackColor = Color.LightGreen;
                btnrst.BackColor = Color.LightGreen;
                btnnbr.BackColor = Color.LightGreen;

                btnSendCall.BackColor = Color.White;
                btnSendRST.BackColor = Color.White;
                btnSendNr.BackColor = Color.White;
            }
            else
            {
                tx_focus = 0;
                msg.bringAppToFront(Fxp[0, 0]);
                btnSendCall.BackColor = Color.LightPink;
                btnSendRST.BackColor = Color.LightPink;
                btnSendNr.BackColor = Color.LightPink;

                btncall.BackColor = Color.White;
                btnrst.BackColor = Color.White;
                btnnbr.BackColor = Color.White;
            }
            tx_lights();
        }

        #endregion

        #region constructor

        public CWExpert120817()
        {
            InitializeComponent();
            msg = new MessageHelper();
            DB.AppDataPath = Application.StartupPath;
            DB.Init();
            Audio.MainForm = this;
            PA19.PA_Initialize();
            SetupForm = new Setup(this);
            cwDecoder = new CWDecode(this);
            btnStartMR.BackColor = Color.LightBlue;
        }

        #endregion

        #region misc function

        public string[] radios = { "FL", "FT", "IC", "K3", "Ma", "TS", "TT" };

        private bool EnsureRemoteWindow()
        {
            try
            {
                int x = 0;

                Fxp[0, 0] = 0;
                Fxp[1, 0] = 0;
                tx_focus = 0;

                int w = msg.getWindowId("ThunderRT6Main", null);
                if (w != 0)
                {
                    do
                    {
                        x = msg.getWindowIdEx(0, x, "ThunderRT6FormDC", null);
                        if (x != 0)
                        {
                            const int nch = 256;
                            StringBuilder Buff = new StringBuilder(nch);
                            if (msg.getLabel(x, Buff, nch) != 0)
                            {
                                string s = Buff.ToString();
                                //                                Debug.WriteLine(s);
                                if (s.Contains(" CW"))
                                {
                                    foreach (string radio in radios)
                                    {
                                        if (s.Contains(radio) && tx_focus < 2)
                                        {
                                            getPointers(x);
                                            Console.WriteLine(radio + "  " + tx_focus.ToString());
                                            tx_focus++;
                                        }
                                    }

                                }
                            }
                        }
                    }
                    while (x != 0);
                }

                so2r = false;
                tx_focus = 0;

                if ((Fxp[0, 0] != 0) || (Fxp[1, 0] != 0))
                {
                    if (Fxp[0, 0] == 0)
                    {
                        tx_focus = 1;
                    }
                    else if (Fxp[1, 0] != 0)
                    {
                        so2r = true;
                        butdva.BackColor = Color.LightPink;
                    }
                    return true;
                }
                else
                {
                    MessageBox.Show("CW radio not found?\n");
                    return false;
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("N1MM Logger not found!\n" + ex.ToString());
                return false;
            }
        }


        private void getPointers(int hndl)
        {
            int i = 0;
            int y = 0;
            Fxp[tx_focus, 0] = hndl;
            do
            {
                y = msg.getWindowIdEx(hndl, y, "ThunderRT6CommandButton", null);
                if (y != 0)
                {
                    i++;
                    if (i < 5) { Fxp[tx_focus, 13 - i] = y; }
                    else if (i == 5) { Fxp[tx_focus, 18] = y; }
                    else if ((i > 5) && (i < 14)) { Fxp[tx_focus, i - 5] = y; }
                }
            }
            while (y != 0);
            y = 0;
            y = msg.getWindowIdEx(hndl, y, "ThunderRT6Frame", null);
            int nmbrPanel = y;
            y = msg.getWindowIdEx(hndl, y, "ThunderRT6Frame", null);
            Fxp[tx_focus, 13] = msg.getWindowIdEx(y, 0, "ThunderRT6TextBox", null);            // call panel location #13!
            i = 0;
            y = 0; 
            do
            {
                y = msg.getWindowIdEx(nmbrPanel, y, "ThunderRT6TextBox", null);
      
                // 1-rcv, 2-snt, 5-itu, 8-cqz, 12-qtc, 6-snr

                if (y != 0)
                {
                    /*
                    msg.bringAppToFront(y);
                    AutoItHelper.AutoItX.Send(i.ToString());
                     */
                    if (i == 1)
                    {
                         Fxp[tx_focus, 14] = y;
                    }
                    else if (i == 2)
                    {
                        Fxp[tx_focus, 15] = y;
                    }
                        
                    else if (i == 6)
                    {
                        Fxp[tx_focus, 16] = y;
                    }
                         
                    else if (i == 3)
                    {
                        Fxp[tx_focus, 17] = y;
                    }
                    i++;
                }
            }
            while (y != 0);
        }
        #endregion

        #region MR Functions keys


        public void btnF1_Click(object sender, EventArgs e)
        {
            try
            {
                if (Fxp[tx_focus, 1] != 0)
                {
                    tx_lights();
                    msg.bringAppToFront(Fxp[tx_focus, 0]);
                    msg.bringAppToFront(Fxp[tx_focus, 1]);
                    AutoItHelper.AutoItX.Send("{F1}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void btnF2_Click(object sender, EventArgs e)
        {
            try
            {
                if (Fxp[tx_focus, 2] != 0)
                {
                    tx_lights();
                    msg.bringAppToFront(Fxp[tx_focus, 0]);
                    msg.bringAppToFront(Fxp[tx_focus, 2]);
                    AutoItHelper.AutoItX.Send("{F2}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void btnF3_Click(object sender, EventArgs e)
        {
            try
            {
                if (Fxp[tx_focus, 3] != 0)
                {
                    tx_lights();
                    msg.bringAppToFront(Fxp[tx_focus, 0]);
                    msg.bringAppToFront(Fxp[tx_focus, 3]);
                    AutoItHelper.AutoItX.Send("{F3}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void btnF4_Click(object sender, EventArgs e)
        {
            try
            {
                if (Fxp[tx_focus, 4] != 0)
                {
                    tx_lights();
                    msg.bringAppToFront(Fxp[tx_focus, 0]);
                    msg.bringAppToFront(Fxp[tx_focus, 4]);
                    AutoItHelper.AutoItX.Send("{F4}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void btnF5_Click(object sender, EventArgs e)
        {
            try
            {
                if (Fxp[tx_focus, 5] != 0)
                {
                    tx_lights();
                    msg.bringAppToFront(Fxp[tx_focus, 0]);
                    msg.bringAppToFront(Fxp[tx_focus, 5]);
                    AutoItHelper.AutoItX.Send("{F5}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void btnF6_Click(object sender, EventArgs e)
        {
            try
            {
                if (Fxp[tx_focus, 6] != 0)
                {
                    tx_lights();
                    msg.bringAppToFront(Fxp[tx_focus, 0]);
                    msg.bringAppToFront(Fxp[tx_focus, 6]);
                    AutoItHelper.AutoItX.Send("{F6}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void btnF7_Click(object sender, EventArgs e)
        {
            try
            {
                if (Fxp[tx_focus, 7] != 0)
                {
                    tx_lights();
                    msg.bringAppToFront(Fxp[tx_focus, 0]);
                    msg.bringAppToFront(Fxp[tx_focus, 7]);
                    AutoItHelper.AutoItX.Send("{F7}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void btnF8_Click(object sender, EventArgs e)
        {
            try
            {
                if (Fxp[tx_focus, 8] != 0)
                {
                    tx_lights();
                    msg.bringAppToFront(Fxp[tx_focus, 0]);
                    msg.bringAppToFront(Fxp[tx_focus, 8]);
                    AutoItHelper.AutoItX.Send("{F8}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void btnF9_Click(object sender, EventArgs e)
        {
            try
            {
                if (Fxp[tx_focus, 9] != 0)
                {
                    tx_lights();
                    msg.bringAppToFront(Fxp[tx_focus, 0]);
                    msg.bringAppToFront(Fxp[tx_focus, 9]);
                    AutoItHelper.AutoItX.Send("{F9}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void btnF10_Click(object sender, EventArgs e)
        {
            try
            {
                if (Fxp[tx_focus, 0] != 0)
                {
                    tx_lights();
                    msg.bringAppToFront(Fxp[tx_focus, 0]);
                    msg.bringAppToFront(Fxp[tx_focus, 10]);
                    AutoItHelper.AutoItX.Send("{F10}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void btnF11_Click(object sender, EventArgs e)
        {
            try
            {
                if (Fxp[tx_focus, 11] != 0)
                {
                    tx_lights();
                    msg.bringAppToFront(Fxp[tx_focus, 0]);
                    msg.bringAppToFront(Fxp[tx_focus, 11]);
                    AutoItHelper.AutoItX.Send("{F11}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void btnF12_Click(object sender, EventArgs e)
        {
            try
            {
                if (Fxp[tx_focus, 12] != 0)
                {
                    tx_lights();
                    msg.bringAppToFront(Fxp[tx_focus, 0]);
                    msg.bringAppToFront(Fxp[tx_focus, 12]);
                    AutoItHelper.AutoItX.Send("{F12}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        #endregion

        #region MorseRunner keys

        private void btnStartMR_Click(object sender, EventArgs e)
        {
            try
            {
                if (!mrIsRunning)
                {
                    btnStartMR.BackColor = Color.Gold;
                    btnStopMR.BackColor = Color.LightSkyBlue;
                    mrIsRunning = true;
                    Audio.callback_return = 0;
                    if (cwDecoder.AudioEvent == null)
                        cwDecoder.AudioEvent = new AutoResetEvent(false);
                    Audio.Start();
                    EnsureRemoteWindow();
                    tx_focus = 0;
                    if (Fxp[tx_focus, 0] == 0)
                    {
                        Console.WriteLine("Error radio 1");
                        return;
                    }
                    else if (Fxp[1, 0] != 0)
                    {
                        tx_focus = 1;
                        tx_change();
                        rx_lights();
                    }
                    cwDecoder.CWdecodeStart();
                    Console.WriteLine("Started");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("N1MM Logger not running?\n" + ex.ToString());
            }
        }

        private void Clearbtns()
        {

            btnStartMR.BackColor = Color.LightBlue;
            btnStopMR.BackColor = Color.White;
            rxa.BackColor = Color.White;
            rxb.BackColor = Color.White;
            btnStopMR.BackColor = Color.White;
            txa.BackColor = Color.White;
            txb.BackColor = Color.White;
            btnSendCall.BackColor = Color.White;
            btnSendRST.BackColor = Color.White;
            btnSendNr.BackColor = Color.White;
            btncall.BackColor = Color.White;
            btnrst.BackColor = Color.White;
            btnnbr.BackColor = Color.White;
        }

        private void btnStopMR_Click(object sender, EventArgs e)
        {
            try
            {
                if (mrIsRunning)
                {
                    Clearbtns();
                    mrIsRunning = false;
                    Audio.callback_return = 2;
                    cwDecoder.CWdecodeStop();
                    Audio.StopAudio();
                    Thread.Sleep(100);
                    cwDecoder.AudioEvent.Close();
                    cwDecoder.AudioEvent = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("N1MM Logger not running?\n" + ex.ToString());
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void setupMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (SetupForm != null)
                    SetupForm.Show();
                else
                {
                    SetupForm = new Setup(this);
                    SetupForm.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening Setup!\n" + ex.ToString());
            }
        }
/*
        private bool raem_check(String testValue)
        {
            Regex _raem = new Regex("^(([0-9])|([0-8][0-9])|(90))[NS]((180)|(1[0-7][0-9])|([0-9][0-9])|([0-9]))[OW]$");
            return (_raem.IsMatch(testValue));
        }
 */

        int i = 0;

        public void btnSendCall_Click(object sender, EventArgs e)      // button call
        {
            i++;
            switch (i)
            {
                case 1:
                    textBox1.Text = txtCall.Text;
                    break;
                case 2:
                    textBox2.Text = txtCall.Text;
                    break;
                case 3:
                    textBox3.Text = txtCall.Text;
                    break;
                case 4:
                    i = 0;
                    break;
            }

            if (tx_focus != 0) { tx_change(); }
            else { tx_lights(); }
            if (Fxp[tx_focus, 13] != 0)
            {
                msg.bringAppToFront(Fxp[tx_focus, 0]);
                msg.bringAppToFront(Fxp[tx_focus, 13]);
                AutoItHelper.AutoItX.Send(txtCall.Text + "{ENTER}");
            }
        }

        int j = 3;

        public void btncall_Click(object sender, EventArgs e)      // button rcall
        {
            j++;
            switch (j)
            {
                case 4:
                    textBox4.Text = txtrCall.Text;
                    break;
                case 5:
                    textBox5.Text = txtrCall.Text;
                    break;
                case 6:
                    textBox6.Text = txtrCall.Text;
                    break;
                case 7:
                    j = 3;
                    break;
            }

            if (tx_focus == 0) { tx_change(); }
            else { tx_lights(); }
            if (Fxp[tx_focus, 13] != 0)
            {
                msg.bringAppToFront(Fxp[tx_focus, 0]);
                msg.bringAppToFront(Fxp[tx_focus, 13]);
                AutoItHelper.AutoItX.Send(txtrCall.Text + "{ENTER}");
            }
        }

        public void btnSendRST_Click(object sender, EventArgs e)       // button RST
        {
            try
            {
                if (tx_focus != 0) { tx_change(); }
                else { tx_lights(); }
                if (Fxp[tx_focus, 16] != 0)
                {
                    msg.bringAppToFront(Fxp[tx_focus, 0]);
                    msg.bringAppToFront(Fxp[tx_focus, 16]);
//                    if (raem_check(txtRST.Text))
                        AutoItHelper.AutoItX.Send(txtRST.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void btnrst_Click(object sender, EventArgs e)       // button rRST
        {
            try
            {
                if (tx_focus == 0) { tx_change(); }
                else { tx_lights(); }
                if (Fxp[tx_focus, 16] != 0)
                {
//                    string s = txtrRST.Text.Substring(1, 1);
                    msg.bringAppToFront(Fxp[tx_focus, 0]);
                    msg.bringAppToFront(Fxp[tx_focus, 16]);
                    AutoItHelper.AutoItX.Send(txtrRST.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void btnSendNr_Click(object sender, EventArgs e)        // button Nr
        {
            try
            {
                if (tx_focus != 0) { tx_change(); }
                else { tx_lights(); }
                if (Fxp[tx_focus, 17] != 0)
                {
                    msg.bringAppToFront(Fxp[tx_focus, 0]);
                    msg.bringAppToFront(Fxp[tx_focus, 17]);
                    AutoItHelper.AutoItX.Send(txtNr.Text + "{ENTER}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void btnnbr_Click(object sender, EventArgs e)        // button Nr
        {
            try
            {
                if (tx_focus == 0) { tx_change(); }
                else { tx_lights(); }
                if (Fxp[tx_focus, 17] != 0)
                {
                    msg.bringAppToFront(Fxp[tx_focus, 0]);
                    msg.bringAppToFront(Fxp[tx_focus, 17]);
                    AutoItHelper.AutoItX.Send(txtrNbr.Text + "{ENTER}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }


        public void btnleft_Click(object sender, EventArgs e)       // button RST
        {
            try
            {
                if (tx_focus != 0) { tx_change(); }
                else { tx_lights(); }
                rx_lights();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void btnright_Click(object sender, EventArgs e)       // button RST
        {
            try
            {
                if (tx_focus == 0) { tx_change(); }
                else { tx_lights(); }
                rx_lights();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void butesc_Click(object sender, EventArgs e)
        {
            try
            {
                if (Fxp[tx_focus, 0] != 0)
                {
                    msg.bringAppToFront(Fxp[tx_focus, 0]);
                    msg.bringAppToFront(Fxp[tx_focus, 18]);
                    AutoItHelper.AutoItX.Send("{ESCAPE}");
                    rx_lights();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void txtNr_KeyUp(object sender, KeyEventArgs e)     // Nr + enter
        {
            if (e.KeyCode == Keys.Enter)
                btnSendNr_Click(null, null);
        }

        private void txtRST_KeyUp(object sender, KeyEventArgs e)    // RST + enter
        {
            if (e.KeyCode == Keys.Enter)
                btnSendRST_Click(null, null);
        }

        private void txtCall_KeyUp(object sender, KeyEventArgs e)   // Call + enter
        {
            if (e.KeyCode == Keys.Enter)
                btnSendCall_Click(null, null);
        }
        private void txtrNbr_KeyUp(object sender, KeyEventArgs e)     // Nr + enter
        {
            if (e.KeyCode == Keys.Enter)
                btnnbr_Click(null, null);
        }

        private void txtrRST_KeyUp(object sender, KeyEventArgs e)    // RST + enter
        {
            if (e.KeyCode == Keys.Enter)
                btnrst_Click(null, null);
        }

        private void txtrCall_KeyUp(object sender, KeyEventArgs e)   // Call + enter
        {
            if (e.KeyCode == Keys.Enter)
                btncall_Click(null, null);
        }

        public void CWExpert_KeyUp(object sender, KeyEventArgs e)      // function keys F1...F12
        {

            switch (e.KeyCode)
            {
                case Keys.F1:
                    btnF1_Click(null, null);
                    break;
                case Keys.F2:
                    btnF2_Click(null, null);
                    break;
                case Keys.F3:
                    btnF3_Click(null, null);
                    break;
                case Keys.F4:
                    btnF4_Click(null, null);
                    break;
                case Keys.F5:
                    btnF5_Click(null, null);
                    break;
                case Keys.F6:
                    btnF6_Click(null, null);
                    break;
                case Keys.F7:
                    btnF7_Click(null, null);
                    break;
                case Keys.F8:
                    btnF8_Click(null, null);
                    break;
                case Keys.F9:
                    btnF9_Click(null, null);
                    break;
                case Keys.F10:
                    btnF10_Click(null, null);
                    break;
                case Keys.F11:
                    btnF11_Click(null, null);
                    break;
                case Keys.F12:
                    btnF12_Click(null, null);
                    break;
            }
        }

        #endregion

        private void CWExpert_Load(object sender, EventArgs e)
        {

        }

        private void txtrCall_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtrRST_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtrNbr_TextChanged(object sender, EventArgs e)
        {

        }

        private void butdva_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (so2r)
                {
                    so2r = false;
                    butdva.BackColor = Color.White;
                }
                else
                {
                    so2r = true;
                    butdva.BackColor = Color.LightPink;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void txtRST_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
