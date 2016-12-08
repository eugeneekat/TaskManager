using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Windows;
using Microsoft.VisualBasic.Devices;

namespace TaskManager
{
    public class ProcessEventArgs
    {
        public int ProcessID { get; set; }
        public string ProcessName { get; set; }
    }

    public class PerformanceEventArgs
    {
        public float CPU_Usage { get; set; }
        public float RAM_Usage { get; set;}
        public float DISC_Usage { get; set; }
        public ulong RAM_Available { get; set; }
        public ulong RAM_Total { get; set; }
        public float DISC_ReadBytes { get; set; }
        public float DISC_WriteBytes { get; set; }
        public float CPU_ThradCount { get; set; }
    }


    class SystemWatcher : IDisposable
    {
        bool disposed = false;

        string machine = string.Format("\\\\{0}\\root\\cimv2", Environment.MachineName);

        ManagementEventWatcher processStartWathcher = null;
        ManagementEventWatcher processStopWathcher = null;

        PerformanceCounter cpuTime  = null;

        PerformanceCounter diskTime = null;
        PerformanceCounter diskReadBytes = null;
        PerformanceCounter diskWriteBytes = null;
        PerformanceCounter threadCount = null;

        ComputerInfo computerInfo = new ComputerInfo();

        System.Timers.Timer timer = new System.Timers.Timer(2000);
        
        //High level - eventhandler delegate type
        public delegate void ProcessEventHandler (object sender, ProcessEventArgs e);
        public delegate void PreformanceEventHandler(object sender, PerformanceEventArgs e);
        
        //High level - process start/stop events
        public event ProcessEventHandler ProcessStart = null;
        public event ProcessEventHandler ProcessStop = null;
        public event PreformanceEventHandler PreformanceUpdate = null;

        public SystemWatcher()
        {
            this.processStartWathcher                   = new ManagementEventWatcher(machine, "SELECT * FROM Win32_ProcessStartTrace");
            this.processStopWathcher                    = new ManagementEventWatcher(machine, "SELECT * FROM Win32_ProcessStopTrace");
            this.processStartWathcher.EventArrived      += new EventArrivedEventHandler(processStartEvent_EventArrived);
            this.processStopWathcher.EventArrived       += new EventArrivedEventHandler(processStopEvent_EventArrived);
            this.timer.Elapsed                          += Timer_Elapsed;
            this.cpuTime    = new PerformanceCounter   { CategoryName = "Processor", CounterName = "% Processor Time", InstanceName = "_Total" };
            this.diskTime   = new PerformanceCounter  { CategoryName = "PhysicalDisk", CounterName = "% Disk Time", InstanceName = "_Total" };
            this.diskReadBytes = new PerformanceCounter { CategoryName = "PhysicalDisk", CounterName = "Disk Read Bytes/sec", InstanceName = "_Total" };
            this.diskWriteBytes = new PerformanceCounter { CategoryName = "PhysicalDisk", CounterName = "Disk Write Bytes/sec", InstanceName = "_Total" };
            this.threadCount = new PerformanceCounter { CategoryName = "Process", CounterName = "Thread Count", InstanceName = "_Total" };
        }



        //Run Watcher
        public void Run()
        {
            this.processStartWathcher.Start();
            this.processStopWathcher.Start();
            this.timer.Start();
        }

        //Low level process start/stop event handlers
        void processStartEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            //Get processID and pass to High level subscribers
            ProcessStart?.Invoke(this, new ProcessEventArgs
            {
                ProcessID = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value),
                ProcessName = e.NewEvent.Properties["ProcessName"].Value.ToString()
            });
        }
        void processStopEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            ProcessStop?.Invoke(this, new ProcessEventArgs
            {
                ProcessID   = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value),
                ProcessName = e.NewEvent.Properties["ProcessName"].Value.ToString()
            });
        }
        //Timer event
        void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.PreformanceUpdate?.Invoke(this, new PerformanceEventArgs
            {
                CPU_Usage           = this.cpuTime.NextValue(),
                DISC_Usage          = this.diskTime.NextValue(),
                RAM_Usage           = ((this.computerInfo.TotalPhysicalMemory - this.computerInfo.AvailablePhysicalMemory) * 100) / this.computerInfo.TotalPhysicalMemory,
                RAM_Available       = this.computerInfo.AvailablePhysicalMemory,
                RAM_Total           = this.computerInfo.TotalPhysicalMemory,
                DISC_ReadBytes      = this.diskReadBytes.NextValue(),
                DISC_WriteBytes     = this.diskWriteBytes.NextValue(),
                CPU_ThradCount      = this.threadCount.NextValue()
            });
        }


        #region IDispasable
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if(!disposed)
            {
                if(disposing)
                {
                    this.processStartWathcher.Dispose();
                    this.processStopWathcher.Dispose();
                    this.timer.Dispose();
                }
                disposed = true;
            }
        }
        ~SystemWatcher()
        {
            this.Dispose(false);
        }
        #endregion
    }
}
