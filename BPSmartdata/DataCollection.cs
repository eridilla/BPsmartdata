using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Timers;
using System.Management;
using System.Runtime.InteropServices;

namespace BPSmartdata
{
    
    // https://raw.githubusercontent.com/sebhildebrandt/systeminformation/master/lib/index.d.ts  
    // body: { id: String, data: sign({Object<os>, Object<disks>}, String<Token>)<JWT> }

    public class DataCollection
    {
        private readonly Timer _timer;

        public DataCollection()
        {
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;            
            string configPath = System.IO.Path.Combine(currentDir, @"config.ini");
            string absolutePath = Path.GetFullPath(configPath);
            
            string[] lines = System.IO.File.ReadAllLines(absolutePath);
            
            string[] split = lines[0].Split(' ');
            
            NumberFormatInfo provider = new NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";
            provider.NumberGroupSeparator = ",";
            double interval = Math.Round(Convert.ToDouble(split[4], provider) * 3600 * 1000);

            //_timer = new Timer(interval) {AutoReset = true};
            _timer = new Timer(1000) {AutoReset = true};
            _timer.Elapsed += TimerElapsed;
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            GetSmartAttr();
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private String GetWinVersion()
        {
            String result = "Unable to retrieve Windows version.";
            ManagementScope scope = null;

            try
            {
                ConnectionOptions options = new ConnectionOptions();
                options.Impersonation = System.Management.ImpersonationLevel.Impersonate;

                scope = new ManagementScope(String.Format("\\\\{0}\\root\\cimv2", "localhost"), options);
                scope.Connect();

                ObjectQuery query = new ObjectQuery("SELECT Caption FROM win32_OperatingSystem");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
                ManagementObjectCollection queryCollection = searcher.Get();
                
                foreach (ManagementObject o in queryCollection)
                {
                    result = o["Caption"].ToString();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return result;
        } 
        
        [StructLayout(LayoutKind.Sequential)]
        public struct Attribute
        {
            public byte AttributeID;
            public ushort Flags;
            public byte Value;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] VendorData;
        }

        static void GetSmartAttr()
        {
            try
            {
                Attribute attributeInfo;
                ManagementScope scope = new ManagementScope(String.Format("\\\\{0}\\root\\WMI", "localhost"), null);
                scope.Connect();
                ObjectQuery query = new ObjectQuery("SELECT VendorSpecific FROM MSStorageDriver_ATAPISmartData");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
                byte attributeID = 0x0A;
                int delta = 12;
                foreach (ManagementObject wmiObject in searcher.Get())
                {
                    byte[] vendorSpecific = (byte[])wmiObject["VendorSpecific"];
                    for (int offset = 2; offset < vendorSpecific.Length; )
                    {
                        if (vendorSpecific[offset] == attributeID)
                        {
                            IntPtr buffer = IntPtr.Zero;
                            try
                            {
                                buffer = Marshal.AllocHGlobal(delta);
                                Marshal.Copy(vendorSpecific, offset, buffer, delta);
                                attributeInfo = (Attribute)Marshal.PtrToStructure(buffer, typeof(Attribute));

                                File.AppendAllText(@"out.txt", String.Format("{0}\n", DateTime.Now.ToString()));
                                File.AppendAllText(@"out.txt", String.Format("AttributeID {0}\n", attributeInfo.AttributeID));
                                File.AppendAllText(@"out.txt", String.Format("Flags {0}\n", attributeInfo.Flags));
                                File.AppendAllText(@"out.txt", String.Format("Value {0}\n", attributeInfo.Value));
                                File.AppendAllText(@"out.txt", String.Format("Data {0}\n\n", BitConverter.ToInt32(attributeInfo.VendorData, 0)));

                                //Console.WriteLine("AttributeID {0}", attributeInfo.AttributeID);
                                //Console.WriteLine("Flags {0}", attributeInfo.Flags);
                                //Console.WriteLine("Value {0}", attributeInfo.Value);
                                //HEX Console.WriteLine("Value {0}", BitConverter.ToString(attributeInfo.VendorData));
                                //INT
                                //Console.WriteLine("Data {0}", BitConverter.ToInt32(attributeInfo.VendorData, 0));
                            }
                            finally
                            {
                                if (buffer != IntPtr.Zero)
                                {
                                    Marshal.FreeHGlobal(buffer);
                                }
                            }
                        }
                        offset += delta;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("Exception {0} Trace {1}", e.Message, e.StackTrace));
            }
            
        }
    }
}