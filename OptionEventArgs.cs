using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SysMonitor
{
    public class OptionEventArgs : EventArgs
    {
        //直接定义为public属性，之后在其他类中可以直接引用
        public enum ClosingFlags
        {
            WindowIsClosing            
        };

        private int iClosingFlag = 0;
        public int ClosingFlag
        {
            get { return iClosingFlag; }
            set { this.iClosingFlag = value; }
        }

        public OptionEventArgs(int flag)
        {
            this.ClosingFlag = flag;
        } 
    }
}
