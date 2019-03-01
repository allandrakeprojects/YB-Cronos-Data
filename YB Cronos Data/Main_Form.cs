using CefSharp;
using CefSharp.WinForms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;

namespace YB_Cronos_Data
{
    public partial class Main_Form : Form
    {
        private string __root_url = "http://103.4.104.8";
        private string __url = "";
        private string __start_datetime_elapsed = "";
        private string __file_location = "\\\\192.168.10.22\\ssi-reporting";
        private string __brand_code = "YB";
        private string __brand_color_hex = "#EC6506";
        private string __brand_color_rgb = "236, 103, 5";
        private string __app = "Cronos Data";
        private string __app_type = "2";
        private string __display_length = "100000";
        private int __send = 0;
        private int __timer_count = 10;
        private bool __is_login = false;
        private bool __is_start = false;
        private bool __is_autostart = true;
        private bool __detect_header = false;
        private bool __is_send = true;
        private JObject __jo;
        private JToken __jo_count;
        private ChromiumWebBrowser chromeBrowser;
        List<string> __getdata_affiliatelist = new List<string>();
        List<string> __getdata_bonuscode = new List<string>();
        Timer timer = new Timer();
        Form __mainform_handler;
        
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
            DialogResult dr = MessageBox.Show("Exit the program?", __brand_code + " Cronos Data", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
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
            chromeBrowser = new ChromiumWebBrowser(__root_url + "/page/manager/login.jsp");
            panel_cefsharp.Controls.Add(chromeBrowser);
            chromeBrowser.AddressChanged += ChromiumBrowserAddressChanged;
        }

        // CefSharp Address Changed
        private void ChromiumBrowserAddressChanged(object sender, AddressChangedEventArgs e)
        {
            __url = e.Address.ToString();
            if (e.Address.ToString().Equals(__root_url + "/page/manager/login.jsp"))
            {
                if (__is_login)
                {
                    Invoke(new Action(() =>
                    {
                        pictureBox_loader.Visible = false;
                        label_page_count.Visible = false;
                        label_total_records.Visible = false;
                        button_start.Visible = false;
                        panel_loader.Visible = false;
                        __mainform_handler = Application.OpenForms[0];
                        __mainform_handler.Size = new Size(569, 514);
                        panel_loader.Visible = false;
                        label_navigate_up.Enabled = false;
                        
                        SendITSupport("The application have been logout, please re-login again.");
                        SendMyBot("The application have been logout, please re-login again.");
                        __send = 0;
                    }));
                }

                __is_login = false;

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
                                    args.Frame.ExecuteJavaScriptAsync("document.getElementById('username').value = 'ybrain';");
                                    args.Frame.ExecuteJavaScriptAsync("document.getElementById('password').value = 'pass123';");
                                    args.Frame.ExecuteJavaScriptAsync("window.scrollTo(0,document.body.scrollHeight)");
                                    __is_login = false;
                                    panel_cefsharp.Visible = true;
                                    pictureBox_loader.Visible = false;
                                    label_status.Text = "Logout";
                                }
                            }));
                        }
                    };
                }));
            }

            if (e.Address.ToString().Equals(__root_url + "/page/manager/member/search.jsp") || e.Address.ToString().Equals(__root_url + "/page/manager/dashboard.jsp"))
            {
                Invoke(new Action(async () =>
                {
                    pictureBox_loader.Visible = true;
                    label_page_count.Visible = true;
                    label_total_records.Visible = true;
                    button_start.Visible = true;
                    __mainform_handler = Application.OpenForms[0];
                    __mainform_handler.Size = new Size(569, 208);
                    panel_loader.Visible = true;
                    label_navigate_up.Enabled = false;

                    if (!__is_login)
                    {
                        __is_login = true;
                        panel_cefsharp.Visible = false;
                        pictureBox_loader.Visible = true;
                    }

                    if (!__is_start)
                    {
                        if (Properties.Settings.Default.______start_detect == "0")
                        {
                            button_start.Enabled = false;
                            panel_filter.Enabled = false;

                            label_status.Text = "Waiting";
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
                    else
                    {
                        label_status.Text = "Waiting";
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
            
            panel_landing.Visible = false;
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
            ___GETDATA_BONUSCODE();
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
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DateTime today = DateTime.Now;
            DateTime date = today.AddDays(1);
            Properties.Settings.Default.______midnight_time = date.ToString("yyyy-MM-dd 00:30");
            Properties.Settings.Default.______start_detect = "0";
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
                MessageBox.Show("No data found.", __brand_code + " Cronos Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                        if (__start_datetime_elapsed == "")
                        {
                            label_start_datetime.Text = DateTime.Now.ToString("ddd, dd MMM HH:mm:ss");
                            __start_datetime_elapsed = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                        }
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
                            label_yb_status.Text = "status: doing calculation... --- DEPOSIT RECORD";
                            await ___PAYMENTAsync();
                        }
                        else if (comboBox_list.SelectedIndex == 2)
                        {
                            // Bonus
                            label_yb_status.Text = "status: doing calculation... --- BONUS REPORT";
                            await ___BONUSAsync();
                        }
                        else if (comboBox_list.SelectedIndex == 3)
                        {
                            // Turnover Record
                            label_yb_status.Text = "status: doing calculation... --- TURNOVER RECORD";
                            await ___TURNOVERAsync();
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
            try
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

                label_page_count.Text = "-";
                byte[] result = await wc.DownloadDataTaskAsync(__root_url + "/manager/member/searchMember?userId=&userName=&email=&lastDepositSince=&lastBetTimeSince=&noLoginSince=&loginIp=&vipLevel=-1&phoneNumber=&registeredDateStart=" + start + "&registeredDateEnd=" + end + "&birthOfDateStart=&birthOfDateEnd=&searchType=1&affiliateCode=All&pageNumber=1&pageSize=" + __display_length + "&sortCondition=1&sortName=sign_up_time&sortOrder=1&searchText=");
                string responsebody = Encoding.UTF8.GetString(result);
                var deserialize_object = JsonConvert.DeserializeObject(responsebody);
                __jo = JObject.Parse(deserialize_object.ToString());
                __jo_count = __jo.SelectToken("$.aaData");
                label_page_count.Text = "1 of 1";
                label_total_records.Text = "0 of " + __jo_count.Count().ToString("N0");
                label_yb_status.Text = "status: getting data... --- MEMBER LIST";

                // REGISTRATION PROCESS DATA
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
                    JToken _contact_number = __jo.SelectToken("$.aaData[" + i + "].phoneNumber").ToString();
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
                    string _fd_date = await ___REGISTRATION_FIRSTDEPOSITAsync(_username.ToString());
                    string _first_fd_month = "";
                    if (_fd_date != "")
                    {
                        DateTime _fd_date_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_fd_date.ToString()) / 1000d)).ToLocalTime();
                        _fd_date = _fd_date_replace.ToString("yyyy-MM-dd");
                        _first_fd_month = _fd_date_replace.ToString("yyyy-MM-01");
                    }
                    else
                    {
                        _fd_date = "";
                    }
                    // -----
                    JToken _ld_date = __jo.SelectToken("$.aaData[" + i + "].lastDepositTime").ToString();
                    if (_ld_date.ToString() != "")
                    {
                        DateTime _ld_date_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_ld_date.ToString()) / 1000d)).ToLocalTime();
                        _ld_date = _ld_date_replace.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        _ld_date = "";
                    }
                    // -----
                    JToken _registration_date = __jo.SelectToken("$.aaData[" + i + "].createTime").ToString();
                    string _month_reg = "";
                    if (_registration_date.ToString() != "")
                    {
                        DateTime _registration_date_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_registration_date.ToString()) / 1000d)).ToLocalTime();
                        _registration_date = _registration_date_replace.ToString("yyyy-MM-dd");
                        _month_reg = _registration_date_replace.ToString("yyyy-MM-01");

                    }
                    else
                    {
                        _registration_date = "";
                    }
                    // -----
                    JToken _last_login_date = __jo.SelectToken("$.aaData[" + i + "].loginTime").ToString();
                    if (_last_login_date.ToString() != "")
                    {
                        DateTime _last_login_date_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_last_login_date.ToString()) / 1000d)).ToLocalTime();
                        _last_login_date = _last_login_date_replace.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        _last_login_date = "";
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
                    else
                    {
                        _status = "";
                    }

                    //MessageBox.Show("Username: " + _username + "\nName: " + _name + "\nEmail: " + _email + "\nPhone: " + _contact_number + "\nDOB: " + _dob + "\nVIP: " + _vip + "\nURL: " + _affiliate_url + "\nSource: " + _source + "\nFD: " + _fd_date + "\nLD: " + _ld_date + "\nDate Registered: " + _registration_date + "\nLast Login Time: " + _last_login_date "\nStatus: " + _status);

                    if (_display_count == 1)
                    {
                        var header = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}", "Brand", "Username", "Name", "Status", "Date Registered", "Last Login Date", "Last Deposit Date", "Contact Number", "Email", "VIP Level", "Registration Date", "Month Reg", "First Deposit Date", "First Deposit Month", "IP Address", "Affiliate", "Source", "Date of Birth", "User ID", "Wechat", "QQ");
                        _DATA.AppendLine(header);
                    }
                    var data = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}", __brand_code, "\"" + _username + "\"", "\"" + _name + "\"", "\"" + _status + "\"", "\"" + _registration_date + "\"", "\"" + _last_login_date + "\"", "\"" + _ld_date + "\"", "\"" + "86" + _contact_number + "\"", "\"" + _email + "\"", "\"" + _vip + "\"", "\"" + _registration_date + "\"", "\"" + _month_reg + "\"", "\"" + _fd_date + "\"", "\"" + _first_fd_month + "\"", "\"" + _ip_address + "\"", "\"" + _affiliate_url + "\"", "\"" + _source + "\"", "\"" + _dob + "\"", "\"" + "" + "\"", "\"" + "" + "\"", "\"" + "" + "\"");
                    _DATA.AppendLine(data);
                }

                if (__jo_count.ToString() != "0")
                {
                    // REGISTRATION SAVING TO EXCEL
                    string _current_datetime = DateTime.Now.ToString("yyyy-MM-dd");

                    label_yb_status.ForeColor = Color.FromArgb(78, 122, 159);
                    label_yb_status.Text = "status: saving excel... --- MEMBER LIST";
                    label_page_count.Text = "1 of 1";

                    if (!Directory.Exists(__file_location + "\\Cronos Data"))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data");
                    }

                    if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code);
                    }

                    if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime);
                    }

                    string _folder_path_result = __file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\" + __brand_code + " Registration.txt";
                    string _folder_path_result_xlsx = __file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\" + __brand_code + " Registration.xlsx";

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
                    firstRow.AutoFilter(1,
                                        Type.Missing,
                                        Excel.XlAutoFilterOperator.xlAnd,
                                        Type.Missing,
                                        true);
                    worksheet.Columns[5].NumberFormat = "MM/dd/yyyy";
                    worksheet.Columns[2].NumberFormat = "@";
                    Excel.Range usedRange = worksheet.UsedRange;
                    Excel.Range rows = usedRange.Rows;
                    int count = 0;
                    foreach (Excel.Range row in rows)
                    {
                        if (count == 0)
                        {
                            Excel.Range firstCell = row.Cells[1];

                            string firstCellValue = firstCell.Value as string;

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
                    for (i_ = 1; i_ <= 21; i_++)
                    {
                        worksheet.Columns[i_].ColumnWidth = 20;
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

                // REGISTRATION SEND TO DATABASE
                // AUTO START


                // next payment
                Properties.Settings.Default.______start_detect = "2";
                Properties.Settings.Default.Save();

                panel_status.Visible = false;
                label_yb_status.Text = "-";
                label_page_count.Text = "-";
                label_total_records.Text = "-";
                button_start.Visible = true;
                if (__is_autostart)
                {
                    comboBox_list.SelectedIndex = 1;
                    button_start.PerformClick();
                }
                else
                {
                    panel_filter.Enabled = true;
                }
                
                __send = 0;
            }
            catch (Exception err)
            {
                __send++;
                if (__send == 5)
                {
                    SendITSupport("There's a problem to the server, please re-open the application.");
                    SendMyBot(err.ToString());

                    Environment.Exit(0);
                }
                else
                {
                    ___WaitNSeconds(10);
                    await ___REGISTRATIONAsync();
                }
            }
        }

        private async Task ___PAYMENTAsync()
        {
            await ___PAYMENT_DEPOSITAsync();
        }

        private async Task ___BONUSAsync()
        {
            try
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
                start = start.Replace("00:00:00", "");

                string end = dateTimePicker_end.Text;
                end = end.Replace("-", "%2F");
                end = end.Replace("00:00:00", "");

                label_page_count.Text = "-";
                byte[] result = await wc.DownloadDataTaskAsync(__root_url + "/manager/ReportController/searchBonusReport?userName=&category=-1&type=-1&templateStatus=-1&createTimeStart=" + start + "&createTimeEnd=" + end + "&pageNumber=1&pageSize=" + __display_length + "&sortCondition=9&sortName=createTime&sortOrder=1&searchText=");
                string responsebody = Encoding.UTF8.GetString(result);
                var deserialize_object = JsonConvert.DeserializeObject(responsebody);
                __jo = JObject.Parse(deserialize_object.ToString());
                __jo_count = __jo.SelectToken("$.aaData");
                label_page_count.Text = "1 of 1";
                label_total_records.Text = "0 of " + __jo_count.Count().ToString("N0");
                label_yb_status.Text = "status: getting data... --- BONUS REPORT";

                // BONUS PROCESS DATA
                StringBuilder _DATA = new StringBuilder();
                int _display_count = 0;
                
                for (int i = 0; i < __jo_count.Count(); i++)
                {
                    Application.DoEvents();
                    _display_count++;
                    label_total_records.Text = _display_count.ToString("N0") + " of " + __jo_count.Count().ToString("N0");

                    // -----
                    JToken _username = __jo.SelectToken("$.aaData[" + i + "].userId").ToString();
                    // -----
                    JToken _submitted_date = __jo.SelectToken("$.aaData[" + i + "].createTime").ToString().Replace("/", "-");
                    DateTime _submitted_date_replace = DateTime.ParseExact(_submitted_date.ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    string _month = "";
                    string _date = "";
                    _submitted_date = _submitted_date_replace.ToString("yyyy-MM-dd HH:mm:ss");
                    _month = _submitted_date_replace.ToString("yyyy-MM-01");
                    _date = _submitted_date_replace.ToString("yyyy-MM-dd");
                    // -----
                    JToken _updated_date = __jo.SelectToken("$.aaData[" + i + "].updateTime").ToString().Replace("/", "-");
                    DateTime _updated_date_replace = DateTime.ParseExact(_updated_date.ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    _updated_date = _updated_date_replace.ToString("yyyy-MM-dd HH:mm:ss");
                    // -----
                    JToken _transaction_id = __jo.SelectToken("$.aaData[" + i + "].id").ToString();
                    // -----
                    JToken _bonus_code = __jo.SelectToken("$.aaData[" + i + "].templateCode").ToString();
                    string _bonus_code_replace = Regex.Replace(_bonus_code.ToString(), @"\t|\n|\r", "");
                    _bonus_code = _bonus_code_replace;
                    // -----
                    JToken _purpose = __jo.SelectToken("$.aaData[" + i + "].purpose").ToString();
                    // -----
                    JToken _amount = __jo.SelectToken("$.aaData[" + i + "].bonus").ToString();
                    // -----
                    JToken _vip = __jo.SelectToken("$.aaData[" + i + "].vipLevel").ToString();
                    // -----
                    JToken _updated_by = __jo.SelectToken("$.aaData[" + i + "].updater").ToString();
                    // ----- Transaction Time
                    string _transaction_time = "";
                    if (_updated_date.ToString() != "")
                    {
                        // duration time
                        DateTime start_date = DateTime.ParseExact(_submitted_date.ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        DateTime end_date = DateTime.ParseExact(_updated_date.ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        TimeSpan span = end_date - start_date;

                        // transaction time
                        if (span.Hours.ToString().Length == 1)
                        {
                            _transaction_time += "0" + span.Hours + ":";
                        }
                        else
                        {
                            _transaction_time += span.Hours + ":";
                        }
                        if (span.Minutes.ToString().Length == 1)
                        {
                            _transaction_time += "0" + span.Minutes + ":";
                        }
                        else
                        {
                            _transaction_time += span.Minutes + ":";
                        }
                        if (span.Seconds.ToString().Length == 1)
                        {
                            _transaction_time += "0" + span.Seconds;
                        }
                        else
                        {
                            _transaction_time += span.Seconds;
                        }
                    }
                    // ----- Bonus Category
                    string _bonus_category = "";
                    if (!_bonus_code.ToString().ToLower().Contains("no_bonus"))
                    {
                        try
                        {
                            if (!_username.ToString().ToLower().Contains("test"))
                            {
                                string _bonus_code_replace_ = "";
                                foreach (char c in _bonus_code.ToString())
                                {
                                    if (c == ';')
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        if (c != ' ')
                                        {
                                            _bonus_code_replace_ += c;
                                        }
                                    }
                                }

                                char[] split = "*|*".ToCharArray();
                                for (int i_b = 0; i_b < __getdata_bonuscode.Count; i_b++)
                                {
                                    string[] results = __getdata_bonuscode[i_b].Split(split);
                                    if (results[0].Trim() == _bonus_code_replace_)
                                    {
                                        _bonus_category = results[3].Trim();
                                        if (_purpose.ToString() == "")
                                        {
                                            _purpose = results[6].Trim();
                                        }
                                        break;
                                    }
                                }

                                if (_bonus_category == "")
                                {
                                    string get1 = _bonus_code.ToString().Substring(6, 3);

                                    string get2 = get1.Substring(0, 2);
                                    string get3 = get1.Substring(2);
                                    string get4 = get1.Substring(0, 2);

                                    ArrayList items_code = new ArrayList(new string[] { "AD", "FD", "FDB", "DP", "PZ", "RF", "RAF", "RB", "SU", "TO", "RR", "CB", "GW", "RW", "TE" });
                                    ArrayList items_bonus_category = new ArrayList(new string[] { "Adjustment", "FDB", "FDB", "Deposit", "Prize", "Refer friend", "Refer friend", "Reload", "Signup Bonus", "Turnover", "Rebate", "Cashback", "Goodwill Bonus", "Reward", "Test" });
                                    int count_ = 0;
                                    foreach (var item in items_code)
                                    {
                                        if (get2 == item.ToString())
                                        {
                                            _bonus_category = items_bonus_category[count_].ToString();
                                            break;
                                        }

                                        count_++;
                                    }

                                    if (_purpose.ToString() == "")
                                    {
                                        if (get3 == "0")
                                        {
                                            if (get4 == "FD" || get4 == "RA")
                                            {
                                                get1 = _bonus_code.ToString().Substring(6, 5);
                                                get2 = get1.Substring(0, 4);
                                                get3 = get1.Substring(4);
                                            }
                                            else
                                            {
                                                get1 = _bonus_code.ToString().Substring(6, 4);
                                                get2 = get1.Substring(0, 3);
                                                get3 = get1.Substring(3);
                                            }
                                        }

                                        ArrayList items_code_ = new ArrayList(new string[] { "0", "1", "2", "3", "4" });
                                        ArrayList items_bonus_category_ = new ArrayList(new string[] { "Retention", "Acquisition", "Conversion", "Retention", "Reactivation" });
                                        int count__ = 0;
                                        foreach (var item in items_code_)
                                        {
                                            if (get3 == item.ToString())
                                            {
                                                _purpose = items_bonus_category_[count__].ToString();
                                                break;
                                            }

                                            count__++;
                                        }
                                    }

                                    if (_bonus_category == "" && _purpose.ToString() == "")
                                    {
                                        _bonus_category = "Rebate";
                                        _purpose = "Retention";
                                    }
                                }
                            }
                            else
                            {
                                _bonus_category = "Other";
                                _purpose = "Adjustment";
                            }
                        }
                        catch (Exception err)
                        {
                            _bonus_category = "Other";
                            _purpose = "Adjustment";

                            SendMyBot(err.ToString());
                        }

                        if (_display_count == 1)
                        {
                            var header = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}", "Brand", "Month", "Date", "Transaction Time", "Transaction ID", "Username", "Bonus Code", "Bonus Category", "Purpose", "Amount", "VIP", "Updated by", "Product");
                            _DATA.AppendLine(header);
                        }
                        var data = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}", __brand_code, "\"" + _month + "\"", "\"" + _date + "\"", "\"" + _transaction_time + "\"", "\"" + _transaction_id + "\"", "\"" + _username + "\"", "\"" + _bonus_code + "\"", "\"" + _bonus_category + "\"", "\"" + _purpose + "\"", "\"" + _amount + "\"", "\"" + _vip + "\"", "\"" + _updated_by + "\"", "\"" + "" + "\"");
                        _DATA.AppendLine(data);
                    }
                    else
                    {
                        if (_display_count == 1)
                        {
                            var header = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}", "Brand", "Month", "Date", "Transaction Time", "Transaction ID", "Username", "Bonus Code", "Bonus Category", "Purpose", "Amount", "VIP", "Updated by", "Product");
                            _DATA.AppendLine(header);
                        }
                    }
                }

                if (__jo_count.ToString() != "0")
                {
                    // BONUS SAVING TO EXCEL
                    __detect_header = true;
                    string _current_datetime = DateTime.Now.ToString("yyyy-MM-dd");

                    label_yb_status.ForeColor = Color.FromArgb(78, 122, 159);
                    label_yb_status.Text = "status: saving excel... --- BONUS REPORT";

                    if (!Directory.Exists(__file_location + "\\Cronos Data"))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data");
                    }

                    if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code);
                    }

                    if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime);
                    }

                    if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Bonus Report"))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Bonus Report");
                    }

                    string _folder_path_result = __file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Bonus Report\\" + __brand_code + "_BonusReport_" + _current_datetime + "_1.txt";
                    string _folder_path_result_xlsx = __file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Bonus Report\\" + __brand_code + "_BonusReport_" + _current_datetime + "_1.xlsx";

                    if (File.Exists(_folder_path_result))
                    {
                        File.Delete(_folder_path_result);
                    }

                    if (File.Exists(_folder_path_result_xlsx))
                    {
                        File.Delete(_folder_path_result_xlsx);
                    }

                    _DATA.ToString().Reverse();

                    using (StreamWriter file = new StreamWriter(_folder_path_result, true, Encoding.UTF8))
                    {
                        file.Write(_DATA.ToString());
                    }

                    _DATA.Clear();
                }

                label_yb_status.Text = "status: doing calculation... --- A-BONUS RECORD";
                await ___BONUS_ADJUSTMENTAsync();
                
                __send = 0;
            }
            catch (Exception err)
            {
                __send++;
                if (__send == 5)
                {
                    SendITSupport("There's a problem to the server, please re-open the application.");
                    SendMyBot(err.ToString());

                    Environment.Exit(0);
                }
                else
                {
                    ___WaitNSeconds(10);
                    await ___BONUSAsync();
                }
            }
        }

        private async Task ___TURNOVERAsync()
        {
            try
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
                start = start.Replace("00:00:00", "");

                string end = dateTimePicker_end.Text;
                end = end.Replace("-", "%2F");
                end = end.Replace("00:00:00", "");

                label_page_count.Text = "-";
                byte[] result = await wc.DownloadDataTaskAsync(__root_url + "/manager/ReportController/searchTurnReport?userName=&type=-1&placedDateStart=" + start + "&placedDateEnd=" + end + "&pageNumber=1&pageSize=" + __display_length + "&sortCondition=3&sortName=summaryDate&sortOrder=1&searchText=");
                string responsebody = Encoding.UTF8.GetString(result);
                var deserialize_object = JsonConvert.DeserializeObject(responsebody);
                __jo = JObject.Parse(deserialize_object.ToString());
                __jo_count = __jo.SelectToken("$.aaData");
                label_page_count.Text = "1 of 1";
                label_total_records.Text = "0 of " + __jo_count.Count().ToString("N0");
                label_yb_status.Text = "status: getting data... --- TURNOVER RECORD";

                // TURNOVER PROCESS DATA
                StringBuilder _DATA = new StringBuilder();
                int _display_count = 0;

                for (int i = 0; i < __jo_count.Count(); i++)
                {
                    Application.DoEvents();
                    _display_count++;
                    label_total_records.Text = _display_count.ToString("N0") + " of " + __jo_count.Count().ToString("N0");

                    // -----
                    JToken _member = __jo.SelectToken("$.aaData[" + i + "].userId").ToString();
                    // -----
                    JToken _provider = __jo.SelectToken("$.aaData[" + i + "].vendorId").ToString();
                    string _provider_replace = Regex.Replace(_provider.ToString(), @" ?\(.*?\)", string.Empty);
                    _provider = _provider_replace;
                    // -----
                    JToken _category = __jo.SelectToken("$.aaData[" + i + "].gameType").ToString();
                    if (_category.ToString().ToLower() == "slot")
                    {
                        _category = "Slots";
                    }
                    else if (_category.ToString().ToLower() == "sport")
                    {
                        _category = "Sports";
                    }
                    else if (_category.ToString().ToLower() == "casino")
                    {
                        _category = "Live Casino";
                    }
                    if (_provider.ToString() == "开元棋牌")
                    {
                        _category = "Card Game";
                    }
                    // -----
                    JToken _vip = __jo.SelectToken("$.aaData[" + i + "].vipLevel").ToString();
                    // -----
                    JToken _stake = __jo.SelectToken("$.aaData[" + i + "].sumBetAmount").ToString();
                    // -----
                    JToken _stake_ex_draw = __jo.SelectToken("$.aaData[" + i + "].turnover").ToString();
                    // -----
                    JToken _bet_count = __jo.SelectToken("$.aaData[" + i + "].betCount").ToString();
                    // -----
                    JToken _company_wl = __jo.SelectToken("$.aaData[" + i + "].profit").ToString();
                    // -----
                    JToken _date = __jo.SelectToken("$.aaData[" + i + "].summaryDate").ToString();

                    string _fd_date = await ___REGISTRATION_FIRSTDEPOSITAsync(_member.ToString());
                    string _ld_date = await ___REGISTRATION_LASTDEPOSITAsync(_member.ToString());
                    if (_fd_date != "")
                    {
                        DateTime _fd_date_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_fd_date.ToString()) / 1000d)).ToLocalTime();
                        _fd_date = _fd_date_replace.ToString("MM/dd/yyyy");
                    }
                    else
                    {
                        _fd_date = "";
                    }
                    if (_ld_date != "")
                    {
                        DateTime _ld_date_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_ld_date.ToString()) / 1000d)).ToLocalTime();
                        _ld_date = _ld_date_replace.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        _ld_date = "";
                    }
                    // ----- Retained
                    string _retained = "";
                    string _month_ = DateTime.Now.Month.ToString();
                    string _year_ = DateTime.Now.Year.ToString();
                    string _year_month = _year_ + "-" + _month_;
                    string _current_month = DateTime.Now.ToString("MM/dd/yyyy");
                    string _last_month = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
                    if (_fd_date == _current_month)
                    {
                        _retained = "Not Retained";
                    }
                    else if (_ld_date == _last_month)
                    {
                        _retained = "Not Retained";
                    }
                    else
                    {
                        _retained = "Not Retained";
                    }
                    // ----- New Based on Reg && Reg Month
                    string _reg_month = await ___TURNOVER_REGMONTHsync(_member.ToString());
                    string _month = "";
                    string _new_based_on_reg = "";
                    if (_reg_month != "")
                    {
                        DateTime _reg_month_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_reg_month.ToString()) / 1000d)).ToLocalTime();
                        _reg_month = _reg_month_replace.ToString("MM/dd/yyyy");
                        _month = _reg_month_replace.ToString("yyyy-MM-01");

                        DateTime _reg_month_replace_ = DateTime.ParseExact(_reg_month, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                        if (_reg_month_replace_.ToString("yyyy-MM") == _year_month)
                        {
                            _new_based_on_reg = "Yes";
                        }
                        else
                        {
                            _new_based_on_reg = "No";
                        }
                    }
                    else
                    {
                        _new_based_on_reg = "No";
                        _reg_month = "";
                    }
                    // ----- New Based on Dep
                    // ----- Real Player
                    string _real_player = "";
                    string _new_based_on_dep = "";
                    if (_fd_date != "")
                    {
                        DateTime _first_deposit = DateTime.ParseExact(_fd_date, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                        if (_first_deposit.ToString("yyyy-MM") == _year_month)
                        {
                            _new_based_on_dep = "Yes";
                        }
                        else
                        {
                            _new_based_on_dep = "No";
                        }

                        _real_player = "Yes";
                    }
                    else
                    {
                        _new_based_on_dep = "No";
                        _real_player = "No";
                    }
                    
                    if (_display_count == 1)
                    {
                        var header = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17}", "Brand", "Provider", "Category", "Month", "Date", "Member", "Currency", "Stake", "Stake Ex. Draw", "Bet Count", "Company Winloss", "VIP", "Retained", "Reg Month", "First Dep Month", "New Based on Reg", "New Based on Dep", "Real Player");
                        _DATA.AppendLine(header);
                    }
                    var data = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17}", __brand_code, "\"" + _provider + "\"", "\"" + _category + "\"", "\"" + _month + "\"", "\"" + _date + "\"", "\"" + _member + "\"", "\"" + "CNY" + "\"", "\"" + _stake + "\"", "\"" + _stake + "\"", "\"" + _bet_count + "\"", "\"" + _company_wl + "\"", "\"" + _vip + "\"", "\"" + _retained + "\"", "\"" + _reg_month + "\"", "\"" + _fd_date + "\"", "\"" + _new_based_on_reg + "\"", "\"" + _new_based_on_dep + "\"", "\"" + _real_player + "\"");
                    _DATA.AppendLine(data);
                }

                if (__jo_count.ToString() != "0")
                {
                    // TURNOVER SAVING TO EXCEL
                    string _current_datetime = DateTime.Now.ToString("yyyy-MM-dd");

                    label_yb_status.ForeColor = Color.FromArgb(78, 122, 159);
                    label_yb_status.Text = "status: saving excel... --- TURNOVER RECORD";

                    if (!Directory.Exists(__file_location + "\\Cronos Data"))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data");
                    }

                    if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code);
                    }

                    if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime);
                    }

                    if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Turnover Record"))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Turnover Record");
                    }

                    string _folder_path_result = __file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Turnover Record\\" + __brand_code + "_TurnoverRecord_" + _current_datetime + "_1.txt";
                    string _folder_path_result_xlsx = __file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Turnover Record\\" + __brand_code + "_TurnoverRecord_" + _current_datetime + "_1.xlsx";

                    if (File.Exists(_folder_path_result))
                    {
                        File.Delete(_folder_path_result);
                    }

                    if (File.Exists(_folder_path_result_xlsx))
                    {
                        File.Delete(_folder_path_result_xlsx);
                    }
                    
                    File.WriteAllText(_folder_path_result, _DATA.ToString(), Encoding.UTF8);

                    Excel.Application app = new Excel.Application();
                    Excel.Workbook wb = app.Workbooks.Open(_folder_path_result, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                    Excel.Worksheet worksheet = wb.ActiveSheet;
                    worksheet.Activate();
                    worksheet.Application.ActiveWindow.SplitRow = 1;
                    worksheet.Application.ActiveWindow.FreezePanes = true;
                    Excel.Range firstRow = (Excel.Range)worksheet.Rows[1];
                    firstRow.AutoFilter(1,
                                        Type.Missing,
                                        Excel.XlAutoFilterOperator.xlAnd,
                                        Type.Missing,
                                        true);
                    Excel.Range usedRange = worksheet.UsedRange;
                    Excel.Range rows = usedRange.Rows;
                    int count = 0;
                    foreach (Excel.Range row in rows)
                    {
                        if (count == 0)
                        {
                            Excel.Range firstCell = row.Cells[1];

                            string firstCellValue = firstCell.Value as string;

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
                    for (i_ = 1; i_ <= 21; i_++)
                    {
                        worksheet.Columns[i_].ColumnWidth = 20;
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

                // DONE REPORTS
                Properties.Settings.Default.______start_detect = "0";
                Properties.Settings.Default.Save();

                panel_status.Visible = false;
                label_yb_status.Text = "-";
                label_page_count.Text = "-";
                label_total_records.Text = "-";
                button_start.Visible = true;
                if (__is_autostart)
                {
                    comboBox_list.SelectedIndex = 0;
                    button_start.Enabled = false;

                    SendITSupport("Reports has been completed.");
                    SendReportsTeam("Reports has been completed.");
                }
                else
                {
                    panel_filter.Enabled = true;
                }
                
                __getdata_affiliatelist.Clear();
                __getdata_bonuscode.Clear();
                __start_datetime_elapsed = "";
                label_finish_datetime.Text = DateTime.Now.ToString("ddd, dd MMM HH:mm:ss");
                timer_elapsed.Stop();

                label_start_datetime.Text = "-";
                label_finish_datetime.Text = "-";

                label_status.Text = "Waiting";
                
                __send = 0;
            }
            catch (Exception err)
            {
                __send++;
                if (__send == 5)
                {
                    SendITSupport("There's a problem to the server, please re-open the application.");
                    SendMyBot(err.ToString());

                    Environment.Exit(0);
                }
                else
                {
                    ___WaitNSeconds(10);
                    await ___TURNOVERAsync();
                }
            }
        }

        private async Task ___BETAsync()
        {
            try
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

                byte[] result = await wc.DownloadDataTaskAsync(__root_url + "/manager/ReportController/searchBetReport");
                string responsebody = Encoding.UTF8.GetString(result);
                var deserialize_object = JsonConvert.DeserializeObject(responsebody);
                JObject _jo = JObject.Parse(deserialize_object.ToString());
                JToken _jo_count = _jo.SelectToken("$.bets");

                for (int i = 0; i < _jo_count.Count(); i++)
                {
                    Application.DoEvents();

                    string yesterday_date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                    // -----
                    JToken _date = _jo.SelectToken("$.bets[" + i + "].date").ToString().Replace("/", "-");
                    if (yesterday_date == _date.ToString())
                    {
                        // -----
                        JToken _name = _jo.SelectToken("$.bets[" + i + "].name").ToString();
                        string _file_name = _name.ToString().Remove(18, 3);

                        string _current_datetime = DateTime.Now.ToString("yyyy-MM-dd");
                        if (!Directory.Exists(__file_location + "\\Cronos Data"))
                        {
                            Directory.CreateDirectory(__file_location + "\\Cronos Data");
                        }

                        if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code))
                        {
                            Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code);
                        }

                        if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime))
                        {
                            Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime);
                        }

                        if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Bet Record"))
                        {
                            Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Bet Record");
                        }
                        
                        string _folder_path_result_xlsx = __file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Bet Record\\" + _file_name + ".xlsx";

                        if (File.Exists(_folder_path_result_xlsx))
                        {
                            File.Delete(_folder_path_result_xlsx);
                        }

                        await wc.DownloadFileTaskAsync(
                            new Uri(__root_url + "/manager/ReportController/downloadBetReport?fileName=" + _file_name + "&realName=" + _name),
                            _folder_path_result_xlsx
                        );

                        __send = 0;

                        ___BET_PROCESS(_folder_path_result_xlsx);

                        break;
                    }
                }
            }
            catch (Exception err)
            {
                __send++;
                if (__send == 5)
                {
                    SendReportsTeam("Can't download Bet Record at this moment.");
                    SendMyBot(err.ToString());
                }
                else
                {
                    ___WaitNSeconds(10);
                    await ___BETAsync();
                }
            }
        }

        private void ___BET_PROCESS(string path)
        {
            StringBuilder _DATA = new StringBuilder();
            int _display_count = 0;
            label_betrecord.Visible = true;

            try
            {
                Excel.Application app_ = new Excel.Application();
                Excel.Workbook workbook_ = app_.Workbooks.Open(path);
                Excel._Worksheet worksheet_ = workbook_.Sheets[1];
                Excel.Range range_ = worksheet_.UsedRange;

                int rowCount = range_.Rows.Count;
                int colCount = range_.Columns.Count;

                for (int i = 1; i <= rowCount; i++)
                {
                    if (i != 1)
                    {
                        Application.DoEvents();

                        int count_ = 0;
                        string _month = DateTime.Now.ToString("MMM-y");
                        string _date = DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd");
                        string _product = "";
                        string _provider = "";
                        string _username = "";
                        string _vip = "";
                        string _bet_no = "";
                        string _time_placed = "";
                        string _time_settled = "";
                        string _game = "";
                        string _settlement = "";
                        string _bet_amount = "";
                        string _turnover = "";
                        string _payout = "";
                        string _member_wl = "";
                        string _create_time = "";

                        _display_count++;
                        label_betrecord.Text = "Bet Record: " + _display_count.ToString("N0") + " of " + rowCount.ToString("N0");
                        string _details = "";
                        for (int j = 1; j <= colCount; j++)
                        {
                            count_++;

                            try
                            {
                                _details += range_.Cells[i, j].Value2.ToString() + "|";
                            }
                            catch (Exception err)
                            {
                                _details += "" + "|";
                            }
                        }

                        string[] results = _details.Split('|');
                        _product = results[2];
                        _provider = results[3];
                        if (_provider.Trim() == "开元棋牌")
                        {
                            _product = "Card Game";
                        }
                        _username = results[4];
                        _vip = results[5];
                        _bet_no = results[6];
                        _time_placed = results[7];
                        DateTime _time_placed_repalce = DateTime.ParseExact(_time_placed.ToString(), "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
                        _time_placed = _time_placed_repalce.ToString("yyyy/MM/dd hh:mm:ss tt");
                        _time_settled = results[8];
                        DateTime _time_settled_replace = DateTime.ParseExact(_time_settled.ToString(), "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
                        _time_settled = _time_settled_replace.ToString("yyyy/MM/dd hh:mm:ss tt");
                        _game = results[9];
                        _settlement = results[10];
                        _bet_amount = results[11];
                        _turnover = results[12];
                        _payout = results[13];
                        _member_wl = results[14];
                        _create_time = results[15];

                        if (_display_count == 1)
                        {
                            var header = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}", "Month", "Date", "Product", "Provider", "Username", "VIP Level", "Bet No", "Time Placed", "Time Settled", "Game(Translate English)", "Settlement", "Bet Amount", "Turnover", "Payout", "Member WL", "Create Time");
                            _DATA.AppendLine(header);
                        }
                        var data = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}", _month, "\"" + _date + "\"", "\"" + _product + "\"", "\"" + _provider + "\"", "\"" + _username + "\"", "\"" + _vip + "\"", "\"" + _bet_no + "\"", "\"" + _time_placed + "\"", "\"" + _time_settled + "\"", "\"" + _game + "\"", "\"" + _settlement + "\"", "\"" + _bet_amount + "\"", "\"" + _turnover + "\"", "\"" + _payout + "\"", "\"" + _member_wl + "\"", "\"" + _create_time + "\"");
                        _DATA.AppendLine(data);
                    }
                }

                workbook_.Close();
                app_.Quit();
                Marshal.ReleaseComObject(app_);

                // BET SAVING EXCEL
                string _current_datetime = DateTime.Now.ToString("yyyy-MM-dd");
                if (!Directory.Exists(__file_location + "\\Cronos Data"))
                {
                    Directory.CreateDirectory(__file_location + "\\Cronos Data");
                }

                if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code))
                {
                    Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code);
                }

                if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime))
                {
                    Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime);
                }

                if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Bet Record"))
                {
                    Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Bet Record");
                }

                string _folder_path_result = __file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Bet Record\\" + __brand_code + "_BetRecord_" + _current_datetime + "_1.txt";
                string _folder_path_result_xlsx = __file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Bet Record\\" + __brand_code + "_BetRecord_" + _current_datetime + "_1.xlsx";

                if (File.Exists(_folder_path_result))
                {
                    File.Delete(_folder_path_result);
                }

                if (File.Exists(_folder_path_result_xlsx))
                {
                    File.Delete(_folder_path_result_xlsx);
                }

                File.WriteAllText(_folder_path_result, _DATA.ToString(), Encoding.UTF8);

                Excel.Application app = new Excel.Application();
                Excel.Workbook wb = app.Workbooks.Open(_folder_path_result, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                Excel.Worksheet worksheet = wb.ActiveSheet;
                worksheet.Activate();
                worksheet.Application.ActiveWindow.SplitRow = 1;
                worksheet.Application.ActiveWindow.FreezePanes = true;
                Excel.Range firstRow = (Excel.Range)worksheet.Rows[1];
                firstRow.AutoFilter(1,
                                    Type.Missing,
                                    Excel.XlAutoFilterOperator.xlAnd,
                                    Type.Missing,
                                    true);
                worksheet.Columns[1].NumberFormat = "MMM-y";
                worksheet.Columns[8].NumberFormat = "MM/dd/yyyy HH:mm";
                worksheet.Columns[9].NumberFormat = "MM/dd/yyyy HH:mm";
                Excel.Range usedRange = worksheet.UsedRange;
                Excel.Range rows = usedRange.Rows;
                int count = 0;
                foreach (Excel.Range row in rows)
                {
                    if (count == 0)
                    {
                        Excel.Range firstCell = row.Cells[1];

                        string firstCellValue = firstCell.Value as string;

                        if (!string.IsNullOrEmpty(firstCellValue))
                        {
                            row.Interior.Color = Color.FromArgb(27, 96, 168);
                            row.Font.Color = Color.FromArgb(255, 255, 255);
                        }

                        break;
                    }

                    count++;
                }
                int i_;
                for (i_ = 1; i_ <= 21; i_++)
                {
                    worksheet.Columns[i_].ColumnWidth = 20;
                }
                wb.SaveAs(_folder_path_result_xlsx, Excel.XlFileFormat.xlOpenXMLWorkbook, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Excel.XlSaveAsAccessMode.xlExclusive, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                wb.Close();
                app.Quit();
                Marshal.ReleaseComObject(app);

                if (File.Exists(_folder_path_result))
                {
                    File.Delete(_folder_path_result);
                }

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                _DATA.Clear();
                _display_count = 0;
                __send = 0;
                
                SendReportsTeam("Bet Record Completed.");
                label_betrecord.Visible = false;
                label_betrecord.Text = "-";
            }
            catch (Exception err)
            {
                ___WaitNSeconds(5);
                _DATA.Clear();
                _display_count = 0;
                ___BET_PROCESS(path);
            }
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

            byte[] result = await wc.DownloadDataTaskAsync(__root_url + "/manager/member/getProfileOverview?userId=" + username);
            string responsebody = Encoding.UTF8.GetString(result);
            var deserialize_object = JsonConvert.DeserializeObject(responsebody);
            JObject _jo = JObject.Parse(deserialize_object.ToString());
            JToken _fd_date_time = _jo.SelectToken("$.firstDepositTime").ToString();
            return _fd_date_time.ToString();
        }
        
        private async Task<string> ___REGISTRATION_LASTDEPOSITAsync(string username)
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

            byte[] result = await wc.DownloadDataTaskAsync(__root_url + "/manager/member/getProfileOverview?userId=" + username);
            string responsebody = Encoding.UTF8.GetString(result);
            var deserialize_object = JsonConvert.DeserializeObject(responsebody);
            JObject _jo = JObject.Parse(deserialize_object.ToString());
            JToken _ld_date_time = _jo.SelectToken("$.lastDepositTime").ToString();
            return _ld_date_time.ToString();
        }

        private async Task<string> ___PAYMENT_REMARKAsync(string id)
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

            byte[] result = await wc.DownloadDataTaskAsync(__root_url + "/manager/payment/getAdjustmentDetail?id=" + id);
            string responsebody = Encoding.UTF8.GetString(result);
            var deserialize_object = JsonConvert.DeserializeObject(responsebody);
            JObject _jo = JObject.Parse(deserialize_object.ToString());
            JToken _payment_remark = _jo.SelectToken("$.remark").ToString();
            string _payment_remark_replace = Regex.Replace(_payment_remark.ToString(), @"\t|\n|\r", "");
            _payment_remark = _payment_remark_replace;
            return _payment_remark.ToString();
        }

        private async Task<string> ___TURNOVER_REGMONTHsync(string username)
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

            byte[] result = await wc.DownloadDataTaskAsync(__root_url + "/manager/member/getProfileOverview?userId=" + username);
            string responsebody = Encoding.UTF8.GetString(result);
            var deserialize_object = JsonConvert.DeserializeObject(responsebody);
            JObject _jo = JObject.Parse(deserialize_object.ToString());
            JToken _reg_month = _jo.SelectToken("$.createTime").ToString();
            return _reg_month.ToString();
        }

        private async Task ___PAYMENT_DEPOSITAsync()
        {
            try
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

                label_page_count.Text = "-";
                byte[] result = await wc.DownloadDataTaskAsync(__root_url + "/manager/payment/searchDeposit?transactionId=&referenceNo=&userId=&status=9999&type=0&toBankIdOrBranch=-1&createDateStart=" + start + "&createDateEnd=" + end + "&vipLevel=-1&approvedDateStart=&approvedDateEnd=&pageNumber=1&pageSize=" + __display_length +"&sortCondition=4&sortName=createTime&sortOrder=1&searchText=");
                string responsebody = Encoding.UTF8.GetString(result);
                var deserialize_object = JsonConvert.DeserializeObject(responsebody);
                __jo = JObject.Parse(deserialize_object.ToString());
                __jo_count = __jo.SelectToken("$.aaData");
                label_page_count.Text = "1 of 1";
                label_total_records.Text = "0 of " + __jo_count.Count().ToString("N0");
                label_yb_status.Text = "status: getting data... --- DEPOSIT RECORD";

                // PAYMENT DEPOSIT PROCESS DATA
                StringBuilder _DATA = new StringBuilder();
                int _display_count = 0;
                
                for (int i = 0; i < __jo_count.Count(); i++)
                {
                    Application.DoEvents();
                    _display_count++;
                    label_total_records.Text = _display_count.ToString("N0") + " of " + __jo_count.Count().ToString("N0");
                    
                    // -----
                    JToken _member = __jo.SelectToken("$.aaData[" + i + "].userId").ToString();
                    // -----
                    JToken _transaction_id = __jo.SelectToken("$.aaData[" + i + "].id").ToString();
                    // -----
                    JToken _amount = __jo.SelectToken("$.aaData[" + i + "].amount").ToString();
                    // -----
                    JToken _vip = __jo.SelectToken("$.aaData[" + i + "].vipLevel").ToString();
                    // -----
                    string _fd_date = await ___REGISTRATION_FIRSTDEPOSITAsync(_member.ToString());
                    string _ld_date = await ___REGISTRATION_LASTDEPOSITAsync(_member.ToString());
                    string _first_fd_month = "";
                    if (_fd_date != "")
                    {
                        DateTime _fd_date_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_fd_date.ToString()) / 1000d)).ToLocalTime();
                        _fd_date = _fd_date_replace.ToString("MM/dd/yyyy");
                    }
                    else
                    {
                        _fd_date = "";
                    }
                    if (_ld_date != "")
                    {
                        DateTime _ld_date_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_ld_date.ToString()) / 1000d)).ToLocalTime();
                        _ld_date = _ld_date_replace.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        _ld_date = "";
                    }
                    // -----
                    JToken _payment_type = __jo.SelectToken("$.aaData[" + i + "].type").ToString();
                    if (_payment_type.ToString() == "0")
                    {
                        _payment_type = "Deposit";
                    }
                    else if (_payment_type.ToString() == "2")
                    {
                        _payment_type = "Payment Gateway";
                    }
                    else
                    {
                        _payment_type = "";
                    }
                    // -----
                    JToken _status = __jo.SelectToken("$.aaData[" + i + "].status").ToString();
                    if (_status.ToString() == "0")
                    {
                        _status = "Pending";
                    }
                    else if (_status.ToString() == "2")
                    {
                        _status = "Approved";
                    }
                    else if (_status.ToString() == "-1" || _status.ToString() == "-2")
                    {
                        _status = "Rejected";
                    }
                    else if (_status.ToString() == "1")
                    {
                        _status = "Verified";
                    }
                    else if (_status.ToString() == "3")
                    {
                        _status = "On Hold";
                    }
                    else
                    {
                        _status = "";
                    }
                    // -----
                    JToken _pg_company = __jo.SelectToken("$.aaData[" + i + "].toBankName").ToString();
                    // -----
                    JToken _pg_type = __jo.SelectToken("$.aaData[" + i + "].toPaymentType").ToString();
                    // -----
                    JToken _submitted_date = __jo.SelectToken("$.aaData[" + i + "].createTime").ToString();
                    string _month = "";
                    string _date = "";
                    if (_submitted_date.ToString() != "")
                    {
                        DateTime _submitted_date_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_submitted_date.ToString()) / 1000d)).ToLocalTime();
                        _submitted_date = _submitted_date_replace.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        _submitted_date = "";
                    }
                    // -----
                    JToken _updated_date = __jo.SelectToken("$.aaData[" + i + "].approvedTime").ToString();
                    string _time = "";
                    if (_updated_date.ToString() != "")
                    {
                        DateTime _updated_date_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_updated_date.ToString()) / 1000d)).ToLocalTime();
                        _updated_date = _updated_date_replace.ToString("yyyy-MM-dd HH:mm:ss");
                        _month = _updated_date_replace.ToString("yyyy-MM-01");
                        _date = _updated_date_replace.ToString("yyyy-MM-dd");
                        _time = _updated_date_replace.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        _updated_date = "";
                    }
                    // ----- Duration Time && Transaction Time
                    string _duration_time = "";
                    string _transaction_time = "";
                    if (_updated_date.ToString() != "")
                    {
                        // duration time
                        DateTime start_date = DateTime.ParseExact(_submitted_date.ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        DateTime end_date = DateTime.ParseExact(_updated_date.ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        TimeSpan span = end_date - start_date;
                        double totalMinutes = Math.Floor(span.TotalMinutes);
                        if (totalMinutes <= 5)
                        {
                            // 0-5
                            _duration_time = "0-5min";
                        }
                        else if (totalMinutes <= 10)
                        {
                            // 6-10
                            _duration_time = "6-10min";
                        }
                        else if (totalMinutes <= 15)
                        {
                            // 11-15
                            _duration_time = "11-15min";
                        }
                        else if (totalMinutes <= 20)
                        {
                            // 16-20
                            _duration_time = "16-20min";
                        }
                        else if (totalMinutes <= 25)
                        {
                            // 21-25
                            _duration_time = "21-25min";
                        }
                        else if (totalMinutes <= 30)
                        {
                            // 26-30
                            _duration_time = "26-30min";
                        }
                        else if (totalMinutes <= 60)
                        {
                            // 31-60
                            _duration_time = "31-60min";
                        }
                        else if (totalMinutes >= 61)
                        {
                            // >60
                            _duration_time = ">60min";
                        }

                        // transaction time
                        if (span.Hours.ToString().Length == 1)
                        {
                            _transaction_time += "0" + span.Hours + ":";
                        }
                        else
                        {
                            _transaction_time += span.Hours + ":";
                        }
                        if (span.Minutes.ToString().Length == 1)
                        {
                            _transaction_time += "0" + span.Minutes + ":";
                        }
                        else
                        {
                            _transaction_time += span.Minutes + ":";
                        }
                        if (span.Seconds.ToString().Length == 1)
                        {
                            _transaction_time += "0" + span.Seconds;
                        }
                        else
                        {
                            _transaction_time += span.Seconds;
                        }
                    }
                    // ----- Retained && New && Reactivated
                    string _retained = "";
                    string _new = "";
                    string _reactivated = "";
                    if (_status.ToString() == "Approved" && !_member.ToString().ToLower().Contains("test"))
                    {
                        string _current_month = DateTime.Now.ToString("MM/dd/yyyy");
                        string _last_month = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
                        if (_fd_date == _current_month)
                        {
                            _retained = "Not Retained";
                            _new = "New";
                            _reactivated = "Not Reactivated";
                        }
                        else if (_ld_date == _last_month)
                        {
                            _retained = "Not Retained";
                            _new = "New";
                            _reactivated = "Not Reactivated";
                        }
                        else
                        {
                            _retained = "Not Retained";
                            _new = "Not New";
                            _reactivated = "Reactivated";
                        }
                    }
                    else
                    {
                        _fd_date = "";
                    }
                    
                    //if (_updated_date.ToString() != "")
                    //{
                    //    DateTime _updated_date_replace_ = DateTime.ParseExact(_updated_date.ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    //    _updated_date = _updated_date_replace_.ToString("yyyy-MM-dd");
                    //}
                    //else
                    //{
                    //    _updated_date = "";
                    //}

                    if (_display_count == 1)
                    {
                        var header = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}", "Brand", "Month", "Date", "Time", "Submitted Date", "Updated Date", "Member", "Payment Type", "Transaction ID", "Amount", "Transaction Time", "Transaction Type", "Duration Time", "VIP", "Status", "PG Company", "PG Type", "Retained", "FD Date", "New", "Reactivated");
                        _DATA.AppendLine(header);
                    }
                    var data = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}", __brand_code, "\"" + _month + "\"", "\"" + _date + "\"", "\"" + _time + "\"", "\"" + _submitted_date + "\"", "\"" + _updated_date + "\"", "\"" + _member + "\"", "\"" + _payment_type + "\"", "\"" + _transaction_id + "\"", "\"" + _amount + "\"", "\"" + _transaction_time + "\"", "\"" + "Deposit" + "\"", "\"" + _duration_time + "\"", "\"" + _vip + "\"", "\"" + _status + "\"", "\"" + _pg_company + "\"", "\"" + _pg_type + "\"", "\"" + _retained + "\"", "\"" + _fd_date + "\"", "\"" + _new + "\"", "\"" + _reactivated + "\"");
                    _DATA.AppendLine(data);
                }

                if (__jo_count.ToString() != "0")
                {
                    // PAYMENT DEPOSIT SAVING TO EXCEL
                    __detect_header = true;
                    string _current_datetime = DateTime.Now.ToString("yyyy-MM-dd");

                    label_yb_status.ForeColor = Color.FromArgb(78, 122, 159);
                    label_yb_status.Text = "status: saving excel... --- DEPOSIT RECORD";

                    if (!Directory.Exists(__file_location + "\\Cronos Data"))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data");
                    }

                    if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code);
                    }

                    if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime);
                    }

                    if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Payment Report"))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Payment Report");
                    }

                    string _folder_path_result = __file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Payment Report\\" + __brand_code + "_PaymentReport_" + _current_datetime + "_1.txt";
                    string _folder_path_result_xlsx = __file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Payment Report\\" + __brand_code + "_PaymentReport_" + _current_datetime + "_1.xlsx";

                    if (File.Exists(_folder_path_result))
                    {
                        File.Delete(_folder_path_result);
                    }

                    if (File.Exists(_folder_path_result_xlsx))
                    {
                        File.Delete(_folder_path_result_xlsx);
                    }

                    _DATA.ToString().Reverse();

                    using (StreamWriter file = new StreamWriter(_folder_path_result, true, Encoding.UTF8))
                    {
                        file.Write(_DATA.ToString());
                    }

                    _DATA.Clear();
                }

                label_yb_status.Text = "status: doing calculation... --- WITHDRAWAL RECORD";
                await ___PAYMENT_WITHDRAWALAsync();
                
                __send = 0;
            }
            catch (Exception err)
            {
                __send++;
                if (__send == 5)
                {
                    SendITSupport("There's a problem to the server, please re-open the application.");
                    SendMyBot(err.ToString());

                    Environment.Exit(0);
                }
                else
                {
                    ___WaitNSeconds(10);
                    await ___PAYMENT_DEPOSITAsync();
                }
            }
        }

        private async Task ___PAYMENT_WITHDRAWALAsync()
        {
            try
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

                label_page_count.Text = "-";
                byte[] result = await wc.DownloadDataTaskAsync(__root_url + "/manager/payment/searchWithdrawal?transactionId=&userId=&vipLevel=-1&status=9999&createDateStart=" + start + "&createDateEnd=" + end + "&verifyDateStart=&verifyDateEnd=&approvedDateStart=&approvedDateEnd=&pageNumber=1&pageSize=" + __display_length + "&sortCondition=4&sortName=createTime&sortOrder=1&searchText=");
                string responsebody = Encoding.UTF8.GetString(result);
                var deserialize_object = JsonConvert.DeserializeObject(responsebody);
                __jo = JObject.Parse(deserialize_object.ToString());
                __jo_count = __jo.SelectToken("$.aaData");
                label_page_count.Text = "1 of 1";
                label_total_records.Text = "0 of " + __jo_count.Count().ToString("N0");
                label_yb_status.Text = "status: getting data... --- WITHDRAWAL RECORD";

                // PAYMENT WITHDRAWAL PROCESS DATA
                StringBuilder _DATA = new StringBuilder();
                int _display_count = 0;
                
                for (int i = 0; i < __jo_count.Count(); i++)
                {
                    Application.DoEvents();
                    _display_count++;
                    label_total_records.Text = _display_count.ToString("N0") + " of " + __jo_count.Count().ToString("N0");

                    // -----
                    JToken _member = __jo.SelectToken("$.aaData[" + i + "].userId").ToString();
                    // -----
                    JToken _transaction_id = __jo.SelectToken("$.aaData[" + i + "].id").ToString();
                    // -----
                    JToken _amount = __jo.SelectToken("$.aaData[" + i + "].amount").ToString();
                    // -----
                    JToken _vip = __jo.SelectToken("$.aaData[" + i + "].vipLevel").ToString();
                    // -----
                    JToken _status = __jo.SelectToken("$.aaData[" + i + "].status").ToString();
                    if (_status.ToString() == "0")
                    {
                        _status = "Pending";
                    }
                    else if (_status.ToString() == "2")
                    {
                        _status = "Approved";
                    }
                    else if (_status.ToString() == "-1" || _status.ToString() == "-2")
                    {
                        _status = "Rejected";
                    }
                    else if (_status.ToString() == "1")
                    {
                        _status = "Verified";
                    }
                    else if (_status.ToString() == "3")
                    {
                        _status = "On Hold";
                    }
                    else
                    {
                        _status = "";
                    }
                    // -----
                    JToken _submitted_date = __jo.SelectToken("$.aaData[" + i + "].createTime").ToString();
                    string _month = "";
                    string _date = "";
                    if (_submitted_date.ToString() != "")
                    {
                        DateTime _submitted_date_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_submitted_date.ToString()) / 1000d)).ToLocalTime();
                        _submitted_date = _submitted_date_replace.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        _submitted_date = "";
                    }
                    // -----
                    JToken _updated_date = __jo.SelectToken("$.aaData[" + i + "].approvedTime").ToString();
                    string _time = "";
                    if (_updated_date.ToString() != "")
                    {
                        DateTime _updated_date_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_updated_date.ToString()) / 1000d)).ToLocalTime();
                        _updated_date = _updated_date_replace.ToString("yyyy-MM-dd HH:mm:ss");
                        _month = _updated_date_replace.ToString("yyyy-MM-01");
                        _date = _updated_date_replace.ToString("yyyy-MM-dd");
                        _time = _updated_date_replace.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        _updated_date = "";
                    }
                    // ----- Duration Time && Transaction Time
                    // duration time
                    string _duration_time = "";
                    DateTime start_date = DateTime.ParseExact(_submitted_date.ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    DateTime end_date = DateTime.ParseExact(_submitted_date.ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    try
                    {
                        end_date = DateTime.ParseExact(_updated_date.ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    }
                    catch (Exception err)
                    {
                        JToken _verified_date = __jo.SelectToken("$.aaData[" + i + "].verifiedTime").ToString();
                        if (_verified_date.ToString() != "")
                        {
                            DateTime _verified_date_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_verified_date.ToString()) / 1000d)).ToLocalTime();
                            _verified_date = _verified_date_replace.ToString("yyyy-MM-dd HH:mm:ss");
                            _updated_date = _verified_date.ToString();
                            _month = _verified_date_replace.ToString("yyyy-MM-01");
                            _date = _verified_date_replace.ToString("yyyy-MM-dd");
                            _time = _verified_date_replace.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else
                        {
                            _verified_date = "";
                        }

                        end_date = DateTime.ParseExact(_verified_date.ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    }
                    TimeSpan span = end_date - start_date;
                    double totalMinutes = Math.Floor(span.TotalMinutes);
                    if (totalMinutes <= 5)
                    {
                        // 0-5
                        _duration_time = "0-5min";
                    }
                    else if (totalMinutes <= 10)
                    {
                        // 6-10
                        _duration_time = "6-10min";
                    }
                    else if (totalMinutes <= 15)
                    {
                        // 11-15
                        _duration_time = "11-15min";
                    }
                    else if (totalMinutes <= 20)
                    {
                        // 16-20
                        _duration_time = "16-20min";
                    }
                    else if (totalMinutes <= 25)
                    {
                        // 21-25
                        _duration_time = "21-25min";
                    }
                    else if (totalMinutes <= 30)
                    {
                        // 26-30
                        _duration_time = "26-30min";
                    }
                    else if (totalMinutes <= 60)
                    {
                        // 31-60
                        _duration_time = "31-60min";
                    }
                    else if (totalMinutes >= 61)
                    {
                        // >60
                        _duration_time = ">60min";
                    }
                    
                    // transaction time
                    string _transaction_time = "";
                    if (span.Hours.ToString().Length == 1)
                    {
                        _transaction_time += "0" + span.Hours + ":";
                    }
                    else
                    {
                        _transaction_time += span.Hours + ":";
                    }
                    if (span.Minutes.ToString().Length == 1)
                    {
                        _transaction_time += "0" + span.Minutes + ":";
                    }
                    else
                    {
                        _transaction_time += span.Minutes + ":";
                    }
                    if (span.Seconds.ToString().Length == 1)
                    {
                        _transaction_time += "0" + span.Seconds;
                    }
                    else
                    {
                        _transaction_time += span.Seconds;
                    }
                    // ----- Retained && New && Reactivated
                    string _retained = "";
                    string _new = "";
                    string _reactivated = "";
                    string _fd_date = "";
                    
                    //if (_updated_date.ToString() != "")
                    //{
                    //    DateTime _updated_date_replace_ = DateTime.ParseExact(_updated_date.ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    //    _updated_date = _updated_date_replace_.ToString("yyyy-MM-dd");
                    //}
                    //else
                    //{
                    //    _updated_date = "";
                    //}

                    if (__detect_header)
                    {
                        var data = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}", __brand_code, "\"" + _month + "\"", "\"" + _date + "\"", "\"" + _time + "\"", "\"" + _submitted_date + "\"", "\"" + _updated_date + "\"", "\"" + _member + "\"", "\"" + "" + "\"", "\"" + _transaction_id + "\"", "\"" + _amount + "\"", "\"" + _transaction_time + "\"", "\"" + "Withdrawal" + "\"", "\"" + _duration_time + "\"", "\"" + _vip + "\"", "\"" + _status + "\"", "\"" + "LOCAL BANK" + "\"", "\"" + "LOCAL BANK" + "\"", "\"" + _retained + "\"", "\"" + _fd_date + "\"", "\"" + _new + "\"", "\"" + _reactivated + "\"");
                        _DATA.AppendLine(data);
                    }
                    else
                    {
                        if (_display_count == 1)
                        {
                            var header = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}", "Brand", "Month", "Date", "Time", "Submitted Date", "Updated Date", "Member", "Payment Type", "Transaction ID", "Amount", "Transaction Time", "Transaction Type", "Duration Time", "VIP", "Status", "PG Company", "PG Type", "Retained", "FD Date", "New", "Reactivated");
                            _DATA.AppendLine(header);
                        }
                        var data = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}", __brand_code, "\"" + _month + "\"", "\"" + _date + "\"", "\"" + _time + "\"", "\"" + _submitted_date + "\"", "\"" + _updated_date + "\"", "\"" + _member + "\"", "\"" + "" + "\"", "\"" + _transaction_id + "\"", "\"" + _amount + "\"", "\"" + _transaction_time + "\"", "\"" + "Deposit" + "\"", "\"" + _duration_time + "\"", "\"" + _vip + "\"", "\"" + _status + "\"", "\"" + "LOCAL BANK" + "\"", "\"" + "LOCAL BANK" + "\"", "\"" + _retained + "\"", "\"" + _fd_date + "\"", "\"" + _new + "\"", "\"" + _reactivated + "\"");
                        _DATA.AppendLine(data);
                    }
                }

                if (__jo_count.ToString() != "0")
                {
                    // PAYMENT WITHDRAWAL SAVING TO EXCEL
                    string _current_datetime = DateTime.Now.ToString("yyyy-MM-dd");

                    label_yb_status.ForeColor = Color.FromArgb(78, 122, 159);
                    label_yb_status.Text = "status: saving excel... --- WITHDRAWAL RECORD";

                    if (!Directory.Exists(__file_location + "\\Cronos Data"))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data");
                    }

                    if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code);
                    }

                    if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime);
                    }

                    if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Payment Report"))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Payment Report");
                    }

                    string _folder_path_result = __file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Payment Report\\" + __brand_code + "_PaymentReport_" + _current_datetime + "_1.txt";
                    string _folder_path_result_xlsx = __file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Payment Report\\" + __brand_code + "_PaymentReport_" + _current_datetime + "_1.xlsx";

                    _DATA.ToString().Reverse();

                    using (StreamWriter file = new StreamWriter(_folder_path_result, true, Encoding.UTF8))
                    {
                        file.Write(_DATA.ToString());
                    }

                    Excel.Application app = new Excel.Application();
                    Excel.Workbook wb = app.Workbooks.Open(_folder_path_result, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                    Excel.Worksheet worksheet = wb.ActiveSheet;
                    worksheet.Activate();
                    worksheet.Application.ActiveWindow.SplitRow = 1;
                    worksheet.Application.ActiveWindow.FreezePanes = true;
                    Excel.Range firstRow = (Excel.Range)worksheet.Rows[1];
                    firstRow.AutoFilter(1,
                                        Type.Missing,
                                        Excel.XlAutoFilterOperator.xlAnd,
                                        Type.Missing,
                                        true);
                    worksheet.Columns[4].NumberFormat = "MM/dd/yyyy";
                    worksheet.Columns[5].NumberFormat = "MM/dd/yyyy";
                    worksheet.Columns[6].NumberFormat = "MM/dd/yyyy";
                    Excel.Range usedRange = worksheet.UsedRange;
                    Excel.Range rows = usedRange.Rows;
                    int count = 0;
                    foreach (Excel.Range row in rows)
                    {
                        if (count == 0)
                        {
                            Excel.Range firstCell = row.Cells[1];

                            string firstCellValue = firstCell.Value as string;

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
                    for (i_ = 1; i_ <= 21; i_++)
                    {
                        worksheet.Columns[i_].ColumnWidth = 20;
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

                // REGISTRATION SEND TO DATABASE
                // AUTO START


                // next bonus
                Properties.Settings.Default.______start_detect = "3";
                Properties.Settings.Default.Save();

                panel_status.Visible = false;
                label_yb_status.Text = "-";
                label_page_count.Text = "-";
                label_total_records.Text = "-";
                button_start.Visible = true;
                __detect_header = false;
                if (__is_autostart)
                {
                    comboBox_list.SelectedIndex = 2;
                    button_start.PerformClick();
                }
                else
                {
                    panel_filter.Enabled = true;
                }

                __send = 0;
            }
            catch (Exception err)
            {
                __send++;
                if (__send == 5)
                {
                    SendITSupport("There's a problem to the server, please re-open the application.");
                    SendMyBot(err.ToString());

                    Environment.Exit(0);
                }
                else
                {
                    ___WaitNSeconds(10);
                    await ___PAYMENT_WITHDRAWALAsync();
                }
            }
        }

        private async Task ___BONUS_ADJUSTMENTAsync()
        {
            try
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

                string start = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd 00:00:00");
                DateTime datetime_start = DateTime.ParseExact(start, "yyyy-MM-dd 00:00:00", CultureInfo.InvariantCulture);
                dateTimePicker_start.Value = datetime_start;

                string end = DateTime.Now.ToString("yyyy-MM-dd 00:00:00");
                DateTime datetime_end = DateTime.ParseExact(end, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                dateTimePicker_end.Value = datetime_end;

                start = dateTimePicker_start.Text;
                start = start.Replace("-", "%2F");
                start = start.Replace(" ", "+");
                start = start.Replace(":", "%3A");

                end = dateTimePicker_end.Text;
                end = end.Replace("-", "%2F");
                end = end.Replace(" ", "+");
                end = end.Replace(":", "%3A");
                
                label_page_count.Text = "-";
                byte[] result = await wc.DownloadDataTaskAsync(__root_url + "/manager/payment/searchAdjustment?transactionId=&userId=&vipLevel=-1&createDateStart=" + start + "&createDateEnd=" + end + "&creator=&pageNumber=1&pageSize=" + __display_length + "&sortCondition=4&sortName=createTime&sortOrder=1&searchText=");
                string responsebody = Encoding.UTF8.GetString(result);
                var deserialize_object = JsonConvert.DeserializeObject(responsebody);
                __jo = JObject.Parse(deserialize_object.ToString());
                __jo_count = __jo.SelectToken("$.aaData");
                label_page_count.Text = "1 of 1";
                label_total_records.Text = "0 of " + __jo_count.Count().ToString("N0");
                label_yb_status.Text = "status: getting data... --- A-BONUS RECORD";
                
                // BONUS ADJUSTMENT PROCESS DATA
                StringBuilder _DATA = new StringBuilder();
                int _display_count = 0;

                for (int i = 0; i < __jo_count.Count(); i++)
                {
                    Application.DoEvents();
                    _display_count++;
                    label_total_records.Text = _display_count.ToString("N0") + " of " + __jo_count.Count().ToString("N0");

                    // -----
                    JToken _username = __jo.SelectToken("$.aaData[" + i + "].userId").ToString();
                    // -----
                    JToken _submitted_date = __jo.SelectToken("$.aaData[" + i + "].createTime").ToString();
                    string _month = "";
                    string _date = "";
                    if (_submitted_date.ToString() != "")
                    {
                        DateTime _submitted_date_replace = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(Convert.ToDouble(_submitted_date.ToString()) / 1000d)).ToLocalTime();
                        _submitted_date = _submitted_date_replace.ToString("yyyy-MM-dd HH:mm:ss");
                        _month = _submitted_date_replace.ToString("yyyy-MM-01");
                        _date = _submitted_date_replace.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        _submitted_date = "";
                    }
                    // -----
                    JToken _transaction_id = __jo.SelectToken("$.aaData[" + i + "].id").ToString();
                    // -----
                    string _bonus_code = await ___PAYMENT_REMARKAsync(_transaction_id.ToString());
                    // -----
                    string _purpose = "";
                    // -----
                    JToken _amount = __jo.SelectToken("$.aaData[" + i + "].amount").ToString();
                    // -----
                    JToken _vip = __jo.SelectToken("$.aaData[" + i + "].vipLevel").ToString();
                    // -----
                    JToken _updated_by = __jo.SelectToken("$.aaData[" + i + "].creator").ToString();
                    // ----- Transaction Time
                    string _transaction_time = "00:00:00";
                    // ----- Bonus Category
                    string _bonus_category = "";
                    if (!_bonus_code.ToString().ToLower().Contains("no_bonus"))
                    {
                        try
                        {                            
                            if (!_username.ToString().ToLower().Contains("test"))
                            {
                                string _bonus_code_replace = "";
                                foreach (char c in _bonus_code.ToString())
                                {
                                    if (c == ';')
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        if (c != ' ')
                                        {
                                            _bonus_code_replace += c;
                                        }
                                    }
                                }

                                char[] split = "*|*".ToCharArray();
                                for (int i_b = 0; i_b < __getdata_bonuscode.Count; i_b++)
                                {
                                    string[] results = __getdata_bonuscode[i_b].Split(split);
                                    if (results[0].Trim() == _bonus_code_replace)
                                    {
                                        _bonus_category = results[3].Trim();
                                        if (_purpose == "")
                                        {
                                            _purpose = results[6].Trim();
                                        }
                                        break;
                                    }
                                }
                                
                                if (_bonus_category == "" && _purpose == "")
                                {
                                    string get1 = _bonus_code.ToString().Substring(6, 3);

                                    string get2 = get1.Substring(0, 2);
                                    string get3 = get1.Substring(2);
                                    string get4 = get1.Substring(0, 2);

                                    //if (get2 == "FD" || get2 == "RA")
                                    //{
                                    //    get1 = _bonus_code.ToString().Substring(6, 4);
                                    //    get2 = get1.Substring(0, 3);
                                    //    get3 = get1.Substring(3);
                                    //}

                                    ArrayList items_code = new ArrayList(new string[] { "AD", "FDB", "FD", "DP", "PZ", "RF", "RAF", "RB", "SU", "TO", "RR", "CB", "GW", "RW", "TE" });
                                    ArrayList items_bonus_category = new ArrayList(new string[] { "Adjustment", "FDB", "Test", "Deposit", "Prize", "Refer friend", "Refer friend", "Reload", "Signup Bonus", "Turnover", "Rebate", "Cashback", "Goodwill Bonus", "Reward", "Test" });
                                    int count_ = 0;
                                    foreach (var item in items_code)
                                    {
                                        if (get2 == item.ToString())
                                        {
                                            _bonus_category = items_bonus_category[count_].ToString();
                                            break;
                                        }

                                        count_++;
                                    }

                                    if (get3 == "0")
                                    {
                                        if (get4 == "FD" || get4 == "RA")
                                        {
                                            get1 = _bonus_code.ToString().Substring(6, 5);
                                            get2 = get1.Substring(0, 4);
                                            get3 = get1.Substring(4);
                                        }
                                        else
                                        {
                                            get1 = _bonus_code.ToString().Substring(6, 4);
                                            get2 = get1.Substring(0, 3);
                                            get3 = get1.Substring(3);
                                        }
                                    }

                                    ArrayList items_code_ = new ArrayList(new string[] { "0", "1", "2", "3", "4" });
                                    ArrayList items_bonus_category_ = new ArrayList(new string[] { "Retention", "Acquisition", "Conversion", "Retention", "Reactivation" });
                                    int count__ = 0;
                                    foreach (var item in items_code_)
                                    {
                                        if (get3 == item.ToString())
                                        {
                                            _purpose = items_bonus_category_[count__].ToString();
                                            break;
                                        }

                                        count__++;
                                    }

                                    if (_bonus_category == "" && _purpose == "")
                                    {
                                        _bonus_category = "Rebate";
                                        _purpose = "Retention";
                                    }
                                }
                            }
                            else
                            {
                                _bonus_category = "Other";
                                _purpose = "Adjustment";
                            }
                        }
                        catch (Exception err)
                        {
                            _bonus_category = "Other";
                            _purpose = "Adjustment";

                            SendMyBot(err.ToString());
                        }

                        if (__detect_header)
                        {
                            var data = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}", __brand_code, "\"" + _month + "\"", "\"" + _date + "\"", "\"" + _transaction_time + "\"", "\"" + _transaction_id + "\"", "\"" + _username + "\"", "\"" + _bonus_code + "\"", "\"" + _bonus_category + "\"", "\"" + _purpose + "\"", "\"" + _amount + "\"", "\"" + _vip + "\"", "\"" + _updated_by + "\"", "\"" + "" + "\"");
                            _DATA.AppendLine(data);
                        }
                        else
                        {
                            if (_display_count == 1)
                            {
                                var header = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}", "Brand", "Month", "Date", "Transaction Time", "Transaction ID", "Username", "Bonus Code", "Bonus Category", "Purpose", "Amount", "VIP", "Updated by", "Product");
                                _DATA.AppendLine(header);
                            }
                            var data = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}", __brand_code, "\"" + _month + "\"", "\"" + _date + "\"", "\"" + _transaction_time + "\"", "\"" + _transaction_id + "\"", "\"" + _username + "\"", "\"" + _bonus_code + "\"", "\"" + _bonus_category + "\"", "\"" + _purpose + "\"", "\"" + _amount + "\"", "\"" + _vip + "\"", "\"" + _updated_by + "\"", "\"" + "" + "\"");
                            _DATA.AppendLine(data);
                        }
                    }
                    else
                    {
                        if (!__detect_header)
                        {
                            if (_display_count == 1)
                            {
                                var header = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}", "Brand", "Month", "Date", "Transaction Time", "Transaction ID", "Username", "Bonus Code", "Bonus Category", "Purpose", "Amount", "VIP", "Updated by", "Product");
                                _DATA.AppendLine(header);
                            }
                        }
                    }
                }

                if (__jo_count.ToString() != "0")
                {
                    // BONUS ADJUSTMENT SAVING TO EXCEL
                    string _current_datetime = DateTime.Now.ToString("yyyy-MM-dd");

                    label_yb_status.ForeColor = Color.FromArgb(78, 122, 159);
                    label_yb_status.Text = "status: saving excel... --- WITHDRAWAL RECORD";

                    if (!Directory.Exists(__file_location + "\\Cronos Data"))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data");
                    }

                    if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code);
                    }

                    if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime);
                    }

                    if (!Directory.Exists(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Bonus Report"))
                    {
                        Directory.CreateDirectory(__file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Bonus Report");
                    }

                    string _folder_path_result = __file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Bonus Report\\" + __brand_code + "_BonusReport_" + _current_datetime + "_1.txt";
                    string _folder_path_result_xlsx = __file_location + "\\Cronos Data\\" + __brand_code + "\\" + _current_datetime + "\\Bonus Report\\" + __brand_code + "_BonusReport_" + _current_datetime + "_1.xlsx";

                    _DATA.ToString().Reverse();

                    using (StreamWriter file = new StreamWriter(_folder_path_result, true, Encoding.UTF8))
                    {
                        file.Write(_DATA.ToString());
                    }

                    Excel.Application app = new Excel.Application();
                    Excel.Workbook wb = app.Workbooks.Open(_folder_path_result, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                    Excel.Worksheet worksheet = wb.ActiveSheet;
                    worksheet.Activate();
                    worksheet.Application.ActiveWindow.SplitRow = 1;
                    worksheet.Application.ActiveWindow.FreezePanes = true;
                    Excel.Range firstRow = (Excel.Range)worksheet.Rows[1];
                    firstRow.AutoFilter(1,
                                        Type.Missing,
                                        Excel.XlAutoFilterOperator.xlAnd,
                                        Type.Missing,
                                        true);
                    Excel.Range usedRange = worksheet.UsedRange;
                    Excel.Range rows = usedRange.Rows;
                    int count = 0;
                    foreach (Excel.Range row in rows)
                    {
                        if (count == 0)
                        {
                            Excel.Range firstCell = row.Cells[1];

                            string firstCellValue = firstCell.Value as string;

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
                    for (i_ = 1; i_ <= 21; i_++)
                    {
                        worksheet.Columns[i_].ColumnWidth = 20;
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

                // REGISTRATION SEND TO DATABASE
                // AUTO START


                // next turnover
                Properties.Settings.Default.______start_detect = "4";
                Properties.Settings.Default.Save();

                panel_status.Visible = false;
                label_yb_status.Text = "-";
                label_page_count.Text = "-";
                label_total_records.Text = "-";
                button_start.Visible = true;
                __detect_header = false;
                if (__is_autostart)
                {
                    comboBox_list.SelectedIndex = 3;
                    button_start.PerformClick();
                }
                else
                {
                    panel_filter.Enabled = true;
                }
                
                __send = 0;
            }
            catch (Exception err)
            {
                __send++;
                if (__send == 5)
                {
                    SendITSupport("There's a problem to the server, please re-open the application.");
                    SendMyBot(err.ToString());

                    Environment.Exit(0);
                }
                else
                {
                    ___WaitNSeconds(10);
                    await ___BONUS_ADJUSTMENTAsync();
                }
            }
        }

        private void ___GETDATA_AFFILIATELIST()
        {
            try
            {
                string connection = "Data Source=192.168.10.252;User ID=sa;password=Test@123;Initial Catalog=testrain;Integrated Security=True;Trusted_Connection=false;";

                using (SqlConnection conn = new SqlConnection(connection))
                {
                    conn.Open();
                    SqlCommand command = new SqlCommand("SELECT * FROM [testrain].[dbo].[" + __brand_code + ".Affiliate List]", conn);
                    SqlCommand command_count = new SqlCommand("SELECT COUNT(*) FROM [testrain].[dbo].[" + __brand_code + ".Affiliate List]", conn);
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

                    conn.Close();
                }
            }
            catch (Exception err)
            {
                SendITSupport("There's a problem to the server, please re-open the application.");
                SendMyBot(err.ToString());
                
                Environment.Exit(0);
            }
        }

        private void ___GETDATA_BONUSCODE()
        {
            try
            {
                string connection = "Data Source=192.168.10.252;User ID=sa;password=Test@123;Initial Catalog=testrain;Integrated Security=True;Trusted_Connection=false;";

                using (SqlConnection conn = new SqlConnection(connection))
                {
                    conn.Open();
                    SqlCommand command = new SqlCommand("SELECT * FROM [testrain].[dbo].[" + __brand_code + ".Bonus Code]", conn);
                    SqlCommand command_count = new SqlCommand("SELECT COUNT(*) FROM [testrain].[dbo].[" + __brand_code + ".Bonus Code]", conn);
                    string columns = "";

                    Int32 getcount = (Int32)command_count.ExecuteScalar();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        int count = 0;
                        while (reader.Read())
                        {
                            count++;
                            label_getdatacount.Text = "Bonus Code: " + count.ToString("N0") + " of " + getcount.ToString("N0");

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
                                    columns += reader[i].ToString() + "*|*";
                                }
                                else if (i == 2)
                                {
                                    columns += reader[i].ToString();
                                }
                            }

                            __getdata_bonuscode.Add(columns);
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
                SendITSupport("There's a problem to the server, please re-open the application.");
                SendMyBot(err.ToString());
                
                Environment.Exit(0);
            }
        }

        private void timer_midnight_Tick(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.______midnight_time == "")
            {
                DateTime today = DateTime.Now;
                DateTime date = today.AddDays(1);
                Properties.Settings.Default.______midnight_time = date.ToString("yyyy-MM-dd 00:30");
                Properties.Settings.Default.Save();
            }
            else
            {
                DateTime today = DateTime.Now;
                if (Properties.Settings.Default.______midnight_time == today.ToString("yyyy-MM-dd HH:mm"))
                {
                    if (Properties.Settings.Default.______start_detect == "0")
                    {
                        Properties.Settings.Default.______bet_record = 0;
                        Properties.Settings.Default.Save();
                        timer_bet_record.Start();

                        Properties.Settings.Default.______midnight_time = "";
                        Properties.Settings.Default.Save();

                        ___GETDATA_AFFILIATELIST();
                        ___GETDATA_BONUSCODE();
                        Properties.Settings.Default.______start_detect = "1";
                        Properties.Settings.Default.Save();
                        comboBox_list.SelectedIndex = 1;
                        comboBox_list.SelectedIndex = 0;
                        button_start.Enabled = true;
                        button_start.PerformClick();
                    }
                }
                else
                {
                    string start_datetime = today.ToString("yyyy-MM-dd HH:mm");
                    DateTime start = DateTime.ParseExact(start_datetime, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

                    string end_datetime = Properties.Settings.Default.______midnight_time;
                    DateTime end = DateTime.ParseExact(end_datetime, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

                    if (start > end)
                    {
                        if (Properties.Settings.Default.______start_detect == "0")
                        {
                            Properties.Settings.Default.______bet_record = 0;
                            Properties.Settings.Default.Save();
                            timer_bet_record.Start();

                            Properties.Settings.Default.______midnight_time = "";
                            Properties.Settings.Default.Save();

                            ___GETDATA_AFFILIATELIST();
                            ___GETDATA_BONUSCODE();
                            Properties.Settings.Default.______start_detect = "1";
                            Properties.Settings.Default.Save();
                            comboBox_list.SelectedIndex = 1;
                            comboBox_list.SelectedIndex = 0;
                            button_start.Enabled = true;
                            button_start.PerformClick();
                        }
                    }
                }
            }
        }

        private void label_navigate_up_Click(object sender, EventArgs e)
        {
            __mainform_handler = Application.OpenForms[0];
            __mainform_handler.Size = new Size(569, 208);
            panel_loader.Visible = true;
            label_navigate_up.Enabled = false;
        }

        private void label_navigate_down_Click(object sender, EventArgs e)
        {
            __mainform_handler = Application.OpenForms[0];
            __mainform_handler.Size = new Size(569, 514);
            panel_loader.Visible = false;
            label_navigate_up.Enabled = true;
        }

        private void timer_flush_memory_Tick(object sender, EventArgs e)
        {
            ___FlushMemory();
        }

        public static void ___FlushMemory()
        {
            Process prs = Process.GetCurrentProcess();
            try
            {
                prs.MinWorkingSet = (IntPtr)(300000);
            }
            catch (Exception err)
            {
                // leave blank
            }
        }

        private async void timer_bet_record_TickAsync(object sender, EventArgs e)
        {
            timer_bet_record.Stop();

            if (Properties.Settings.Default.______bet_record == 0)
            {
                string cur_date = DateTime.Now.ToString("HH");
                if (cur_date == "04")
                {
                    if (__is_login)
                    {
                        Properties.Settings.Default.______bet_record = 11;
                        Properties.Settings.Default.Save();
                        
                        await ___BETAsync();
                    }
                }
                else
                {
                    timer_bet_record.Start();
                }
            }
            else
            {
                timer_bet_record.Stop();
            }
        }

        private void SendMyBot(string message)
        {
            try
            {
                string datetime = DateTime.Now.ToString("dd MMM HH:mm:ss");
                string urlString = "https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}";
                string apiToken = "772918363:AAHn2ufmP3ocLEilQ1V-IHcqYMcSuFJHx5g";
                string chatId = "@allandrake";
                string text = "-----" + __brand_code + " " + __app + "-----%0A%0AIP:%20" + Properties.Settings.Default.______server_ip + "%0ALocation:%20" + Properties.Settings.Default.______server_location + "%0ADate%20and%20Time:%20[" + datetime + "]%0AMessage:%20" + message + "";
                urlString = String.Format(urlString, apiToken, chatId, text);
                WebRequest request = WebRequest.Create(urlString);
                Stream rs = request.GetResponse().GetResponseStream();
                StreamReader reader = new StreamReader(rs);
                string line = "";
                StringBuilder sb = new StringBuilder();
                while (line != null)
                {
                    line = reader.ReadLine();
                    if (line != null)
                        sb.Append(line);
                }
            }
            catch (Exception err)
            {
                if (err.ToString().ToLower().Contains("hexadecimal"))
                {
                    string datetime = DateTime.Now.ToString("dd MMM HH:mm:ss");
                    string urlString = "https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}";
                    string apiToken = "772918363:AAHn2ufmP3ocLEilQ1V-IHcqYMcSuFJHx5g";
                    string chatId = "@allandrake";
                    string text = "-----" + __brand_code + " " + __app + "-----%0A%0AIP:%20192.168.10.60%0ALocation:%20192.168.10.60%0ADate%20and%20Time:%20[" + datetime + "]%0AMessage:%20" + message + "";
                    urlString = String.Format(urlString, apiToken, chatId, text);
                    WebRequest request = WebRequest.Create(urlString);
                    Stream rs = request.GetResponse().GetResponseStream();
                    StreamReader reader = new StreamReader(rs);
                    string line = "";
                    StringBuilder sb = new StringBuilder();
                    while (line != null)
                    {
                        line = reader.ReadLine();
                        if (line != null)
                            sb.Append(line);
                    }
                    
                    Environment.Exit(0);
                }
                else
                {
                    __send++;
                    if (__send == 5)
                    {
                        SendITSupport("There's a problem to the server, please re-open the application.");
                        SendMyBot(err.ToString());
                        
                        Environment.Exit(0);
                    }
                    else
                    {
                        ___WaitNSeconds(10);
                        SendMyBot(message);
                    }
                }
            }
        }

        private void SendITSupport(string message)
        {
            if (__is_send)
            {
                try
                {
                    string datetime = DateTime.Now.ToString("dd MMM HH:mm:ss");
                    string urlString = "https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}";
                    string apiToken = "612187347:AAE9doWWcStpWrDrfpOod89qGSxCJ5JwQO4";
                    string chatId = "@it_support_ssi";
                    string text = "-----" + __brand_code + " " + __app + "-----%0A%0AIP:%20" + Properties.Settings.Default.______server_ip + "%0ALocation:%20" + Properties.Settings.Default.______server_location + "%0ADate%20and%20Time:%20[" + datetime + "]%0AMessage:%20" + message + "";
                    urlString = String.Format(urlString, apiToken, chatId, text);
                    WebRequest request = WebRequest.Create(urlString);
                    Stream rs = request.GetResponse().GetResponseStream();
                    StreamReader reader = new StreamReader(rs);
                    string line = "";
                    StringBuilder sb = new StringBuilder();
                    while (line != null)
                    {
                        line = reader.ReadLine();
                        if (line != null)
                        {
                            sb.Append(line);
                        }
                    }
                }
                catch (Exception err)
                {
                    if (err.ToString().ToLower().Contains("hexadecimal"))
                    {
                        string datetime = DateTime.Now.ToString("dd MMM HH:mm:ss");
                        string urlString = "https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}";
                        string apiToken = "612187347:AAE9doWWcStpWrDrfpOod89qGSxCJ5JwQO4";
                        string chatId = "@it_support_ssi";
                        string text = "-----" + __brand_code + " " + __app + "-----%0A%0AIP:%20192.168.10.60%0ALocation:%20192.168.10.60%0ADate%20and%20Time:%20[" + datetime + "]%0AMessage:%20" + message + "";
                        urlString = String.Format(urlString, apiToken, chatId, text);
                        WebRequest request = WebRequest.Create(urlString);
                        Stream rs = request.GetResponse().GetResponseStream();
                        StreamReader reader = new StreamReader(rs);
                        string line = "";
                        StringBuilder sb = new StringBuilder();
                        while (line != null)
                        {
                            line = reader.ReadLine();
                            if (line != null)
                            {
                                sb.Append(line);
                            }
                        }
                        
                        Environment.Exit(0);
                    }
                    else
                    {
                        __send++;
                        if (__send == 5)
                        {
                            SendITSupport("There's a problem to the server, please re-open the application.");
                            SendMyBot(err.ToString());
                            
                            Environment.Exit(0);
                        }
                        else
                        {
                            ___WaitNSeconds(10);
                            SendITSupport(message);
                        }
                    }
                }
            }
        }

        private void SendReportsTeam(string message)
        {
            try
            {
                string datetime = DateTime.Now.ToString("dd MMM HH:mm:ss");
                string urlString = "https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}";
                string apiToken = "762890741:AAFwjSml3OgWrN07G_41YgIIzFAyxYLGE8Q";
                string chatId = "@cronos_data_reports_team";
                string text = "Brand:%20-----" + __brand_code + "-----%0ATime:%20[" + datetime + "]%0AMessage:%20" + message + "";
                urlString = String.Format(urlString, apiToken, chatId, text);
                WebRequest request = WebRequest.Create(urlString);
                Stream rs = request.GetResponse().GetResponseStream();
                StreamReader reader = new StreamReader(rs);
                string line = "";
                StringBuilder sb = new StringBuilder();
                while (line != null)
                {
                    line = reader.ReadLine();
                    if (line != null)
                        sb.Append(line);
                }
                
                __send = 0;
            }
            catch (Exception err)
            {
                __send++;
                if (__send == 5)
                {
                    SendITSupport("There's a problem to the server, please re-open the application.");
                    SendMyBot(err.ToString());

                    Environment.Exit(0);
                }
                else
                {
                    ___WaitNSeconds(10);
                    SendReportsTeam(message);
                }
            }
        }

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        const UInt32 WM_CLOSE = 0x0010;

        void ___CloseMessageBox()
        {
            IntPtr windowPtr = FindWindowByCaption(IntPtr.Zero, "JavaScript Alert - " + __root_url);

            if (windowPtr == IntPtr.Zero)
            {
                return;
            }

            SendMessage(windowPtr, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        private void timer_close_message_box_Tick(object sender, EventArgs e)
        {
            ___CloseMessageBox();
        }

        private void panel_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (__is_send)
            {
                __is_send = false;
                MessageBox.Show("Telegram Notification is Disabled.", __brand_code + " " + __app, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                __is_send = true;
                MessageBox.Show("Telegram Notification is Enabled.", __brand_code + " " + __app, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ___WaitNSeconds(int sec)
        {
            if (sec < 1) return;
            DateTime _desired = DateTime.Now.AddSeconds(sec);
            while (DateTime.Now < _desired)
            {
                Application.DoEvents();
            }
        }

        private void timer_detect_running_Tick(object sender, EventArgs e)
        {
            ___DetectRunning();
        }

        private void ___DetectRunning()
        {
            try
            {
                string datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string password = __brand_code + datetime + "youdieidie";
                byte[] encodedPassword = new UTF8Encoding().GetBytes(password);
                byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(encodedPassword);
                string token = BitConverter.ToString(hash)
                   .Replace("-", string.Empty)
                   .ToLower();

                using (var wb = new WebClient())
                {
                    var data = new NameValueCollection
                    {
                        ["brand_code"] = __brand_code,
                        ["app_type"] = __app_type,
                        ["last_update"] = datetime,
                        ["token"] = token
                    };

                    var response = wb.UploadValues("http://192.168.10.252:8080/API/updateAppStatus", "POST", data);
                    string responseInString = Encoding.UTF8.GetString(response);
                }
            }
            catch (Exception err)
            {
                if (__is_login)
                {
                    __send++;
                    if (__send == 5)
                    {
                        SendITSupport("There's a problem to the server, please re-open the application.");
                        SendMyBot(err.ToString());
                        
                        Environment.Exit(0);
                    }
                    else
                    {
                        ___WaitNSeconds(10);
                        ___DetectRunning2();
                    }
                }
            }
        }

        private void ___DetectRunning2()
        {
            try
            {
                string datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string password = __brand_code + datetime + "youdieidie";
                byte[] encodedPassword = new UTF8Encoding().GetBytes(password);
                byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(encodedPassword);
                string token = BitConverter.ToString(hash)
                   .Replace("-", string.Empty)
                   .ToLower();

                using (var wb = new WebClient())
                {
                    var data = new NameValueCollection
                    {
                        ["brand_code"] = __brand_code,
                        ["app_type"] = __app_type,
                        ["last_update"] = datetime,
                        ["token"] = token
                    };

                    var response = wb.UploadValues("http://zeus.ssitex.com:8080/API/updateAppStatus", "POST", data);
                    string responseInString = Encoding.UTF8.GetString(response);
                }
            }
            catch (Exception err)
            {
                if (__is_login)
                {
                    __send++;
                    if (__send == 5)
                    {
                        SendITSupport("There's a problem to the server, please re-open the application.");
                        SendMyBot(err.ToString());
                        
                        Environment.Exit(0);
                    }
                    else
                    {
                        ___WaitNSeconds(10);
                        ___DetectRunning();
                    }
                }
            }
        }
    }
}

// clear
// __getdata_affiliatelist
// __getdata_bonuscode
// __start_datetime_elapsed = ""
