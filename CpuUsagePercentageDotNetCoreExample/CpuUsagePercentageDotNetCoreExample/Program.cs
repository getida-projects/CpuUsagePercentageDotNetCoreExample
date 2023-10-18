using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CpuUsagePercentageDotNetCoreExample
{
    internal class CpuUsageTime
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    class Program
    {
        public static async Task Main(string[] args)
        {
            //var task = Task.Run(() => ConsumeCPU(50));

            try
            {
                Process[] processes = Process.GetProcesses()
                    .Where(p => p.ProcessName != "Idle" && !p.HasExited)
                    .ToArray();

                //Console.WriteLine($"Currently {processes.Length} processes is active");

                double cpuUsage = await GetCpuUsage(processes);

                Console.WriteLine(cpuUsage);


                while (true)
                {
                    await Task.Delay(2000);

                    processes = Process.GetProcesses()
                        .Where(p => p.ProcessName != "Idle" && !p.HasExited)
                        .ToArray();

                    //Console.WriteLine($"Currently {processes.Length} processes is active");

                    var newCpuUsage = await GetCpuUsage(processes);
                    cpuUsage = (cpuUsage + newCpuUsage) / 2;

                    Console.WriteLine(cpuUsage);
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.ToString());
            }


        }

        public static void ConsumeCPU(int percentage)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            while (true)
            {
                if (watch.ElapsedMilliseconds > percentage)
                {
                    Thread.Sleep(100 - percentage);
                    watch.Reset();
                    watch.Start();
                }
            }
        }

        private static async Task<double> GetCpuUsage(Process[] processes)
        {
            Dictionary<Process, CpuUsageTime> cpuUsageMap = new(); 

            var startTime = DateTime.UtcNow;

            foreach (Process process in processes)
            {
                if (process.HasExited)
                {
                    Console.WriteLine($"Process {process.ProcessName} (ID: {process.Id} has exited.");
                    continue;
                }

                cpuUsageMap[process] = new CpuUsageTime
                {
                    StartTime = process.TotalProcessorTime
                };
            }

            var survivedProcesses = cpuUsageMap.Keys.ToArray();

            await Task.Delay(500);

            var endTime = DateTime.UtcNow;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds * Environment.ProcessorCount;

            foreach (Process process in survivedProcesses)
            {
                if (process.HasExited)
                {
                    cpuUsageMap.Remove(process);

                    Console.WriteLine($"Process {process.ProcessName} (ID: {process.Id} has exited.");

                    continue;
                }

                cpuUsageMap[process].EndTime = process.TotalProcessorTime;
            }

            TimeSpan cpuTotalUsage = new();

            foreach (Process process in cpuUsageMap.Keys)
            {
                CpuUsageTime cpuUsageTime = cpuUsageMap[process];

                cpuTotalUsage += cpuUsageTime.EndTime - cpuUsageTime.StartTime;
            }


            return cpuTotalUsage.TotalMilliseconds /totalMsPassed * 100;
        }
    }
}
