using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;


namespace CWExpert
{
    public class CWDecode
    {
        #region variable

        delegate void CrossThreadCallback(string command, string data);

        public double lm = 0;
        public double rm = 0;
        public bool cqing = false;
        public int rx_focus = 0;
        public int rcvd = 0;
        public int moni = 12;
        public int ponovi = 12;
        public int repeat = 0;
        public int z = 0;
        public int loopend = 0;
        public int totalsamples = 0;
        public int logf2l = 0;
        public const int rate = 8000;
        public const int FFTlen = 64;
        public const int F2L = 128;
        public const int noise = 8;
        public const int aver = 10;
        public const int bwl = 4;
        public const int bwh = 20;
        public bool run_thread = false;
        public Thread CWThread;
        public AutoResetEvent AudioEvent;
        public short[] read_buffer_l;
        public short[] read_buffer_r;
        public short right = 0;
        public short left = 0;
        public char ch = (char)0;
        public bool key = false;
        public bool nr_agn = false;
        public bool call_found = false;
        public bool rprt_found = false;
        public bool transmit = false;
        public int serial = 0;
        public string mystr = new string(' ', 64);
        public string mycall = new string(' ', 14);
        public string rst = new string(' ', 3);
        public string report = new string(' ', 4);
        public string call_sent = new string(' ', 14);
        public string call = new string(' ', 14);
        public string nmbr = new string(' ', 7);
        public string morse = new string(' ', 64);
        public string[] output = new string[F2L];
        public int[] sum = new int[F2L];
        public int[] ave = new int[F2L];
        public double[] RealF = new double[F2L];
        public double[] ImagF = new double[F2L];
        public double[] Mag = new double[F2L];
        public double[,] Num = new double[F2L, 4];
        public double[] Maxi = new double[F2L];
        public double[] prag = new double[F2L];
        public int[] kanal = new int[F2L];
        public int[] broj = new int[F2L];
        public double[] si = new double[F2L];
        public double[] co = new double[F2L];
        public double[] wd = new double[F2L];
        public double[] peaks = new double[F2L];
        public int[] tim = new int[F2L];
        public double thd = 0;
        private double Period = 0.0f;
        public int t = 0;
        public int ctr = 0;
        public int tx_timer = 0;
        public int cq_timer = 1;
        public int dotmin = 0;
        public byte[] bitrev = new byte[F2L];
        public short[] old1 = new short[F2L];
        public short[] old2 = new short[F2L];
        public bool[] keyes = new bool[F2L];
        char[] delimiterChars = { ' ', '-' };

        private CWExpert MainForm;

        #endregion

        #region constructor and destructor

        public CWDecode(CWExpert mainForm)
        {
            try
            {
                MainForm = mainForm;
                read_buffer_l = new short[2048];
                read_buffer_r = new short[2048];
                AudioEvent = new AutoResetEvent(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        ~CWDecode()
        {
        }

        #endregion


        #region crossthread

        private void CrossThreadCommand(string action, string data)
        {
            try
            {
                //  Select an action to perform on the form:

                switch (action)
                {
                    case "Send CALL":
                        {
                            MainForm.txtCALL = data;
                            MainForm.btnSendCall_Click(null, null);
                        }
                        break;

                    case "Escape":
                        {
                            MainForm.butesc_Click(null, null);
                        }
                        break;

                    case "RXLights":
                        {
                            MainForm.rx_lights();
                        }
                        break;

                    case "TXLights":
                        {
                            MainForm.tx_lights();
                        }
                        break;

                    case "Turn_off_lights":
                        {
                            MainForm.turn_off();
                        }
                        break;

                    case "Send RST":
                        {
                            MainForm.txtRst = data;
                            MainForm.btnSendRST_Click(null, null);
                        }
                        break;

                    case "Send NR":
                        {
                            MainForm.txtNR = data;
                            MainForm.btnSendNr_Click(null, null);
                        }
                        break;

                    case "Send rCALL":
                        {
                            MainForm.txtrCALL = data;
                            MainForm.btncall_Click(null, null);
                        }
                        break;

                    case "Send rRST":
                        {
                            MainForm.txtrRst = data;
                            MainForm.btnrst_Click(null, null);
                        }
                        break;

                    case "Send rNR":
                        {
                            MainForm.txtrNR = data;
                            MainForm.btnnbr_Click(null, null);
                        }
                        break;

                    case "Send F1":
                        {
                            MainForm.btnF1_Click(null, null);
                        }
                        break;

                    case "Send F2":
                        {
                            MainForm.btnF2_Click(null, null);
                        }
                        break;

                    case "Send F3":
                        {
                            MainForm.btnF3_Click(null, null);
                        }
                        break;
                    case "Send F5":
                        {
                            MainForm.btnF5_Click(null, null);
                        }
                        break;
                    case "Send F8":
                        {
                            MainForm.btnF8_Click(null, null);
                        }
                        break;

                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex + "\n\n" + ex.StackTrace.ToString());
            }
        }

        #endregion

        public void CW_Thread()
        {
            try
            {
                ctr = 0;
                while (run_thread)
                {
                    AudioEvent.WaitOne();

                    FFTSpectra();

                    if (!transmit || (MainForm.tx_focus != 0))
                    {
                        rx_focus = 0;
                        Sig2Sym(0);
                        Analyse(0);
                        SO2Rx();
                    }

                    if (MainForm.so2r && ((!transmit) || (MainForm.tx_focus == 0)))
                    {
                        rx_focus = 1;
                        Sig2Sym(FFTlen);
                        Analyse(FFTlen);
                        SO2Rx();
                    }
                    if (transmit) { TRtiming(); }

                    MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Turn_off_lights", "");

                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex.ToString());
                run_thread = false;
            }
        }

        public bool CWdecodeStart()
        {
            bool result = false;

            if (Init())
            {
                run_thread = true;
                CWThread = new Thread(new ThreadStart(CW_Thread));
                CWThread.Name = "CW Thread";
                CWThread.Priority = ThreadPriority.Normal;
                CWThread.IsBackground = true;
                CWThread.Start();
            }

            return result;
        }

        public void CWdecodeStop()
        {
            run_thread = false;
            AudioEvent.Set();
        }

        public bool Init()
        {

            int n = 0;
            int n1 = 0;
            int w = 0;
            int x = 0;
            int y = 0;
            int z = 0;

            try
            {
                totalsamples = F2L;
                logf2l = (int)(Math.Round(Math.Log(totalsamples) / Math.Log(2.0)));
                Period = (double)totalsamples / (double)rate;
                for (n = 0; n < totalsamples; n++)
                {
                    x = n;
                    y = 0;
                    n1 = totalsamples;
                    for (w = 1; w <= logf2l; w++)
                    {
                        n1 = n1 >> 1;
                        if (x >= n1)
                        {
                            if (w == 1)
                                y++;
                            else
                                y = y + (2 << (w - 2));
                            x = x - n1;
                        }
                    }
                    bitrev[n] = (byte)y;
                }
                dotmin = (int)Math.Round(0.03 / Period);  // 40 wpm, 30 msec dot
                morse = "ETIANMSURWDKGOHVF*L*PJBXCYZQ**54*3***2 ******16*/*****7***8*90**";
                for (n = 0; n < F2L; n++)
                {
                    peaks[n] = 0;
                    prag[n] = noise;
                    ave[n] = aver;
                    sum[n] = 0;
                    tim[n] = 1;
                    old1[n] = 0;
                    old2[n] = 0;
                    keyes[n] = false;
                    output[n] = "";
                    broj[n] = 0;
                    for (z = 0; z < 4; z++)
                        Num[n, z] = 0;
                }
                double v = 2 * Math.PI / totalsamples;
                for (n = 0; n < totalsamples; n++)
                {
                    RealF[n] = 0;
                    ImagF[n] = 0;
                    si[n] = -Math.Sin(n * v);
                    co[n] = Math.Cos(n * v);
                    wd[n] = (1 + co[n]) / F2L;
                }

                mycall = "S56A";
                call_sent = mycall;
                call_found = false;
                rprt_found = false;
                nr_agn = false;
                tx_timer = 0;
                cq_timer = 1;
                transmit = false;
                serial = 1;

                return true;
            }
            catch (Exception ex)
            {
                Debug.Write(ex.ToString());
                return false;
            }
        }

        private void cw2asc()
        {
            if (sum[z] < 63)
            {
                ch = morse[sum[z] - 1];
                cq_timer = ponovi;
            }
            else if (sum[z] == 75) { ch = '?'; }
            else { ch = '*'; }
            output[z] += ch;
            sum[z] = 0;
            Debug.Write(ch);
        }

        private void CWdecode()
        {
            try
            {
                keyes[z] = key;
                t = ctr - tim[z];
                tim[z] = ctr;
                if (key)
                {
                    Debug.Write("=");
                    if (t > ave[z])
                    {
                        if (sum[z] > 0) { cw2asc(); }
                    }
                    else if (t > dotmin)
                    {
                        ave[z] = t + (ave[z] / 2);
                        sum[z] = 2 * sum[z];
                        if (sum[z] > 255) { sum[z] = 0; }
                    }
                }
                else
                {
                    if (t > ave[z]) { sum[z] += 2; }
                    else if (t > dotmin)
                    {
                        ave[z] = t + (ave[z] / 2);
                        sum[z]++;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex.ToString());
            }
        }

        private void Sig2Sym(int baza)
        {
            for (z = bwl + baza; z <= bwh + baza; z++)
            {
                thd = (prag[z] + noise) / 2;
                if (thd < noise) { thd = noise; }
                int b = broj[z];
                if (Mag[z] > thd)
                {
                    if (b < 3) { b++; }
                }
                else if (b > 0) { b--; }
                broj[z] = b;
                if (b > 1) { key = true; }
                else { key = false; }
                if (key != keyes[z]) 
                { 
                    CWdecode();
                    
                }
                else if (!key)
                {
                    Debug.Write("_");
                    if ((ctr - tim[z]) == (2 * ave[z]))
                    {
                        if (sum[z] > 0)   { cw2asc(); }
                        if (output[z].Length >= 2) { output[z] += " "; }
                    }
                    else if ((ctr - tim[z]) == (4 * ave[z]))
                    {
                        if (sum[z] > 0) { cw2asc(); }
                        if (output[z].Length >= 3) { output[z] += " -"; }
                    }
                }
            }
        }

        private void CallFFT()
        {
            int i, k, m, mx, I1, I2, I3, I4, I5, x;
            double A1, A2, B1, B2, Z1, Z2;
            I1 = totalsamples / 2;
            I2 = 1;
            for (i = 1; i <= logf2l; i++)
            {
                I3 = 0;
                I4 = I1;
                for (k = 1; k <= I2; k++)
                {
                    x = I3 / I1;
                    I5 = bitrev[x];
                    Z1 = co[I5];
                    Z2 = si[I5];
                    loopend = I4 - 1;
                    for (m = I3; m <= loopend; m++)
                    {
                        A1 = RealF[m];
                        A2 = ImagF[m];
                        mx = m + I1;
                        B1 = Z1 * RealF[mx] - Z2 * ImagF[mx];
                        B2 = Z2 * RealF[mx] + Z1 * ImagF[mx];
                        RealF[m] = (A1 + B1);
                        ImagF[m] = (A2 + B2);
                        RealF[mx] = (A1 - B1);
                        ImagF[mx] = (A2 - B2);
                    }
                    I3 = I3 + (I1 << 1);
                    I4 = I4 + (I1 << 1);
                }
                I1 = I1 >> 1;
                I2 = I2 << 1;
            }
        }

        private double Median(double mag)
        {

            Num[z, 1] = Num[z, 2]; Num[z, 2] = Num[z, 3]; Num[z, 3] = mag;

            if ((Num[z, 1] >= Num[z, 2]) && (Num[z, 2] >= Num[z, 3]))
                Num[z, 0] = Num[z, 2];
            else if ((Num[z, 1] <= Num[z, 2]) && (Num[z, 2] <= Num[z, 3]))
                Num[z, 0] = Num[z, 2];
            else if ((Num[z, 1] <= Num[z, 2]) && (Num[z, 2] >= Num[z, 3]) && (Num[z, 1] <= Num[z, 3]))
                Num[z, 0] = Num[z, 3];
            else if ((Num[z, 1] <= Num[z, 2]) && (Num[z, 2] >= Num[z, 3]) && (Num[z, 1] >= Num[z, 3]))
                Num[z, 0] = Num[z, 1];
            else if ((Num[z, 1] >= Num[z, 2]) && (Num[z, 2] <= Num[z, 3]) && (Num[z, 1] >= Num[z, 3]))
                Num[z, 0] = Num[z, 3];
            else
                Num[z, 0] = Num[z, 1];

            return Num[z, 0];
        }

        private void Spectra()
        {

            int n = 0;
            double[] im0 = new double[F2L];
            double[] re0 = new double[F2L];
            double[] re1 = new double[F2L];
            double[] im1 = new double[F2L];
            double[] im2 = new double[F2L];
            double[] re2 = new double[F2L];

            for (n = bwl - 1; n <= bwh + 1; n++)
            {
                z = bitrev[n];
                re0[n] = RealF[z];
                im0[n] = ImagF[z];

                z = bitrev[F2L - n];
                re0[F2L - n] = RealF[z];
                im0[F2L - n] = ImagF[z];

                re1[n] = (re0[n] + re0[F2L - n]) / 2;
                im1[n] = (im0[n] - im0[F2L - n]) / 2;
                re2[n] = (im0[n] + im0[F2L - n]) / 2;
                im2[n] = (re0[F2L - n] - re0[n]) / 2;

                z = n;
                Mag[z] = Median(Math.Sqrt((re1[n] * re1[n]) + (im1[n] * im1[n])));
                prag[z] = (31 * prag[z] + Mag[z]) / 32;
                peaks[z] += Mag[z];
                if (prag[z] > lm)
                {
                    lm = prag[z];
                    rm = z;
                }

                z = n + FFTlen;
                Mag[z] = Median(Math.Sqrt((re2[n] * re2[n]) + (im2[n] * im2[n])));
                prag[z] = (31 * prag[z] + Mag[z]) / 32;
                peaks[z] += Mag[z];
                if (prag[z] > lm)
                {
                    lm = prag[z];
                    rm = z;
                }
            }

        }

        private void FFTSpectra()
        {
            try
            {
                int n = 0;
                int indeks = 0;
                lm = 0;
                while (indeks < read_buffer_l.Length)
                {
                    for (n = 0; n < FFTlen; n++)
                    {
                        left = read_buffer_l[indeks + n];
                        right = read_buffer_r[indeks + n];

                        ImagF[n + FFTlen] = wd[n + FFTlen] * old2[n];
                        old2[n] = right;
                        ImagF[n] = wd[n] * old2[n];

                        RealF[n + FFTlen] = wd[n + FFTlen] * old1[n];
                        old1[n] = left;
                        RealF[n] = wd[n] * old1[n];
                    }
                    CallFFT();
                    Spectra();
                    indeks += FFTlen;
                    ctr++;  // 8 msec counter for dot and dash timing
                }
                Debug.WriteLine(ctr.ToString() + "  " + Math.Truncate(lm).ToString() + "  " + Math.Truncate(rm).ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void Report_Filter(string stev)
        {
            int number;
            string nmbr;
            try
            {
                int i = stev.IndexOf("NN");
                if (i == 0) { nmbr = stev.Insert(0, "5"); }
                else if ((i >= 1) && (i <= 3)) { nmbr = "5" + stev.Substring(i, stev.Length - i); }
                else if (stev.StartsWith("R5")) { nmbr = stev.Remove(0, 1); }
                else if (stev.StartsWith("TU5")) { nmbr = stev.Remove(0, 2); }
                else if (stev.StartsWith("N")) { nmbr = stev.Insert(0, "59"); }
                else nmbr = stev;
                nmbr = nmbr.Replace("N", "9");
                nmbr = nmbr.Replace("O", "0");
                nmbr = nmbr.Replace("T", "0");
                nmbr = nmbr.Replace("A", "1");
                nmbr = nmbr.Replace("E", "5");

                rcvd = 0;
                rst = nmbr.Substring(0, 3);
                if (Int32.TryParse(rst, out number))
                {
                    if ((number == 559) || (number == 569) || (number == 579) || (number == 589) || (number == 599))
                    {
                        rprt_found = true;
                        report = nmbr.Substring(3, nmbr.Length - 3);
                        //  if (number != 599)
                        //      MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send RST", rst);
                    }
                }
                else
                {
                    report = nmbr;
                    rst = "599";
                }
                if (Int32.TryParse(report, out number))
                {
                    if ((number < 41) && (number > 0))
                    {
                        rprt_found = true;
                        rcvd = number;
                    }
                    else rprt_found = false;
                }
                else rprt_found = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

        private void Call_Filter(string znak)
        {
            string clsn = znak;
            if (clsn.StartsWith("DE")) { clsn = clsn.Remove(0, 2); }
            int i = clsn.IndexOf("/");
            if ((!clsn.Contains(mycall)) && (!clsn.Contains(call_sent)) && (!clsn.Contains("?")) && (!clsn.Contains("*")))
            {
                if (((i > 1) && (i < clsn.Length - 1))
                || ((Char.IsLetter(clsn, clsn.Length - 1)) && ((char.IsDigit(clsn, 1)) || (char.IsDigit(clsn, 2)))))
                {
                    call_found = true;
                    rprt_found = false;
                    call = clsn;
                }
            }
        }

        private void Analyse(int osnova)
        {
            try
            {
                z = bwl + osnova;
                do
                {
                    string text = output[z];
                    if (text.Contains("-"))
                    {
                        string[] words = text.Split(delimiterChars);
                        output[z] = " ";
                        foreach (string mystr1 in words)
                        {
                            mystr = mystr1;

                            if ((((mystr.Length) % 2) == 0) && (mystr.Length > 6))
                            {
                                string s1 = mystr.Substring(0, mystr.Length / 2);
                                string s2 = mystr.Substring(mystr.Length / 2, mystr.Length / 2);
                                if (s1.Equals(s2))
                                    mystr = s1;
                            }

                            Debug.WriteLine(ctr.ToString() + "  " + z.ToString() + "  " + mystr);

                            if ((mystr.Contains("NR?")) || (mystr.Contains("AGN"))) { nr_agn = true; }
                            if ((!transmit) && (!nr_agn) && (mystr.Length > 3) && (mystr.Length < 15)
                                && (peaks[z] > peaks[z - 1]) && (peaks[z] > peaks[z + 1]))
                            {
                                Report_Filter(mystr);
                                if (!rprt_found) { Call_Filter(mystr); }
                                Debug.WriteLine(ctr.ToString() + "  " + z.ToString() + "  " + mystr);
                            }
                        }
                    }
                    z++;
                }
                while ((z <= bwh + osnova) && (!call_found) && (!rprt_found) && (!nr_agn));
                if (z <= bwh + osnova)
                {
                    cq_timer = ponovi;
                    for (z = bwl + osnova - 1; z <= bwh + osnova + 1; z++)
                    {
                        peaks[z] = 0;
                        output[z] = " ";
                        prag[z] = noise;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public int f2len()
        {
            string snr = "000";
            if (serial >= 1000)
                snr = serial.ToString();
            else
                snr = serial.ToString("000");
            snr = snr.Replace('0', 'T');
            snr = snr.Replace('9', 'N');

            return txdots(" 5NN" + snr);
        }

        public int txdots(String message)
        {
            //  int[] cwlen = {4,6,6,8,8,10,8,10,10,12,10,12,12,14,10,12,12,0,12,0,14,16,12,14,14,16,14,16,0,0,12,14,0,16,0,0,0,18,0,0,0,0,0,0,0,20,14,0,16,0,0,0,0,0,16,0,0,0,18,0,20,22,0,0};
            int[] cwnrs = { 22, 20, 18, 16, 14, 12, 14, 16, 18, 20 };
            int[] cwltr = { 8, 12, 14, 10, 4, 12, 12, 10, 6, 16, 12, 12, 10, 8, 14, 14, 16, 10, 8, 6, 10, 12, 12, 14, 16, 14 };

            int i = 0;
            int tx_len = 0;

            transmit = true;
            cq_timer = ponovi;

            MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "TXLights", "");

            do
            {
                char c = message[i];
                if (Char.IsLetter(c))
                    tx_len += cwltr[c.CompareTo('A')];
                else if (Char.IsDigit(c))
                    tx_len += cwnrs[c.CompareTo('0')];
                else if (c.Equals(' '))
                    tx_len += 4;
                else if (c.Equals('/'))
                    tx_len += 16;
                else if (c.Equals('?'))
                    tx_len += 18;
                else if (!c.Equals('*'))
                    Debug.Write(".");
                i++;
            }
            while (i < message.Length);

            i = (tx_len / 8);

            return i++;
        }

        private void TRtiming()
        {
            //            int n = 0;
            try
            {
                cq_timer = ponovi;
                if (tx_timer > 0)
                {
                    tx_timer--;
                    if (tx_timer == 0)
                    {
                        transmit = false;
                        MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "RXLights", "");
                        /*
                        for (n = bwl - 1; n <= bwh + 1; n++)
                        {
                            output[n] = " ";
                            peaks[n] = 0;
                            prag[n] = noise;
                        }
                         */
                        call_found = false;
                        rprt_found = false;
                        cq_timer = ponovi;
                        cqing = false;
                        Debug.WriteLine("RX");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void SO2Rx()
        {
            try
            {
                if (!transmit && (cq_timer > 0)) { cq_timer--; }
                /*
                                if ((!transmit) &&(cq_timer == 0))
                                {
                    
                                    if ((call_sent.Equals(mycall)) || (repeat > 1))
                                    {
                                        tx_timer = txdots("CQ ") + txdots(mycall) + txdots(" TEST");
                                        MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send F1", "");
                                        repeat = 0;
                                        cqing = MainForm.so2r;
                                        Debug.WriteLine("CQ");
                                    }
                                    else
                                    {
                                        tx_timer = txdots(call) + f2len();
                                        MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send F5", "");
                                        MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send F2", "");
                                        repeat++;
                                    }
                     
                                }
                */
                if (call_found)
                {
                    if (cqing)
                    {
                        MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Escape", "");
                        tx_timer = 1;
                        TRtiming();
                    }
                    if (rprt_found) { tx_timer = txdots(call + " ? "); }
                    else { tx_timer = txdots(call) + f2len(); }
                    if (rx_focus != 0)
                        MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send rCALL", call);
                    else
                        MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send CALL", call);
                    call_sent = call;
                    call_found = false;
                    Debug.WriteLine("CL");
                }

                if (rprt_found)
                {
                    tx_timer += txdots("TU");
                    if (rx_focus != 0)
                        MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send rNR", rcvd.ToString());
                    else
                        MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send NR", rcvd.ToString());
                    rprt_found = false;
                    call_sent = mycall;
                    serial++;
                    Debug.WriteLine("NR");
                }

                if (nr_agn)
                {
                    tx_timer = f2len();
                    MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send F2", "");
                    nr_agn = false;
                    Debug.WriteLine("AGN");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

    }
}
