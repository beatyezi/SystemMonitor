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

namespace SysMonitor
{
	/// <summary>
	/// Interaction logic for ModifyPassword.xaml
	/// </summary>
	public partial class ModifyPassword : Window
	{
		public ModifyPassword()
		{
			this.InitializeComponent();
            InitDlgCtrls();
			// Insert code required on object creation below this point.
		}

        private void InitDlgCtrls()
        {
            pwdOld.Password = String.Empty;
            pwdNew.Password = String.Empty;
            pwdNewConfirm.Password = String.Empty;
        }

		private void btnModifyPasswordConfirm_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            string oldPasswordInput = pwdOld.Password, newPasswordInput = pwdNew.Password, newPasswordConfirmInput = pwdNewConfirm.Password;
            if(String.IsNullOrEmpty(oldPasswordInput))
            {                
                MessageBox.Show("请输入旧密码！", "提示", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }
            string oldPassword = MainWindow.MaskcodeToPassword(MainWindow.RegisterInfoPassword);
            if (oldPasswordInput.Equals(oldPassword))
            {
                if (String.IsNullOrEmpty(newPasswordInput))
                {
                    MessageBox.Show("请输入新密码！", "提示", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }
                if (String.IsNullOrEmpty(newPasswordConfirmInput))
                {
                    MessageBox.Show("请再次输入新密码！", "提示", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }
                if (newPasswordInput.Equals(newPasswordConfirmInput))
                {
                    if (MainWindow.SaveInformationToRegistry("userPwd", MainWindow.PasswordToMaskcode(newPasswordInput)))
                    {
                        MainWindow.RegisterInfoPassword = MainWindow.PasswordToMaskcode(newPasswordInput);
                        MessageBox.Show("修改成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        MainWindow.IsModifyPassword = false;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("修改失败！", "提示", MessageBoxButton.OK, MessageBoxImage.Stop);
                        MainWindow.IsModifyPassword = false;
                        this.Close();
                    }
                }
                else
                {
                    MessageBox.Show("新密码输入不一致！", "提示", MessageBoxButton.OK, MessageBoxImage.Stop);
                    pwdNew.Password = String.Empty;
                    pwdNewConfirm.Password = String.Empty;
                    pwdNew.Focus();
                    return;
                }
            }
            else
            {
                MessageBox.Show("旧密码不正确，请重新输入", "提示", MessageBoxButton.OK, MessageBoxImage.Stop);
                pwdOld.Password = String.Empty;
                pwdOld.Focus();
                return;
            }
		}

		private void btnModifyPasswordCancle_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            MainWindow.IsModifyPassword = false;
            this.Close();
		}
	}
}