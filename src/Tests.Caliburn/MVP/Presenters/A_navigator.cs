namespace Tests.Caliburn.MVP.Presenters
{
    using System;
    using Fakes;
    using global::Caliburn.PresentationFramework.Screens;
    using NUnit.Framework;
    using NUnit.Framework.SyntaxHelpers;

    [TestFixture]
    public class A_navigator : A_presenter_manager
    {
        protected Navigator<IScreen> _navigator;
        private IScreen _activeScreen;

        protected override ScreenBase CreateScreen()
        {
            return new Navigator<IScreen>();
        }

        protected override void given_the_context_of()
        {
            base.given_the_context_of();

            _navigator = (Navigator<IScreen>)_screen;
            _activeScreen = Mock<IScreen>();
        }

        [Test]
        public void cannot_go_back_when_no_previous()
        {
            Assert.That(_navigator.CanGoBack, Is.False);
        }

        [Test]
        public void cannot_go_forward_when_no_forward()
        {
            Assert.That(_navigator.CanGoForward, Is.False);
        }

        [Test]
        public void can_navigate()
        {
            Action<Action<bool>> nav = completed => completed(true);
            bool navigateSuccessful = false;

            _navigator.Navigate(
                nav,
                isSuccess => { navigateSuccessful = isSuccess; });

            Assert.That(navigateSuccessful);
        }

        [Test]
        public void can_go_back_if_history()
        {
            var firstCount = 0;
            var secondCount = 0;
            bool navigateSuccessful = false;

            Action<Action<bool>> first = completed =>{
                firstCount++;
                completed(true);
            };
            Action<Action<bool>> second = completed =>{
                secondCount++;
                completed(true);
            };

            _navigator.Navigate(first);
            _navigator.Navigate(second);

            Assert.That(_navigator.CanGoBack);

            _navigator.GoBack(
                isSuccess => { navigateSuccessful = isSuccess; });

            Assert.That(navigateSuccessful);
            Assert.That(firstCount, Is.EqualTo(2));
        }

        [Test]
        public void can_go_forward_if_next()
        {
            var firstCount = 0;
            var secondCount = 0;
            bool navigateSuccessful = false;

            Action<Action<bool>> first = completed =>{
                firstCount++;
                completed(true);
            };
            Action<Action<bool>> second = completed =>{
                secondCount++;
                completed(true);
            };

            _navigator.Navigate(first);
            _navigator.Navigate(second);

            _navigator.GoBack();

            Assert.That(_navigator.CanGoForward);

            _navigator.GoForward(
                isSuccess => { navigateSuccessful = isSuccess; });

            Assert.That(navigateSuccessful);
            Assert.That(secondCount, Is.EqualTo(2));
        }

        [Test]
        public void knows_how_many_actions_it_contains_after_navigation()
        {
            _navigator.Navigate(completed => completed(true));
            _navigator.Navigate(completed => completed(true));
            _navigator.Navigate(completed => completed(true));

            _navigator.GoBack();
            _navigator.GoForward();

            Assert.That(_navigator.Count, Is.EqualTo(3));
        }

        [Test]
        public void knows_the_index_of_the_current_action()
        {
            _navigator.Navigate(completed => completed(true));
            _navigator.Navigate(completed => completed(true));
            _navigator.Navigate(completed => completed(true));

            Assert.That(_navigator.CurrentPosition, Is.EqualTo(3));

            _navigator.GoBack();
            _navigator.GoBack();

            Assert.That(_navigator.CurrentPosition, Is.EqualTo(1));

            _navigator.GoForward();

            Assert.That(_navigator.CurrentPosition, Is.EqualTo(2));
        }

        [Test]
        public void can_clear_history()
        {
            Action<Action<bool>> nav = completed => completed(true);

            _navigator.Navigate(nav);
            _navigator.Navigate(nav);

            Assert.That(_navigator.CanGoBack);

            _navigator.ClearHistory();

            Assert.That(_navigator.CanGoBack, Is.False);
        }

        [Test]
        public void Raises_evens_on_navigate()
        {
            bool backWasRaised = false;
            bool forwardWasRaised = false;

            _navigator.PropertyChanged +=
                (s, e) =>{
                    if(e.PropertyName == "CanGoBack") backWasRaised = true;
                    if(e.PropertyName == "CanGoForward") forwardWasRaised = true;
                };

            Action<Action<bool>> nav = completed => completed(true);

            _navigator.Navigate(nav);

            Assert.That(backWasRaised);
            Assert.That(forwardWasRaised);
        }

        [Test]
        public void Raises_events_on_back()
        {
            Action<Action<bool>> nav = completed => completed(true);

            _navigator.Navigate(nav);
            _navigator.Navigate(nav);

            bool backWasRaised = false;
            bool forwardWasRaised = false;

            _navigator.PropertyChanged +=
                (s, e) =>{
                    if(e.PropertyName == "CanGoBack") backWasRaised = true;
                    if(e.PropertyName == "CanGoForward") forwardWasRaised = true;
                };

            _navigator.GoBack();

            Assert.That(backWasRaised);
            Assert.That(forwardWasRaised);
        }

        [Test]
        public void Raises_events_on_forward()
        {
            Action<Action<bool>> nav = completed => completed(true);

            _navigator.Navigate(nav);
            _navigator.Navigate(nav);
            _navigator.GoBack();

            bool backWasRaised = false;
            bool forwardWasRaised = false;

            _navigator.PropertyChanged +=
                (s, e) =>{
                    if(e.PropertyName == "CanGoBack") backWasRaised = true;
                    if(e.PropertyName == "CanGoForward") forwardWasRaised = true;
                };

            _navigator.GoForward();

            Assert.That(backWasRaised);
            Assert.That(forwardWasRaised);
        }

        [Test]
        public void Raises_events_on_change_current()
        {
            bool backWasRaised = false;
            bool forwardWasRaised = false;

            _navigator.PropertyChanged +=
                (s, e) =>{
                    if(e.PropertyName == "CanGoBack") backWasRaised = true;
                    if(e.PropertyName == "CanGoForward") forwardWasRaised = true;
                };

            _navigator.ActiveScreen = _activeScreen;

            Assert.That(backWasRaised);
            Assert.That(forwardWasRaised);
        }

        [Test]
        public void syncs_current_change_with_history()
        {
            var screen1 = new FakeScreen {CanShutdownResult = true};
            var screen2 = new FakeScreen {CanShutdownResult = true};

            _navigator.ActiveScreen = screen1;
            _navigator.OpenScreen(screen2);

            bool backSuccessful = false;
            bool forwardSuccessful = false;

            _navigator.GoBack(
                isSuccess => backSuccessful = isSuccess
                );

            _navigator.GoForward(
                isSuccess => forwardSuccessful = isSuccess
                );

            Assert.That(_navigator.ActiveScreen, Is.EqualTo(screen2));
            Assert.That(backSuccessful);
            Assert.That(forwardSuccessful);
        }
    }
}