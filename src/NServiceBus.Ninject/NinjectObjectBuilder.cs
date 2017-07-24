namespace NServiceBus.ObjectBuilder.Ninject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::Ninject;
    using global::Ninject.Activation;
    using global::Ninject.Infrastructure;
    using global::Ninject.Injection;
    using global::Ninject.Parameters;
    using global::Ninject.Planning.Bindings;
    using global::Ninject.Selection;
    using Common;
    using Internal;

    class NinjectObjectBuilder : IContainer
    {
        public NinjectObjectBuilder()
            : this(new StandardKernel(), true)
        {
        }

        public NinjectObjectBuilder(IKernel kernel)
            : this(kernel, false)
        {
        }

        public NinjectObjectBuilder(IKernel kernel, bool owned)
        {
            this.kernel = kernel;
            this.owned = owned;

            RegisterNecessaryBindings();

            propertyHeuristic = this.kernel.Get<IObjectBuilderPropertyHeuristic>();

            AddCustomPropertyInjectionHeuristic();

            this.kernel
                .Bind<NinjectChildContainer>()
                .ToSelf()
                .DefinesNinjectObjectBuilderScope();
        }

        public object Build(Type typeToBuild)
        {
            if (!HasComponent(typeToBuild))
            {
                throw new ArgumentException(typeToBuild + " is not registered in the container");
            }

            return kernel.Get(typeToBuild);
        }

        public IContainer BuildChildContainer()
        {
            return kernel.Get<NinjectChildContainer>();
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            if (HasComponent(typeToBuild))
            {
                return kernel.GetAll(typeToBuild);
            }

            return Enumerable.Empty<object>();
        }

        public void Configure(Type component, DependencyLifecycle dependencyLifecycle)
        {
            if (HasComponent(component))
            {
                return;
            }

            var instanceScope = GetInstanceScopeFrom(dependencyLifecycle);

            var isInstancePerUnitOfWork = dependencyLifecycle == DependencyLifecycle.InstancePerUnitOfWork;
            var bindingConfigurations = BindComponentToItself(component, instanceScope, isInstancePerUnitOfWork);
            AddAliasesOfComponentToBindingConfigurations(component, bindingConfigurations);

            propertyHeuristic.RegisteredTypes.Add(component);
        }

        public void Configure<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {
            var componentType = typeof(T);

            if (HasComponent(componentType))
            {
                return;
            }

            var instanceScope = GetInstanceScopeFrom(dependencyLifecycle);

            var isInstancePerUnitOfWork = dependencyLifecycle == DependencyLifecycle.InstancePerUnitOfWork;
            var bindingConfigurations = BindComponentToMethod(componentFactory, instanceScope, isInstancePerUnitOfWork);
            AddAliasesOfComponentToBindingConfigurations(componentType, bindingConfigurations);

            propertyHeuristic.RegisteredTypes.Add(componentType);
        }

        public void RegisterSingleton(Type lookupType, object instance)
        {
            if (propertyHeuristic.RegisteredTypes.Contains(lookupType))
            {
                kernel
                    .Rebind(lookupType)
                    .ToConstant(instance);
                return;
            }

            propertyHeuristic
                .RegisteredTypes
                .Add(lookupType);

            kernel
                .Bind(lookupType)
                .ToConstant(instance);
        }

        public bool HasComponent(Type componentType)
        {
            var request = kernel.CreateRequest(componentType, null, new IParameter[0], false, true);

            return kernel.CanResolve(request);
        }

        public void Release(object instance)
        {
            kernel.Release(instance);
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        static IEnumerable<Type> GetAllServiceTypesFor(Type component)
        {
            if (component == null)
            {
                return new List<Type>();
            }

            var result = new List<Type>(component.GetInterfaces())
            {
                component
            };

            foreach (var interfaceType in component.GetInterfaces())
            {
                result.AddRange(GetAllServiceTypesFor(interfaceType));
            }

            return result.Distinct();
        }

        void DisposeManaged()
        {
            if (!owned)
            {
                return;
            }

            if (kernel == null)
            {
                return;
            }

            if (!kernel.IsDisposed)
            {
                kernel.Dispose();
            }
        }

        Func<IContext, object> GetInstanceScopeFrom(DependencyLifecycle dependencyLifecycle)
        {
            Func<IContext, object> scope;

            if (!dependencyLifecycleToScopeMapping.TryGetValue(dependencyLifecycle, out scope))
            {
                throw new ArgumentException("The dependency lifecycle is not supported", "dependencyLifecycle");
            }

            return scope;
        }

        void AddAliasesOfComponentToBindingConfigurations(Type component, IEnumerable<IBindingConfiguration> bindingConfigurations)
        {
            var services = GetAllServiceTypesFor(component).Where(t => t != component);

            foreach (var service in services)
            {
                foreach (var bindingConfiguration in bindingConfigurations)
                {
                    kernel.AddBinding(new Binding(service, bindingConfiguration));
                }
            }
        }

        IEnumerable<IBindingConfiguration> BindComponentToItself(Type component, Func<IContext, object> instanceScope, bool addChildContainerScope)
        {
            var bindingConfigurations = new List<IBindingConfiguration>();
            if (addChildContainerScope)
            {
                var instanceScopeConfiguration = kernel
                    .Bind(component)
                    .ToSelf()
                    .WhenNotInUnitOfWork()
                    .InScope(instanceScope)
                    .BindingConfiguration;
                bindingConfigurations.Add(instanceScopeConfiguration);

                var unitOfWorkConfiguration = kernel
                    .Bind(component)
                    .ToSelf()
                    .WhenInUnitOfWork()
                    .InUnitOfWorkScope()
                    .BindingConfiguration;
                bindingConfigurations.Add(unitOfWorkConfiguration);
            }
            else
            {
                var instanceScopeConfiguration = kernel
                    .Bind(component)
                    .ToSelf()
                    .InScope(instanceScope)
                    .BindingConfiguration;
                bindingConfigurations.Add(instanceScopeConfiguration);
            }

            return bindingConfigurations;
        }

        IEnumerable<IBindingConfiguration> BindComponentToMethod<T>(Func<T> component, Func<IContext, object> instanceScope, bool addChildContainerScope)
        {
            var bindingConfigurations = new List<IBindingConfiguration>();
            if (addChildContainerScope)
            {
                var instanceScopeConfiguration = kernel
                    .Bind<T>()
                    .ToMethod(context => component.Invoke())
                    .WhenNotInUnitOfWork()
                    .InScope(instanceScope)
                    .BindingConfiguration;
                bindingConfigurations.Add(instanceScopeConfiguration);

                var unitOfWorkConfiguration = kernel
                    .Bind<T>()
                    .ToMethod(context => component.Invoke())
                    .WhenInUnitOfWork()
                    .InUnitOfWorkScope()
                    .BindingConfiguration;
                bindingConfigurations.Add(unitOfWorkConfiguration);
            }
            else
            {
                var instanceScopeConfiguration = kernel
                    .Bind<T>()
                    .ToMethod(context => component.Invoke())
                    .InScope(instanceScope)
                    .BindingConfiguration;
                bindingConfigurations.Add(instanceScopeConfiguration);
            }

            return bindingConfigurations;
        }

        void AddCustomPropertyInjectionHeuristic()
        {
            var selector = kernel.Components.Get<ISelector>();

            selector.InjectionHeuristics.Add(
                kernel.Get<IObjectBuilderPropertyHeuristic>());
        }

        void RegisterNecessaryBindings()
        {
            kernel
                .Bind<IContainer>()
                .ToConstant(this)
                .InSingletonScope();

            kernel
                .Bind<IObjectBuilderPropertyHeuristic>()
                .To<ObjectBuilderPropertyHeuristic>()
                .InSingletonScope()
                .WithPropertyValue("Settings", context => context.Kernel.Settings);

            kernel
                .Bind<IInjectorFactory>()
                .ToMethod(context => context.Kernel.Components.Get<IInjectorFactory>());
        }

        IDictionary<DependencyLifecycle, Func<IContext, object>> dependencyLifecycleToScopeMapping =
            new Dictionary<DependencyLifecycle, Func<IContext, object>>
            {
                {DependencyLifecycle.SingleInstance, StandardScopeCallbacks.Singleton},
                {DependencyLifecycle.InstancePerCall, StandardScopeCallbacks.Transient},
                {DependencyLifecycle.InstancePerUnitOfWork, StandardScopeCallbacks.Singleton}
            };

        IKernel kernel;
        IObjectBuilderPropertyHeuristic propertyHeuristic;
        bool owned;
    }
}