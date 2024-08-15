using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
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
using System.Windows.Shapes;

namespace GUI___Encrypt
{
    /// <summary>
    /// Interaction logic for CompInfo.xaml
    /// </summary>
    public partial class CompInfo : Window
    {
        public static Database1Entities mdbe;
        public CompInfo(Database1Entities D)
        {
            InitializeComponent();
            mdbe = D;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CollectionViewSource computerViewSource = ((CollectionViewSource)(this.FindResource("computerViewSource")));
            computerViewSource.Source = mdbe.Computers.Local;
            //יצירת הקשר בין הטבלה למסד הנתונים
        }
    }
}
