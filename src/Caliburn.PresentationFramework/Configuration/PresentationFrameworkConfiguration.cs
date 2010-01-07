﻿namespace Caliburn.PresentationFramework.Configuration
{
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using Actions;
    using Commands;
    using Core;
    using Core.Configuration;
    using Core.IoC;
    using Invocation;
    using Microsoft.Practices.ServiceLocation;
    using Parsers;
    using Screens;
    using ViewModels;
    using Action=Actions.Action;

#if SILVERLIGHT
    using System.Windows.Controls;
#endif

    /// <summary>
    /// The presenation framework module.
    /// </summary>
    public class PresentationFrameworkConfiguration :
        ConventionalModule<PresentationFrameworkConfiguration, IPresentationFrameworkServicesDescription>
    {
        private bool _registerAllScreensWithSubjects;

#if !SILVERLIGHT
        private static readonly bool _isInDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());
#else
        private static readonly bool _isInDesignMode = DesignerProperties.GetIsInDesignMode(new UserControl());
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="PresentationFrameworkConfiguration"/> class.
        /// </summary>
        public PresentationFrameworkConfiguration()
        {
            CaliburnModule<CoreConfiguration>
                .Instance
                .Using(x => x.Dispatcher<DispatcherImplementation>());
        }

        /// <summary>
        /// Gets a value indicating whether the framework is in design mode.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the framework is in design mode; otherwise, <c>false</c>.
        /// </value>
        public static bool IsInDesignMode
        {
            get { return _isInDesignMode; }
        }

        /// <summary>
        /// Searches the <see cref="IAssemblySource"/> and registers all screens which concretely implement <see cref="IScreen{T}"/> using their closed interface type.
        /// </summary>
        public void RegisterAllScreensWithSubjects()
        {
            _registerAllScreensWithSubjects = true;
        }

        /// <summary>
        /// Initializes the core.
        /// </summary>
        /// <param name="serviceLocator">The service locator.</param>
        protected override void InitializeCore(IServiceLocator serviceLocator)
        {
            var controller = serviceLocator.GetInstance<IRoutedMessageController>();
            var messageBinder = serviceLocator.GetInstance<IMessageBinder>();
            var parser = serviceLocator.GetInstance<IParser>();

            parser.RegisterMessageParser("Action", new ActionMessageParser(messageBinder));
            parser.RegisterMessageParser("ResourceCommand", new CommandMessageParser(messageBinder, CommandSource.Resource));
            parser.RegisterMessageParser("ContainerCommand", new CommandMessageParser(messageBinder, CommandSource.Container));
            parser.RegisterMessageParser("BoundCommand", new CommandMessageParser(messageBinder, CommandSource.Bound));

            Message.Initialize(
                controller,
                parser
                );

            Action.Initialize(
                controller,
                serviceLocator.GetInstance<IViewModelDescriptionFactory>(),
                serviceLocator
                );

            View.Initialize(
                serviceLocator.GetInstance<IViewLocator>(),
                serviceLocator.GetInstance<IViewModelBinder>()
                );

            ScreenExtensions.Initialize(
                serviceLocator.GetInstance<IViewModelFactory>()
                );

            if (!_registerAllScreensWithSubjects)
                return;

            var registry = serviceLocator.GetAllInstances<IRegistry>()
                .FirstOrDefault();

            if (registry == null)
                return;

            var assemblySource = serviceLocator.GetInstance<IAssemblySource>();
            assemblySource.Apply(x => RegisterScreens(registry, x));
            assemblySource.AssemblyAdded += assembly => RegisterScreens(registry, assembly);
        }

        private static void RegisterScreens(IRegistry registry, Assembly assembly) 
        {
            var matches = from type in assembly.GetExportedTypes()
                          let service = type.FindInterfaceThatCloses(typeof(IScreen<>))
                          where service != null
                          select new PerRequest {Service = service, Implementation = type};

            registry.Register(matches.OfType<IComponentRegistration>());
        }
    }
}