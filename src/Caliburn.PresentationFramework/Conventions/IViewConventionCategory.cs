namespace Caliburn.PresentationFramework.Conventions
{
    using System.Collections.Generic;
    using ViewModels;

    /// <summary>
    /// A set of conventions applicable to a view.
    /// </summary>
    public interface IViewConventionCategory
    {
        /// <summary>
        /// Gets the applications.
        /// </summary>
        /// <param name="viewModelDescription">The view model description.</param>
        /// <param name="elementDescription">The element description.</param>
        /// <returns>The applications.</returns>
        IEnumerable<IViewApplicable> GetApplications(IViewModelDescription viewModelDescription, IElementDescription elementDescription);
    }
}