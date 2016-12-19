//------------------------------------------------------------------------------
// <copyright file="ToolWindow1Control.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace TruffleVSIX
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using System.ComponentModel;
    using System.Text;
    using System.Diagnostics;

    /// <summary>
    /// Interaction logic for ToolWindow1Control.
    /// </summary>
    public partial class ToolWindow1Control : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindow1Control"/> class.
        /// </summary>
        public ToolWindow1Control()
        {
            this.InitializeComponent();

            //System.Timers.Timer timer = new System.Timers.Timer(100);

            // Hook up the Elapsed event for the timer.
            //timer.Elapsed += OnTimedEvent;

            //timer.Enabled = true;


            BackgroundWorker worker = new BackgroundWorker();

            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);

            worker.RunWorkerAsync("8.8.8.8");
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            //StringBuilder result = new StringBuilder();
            //Process process = new Process();
            //process.StartInfo.FileName = "ping";
            //process.StartInfo.Arguments = (string)e.Argument;
            //process.StartInfo.UseShellExecute = false;
            //process.StartInfo.RedirectStandardOutput = true;
            //process.StartInfo.CreateNoWindow = true;
            //process.Start();
            //result.Append(process.StandardOutput.ReadToEnd());
            //process.WaitForExit();
            //e.Result = result.AppendLine().ToString();

            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/C ping 8.8.8.8";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.OutputDataReceived += (s, args) => this.addText(args.Data + "\r\n");
            process.Start();
            process.BeginOutputReadLine();
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string str = "";

            if (e.Result != null) str = e.Result.ToString();
            else if (e.Error != null) str = e.Error.ToString();
            else if (e.Cancelled) str = "User cancelled process";

            this.addText(str);
        }

        private void addText(string str)
        {
            if (!Dispatcher.CheckAccess())
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        this.textBox.AppendText(str);

                        // Magic number 20; just so users can attach it to the bottom again.
                        if (this.scrollViewer.VerticalOffset > this.scrollViewer.ScrollableHeight - 20)
                        {
                            this.scrollViewer.ScrollToBottom();
                        }
                    }, System.Windows.Threading.DispatcherPriority.Input);
                }
                catch
                {
                    // This would fire when VS is closed. Not sure what to do here.
                }
            }
        }


        private void OnTimedEvent(object sender, System.Timers.ElapsedEventArgs args)
        {
           // string str = "Bacon ipsum dolor amet swine alcatra venison bacon shank shankle pastrami rump jerky ball tip short loin kielbasa filet mignon pork chop.\r\n";

            
        }
    }

}