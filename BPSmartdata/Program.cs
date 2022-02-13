using System;
using Topshelf;

namespace BPSmartdata
{
    class Program
    {
        static void Main(string[] args)
        {
            var exitCode = HostFactory.Run(x =>
            {
                x.Service<DataCollection>(s =>
                {
                    s.ConstructUsing(dataCollection => new DataCollection());
                    s.WhenStarted(dataCollection => dataCollection.Start());
                    s.WhenStopped(dataCollection => dataCollection.Stop());
                });
            
                x.RunAsLocalSystem();
                
                x.SetServiceName("SMARTDataCollectionService");
                x.SetDisplayName("SMART Data Collection Service");
                x.SetDescription("Service for collecting SMART data of storage drives");
            });
            
            int exitCodeValue = (int) Convert.ChangeType(exitCode, exitCode.GetTypeCode());
            Environment.ExitCode = exitCodeValue;
        }
    }
}