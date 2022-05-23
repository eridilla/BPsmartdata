using System.Diagnostics;

namespace ServiceInstaller
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = args[0];

            if (path == "uninstall")
            {
                path = args[1];

                foreach (string arg in args.Skip(2))
                {
                    path = path + " " + arg;
                }

                string driveLetter = String.Format(@"{0}{1}", path[0], path[1]);

                try
                {
                    Process proc = new Process();
                    proc.StartInfo.FileName = @"cmd.exe";
                    proc.StartInfo.UseShellExecute = true;
                    proc.StartInfo.Verb = "runas";
                    proc.StartInfo.Arguments = String.Format(@"/C {0}&cd {1}&BPSmartdata.exe uninstall&pause", driveLetter, path);
                    //proc.StartInfo.Arguments = @"/C echo " + path + @" > E:\out.txt";
                    proc.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace.ToString());
                }
            } else
            {
                foreach (string arg in args.Skip(1))
                {
                    path = path + " " + arg;
                }

                string driveLetter = String.Format(@"{0}{1}", path[0], path[1]);

                try
                {
                    Process proc = new Process();
                    proc.StartInfo.FileName = @"cmd.exe";
                    proc.StartInfo.UseShellExecute = true;
                    proc.StartInfo.Verb = "runas";
                    proc.StartInfo.Arguments = String.Format(@"/C {0}&cd {1}&BPSmartdata.exe install start&pause", driveLetter, path);
                    //proc.StartInfo.Arguments = @"/C echo install > E:\out.txt";
                    proc.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace.ToString());
                }
            }
        }
    }
}