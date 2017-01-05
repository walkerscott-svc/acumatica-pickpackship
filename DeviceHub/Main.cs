﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Acumatica.DeviceHub
{
    public partial class Main : Form
    {
        private List<Task> _tasks;
        private CancellationTokenSource _cancellationTokenSource;

        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(Properties.Settings.Default.AcumaticaUrl))
            {
                using (var form = new Configuration())
                {
                    if(form.ShowDialog() != DialogResult.OK)
                    {
                        Application.Exit();
                        return;
                    }
                }
            }
            
            StartMonitors();
        }
        
        private void StartMonitors()
        {
            WriteToLog("Starting monitoring tasks...");
            _cancellationTokenSource = new CancellationTokenSource();
            _tasks = new List<Task>();

            var monitorTypes = new Type[] { typeof(PrintJobMonitor), typeof(ScaleMonitor) };
            foreach (Type t in monitorTypes)
            {
                IMonitor monitor = (IMonitor) Activator.CreateInstance(t);
                Task task = monitor.Initialize(new Progress<string>(p => WriteToLog(p)), _cancellationTokenSource.Token);
                if(task != null)
                {
                    _tasks.Add(task);
                    WriteToLog(String.Format("{0} started.", t.Name));
                }
            }
        }

        private void StopMonitors()
        {
            if (_tasks == null) return;

            WriteToLog("Stopping monitoring tasks...");
            _cancellationTokenSource.Cancel();
            Task.WaitAll(_tasks.ToArray());
            _tasks = null;
            _cancellationTokenSource = null;
            WriteToLog("All the monitoring tasks have been stopped.");

        }

        private void WriteToLog(string message)
        {
            System.Diagnostics.Trace.WriteLine(message);
            logListBox.Items.Insert(0, (object)DateTime.Now.ToString() + " - " + message);
            if (logListBox.Items.Count > 100) logListBox.Items.RemoveAt(100);
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            //TODO: Update tray icon from blue to red based on connection status
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
            }
            else
            { 
                StopMonitors();
            }
        }

        private void notifyIcon_Click(object sender, EventArgs e)
        {
            this.ShowInTaskbar = true;
            this.WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
        }

        private void Main_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                notifyIcon.Visible = true;

                notifyIcon.ShowBalloonTip(1000, this.Text, "The application is still running and monitoring your devices. To access it, click the icon in the tray.", ToolTipIcon.Info);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void configureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            timer.Enabled = false;
            StopMonitors();

            using (var form = new Configuration())
            {
                form.ShowDialog();
                StartMonitors();
            }
        }
    }
}