﻿#if SILVERLIGHT

namespace Caliburn.ShellFramework.Results
{
    using System;
    using System.Windows.Controls;
    using PresentationFramework;
    using PresentationFramework.RoutedMessaging;

    /// <summary>
    /// Opens an OpenFileDialog.
    /// </summary>
    public class OpenFileDialogResult : IResult
    {
        readonly OpenFileDialog dialog;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenFileDialogResult"/> class.
        /// </summary>
        /// <param name="dialog">The dialog.</param>
        public OpenFileDialogResult(OpenFileDialog dialog)
        {
            this.dialog = dialog;
        }

        /// <summary>
        /// Gets the dialog.
        /// </summary>
        /// <value>The dialog.</value>
        public OpenFileDialog Dialog
        {
            get { return dialog; }
        }

        /// <summary>
        /// Executes the result using the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute(ResultExecutionContext context)
        {
            var result = dialog.ShowDialog()
                .GetValueOrDefault(false);

            Completed(this, new ResultCompletionEventArgs {WasCancelled = !result});
        }

        /// <summary>
        /// Occurs when execution has completed.
        /// </summary>
        public event EventHandler<ResultCompletionEventArgs> Completed = delegate { };
    }
}

#endif