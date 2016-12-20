using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;

namespace TruffleVSIX.Helpers
{
    class ProcessRunner
    {
        public delegate void LineHandler(string line);
        public delegate void ErrorHandler(Exception e);

        public event LineHandler OnLine;
        public event ErrorHandler OnError;

        private BackgroundWorker worker;

        Process process;

        public ProcessRunner()
        {
            this.worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
        }

        public void Run(string command)
        {
            if (IsRunning())
            {
                Kill();
            }

            worker.RunWorkerAsync(command);
        }

        public bool IsRunning()
        {
            return process != null && process.HasExited == false;
        }

        public void Kill()
        {
            if (this.process != null)
            {
                this.process.OutputDataReceived -= this.process_OutputDataReceived;
                this.KillProcessAndChildren(this.process.Id);
            }
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/C " + (string)e.Argument;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.OutputDataReceived += this.process_OutputDataReceived;
                process.Start();
                process.BeginOutputReadLine();
            } catch (Exception error)
            {
                this.OnError(error);
            }
        }

        private void process_OutputDataReceived(object sender, DataReceivedEventArgs args)
        {
            this.OnLine(args.Data + "\r\n");
        }

        private void KillProcessAndChildren(int pid)
        {
            ManagementObjectSearcher processSearcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection processCollection = processSearcher.Get();

            try
            {
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }

            if (processCollection != null)
            {
                foreach (ManagementObject mo in processCollection)
                {
                    KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"])); //kill child processes(also kills childrens of childrens etc.)
                }
            }
        }
    }
}
