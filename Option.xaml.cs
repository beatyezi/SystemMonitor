using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SystemMonitor;
using Telerik.Windows.Controls;
using System.IO;

namespace SysMonitor
{
	/// <summary>
	/// Interaction logic for Option.xaml
	/// </summary>
	public partial class Option : Window
	{
		public Option()
		{
			this.InitializeComponent();
            InitDlgCtrls();
			// Insert code required on object creation below this point.
            //其中的MenuItem直接定义Click有问题，要改,unfinished at 20160229, Biting
		}

        private void InitDlgCtrls()
        {
            InitRecentAppsList();
            chkIsPowerboot.IsChecked = MainWindow.ReadPowerbootInformation() ? true : false;
            if (!MainWindow.UserIsAdministrator)
            {
                chkIsPowerboot.IsEnabled = false;
            }
        }

        private const string userDocumentSettingPath = "C:\\Users";//C:\\Documents and Settings";
        //由于会加上空行至ListBox，防止第一行出现空白影响美观，故设定此标识符
        private bool preventFirstBlankRowFlag = false;
        private void InitRecentAppsList()
        {
            //最好是按照最后修改时间进行排序，可使用List类、Sort函数, unfinished at 20160226,Biting
            preventFirstBlankRowFlag = false;
            string[] docSettingSubFolders = Directory.GetDirectories(userDocumentSettingPath);
            string[] tempSubFolder;
            string recentFolderPath = String.Empty;
            foreach(string subFolderUnderDocSetting in docSettingSubFolders)
            {
                tempSubFolder = Directory.GetDirectories(subFolderUnderDocSetting);
                recentFolderPath = System.IO.Path.Combine(subFolderUnderDocSetting, "recent").ToLower();
                foreach(string subFolderUnderFolder in tempSubFolder)
                {
                    if(subFolderUnderFolder.ToLower().Equals(recentFolderPath))
                    {
                        string[] filesUnderRecentFolder = Directory.GetFiles(recentFolderPath);
                        if (filesUnderRecentFolder.Length != 0)
                        {
                            if (preventFirstBlankRowFlag)
                            {
                                AddStringItemToListBox(" ");
                            }
                            preventFirstBlankRowFlag = true;
                            AddStringItemToListBox("以下为账户" + subFolderUnderFolder + "下最近使用过的文件:");
                        }
                        foreach (string name in filesUnderRecentFolder)
                        {
                            AddStringItemToListBox(name);      
                        }
                    }
                }                
            }
        }

        private void AddStringItemToListBox(string content)
        {
            //RadListBoxItem item = new RadListBoxItem();
            ListBoxItem item = new ListBoxItem();
            item.FocusVisualStyle = null;
            item.Content = content;
            lbRecentApps.Items.Add(item);
        }

		private void chkIsPowerboot_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            if (!MainWindow.UserIsAdministrator)
            {
                chkIsPowerboot.IsChecked = !(bool)chkIsPowerboot.IsChecked;
                MessageBox.Show("对不起，您不具备程序管理权限，无法进行更改", "提示", MessageBoxButton.OK);
                return;
            }
            MainWindow.WritePowerbootInformation((bool)chkIsPowerboot.IsChecked);
		}

        public delegate void OptionHandler(object sender, OptionEventArgs e);
        public event OptionHandler ResponseFromOption;

        private void PrepareResponseFromOption(int flag)
        {
            OptionEventArgs args = new OptionEventArgs(flag);
            ResponseFromOption(this, args);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            PrepareResponseFromOption((int)OptionEventArgs.ClosingFlags.WindowIsClosing);
            this.Hide();
            e.Cancel = true;
        }

        private void Window_Activated(object sender, System.EventArgs e)
        {            
            chkIsPowerboot.IsEnabled = MainWindow.UserIsAdministrator ? true : false;
        }

        private void lbRecentApps_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
        	// TODO: Add event handler implementation here.
        }

        private bool IsValidPath(string source)
        {
            return File.Exists(source);
        }

        private void OpenFilePath(string filePath)
        {
            string fileName = String.Empty;
            fileName = System.IO.Path.GetFileName(filePath);
            if (fileName.IndexOf(".lnk") <= 0)
            {
                //Drop的是一个可执行文件，打开其所在目录
                int iPosition = filePath.IndexOf(fileName);
                filePath = filePath.Remove(iPosition);
                System.Diagnostics.Process processRun = new System.Diagnostics.Process();
                processRun.StartInfo.FileName = filePath;
                try
                {
                    processRun.Start();
                }
                catch (System.Exception ex)
                {
                    return;
                }
            }
            else
            {
                //Drop的是一个快捷方式，打开快捷方式指向的文件夹
                try
                {
                    IWshRuntimeLibrary.IWshShortcut shortcut = null;
                    IWshRuntimeLibrary.IWshShell_Class shell = new IWshRuntimeLibrary.IWshShell_Class();
                    shortcut = shell.CreateShortcut(filePath) as IWshRuntimeLibrary.IWshShortcut;
                    filePath = shortcut.TargetPath;
                    fileName = System.IO.Path.GetFileName(filePath);
                    int iPosition = filePath.IndexOf(fileName);
                    filePath = filePath.Remove(iPosition);
                    System.Diagnostics.Process processRun = new System.Diagnostics.Process();
                    processRun.StartInfo.FileName = filePath;
                    processRun.Start();
                }
                catch (System.Exception ex)
                {
                    return;
                }
            }
        }

        private void menuItemRun_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem selectedItem = lbRecentApps.SelectedItem as ListBoxItem;
            string filePath = (string)selectedItem.Content;
            if (!IsValidPath(filePath))
            {
                MessageBox.Show("文件不存在，可能已被删除", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            System.Diagnostics.Process processRun = new System.Diagnostics.Process();
            processRun.StartInfo.FileName = filePath;
            try
            {
                processRun.Start();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Unable to run process");
                return;
            }
        }

        private void menuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem selectedItem = lbRecentApps.SelectedItem as ListBoxItem;
            string filePath = (string)selectedItem.Content;
            if (!IsValidPath(filePath))
            {
                MessageBox.Show("文件不存在，可能已被删除", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            OpenFilePath(filePath);
        }

        private void menuItemRefresh_Click(object sender, RoutedEventArgs e)
        {
            lbRecentApps.Items.Clear();
            InitRecentAppsList();
        } 
	}
}