using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;

namespace GUI___Encrypt
{
    class Class1
    {
        private TextBlock tb;
        private string fullpath;
        public  Class1(TextBlock t,string f)
        {
            tb = t;
            fullpath = f;
        }
        public TextBlock GetTB()
        {
            return this.tb;
        }
        public string GetFullpath()
        {
            return this.fullpath;
        }

    }
}
