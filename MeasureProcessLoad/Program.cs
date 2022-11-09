using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using Co0nUtilZ;
using Co0nUtilZ.Base_Classes;
using System.IO;

namespace MeasureProcessLoad
{
    internal class Program
    {        

        static void printError(Exception ex, string processname)
        {   
            Console.WriteLine("");
            Console.WriteLine("Fehler beim Verarbeiten der Attribute von Prozess: " + processname);
            Console.WriteLine("Details:");
            Console.WriteLine(ex.Message);
            Console.WriteLine("");
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine("");            
        }

        
        static void Main(string[] args)
        {
            int secondstomeasure = 300; //How long should we measure
            string resultfile = @"C:\temp\processload_logger_"+DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")+".csv";

            // Parse arguments
            bool debug = false;
            if (args.Contains("debug"))
            {
                debug = true;
            }

            int argc = 0;
            foreach (string arg in args)
            {   
                // Time of measurements
                if ( (arg.Equals("-runtime") || arg.Equals("/runtime")) && null!=args[argc+1])  {
                    if (!int.TryParse(args[argc + 1], out secondstomeasure))
                    {
                        //parsing was unsuccesful.
                        secondstomeasure = 240; //set to another value
                    }                    
                }
             
                // Resultfile
                if ((arg.Equals("-resultfile") || arg.Equals("/resultfile")) && null != args[argc + 1])
                {
                    resultfile = args[argc + 1];
                }
                argc++;
            }

            DateTime start = DateTime.Now;
            C_WinSessionHelperRDP sh = new C_WinSessionHelperRDP();
            Console.WriteLine("MeasureProcessLoad (v0.1 2022/11) - D. Maienhöfer, KLG-IT");
            Console.WriteLine("Dieses Programm protokolliert, welche Systemauslastung durch die einzelnen Prozesse verursacht wird um Verzögerungen im Anmeldeprozess zu ermitteln.");
            Console.WriteLine("Bitte schließen Sie dieses Fenster nicht. Bei Rückfragen wenden Sie sich bitte an die IT-Abteilung.");
            Console.WriteLine("");

            Console.WriteLine("Started watching for processes at "+ start.ToString("dd.MM.yyyy HH:mm:ss") + ". Maximum runtime is: "+secondstomeasure.ToString()+" seconds...");

            string output = "";

            string msg = "Zeitpunkt;Prozessname;Prozess-ID;Benutzerkontext;CPU-Load;Virtueller Speichersatz (KiB);Arbeitssatz (KiB);Non-Paged Speicher (KiB);Paged Speicher (KiB);Offene Handles;Threads;Startzeitpunkt;Arguments";            
            output = msg + "\r\n";

            PerformanceCounter process_cpu = new PerformanceCounter("Process", "% Processor Time", true);
            DateTime now = DateTime.Now;

            while ((int)Math.Floor(((TimeSpan)(now - start)).TotalSeconds) < secondstomeasure)
            {
                now = DateTime.Now;
                string timestamp = now.ToString("dd.MM.yyyy HH:mm:ss");

                //runtime = (int)Math.Floor(((TimeSpan)(now - start)).TotalSeconds);

                Process[] processlist = Process.GetProcesses(); //Get a fresh list of all processes.
                Console.WriteLine("Current Processcount: " + processlist.Length.ToString());

                foreach (Process theprocess in processlist)
                {
                    string actualprocnamefordebugging = theprocess.ProcessName;
                    theprocess.Refresh();
                    float cpuusageperfcount = 0.0f;
                    float processcpuUsagePercent = 0.0f;

                    try
                    {
                        if (theprocess.HasExited)
                        {
                            continue; //Continue with the next process if this one has exited already, to avoid crashing and errors.
                        }
                    }
                    catch (Exception ex)
                    {
                        if (debug)
                        {
                            printError(ex, actualprocnamefordebugging);
                        }
                        continue; //continue as well, as we're unable to do anything with the process
                    }

                    try {
                        process_cpu.InstanceName = actualprocnamefordebugging; //Set the Instance of the performance-counter to the current process

                        process_cpu.NextValue();
                        cpuusageperfcount = process_cpu.NextValue(); //Needs two be called twice to acutally deliver a valid value                        
                        processcpuUsagePercent = cpuusageperfcount / Environment.ProcessorCount;

                    }
                    catch (Exception ex)
                    {
                        if (debug)
                        {
                            printError(ex, actualprocnamefordebugging);
                        }
                    }
                    try
                    {
                        // Find the username of current processe's session
                        string username = "";                        
                        foreach (SessionInfo s in sh.getAllSessions())
                        {
                            if (theprocess.SessionId == s.SessionId)
                            {
                                username = s.UserName;                                
                                break;
                            }
                        }
                        msg = 
                            timestamp +
                            ";"+ theprocess.ProcessName +
                            ";"+ theprocess.Id +
                            ";"+ username +
                            ";"+ processcpuUsagePercent + 
                            ";"+ (Math.Round(((double)theprocess.VirtualMemorySize64 /1024),2)).ToString() +
                            ";"+ (Math.Round(((double)theprocess.WorkingSet64 / 1024), 2)).ToString() +
                            ";"+ (Math.Round(((double)theprocess.NonpagedSystemMemorySize64 / 1024), 2)).ToString() + 
                            ";"+ (Math.Round(((double)theprocess.PagedMemorySize64 / 1024), 2)).ToString() + 
                            ";"+ theprocess.HandleCount +
                            ";"+ theprocess.Threads.Count +
                            ";"+ theprocess.StartTime +
                            ";"+ theprocess.StartInfo.Arguments;
                        
                        output += msg + "\r\n";

                    }                    
                    catch (Exception gEX)
                    {
                        if (debug)
                        {
                            printError(gEX, actualprocnamefordebugging);
                        }
                    }
                }
                Thread.Sleep(250); //insert a little pause to ensure we're not eating up all processor ressources. This also limits our sampling rate
            }
            process_cpu.Close();
            

            try
            {
                StreamWriter ofs = new StreamWriter(resultfile);                
                ofs.Write(output);
                ofs.Close();
                Console.WriteLine("Results had been written to \""+resultfile+"\"");                
            }
            catch (Exception ex)
            {
                if (debug)
                {
                    Console.Write(output);
                    Console.WriteLine("Unable to write results to file \"" + resultfile + "\"");
                    Console.WriteLine("");
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}
