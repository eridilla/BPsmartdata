using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Timers;
using System.Management;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using JWT.Algorithms;
using JWT.Builder;
using Newtonsoft.Json;

namespace BPSmartdata
{
    public class DataCollection
    {
        private readonly Timer _timer;
        private string id;
        private string token;

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
            
            split = lines[1].Split(' ');
            id = split[3];
            
            split = lines[2].Split(' ');
            token = split[3];

            _timer = new Timer(interval) {AutoReset = true};
            _timer.Elapsed += TimerElapsed;
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            OsData os = new OsData(GetWinVersion());
            List<Disk> disks = GetDisks();
            DiskLayoutData[] layout = new DiskLayoutData[disks.Count];
            
            for (int i = 0; i < disks.Count; i++)
            {
                layout[i] = new DiskLayoutData(disks[i]);
            }
            
            var encoded = JwtBuilder.Create()
                                        .WithAlgorithm(new HMACSHA256Algorithm())
                                        .WithSecret(token)
                                        .AddClaim("os", os)
                                        .AddClaim("disks", layout)
                                        .Encode();
            
            using (var client = new HttpClient())
            {
                var endpoint = new Uri("https://smart-tuke.herokuapp.com/api/sonda/smart");
                var post = new Post()
                {
                    id = this.id,
                    data = encoded
                };
                var postJson = JsonConvert.SerializeObject(post);
                var payload = new StringContent(postJson, Encoding.UTF8, "application/json");
                var result = client.PostAsync(endpoint, payload).Result.Content.ReadAsStringAsync().Result;
            }
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
        
        // [StructLayout(LayoutKind.Sequential)]
        // public struct Attribute
        // {
        //     public byte AttributeID;
        //     public ushort Flags;
        //     public byte Value;
        //     [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        //     public byte[] VendorData;
        // }

        // public struct Disk
        // {
        //     public String Id;
        //     public List<String> Name;
        //     public int Index;
        //     public String Model;
        //     public String Type;
        // }

        public List<Disk> GetDisks()
        {
            var drives = new List<Disk>();
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

            foreach (ManagementObject drive in searcher.Get())
            {
                var disk = new Disk();

                disk.DeviceID = drive["DeviceId"].ToString().Trim();
                disk.PnpDeviceID = drive.GetPropertyValue("PNPDeviceID").ToString();
                disk.Index = Convert.ToInt32(drive["Index"].ToString().Trim());
                disk.Model = drive["Model"].ToString().Trim();
                disk.Type = drive["InterfaceType"].ToString().Trim();
                disk.Serial = drive["SerialNumber"].ToString().Trim();

                foreach (var partition in new ManagementObjectSearcher(
                             "ASSOCIATORS OF {Win32_DiskDrive.DeviceID='" + drive.Properties["DeviceID"].Value
                                    + "'} WHERE AssocClass = Win32_DiskDriveToDiskPartition").Get())
                {

                    foreach (var disk2 in new ManagementObjectSearcher(
                                 "ASSOCIATORS OF {Win32_DiskPartition.DeviceID='"
                                 + partition["DeviceID"]
                                 + "'} WHERE AssocClass = Win32_LogicalDiskToPartition").Get())
                    {
                        disk.DriveLetters.Add(disk2["Name"].ToString());
                    }

                }

                disk = GetSmartAttributes(disk);

                if (disk.Type != "SCSI")
                {
                    drives.Add(disk);
                }
            }
            return drives;
        }

        public Disk GetSmartAttributes(Disk disk)
        {
            ManagementScope scope = new ManagementScope("\\\\.\\ROOT\\WMI");
            ObjectQuery query = new ObjectQuery(@"SELECT * FROM MSStorageDriver_FailurePredictStatus Where InstanceName like ""%"
                                                + disk.PnpDeviceID.Replace("\\", "\\\\") + @"%""");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
            ManagementObjectCollection queryCollection = searcher.Get();
            foreach (ManagementObject m in queryCollection)
            {
                disk.IsOK = (bool)m.Properties["PredictFailure"].Value == false;
            }
            
            var collection = new AttributeCollection();

            try
            {
                var splitOnCRLF = SmartAttributes.AttributeList.Split(
                    Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in splitOnCRLF)
                {
                    var splitLineOnComma = line.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
                    string register = splitLineOnComma[0].Trim();
                    string attributeName = splitLineOnComma[1].Trim();

                    collection.Add(new Attribute(ConvertStringHexToInt(register), attributeName));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("GetSmartRegisters failed with error " + ex);
            }
            
            disk.SmartAttributes.AddRange(collection);

            searcher.Query = new ObjectQuery(@"Select * from MSStorageDriver_FailurePredictData Where InstanceName like ""%"
                                             + disk.PnpDeviceID.Replace("\\", "\\\\") + @"%""");

            foreach (ManagementObject data in searcher.Get())
            {
                byte[] bytes = (byte[])data.Properties["VendorSpecific"].Value;
                for (int i = 0; i < 42; ++i)
                {
                    try
                    {
                        int id = bytes[i * 12 + 2];
                        int flags = bytes[i * 12 + 4]; 
                        bool failureImminent = (flags & 0x1) == 0x1;
                        int value = bytes[i * 12 + 5];
                        int worst = bytes[i * 12 + 6];
                        int vendordata = BitConverter.ToInt32(bytes, i * 12 + 7);
                        if (id == 0) continue;

                        var attr = disk.SmartAttributes.GetAttribute(id);
                        if (attr != null)
                        {
                            attr.Current = value;
                            attr.Worst = worst;
                            attr.Data = vendordata;
                            attr.IsOK = failureImminent == false;
                        }
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(@"errorLog.txt", ex.Message);
                    }
                }
            }
            
            searcher.Query = new ObjectQuery(@"Select * from MSStorageDriver_FailurePredictThresholds Where InstanceName like ""%"
                                             + disk.PnpDeviceID.Replace("\\", "\\\\") + @"%""");
            foreach (ManagementObject data in searcher.Get())
            {
                byte[] bytes = (byte[]) data.Properties["VendorSpecific"].Value;
                for (int i = 0; i < 42; ++i)
                {
                    try
                    {
                        int id = bytes[i * 12 + 2];
                        int thresh = bytes[i * 12 + 3];
                        if (id == 0) continue;
                        var attr = disk.SmartAttributes.GetAttribute(id);
                        
                        if (attr != null)
                        {
                            attr.Threshold = thresh;
                        }
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(@"errorLog.txt", ex.Message);
                    }
                }
            }

            return disk;
        }
        
        // static void GetSmartAttr()
        // {
        //     try
        //     {
        //         Attribute attributeInfo;
        //         ManagementScope scope = new ManagementScope(String.Format("\\\\{0}\\root\\WMI", "localhost"), null);
        //         scope.Connect();
        //         ObjectQuery query = new ObjectQuery("SELECT VendorSpecific FROM MSStorageDriver_ATAPISmartData");
        //         ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
        //         byte attributeID = 0x0A;
        //         int delta = 12;
        //         foreach (ManagementObject wmiObject in searcher.Get())
        //         {
        //             byte[] vendorSpecific = (byte[])wmiObject["VendorSpecific"];
        //             for (int offset = 2; offset < vendorSpecific.Length; )
        //             {
        //                 if (vendorSpecific[offset] == attributeID)
        //                 {
        //                     IntPtr buffer = IntPtr.Zero;
        //                     try
        //                     {
        //                         buffer = Marshal.AllocHGlobal(delta);
        //                         Marshal.Copy(vendorSpecific, offset, buffer, delta);
        //                         attributeInfo = (Attribute)Marshal.PtrToStructure(buffer, typeof(Attribute));
        //
        //                         File.AppendAllText(@"out.txt", String.Format("{0}\n", DateTime.Now.ToString()));
        //                         File.AppendAllText(@"out.txt", String.Format("AttributeID {0}\n", attributeInfo.AttributeID));
        //                         File.AppendAllText(@"out.txt", String.Format("Flags {0}\n", attributeInfo.Flags));
        //                         File.AppendAllText(@"out.txt", String.Format("Value {0}\n", attributeInfo.Value));
        //                         File.AppendAllText(@"out.txt", String.Format("Data {0}\n\n", BitConverter.ToInt32(attributeInfo.VendorData, 0)));
        //
        //                         //Console.WriteLine("AttributeID {0}", attributeInfo.AttributeID);
        //                         //Console.WriteLine("Flags {0}", attributeInfo.Flags);
        //                         //Console.WriteLine("Value {0}", attributeInfo.Value);
        //                         //HEX Console.WriteLine("Value {0}", BitConverter.ToString(attributeInfo.VendorData));
        //                         //INT
        //                         //Console.WriteLine("Data {0}", BitConverter.ToInt32(attributeInfo.VendorData, 0));
        //                     }
        //                     finally
        //                     {
        //                         if (buffer != IntPtr.Zero)
        //                         {
        //                             Marshal.FreeHGlobal(buffer);
        //                         }
        //                     }
        //                 }
        //                 offset += delta;
        //             }
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         Console.WriteLine(String.Format("Exception {0} Trace {1}", e.Message, e.StackTrace));
        //     }
        //     
        // }
        
        public static int ConvertStringHexToInt(string hex)
        {
            try
            {
                int value = (int) new System.ComponentModel.Int32Converter().ConvertFromString(hex);
                return value;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error converting hex value {hex} to integer.", ex);
            }
        }
    }

    public class Post
    {
        public string id { get; set; }
        public string data { get; set; }
    }
}