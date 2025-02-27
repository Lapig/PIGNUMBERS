﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using NHotkey;
using NHotkey.Wpf;

namespace PIGNUMBERS
{
    // TODO: Code Optimization
    public partial class MainWindow : Window
    {
        private Log encounterlog;
        private List<Combatant> lastCombatants = new List<Combatant>();
        public DispatcherTimer damageTimer = new DispatcherTimer();
        public static Dictionary<string, string> skillDict = new Dictionary<string, string>();
        public static string[] ignoreskill;
        private List<string> sessionLogFilenames = new List<string>();
        private string lastStatus = "";
        private IntPtr hwndcontainer;
        List<Combatant> workingList = new List<Combatant>();
        Process thisProcess = Process.GetCurrentProcess();

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // Get this window's handle
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            hwndcontainer = hwnd;
        }

        public MainWindow()
        {
            InitializeComponent();

            Dispatcher.UnhandledException += Panic;
            LowResources.IsChecked = Properties.Settings.Default.LowResources;
            CPUdraw.IsChecked = Properties.Settings.Default.CPUdraw;
            if (Properties.Settings.Default.LowResources) { thisProcess.PriorityClass = ProcessPriorityClass.Idle; }
            if (Properties.Settings.Default.CPUdraw)
            {
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            }
            else
            {
                RenderOptions.ProcessRenderMode = RenderMode.Default;
            }

        /*    try { Directory.CreateDirectory("Logs"); }
            catch
            {
                MessageBox.Show("PIGNUMBERS cannot save logs at the moment. \n\nPlease check that you are running PIGNUMBERS as an administrator or that your account has read/write access to this directory", "PIGNUMBERS Setup", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }*/

            //Directory.CreateDirectory("Debug");

            //FileStream filestream = new FileStream("Debug\\log_" + string.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now) + ".txt", FileMode.Create);
            //var streamwriter = new StreamWriter(filestream)
            //{
            //    AutoFlush = true
            //};
            //Console.SetOut(streamwriter);
            //Console.SetError(streamwriter);

            //Console.WriteLine("PIGNUMBERS V." + Assembly.GetExecutingAssembly().GetName().Version);

          /*  if (Properties.Settings.Default.UpgradeRequired && !Properties.Settings.Default.ResetInvoked)
            {
                //Console.WriteLine("Upgrading settings");
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
            }*/

            Properties.Settings.Default.ResetInvoked = false;

            Top = Properties.Settings.Default.Top;
            Left = Properties.Settings.Default.Left;
            Height = Properties.Settings.Default.Height;
            Width = Properties.Settings.Default.Width;

            //Console.WriteLine("Applying UI settings");
            //Console.WriteLine(this.Top = Properties.Settings.Default.Top);
            //Console.WriteLine(this.Left = Properties.Settings.Default.Left);
            //Console.WriteLine(this.Height = Properties.Settings.Default.Height);
            //Console.WriteLine(this.Width = Properties.Settings.Default.Width);

            bool outOfBounds = (Left <= SystemParameters.VirtualScreenLeft - Width) ||
                (Top <= SystemParameters.VirtualScreenTop - Height) ||
                (SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth <= Left) ||
                (SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight <= Top);

            if (outOfBounds)
            {
                //Console.WriteLine("Window's off-screen, resetting");
                Top = 50;
                Left = 50;
            }



         
            //SeparateZanverse.IsChecked = Properties.Settings.Default.SeparateZanverse;
           // SeparateStatus.IsChecked = Properties.Settings.Default.SeparateStatus;
           // SeparateFinish.IsChecked = Properties.Settings.Default.SeparateFinish;
          
            //NoMyName.IsChecked = Properties.Settings.Default.NomyName;
            //Onlyme.IsChecked = Properties.Settings.Default.Onlyme;
            DPSFormat.IsChecked = Properties.Settings.Default.DPSformat;
            Nodecimal.IsChecked = Properties.Settings.Default.Nodecimal;

            ClickthroughMode.IsChecked = Properties.Settings.Default.ClickthroughEnabled;
         
            AlwaysOnTop.IsChecked = Properties.Settings.Default.AlwaysOnTop;
            AutoHideWindow.IsChecked = Properties.Settings.Default.AutoHideWindow;

            EncounterManualMode.IsChecked = Properties.Settings.Default.ManualMode;

            ShowDamageGraph.IsChecked = Properties.Settings.Default.ShowDamageGraph; ShowDamageGraph_Click(null, null);
            AnonymizeNames.IsChecked = Properties.Settings.Default.AnonymizeNames; AnonymizeNames_Click(null, null);
            HighlightYourDamage.IsChecked = Properties.Settings.Default.HighlightYourDamage; HighlightYourDamage_Click(null, null);
            Clock.IsChecked = Properties.Settings.Default.Clock; Clock_Click(null, null);
            
            HandleWindowOpacity(); HandleListOpacity();
            LoadListColumn();

            // Console.WriteLine($"Launch method: {Properties.Settings.Default.LaunchMethod}");

            if (Properties.Settings.Default.Maximized)
            {
                WindowState = WindowState.Maximized;
            }

            try
            {
                HotkeyManager.Current.AddOrReplace("End Encounter", Key.E, ModifierKeys.Control | ModifierKeys.Shift, EndEncounter_Key);
                HotkeyManager.Current.AddOrReplace("End Encounter (No log)", Key.R, ModifierKeys.Control | ModifierKeys.Shift, EndEncounterNoLog_Key);
                HotkeyManager.Current.AddOrReplace("Default Window Size", Key.D, ModifierKeys.Control | ModifierKeys.Shift, DefaultWindowSize_Key);
                HotkeyManager.Current.AddOrReplace("Always On Top", Key.A, ModifierKeys.Control | ModifierKeys.Shift, AlwaysOnTop_Key);
            }
            catch
            {
                MessageBox.Show("Hot keys are currently not working for this instance of PIGNUMBERS. \n\nPlease check that you are not running multiple instances of PIGNUMBERS", "PIGNUMBERS Setup", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            List<String> tmp_skills = new List<String>();

            // skills_en.csv
            Console.WriteLine("Updating skills.csv");
            try
            {
                WebClient client = new WebClient();
                Stream stream = File.Open("skills.csv", FileMode.Open);
                using (StreamReader sr = new StreamReader(stream))
                {
                  

                    string line;
                    while ((line = sr.ReadLine()) != null) {
                        tmp_skills.Add(line);

                    }
                }
                client.Dispose();
                stream.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"skills.csv update failed: {ex.ToString()}");
                
            }
           
        

            Console.WriteLine("Parsing skills.csv");
            foreach (string s in tmp_skills)
            {
                string[] split = s.Split(',');
                if (split.Length > 1)
                {
                    if (skillDict.ContainsKey(split[1])==false)
                        skillDict.Add(split[1], split[0]);
                }
            }

            //Initializing default log
            //and installing...
            encounterlog = new Log(Properties.Settings.Default.Path);
            UpdateForm(null, null);

            //Initializing damageTimer
            damageTimer.Tick += new EventHandler(UpdateForm);
            damageTimer.Interval = new TimeSpan(0, 0, 0, 0, Properties.Settings.Default.Updateinv);
            damageTimer.Start();

            //Initializing inactiveTimer
            System.Windows.Threading.DispatcherTimer inactiveTimer = new System.Windows.Threading.DispatcherTimer();
            inactiveTimer.Tick += new EventHandler(HideIfInactive);
            inactiveTimer.Interval = TimeSpan.FromMilliseconds(200);
            inactiveTimer.Start();

            //Initializing logCheckTimer
            System.Windows.Threading.DispatcherTimer logCheckTimer = new System.Windows.Threading.DispatcherTimer();
            logCheckTimer.Tick += new EventHandler(CheckForNewLog);
            logCheckTimer.Interval = new TimeSpan(0, 0, 1);
            logCheckTimer.Start();
        }

        private void HideIfInactive(object sender, EventArgs e)
        {
            if (!Properties.Settings.Default.AutoHideWindow)
                return;

            string title = WindowsServices.GetActiveWindowTitle();
            string[] relevant = { "PIGNUMBERS", "PIGNUMBERS Setup", "PIGNUMBERS Error", "Encounter Timeout", "Phantasy Star Online 2", "PHANTASY STAR ONLINE 2 NEW GENESIS" };

            if (!relevant.Contains(title))
            {
                Opacity = 0;
            }
            else
            {
                HandleWindowOpacity();
            }
        }

        private void CheckForNewLog(object sender, EventArgs e)
        {
            DirectoryInfo directory = encounterlog.logDirectory;
            if (!directory.Exists)
            {
                return;
            }
            if (directory.GetFiles().Count() == 0)
            {
                return;
            }

            FileInfo log = directory.GetFiles().Where(f => Regex.IsMatch(f.Name, @"\d+\.csv")).OrderByDescending(f => f.Name).First();

            if (log.Name != encounterlog.filename)
            {
                //Console.WriteLine($"Found a new log file ({log.Name}), switching...");
                encounterlog = new Log(Properties.Settings.Default.Path);
            }
        }

        private void LoadListColumn()
        {
            GridLength temp = new GridLength(0);
            if (!Properties.Settings.Default.ListName) { CombatantView.Columns.Remove(NameColumn); NameHC.Width = temp; }
            if (Properties.Settings.Default.Variable)
            {
                if (Properties.Settings.Default.ListPct) { PercentHC.Width = new GridLength(0.4, GridUnitType.Star); } else { CombatantView.Columns.Remove(PercentColumn); PercentHC.Width = temp; }
                if (Properties.Settings.Default.ListDmg) { DmgHC.Width = new GridLength(0.8, GridUnitType.Star); } else { CombatantView.Columns.Remove(DamageColumn); DmgHC.Width = temp; }
                if (Properties.Settings.Default.ListDmgd) { DmgDHC.Width = new GridLength(0.6, GridUnitType.Star); } else { CombatantView.Columns.Remove(DamagedColumn); DmgDHC.Width = temp; }
                if (Properties.Settings.Default.ListDPS) { DPSHC.Width = new GridLength(0.6, GridUnitType.Star); } else { CombatantView.Columns.Remove(DPSColumn); DPSHC.Width = temp; }
                if (Properties.Settings.Default.ListCri) { CriHC.Width = new GridLength(0.4, GridUnitType.Star); } else { CombatantView.Columns.Remove(CriColumn); CriHC.Width = temp; }
                if (Properties.Settings.Default.ListHit) { MdmgHC.Width = new GridLength(0.6, GridUnitType.Star); } else { CombatantView.Columns.Remove(HColumn); MdmgHC.Width = temp; }
            }
            else
            {
                if (Properties.Settings.Default.ListPct) { PercentHC.Width = new GridLength(39); } else { CombatantView.Columns.Remove(PercentColumn); PercentHC.Width = temp; }
                if (Properties.Settings.Default.ListDmg) { DmgHC.Width = new GridLength(78); } else { CombatantView.Columns.Remove(DamageColumn); DmgHC.Width = temp; }
                if (Properties.Settings.Default.ListDmgd) { DmgDHC.Width = new GridLength(52); } else { CombatantView.Columns.Remove(DamagedColumn); DmgDHC.Width = temp; }
                if (Properties.Settings.Default.ListDPS) { DPSHC.Width = new GridLength(44); } else { CombatantView.Columns.Remove(DPSColumn); DPSHC.Width = temp; }
                if (Properties.Settings.Default.ListCri) { CriHC.Width = new GridLength(44); } else { CombatantView.Columns.Remove(CriColumn); CriHC.Width = temp; }
                if (Properties.Settings.Default.ListHit) { MdmgHC.Width = new GridLength(62); } else { CombatantView.Columns.Remove(HColumn); MdmgHC.Width = temp; }
            }
            if (!Properties.Settings.Default.ListAtk) { CombatantView.Columns.Remove(MaxHitColumn); AtkHC.Width = temp; }
        }

        private void Panic(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try { Directory.CreateDirectory("ErrorLogs"); }
            catch { MessageBox.Show("PIGNUMBERS has failed to create the directory: <ErrorLogs>"); }
            string datetime = string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now);
            string filename = $"ErrorLogs/ErrorLogs - {datetime}.txt";
            string errorMessage1 = string.Format("{0}", e.Exception.Source);
            string errorMessage2 = string.Format("{0}", e.Exception.StackTrace);
            string errorMessage3 = string.Format("{0}", e.Exception.TargetSite);
            string errorMessage4 = string.Format("{0}", e.Exception.InnerException);
            string errorMessage5 = string.Format("{0}", e.Exception.Message);
            //=== UNHANDLED EXCEPTION ===
            //e.Exception.ToString()
            string elog = (errorMessage1 + "\n" + errorMessage2 + "\n" + errorMessage3 + "\n" + errorMessage4 + "\n" + errorMessage5);
            File.WriteAllText(filename, elog);
        }


        public void HandleWindowOpacity()
        {
            TheWindow.Opacity = Properties.Settings.Default.WindowOpacity;
            // ACHTUNG ACHTUNG ACHTUNG ACHTUNG ACHTUNG ACHTUNG ACHTUNG ACHTUNG
            WinOpacity_0.IsChecked = false;
            WinOpacity_25.IsChecked = false;
            Winopacity_50.IsChecked = false;
            WinOpacity_75.IsChecked = false;
            WinOpacity_100.IsChecked = false;

            if (Properties.Settings.Default.WindowOpacity == 0)
            {
                WinOpacity_0.IsChecked = true;
            }
            else if (Properties.Settings.Default.WindowOpacity == .25)
            {
                WinOpacity_25.IsChecked = true;
            }
            else if (Properties.Settings.Default.WindowOpacity == .50)
            {
                Winopacity_50.IsChecked = true;
            }
            else if (Properties.Settings.Default.WindowOpacity == .75)
            {
                WinOpacity_75.IsChecked = true;
            }
            else if (Properties.Settings.Default.WindowOpacity == 1)
            {
                WinOpacity_100.IsChecked = true;
            }
        }


        public void HandleListOpacity()
        {
            MainBack.Opacity = Properties.Settings.Default.ListOpacity;
            ListOpacity_0.IsChecked = false;
            ListOpacity_25.IsChecked = false;
            Listopacity_50.IsChecked = false;
            ListOpacity_75.IsChecked = false;
            ListOpacity_100.IsChecked = false;

            if (Properties.Settings.Default.ListOpacity == 0)
            {
                ListOpacity_0.IsChecked = true;
            }
            else if (Properties.Settings.Default.ListOpacity == .25)
            {
                ListOpacity_25.IsChecked = true;
            }
            else if (Properties.Settings.Default.ListOpacity == .50)
            {
                Listopacity_50.IsChecked = true;
            }
            else if (Properties.Settings.Default.ListOpacity == .75)
            {
                ListOpacity_75.IsChecked = true;
            }
            else if (Properties.Settings.Default.ListOpacity == 1)
            {
                ListOpacity_100.IsChecked = true;
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = AlwaysOnTop.IsChecked;
            if (Properties.Settings.Default.ClickthroughEnabled)
            {
                int extendedStyle = WindowsServices.GetWindowLong(hwndcontainer, WindowsServices.GWL_EXSTYLE);
                WindowsServices.SetWindowLong(hwndcontainer, WindowsServices.GWL_EXSTYLE, extendedStyle | WindowsServices.WS_EX_TRANSPARENT);
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            HandleWindowOpacity();
            Window window = (Window)sender;
            window.Topmost = AlwaysOnTop.IsChecked;
            if (Properties.Settings.Default.ClickthroughEnabled)
            {
                int extendedStyle = WindowsServices.GetWindowLong(hwndcontainer, WindowsServices.GWL_EXSTYLE);
                WindowsServices.SetWindowLong(hwndcontainer, WindowsServices.GWL_EXSTYLE, extendedStyle & ~WindowsServices.WS_EX_TRANSPARENT);
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
        }

        public void UpdateForm(object sender, EventArgs e)
        {
            if (encounterlog == null) { return; }
            if (Properties.Settings.Default.Clock) { Datetime.Content = DateTime.Now.ToString("HH:mm:ss.ff"); }

            if(encounterlog.UpdateLog(this, null)) {
                EndEncounter_Click(null, null);
                EncounterStatus.Content = encounterlog.LogStatus();
                return;
            }
            EncounterStatus.Content = encounterlog.LogStatus();

            // get a copy of the right combatants
            List<Combatant> targetList = (encounterlog.running ? encounterlog.combatants : lastCombatants);
            workingList.Clear();
            foreach (Combatant c in targetList)
            {
                Combatant temp = new Combatant(c.ID, c.Name, c.isTemporary);
                foreach (Attack a in c.Attacks)
                {
                    temp.Attacks.Add(new Attack(a.ID, a.Damage, a.JA, a.Cri));
                }
                temp.Damaged = c.Damaged;
                temp.PercentReadDPS = c.PercentReadDPS;
                temp.ActiveTime = c.ActiveTime;
                workingList.Add(temp);
            }

            // clear out the list
            CombatantData.Items.Clear();

            // for zanverse dummy and status bar because WHAT IS GOOD STRUCTURE
            int elapsed = 0;
            Combatant stealActiveTimeDummy = workingList.FirstOrDefault();
            if (stealActiveTimeDummy != null) { elapsed = stealActiveTimeDummy.ActiveTime; }



            // force resort here to neatly shuffle AIS parses back into place
            workingList.Sort((x, y) => y.ReadDamage.CompareTo(x.ReadDamage));

        

            // get group damage totals
            int totalDamage = workingList.Sum(x => x.Damage);
            int totalReadDamage = workingList.Where(c => c.IsAlly).Sum(x => x.Damage);

            // dps calcs!
            foreach (Combatant c in workingList)
            {
                c.PercentReadDPS = c.ReadDamage / (float)totalReadDamage * 100;
            }

            // damage graph stuff
            Combatant.maxShare = 0;
            foreach (Combatant c in workingList)
            {
                if ((c.IsAlly) && c.ReadDamage > Combatant.maxShare)
                    Combatant.maxShare = c.ReadDamage;

                bool filtered = true;
            
                if ((c.IsAlly || !FilterPlayers.IsChecked) && (c.Damage > 0))
                        filtered = false;
                

                if (!filtered && c.Damage > 0)
                {
                    CombatantData.Items.Add(c);
                }

            }

            // status pane updates
            EncounterIndicator.Fill = new SolidColorBrush(Color.FromArgb(192, 255, 128, 128));
            EncounterStatus.Content = encounterlog.LogStatus();

            if (encounterlog.valid && encounterlog.notEmpty)
            {
                EncounterIndicator.Fill = new SolidColorBrush(Color.FromArgb(192, 64, 192, 64));
                EncounterStatus.Content = $"Waiting - {lastStatus}";
                if (lastStatus == "")
                    EncounterStatus.Content = "Waiting for combat data...";

                CombatantData.Items.Refresh();
            }

            if (encounterlog.running)
            {
                EncounterIndicator.Fill = new SolidColorBrush(Color.FromArgb(192, 0, 192, 255));

                TimeSpan timespan = TimeSpan.FromSeconds(elapsed);
                string timer = timespan.ToString(@"h\:mm\:ss");
                EncounterStatus.Content = $"{timer}";

                float totalDPS = totalDamage / (float)elapsed;

                if (totalDPS > 0)
                    EncounterStatus.Content += $" - Total : {totalDamage.ToString("N0")}" + $" - {totalDPS.ToString("N0")} DPS";

                //if (!Properties.Settings.Default.SeparateZanverse)
                //    EncounterStatus.Content += $" - Zanverse : {totalZanverse.ToString("N0")}";

                lastStatus = EncounterStatus.Content.ToString();
            }

            // autoend //we do this earlier now and also its broken wen we do it earlier kinda basically a mess
           /* if (encounterlog.running)
            {
                if (Properties.Settings.Default.AutoEndEncounters)
                {
                    int unixTimestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    if ((unixTimestamp - Log.newTimestamp) >= Properties.Settings.Default.EncounterTimeout)
                    {
                        //Automatically ending an encounter
                        EndEncounter_Click(null, null);
                    }
                }
            }*/
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Closing...

            if (!Properties.Settings.Default.ResetInvoked)
            {
                if (WindowState == WindowState.Maximized)
                {
                    Properties.Settings.Default.Top = RestoreBounds.Top;
                    Properties.Settings.Default.Left = RestoreBounds.Left;
                    Properties.Settings.Default.Height = RestoreBounds.Height;
                    Properties.Settings.Default.Width = RestoreBounds.Width;
                    Properties.Settings.Default.Maximized = true;
                }
                else
                {
                    Properties.Settings.Default.Top = Top;
                    Properties.Settings.Default.Left = Left;
                    Properties.Settings.Default.Height = Height;
                    Properties.Settings.Default.Width = Width;
                    Properties.Settings.Default.Maximized = false;
                }
            }

        //    encounterlog.WriteLog();

            Properties.Settings.Default.Save();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            Application.Current.Shutdown();
        }

        public void EndEncounter_Key(object sender, HotkeyEventArgs e)
        {
            //Encounter hotkey pressed
            EndEncounter_Click(null, null);
            e.Handled = true;
        }

        public void EndEncounterNoLog_Key(object sender, HotkeyEventArgs e)
        {
            //Encounter hotkey (no log) pressed
            EndEncounterNoLog_Click(null, null);
            e.Handled = true;
        }

        public void DefaultWindowSize_Key(object sender, HotkeyEventArgs e)
        {
            DefaultWindowSize_Click(null, null);
            e.Handled = true;
        }

        private void AlwaysOnTop_Key(object sender, HotkeyEventArgs e)
        {
            // Console.WriteLine("Always-on-top hotkey pressed");
            AlwaysOnTop.IsChecked = !AlwaysOnTop.IsChecked;
            IntPtr wasActive = WindowsServices.GetForegroundWindow();

            // hack for activating PIGNUMBERS window
            this.WindowState = WindowState.Minimized;
            this.Show();
            this.WindowState = WindowState.Normal;

            this.Topmost = AlwaysOnTop.IsChecked;
            AlwaysOnTop_Click(null, null);
            WindowsServices.SetForegroundWindow(wasActive);
            e.Handled = true;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void ListViewItem_MouseRightClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem data = sender as ListViewItem;
            Combatant data2 = (Combatant)data.DataContext;
            Detalis f = new Detalis(data2) { Owner = this };
            f.Show();
        }
    }
}
