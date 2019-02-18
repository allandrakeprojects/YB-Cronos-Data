using CefSharp;
using CefSharp.WinForms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;

namespace YB_Cronos_Data
{
    public partial class Main_Form : Form
    {
        private string __url_01 = "http://103.4.104.8/page/manager/login.jsp";
        private string __url = "";
        private string __start_datetime_elapsed;
        private string __file_location = "\\\\192.168.10.22\\ssi-reporting";
        private int __send = 0;
        private int __timer_count = 10;
        private bool __is_close;
        private bool __is_login = false;
        private bool __is_start = false;
        private bool __is_autostart = true;
        private JObject __jo;
        private JToken __jo_count;
        private ChromiumWebBrowser chromeBrowser;
        List<String> __getdata_affiliatelist = new List<String>();
        Timer timer = new Timer();
        Form __mainFormHandler;
        
        // Drag Header to Move
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        
        // Form Shadow
        private bool m_aeroEnabled;
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse
        );
        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        [DllImport("dwmapi.dll")]
        public static extern int DwmIsCompositionEnabled(ref int pfEnabled);
        private const int CS_DROPSHADOW = 0x00020000;
        private const int WM_NCPAINT = 0x0085;
        private const int WM_ACTIVATEAPP = 0x001C;
        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 0x1;
        private const int HTCAPTION = 0x2;
        private const int WS_MINIMIZEBOX = 0x20000;
        private const int CS_DBLCLKS = 0x8;
        public struct MARGINS
        {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }
        protected override CreateParams CreateParams
        {
            get
            {
                m_aeroEnabled = CheckAeroEnabled();

                CreateParams cp = base.CreateParams;
                if (!m_aeroEnabled)
                    cp.ClassStyle |= CS_DROPSHADOW;

                cp.Style |= WS_MINIMIZEBOX;
                cp.ClassStyle |= CS_DBLCLKS;
                return cp;
            }
        }
        private bool CheckAeroEnabled()
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                int enabled = 0;
                DwmIsCompositionEnabled(ref enabled);
                return (enabled == 1) ? true : false;
            }
            return false;
        }
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_NCPAINT:
                    if (m_aeroEnabled)
                    {
                        var v = 2;
                        DwmSetWindowAttribute(Handle, 2, ref v, 4);
                        MARGINS margins = new MARGINS()
                        {
                            bottomHeight = 1,
                            leftWidth = 0,
                            rightWidth = 0,
                            topHeight = 0
                        };
                        DwmExtendFrameIntoClientArea(Handle, ref margins);

                    }
                    break;
                default:
                    break;
            }
            base.WndProc(ref m);

            if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT)
                m.Result = (IntPtr)HTCAPTION;
        }
        // ----- Form Shadow

        public Main_Form()
        {
            InitializeComponent();

            Opacity = 0;
            timer.Interval = 20;
            timer.Tick += new EventHandler(FadeIn);
            timer.Start();
        }

        private void FadeIn(object sender, EventArgs e)
        {
            if (Opacity >= 1)
            {
                timer_landing.Start();
            }
            else
            {
                Opacity += 0.05;
            }
        }

        private void panel_header_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void label_title_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void panel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void pictureBox_minimize_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void pictureBox_close_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("Exit the program?", "YB Cronos Data", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
                __is_close = false;
                Environment.Exit(0);
            }
        }

        private void panel_yb_Paint(object sender, PaintEventArgs e)
        {
            Rectangle rect = panel_yb.ClientRectangle;
            rect.Width--;
            rect.Height--;
            e.Graphics.DrawRectangle(Pens.LightGray, rect);
        }

        // CefSharp Initialize
        private void InitializeChromium()
        {
            CefSettings settings = new CefSettings();

            settings.CachePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\CEF";
            Cef.Initialize(settings);
            chromeBrowser = new ChromiumWebBrowser(__url_01);
            panel_cefsharp.Controls.Add(chromeBrowser);
            chromeBrowser.AddressChanged += ChromiumBrowserAddressChanged;
        }

        // CefSharp Address Changed
        private void ChromiumBrowserAddressChanged(object sender, AddressChangedEventArgs e)
        {
            __url = e.Address.ToString();
            if (e.Address.ToString().Equals(__url_01))
            {
                if (__is_login)
                {
                    Invoke(new Action(() =>
                    {
                        // comment
                        //label_brand.Visible = false;
                        pictureBox_loader.Visible = false;
                        // comment
                        //label_player_last_bill_no.Visible = false;
                        label_page_count.Visible = false;
                        label_total_records.Visible = false;
                        button_start.Visible = false;
                        //__mainFormHandler = Application.OpenForms[0];
                        //__mainFormHandler.Size = new Size(466, 468);

                        string datetime = DateTime.Now.ToString("dd MMM HH:mm:ss");
                        // comment
                        //SendITSupport("The application have been logout, please re-login again.");
                        //SendMyBot("The application have been logout, please re-login again.");
                        __send = 0;
                        // comment
                        //timer_pending.Stop();
                    }));
                }

                __is_login = false;
                // comment
                //timer.Stop();

                Invoke(new Action(() =>
                {
                    chromeBrowser.FrameLoadEnd += (sender_, args) =>
                    {
                        if (args.Frame.IsMain)
                        {
                            Invoke(new Action(() =>
                            {
                                if (!__is_login)
                                {
                                    args.Frame.ExecuteJavaScriptAsync("document.getElementById('username').value = 'testrain';");
                                    args.Frame.ExecuteJavaScriptAsync("document.getElementById('password').value = 'rain12345';");
                                    args.Frame.ExecuteJavaScriptAsync("window.scrollTo(0,document.body.scrollHeight)");
                                    __is_login = false;
                                    panel_cefsharp.Visible = true;
                                    // comment
                                    //label_player_last_bill_no.Text = "-";
                                    //label_brand.Visible = false;
                                    pictureBox_loader.Visible = false;
                                    label_status.Text = "Logout";
                                    // comment
                                    //label_player_last_bill_no.Visible = false;
                                }
                            }));
                        }
                    };
                }));
            }

            if (e.Address.ToString().Equals("http://103.4.104.8/page/manager/member/search.jsp") || e.Address.ToString().Equals("http://103.4.104.8/page/manager/dashboard.jsp"))
            {
                Invoke(new Action(async () =>
                {
                    // comment
                    //label_brand.Visible = true;
                    pictureBox_loader.Visible = true;
                    // comment
                    //label_player_last_bill_no.Visible = true;
                    label_page_count.Visible = true;
                    label_total_records.Visible = true;
                    button_start.Visible = true;
                    //__mainFormHandler = Application.OpenForms[0];
                    //__mainFormHandler.Size = new Size(466, 168);

                    if (!__is_login)
                    {
                        // comment
                        //timer_pending.Start();
                        __is_login = true;
                        panel_cefsharp.Visible = false;
                        // comment
                        //label_brand.Visible = true;
                        pictureBox_loader.Visible = true;
                        // comment
                        //label_player_last_bill_no.Visible = true;
                        //___PlayerLastBillNo();
                        //await ___GetPlayerListsRequest();
                    }

                    if (!__is_start)
                    {
                        if (Properties.Settings.Default.______start_detect == "0")
                        {
                            button_start.Enabled = false;
                            panel_filter.Enabled = false;
                        }
                        // registration
                        else if (Properties.Settings.Default.______start_detect == "1")
                        {
                            comboBox_list.SelectedIndex = 0;
                            button_start.PerformClick();
                        }
                        // payment
                        else if (Properties.Settings.Default.______start_detect == "2")
                        {
                            comboBox_list.SelectedIndex = 1;
                            button_start.PerformClick();
                        }
                        // bonus
                        else if (Properties.Settings.Default.______start_detect == "3")
                        {
                            comboBox_list.SelectedIndex = 2;
                            button_start.PerformClick();
                        }
                        // turnover
                        else if (Properties.Settings.Default.______start_detect == "4")
                        {
                            comboBox_list.SelectedIndex = 3;
                            button_start.PerformClick();
                        }
                    }
                }));
            }
        }


        private void timer_landing_Tick(object sender, EventArgs e)
        {
            if (!Cef.IsInitialized)
            {
                InitializeChromium();
            }
            
            //panel_landing.Visible = false;
            label_title.Visible = true;
            panel.Visible = true;
            pictureBox_minimize.Visible = true;
            pictureBox_close.Visible = true;
            label_version.Visible = true;
            label_status.Visible = true;
            label_status_1.Visible = true;
            label_cycle_in.Visible = true;
            label_cycle_in_1.Visible = true;
            button1.Visible = true;
            timer_landing.Stop();
        }

        private void Main_Form_Load(object sender, EventArgs e)
        {
            comboBox.SelectedIndex = 0;
            comboBox_list.SelectedIndex = 0;
            dateTimePicker_start.Format = DateTimePickerFormat.Custom;
            dateTimePicker_start.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            dateTimePicker_end.Format = DateTimePickerFormat.Custom;
            dateTimePicker_end.CustomFormat = "yyyy-MM-dd HH:mm:ss";
        }

        private void Main_Form_Shown(object sender, EventArgs e)
        {
            ___GETDATA_AFFILIATELIST();
        }

        private void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox.SelectedIndex == 0)
            {
                // Yesterday
                if (comboBox_list.SelectedIndex == 0)
                {
                    string start = DateTime.Now.ToString("2018-01-22 00:00:00");
                    DateTime datetime_start = DateTime.ParseExact(start, "yyyy-MM-dd 00:00:00", CultureInfo.InvariantCulture);
                    dateTimePicker_start.Value = datetime_start;

                    string end = DateTime.Now.ToString("yyyy-MM-dd 00:00:00");
                    DateTime datetime_end = DateTime.ParseExact(end, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    dateTimePicker_end.Value = datetime_end;
                }
                else if (comboBox_list.SelectedIndex == 1)
                {
                    string start = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd 00:00:00");
                    DateTime datetime_start = DateTime.ParseExact(start, "yyyy-MM-dd 00:00:00", CultureInfo.InvariantCulture);
                    dateTimePicker_start.Value = datetime_start;

                    string end = DateTime.Now.ToString("yyyy-MM-dd 00:00:00");
                    DateTime datetime_end = DateTime.ParseExact(end, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    dateTimePicker_end.Value = datetime_end;
                }
                else
                {
                    string start = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd 00:00:00");
                    DateTime datetime_start = DateTime.ParseExact(start, "yyyy-MM-dd 00:00:00", CultureInfo.InvariantCulture);
                    dateTimePicker_start.Value = datetime_start;

                    string end = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd 00:00:00");
                    DateTime datetime_end = DateTime.ParseExact(end, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    dateTimePicker_end.Value = datetime_end;
                }

            }
            else if (comboBox.SelectedIndex == 1)
            {
                // Last Week
                DayOfWeek weekStart = DayOfWeek.Sunday;
                DateTime startingDate = DateTime.Today;

                while (startingDate.DayOfWeek != weekStart)
                {
                    startingDate = startingDate.AddDays(-1);
                }

                DateTime datetime_start = startingDate.AddDays(-7);
                dateTimePicker_start.Value = datetime_start;

                string last = startingDate.AddDays(-1).ToString("yyyy-MM-dd 00:00:00");
                DateTime datetime_end = DateTime.ParseExact(last, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                dateTimePicker_end.Value = datetime_end;
            }
            else if (comboBox.SelectedIndex == 2)
            {
                // Last Month
                var today = DateTime.Today;
                var month = new DateTime(today.Year, today.Month, 1);
                var first = month.AddMonths(-1).ToString("yyyy-MM-dd 00:00:00");
                var last = month.AddDays(-1).ToString("yyyy-MM-dd 00:00:00");

                DateTime datetime_start = DateTime.ParseExact(first, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                dateTimePicker_start.Value = datetime_start;

                DateTime datetime_end = DateTime.ParseExact(last, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                dateTimePicker_end.Value = datetime_end;
            }
        }

        private void comboBox_list_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_list.SelectedIndex == 0)
            {
                // Registration
                string start = DateTime.Now.ToString("2018-01-22 00:00:00");
                DateTime datetime_start = DateTime.ParseExact(start, "yyyy-MM-dd 00:00:00", CultureInfo.InvariantCulture);
                dateTimePicker_start.Value = datetime_start;

                string end = DateTime.Now.ToString("yyyy-MM-dd 00:00:00");
                DateTime datetime_end = DateTime.ParseExact(end, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                dateTimePicker_end.Value = datetime_end;
            }
            else if (comboBox_list.SelectedIndex == 1)
            {
                // Payment
                string start = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd 00:00:00");
                DateTime datetime_start = DateTime.ParseExact(start, "yyyy-MM-dd 00:00:00", CultureInfo.InvariantCulture);
                dateTimePicker_start.Value = datetime_start;

                string end = DateTime.Now.ToString("yyyy-MM-dd 00:00:00");
                DateTime datetime_end = DateTime.ParseExact(end, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                dateTimePicker_end.Value = datetime_end;
            }
            else if (comboBox_list.SelectedIndex == 2)
            {
                // Bonus
                string start = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                DateTime datetime_start = DateTime.ParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                dateTimePicker_start.Value = datetime_start;

                string end = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                DateTime datetime_end = DateTime.ParseExact(end, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                dateTimePicker_end.Value = datetime_end;
            }
            else if (comboBox_list.SelectedIndex == 3)
            {
                // Turnover Record
                string start = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                DateTime datetime_start = DateTime.ParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                dateTimePicker_start.Value = datetime_start;

                string end = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                DateTime datetime_end = DateTime.ParseExact(end, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                dateTimePicker_end.Value = datetime_end;
            }
            else if (comboBox_list.SelectedIndex == 4)
            {
                // Bet Record
                string start = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                DateTime datetime_start = DateTime.ParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                dateTimePicker_start.Value = datetime_start;

                string end = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                DateTime datetime_end = DateTime.ParseExact(end, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                dateTimePicker_end.Value = datetime_end;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DateTime today = DateTime.Now;
            DateTime date = today.AddDays(1);
            Properties.Settings.Default.______midnight_time = date.ToString("yyyy-MM-dd 00:30");
            Properties.Settings.Default.______start_detect = "1";
            Properties.Settings.Default.Save();
        }

        private void timer_cycle_in_Tick(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.______midnight_time != "")
            {
                string cyclein_parse = Properties.Settings.Default.______midnight_time;
                DateTime cyclein = DateTime.ParseExact(cyclein_parse, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

                string start_parse = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                DateTime start = DateTime.ParseExact(start_parse, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                TimeSpan difference = cyclein - start;
                int hrs = difference.Hours;
                int mins = difference.Minutes;
                int secs = difference.Seconds;

                TimeSpan spinTime = new TimeSpan(hrs, mins, secs);

                TimeSpan delta = DateTime.Now - start;
                TimeSpan timeRemaining = spinTime - delta;

                if (timeRemaining.Hours != 0 && timeRemaining.Minutes != 0)
                {
                    label_cycle_in.Text = timeRemaining.Hours + " hr(s) " + timeRemaining.Minutes + " min(s)";
                }
                else if (timeRemaining.Hours == 0 && timeRemaining.Minutes == 0)
                {
                    label_cycle_in.Text = timeRemaining.Seconds + " sec(s)";
                }
                else if (timeRemaining.Hours == 0)
                {
                    label_cycle_in.Text = timeRemaining.Minutes + " min(s) " + timeRemaining.Seconds + " sec(s)";
                }
            }
            else
            {
                label_cycle_in.Text = "-";
            }
        }

        private void button_start_Click(object sender, EventArgs e)
        {
            __is_start = true;
            panel_filter.Enabled = false;
            label_status.Text = "Waiting";

            string start_datetime = dateTimePicker_start.Text;
            DateTime start = DateTime.Parse(start_datetime);

            string end_datetime = dateTimePicker_end.Text;
            DateTime end = DateTime.Parse(end_datetime);

            string result_start = start.ToString("yyyy-MM-dd");
            string result_end = end.ToString("yyyy-MM-dd");
            string result_start_time = start.ToString("HH:mm:ss");
            string result_end_time = end.ToString("HH:mm:ss");

            if (start <= end)
            {
                button_stop.Visible = true;
                button_start.Visible = false;
                __timer_count = 10;
                label_count.Text = __timer_count.ToString();
                __timer_count = 9;
                label_count.Visible = true;
                timer_start_button.Start();
            }
            else
            {
                MessageBox.Show("No data found.", "YB Cronos Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                panel_filter.Enabled = true;
            }
            
        }

        private void button_stop_Click(object sender, EventArgs e)
        {
            panel_filter.Enabled = true;
            button_stop.Visible = false;
            button_start.Visible = true;
            __timer_count = 10;
            label_count.Visible = false;
            timer_start_button.Stop();
            __is_autostart = false;
            label_status.Text = "Stop";
        }

        private void timer_elapsed_Tick(object sender, EventArgs e)
        {
            string start_datetime = __start_datetime_elapsed;
            DateTime start = DateTime.ParseExact(start_datetime, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            string finish_datetime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            DateTime finish = DateTime.ParseExact(finish_datetime, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            TimeSpan span = finish.Subtract(start);

            if (span.Hours == 0 && span.Minutes == 0)
            {
                label_elapsed.Text = span.Seconds + " sec(s)";
            }
            else if (span.Hours != 0)
            {
                label_elapsed.Text = span.Hours + " hr(s) " + span.Minutes + " min(s) " + span.Seconds + " sec(s)";
            }
            else if (span.Minutes != 0)
            {
                label_elapsed.Text = span.Minutes + " min(s) " + span.Seconds + " sec(s)";
            }
            else
            {
                label_elapsed.Text = span.Seconds + " sec(s)";
            }
        }

        private async void timer_start_button_TickAsync(object sender, EventArgs e)
        {
            if (__is_login)
            {
                try
                {
                    label_count.Text = __timer_count--.ToString();
                    if (label_count.Text == "-1")
                    {
                        label_status.Text = "Running";
                        panel_status.Visible = true;
                        label_start_datetime.Text = DateTime.Now.ToString("ddd, dd MMM HH:mm:ss");
                        __start_datetime_elapsed = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                        timer_elapsed.Start();
                        button_stop.Visible = false;
                        label_count.Visible = false;
                        timer_start_button.Stop();

                        if (comboBox_list.SelectedIndex == 0)
                        {
                            // Registration
                            label_yb_status.Text = "status: doing calculation... --- MEMBER LIST";
                            await ___REGISTRATIONAsync();
                        }
                        else if (comboBox_list.SelectedIndex == 1)
                        {
                            // Payment
                            ___PAYMENT();
                        }
                        else if (comboBox_list.SelectedIndex == 2)
                        {
                            // Bonus
                            ___BONUS();
                        }
                        else if (comboBox_list.SelectedIndex == 3)
                        {
                            // Turnover Record
                            ___TURNOVER();
                        }
                        else if (comboBox_list.SelectedIndex == 4)
                        {
                            // Bet Record
                            ___BET();
                        }
                    }
                }
                catch (Exception err)
                {
                    // send telegram
                    MessageBox.Show(err.ToString());
                }
            }
        }

        private async Task ___REGISTRATIONAsync()
        {
            var cookie_manager = Cef.GetGlobalCookieManager();
            var visitor = new CookieCollector();
            cookie_manager.VisitUrlCookies(__url, true, visitor);
            var cookies = await visitor.Task;
            var cookie = CookieCollector.GetCookieHeader(cookies);
            WebClient wc = new WebClient();
            wc.Headers.Add("Cookie", cookie);
            wc.Encoding = Encoding.UTF8;
            wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

            string start = dateTimePicker_start.Text;
            start = start.Replace("-", "%2F");
            start = start.Replace(" ", "+");
            start = start.Replace(":", "%3A");
            
            string end = dateTimePicker_end.Text;
            end = end.Replace("-", "%2F");
            end = end.Replace(" ", "+");
            end = end.Replace(":", "%3A");

            byte[] result = await wc.DownloadDataTaskAsync("http://103.4.104.8/manager/member/searchMember?userId=&userName=&email=&lastDepositSince=&lastBetTimeSince=&noLoginSince=&loginIp=&vipLevel=-1&phoneNumber=&registeredDateStart=" + start + "&registeredDateEnd="+ end +"&birthOfDateStart=&birthOfDateEnd=&searchType=1&affiliateCode=All&pageNumber=1&pageSize=100000&sortCondition=1&sortName=sign_up_time&sortOrder=1&searchText=");
            string responsebody = Encoding.UTF8.GetString(result);
            var deserialize_object = JsonConvert.DeserializeObject(responsebody);
            __jo = JObject.Parse(deserialize_object.ToString());
            __jo_count = __jo.SelectToken("$.aaData");
            label_page_count.Text = "0 of 1";
            label_total_records.Text = "0 of " + __jo_count.Count().ToString("N0");

            // PROCESS DATA
            StringBuilder _DATA = new StringBuilder();
            int _display_count = 0;

            for (int i = 0; i < __jo_count.Count(); i++)
            {
                Application.DoEvents();
                _display_count++;
                label_total_records.Text = _display_count.ToString("N0") + " of " + __jo_count.Count().ToString("N0");

                JToken _username = __jo.SelectToken("$.aaData[" + i + "].userId").ToString();
                // -----
                JToken _name = __jo.SelectToken("$.aaData[" + i + "].userName").ToString();
                // -----
                JToken _email = __jo.SelectToken("$.aaData[" + i + "].email").ToString();
                // -----
                JToken _phone = __jo.SelectToken("$.aaData[" + i + "].phoneNumber").ToString();
                // -----
                JToken _dob = __jo.SelectToken("$.aaData[" + i + "].birthday").ToString();
                if (_dob.ToString() != "-1")
                {
                    DateTime _dob_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_dob.ToString()) / 1000d)).ToLocalTime();
                    _dob = _dob_replace.ToString("yyyy-MM-dd");
                }
                else
                {
                    _dob = "";
                }
                // -----
                JToken _vip = __jo.SelectToken("$.aaData[" + i + "].vipLevel").ToString();
                // -----
                JToken _affiliate_url = __jo.SelectToken("$.aaData[" + i + "].affiliateUrl").ToString();
                // -----
                string _source = "";
                char[] split = "*|*".ToCharArray();
                for (int i_s = 0; i_s < __getdata_affiliatelist.Count; i_s++)
                {
                    string[] results = __getdata_affiliatelist[i_s].Split(split);
                    if (results[0].Trim() == _affiliate_url.ToString())
                    {
                        _source = results[3].Trim();
                        break;
                    }
                }
                // -----
                string _first_deposit = await ___REGISTRATION_FIRSTDEPOSITAsync(_username.ToString());
                string _month_first_deposit = "";
                if (_first_deposit != "")
                {
                    DateTime _first_deposit_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_first_deposit.ToString()) / 1000d)).ToLocalTime();
                    _first_deposit = _first_deposit_replace.ToString("yyyy-MM-dd");
                    _month_first_deposit = _first_deposit_replace.ToString("yyyy-MM-01");
                }
                else
                {
                    _first_deposit = "";
                }
                // -----
                JToken _last_deposit = __jo.SelectToken("$.aaData[" + i + "].lastDepositTime").ToString();
                if (_last_deposit.ToString() != "")
                {
                    DateTime _last_deposit_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_last_deposit.ToString()) / 1000d)).ToLocalTime();
                    _last_deposit = _last_deposit_replace.ToString("yyyy-MM-dd");
                }
                else
                {
                    _last_deposit = "";
                }
                // -----
                JToken _date_registered = __jo.SelectToken("$.aaData[" + i + "].createTime").ToString();
                string _month_registered = "";
                if (_date_registered.ToString() != "")
                {
                    DateTime _date_registered_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_date_registered.ToString()) / 1000d)).ToLocalTime();
                    _date_registered = _date_registered_replace.ToString("yyyy-MM-dd");
                    _month_registered = _date_registered_replace.ToString("yyyy-MM-01");

                }
                else
                {
                    _date_registered = "";
                }
                // -----
                JToken _last_login_time = __jo.SelectToken("$.aaData[" + i + "].loginTime").ToString();
                if (_last_login_time.ToString() != "")
                {
                    DateTime _last_login_time_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_last_login_time.ToString()) / 1000d)).ToLocalTime();
                    _last_login_time = _last_login_time_replace.ToString("yyyy-MM-dd");
                }
                else
                {
                    _last_login_time = "";
                }
                // -----
                JToken _ip_address = __jo.SelectToken("$.aaData[" + i + "].loginIp").ToString();
                // -----
                JToken _status = __jo.SelectToken("$.aaData[" + i + "].status").ToString();
                if (_status.ToString() == "0")
                {
                    _status = "INACTIVATED";
                }
                else if (_status.ToString() == "1")
                {
                    _status = "ACTIVE";
                }
                else if (_status.ToString() == "2")
                {
                    _status = "SUSPEND";
                }
                else if (_status.ToString() == "3")
                {
                    _status = "LOCKED";
                }

                //MessageBox.Show("Username: " + _username + "\nName: " + _name + "\nEmail: " + _email + "\nPhone: " + _phone + "\nDOB: " + _dob + "\nVIP: " + _vip + "\nURL: " + _affiliate_url + "\nSource: " + _source + "\nFD: " + _first_deposit + "\nLD: " + _last_deposit + "\nDate Registered: " + _date_registered + "\nLast Login Time: " + _last_login_time "\nStatus: " + _status);

                if (_display_count == 1)
                {
                    var header = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}", "Brand", "Username", "Name", "Status", "Date Registered", "Last Login Date", "Last Deposit Date", "Contact Number", "Email", "VIP Level", "Registration Date", "Month Reg", "First Deposit Date", "First Deposit Month", "IP Address", "Affiliate", "Source", "Date of Birth", "User ID", "Wechat", "QQ");
                    _DATA.AppendLine(header);
                }
                var data = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}", "YB", "\"" + _username + "\"", "\"" + _name + "\"", "\"" + _status + "\"", "\"" + _date_registered + "\"", "\"" + _last_login_time + "\"", "\"" + _last_deposit + "\"", "\"" + _phone + "\"", "\"" + _email + "\"", "\"" + _vip + "\"", "\"" + _date_registered + "\"", "\"" + _month_registered + "\"", "\"" + _first_deposit + "\"", "\"" + _month_first_deposit + "\"", "\"" + _ip_address + "\"", "\"" + _affiliate_url + "\"", "\"" + _source + "\"", "\"" + _dob + "\"", "\"" + "" + "\"", "\"" + "" + "\"", "\"" + "" + "\"");
                _DATA.AppendLine(data);
            }
            
            // SAVING TO EXCEL
            string _current_datetime = DateTime.Now.ToString("yyyy-MM-dd");

            label_yb_status.ForeColor = Color.FromArgb(78, 122, 159);
            label_yb_status.Text = "status: saving excel... --- MEMBER LIST";

            if (!Directory.Exists(__file_location + "\\Cronos Data"))
            {
                Directory.CreateDirectory(__file_location + "\\Cronos Data");
            }

            if (!Directory.Exists(__file_location + "\\Cronos Data\\YB"))
            {
                Directory.CreateDirectory(__file_location + "\\Cronos Data\\YB");
            }

            if (!Directory.Exists(__file_location + "\\Cronos Data\\YB\\" + _current_datetime))
            {
                Directory.CreateDirectory(__file_location + "\\Cronos Data\\YB\\" + _current_datetime);
            }

            string _folder_path_result = __file_location + "\\Cronos Data\\YB\\" + _current_datetime + "\\YB Registration.txt";
            string _folder_path_result_xlsx = __file_location + "\\Cronos Data\\YB\\" + _current_datetime + "\\YB Registration.xlsx";

            if (File.Exists(_folder_path_result))
            {
                File.Delete(_folder_path_result);
            }

            if (File.Exists(_folder_path_result_xlsx))
            {
                File.Delete(_folder_path_result_xlsx);
            }

            _DATA.ToString().Reverse();
            File.WriteAllText(_folder_path_result, _DATA.ToString(), Encoding.UTF8);

            Excel.Application app = new Excel.Application();
            Excel.Workbook wb = app.Workbooks.Open(_folder_path_result, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            Excel.Worksheet worksheet = wb.ActiveSheet;
            worksheet.Activate();
            worksheet.Application.ActiveWindow.SplitRow = 1;
            worksheet.Application.ActiveWindow.FreezePanes = true;
            Excel.Range firstRow = (Excel.Range)worksheet.Rows[1];
            //firstRow.AutoFilter(1,
            //                    Type.Missing,
            //                    Excel.XlAutoFilterOperator.xlAnd,
            //                    Type.Missing,
            //                    true);
            //worksheet.Columns[6].NumberFormat = "MM/dd/yyyy";
            Excel.Range usedRange = worksheet.UsedRange;
            Excel.Range rows = usedRange.Rows;
            int count = 0;
            foreach (Excel.Range row in rows)
            {
                if (count == 0)
                {
                    Excel.Range firstCell = row.Cells[1];

                    string firstCellValue = firstCell.Value as String;

                    if (!string.IsNullOrEmpty(firstCellValue))
                    {
                        row.Interior.Color = Color.FromArgb(236, 103, 5);
                        row.Font.Color = Color.FromArgb(255, 255, 255);
                    }

                    break;
                }

                count++;
            }
            int i_;
            for (i_ = 1; i_ <= 17; i_++)
            {
                worksheet.Columns[i_].ColumnWidth = 18;
            }
            wb.SaveAs(_folder_path_result_xlsx, Excel.XlFileFormat.xlOpenXMLWorkbook, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Excel.XlSaveAsAccessMode.xlExclusive, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            wb.Close();
            app.Quit();
            Marshal.ReleaseComObject(app);

            if (File.Exists(_folder_path_result))
            {
                File.Delete(_folder_path_result);
            }

            _DATA.Clear();
        }

        private void ___PAYMENT()
        {

        }

        private void ___BONUS()
        {

        }

        private void ___TURNOVER()
        {

        }

        private void ___BET()
        {

        }
        
        private async Task<string> ___REGISTRATION_FIRSTDEPOSITAsync(string username)
        {
            var cookie_manager = Cef.GetGlobalCookieManager();
            var visitor = new CookieCollector();
            cookie_manager.VisitUrlCookies(__url, true, visitor);
            var cookies = await visitor.Task;
            var cookie = CookieCollector.GetCookieHeader(cookies);
            WebClient wc = new WebClient();
            wc.Headers.Add("Cookie", cookie);
            wc.Encoding = Encoding.UTF8;
            wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

            byte[] result = await wc.DownloadDataTaskAsync("http://103.4.104.8/manager/member/getProfileOverview?userId=" + username);
            string responsebody = Encoding.UTF8.GetString(result);
            var deserialize_object = JsonConvert.DeserializeObject(responsebody);
            JObject _jo = JObject.Parse(deserialize_object.ToString());
            JToken _first_deposit_time = _jo.SelectToken("$.firstDepositTime").ToString();
            return _first_deposit_time.ToString();
        }

        private void ___GETDATA_AFFILIATELIST()
        {
            try
            {
                string connection = "Data Source=192.168.10.252;User ID=sa;password=Test@123;Initial Catalog=testrain;Integrated Security=True;Trusted_Connection=false;";

                using (SqlConnection conn = new SqlConnection(connection))
                {
                    conn.Open();
                    SqlCommand command = new SqlCommand("SELECT * FROM [testrain].[dbo].[YB.Affiliate List]", conn);
                    SqlCommand command_count = new SqlCommand("SELECT COUNT(*) FROM [testrain].[dbo].[YB.Affiliate List]", conn);
                    string columns = "";

                    Int32 getcount = (Int32)command_count.ExecuteScalar();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        int count = 0;
                        while (reader.Read())
                        {
                            count++;
                            label_getdatacount.Text = "Affiliate List: " + count.ToString("N0") + " of " + getcount.ToString("N0");

                            Application.DoEvents();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                Application.DoEvents();
                                if (i == 0)
                                {
                                    columns += reader[i].ToString() + "*|*";
                                }
                                else if (i == 1)
                                {
                                    columns += reader[i].ToString();
                                }
                            }

                            __getdata_affiliatelist.Add(columns);
                            columns = "";
                        }
                    }

                    panel_yb.Enabled = true;
                    label_getdatacount.Visible = false;
                    label_getdatacount.Text = "-";

                    conn.Close();
                }
            }
            catch (Exception err)
            {
                // comment
                //SendITSupport("There's a problem to the server, please re-open the application.");
                //SendMyBot(err.ToString());
                //__send = 0;

                //isClose = false;
                Environment.Exit(0);
            }
        }
    }
}

// clear
// __getdata_affiliatelist
