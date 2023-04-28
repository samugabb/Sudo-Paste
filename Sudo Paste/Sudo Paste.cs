using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaterialSkin.Controls;
using MaterialSkin;
using System.Text.RegularExpressions;
using System.Reflection;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Sudo_Paste
{
    public partial class Form1 : MaterialForm
    {
        BackgroundWorker bgw;
        String TextToWrite;
        bool StartupEnabled;
        bool EnterSuffix;
        Icon[] TimerIcons = new Icon[3];
        Icon Logo;

        // -------------------------------MAIN PROGRAM FUNCTIONS-------------------------------------

        public Form1()
        {
            InitializeComponent();

            // stuff for materialSkin
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            materialSkinManager.ColorScheme = new ColorScheme(Primary.Blue300, Primary.Blue300, Primary.BlueGrey500, Accent.LightBlue200, TextShade.WHITE);

            // declare a new BackgroundWorker
            bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(bgw_DoWork);
            bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler
                    (bgw_RunWorkerCompleted);

            KeyPreview = true;

            this.WindowState = FormWindowState.Minimized; // starts the window as minimized to taskbar
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "Sudo Paste v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(2); // change the form title to the version of the application

            this.notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();                         // add a ContextMenuStrip to the notifyIcon
            this.notifyIcon.ContextMenuStrip.Items.Add("GO", null, this.MenuGo_Click);                              // add a "Go" option to the CMS
            this.notifyIcon.ContextMenuStrip.Items.Add("-");                                                        // add a Separator to the CMS
            this.notifyIcon.ContextMenuStrip.Items.Add("Run at startup [OFF]", null, this.MenuStartup_Click);       // add a "Run at Startup" toggle to the CMS
            this.notifyIcon.ContextMenuStrip.Items.Add("Press ENTER when done [ON]", null, this.MenuENTER_Click);   // add a "Press ENTER when done" toggle to the CMS
            this.notifyIcon.ContextMenuStrip.Items.Add("-");                                                        // add a Separator to the CMS
            this.notifyIcon.ContextMenuStrip.Items.Add("Open", null, this.MenuOpen_Click);                          // add an "Open" option to the CMS
            this.notifyIcon.ContextMenuStrip.Items.Add("Exit", null, this.MenuExit_Click);                          // add an "Exit" option to the CMS
            this.notifyIcon.ContextMenuStrip.Items.Add("-");                                                        // add a Separator to the CMS
            this.notifyIcon.ContextMenuStrip.Items.Add(this.Text);                                                  // add the indication of the software version
            this.notifyIcon.ContextMenuStrip.Items[8].Enabled = false;                                              // disable the row with the software version (it's not a button!)

            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true); // open the registry key for autostart to handle it later
            if (key.GetValue("TextReplicator") == null) // set the checkbox for autostart accordingly to how the registry key is set
            {
                StartupEnabled = false;
                // no need to set the ContextMenuStrip text as it's always set to OFF as default
            }
            else
            {
                StartupEnabled = true;
                this.notifyIcon.ContextMenuStrip.Items[2].Text = "Run at startup [ON]";
            }

            EnterSuffix = true; // automatically enable the ENTER Suffix

            IconsProcessing(); // process the icons to use them later
        }

        // process the timer icons and put them in an icon array. This array will be used by the backgroundworker
        void IconsProcessing()
        {
            Bitmap theBitmap1 = new Bitmap(Properties.Resources._1, new Size(500, 500));
            IntPtr Hicon1 = theBitmap1.GetHicon();                                          // Get an Hicon from the Bitmap
            TimerIcons[0] = Icon.FromHandle(Hicon1);                                               // Create a new icon from the handle

            Bitmap theBitmap2 = new Bitmap(Properties.Resources._2, new Size(500, 500));
            IntPtr Hicon2 = theBitmap2.GetHicon();                                          // Get an Hicon from the Bitmap
            TimerIcons[1] = Icon.FromHandle(Hicon2);                                               // Create a new icon from the handle

            Bitmap theBitmap3 = new Bitmap(Properties.Resources._3, new Size(500, 500));
            IntPtr Hicon3 = theBitmap3.GetHicon();                                          // Get an Hicon from the Bitmap
            TimerIcons[2] = Icon.FromHandle(Hicon3);                                               // Create a new icon from the handle

            // process the app icon
            Bitmap LogoBitmap = new Bitmap(Properties.Resources.SP_Logo, new Size(16, 16));
            IntPtr HiconLogo = LogoBitmap.GetHicon();                                       // Get an Hicon from the Bitmap
            Logo = Icon.FromHandle(HiconLogo);                                              // Create a new icon from the handle
        }

        // -------------------------------BACKGROUNDWORKER-------------------------------------

        // wait and display a timer
        void bgw_DoWork(object sender, DoWorkEventArgs e) // bgw that waits 1 second until the expected timeframe has been met
        {
            for (int i = 3; i > 0; i--)
            {
                this.notifyIcon.Icon = TimerIcons[i-1]; // change the notifyicon icon to reflect the timer status
                System.Threading.Thread.Sleep(1000); // wait 1 second
            }
        }

        // send the keypresses
        void bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) // this is executed when the bgw has done waiting. This does the actual writing
        {
            if (Control.IsKeyLocked(Keys.CapsLock))
                SendKeys.Send("{CAPSLOCK}" + TextToWrite + "{CAPSLOCK}"); // write the clipboard contents after disabling CAPS LOCK, then reenable it
            else
                SendKeys.Send(TextToWrite); // write the clipboard

            if (EnterSuffix) // if the ENTER option is checked, this simulates the ENTER key when done writing
                SendKeys.Send("{Enter}");

            this.notifyIcon.ContextMenuStrip.Items[0].Enabled = true; // re-enable the GO option in the CMS
            this.notifyIcon.Icon = Logo; // reset the NotifyIcon icon to the app icon
            bgw.Dispose(); //dispose of the BackGroundWorker
            this.Hide(); //Hide the GUI if it was active
        }

        // -------------------------------NOTIFY ICON-------------------------------------

        // hide the program icon from the taskbar if the window is minimized
        private void Form1_Resize(object sender, EventArgs e) // this is fired up everythime the window is resized (i.e. when the form is minimized)
        {
            if (WindowState == FormWindowState.Minimized) // if the form is getting minimized
            {
                this.Hide(); // hide its icon from the taskbar
            }
        }

        // make the main window visible when the NotifyIcon is double clicked
        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e) // when the notifyIcon is double clicked
        {
            Show(); // make the window visible again
            this.WindowState = FormWindowState.Normal; // restore the window state
        }

        // -------------------------------MENU FUNCTIONS-------------------------------------

        // do preliminary checks, then fire up the Bgw
        void MenuGo_Click(object sender, EventArgs e) // this is fired up every time the GO button from the Notify Icon is pressed
        {
            if (Clipboard.ContainsText())
            {
                if (Clipboard.GetText().Contains(Environment.NewLine))
                {
                    notifyIcon.BalloonTipText = "Be careful, clipboard contains more than one line!";
                    notifyIcon.ShowBalloonTip(3);
                }
                TextToWrite = Regex.Replace(Clipboard.GetText(), "[+^%~(){}]", "{$0}"); // escape special characters
                this.notifyIcon.ContextMenuStrip.Items[0].Enabled = false;
                bgw.RunWorkerAsync(); // fire up the bgw
            }
            else
            {
                notifyIcon.BalloonTipText = "Clipboard does not contain text!";
                notifyIcon.ShowBalloonTip(3);
            }
        }

        // enable/disable launch at startup
        void MenuStartup_Click(object sender, EventArgs e)
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (!StartupEnabled)
            {
                StartupEnabled = true;
                this.notifyIcon.ContextMenuStrip.Items[2].Text = "Run at startup [ON]";

                key.SetValue("TextReplicator", Application.ExecutablePath);
            }
            else
            {
                StartupEnabled = false;
                this.notifyIcon.ContextMenuStrip.Items[2].Text = "Run at startup [OFF]";

                key.DeleteValue("TextReplicator", false);
            }
        }

        private void SignatureLabel_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://samugabb.github.io/cv/");
        }

        // enable/disable ENTER suffix
        void MenuENTER_Click(object sender, EventArgs e)
        {
            if (EnterSuffix)
            {
                EnterSuffix = false;
                this.notifyIcon.ContextMenuStrip.Items[3].Text = "Press ENTER when done [OFF]";
            }
            else
            {
                EnterSuffix = true;
                this.notifyIcon.ContextMenuStrip.Items[3].Text = "Press ENTER when done [ON]";
            }
        }

        void MenuOpen_Click(object sender, EventArgs e)
        {
            Show(); // make the window visible again
            this.WindowState = FormWindowState.Normal; // restore the window state
        }

        void MenuExit_Click(object sender, EventArgs e)
        {
            Application.Exit(); // close the application
        }
    }
}