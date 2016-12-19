using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;

namespace TruffleVSIX.Helpers
{
    class ProcessRunner
    {
        public delegate void LineHandler(string line);
        public event LineHandler OnLine;

        private BackgroundWorker worker;
        private LineHandler handler;

        public ProcessRunner()
        {
            this.worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
        }

        public void run(string command)
        {
            worker.RunWorkerAsync(command);
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/C " + (string)e.Argument;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.OutputDataReceived += (s, args) => {
                this.OnLine(args.Data + "\r\n");
            };
            process.Start();
            process.BeginOutputReadLine();
        }




    }
}
