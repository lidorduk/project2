using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
    /// Interaction logic for Window1.xaml
    /// </summary>
    /// 
    

    public partial class Window1 : Window
    {
        static Database1Entities mdbe = new Database1Entities();
        public Window1()
        {   
            Save();
            InitializeComponent();
            Fill();
        }
        public static bool AddCompToDB(string s,string IP)//הוספה של מחשב אל מסד הנתונים
        {
            List<string> seperators = new List<string>();
            seperators.Add("   ");
            List<string> Names = s.Split(seperators.ToArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
            bool found = false;
            foreach (Computer C in mdbe.Computers)//חיפוש האם קיים המחשב
            {
                if (C.MAC.Equals(Names[0]))
                {
                    found = true;
                    break;
                }
            }
            if (!found)//במידה ולא נמצא, יצירת אובייקט מסוג מחשב חדש והוספתו למסד
            {
                Computer C = new Computer();
                C.MAC = Names[0];
                C.IP = IP;
                C.MachineName = Names[1];
                C.OS = Names[2];
                mdbe.Computers.Add(C);
                mdbe.Entry(C).State = EntityState.Added;
                Save();
                return true;
            }
            return false;
        }
        public static int[] ConvertIPtoInt(string ip)//המרת מערך של מספרים שלמים למחרוזת IP
        {
            string[] s = ip.Split('.');
            int[] n = new int[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                n[i] = int.Parse(s[i]);
            }
            return n;
        }
        public void Fill()//סריקת הרשת המקומית למציאת מחשבים שבהם תוכנת הסוכן רצה
        {
            int[] ipAddrSplit = ConvertIPtoInt(MyIP);
            for (int j = 0; j <= 254; j++)
            {

                
                    string dest = ipAddrSplit[0] + "." + ipAddrSplit[1] + "." + ipAddrSplit[2] + "." + j;
                    if (HasListener(dest))//במידה ויש סוכן יצירת כפתור בשבילו
                    {
                        Button B = new Button();
                        B.Content = dest;
                        SP1.Children.Add(B);
                        B.Click += B_Click;
                    }
                
            }
        }

        private void B_Click(object sender, RoutedEventArgs e)//פתיחת חלון הסייר למחשב שנבחר
        {
            Button B = sender as Button;
            MainWindow MW = new MainWindow(B.Content.ToString(),mdbe);
            MW.Show();
        }

        public static bool HasListener(string IP)//בדיקה האם למחשב יש סוכן
        {
            TcpClient client = new TcpClient();
            NetworkStream netStream = null;
            try
            {
                int servPort = 40035;
                var result = client.BeginConnect(IP, servPort, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(0.05));//המתנה להתחברות, במידה ואין מענה הסוכן אינו זמין ולכן לא ניתן ליצור עמו קשר
                if (!success)
                {
                    return false;
                }
                netStream = client.GetStream();
                byte[] rcvBuffer = new byte[2048];
                byte[] byteBuffer = Encoding.UTF8.GetBytes("FIRS:");//שליחת הבקשה הראשונה לקבלת פרטים על המחשב למען הוספה למסד הנתונים
                netStream.Write(byteBuffer, 0, byteBuffer.Length);
                int bytesRcvd = 0;
                string s = "";
                int totalbytesrcv = 0;
                List<byte> test = new List<byte>();
                while (!netStream.DataAvailable)
                {
                }
                while (netStream.DataAvailable)
                {
                    rcvBuffer = new byte[2048];
                    bytesRcvd = netStream.Read(rcvBuffer, 0, rcvBuffer.Length);
                    totalbytesrcv += bytesRcvd;
                    byte[] clean = rcvBuffer.Take(bytesRcvd).ToArray();
                    test.AddRange(clean);
                }
                //קריאת המידע שנשלח על ידי הוסכן
                s = Encoding.UTF8.GetString(test.ToArray());
                netStream.Flush();
               return AddCompToDB(s,IP);//טיפול במידע שנקרא
            }
            catch (Exception E)
            {
                client.Close();
                if (netStream != null)
                {
                    netStream.Close();
                }
                return false;
            }
        }
        static string MyIP = GetIP();
        static string GetIP()// השגת האייפי של המחשב שבו רץ הממשק למען דילוג עליו במהלך הסריקה
        {
            ManagementObjectSearcher query = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = 'TRUE'");
            ManagementObjectCollection queryCollection = query.Get();
            foreach (ManagementObject mo in queryCollection)
            {
                string[] ipAddresses = (string[])mo["IPAddress"];
                return ipAddresses[0];
            }
            return null;
        }

        private void CompB_Click(object sender, RoutedEventArgs e)//פתיחת חלון המחשבים
        {
            CompInfo TC = new CompInfo(mdbe);
            TC.Show();
        }

        public static void Save()//שמירת השינויים במסד הנתונים
        {
            try
            {
                mdbe.SaveChanges();
            }
            catch (DbEntityValidationException dbEx)
            {
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        Trace.TraceInformation("Property: {0} Error: {1}",
                                                validationError.PropertyName,
                                                validationError.ErrorMessage);
                    }
                }
            }
        }

        private void FilesB_Click(object sender, RoutedEventArgs e)//פתיחת חלון הצגת הקבצים המוצפנים
        {
            Button B = sender as Button;
            EncryptedFiles EF = new EncryptedFiles(false,"LUL", mdbe);
            EF.Show();
        }
    }
}
