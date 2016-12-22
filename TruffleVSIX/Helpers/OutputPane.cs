using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace TruffleVSIX.Helpers
{
    public class OutputPane
    {
        private IVsOutputWindowPane _customPane = null;
        private Guid _customPaneGuid;

        private TrufflePackage package;
        private string name;

        public ProcessRunner runner;

        public delegate void DoneHandler();

        public OutputPane(TrufflePackage package, string name)
        {
            this.package = package;
            this.name = name;

            _customPaneGuid = Guid.NewGuid();

            this.runner = new ProcessRunner();

            this.runner.OnLine += (line) =>
            {
                AddText(line);
            };

            this.runner.OnError += (Exception e) =>
            {
                AddText(e.Message);
            };

            // Can't get this to work; this.after will show up in the output before the rest of the command output has been printed.
            this.runner.OnExit += () =>
            {
                //control.AddText(this.after);
            };
        }

        public IVsOutputWindowPane CustomPane
        {
            get
            {
                if (_customPane == null)
                {
                    IVsOutputWindow outputWindow = this.package.GetServicePublic(typeof(SVsOutputWindow)) as IVsOutputWindow;
                    if (outputWindow != null)
                    {
                        if (_customPaneGuid == Guid.Empty || ErrorHandler.Failed(outputWindow.GetPane(ref _customPaneGuid, out _customPane)) || _customPane == null)
                        {
                            // create a new solution updater pane
                            outputWindow.CreatePane(ref _customPaneGuid, this.name, 1, 1);
                            if (ErrorHandler.Failed(outputWindow.GetPane(ref _customPaneGuid, out _customPane)) || _customPane == null)
                            {
                                // pane could not be created or found
                                throw new Exception("Custom pane could not be created and/or found.");
                            }
                        }
                    }
                }
                if (_customPane != null)
                {
                    _customPane.Activate();
                }
                return _customPane;
            }
        }

        public void RunCommand(string command, DoneHandler done = null)
        {
            //AddText(command + "\r\n");
            this.runner.Run(command, () =>
            {
                done?.Invoke();
            });
        }

        public void RunTruffleCommand(string command, DoneHandler done = null)
        {
            this.RunCommand("\"cd /d " + package.ProjectPath + " && .\\node_modules\\.bin\\truffle.cmd " + command + " --no-colors", done);
        }

        public void RunInProject(string command, DoneHandler done = null)
        {
            this.RunCommand("\"cd /d " + package.ProjectPath + " && " + command + "\"", done);
        }

        public void AddText(string text)
        {
            CustomPane.OutputString(text);
        }

        public void AddLine(string line)
        {
            AddText(line + Environment.NewLine);
        }

        public void Clear()
        {
            CustomPane.Clear();
        }

        public bool IsRunning()
        {
            return this.runner.IsRunning();
        }

        public void Kill()
        {
            this.runner.Kill();
        }

    }
}
