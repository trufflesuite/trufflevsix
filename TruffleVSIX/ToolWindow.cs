//------------------------------------------------------------------------------
// <copyright file="ToolWindow1.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace TruffleVSIX
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;
    using Helpers;

    using EnvDTE;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("37dab6b9-de4f-4ab1-80c2-a5d1ee7b93e6")]
    public class ToolWindow : ToolWindowPane
    {
        ProcessRunner runner;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindow"/> class.
        /// </summary>
        public ToolWindow() : base(null)
        {
            this.Caption = "Truffle";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new ToolWindowControl();
      
            this.runner = new ProcessRunner();

            this.runner.OnLine += (line) =>
            {
                ToolWindowControl control = (ToolWindowControl)this.Content;
                control.AddText(line);
            };
        }

        public void RunCommand(string command)
        {
            ToolWindowControl control = (ToolWindowControl)this.Content;
            control.ClearText();

            //control.AddText(command);
            this.runner.Run(command);
        }

        public void RunTruffleCommand(string command)
        {
            TrufflePackage trufflePackage = (TrufflePackage)this.Package;
            this.RunCommand(trufflePackage.TrufflePath + " " + command + " --working-directory " + trufflePackage.ProjectPath);
        }

    }
}
