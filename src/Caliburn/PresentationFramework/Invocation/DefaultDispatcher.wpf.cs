#if !SILVERLIGHT

namespace Caliburn.PresentationFramework.Invocation
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Threading;
    using Core.Invocation;

    /// <summary>
    /// An implementation of <see cref="IDispatcher"/> for WPF.
    /// </summary>
    public class DefaultDispatcher : IDispatcher
    {
        readonly Dispatcher dispatcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDispatcher"/> class.
        /// </summary>
        public DefaultDispatcher()
        {
            dispatcher = GetDispatcher();
            DefaultPriority = DispatcherPriority.Normal;
        }

        /// <summary>
        /// Gets or sets the default dispatcher priority.
        /// </summary>
        /// <value>The default priority.</value>
        public DispatcherPriority DefaultPriority { get; set; }

        /// <summary>
        /// Gets the dispatcher.
        /// </summary>
        /// <returns></returns>
        protected virtual Dispatcher GetDispatcher()
        {
            if (Application.Current != null && Application.Current.Dispatcher != null)
                return Application.Current.Dispatcher;
            return Dispatcher.CurrentDispatcher;
        }

        /// <summary>
        /// Executes code on the background thread.
        /// </summary>
        /// <param name="backgroundAction">The background action.</param>
        /// <param name="uiCallback">The UI callback.</param>
        /// <param name="progressChanged">The progress change callback.</param>
        public IBackgroundTask ExecuteOnBackgroundThread(Action backgroundAction, RunWorkerCompletedEventHandler uiCallback, ProgressChangedEventHandler progressChanged)
        {
            var task = new BackgroundTask(
                () =>{
                    backgroundAction();
                    return null;
                });

            if(uiCallback != null)
                task.Completed += (s, e) => ExecuteOnUIThread(() => uiCallback(s, e));

            if(progressChanged != null)
                task.ProgressChanged += (s, e) => ExecuteOnUIThread(() => progressChanged(s, e));

            task.Start(null);

            return task;
        }

        /// <summary>
        /// Executes code on the UI thread.
        /// </summary>
        /// <param name="uiAction">The UI action.</param>
        public void ExecuteOnUIThread(Action uiAction)
        {
            if(dispatcher.CheckAccess())
                uiAction();
            else
            {
                dispatcher.Invoke(
                    DefaultPriority,
                    uiAction
                    );
            }
        }

        /// <summary>
        /// Executes code on the UI thread asynchronously.
        /// </summary>
        /// <param name="uiAction">The UI action.</param>
        public IDispatcherOperation BeginExecuteOnUIThread(Action uiAction)
        {
            var operation = dispatcher.BeginInvoke(
                uiAction,
                DefaultPriority
                );

            return new DispatcherOperationProxy(operation);
        }
    }
}

#endif