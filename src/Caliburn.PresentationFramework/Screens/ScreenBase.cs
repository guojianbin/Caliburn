namespace Caliburn.PresentationFramework.Screens
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using Behaviors;
    using Core.Logging;
    using Views;
    using System.Linq;

    /// <summary>
    /// Implements common functionality used by all implementors of <see cref="IScreen"/>.
    /// </summary>
    public abstract class ScreenBase : PropertyChangedBase, IScreenEx
    {
        private static readonly ILog Log = LogManager.GetLog(typeof(ScreenBase));
        private readonly Dictionary<object, object> _views = new Dictionary<object, object>();

        private IScreenCollection _parent;
        private bool _isActive;
        private bool _isInitialized;
        private string _displayName;

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        [DoNotNotify]
        public virtual IScreenCollection Parent
        {
            get { return _parent; }
            set
            {
                _parent = value;
                NotifyOfPropertyChange(() => Parent);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is initialized.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is initialized; otherwise, <c>false</c>.
        /// </value>
        [DoNotNotify]
        public virtual bool IsInitialized
        {
            get { return _isInitialized; }
            protected set
            {
                _isInitialized = value;
                NotifyOfPropertyChange(() => IsInitialized);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is active.
        /// </summary>
        /// <value><c>true</c> if this instance is active; otherwise, <c>false</c>.</value>
        [DoNotNotify]
        public virtual bool IsActive
        {
            get { return _isActive; }
            protected set
            {
                _isActive = value;
                NotifyOfPropertyChange(() => IsActive);
            }
        }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        /// <value>The display name.</value>
        [DoNotNotify]
        public virtual string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(_displayName))
                    _displayName = GetType().Name;
                
                return _displayName;
            }
            set
            {
                _displayName = value;
                NotifyOfPropertyChange(() => DisplayName);
            }
        }

        /// <summary>
        /// Determines whether this instance can shutdown.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance can shutdown; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanShutdown()
        {
            AttemptingShutdown(this, EventArgs.Empty);
            return CanShutdownCore();
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Shuts down this instance.
        /// </summary>
        public abstract void Shutdown();

        /// <summary>
        /// Activates this instance.
        /// </summary>
        public abstract void Activate();

        /// <summary>
        /// Deactivates this instance.
        /// </summary>
        public abstract void Deactivate();

        /// <summary>
        /// Occurs when [initialized].
        /// </summary>
        public virtual event EventHandler Initialized = delegate { };

        /// <summary>
        /// Occurs before attempting to shutdown.
        /// </summary>
        public virtual event EventHandler AttemptingShutdown = delegate { };

        /// <summary>
        /// Occurs when [was shutdown].
        /// </summary>
        public virtual event EventHandler WasShutdown = delegate { };

        /// <summary>
        /// Occurs when [activated].
        /// </summary>
        public virtual event EventHandler Activated = delegate { };

        /// <summary>
        /// Occurs when [deactivated].
        /// </summary>
        public virtual event EventHandler Deactivated = delegate { };

        /// <summary>
        /// Called when [initialize].
        /// </summary>
        protected virtual void OnInitialize()
        {
            Initialized(this, EventArgs.Empty);
            Log.Info("Initialized {0}.", this);
        }

        /// <summary>
        /// Determines whether this instance can shutdown.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance can shutdown; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool CanShutdownCore()
        {
            return true;
        }

        /// <summary>
        /// Called when [shutdown].
        /// </summary>
        protected virtual void OnShutdown()
        {
            WasShutdown(this, EventArgs.Empty);
            Log.Info("Shut down {0}.", this);
        }

        /// <summary>
        /// Called when [activate].
        /// </summary>
        protected virtual void OnActivate()
        {
            Activated(this, EventArgs.Empty);
            Log.Info("Activated {0}.", this);
        }

        /// <summary>
        /// Called when [deactivate].
        /// </summary>
        protected virtual void OnDeactivate()
        {
            Deactivated(this, EventArgs.Empty);
            Log.Info("Deactivated {0}.", this);
        }

        /// <summary>
        /// Called when the screen's view is loaded.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <param name="context">The context.</param>
        public virtual void AttachView(object view, object context)
        {
            var loadWired = _views.Values.Contains(view);
            _views[context ?? DefaultViewLocator.DefaultContext] = view;

            var dependencyObject = view as DependencyObject;
            if (!loadWired && dependencyObject != null)
                dependencyObject.OnLoad(delegate{
                    OnViewLoaded(view);
                    Log.Info("View {0} loaded for {1}.", view, this);
                });
        }

        /// <summary>
        /// Called when an attached view is loaded.
        /// </summary>
        /// <param name="view">The view.</param>
        protected virtual void OnViewLoaded(object view) {}

        /// <summary>
        /// Gets the view.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The view</returns>
        public virtual object GetView(object context)
        {
            object view;
            _views.TryGetValue(context ?? DefaultViewLocator.DefaultContext, out view);
            return view;
        }

        /// <summary>
        /// Closes this instance by asking its Parent to initiate shutdown or by asking it's corresponding default view to close.
        /// </summary>
        public virtual void Close()
        {
            if (Parent != null)
                Parent.ShutdownScreen(this, delegate { });
            else
            {
                var view = GetView(null);

                if (view == null)
                {
                    var ex1 = new NotSupportedException(
                        "You cannot close an instance without a parent or a default view."
                        );

                    Log.Error(ex1);
                    throw ex1;
                }

                var method = view.GetType().GetMethod("Close");
                if(method != null)
                {
                    method.Invoke(view, null);
                    return;
                }

                var property = view.GetType().GetProperty("IsOpen");
                if (property != null)
                {
                    property.SetValue(view, false, new object[] {});
                    return;
                }

                var ex2 = new NotSupportedException(
                        "The default view does not support the Close method or the IsOpen property."
                        );

                Log.Error(ex2);
                throw ex2;
            }
        }

#if !SILVERLIGHT

        /// <summary>
        /// Closes this instance by asking its Parent to initiate shutdown or by asking it's corresponding default view to close.
        /// This overload also provides an opportunity to pass a dialog result to it's corresponding default view.
        /// </summary>
        /// <param name="dialogResult">The dialog result.</param>
        public virtual void Close(bool? dialogResult)
        {
            var view = GetView(null);

            if(view != null)
            {
                var property = view.GetType().GetProperty("DialogResult");
                if(property != null)
                    property.SetValue(view, dialogResult, null);
            }

            Close();
        }

#endif
    }
}