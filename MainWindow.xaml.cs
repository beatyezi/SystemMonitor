using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Threading;
using System.Windows.Threading;
using System.Timers;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SysMonitor;
using System.Security.Permissions;

namespace SystemMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //调试信息记录标识符
        private bool DEBUGINFO_OPEN = false;
        //最小化至系统托盘
        public bool gbIsMinimizeToNotifyCenter = false;
        //记得要在工程项目上点击右键->Add Reference中添加NotifyIcon的dll
        private System.Windows.Forms.NotifyIcon gNotifyIcon;
        private System.Windows.Forms.ContextMenu gContextMenu;
        private System.Windows.Forms.MenuItem gMenuItemExit;
        private System.Windows.Forms.MenuItem gMenuItemLock;
        private System.Windows.Forms.MenuItem gMenuItemRecordKey;
        private System.Windows.Forms.MenuItem gMenuItemModifyPassword;
        private System.Windows.Forms.MenuItem gMenuItemOption;
        //锁定系统标识符
        private bool isLockSystem = false;
        //记录键盘标识符
        private bool isRecordKey = false;
        //窗口是否位于前端
        //状态直接用IsActive属性判断，屏蔽此标识符
        //private bool isForeground = true;
        //获取当前时间
        public delegate void CurrentTimeDispatcherDelegate();
        private static System.Timers.Timer timerCurrentTime;
        private static DateTime currentTime;
        private const string datePattern = @"yyyy年M月d日";
        private const string timePattern = @"HH:mm:ss";
        private const string timestampPattern = @"HH:mm:ss.fff";
        private const string datePatternForSaveRecordedKey = @"yyyy年M月d日";
        private const string timePatternForSaveRecordedKey = @"HH.mm.ss";
        //显示界面置中
        private double appTop = 0.0, appLeft = 0.0;
        //监控保护程序
        public delegate void MonitorProtectionDispatcherDelegate();
        private static System.Timers.Timer timerMonitorProtection;
        //开机锁定机器
        public delegate void LockAfterStartDispatcherDelegate();
        private static System.Timers.Timer timerLockAfterStart;
        //监控保护程序的地址和备份地址
        private string protectionDirectory = String.Empty;
        private string protectionBackupDirectory = "C:\\Windows\\suchost.exe";
        //日志文件地址
        private string logFilePath = String.Empty;
        private string logTimestamp = String.Empty;
       
        public MainWindow()
        {
            InitializeComponent();
            InitDlgCtrls();
            InitNotifyCenter();
        }

        //Remember using System.Security.Permissions;
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]

        //初始化系统托盘中的通知
        private void InitNotifyCenter()
        {
            this.StateChanged += new EventHandler(MainWindow_StateChanged);            
            
            //初始化气泡提示
            this.gNotifyIcon = new System.Windows.Forms.NotifyIcon();
            this.gNotifyIcon.BalloonTipText = "监控程序正在运行...";
            this.gNotifyIcon.Text = "SysMonitor V1.0.0.0 Biting";
            //string iconPath = System.Windows.Forms.Application.StartupPath + "\\UartDemo_16x16.ico";
            string strCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;//Directory.GetCurrentDirectory();
            strCurrentDirectory += AppDomain.CurrentDomain.SetupInformation.ApplicationName;//("\\" + System.IO.Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().GetName().Name) + ".exe");
            System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(strCurrentDirectory);
            System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(icon.Handle, new Int32Rect(0, 0, icon.Width, icon.Height), BitmapSizeOptions.FromEmptyOptions());
            this.gNotifyIcon.Icon = icon;//new System.Drawing.Icon(iconPath);
            this.gNotifyIcon.Visible = true;
            this.gNotifyIcon.ShowBalloonTip(2000);
            this.gNotifyIcon.DoubleClick += new EventHandler(gNotifyIcon_DoubleClick);

            //初始化右键菜单
            this.gMenuItemExit = new System.Windows.Forms.MenuItem();
            this.gMenuItemLock = new System.Windows.Forms.MenuItem();
            this.gMenuItemRecordKey = new System.Windows.Forms.MenuItem();
            this.gMenuItemModifyPassword = new System.Windows.Forms.MenuItem();
            this.gMenuItemOption = new System.Windows.Forms.MenuItem();
            System.Windows.Forms.MenuItem[] children = new System.Windows.Forms.MenuItem[] { this.gMenuItemExit, this.gMenuItemLock, this.gMenuItemRecordKey, this.gMenuItemModifyPassword, this.gMenuItemOption };
            this.gContextMenu = new System.Windows.Forms.ContextMenu(children);
            this.gMenuItemExit.Index = 4;
            this.gMenuItemExit.Text = "退出";
            this.gMenuItemExit.Click += new EventHandler(gMenuItemExit_Click);
            this.gMenuItemLock.Index = 3;
            this.gMenuItemLock.Text = "锁定";
            this.gMenuItemLock.Click += new EventHandler(gMenuItemLock_Click);
            this.gMenuItemRecordKey.Index = 2;
            this.gMenuItemRecordKey.Text = "记录键盘";
            this.gMenuItemRecordKey.Click += new EventHandler(gMenuItemRecordKey_Click);
            this.gMenuItemModifyPassword.Index = 1;
            this.gMenuItemModifyPassword.Text = "更改密码";
            this.gMenuItemModifyPassword.Click += new EventHandler(gMenuItemModifyPassword_Click);
            this.gMenuItemOption.Index = 0;
            this.gMenuItemOption.Text = "选项(当前模式为【非管理员】)";
            this.gMenuItemOption.Click += new EventHandler(gMenuItemOption_Click);            
            this.gNotifyIcon.ContextMenu = this.gContextMenu;
            PRINT("InitNotifyCenter: OK!");           
        }               

        private void InitDlgCtrls()
        {
            InitLogModule();
            PRINT("InitDlgCtrls:InitLogModule OK!" + globalException);
            if (!JudgeOneInstance())
            {
                //进程表中已经有实例项，故结束本实例 
                PRINT("InitDlgCtrls: JudgeOneInstance false, process is running, will exit.");
                Environment.Exit(0);
            }
            if (InitProtectionModule())
            {
                PRINT("InitDlgCtrls:InitProtectionModule OK!");
            }                
            else
            {
                PRINT("InitDlgCtrls:InitProtectionModule FAIL!" + globalException);
            }
            InitMaskCode();
            PRINT("InitDlgCtrls:InitMaskCode OK!");
            InitTimers();
            PRINT("InitDlgCtrls:InitTimers OK!");
            ReadRegistry();
            InitFileSystemWatcher();
            PRINT("InitDlgCtrls:InitFileSystemWatcher OK!");
            //获取窗口句柄 
            var handle = new WindowInteropHelper(this).Handle;
            //获取当前显示器屏幕
            System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromHandle(handle);
            appTop = (screen.Bounds.Height / 2 - this.Height / 2);
            appLeft = (screen.Bounds.Width / 2 - this.Width / 2);
            this.Deactivated += new EventHandler(MainWindow_Deactivated);
            this.Activated += new EventHandler(MainWindow_Activated);
            editPassword.Focus();
            this.WindowState = WindowState.Minimized;
            this.Visibility = Visibility.Hidden;
            PRINT("InitDlgCtrls: OK!");
        }        

        private void InitLogModule()
        {
            try
            {
                logFilePath = AppDomain.CurrentDomain.BaseDirectory + "log\\" + DateTime.Now.ToLocalTime().ToString(datePattern) + ".txt";
                globalException = logFilePath;
            }
            catch (System.Exception ex)
            {
                globalException = ex.ToString();
                logFilePath = "C:\\SystemMonitorErrorLog.txt";
            }                        
        }

        private void PRINT(string log)
        {
            if (DEBUGINFO_OPEN)
            {
                SaveLogToFile(log);
            }
        }

        private bool InitProtectionModule()
        {
            //protectionDirectory = Directory.GetCurrentDirectory() + "\\suchost.exe";//程序初始化时用这句来获取运行目录会不正确,modified by Biting at 20160201
            protectionDirectory = AppDomain.CurrentDomain.BaseDirectory + "suchost.exe";
            //将当前目录下的suchost拷贝至C:/Windows下作为备份
            if (BackupProtectionApp(protectionBackupDirectory))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        private string globalException = String.Empty;
        private bool BackupProtectionApp(string backupFullPath)
        {
            try
            {
                File.Copy(protectionDirectory, backupFullPath, true);
                return true;
            }
            catch (System.Exception ex)
            {
                globalException = ex.ToString();
                return false;
            }                
        }

        private bool RecoverProtectionApp(string appSourceFullPath)
        {
            if (File.Exists(appSourceFullPath))
            {
                try
                {
                    File.Copy(appSourceFullPath, protectionDirectory);
                    return true;
                }
                catch (System.Exception ex)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool JudgeOneInstance()
        {
            Process[] process = new Process[256];
            process = Process.GetProcessesByName("SysMonitor");
            if (process.Length == 0 || process.Length == 1)
            {
                return true;
            }
            else
            {
                return false;                
            }
        }

        private bool SaveLogToFile(string logForSave)
        {
            try
            {
                File.AppendAllText(logFilePath, logTimestamp + " " + logForSave + "\r\n");
                return true;
            }
            catch (System.Exception ex)
            {
                return false;
            }
        }

        private void InitTimers()
        {
            timerCurrentTime = new System.Timers.Timer(1000);
            timerCurrentTime.Elapsed += new ElapsedEventHandler(timerCurrentTime_Elapsed);
            timerCurrentTime.Enabled = true;
            timerMonitorProtection = new System.Timers.Timer(100);
            timerMonitorProtection.Elapsed += new ElapsedEventHandler(timerMonitorProtection_Elapsed);
            timerMonitorProtection.Enabled = true;
            //运行后锁定策略：30s内无特定事件触发后，则锁定计算机；反之，不锁定
            timerLockAfterStart = new System.Timers.Timer(30000);
            timerLockAfterStart.Elapsed += new ElapsedEventHandler(timerLockAfterStart_Elapsed);
            timerLockAfterStart.Enabled = true;
        }

        void timerLockAfterStart_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new LockAfterStartDispatcherDelegate(LockAfterStart));
        }

        void timerMonitorProtection_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new MonitorProtectionDispatcherDelegate(MonitorProtection));            
        }

        void timerCurrentTime_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new CurrentTimeDispatcherDelegate(ShowCurrentTime));            
        }

        private System.Drawing.Point mousePositionForLockAfterStart;
        private void ShowCurrentTime()
        {
            string strTemp = string.Empty;
            currentTime = DateTime.Now;
            strTemp = currentTime.ToLocalTime().ToString(datePattern);
            strTemp += currentTime.ToLocalTime().ToString(timePattern);
            logTimestamp = currentTime.ToLocalTime().ToString(timestampPattern);
            tbCurrentTime.Text = strTemp;
            if (!userIsAdmintstrator && !lockAfterStartComplete)
            {
                mousePositionForLockAfterStart = System.Windows.Forms.Control.MousePosition;
                if (mousePositionForLockAfterStart.X <= 5 && mousePositionForLockAfterStart.Y <= 5)
                {
                    lockAfterStartCounter++;
                    if (lockAfterStartCounter > 5)
                    {
                        //lockAfterStartComplete = true;
                        userIsAdmintstrator = true;
                        this.gMenuItemOption.Text = "选项(当前模式为【管理员】)";
                        MessageBox.Show("程序已配置为管理员模式！", "提示", MessageBoxButton.OK);
                    }
                }
            }
        }
        
        private void InitFileSystemWatcher()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            string[] availableDriveName = new string[32];
            int counter = 0;
            foreach (DriveInfo d in allDrives)
            {
                if (d.IsReady && d.DriveType.ToString().ToLower() == "fixed")
                {
                    availableDriveName[counter++] = d.Name;
                }
            }                        
            for(int iLoop = 0; iLoop < counter; iLoop ++)
            {
                FileSystemWatcher watcher = new FileSystemWatcher();
                watcher.Path = availableDriveName[iLoop];
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;                
                watcher.Filter = "*.*";
                watcher.IncludeSubdirectories = true;
                watcher.Changed += new FileSystemEventHandler(OnChanged);
                //watcher.Created += new FileSystemEventHandler(OnChanged);
                watcher.Deleted += new FileSystemEventHandler(OnChanged);
                watcher.Renamed += new RenamedEventHandler(OnRenamed);
                watcher.EnableRaisingEvents = true;

                //监控最近使用的程序
                FileSystemWatcher watcherRecentApps = new FileSystemWatcher();
                watcherRecentApps.Path = availableDriveName[iLoop];
                watcherRecentApps.NotifyFilter = NotifyFilters.LastAccess;
                watcherRecentApps.Filter = "*.exe";
                watcherRecentApps.IncludeSubdirectories = true;
                watcherRecentApps.Changed += new FileSystemEventHandler(watcherRecentApps_Changed);
                watcherRecentApps.EnableRaisingEvents = true;
            }
        }

        void watcherRecentApps_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                File.AppendAllText(logFilePath + "2", logTimestamp + " " + e.FullPath + "\r\n");                
            }
            catch (System.Exception ex)
            {
                return;
            }
        }
        
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            if (logFilePath != e.FullPath)
            {
                string log = e.FullPath + " " + ChangedTypeToChinese(e.ChangeType);
                SaveLogToFile(log);
            }            
        }

        private string ChangedTypeToChinese(WatcherChangeTypes e)
        {
            string afterTranslated = String.Empty;
            switch (e)
            {
                case WatcherChangeTypes.Changed:
                    afterTranslated = "被修改";
                    break;
                case WatcherChangeTypes.Created:
                    afterTranslated = "被创建";
                    break;
                case WatcherChangeTypes.Deleted:
                    afterTranslated = "被删除";
                    break;
                case WatcherChangeTypes.Renamed:
                    afterTranslated = "重命名为";
                    break;
                case WatcherChangeTypes.All:
                    afterTranslated = "更改/创建/删除/重命名为";
                    break;
                default:
                    afterTranslated = "未知操作";
                    break;
            }
            return afterTranslated;
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            string log = String.Format("{0} 重命名为 {1}", e.OldFullPath, System.IO.Path.GetFileName(e.FullPath));
            SaveLogToFile(log);
        }


        private Process[] machineProcesses = new Process[256];        
        private void MonitorProtection()
        {
            ReadRegistry();
            if (Convert.ToInt32(registerInfo[3]) != superCode)
            {
                machineProcesses = Process.GetProcessesByName("suchost");
                if (machineProcesses.Length == 0)
                {
                    PRINT("MonitorProtection:No suchost in processes");
                    if (File.Exists(protectionDirectory))
                    {
                        Process process = new Process();
                        process.StartInfo.FileName = protectionDirectory;
                        process.Start();
                        PRINT("MonitorProtection:suchost exist, will run process");
                    }
                    else
                    {
                        PRINT("MonitorProtection:suchost no exist, will copy from c");
                        //将C:/Windows下的suchost拷贝至当前目录
                        if (RecoverProtectionApp(protectionBackupDirectory))
                        {
                            PRINT("MonitorProtection:RecoverProtectionApp OK!");
                        }
                        else
                        {
                            PRINT("MonitorProtection:RecoverProtectionApp FAIL!");
                        }
                    }
                }
            }
        }

        private long lockAfterStartCounter = 0;
        private bool lockAfterStartComplete = false;
        private static bool userIsAdmintstrator = false;
        public static bool UserIsAdministrator
        {
            get { return userIsAdmintstrator; }
        }

        private void LockAfterStart()
        {
            timerLockAfterStart.Enabled = false;
            //lockAfterStartComplete = true;
            if (lockAfterStartCounter <= 5)
            {
                if (IsModifyPassword)
                {
                    return;
                }
                if (!isLockSystem)
                {
                    isLockSystem = true;
                    editPassword.Password = String.Empty;
                    tbInfo.Text = "机器被锁定！\r\n\r\n" + "如需解锁，请联系 " + registerInfo[0] + "\r\n\r\n电话：" + registerInfo[1];
                    this.Show();
                    editPassword.Focus();
                    this.Top = appTop;
                    this.Left = appLeft;
                    this.WindowState = WindowState.Normal;
                    this.Visibility = Visibility.Visible;
                    this.Topmost = true;
                    SetHook();
                }
            }
            //lockAfterStartCounter = 0;
        }

        //明码和暗码相互转化
        private static char[] maskCode;
        private static string maskCodeIndex = "qsnleywrczmfvupkagojxdhbit";
        private void InitMaskCode()
        {
            maskCode = new char[26];
            maskCode = maskCodeIndex.ToCharArray();            
            string getMaskCode = PasswordToMaskcode(originRegisterInfo[2]);
            originRegisterInfo[2] = getMaskCode;            
        }

        public static string PasswordToMaskcode(string password)
        {
            char[] pwd = password.ToCharArray();
            char[] mask = new char[26];
            int iLoop = 0;
            string strReturn = String.Empty;
            byte tempByte = 0x00;
            foreach (char c in pwd)
            {
                tempByte = Convert.ToByte(c);
                if (tempByte >= 0x61 && tempByte <= 0x7A)
                {
                    mask[iLoop] = maskCode[tempByte - 0x61];
                    strReturn += mask[iLoop].ToString();
                }
                else
                {
                    if (tempByte >= 0x41 && tempByte <= 0x5A)
                    {
                        mask[iLoop] = maskCode[tempByte - 0x41];
                        strReturn += mask[iLoop].ToString().ToUpper();
                    }
                    else
                    {
                        if (tempByte >= 0x30 && tempByte <= 0x39)
                        {
                            mask[iLoop] = c;
                            strReturn += mask[iLoop].ToString();
                        }
                    }
                }
                iLoop++;
            }
            return strReturn;
        }

        public static string MaskcodeToPassword(string maskcode)
        {
            char[] mask = maskcode.ToCharArray();
            char[] pwd = new char[26];
            int iLoop = 0;
            string strReturn = String.Empty;
            byte tempByte = 0x00;
            foreach (char c in mask)
            {
                tempByte = Convert.ToByte(c);
                if (tempByte >= 0x61 && tempByte <= 0x7A)
                {
                    pwd[iLoop] = Convert.ToChar(maskCodeIndex.IndexOf(c) + 0x61);                    
                }
                else
                {
                    if (tempByte >= 0x41 && tempByte <= 0x5A)
                    {
                        pwd[iLoop] = Convert.ToChar(maskCodeIndex.ToUpper().IndexOf(c) + 0x41);                        
                    }
                    else
                    {
                        if (tempByte >= 0x30 && tempByte <= 0x39)
                        {
                            pwd[iLoop] = c;                            
                        }
                    }
                }
                strReturn += pwd[iLoop].ToString();
                iLoop++;
            }
            return strReturn;
        }

        void MainWindow_Activated(object sender, EventArgs e)
        {            
            //isForeground = true;
        }

        void MainWindow_Deactivated(object sender, EventArgs e)
        {
            //isForeground = false;
        }

        private Option optionDlg;
        private bool isSetOption = false;
        void gMenuItemOption_Click(object sender, EventArgs e)
        {
            if (!isSetOption)
            {
                isSetOption = true;
                optionDlg = new Option();
                optionDlg.ResponseFromOption += new Option.OptionHandler(optionDlg_ResponseFromOption);
                optionDlg.Show();
                optionDlg.Activate();
            }
            else
            {
                optionDlg.Show();
                optionDlg.Activate();
            }         
        }

        void optionDlg_ResponseFromOption(object sender, OptionEventArgs e)
        {
            switch (e.ClosingFlag)
            {
                case (int)OptionEventArgs.ClosingFlags.WindowIsClosing:
                    
                    break;
            }
        }

        void gMenuItemExit_Click(object sender, EventArgs e)
        {
            if (IsModifyPassword)
            {
                return;
            }
            if (!UserIsAdministrator)
            {
                MessageBox.Show("对不起，您不具备程序管理权限，无法退出程序", "提示", MessageBoxButton.OK);
                return;
            }
            this.gNotifyIcon.Visible = false;
            if (isLockSystem)
            {
                UnHook();
            }
            Environment.Exit(0);
        }

        void gMenuItemLock_Click(object sender, EventArgs e)
        {
            if (IsModifyPassword)
            {
                return;
            }
            if (!isLockSystem)
            {
                isLockSystem = true;
                editPassword.Password = String.Empty;
                tbInfo.Text = "机器被锁定！\r\n\r\n" + "如需解锁，请联系 " + registerInfo[0] + "\r\n\r\n电话：" + registerInfo[1];
                this.Show();
                editPassword.Focus();
                this.Top = appTop;
                this.Left = appLeft;
                this.WindowState = WindowState.Normal;
                this.Visibility = Visibility.Visible;
                this.Topmost = true;
                SetHook();
            }
        }

        private string fileTotalPath = String.Empty;
        private SaveFileDialog saveRecordedKeyDlg; 
        void gMenuItemRecordKey_Click(object sender, EventArgs e)
        {
            if (IsModifyPassword)
            {
                return;
            }
            if (!isRecordKey)
            {
                if (!isLockSystem)
                {
                    DateTime time;
                    string strCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;//Directory.GetCurrentDirectory();
                    time = DateTime.Now;
                    fileTotalPath = time.ToLocalTime().ToString(datePatternForSaveRecordedKey);
                    fileTotalPath += "_";
                    fileTotalPath += time.ToLocalTime().ToString(timePatternForSaveRecordedKey);
                    fileTotalPath += ".txt";                    
                    fileTotalPath = strCurrentDirectory + fileTotalPath;
                    saveRecordedKeyDlg = new SaveFileDialog();
                    saveRecordedKeyDlg.Filter = "txt files (*.txt) |*.txt";
                    saveRecordedKeyDlg.FileName = fileTotalPath;
                    saveRecordedKeyDlg.Title = "选择保存位置及文件名";
                    if ((bool)saveRecordedKeyDlg.ShowDialog())
                    {
                        SetHook();
                        isRecordKey = true;
                        fileTotalPath = saveRecordedKeyDlg.FileName;
                        this.gMenuItemRecordKey.Text = "停止记录";
                    }                    
                }
            }
            else
            {
                UnHook();
                isRecordKey = false;
                this.gMenuItemRecordKey.Text = "记录键盘";
            }
        }

        //修改密码界面
        private ModifyPassword modifyPasswordDlg;
        private static bool isModifyPassword = false;
        public static bool IsModifyPassword
        {
            get { return isModifyPassword; }
            set { isModifyPassword = value; }
        }

        void gMenuItemModifyPassword_Click(object sender, EventArgs e)
        {
            if (!UserIsAdministrator)
            {
                MessageBox.Show("对不起，您不具备程序管理权限，无法修改密码", "提示", MessageBoxButton.OK);
                return;
            }
            IsModifyPassword = true;
            modifyPasswordDlg = new ModifyPassword();
            modifyPasswordDlg.ShowDialog();
            modifyPasswordDlg.Activate();
        } 

        void MainWindow_StateChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case WindowState.Normal:
                    this.WindowState = WindowState.Normal;
                    this.Visibility = Visibility.Visible;
                    break;
                case WindowState.Maximized:
                    this.WindowState = WindowState.Maximized;
                    this.Visibility = Visibility.Visible;
                    break;
                case WindowState.Minimized:
                    this.WindowState = WindowState.Minimized;
                    this.Visibility = Visibility.Hidden;
                    break;
            }
        }

        void gNotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            //不需要双击显示，故屏蔽此段代码
            /*if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
                this.Visibility = Visibility.Visible;
                this.Show();
                this.Activate();
            }*/
        }
        
        int hHookKeyboard, hHookMouse;
        Win32Api.HookProc KeyboardHookDelegate;
        Win32Api.HookProc MouseHookDelegate;
        /// <summary>
        /// 安装钩子
        /// </summary>
        public void SetHook()
        {
            KeyboardHookDelegate = new Win32Api.HookProc(KeyboardHookProc);
            MouseHookDelegate = new Win32Api.HookProc(MouseHookProc);
            ProcessModule cModule = Process.GetCurrentProcess().MainModule;
            var mh = Win32Api.GetModuleHandle(cModule.ModuleName);
            hHookKeyboard = Win32Api.SetWindowsHookEx(Win32Api.WH_KEYBOARD_LL, KeyboardHookDelegate, mh, 0);
            hHookMouse = Win32Api.SetWindowsHookEx(Win32Api.WH_MOUSE_LL, MouseHookDelegate, mh, 0);            
        }

        /// <summary>
        /// 卸载钩子
        /// </summary>
        public void UnHook()
        {
            Win32Api.UnhookWindowsHookEx(hHookKeyboard);
            Win32Api.UnhookWindowsHookEx(hHookMouse);
        }

        /// <summary>
        /// 获取键盘消息
        /// </summary>
        private int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {            
            if (nCode >= 0)
            {                
                Win32Api.KeyboardHookStruct KeyDataFromHook = (Win32Api.KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(Win32Api.KeyboardHookStruct));
                int keyData = KeyDataFromHook.vkCode;               
                string actualKey = KeyInterop.KeyFromVirtualKey(keyData).ToString();
                if (isLockSystem)
                {                        
                    if (this.IsActive)
                    {
                        //backspace           caps              left shift           right shift        num lock
                        if ((keyData == 8) || (keyData == 20) || (keyData == 160) || (keyData == 161) || (keyData == 144) || (keyData >= 65 && keyData <= 90) ||
                            (keyData >= 48 && keyData <= 57) || (keyData >= 96 && keyData <= 105))
                        {
                            return Win32Api.CallNextHookEx(hHookKeyboard, nCode, wParam, lParam);                    
                        }
                        else
                        {
                            return 1;
                        }
                    }
                    else
                    {
                        return 1;
                    }
                }
                if ((wParam == Win32Api.WM_KEYUP || wParam == Win32Api.WM_SYSKEYUP))
                {
                    //editPassword.Text += keyData.ToString();//+= (actualKey + " ");                                                                               
                    if (isRecordKey)
                    {
                        File.AppendAllText(fileTotalPath, actualKey + "\r\n");
                        return Win32Api.CallNextHookEx(hHookKeyboard, nCode, wParam, lParam);                    
                    }
                }
            }
            return Win32Api.CallNextHookEx(hHookKeyboard, nCode, wParam, lParam);        
        }

        /// <summary>
        /// 获取鼠标消息
        /// </summary>
        private System.Drawing.Point mousePosition;
        private int MouseHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {            
            if ((wParam == Win32Api.WM_LBUTTONUP || wParam == Win32Api.WM_RBUTTONUP || wParam == Win32Api.WM_LBUTTONDBLCLK || wParam == Win32Api.WM_RBUTTONDBLCLK
                || wParam == Win32Api.WM_LBUTTONDOWN || wParam == Win32Api.WM_RBUTTONDOWN))
            {
                if (isLockSystem)
                {
                    mousePosition = System.Windows.Forms.Control.MousePosition;                    
                    if((mousePosition.X > appLeft) && (mousePosition.X < (appLeft + this.Width)) 
                        && (mousePosition.Y > appTop) && (mousePosition.Y < (appTop + this.Height)))
                    {
                        return Win32Api.CallNextHookEx(hHookMouse, nCode, wParam, lParam);
                    }
                    else
                    {
                        return 1;
                    }
                }
                if (isRecordKey)
                {
                    return Win32Api.CallNextHookEx(hHookMouse, nCode, wParam, lParam);
                }
            }
            return Win32Api.CallNextHookEx(hHookMouse, nCode, wParam, lParam);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //屏蔽用户的关闭命令
            e.Cancel = true;
        }

        private void btnUnlock_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string password = editPassword.Password;
            if (password.Equals(MaskcodeToPassword(registerInfo[2])))
            {
                isLockSystem = false;
                this.WindowState = WindowState.Minimized;
                this.Visibility = Visibility.Hidden;
                UnHook();
            }
            else
            {
                editPassword.Password = String.Empty;
            }
        }

        private string[] originRegisterInfo = { "导航所 毕挺", "622906/18720298906", "beating", "33847"};
        private int superCode = 910122;
        private static string[] registerInfo = { "", "", "", ""};
        public static string RegisterInfoPassword
        {
            get { return registerInfo[2]; }
            set { registerInfo[2] = value; }
        }        

        private void ReadRegistry()
        {            
            RegistryKey rkLocalMachine = Registry.LocalMachine;
            try
            {
                //Name
                RegistryKey rkTotal = rkLocalMachine.CreateSubKey("SOFTWARE\\SysMonitor");                
                string userName = (string)rkTotal.GetValue("userName");
                if (userName == String.Empty || userName == null)
                {
                    rkTotal.SetValue("userName", originRegisterInfo[0]);
                    userName = originRegisterInfo[0];
                }
                registerInfo[0] = userName;

                //Tel                
                string userTel = (string)rkTotal.GetValue("userTel");
                if (userTel == String.Empty || userTel == null)
                {
                    rkTotal.SetValue("userTel", originRegisterInfo[1]);
                    userTel = originRegisterInfo[1];
                }
                registerInfo[1] = userTel;

                //Pwd        
                string userPwd = (string)rkTotal.GetValue("userPwd");
                if (userPwd == String.Empty || userPwd == null)
                {                    
                    rkTotal.SetValue("userPwd", originRegisterInfo[2]);
                    userPwd = originRegisterInfo[2];
                }
                registerInfo[2] = userPwd;

                //super code
                string youChangeYouDie = (string)rkTotal.GetValue("YouChangeYouDie");
                if (youChangeYouDie == String.Empty || youChangeYouDie == null)
                {
                    rkTotal.SetValue("YouChangeYouDie", originRegisterInfo[3]);
                    youChangeYouDie = originRegisterInfo[3];
                }
                registerInfo[3] = youChangeYouDie;               
            }
            catch (System.Exception ex)
            {                
                MessageBox.Show("无法读取或配置注册表\r\n" + ex.ToString(), "错误");
                return;
            }
        }

        //保存相应的信息至注册表
        public static bool SaveInformationToRegistry(string strName, string strValue)
        {
            RegistryKey rkLocalMachine = Registry.LocalMachine;
            RegistryKey rkTotal = rkLocalMachine.CreateSubKey("SOFTWARE\\SysMonitor");
            try
            {
                rkTotal.SetValue(strName, strValue);
                return true;
            }
            catch (System.Exception ex)
            {                
                MessageBox.Show("无法配置注册表\r\n" + ex.ToString(), "错误");
                return false;
            }
        }

        public static bool ReadPowerbootInformation()
        {            
            string strExeName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            RegistryKey rkLocalMachine = Registry.LocalMachine;
            try
            {
                RegistryKey rkRun = rkLocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                string appPowerbootInfo = rkRun.GetValue("SysMonitor") as string;
                if (String.IsNullOrEmpty(appPowerbootInfo))
                {
                    return false;
                }
                else
                {
                    if (appPowerbootInfo.Equals(strExeName))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (System.Exception ex)
            {
            	return false;
            }
        }

        public static bool WritePowerbootInformation(bool isAppPowerboot)
        {
            string strExeName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            RegistryKey rkLocalMachine = Registry.LocalMachine; 
            try
            {
                RegistryKey rkRun = rkLocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (isAppPowerboot)
                {
                    rkRun.SetValue("SysMonitor", strExeName);
                    return true;
                }
                else
                {
                    rkRun.SetValue("SysMonitor", "");
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                return false;
            }
        }
    }
}
