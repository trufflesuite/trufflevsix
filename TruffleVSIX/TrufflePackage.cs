//------------------------------------------------------------------------------
// <copyright file="Command1Package.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using EnvDTE;
using System.IO;
using TruffleVSIX.Helpers;
using System.Text.RegularExpressions;

namespace TruffleVSIX
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#1110", "#1112", "1.0", IconResourceID = 1400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(TrufflePackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids.SolutionHasSingleProject)] // Not sure which one to use, this one seems faster than SolutionExists
    public sealed class TrufflePackage : Package
    {
        /// <summary>
        /// Command1Package GUID string.
        /// </summary>
        public const string PackageGuidString = "8782a030-f21c-4cd9-9588-a6127bb3414c";

        /// <summary>
        /// Initializes a new instance of the <see cref="TruffleMenu"/> class.
        /// </summary>
        public TrufflePackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        private DTE _dte;
        private SolutionEvents _solutionEvents;
        public bool InSolution { get; private set; }
        public bool TruffleInstalled { get; private set; }
        public bool TestRPCInstalled { get; private set; }
        public bool NPMInstalled { get; private set; }
        public string SolutionPath { get; private set; }
        public string ProjectPath { get; private set; }
        public string TrufflePath { get; private set; }
        public bool TruffleProjectInitialized { get; private set; }

        public OutputPane TrufflePane { get; private set; }
        public OutputPane TestRPCPane { get; private set; }

        public delegate void OpenHandler();
        public event OpenHandler OnOpen;

        public delegate void CloseHandler();
        public event CloseHandler OnClose;


        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            TruffleMenu.Initialize(this);
            base.Initialize();

            this.TrufflePane = new OutputPane(this, "Truffle");
            this.TestRPCPane = new OutputPane(this, "TestRPC");

            // Documentation says this is Microsoft Interal Use Only.
            // How else do we determine the solution that's opened?
            this._dte = (DTE)this.GetService(typeof(DTE));
            this._solutionEvents = ((Events)this._dte.Events).SolutionEvents;
            this._solutionEvents.Opened += SolutionOpened;
            this._solutionEvents.AfterClosing += SolutionClosed;

            TestRPCPane.runner.OnStart += () =>
            {
                this.RecheckEnvironment();
            };

            // Set all variables as not in a solution initially.
            SolutionClosed();
        }

        // This is hacky.
        public void RecheckEnvironment()
        {
            this.SolutionOpened();
        }

        private void SolutionOpened()
        {
            string solutionPath;
            Projects projects;

            try
            {
                DTE dte = (DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE));
                projects = dte.Solution.Projects;
                solutionPath = Path.GetDirectoryName(dte.Solution.FullName);
            } catch
            {
                // Nothing we can do here if this errors. Don't go on.
                return;
            }

            this.InSolution = true;
            this.SolutionPath = solutionPath;

            // Use the first Truffle project we find
            // TODO: Somehow support multiple projects
            foreach (Project project in projects)
            {
                string projectPath = Path.GetDirectoryName(project.FullName);

                // If no project path is specified, let's at least set the first one.
                if (this.ProjectPath == null)
                {
                    this.ProjectPath = projectPath;
                }

                if (TruffleENV.CheckTruffleInstalled(projectPath) == true)
                {
                    this.ProjectPath = projectPath;
                    this.TruffleInstalled = true;
                    this.TrufflePath = TruffleENV.ExpectedTruffleBinary(projectPath);
                    break;
                }
            }

            this.TestRPCInstalled = TruffleENV.CheckTestRPCInstalled(this.ProjectPath);
            this.TruffleProjectInitialized = TruffleENV.CheckTruffleProjectInitialized(this.ProjectPath);

            this.OnOpen();
        }

        private void SolutionClosed()
        {
            this.InSolution = false;
            this.TruffleInstalled = false;
            this.NPMInstalled = false;
            this.SolutionPath = null;
            this.ProjectPath = null;
            this.TrufflePath = null;

            TrufflePane.Kill();
            TestRPCPane.Kill();

            this.OnClose();
        }

        public object GetServicePublic(Type serviceType)
        {
            return this.GetService(serviceType);
        }

        public bool CheckTestRPCRunning()
        {
            return TestRPCPane.IsRunning();
        }

        #endregion
    }
}
