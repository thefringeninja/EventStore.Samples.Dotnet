using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EventStore.Common.Log;

namespace EmbeddedClustering
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            LogManager.SetLogFactory(_ => new ConsoleLogger());
            if (args.Length == 0)
            {
                StartCluster();
            }
            else
            {
                StartClusterNode(int.Parse(args[0]));
            }


            return 0;
        }

        [DllImport("user32.dll")]
        public static extern bool SetWindowText(IntPtr hwnd, string title);

        private static void StartClusterNode(int nodeId)
        {
            var process = Process.GetCurrentProcess();
            
            var node = GetEventStoreClusterNode.Create(
                nodeId, 3, process.NotifyMaster, process.NotifyNotMaster);
            
            node.Start();

            Console.ReadLine();
        }

      
        private static void NotifyMaster(this Process process)
        {
            SetWindowText(process, "I am master. Kill me to see an election!");
        }
        private static void NotifyNotMaster(this Process process)
        {
            SetWindowText(process, "I am not master.");
        }

        private static void SetWindowText(this Process process, string title)
        {
            SetWindowText(process.MainWindowHandle, title);
        }

        private static void StartCluster()
        {
            var executable = Path.Combine(
                AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                AppDomain.CurrentDomain.SetupInformation.ApplicationName);

            var clusterProcesses = Enumerable.Range(0, 3)
                .Select(nodeId => new ProcessStartInfo(executable, nodeId.ToString()))
                .Select(Process.Start)
                .ToList();

            Console.ReadLine();

            clusterProcesses.ForEach(process => process.Kill());
        }
    }
}
