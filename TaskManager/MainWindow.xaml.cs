using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Management;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
using System.ComponentModel;

using System.Collections.ObjectModel;

namespace TaskManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        ConcurrentDictionary<int, Process> dic = new ConcurrentDictionary<int, Process>(Process.GetProcesses().ToDictionary(x => x.Id));

        SystemWatcher wathcer = new SystemWatcher();

        public MainWindow()
        {
            InitializeComponent();
            //Process start/stop events subscribe  
            wathcer.ProcessStart    += this.ProcessStart;
            wathcer.ProcessStop     += this.ProcessStop;
            this.ProcessListView.ItemsSource = dic.Values;
        }

        void ProcessStart(object sender, ProcessEventArgs e)
        {
            bool gettedProcess = true;
            try
            {
                Process newProcess = Process.GetProcessById(e.ProcessID);
                bool added = false;
                while(!added)
                    added = this.dic.TryAdd(newProcess.Id, newProcess);              
            }
            catch(Exception)
            {
                gettedProcess = false;
            }
            if (gettedProcess && !Dispatcher.CheckAccess())
                Dispatcher.Invoke(this.UpdateProcesses);
        }

        void ProcessStop(object sender, ProcessEventArgs e)
        {           
            if(dic.ContainsKey(e.ProcessID))
            {
                Process proc = null;
                while (proc == null)
                    dic.TryRemove(e.ProcessID, out proc);
            }           
            if (!Dispatcher.CheckAccess())
                Dispatcher.Invoke(this.UpdateProcesses);
        }
       
        void UpdateProcesses()
        {
            this.ProcessListView.ItemsSource = dic.Values;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.wathcer.Dispose();
        }
    }
}
