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
  
    /// <summary>
    /// Interaction logic for ToolWindow1Control.
    /// </summary>
    public partial class ToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindowControl"/> class.
        /// </summary>
        public ToolWindowControl()
        {
            this.InitializeComponent();
            this.textBox.Text = "To use Truffle, open a Truffle project and then select actions from the Truffle menu above.";
        }

        public void ClearText()
        {
            if (!Dispatcher.CheckAccess())
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        this.textBox.Clear();
                    }, System.Windows.Threading.DispatcherPriority.Input);
                }
                catch
                {
                    // This would fire when VS is closed. Not sure what to do here.
                }
            } else
            {
                this.textBox.Clear();
            }
        }

        public void AddText(string str)
        {
            if (!Dispatcher.CheckAccess())
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        AppendText(str);
                    }, System.Windows.Threading.DispatcherPriority.Input);
                }
                catch
                {
                    // This would fire when VS is closed. Not sure what to do here.
                }
            } else
            {
                AppendText(str);
            }
        }

        private void AppendText(string str)
        {
            this.textBox.AppendText(str);

            // Magic number 20; just so users can attach it to the bottom again.
            if (this.scrollViewer.VerticalOffset > this.scrollViewer.ScrollableHeight - 20)
            {
                this.scrollViewer.ScrollToBottom();
            }
        }
    }

}