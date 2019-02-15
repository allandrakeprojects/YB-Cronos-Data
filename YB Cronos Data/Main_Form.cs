using CefSharp;
using CefSharp.WinForms;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace YB_Cronos_Data
{
    public partial class Main_Form : Form
    {
        private string __url_01 = "http://103.4.104.8/page/manager/login.jsp";
        private string __url = "";
        private string __get_value;
        private int __send = 0;
        private bool __is_close;
        private bool __is_login = false;
        private bool __is_start = false;
        private ChromiumWebBrowser chromeBrowser;
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
                        label_currentrecord.Visible = false;
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
                timer.Stop();

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
                                    // comment
                                    //label_player_last_bill_no.Text = "-";
                                    //label_brand.Visible = false;
                                    pictureBox_loader.Visible = false;
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
                    label_currentrecord.Visible = true;
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
    }
}
