using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;

namespace TruffleVSIX.Helpers
{
    public class ProcessRunner
    {
        public delegate void StartHandler();
        public delegate void LineHandler(string line);
        public delegate void ErrorHandler(Exception e);
        public delegate void ExitHandler();

        public event StartHandler OnStart;
        public event LineHandler OnLine;
        public event ErrorHandler OnError;
        public event ExitHandler OnExit;

        private BackgroundWorker worker;

        public List<ExitHandler> doneCallbacks = new List<ExitHandler>();

        private Semaphore processLock;

        private Process process;

        public ProcessRunner()
        {
            this.processLock = new Semaphore(1,1);
        }

        public void Run(string command, ExitHandler done = null)
        {
            if (IsRunning() == true)
            {
                Kill();
            }

            if (done != null)
            {
                doneCallbacks.Add(done);
            }

            this.worker = new BackgroundWorker();
            this.worker.WorkerSupportsCancellation = true;
            this.worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            this.worker.RunWorkerAsync(command);            
        }

        public bool IsRunning()
        {
           
            if (process == null) return false;

            int processId;

            try
            {
                processId = process.Id;
            } catch (InvalidOperationException e)
            {
                // No process associated. Not running.
                return false;
            }

            return IsProcessOrChildrenRunning(processId);
        }

        public void Kill()
        {
            if (this.process != null)
            {
                this.process.OutputDataReceived -= this.process_OutputDataReceived;
                this.KillProcessAndChildren(this.process.Id);
                this.process_Exited(this, new EventArgs());
            }

            try
            {
                if (this.worker != null)
                {
                    this.worker.CancelAsync();
                }
            } catch (InvalidOperationException e)
            {
                // Already canceled? 
            }
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            //processLock.WaitOne();

            process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/C " + (string)e.Argument;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
            process.EnableRaisingEvents = true;
            //process.OutputDataReceived += this.process_OutputDataReceived;
            //process.ErrorDataReceived += this.process_OutputDataReceived;
            //process.Exited += process_Exited;
            process.Start();
            // process.BeginOutputReadLine();
            //process.BeginErrorReadLine();

            // Read stderr synchronously (on another thread)
            this.OnStart?.Invoke();

            string errorText = null;
            var stderrThread = new Thread(() =>
            {

                // Read stdout synchronously (on this thread)

                while (true)
                {
                    var line = process.StandardError.ReadLine();
                    if (line == null)
                        break;

                    line += Environment.NewLine;
                    this.OnLine?.Invoke(line);
                }

                errorText = process.StandardError.ReadToEnd();
            });
            stderrThread.Start();

            // Read stdout synchronously (on this thread)

            while (true)
            {
                var line = process.StandardOutput.ReadLine();
                if (line == null)
                    break;

                line += Environment.NewLine;
                this.OnLine?.Invoke(line);
            }

            process.WaitForExit();

            stderrThread.Join();

            process_Exited(this, new EventArgs());
        }

        private void process_OutputDataReceived(object sender, DataReceivedEventArgs args)
        {
            string line = args.Data;

            if (line != null)
            {
                line += Environment.NewLine;

                this.OnLine?.Invoke(line);
            } else
            {
                //this.process_Exited(sender, args);
            }
            
        }

        private void process_Exited(object sender, EventArgs args)
        {
            List<ExitHandler> _doneCallbacks = doneCallbacks;
            doneCallbacks = new List<ExitHandler>();

            // Fire all the done callbacks.
            foreach (ExitHandler handler in _doneCallbacks)
            {
                handler();
            }

            this.OnExit?.Invoke();
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
        private bool IsProcessOrChildrenRunning(int pid)
        {
            ManagementObjectSearcher processSearcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection processCollection = processSearcher.Get();

            try
            {
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited) return true;
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }

            if (processCollection != null)
            {
                foreach (ManagementObject mo in processCollection)
                {
                    bool isChildRunning = IsProcessOrChildrenRunning(Convert.ToInt32(mo["ProcessID"])); //kill child processes(also kills childrens of childrens etc.)
                    if (isChildRunning == true) return true;
                }
            }

            return false;
        }

    }
}
