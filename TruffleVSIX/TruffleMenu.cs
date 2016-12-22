//------------------------------------------------------------------------------
// <copyright file="Command1.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using TruffleVSIX.Helpers;

namespace TruffleVSIX
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TruffleMenu
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CompileCommandId = 0x0100;
        public const int MigrateCommandId = 0x0110;
        public const int TestCommandId = 0x0120;
        public const int InitializeProjectId = 0x0150;

        public const int InstallTruffleId = 0x0800;
        public const int InstallTestRPCId = 0x0850;

        public const int StartTestRPCId = 0x0855;
        public const int StopTestRPCId = 0x0860;

        public const int AboutCommandId   = 0x0900;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("41aca4e2-e75b-4335-8072-294f3bd9ac04");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly TrufflePackage package;

        private List<OleMenuCommand> allMenuItems = new List<OleMenuCommand>();
        private List<OleMenuCommand> projectOnlyMenuItems = new List<OleMenuCommand>();

        OleMenuCommand compile;
        OleMenuCommand migrate;
        OleMenuCommand test;
        OleMenuCommand initializeProject;
        OleMenuCommand installTruffle;
        OleMenuCommand installTestRPC;
        OleMenuCommand startTestRPC;
        OleMenuCommand stopTestRPC;
        OleMenuCommand about;

        /// <summary>
        /// Initializes a new instance of the <see cref="TruffleMenu"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private TruffleMenu(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = (TrufflePackage)package;

            compile = this.createMenuItem(CompileCommandId, this.CompileCallback);
            migrate = this.createMenuItem(MigrateCommandId, this.MigrateCallback);
            test = this.createMenuItem(TestCommandId, this.TestCallback);

            initializeProject = this.createMenuItem(InitializeProjectId, this.InitializeProjectCallback);

            installTruffle = this.createMenuItem(InstallTruffleId, this.InstallTruffleCallback);
            installTestRPC = this.createMenuItem(InstallTestRPCId, this.InstallTestRPCCallback);

            startTestRPC = this.createMenuItem(StartTestRPCId, this.StartTestRPCCallback);
            stopTestRPC = this.createMenuItem(StopTestRPCId, this.StopTestRPCCallback);

            about = this.createMenuItem(AboutCommandId, this.ShowAboutCallback);

            // Add items to project only menus list
            projectOnlyMenuItems.Add(compile);
            projectOnlyMenuItems.Add(migrate);
            projectOnlyMenuItems.Add(test);

            ((TrufflePackage)this.package).OnOpen += HandleProjectEnvironmentChange;
            ((TrufflePackage)this.package).OnClose += HandleProjectEnvironmentChange;
        }

        public OleMenuCommand createMenuItem(int commandId, EventHandler handler)
        {
            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, commandId);
                var menuItem = new OleMenuCommand(handler, menuCommandID);
                //menuItem.BeforeQueryStatus += (object sender, EventArgs e) =>
                //{
                //    HandleProjectEnvironmentChange();
                //};
                commandService.AddCommand(menuItem);
                allMenuItems.Add(menuItem);
                return menuItem;
            }

            return null;
        }

        private void HandleProjectEnvironmentChange()
        {
            TrufflePackage package = ((TrufflePackage)this.package);

            // If we're not in a solution, hide everything.
            if (package.InSolution == false)
            {
                allMenuItems.ForEach(delegate (OleMenuCommand menuItem)
                {
                    menuItem.Visible = false;
                });

                return;
            }

            // We're in a solution. Now check environment related items.
            allMenuItems.ForEach(delegate (OleMenuCommand menuItem)
            {
                bool isProjectMenuItem = projectOnlyMenuItems.Contains(menuItem);

                if (menuItem == installTruffle)
                {
                    menuItem.Visible = !package.TruffleInstalled;
                }
                else if (menuItem == installTestRPC)
                {
                    // menuItem.Visible = !package.TestRPCInstalled;
                    menuItem.Visible = false; // TestRPC is a dependency of Truffle. TODO: Remove this item.
                }
                else if (menuItem == startTestRPC)
                {
                    menuItem.Visible = package.TestRPCInstalled && !package.CheckTestRPCRunning();
                }
                else if (menuItem == stopTestRPC)
                {
                    menuItem.Visible = package.TestRPCInstalled && package.CheckTestRPCRunning();
                }
                else if (menuItem == initializeProject)
                {
                    menuItem.Visible = package.TruffleInstalled && !package.TruffleProjectInitialized;
                }
                else
                {
                    menuItem.Visible = true;
                }

                
                if (isProjectMenuItem == true)
                {
                    menuItem.Enabled = package.TruffleInstalled && package.TruffleProjectInitialized;
                } else
                {
                    menuItem.Enabled = true;
                }
            });
        } 

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static TruffleMenu Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new TruffleMenu(package);
        }

        private void BeforeQueryStatusCallback(object sender, EventArgs e)
        {
            var cmd = (OleMenuCommand)sender;
            cmd.Enabled = ((TrufflePackage)this.package).InSolution;
        }

        private void CompileCallback(object sender, EventArgs e)
        {
            OutputPane pane = package.TrufflePane;
            pane.Clear();
            pane.AddLine("Compiling...");
            pane.RunTruffleCommand("compile --all", () =>
            {
                pane.AddLine("Done.");
            });
        }

        private void MigrateCallback(object sender, EventArgs e)
        {
            OutputPane pane = package.TrufflePane;
            pane.Clear();
            pane.AddLine("Migrating...");
            pane.RunTruffleCommand("migrate --reset", () =>
            {
                pane.AddLine("Done.");
            });
        }

        private void TestCallback(object sender, EventArgs e)
        {
            OutputPane pane = package.TrufflePane;
            pane.Clear();
            pane.AddLine("Running tests...");
            pane.RunTruffleCommand("test", () =>
            {
                pane.AddLine("Done.");
            });
        }

        private void InitializeProjectCallback(object sender, EventArgs e)
        {
            OutputPane pane = package.TrufflePane;
            pane.Clear();
            pane.AddLine("Initializing project...");
            pane.RunTruffleCommand("init", () =>
            {
                pane.AddLine("Done");
                this.package.RecheckEnvironment();
            });
        }

        private void InstallTruffleCallback(object sender, EventArgs e)
        {
            OutputPane pane = package.TrufflePane;

            pane.Clear();
            pane.AddLine("Checking Node.JS and NPM installation...");

            TruffleENV.CheckNPMInstalled((isNPMInstalled) =>
            {
                if (isNPMInstalled == false)
                {
                    pane.AddLine("Cannot install Truffle. It appears you don't have Node.JS or NPM installed on your system. Please visit http://nodejs.org for more information.");
                    return;
                }

                pane.AddLine("Installing Truffle... (this may take a minute)");

                pane.RunInProject("npm install truffle@beta --save-dev", () =>
                {
                    pane.AddLine("Done! Checking installation...");

                    if (TruffleENV.CheckTruffleInstalled(package.ProjectPath) == true)
                    {
                        ((TrufflePackage)this.package).RecheckEnvironment();
                        pane.AddLine("Completed successfully.");
                    }
                    else
                    {
                        pane.AddLine("Installation failed. Please see error messages above and try again.");
                    }
                });
            });
        }

        private void InstallTestRPCCallback(object sender, EventArgs e)
        {
            OutputPane pane = package.TrufflePane;

            pane.Clear();
            pane.AddLine("Checking Node.JS and NPM installation...");

            TruffleENV.CheckNPMInstalled((isNPMInstalled) =>
            {
                if (isNPMInstalled == false)
                {
                    pane.AddLine("Cannot install TestRPC. It appears you don't have Node.JS or NPM installed on your system. Please visit http://nodejs.org for more information.");
                    return;
                }

                pane.AddLine("Installing TestRPC... (this may take a minute)");

                pane.RunInProject("npm install ethereumjs-testrpc", () =>
                {
                    pane.AddLine("Done! Checking installation...");

                    if (TruffleENV.CheckTestRPCInstalled(package.ProjectPath) == true)
                    {
                        ((TrufflePackage)this.package).RecheckEnvironment();
                        pane.AddLine("Completed successfully.");
                    }
                    else
                    {
                        pane.AddLine("Installation failed. Please see error messages above and try again.");
                    }
                });
            });
        }

        private void StartTestRPCCallback(object sender, EventArgs e)
        {
            package.TestRPCPane.Clear();
            package.TestRPCPane.AddLine("Starting TestRPC...");
            package.TestRPCPane.RunCommand("\"" + TruffleENV.ExpectedTestRPCBinary(package.ProjectPath) + "\"", () =>
            {
                package.TestRPCPane.AddLine("Stopped.");
                package.RecheckEnvironment();
            });
        }

        private void StopTestRPCCallback(object sender, EventArgs e)
        {
            package.TestRPCPane.Kill();
        }

        private void ShowAboutCallback(object sender, EventArgs e)
        {
            //AboutBox aboutBox = new AboutBox();
            //aboutBox.Show();
            System.Diagnostics.Process.Start("http://truffleframework.com");
        }
    }
}
