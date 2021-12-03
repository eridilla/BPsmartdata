using System;
using System.IO;
using System.Net;
using System.Timers;
using System.Management;
using System.Runtime.InteropServices;

namespace BPSmartdata
{
    public class Heartbeat
    {
        private readonly Timer _timer;

        public Heartbeat()
        {
            _timer = new Timer(1000) {AutoReset = true};
            _timer.Elapsed += TimerElapsed;
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            // string[] lines = {DateTime.Now.ToString(), GetWinVersion()};
            // File.AppendAllLines(@"E:\TUKE\Rider\BPSmartdata\BPSmartdata\out.txt", lines);
            // Console.WriteLine($"{DateTime.Now.ToString()} | {GetWinVersion()}");

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
                byte spinRetryCount = 0x0A;
                int delta = 12;
                foreach (ManagementObject wmiObject in searcher.Get())
                {
                    byte[] vendorSpecific = (byte[])wmiObject["VendorSpecific"];
                    for (int offset = 2; offset < vendorSpecific.Length; )
                    {
                        if (vendorSpecific[offset] == spinRetryCount)
                        {
                            IntPtr buffer = IntPtr.Zero;
                            try
                            {
                                buffer = Marshal.AllocHGlobal(delta);
                                Marshal.Copy(vendorSpecific, offset, buffer, delta);
                                attributeInfo = (Attribute)Marshal.PtrToStructure(buffer, typeof(Attribute));
                                Console.WriteLine("AttributeID {0}", attributeInfo.AttributeID);
                                Console.WriteLine("Flags {0}", attributeInfo.Flags);
                                Console.WriteLine("Value {0}", attributeInfo.Value);
                                //if you want HEX values use this line
                                // Console.WriteLine("Value {0}", BitConverter.ToString(attributeInfo.VendorData));
                                //if you want INT values use this line
                                Console.WriteLine("Data {0}", BitConverter.ToInt32(attributeInfo.VendorData, 0));
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