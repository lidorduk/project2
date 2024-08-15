using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
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
    /// Interaction logic for EncryptedFiles.xaml
    /// </summary>
    public partial class EncryptedFiles : Window
    {
        Database1Entities mdbe;
        string CurrentMAC;
        bool Specific;
        CollectionViewSource FileViewSource;
        List<File> Filtered;
        public EncryptedFiles(bool S, string CM, Database1Entities D)
        {
            InitializeComponent();
            mdbe = D;
            CurrentMAC = CM;
            Specific = S;       
        }
        private void DecryptB_Click(object sender, RoutedEventArgs e)//כפתור פענוח כל הקבצים שנמצאים בטבלה
        {

            while (Filtered.Count() > 0)
            {
                File F = Filtered.First();
                string MAC = F.MAC;
                string path = F.path;
                File Highest = Filtered.Where(H => H.MAC.Equals(MAC) && H.path.Equals(path)).OrderByDescending(H => H.Time).First();
                //מציאת ההצפנה האחרונה של קובץ מסוים
                string pass = Encoding.UTF8.GetString(Highest.password);
                string sal = Encoding.UTF8.GetString(Highest.salt);
                string Str = "ASDE:" + Highest.path + "   " + pass + "   " + sal;
                string IP = mdbe.Computers.Single(C => C.MAC.Equals(Highest.MAC)).IP;
                //השגת המידע ממסד הנתונים על הקובץ
                SendDecrypt(IP, Str, Highest.MAC);
                Filtered.Remove(Highest);
            }
            InitFVS();
        }

        public void SendDecrypt(string CurrentIP, string Str, string CurrentMAC)//יצירת קשר עם הסוכן ושליחת בקשת פענוח של קובץ
        {
            TcpClient client = new TcpClient();
            NetworkStream netStream = null;
            try
            {
                String Listener = CurrentIP;
                int servPort = 40035;
                var result = client.BeginConnect(Listener, servPort, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(0.5));
                if (!success)
                {
                    throw new Exception("Failed to connect.");
                }
                netStream = client.GetStream();
                byte[] rcvBuffer = new byte[2048];
                byte[] byteBuffer = Encoding.UTF8.GetBytes(Str.ToString());
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
                s = Encoding.UTF8.GetString(test.ToArray());
                netStream.Flush();
                if (s.Substring(0, 4).Equals("YES:"))
                {
                    string p = s.Remove(0, 4);
                    File Fsend = null;
                    int count = 0;
                    Fsend = mdbe.Files.Where(F => F.MAC.Equals(CurrentMAC) && F.path.Equals(p)).OrderByDescending(T => T.Time).First();//השגת הקובץ
                    mdbe.Files.Remove(Fsend);
                    mdbe.Entry(Fsend).State = EntityState.Deleted;
                    //מחיקת הקובץ ממסד הנתונים
                    Save();
                }
            }
            catch (Exception E)
            {
                client.Close();
                if (netStream != null)
                {
                    netStream.Close();
                }
            }
        }
        public void Save()//שמירת השינויים במסד הנתונים
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
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitFVS();
        }
        public void InitFVS()
        {
            FileViewSource = ((CollectionViewSource)(this.FindResource("fileViewSource")));
            FileViewSource.Source = mdbe.Files.Local;
            if (Specific)
            {
                Filtered= mdbe.Files.ToList().Where(f => f.MAC.Equals(CurrentMAC)).ToList();
            }
            else
            {
                Filtered = mdbe.Files.ToList();
            }
            // FileViewSource.Source = mdbe.Files.Select()
            FileViewSource.Filter += new FilterEventHandler(Filter);
        }//טעינת הטבלה על פי הנתונים במסד הנתונים
        private void Filter(object sender, FilterEventArgs e)//מיון הנתונים הרלוונטים מתוך המסד
        {
            if (e.Item != null)
            {
                File f = e.Item as File;
                bool found = false;
                foreach (File F in mdbe.Files)
                {
                    if (F.MAC.Equals(f.MAC) && F.path.Equals(f.path))
                    {
                        if (F.Time > f.Time)
                        {
                            found = true;
                            e.Accepted = false;
                            break;
                        }
                    }
                    //הצגה אחת של מידע על קובץ על פי מספר הפעמים שהוצפן
                }
                if (!found)
                {
                    if (Specific)
                    {
                        if (f.MAC.Equals(CurrentMAC))
                        {
                            e.Accepted = true;
                        }
                        else
                        {
                            e.Accepted = false;
                        }
                    }
                    else
                    {
                        e.Accepted = true;
                    }
                    //מיון על פי כניסה מחלון התפריט או מהסייר
                }
            }
        }
    }
}