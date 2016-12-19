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
    using Helpers;

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

            ProcessRunner runner = new ProcessRunner();

            runner.OnLine += (line) =>
            {
                this.addText(line);
            };

            runner.run("pwd");
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
    }

}