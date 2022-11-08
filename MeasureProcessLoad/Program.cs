using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace MeasureProcessLoad
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Process[] processlist = Process.GetProcesses();

            uint measurements = 0;
            uint secondstomeasure = 10; //How long should we measure

            uint intervallmillies = 100;
            uint multiplicator = 1000 / intervallmillies;

            uint maxmeasures = secondstomeasure * multiplicator;

            while (measurements < maxmeasures)
            {


                DateTime now = DateTime.Now;
                string timestamp = now.Day + "." + now.Month + "." + now.Year + " " + now.Hour + ":" + now.Minute + ":" + now.Second + ":" + now.Millisecond;
                foreach (Process theprocess in processlist)
                {
                    theprocess.Refresh();

                    /*
                    PerformanceCounter process_cpu = new PerformanceCounter("Process", "% Processor Time", theprocess.ProcessName, true);
                    float processcpuUsage = process_cpu.NextValue() / Environment.ProcessorCount;
                    */

                    /*
                    PerformanceCounter process_mem = new PerformanceCounter("Process", "Working Set", theprocess.ProcessName, true);
                    float processmemUsage = process_mem.NextValue();
                    */

                    // string cpuload = processcpuUsage.ToString()+"%";
                    //string memload = processmemUsage.ToString() + "%";
                    try
                    {
                        Console.WriteLine("{0};{1};{2};{3};{4};{5}", timestamp, theprocess.ProcessName, theprocess.Id, theprocess.TotalProcessorTime, theprocess.WorkingSet64, theprocess.HandleCount, theprocess.StartTime);
                    }
                    catch (Exception ex)
                    {

                    }
                }

                Thread.Sleep((int)intervallmillies);
                measurements++;
            }
        }
    }
}
