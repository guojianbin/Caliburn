namespace Caliburn.Core.Invocation
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Metadata;
    using Threading;

    /// <summary>
    /// An implementation of <see cref="IMethodFactory"/>.
    /// </summary>
    public class MethodFactory : IMethodFactory
    {
        private readonly IThreadPool _threadPool;
        private readonly Dictionary<MethodInfo, IMethod> _cache = new Dictionary<MethodInfo, IMethod>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodFactory"/> class.
        /// </summary>
        /// <param name="threadPool">The thread pool.</param>
        public MethodFactory(IThreadPool threadPool)
        {
            _threadPool = threadPool;
        }

        /// <summary>
        /// Creates an instance of <see cref="IMethod"/> using the <see cref="MethodInfo"/>.
        /// </summary>
        /// <param name="methodInfo">The method info.</param>
        /// <returns>An instance of <see cref="IMethod"/>.</returns>
        public IMethod CreateFrom(MethodInfo methodInfo)
        {
            IMethod method;

            if(!_cache.TryGetValue(methodInfo, out method))
            {
                if(methodInfo.ReturnType == typeof(void))
                    method = new Procedure(methodInfo, _threadPool);
                else method = new Function(methodInfo, _threadPool);

                _cache[methodInfo] = method;
            }

            return method;
        }

        /// <summary>
        /// A base class for <see cref="IMethod"/> implementations.
        /// </summary>
        private abstract class MethodProxyBase : MetadataContainer, IMethod
        {
            private readonly MethodInfo _info;
            protected readonly IThreadPool _threadPool;

            /// <summary>
            /// Initializes a new instance of the <see cref="MethodProxyBase"/> class.
            /// </summary>
            /// <param name="info">The info.</param>
            /// <param name="threadPool">The thread pool.</param>
            protected MethodProxyBase(MethodInfo info, IThreadPool threadPool)
            {
                _info = info;
                _threadPool = threadPool;

                AddMetadataFrom(_info);
            }

            /// <summary>
            /// Gets the <see cref="MethodInfo"/> to which this instance applies.
            /// </summary>
            /// <value>The info.</value>
            public MethodInfo Info
            {
                get { return _info; }
            }

            /// <summary>
            /// Invokes the specified method on the provided instance with the given parameters.
            /// </summary>
            /// <param name="instance">The instance.</param>
            /// <param name="parameters">The parameters.</param>
            /// <returns>
            /// The result of the function or null if it is a procedure.
            /// </returns>
            public abstract object Invoke(object instance, params object[] parameters);

            /// <summary>
            /// Creates a background task for executing this method asynchronously.
            /// </summary>
            /// <param name="instance">The instance.</param>
            /// <param name="parameters">The parameters.</param>
            /// <returns>
            /// An instance of <see cref="IBackgroundTask"/>.
            /// </returns>
            public abstract IBackgroundTask CreateBackgroundTask(object instance, params object[] parameters);
        }

        /// <summary>
        /// A procedure implementation of <see cref="IMethod"/>.
        /// </summary>
        private class Procedure : MethodProxyBase
        {
            private readonly LateBoundProc _theDelegate;

            /// <summary>
            /// Initializes a new instance of the <see cref="Procedure"/> class.
            /// </summary>
            /// <param name="info">The info.</param>
            /// <param name="threadPool">The thread pool.</param>
            public Procedure(MethodInfo info, IThreadPool threadPool)
                : base(info, threadPool)
            {
                _theDelegate = DelegateFactory.Create<LateBoundProc>(info);
            }

            /// <summary>
            /// Invokes the specified method on the provided instance with the given parameters.
            /// </summary>
            /// <param name="instance">The instance.</param>
            /// <param name="parameters">The parameters.</param>
            /// <returns>
            /// The result of the function or null if it is a procedure.
            /// </returns>
            public override object Invoke(object instance, params object[] parameters)
            {
                return SafeInvoke(instance, parameters);
            }

            /// <summary>
            /// Creates a background task for executing this method asynchronously.
            /// </summary>
            /// <param name="instance">The instance.</param>
            /// <param name="parameters">The parameters.</param>
            /// <returns>
            /// An instance of <see cref="IBackgroundTask"/>.
            /// </returns>
            public override IBackgroundTask CreateBackgroundTask(object instance, params object[] parameters)
            {
                return new BackgroundTask(_threadPool, () => SafeInvoke(instance, parameters));
            }

            private object SafeInvoke(object instance, object[] parameters)
            {
                try
                {
                    _theDelegate(instance, parameters);
                    return null;
                }
                catch (Exception)
                {
                    var requirements = Info.GetParameters();

                    if (requirements.Length != parameters.Length)
                        throw new CaliburnException(
                            string.Format(
                                "The method '{0}' expected {1} parameters but was provided {2}.",
                                Info.Name,
                                requirements.Length,
                                parameters.Length)
                            );
                    throw;
                }
            }
        }

        /// <summary>
        /// A function implementation of <see cref="IMethod"/>.
        /// </summary>
        private class Function : MethodProxyBase
        {
            private readonly LateBoundFunc _theDelegate;

            /// <summary>
            /// Initializes a new instance of the <see cref="Function"/> class.
            /// </summary>
            /// <param name="info">The info.</param>
            /// <param name="threadPool">The thread pool.</param>
            public Function(MethodInfo info, IThreadPool threadPool)
                : base(info, threadPool)
            {
                _theDelegate = DelegateFactory.Create<LateBoundFunc>(info);
            }

            /// <summary>
            /// Invokes the specified method on the provided instance with the given parameters.
            /// </summary>
            /// <param name="instance">The instance.</param>
            /// <param name="parameters">The parameters.</param>
            /// <returns>
            /// The result of the function or null if it is a procedure.
            /// </returns>
            public override object Invoke(object instance, params object[] parameters)
            {
                return SafeInvoke(instance, parameters);
            }

            /// <summary>
            /// Creates a background task for executing this method asynchronously.
            /// </summary>
            /// <param name="instance">The instance.</param>
            /// <param name="parameters">The parameters.</param>
            /// <returns>
            /// An instance of <see cref="IBackgroundTask"/>.
            /// </returns>
            public override IBackgroundTask CreateBackgroundTask(object instance, params object[] parameters)
            {
                return new BackgroundTask(_threadPool, () => SafeInvoke(instance, parameters));
            }

            private object SafeInvoke(object instance, object[] parameters)
            {
                try
                {
                    return _theDelegate(instance, parameters);
                }
                catch(Exception)
                {
                    var requirements = Info.GetParameters();

                    if (requirements.Length != parameters.Length)
                        throw new CaliburnException(
                            string.Format(
                                "The method '{0}' expected {1} parameters but was provided {2}.",
                                Info.Name,
                                requirements.Length,
                                parameters.Length)
                            );
                    throw;
                }
            }
        }
    }
}