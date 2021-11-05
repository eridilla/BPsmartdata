using System;
using System.IO;
using System.Net;
using System.Timers;
using System.Management;

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
            string[] lines = new string[] {DateTime.Now.ToString(), GetWinVersion()};
            File.AppendAllLines(@"E:\TUKE\Rider\BPSmartdata\BPSmartdata\out.txt", lines);
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

                scope = new ManagementScope("\\\\eddie\\root\\cimv2", options);
                scope.Connect();

                ObjectQuery query = new ObjectQuery("Select Caption FROM win32_OperatingSystem");
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
    }
}