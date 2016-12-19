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

    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

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

            System.Timers.Timer timer = new System.Timers.Timer(100);

            // Hook up the Elapsed event for the timer.
            timer.Elapsed += OnTimedEvent;

            timer.Enabled = true;
        }

        private void OnTimedEvent(object sender, System.Timers.ElapsedEventArgs args)
        {
            string str = "Bacon ipsum dolor amet swine alcatra venison bacon shank shankle pastrami rump jerky ball tip short loin kielbasa filet mignon pork chop.\r\n";

            if (!Dispatcher.CheckAccess())
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        this.textBox.AppendText(str);

                        // Magic number; just so users can attach it to the bottom again.
                        if (this.scrollViewer.VerticalOffset > this.scrollViewer.ScrollableHeight - 20)
                        {
                            this.scrollViewer.ScrollToBottom();
                        }
                    }, System.Windows.Threading.DispatcherPriority.Input);
                } catch
                {
                    // This would fire when VS is closed. Not sure what to do here.
                }
            }
        }
    }

}