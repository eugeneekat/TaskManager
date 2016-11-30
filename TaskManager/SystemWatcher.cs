using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Windows;

namespace TaskManager
{
    public class ProcessEventArgs
    {
        public int ProcessID { get; private set; }
        public string ProcessName { get; private set; }
        public ProcessEventArgs(int processID, string processName)
        {
            this.ProcessID = processID;
            this.ProcessName = processName;
        } 
    }


    class SystemWatcher : IDisposable
    {
        private bool disposed = false;

        string machine = string.Format("\\\\{0}\\root\\cimv2", Environment.MachineName);

        ManagementEventWatcher processStartWathcher = null;
        ManagementEventWatcher processStopWathcher = null;
        //High level - eventhandler delegate type
        public delegate void ProcessEventHandler (object sender, ProcessEventArgs e);
        //High level - process start/stop events
        public event ProcessEventHandler ProcessStart = null;
        public event ProcessEventHandler ProcessStop = null;

        public SystemWatcher()
        {
            this.processStartWathcher = new ManagementEventWatcher(machine, "SELECT * FROM Win32_ProcessStartTrace");
            this.processStopWathcher = new ManagementEventWatcher(machine, "SELECT * FROM Win32_ProcessStopTrace");
            processStartWathcher.EventArrived += new EventArrivedEventHandler(processStartEvent_EventArrived);
            processStopWathcher.EventArrived += new EventArrivedEventHandler(processStopEvent_EventArrived);
            processStartWathcher.Start();
            processStopWathcher.Start();
        }

       

        //Low level process start/stop event handlers
        void processStartEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            //Get processID and pass to High level subscribers
            int processID = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);
            string processName = e.NewEvent.Properties["ProcessName"].Value.ToString();
            ProcessStart?.Invoke(this, new ProcessEventArgs(processID, processName));
        }
        void processStopEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            int processID = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);
            string processName = e.NewEvent.Properties["ProcessName"].Value.ToString();
            ProcessStop?.Invoke(this, new ProcessEventArgs(processID, processName));
        }

        /*-------------------Dispose--------------------*/
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
                }
                disposed = true;
            }
        }
        ~SystemWatcher()
        {
            this.Dispose(false);
        }
    }
}
