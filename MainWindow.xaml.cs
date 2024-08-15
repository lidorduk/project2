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
using System.Data.Entity;
using System.IO;
using System.Data.Entity.Validation;
using System.Diagnostics;

namespace GUI___Encrypt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static Database1Entities mdbe;
        static string currentpath;
        static string searchpath;
        static string encdyc;
        static string CurrentIP;
        public MainWindow(string IP, Database1Entities D)
        {
            InitializeComponent();
            CurrentIP = IP;
            mdbe = D;
            ComHandlerPath("FIRS:");//שליחת הבקשה הראשונה למידע על המחשב
            ComHandlerPath("DRIV:");//שליחת בקשה לכוננים שעל המחשב
            CurrentMAC = mdbe.Computers.Single(T => T.IP.Equals(CurrentIP)).MAC;
        }
        private void Drivers_Click(object sender, RoutedEventArgs e)//בקשה לסריקה חדשה של הכוננים במחשב
        {
            Drivers.Children.Clear();
            ComHandlerPath("DRIV:");
        }
        private void Drive_Click(object sender, RoutedEventArgs e)//שליחת בקשה למעבר של נתיב לכונן מסוים
        {
            Button B = sender as Button;
            searchpath = B.Content.ToString();
            encdyc = B.Content.ToString();
            string Str = "PATH:" + B.Content.ToString();
            ComHandlerPath(Str);
            currentpath = B.Content.ToString();

        }
        private void Path_Click(object sender, RoutedEventArgs e)//שליחת בקשה למעבר לנתיב מסוים
        {
            if (TB.Text != "" && TB.Text != null)//במידה ויש נתיב בשורת החיפוש יש לחפש אותו
            {
                searchpath = TB.Text;
                string Str = "PATH:" + TB.Text;
                ComHandlerPath(Str);
            }
            else
            {
                if (searchpath != null)//במידה ונבחרה תיקייה בסייר יש לחפש את נתיבה
                {
                    if (!searchpath.Equals(currentpath))
                    {
                        string Str = "PATH:" + searchpath;
                        ComHandlerPath(Str);

                    }
                }
            }
        }
        private void LbTodoList_SelectionChanged(object sender, SelectionChangedEventArgs e)//בחירת תיקייה
        {
            if (LB1.SelectedItem != null)
            {
                searchpath = (LB1.SelectedItem as TextBlock).Text;
                if ((LB1.SelectedItem as TextBlock).Background != null)
                {
                    encdyc = (LB1.SelectedItem as TextBlock).Text;
                }
            }
        }
        static string CurrentMAC = null;
        public void ComHandlerPath(string Str)//הפעולה אשר מבצעת את התקשורת עם הסוכן, מטרתה היא להעביר את הבקשות אל הסוכן
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
                //יצירת קשר עם הסוכן
                byte[] rcvBuffer = new byte[2048];
                byte[] byteBuffer = Encoding.UTF8.GetBytes(Str.ToString());
                netStream.Write(byteBuffer, 0, byteBuffer.Length);//שליחת בקשה אל הסוכן
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
                //קריאת הפלט שהתקבל מן הסוכן
                s = Encoding.UTF8.GetString(test.ToArray());
                netStream.Flush();
                OUTPUT.Text = s;
                switch (Str.Substring(0, 5))
                {
                    case ("DECR:")://נשלחה בקשת הצפנה
                        {
                            while (!s.Substring(0, 8).Equals("FINISHED"))//ריצה ושליחה של בקשות פענוח בתיקייה מסוימת
                            {
                                File Fsend = null;
                                int Count = 0;
                                foreach (File F in mdbe.Files)
                                {
                                    if (F.MAC.Equals(CurrentMAC) && F.path.Equals(s))
                                    {
                                        if (F.Time > Count)
                                        {
                                            Count = F.Time;
                                            Fsend = F;
                                        }
                                    }
                                }
                                //חיפוש האם הקובץ הוצפן ונמצא במסד הנתונים
                                byte[] Send = null;
                                if (Fsend != null)
                                {
                                    string pass = Encoding.UTF8.GetString(Fsend.password);
                                    string sal = Encoding.UTF8.GetString(Fsend.salt);
                                    mdbe.Files.Remove(Fsend);
                                    mdbe.Entry(Fsend).State = EntityState.Deleted;
                                    Save();
                                    Send = Encoding.UTF8.GetBytes("YES:" + pass + "   " + sal);
                                }
                                //השגת המידע על הקובץ במידה וקיים 
                                else
                                {
                                    Send = Encoding.UTF8.GetBytes("NOO:");
                                }
                                totalbytesrcv = 0;
                                s = null;
                                netStream.Write(Send, 0, Send.Length);
                                //שליחת המידע אל הסוכן
                                while (!netStream.DataAvailable)
                                {

                                }
                                while (netStream.DataAvailable)
                                {
                                    rcvBuffer = new byte[2048];
                                    bytesRcvd = netStream.Read(rcvBuffer, 0, rcvBuffer.Length);
                                    totalbytesrcv += bytesRcvd;
                                    byte[] clean = rcvBuffer.Take(bytesRcvd).ToArray();
                                    s += Encoding.UTF8.GetString(clean);
                                   
                                }
                                //קריאת התשובה מן הסוכן
                                netStream.Flush();
                                OUTPUT.Text = s;
                            }
                        }
                        break;
                    case ("FIRS:")://נשלחה הבקשה הראשונה
                        {
                            AddCompToDB(s, CurrentIP);//הוספת המידע שהתקבל אל המאגר
                        }
                        break;
                    case ("ENCR:")://נשלחה בקשת הצפנה
                        {
                            while (!s.Substring(0, 8).Equals("FINISHED"))//ריצה ושליחה של בקשות הצפנה בתיקייה מסוימת
                            {
                                List<string> seperators = new List<string>() { "   " };
                                List<string> Names = s.Split(seperators.ToArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                                //חילוק המידע שהתקבל
                                if (Names[0].Substring(0, 4).Equals("YES:"))
                                {
                                    int count = 0;
                                    foreach (File F in mdbe.Files)
                                    {
                                        if (F.path.Equals(Names[0].Remove(0, 4)) && F.MAC.Equals(CurrentMAC))
                                        {
                                            count++;
                                        }
                                    }
                                    File f = new File()
                                    {
                                        Time = count + 1,
                                        MAC = CurrentMAC,
                                        path = Names[0].Remove(0, 4)
                                    };                                 
                                    f.password = Encoding.UTF8.GetBytes(Names[1]);
                                    f.salt = Encoding.UTF8.GetBytes(Names[2]);
                                    mdbe.Files.Add(f);
                                    mdbe.Entry(f).State = EntityState.Added;
                                    Save();
                                    // הוספת המידע על הקבצים שהוצפנו מן הסוכן אל מסד הנתונים
                                }
                                byte[] send = Encoding.UTF8.GetBytes("1");
                                netStream.Write(send, 0, send.Length);
                                //שליחת אישור קבלה
                                s = null;
                                while (!netStream.DataAvailable)
                                {

                                }
                                while (netStream.DataAvailable)
                                {
                                    rcvBuffer = new byte[2048];
                                    bytesRcvd = netStream.Read(rcvBuffer, 0, rcvBuffer.Length);
                                    totalbytesrcv += bytesRcvd;
                                    byte[] clean = rcvBuffer.Take(bytesRcvd).ToArray();
                                    s += Encoding.UTF8.GetString(clean);
                                }
                                //קריאת מידע מן הסוכן
                            }
                        }
                        break;
                    case ("DRIV:")://נשלחה בקשת חיפוש כוננים
                        {
                            List<string> seperators = new List<string>();
                            seperators.Add("   ");
                            List<string> Names = s.Split(seperators.ToArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                            foreach (string n in Names)
                            {
                                Button B = new Button();
                                B.Content = n;
                                B.Click += Drive_Click;
                                B.Width = (300 / Names.Count);
                                Drivers.Children.Add(B);
                            }
                            //פענוח המידע והוספת כפתורים לכוננים
                        }
                        break;

                    case ("PATH:")://נשלחה בקשה של חיפוש נתיב
                        {

                            List<string> seperators = new List<string>();
                            seperators.Add("   ");
                            List<string> Names = s.Split(seperators.ToArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                            LB1.Items.Clear();
                            int div = Names.IndexOf("*****");
                            List<string> Files = new List<string>();
                            List<string> Folders = new List<string>();
                            foreach (string n in Names)
                            {
                                if (n.Substring(0, 5).Equals("FILE:"))
                                {
                                    string add = n.Replace(n.Substring(0, 5), "");
                                    Files.Add(add);
                                }
                                else
                                {
                                    string add = n.Replace(n.Substring(0, 5), "");
                                    Folders.Add(add);
                                }
                            }
                            //הבחנה בין נתיבים ותיקיות
                            foreach (string folder in Folders)
                            {
                                TextBlock Tb = new TextBlock();
                                string oz = folder.Replace(Str.Substring(5), "");
                                Tb.Text = folder;
                                Tb.Background = Brushes.Yellow;
                                Tb.Width = LB1.Width;
                                LB1.Items.Add(Tb);
                            }
                            //יצירת טקסטים לתיקיות שהתקבלו
                            foreach (string File in Files)
                            {
                                TextBlock Tb = new TextBlock();
                                string oz = File.Replace(Str.Substring(5), "");
                                Tb.Text = File;
                                LB1.Items.Add(Tb);
                            }
                            //יצירת טקסטים לקבצים שהתקבלו
                            searchpath = null;
                            currentpath = Str.Substring(5, Str.Length - 5);//עדכון הנתיב הנוכחי
                            TB.Text = "";
                        }
                        break;
                }
                client.Close();
                netStream.Close();
                //סגירת הסטרים והקליינט עם הסוכן

            }
            catch (Exception E)
            {
                OUTPUT.Text = "HERE: " + E.Message;
                client.Close();
                if (netStream != null)
                {
                    netStream.Close();
                }
            }
            //אירועי שגיאה
        }
        private void Back_Click(object sender, RoutedEventArgs e)//כפתור החזרה לאחור
        {

            if (LB1.SelectedItem == null)
            {
                if (currentpath != null)
                {
                    if (currentpath.Remove(currentpath.LastIndexOf('\\')).Length > 2)
                    {
                        string Str = "PATH:" + currentpath.Remove(currentpath.LastIndexOf('\\'));//מציאת תיקיית האם
                        ComHandlerPath(Str);
                    }
                    else
                    {
                        try
                        {
                            string Str = "PATH:" + currentpath.Remove(currentpath.LastIndexOf('\\') + 1);//במידה ואין תיקיית אם, הנתיב יהיה הכונן
                            ComHandlerPath(Str);
                        }
                        catch (Exception)
                        {
                        }

                    }
                }
            }
            else
            {
                string first = currentpath.Remove(currentpath.LastIndexOf('\\'));     
                if (first.Length <= 2)
                {
                    string Str = "PATH:" + first + "\\";
                    ComHandlerPath(Str);
                    currentpath = first + "\\";
                }
                else
                {
                    string Str = "PATH:" + first;
                    ComHandlerPath(Str);
                    currentpath = first;

                }


            }
        }
        private void Encrypt_Click(object sender, RoutedEventArgs e)//כפתורי ההצפנה
        {
            Button B = sender as Button;
            string extensions = null;
            switch (B.Content.ToString())
            {
                case ("Pictures")://הצפנה של תמונות על פי הפורמטים
                    extensions = "   .JPEG   .JPG   .BMP   .PNG";
                    break;
                case ("Documents")://הצפנה של מסמכים 
                    extensions = "   .DOC   .DOCX   .XLS   .XLSX   .PPT   .PPTX";
                    break;
                case ("Encrypt")://הצפנה כללית של כל הקבצים
                    extensions = "";
                    break;
            }

            if (TB.Text != "" && TB.Text != null)//הצפנה על פי תיבת החיפוש
            {
                string Str = "ENCR:" + TB.Text + extensions;
                ComHandlerPath(Str);
            }
            else
            {
                if (searchpath != null)
                {
                    ComHandlerPath("ENCR:" + searchpath + extensions);//הצפנה של התיקייה או הקובץ שנבחר בסייר
                }
                else
                {
                    ComHandlerPath("ENCR:" + currentpath + extensions);//הצפנה של התיקייה הנוכחית
                }
            }
            LB1.SelectedItem = null;
        }
        private void Decrypt_Click(object sender, RoutedEventArgs e)//כפתור שליחת הפענוח
        {
            if (TB.Text != "" && TB.Text != null)//פענוח הנתיב שבתיבת החיפוש
            {
                string Str = "DECR:" + TB.Text;
                ComHandlerPath(Str);
            }

            else
            {
                if (searchpath != null)
                {
                    ComHandlerPath("DECR:" + searchpath);//פענוח התיקייה או הקובץ שנבחרו בסייר
                }
                else
                {
                    ComHandlerPath("DECR:" + currentpath);//פענוח התיקייה הנוכחית
                }
            }
            LB1.SelectedItem = null;
        }
        public static void Save()//שמירת שינויים במסד הנתונים
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

        private void FilesB_Click(object sender, RoutedEventArgs e)//כפתור מעבר לחלון הקבצים המוצפנים
        {
            EncryptedFiles EF = new EncryptedFiles(true, CurrentMAC, mdbe);
            EF.Show();

        }
        public static bool AddCompToDB(string s, string IP)//הוספת המידע שהתקבל מהבקשה הראשונה אל מסד הנתונים
        {
            List<string> seperators = new List<string>();
            seperators.Add("   ");
            List<string> Names = s.Split(seperators.ToArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
            CurrentMAC = Names[0];
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
    }
}

