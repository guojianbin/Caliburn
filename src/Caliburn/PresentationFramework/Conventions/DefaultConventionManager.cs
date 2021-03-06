namespace Caliburn.PresentationFramework.Conventions
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Actions;
    using Converters;
    using Core;
    using Core.Invocation;
    using Core.Logging;
    using Filters;
    using ViewModels;
    using Views;

    /// <summary>
    /// The default implementation of <see cref="IConventionManager"/>.
    /// </summary>
    public class DefaultConventionManager : IConventionManager
    {
        static readonly ILog Log = LogManager.GetLog(typeof(DefaultConventionManager));

        private class ConverterConvention
        {
            public DependencyProperty Target;
            public Type Source;
            public IValueConverter Converter;
        }

        readonly IMethodFactory methodFactory;

        readonly Dictionary<Type, IElementConvention> elementConventions = new Dictionary<Type, IElementConvention>();
        readonly List<IViewConventionCategory> viewConventions = new List<IViewConventionCategory>();
        readonly List<ConverterConvention> converters = new List<ConverterConvention>();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultConventionManager"/> class.
        /// </summary>
        /// <param name="methodFactory">The method factory.</param>
        public DefaultConventionManager(IMethodFactory methodFactory)
        {
            this.methodFactory = methodFactory;

            GetDefaultElementConventions()
                .Apply(AddElementConvention);

            SetupDefaultViewConventions();

            SetupDefaultConverterConventions();
        }

        /// <summary>
        /// Adds the element convention.
        /// </summary>
        /// <param name="convention">The convention.</param>
        public void AddElementConvention(IElementConvention convention)
        {
            elementConventions[convention.Type] = convention;
        }

        /// <summary>
        /// Adds the view conventions.
        /// </summary>
        /// <param name="conventionCategory">The convention set.</param>
        public void AddViewConventions(IViewConventionCategory conventionCategory)
        {
            viewConventions.Add(conventionCategory);
        }

        /// <summary>
        /// Gets the element convention for the type of element specified.
        /// </summary>
        /// <param name="elementType">Type of the element.</param>
        /// <returns>The convention.</returns>
        public IElementConvention GetElementConvention(Type elementType)
        {
            if (elementType == null) 
                return null;

            IElementConvention convention;
            elementConventions.TryGetValue(elementType, out convention);
            return convention ?? GetElementConvention(elementType.BaseType);
        }

        /// <summary>
        /// Gets the conventional value converter.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="source">The source.</param>
        /// <returns>
        /// The converter or null if none is defined.
        /// </returns>
        public IValueConverter GetValueConverter(DependencyProperty target, Type source)
        {
            return (from convention in converters
            where convention.Target == target
                && convention.Source.IsAssignableFrom(source)
            select convention.Converter).FirstOrDefault();
        }

        /// <summary>
        /// Adds the value converter convention.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="source">The source.</param>
        /// <param name="converter">The converter.</param>
        public void AddConverterConvention(DependencyProperty target, Type source, IValueConverter converter)
        {
            converters.Add(new ConverterConvention
            {
                Target = target, 
                Source = source, 
                Converter = converter
            });
        }

        /// <summary>
        /// Determines the conventions for a view model and a set of UI elements.
        /// </summary>
        /// <param name="viewModelDescription">The view model description.</param>
        /// <param name="elementDescriptions">The element descriptions.</param>
        /// <returns>The applicable conventions.</returns>
        public virtual IEnumerable<IViewApplicable> DetermineConventions(IViewModelDescription viewModelDescription, IEnumerable<ElementDescription> elementDescriptions)
        {
            foreach (var elementDescription in elementDescriptions)
            {
                bool found = false;

                foreach(var set in viewConventions)
                {
                    var applications = set.GetApplications(this, viewModelDescription, elementDescription);

                    foreach(var application in applications)
                    {
                        yield return application;
                    }

                    if (applications.Any())
                    {
                        found = true;
                        break;
                    }
                }

                if(!found)
                    Log.Warn("No convention matched for {0}.", elementDescription.Name);
            }
        }

        /// <summary>
        /// Applies the action creation conventions.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="targetMethod">The target method.</param>
        public virtual void ApplyActionCreationConventions(IAction action, IMethod targetMethod)
        {
            var canExecuteName = DeriveCanExecuteName(targetMethod.Info.Name);
            var found = targetMethod.Info.GetAttributes<PreviewAttribute>(true)
                .FirstOrDefault(x => x.MethodName == canExecuteName);

            if (found != null)
                return;

            var canExecute = targetMethod.Info.DeclaringType.GetMethod(
                                 canExecuteName,
                                 targetMethod.Info.GetParameters().Select(x => x.ParameterType).ToArray()
                                 )
                             ?? targetMethod.Info.DeclaringType.GetMethod("get_" + canExecuteName);

            if (canExecute != null)
            {
                action.Filters.Add(new PreviewAttribute(methodFactory.CreateFrom(canExecute)));
                Log.Info("Action preview convention added for {0} on {1}.", targetMethod.Info.Name, canExecute.Name);
            }
        }

        /// <summary>
        /// Derives the name of the can execute method/property.
        /// </summary>
        /// <param name="baseName">Name of the base method.</param>
        /// <returns>The conventional name of the can execute poroperty.</returns>
        protected virtual string DeriveCanExecuteName(string baseName)
        {
            return "Can" + baseName;
        }

        /// <summary>
        /// Sets up the default view conventions.
        /// </summary>
        protected virtual void SetupDefaultViewConventions()
        {
            var actionSet = new ViewConventionCategory<IAction>(x => x.Actions);
            actionSet.AddConvention(new DefaultActionConvention());
            AddViewConventions(actionSet);

            var bindingSet = new ViewConventionCategory<PropertyInfo>(x => x.Properties);
            bindingSet.AddConvention(new DefaultBindingConvention());
            bindingSet.AddConvention(new ItemsControlBindingConvention());
            AddViewConventions(bindingSet);

            var subActions = new ViewConventionCategory<PropertyInfo>(x => x.Properties);
            subActions.AddConvention(new SubActionConvention());
            AddViewConventions(subActions);
        }

        /// <summary>
        /// Sets up the default converter conventions.
        /// </summary>
        protected virtual void SetupDefaultConverterConventions()
        {
            AddConverterConvention(UIElement.VisibilityProperty, typeof(bool), new BooleanToVisibilityConverter());
            AddConverterConvention(Selector.SelectedItemProperty, typeof(BindableCollection<BindableEnum>), new EnumConverter());
        }

        /// <summary>
        /// Gets the default element conventions.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<IElementConvention> GetDefaultElementConventions()
        {
#if !SILVERLIGHT
                yield return ElementConvention<Hyperlink>("Click", Hyperlink.DataContextProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<RichTextBox>("TextChanged", RichTextBox.DataContextProperty, (c, o) => c.Document = (FlowDocument)o, c => c.Document);
                yield return ElementConvention<Menu>("Click", Menu.ItemsSourceProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<MenuItem>("Click", MenuItem.ItemsSourceProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<Label>("DataContextChanged", Label.ContentProperty, (c, o) => c.Content = o, c => c.Content);
                yield return ElementConvention<DockPanel>("Loaded", DockPanel.VisibilityProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<UniformGrid>("Loaded", UniformGrid.VisibilityProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<WrapPanel>("Loaded", WrapPanel.VisibilityProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<Viewbox>("Loaded", Viewbox.VisibilityProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<BulletDecorator>("Loaded", BulletDecorator.VisibilityProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<Slider>("ValueChanged", Slider.ValueProperty, (c, o) => c.Value = (double)o, c => c.Value);
                yield return ElementConvention<Expander>("Expanded", Expander.IsExpandedProperty, (c, o) => c.IsExpanded = (bool)o, c => c.IsExpanded);
                yield return ElementConvention<UserControl>("Loaded", UserControl.VisibilityProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<Window>("Loaded", Window.DataContextProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<StatusBar>("Loaded", StatusBar.ItemsSourceProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<ToolBar>("Loaded", ToolBar.ItemsSourceProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<ToolBarTray>("Loaded", ToolBarTray.VisibilityProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<TreeView>("SelectedItemChanged", TreeView.ItemsSourceProperty, (c, o) => c.ItemsSource = (IEnumerable)o, c => c.SelectedItem);
                yield return ElementConvention<TabControl>("SelectionChanged", TabControl.ItemsSourceProperty, (c, o) => c.ItemsSource = (IEnumerable)o, c => c.SelectedItem);
                yield return ElementConvention<TabItem>("DataContextChanged", TabItem.ContentProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<ListView>("SelectionChanged", ListView.ItemsSourceProperty, (c, o) => c.ItemsSource = (IEnumerable)o,
                                   c =>
                                   {
                                       if (c.SelectionMode == SelectionMode.Extended ||
                                          c.SelectionMode == SelectionMode.Multiple)
                                           return c.SelectedItems;
                                       return c.SelectedItem;
                                   });
                yield return ElementConvention<ListBox>("SelectionChanged", ListBox.ItemsSourceProperty, (c, o) => c.ItemsSource = (IEnumerable)o,
                                  c =>
                                  {
                                      if (c.SelectionMode == SelectionMode.Extended ||
                                         c.SelectionMode == SelectionMode.Multiple)
                                          return c.SelectedItems;
                                      return c.SelectedItem;
                                  });
                yield return ElementConvention<ComboBox>("SelectionChanged", ComboBox.ItemsSourceProperty, (c, o) => c.ItemsSource = (IEnumerable)o, c => c.IsEditable ? c.Text : c.SelectedItem);
#else
                yield return ElementConvention<HyperlinkButton>("Click", HyperlinkButton.ContentProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<UserControl>("Loaded", UserControl.VisibilityProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<ListBox>("SelectionChanged", ListBox.ItemsSourceProperty, (c, o) => c.ItemsSource = (IEnumerable) o, c => c.SelectedItem);
                yield return ElementConvention<ComboBox>("SelectionChanged", ComboBox.ItemsSourceProperty, (c, o) => c.ItemsSource = (IEnumerable) o, c => c.SelectedItem);
#endif
                yield return ElementConvention<Image>("Loaded", Image.SourceProperty, (c, o) => c.Source = (ImageSource)o, c => c.Source);
                yield return ElementConvention<ButtonBase>("Click", ButtonBase.ContentProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<Button>("Click", Button.ContentProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<ToggleButton>("Click", ToggleButton.IsCheckedProperty, (c, o) => c.IsChecked = (bool)o, c => c.IsChecked);
                yield return ElementConvention<RadioButton>("Click", RadioButton.IsCheckedProperty, (c, o) => c.IsChecked = (bool)o, c => c.IsChecked);
                yield return ElementConvention<CheckBox>("Click", CheckBox.IsCheckedProperty, (c, o) => c.IsChecked = (bool)o, c => c.IsChecked);
                yield return ElementConvention<TextBox>("TextChanged", TextBox.TextProperty, (c, o) => c.Text = o.SafeToString(), c => c.Text);
                yield return ElementConvention<TextBlock>("DataContextChanged", TextBlock.TextProperty, (c, o) => c.Text = o.SafeToString(), c => c.Text);
                yield return ElementConvention<StackPanel>("Loaded", StackPanel.VisibilityProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<Grid>("Loaded", Grid.VisibilityProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<Border>("Loaded", Border.VisibilityProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<ItemsControl>("Loaded", ItemsControl.ItemsSourceProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return new DefaultElementConvention<ContentControl>("Loaded", ContentControl.ContentProperty, (c, o) => c.DataContext = o, c => c.DataContext,
                    (element, property) =>{
#if SILVERLIGHT
                        return element.ContentTemplate == null && !(element.Content is DependencyObject)
                            ? View.ModelProperty
                            : property;
#else
                        return element.ContentTemplate == null && element.ContentTemplateSelector == null && !(element.Content is DependencyObject)
                            ? View.ModelProperty
                            : property;
#endif
                    });

#if SILVERLIGHT
                yield return ElementConvention<PasswordBox>("PasswordChanged", PasswordBox.PasswordProperty, (c, o) => c.Password = o.SafeToString(), c => c.Password);
#else
                yield return ElementConvention<PasswordBox>("PasswordChanged", PasswordBox.DataContextProperty, (c, o) => c.Password = o.SafeToString(), c => c.Password);
#endif
                yield return ElementConvention<Shape>("MouseLeftButtonUp", Shape.VisibilityProperty, (c, o) => c.DataContext = o, c => c.DataContext);
                yield return ElementConvention<FrameworkElement>("Loaded", FrameworkElement.VisibilityProperty, (c, o) => c.DataContext = o, c => c.DataContext);
        }

        /// <summary>
        /// Creates an element convention.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="defaultEvent">The default event.</param>
        /// <param name="bindableProperty">The bindable property.</param>
        /// <param name="setter">The setter.</param>
        /// <param name="getter">The getter.</param>
        /// <returns>The element convention.</returns>
        protected virtual IElementConvention ElementConvention<T>(string defaultEvent, DependencyProperty bindableProperty, Action<T, object> setter, Func<T, object> getter)
            where T : DependencyObject
        {
            return new DefaultElementConvention<T>(
                defaultEvent,
                bindableProperty,
                setter,
                getter,
                null
                );
        }
    }
}