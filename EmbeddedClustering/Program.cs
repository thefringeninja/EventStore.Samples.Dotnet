using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                var node = GetEventStoreNode.Create(
                    int.Parse(args[0]), 3, () => Console.WriteLine("I am master"), () => Console.WriteLine("I ain't"));

                node.Start();

                Console.ReadLine();
            }

            return 0;
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
