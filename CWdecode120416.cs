using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;


namespace CWExpert
{
    public class CWDecode
    {
        #region variable

        delegate void CrossThreadCallback(string command, string data);

        public bool once = true;
        public int qrg = 0;
        public bool swl = false;
        public bool usa = false;
        public int moni0 = 12;
        public int moni1 = 76;
        public const int ponovi = 64;
        public bool repeat = false;
        public int loopend = 0;
        public int logf2l = 0;
        public int agc = 32;
        public const int rate = 8000;
        public const int F2L = 128;
        public int totalsamples = F2L;
        public const int FFTlen = F2L / 2;
        public const int nofs = 32;
        public double thld0 = 1024;
        public double thld1 = 1024;
        public const int aver = 10;
        public int bwl0 = 3;
        public int bwh0 = 23;
        public int bwl1 = 3 + FFTlen;
        public int bwh1 = 23 + FFTlen;
        public bool run_thread = false;
        public Thread CWThread;
        public AutoResetEvent AudioEvent;
        public ushort[] read_buffer_l;
        public ushort[] read_buffer_r;
        public bool key = false;
        public bool nr_agn = false;
        public bool call_found = false;
        public bool rprt_found = false;
        public bool transmit = false;
        public int serial = 0;
        public string mycall = new string(' ', 14);
        public string rst = new string(' ', 3);
        public string report = new string(' ', 4);
        public string call_sent = new string(' ', 14);
        public string call = new string(' ', 14);
        public string morse = new string(' ', 64);
        public string[] output = new string[F2L];
        public string[] scp = new string[45000];
        public string[] wkd = new string[256];
        public int[] sum = new int[F2L];
        public int[] ave = new int[F2L];
        public double[] Noise = new double[F2L];
        public double[] RealF = new double[F2L];
        public double[] ImagF = new double[F2L];
        public double[,] Mag = new double[F2L, nofs];
        public double[,] Num = new double[F2L, 4];
        public double[] prag = new double[F2L];
        public double[] temp = new double[F2L];
        public double[] si = new double[F2L];
        public double[] co = new double[F2L];
        public double[] wd = new double[F2L];
        public double[] sigs = new double[F2L];
        public int[] tim = new int[F2L];
        private double Period = 0.0f;
        public int ctr = 0;
        public int tx_timer = 0;
        public int rx_timer = ponovi;
        public int dotmin = 1;
        public int[] bitrev = new int[F2L];
        public short[] old1 = new short[F2L];
        public short[] old2 = new short[F2L];
        public bool[] keyes = new bool[F2L];
        public bool[] valid = new bool[F2L];
//        char[] delimiterChars = { ' ', '-' };
//        public bool cqing = false;
        public int ok = 0;

        private CWExpert MainForm;

        #endregion

        #region constructor and destructor

        public CWDecode(CWExpert mainForm)
        {
            try
            {
                MainForm = mainForm;
                read_buffer_l = new ushort[2048];
                read_buffer_r = new ushort[2048];
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
                switch (action)
                {

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

                    case "TXexch":
                        {
                            MainForm.tx_change();
                        }
                        break;

                    case "Send CALL":
                        {
                            MainForm.txtCALL = data;
                            MainForm.btnSendCall_Click(null, null);
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

                    case "Send F4":
                        {
                            MainForm.btnF4_Click(null, null);
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

                    case "Send F11":
                        {
                            MainForm.btnF11_Click(null, null);
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
                    StereoAudio2ASCII();
                    ctr += nofs;
                    if (once)
                        once = false;
 //                       Channel();
                    else if (transmit)
                        TRtiming();
                    else
                    {
                        if (rx_timer > 0)
                            rx_timer--;
                        if (!swl && rx_timer == 0)
                            Timedout();
 //                       if (qrg == 0 && tx_timer == 0)
 //                           cqcqcq();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex.ToString());
                run_thread = false;
            }
        }


        public void Timedout()
        {
            if (repeat)
            {
                if (((qrg < FFTlen) && (MainForm.tx_focus != 0)) || ((qrg > FFTlen) && (MainForm.tx_focus == 0)))
                {
                    MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "TXexch", "");
                }
                tx_timer = txdots(call_sent) + f2len();
                MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send F5", "");
                MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send F2", "");
                repeat = false;
                Debug.Write(" RPT " + tx_timer.ToString());
            }
            else
                cqcqcq();
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
                    bitrev[n] = y;
                }
                dotmin = (int)Math.Round(1.2 / (40 * Period));  // 40 wpm, 30 msec dot
                morse = "ETIANMSURWDKGOHVF*L*PJBXCYZQ**54*3***2 ******16*/*****7***8*90**";
                for (n = 0; n < F2L; n++)
                {
                    temp[n] = 0;
                    sigs[n] = 0;
                    prag[n] = 1024;
                    Noise[n] = 1024;
                    ave[n] = aver;
                    sum[n] = 0;
                    tim[n] = 1;
                    old1[n] = 0;
                    old2[n] = 0;
                    keyes[n] = false;
                    valid[n] = false;
                    output[n] = "";
                    for (z = 0; z < 4; z++)
                        Num[n, z] = 1;
                }
                double v = 2 * Math.PI / totalsamples;
                for (n = 0; n < totalsamples; n++)
                {
                    RealF[n] = 0;
                    ImagF[n] = 0;
                    si[n] = -Math.Sin(n * v);
                    co[n] = Math.Cos(n * v);
                    wd[n] = (0.54 - 0.46 * co[n]) / F2L;
                }
                mycall = MainForm.SetupForm.txtCALL.Text;
                call_sent = mycall;
                call_found = false;
                rprt_found = false;
                nr_agn = false;
                tx_timer = ponovi;
                rx_timer = ponovi;
                transmit = false;
                serial = 15;
                repeat = false;
                if (MainForm.so2r)
                    MainForm.tx_focus = 0;
                SCPLoad();
                wkd[0] = String.Empty;
                once = true;
                return true;
            }
            catch (Exception ex)
            {
                Debug.Write(ex.ToString());
                return false;
            }
        }

        private void SCPLoad()
        {
            int counter = 0;
            string line;

            System.IO.StreamReader file1 =
               new System.IO.StreamReader("MASTER.SCP");
            while ((line = file1.ReadLine()) != null)
            {
                scp[counter] = line;
                counter++;
            }
            Array.Sort(scp);
            file1.Close();
            Debug.WriteLine("SCP " + counter.ToString());
        }

        public int which = 0;
  
        private void Channel()
        {
            int moni = 0;
            int n=0;
            int lower  = bwl0;
            int higher = bwh0;
            double thd = 0;
            double thld = 0;
            bool enkrat = true;

            if (enkrat)
            {
                enkrat = false;

                if (which == 0)
                {
                    lower = bwl0;
                    higher = bwh0;
                }
                else
                {
                    lower = bwl1;
                    higher = bwh1;
                }
            }

            for (n = lower; n <= higher; n++)
                temp[n] += sigs[n];

            if (transmit)
            {
                tx_timer--;
                if (tx_timer == 1)
                {
                    thd = 0;
                    for (n = lower; n <= higher; n++)
                    {
                        tim[n] = 1;
                        if (temp[n] > thd)
                        {
                            thd = (int)temp[n];
                            moni = n;
                        }
                        temp[n] = 0;
                    }

                    if (thd > 2 * Noise[moni])
                    {
                        lower = moni / 2;
                        higher = lower + moni;
                    }
                    else
                        moni = (lower + higher) / 2;

                    if (which == 0)
                    {
                        bwl0 = lower;
                        bwh0 = higher;
                        moni0 = moni;
                    }
                    else
                    {
                        bwl1 = lower;
                        bwh1 = higher;
                        moni1 = moni;
                    }

                    Debug.WriteLine(lower.ToString() + "  " + moni.ToString() + "  " + higher.ToString());

                    if (!(MainForm.so2r && which == 0))
                    {
                        which = 0;
                        once = false;
                        ctr = 0;
                    }
                    else
                    {
                        which = 1;
                        enkrat = true;
                        transmit = false;
                        rx_timer = ponovi;
                        Channel();
                    }
                }
            }
            else
            {
                rx_timer--;
                if (rx_timer == 0)
                {
                    thd = 0;
                    for (n = lower; n <= higher; n++)
                    {
                        temp[n] = temp[n] / ponovi;
                        thd += temp[n];
                    }
                    thd /= higher - lower + 1;
                    thld = (int)(2 * thd);
                    for (n = lower; n <= higher; n++)
                    {
                        Noise[n] = 2 * temp[n];
                        if (Noise[n] < thld)
                            Noise[n] = thld;
                        temp[n] = 0;
                    }

                    if (which == 0)
                    {
                        thld0 = thld;
                        if (MainForm.tx_focus == 1)
                            MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "TXexch", "");
                    }
                    else
                    {
                        thld1 = thld;
                        if (MainForm.tx_focus == 0)
                            MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "TXexch", "");
                    }

                    tx_timer = txdots("?");
                    MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send F8", "");
                }
            }
        }

        private void cw2asc(int z)
        {
            char ch = '*';
            if (sum[z] < 63)
            {
                ch = morse[sum[z] - 1];
                if (char.IsDigit(ch))
                {
                    rx_timer = ponovi;
                    valid[z] = true;
                }
            }
            else if (sum[z] == 75)
                ch = '?';
            output[z] += ch;
            sum[z] = 0;
        }

        private void CWdecode(int z, int t)
        {
            int dt = 0;
            try
            {
                keyes[z] = key;
                dt = t - tim[z];
                tim[z] = t;
                if (key)
                {
                    if (dt > ave[z])
                    {
                        if (sum[z] > 0)
                            cw2asc(z);
                    }
                    else
                    {
                        if (dt > dotmin)
                            ave[z] = dt + (ave[z] / 2);
                        sum[z] = 2 * sum[z];
                        if (sum[z] > 75)
                            sum[z] = 0;
                    }
                }
                else
                {
                    sum[z]++;
                    if (dt > ave[z])
                        sum[z]++;
                    else if (dt > dotmin)
                        ave[z] = dt + (ave[z] / 2);
                }

            }
            catch (Exception ex)
            {
                Debug.Write(ex.ToString());
            }
        }

        private void Sig2Sym(int z)
        {
            for (int t = 0; t < nofs; t++)
            {
                if (Mag[z, t] > prag[z])
                    key = true;
                else
                    key = false;
                if (key != keyes[z]) { CWdecode(z, t + ctr); }
                else if (!key)
                {
                    if ((ctr + t - tim[z]) == (ave[z]))
                    {
                        if (sum[z] > 0) { cw2asc(z); }
                    }
                    else if ((ctr + t - tim[z]) == (2 * ave[z]))
                    {
                        if (valid[z] || output[z].Length > 2)
                            output[z] += " ";
                        else
                            output[z] = "";
                    }
                    else if ((ctr + t - tim[z]) == (3 * ave[z]))
                    {
                        if (output[z].Length > 2)
                        {
                            if (valid[z])
                            {
                                string clsn = output[z].Substring(0, output[z].Length - 1);

//                                if (swl)
                                    Debug.WriteLine((ctr + t).ToString() + "  " + z.ToString() + "  " + Math.Round(prag[z]/Noise[z]).ToString() + "  " + clsn);

                                Call_Filter(clsn);

                                if (call_found)
                                {
                                    int i = Array.BinarySearch(scp, call);
 
                                    if (i > 0)
                                    {
  //                                      call = clsn;
                                        Debug.WriteLine(call + "  " + z.ToString());
                                        if (qrg == 0)
                                            qrg = z;
                                    }
                                    else
                                        call_found = false;
                                }
                                else
                                {
                                    string rprt = output[z].Substring(0, output[z].Length - 1);
                                    Report_Filter(rprt);
                                }
                            }
                            else if (!swl && (z == qrg) && output[z].Contains(" "))
                            {
                                if ((output[z].Contains("NR?")) || (output[z].Contains("AGN"))) { nr_agn = true; }
                            }
                        }
                        output[z] = "";
                        valid[z] = false;
                    }
                }
            }
        }

        private void StereoSpectra(int seq)
        {
            int n, z, l , h;
            double[] im0 = new double[F2L];
            double[] re0 = new double[F2L];
            double[] re1 = new double[F2L];
            double[] im1 = new double[F2L];
            double[] im2 = new double[F2L];
            double[] re2 = new double[F2L];

            if (bwl0 < (bwl1 - FFTlen))
                l = bwl0;
            else
                l = bwl1 - FFTlen;

            if (bwh0 > (bwh1 - FFTlen))
                h = bwh0;
            else
                h = bwh1 - FFTlen;

                for (n = l - 1; n <= h + 1; n++)
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
                    Mag[z, seq] = Median(Math.Sqrt((re1[n] * re1[n]) + (im1[n] * im1[n])), z);
                    sigs[z] += Mag[z, seq] / nofs;

                    z = n + FFTlen;
                    Mag[z, seq] = Median(Math.Sqrt((re2[n] * re2[n]) + (im2[n] * im2[n])), z);
                    sigs[z] += Mag[z, seq] / nofs;
                }

            for (n = bwl0; n <= bwh0; n++)
            {
                //                            prag[n] = ((agc - 1) * prag[n] + sigs[n]) / agc;
                prag[n] = (prag[n] + sigs[n - 1] + sigs[n] + sigs[n + 1]) / 4;
                if (prag[n] < Noise[n])
                    prag[n] = Noise[n];
            }

            for (n = bwl1; n <= bwh1; n++)
            {
                //                            prag[n] = ((agc - 1) * prag[n] + sigs[n]) / agc;
                prag[n] = (prag[n] + sigs[n - 1] + sigs[n] + sigs[n + 1]) / 4;
                if (prag[n] < Noise[n])
                    prag[n] = Noise[n];
            }

        }


        private void StereoAudio2ASCII()
        {
            try
            {
                int n = 0;
                int indeks = 0;
                int seq = 0;

                for (n = bwl0 - 1; n <= bwh1 + 1; n++)
                    sigs[n] = 0;

                while (indeks < read_buffer_l.Length)
                {
                    for (n = 0; n < FFTlen; n++)
                    {
                        short left = (short)read_buffer_l[indeks + n];
                        short right = (short)read_buffer_r[indeks + n];

                        ImagF[n + FFTlen] = wd[n + FFTlen] * old2[n];
                        old2[n] = right;
                        ImagF[n] = wd[n] * old2[n];

                        RealF[n + FFTlen] = wd[n + FFTlen] * old1[n];
                        old1[n] = left;
                        RealF[n] = wd[n] * old1[n];
                    }

                    CalcFFT();

                    StereoSpectra(seq);

                    indeks += FFTlen;

                    seq++;
                }

                if (swl || once || ((qrg == 0 || qrg > FFTlen || !MainForm.so2r) && !(transmit && MainForm.tx_focus == 0)))
                {
                    for (n = bwl0; n <= bwh0; n++)
                        Sig2Sym(n);
                    if (call_found && !swl && !once)
                        Respond(0);
                }

                if (swl || once || (qrg < FFTlen && MainForm.so2r && !(transmit && MainForm.tx_focus == 1)))
                {
                    for (n = bwl1; n <= bwh1; n++)
                        Sig2Sym(n);
                    if (call_found && !swl && !once)
                        Respond(FFTlen);
                }

                if (qrg > 0 && !swl && !once)
                {
                    Sig2Sym(qrg);
                    if (rprt_found)
                        Respond(qrg);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void CalcFFT()
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

        private double Median(double mag, int z)
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

        public void Report_Filter(string stev)
        {
            int number;

            if (stev.Contains("NN") && stev.Length > 3)
            {
                report = stev.Substring(2 + stev.IndexOf("NN"), stev.Length - 2 - stev.IndexOf("NN"));
            }
            else
                report = stev;

            report = report.Replace("N", "9");
            report = report.Replace("O", "0");
            report = report.Replace("T", "0");
            //            report = report.Replace("A", "1");
            //            report = report.Replace("E", "5");

            rst = "599";

            rprt_found = false;

            if (Int32.TryParse(report, out number))
            {
                if (number > 0 && number < 41)
                {
                    rprt_found = true;
                    Debug.WriteLine(rst + report);
                }
            }
        }

        private void Call_Filter(string clsn)
        {

            if (clsn.StartsWith("DE") || clsn.StartsWith("TU")) { clsn = clsn.Remove(0, 2); }
            int i = clsn.IndexOf("/");
            if ((clsn.Length > 2 && !regex_check(clsn) && !clsn.Contains(mycall)) && (!clsn.Contains(call_sent)) && (!clsn.Contains("?")) && (!clsn.Contains("*")))
            {
                if (((i > 1) && (i < clsn.Length - 1))
                || ((Char.IsLetter(clsn, clsn.Length - 1)) && ((char.IsDigit(clsn, 1)) || (char.IsDigit(clsn, 2)))))
                {
                    call_found = true;
                    rprt_found = false;
                    call = clsn;
                }
                else call_found = false;
            }
        }

        private bool regex_check(String testValue)
        {
            Regex _rgx = new Regex("[ ETIANSH5*]");
            return (_rgx.Matches(testValue).Count == testValue.Length);
        }

        private Boolean IsUSA(String anycall)
        {
            Regex rgx1 = new Regex("^[KNW][^HLP]");
            Regex rgx2 = new Regex("^A[A-G,I-K]");
            return (rgx1.IsMatch(anycall) || rgx2.IsMatch(anycall));
        }

        public void Check4QSO(string mystr)
        {
            Report_Filter(mystr);
            if (!rprt_found) { Call_Filter(mystr); }
            Debug.WriteLine(ctr.ToString() + "  " + mystr + "  " + call_found + "  " + rprt_found);
        }

        public int f2len()
        {
            string snr = "15"; // "000";
            if (serial >= 1000)
                snr = serial.ToString();
            else
//                snr = serial.ToString("000");
            snr = snr.Replace('0', 'T');
            snr = snr.Replace('9', 'N');

            return txdots(" 5NN" + snr);
        }

        public int txdots(String message)
        {
            int[] cwnrs = { 22, 20, 18, 16, 14, 12, 14, 16, 18, 20 };
            int[] cwltr = { 8, 12, 14, 10, 4, 12, 12, 10, 6, 16, 12, 12, 10, 8, 14, 14, 16, 10, 8, 6, 10, 12, 12, 14, 16, 14 };

            int i = 0;
            int tx_len = 0;

            transmit = true; // MainForm.run;
            rx_timer = ponovi;
            MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "TXLights", "");

            while (i < message.Length)
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

            i = 5 * tx_len / 32;

            return i++;
        }

        private void TRtiming()
        {

            try
            {
                if (tx_timer > 0)
                {
                    tx_timer--;
                    if (tx_timer == 0)
                    {
                        Debug.WriteLine(" RX");
                        MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "RXLights", "");
                        if (MainForm.tx_focus != 0)
                        {
                            for (int n = bwl1 - 1; n <= bwh1 + 1; n++)
                            {
                                output[n] = String.Empty;
                                keyes[n] = false;
                                prag[n] = Noise[n];
                            }
                        }
                        else
                        {
                            for (int n = bwl0 - 1; n <= bwh0 + 1; n++)
                            {
                                output[n] = String.Empty;
                                keyes[n] = false;
                                prag[n] = Noise[n];
                            }
   
                        }

                        transmit = false;
                        call_found = false;
                        rprt_found = false;
                        rx_timer = ponovi;

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void cqcqcq()
        {
            if (MainForm.so2r)
                MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "TXexch", "");
            tx_timer = txdots("CQ ") + 2 * txdots(mycall) + txdots(" TEST ");
            MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send F1", "");
            repeat = false;
            qrg = 0;
//            cqing = true;
            call_found = false;
            rprt_found = false;
            call_sent = mycall;
            Debug.Write(" CQ " + (ctr / 32).ToString());
        }

        private void Respond(int freq)
        {
            try
            {
                if (call_found)
                {
                    if (rprt_found)
                        tx_timer = txdots(call + " ? ");
                    else
                        tx_timer = txdots(call) + f2len();

//                    MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send F11", "");  // F11 function???

                    if (freq < FFTlen)
                    {
                        MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send CALL", call);
                    }
                    else
                        MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send rCALL", call);

//                    MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send F5", "");
//                    MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send F2", "");
                    call_sent = call;
                    call_found = false;
                    Debug.Write(" CL " + tx_timer.ToString());
                }

                if (rprt_found)
                {
                    if (((freq < FFTlen) && (MainForm.tx_focus != 0)) || ((freq >= FFTlen) && (MainForm.tx_focus == 0)))
                    {
                        MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "TXexch", "");
                    }
                    tx_timer += txdots("TU " + mycall);

                    if (freq < FFTlen)
                    {
                        if (!rst.Equals("599"))
                           MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send RST", rst);
                        MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send NR", report);
                    }
                    else
                    {
                        if (!rst.Equals("599"))
                           MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send rRST", rst);
                        MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send rNR", report);
                    }
//                    MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send F3", "");
//                    MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send F4", "");
                    rprt_found = false;
                    call_sent = mycall;
//                    serial++;
                    qrg = 0;
                    Debug.Write(" NR " + tx_timer.ToString());
                }

                if (nr_agn)
                {
                    if (((freq < FFTlen) && (MainForm.tx_focus != 0)) || ((freq >= FFTlen) && (MainForm.tx_focus == 0)))
                    {
                        MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "TXexch", "");
                    }
                    tx_timer = f2len();
                    MainForm.Invoke(new CrossThreadCallback(CrossThreadCommand), "Send F2", "");
                    nr_agn = false;
                    Debug.Write(" AGN " + tx_timer.ToString());
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}

