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
        ConcurrentDictionary<int, Process> processes = new ConcurrentDictionary<int, Process>(Process.GetProcesses().ToDictionary(x => x.Id));

        SystemWatcher wathcer = new SystemWatcher();

        GridViewColumnHeader lastHeaderClicked = null;
        ListSortDirection lastDirection = ListSortDirection.Ascending;

        public MainWindow()
        {
            InitializeComponent();
            //Process start/stop events subscribe
            wathcer.ProcessStart += this.ProcessStart;
            wathcer.ProcessStop += this.ProcessStop;
            wathcer.PreformanceUpdate += PreformanceUpdate;
            wathcer.Run();
            this.ProcessListView.ItemsSource = processes.Values;
        }

        private void PreformanceUpdate(object sender, PerformanceEventArgs e)
        {
            Dispatcher.Invoke(() => {
                this.pbCPU.Value = (int)e.CPU_Usage;
                this.pbRAM.Value = (int)(e.RAM_Usage);
                this.pbDisc.Value = (int)e.DISC_Usage;
                this.DiskInfo.Text = string.Format(
                    "Read (mb/s): {0:F2}\n" +
                    "Write (mb/s): {1:F2}",
                    (e.DISC_ReadBytes / 1024f) / 1024f,
                    (e.DISC_WriteBytes / 1024f) / 1024f);
                this.RamInfo.Text = string.Format(
                    "Available(GB): {0:F2}\n" +
                    "Total(GB): {1:F2}",
                    ((e.RAM_Available / 1024f)/1024f)/1024f,
                    ((e.RAM_Total / 1024f)/1024f)/1024f);
                this.CpuInfo.Text = string.Format(
                    "Process Count: {0}\n" +
                    "Thread Count: {1:F0}",
                    this.processes.Count,
                    e.CPU_ThradCount);
                this.cpuGradient.Offset = 2.0 - (this.pbCPU.Value / 100);
                this.ramGradient.Offset = 2.0 - (this.pbRAM.Value / 100);
                this.diskGradient.Offset = 2.0 - (this.pbDisc.Value / 100);

     
            });
        }

        void ProcessStart(object sender, ProcessEventArgs e)
        {
            bool gettedProcess = true;
            try
            {
                Process newProcess = Process.GetProcessById(e.ProcessID);
                bool added = false;
                while (!added)
                    added = this.processes.TryAdd(newProcess.Id, newProcess);
            }
            catch (Exception)
            {
                gettedProcess = false;
            }
            if (gettedProcess && !Dispatcher.CheckAccess())
                Dispatcher.Invoke(this.UpdateProcessList);
        }

        void ProcessStop(object sender, ProcessEventArgs e)
        {
            if (processes.ContainsKey(e.ProcessID))
            {
                Process proc = null;
                while (proc == null)
                    processes.TryRemove(e.ProcessID, out proc);
            }
            if (!Dispatcher.CheckAccess())
                Dispatcher.Invoke(this.UpdateProcessList);
        }

        void UpdateProcessList()
        {
            this.ProcessListView.ItemsSource = processes.Values;
            if(this.lastHeaderClicked != null)
                this.Sort(this.lastHeaderClicked.Content.ToString(), lastDirection);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.wathcer.Dispose();
        }

        private void Kill_Process(object sender, RoutedEventArgs e)
        {
            Process proc = this.ProcessListView.SelectedItem as Process;
            if(proc != null)
                proc.Kill(); 
        }

        private void ProcessListView_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;
            if (headerClicked != null && headerClicked.Column != null)
            {
                if (headerClicked != this.lastHeaderClicked)
                    direction = ListSortDirection.Ascending;
                else
                {
                    if (this.lastDirection == ListSortDirection.Ascending)
                        direction = ListSortDirection.Descending;
                    else
                        direction = ListSortDirection.Ascending;
                }
                string header = headerClicked.Column.Header as string;
                this.Sort(header, direction);
                this.lastHeaderClicked = headerClicked;
                this.lastDirection = direction;
            }
        }
        private void Sort(string sortBy, ListSortDirection direction)
        {
            this.ProcessListView.Items.SortDescriptions.Clear();       
            SortDescription sd = new SortDescription(sortBy, direction);
            this.ProcessListView.Items.SortDescriptions.Add(sd);
            this.ProcessListView.Items.Refresh();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process selectedProcess = this.ProcessListView.SelectedItem as Process;
            
            if (selectedProcess != null && !selectedProcess.HasExited)
            {
                MenuItem item = sender as MenuItem;
                if (item != null)
                {
                    switch (item.Header.ToString())
                    {
                        case "Idle":
                            selectedProcess.PriorityClass = ProcessPriorityClass.Idle;
                            break;
                        case "Normal":
                            selectedProcess.PriorityClass = ProcessPriorityClass.Normal;
                            break;
                        case "High":
                            selectedProcess.PriorityClass = ProcessPriorityClass.High;
                            break;
                        case "RealTime":
                            selectedProcess.PriorityClass = ProcessPriorityClass.RealTime;
                            break;
                    }
                }
            }
            this.UpdateProcessList();            
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            Button btn = sender as Button;
            Brush backGround = btn.Foreground;
            Brush foreGround = btn.Background;
            btn.Foreground = foreGround;
            btn.Background = backGround;         
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            Button btn = sender as Button;
            Brush backGround = btn.Foreground;
            Brush foreGround = btn.Background;
            btn.Foreground = foreGround;
            btn.Background = backGround;
        }
    }
}
