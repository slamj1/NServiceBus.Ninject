namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using Features;
    using global::Ninject;
    using Hosting.Helpers;
    using ObjectBuilder;

    public class DefaultServer : IEndpointSetupTemplate
    {
        public DefaultServer()
        {
            typesToInclude = new List<Type>();
        }

        public DefaultServer(List<Type> typesToInclude)
        {
            this.typesToInclude = typesToInclude;
        }

#pragma warning disable 618
        public Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Action<EndpointConfiguration> configurationBuilderCustomization)
#pragma warning restore 618
        {
            var types = GetTypesScopedByTestClass(endpointConfiguration);

            typesToInclude.AddRange(types);

            var builder = new EndpointConfiguration(endpointConfiguration.EndpointName);

            builder.TypesToIncludeInScan(typesToInclude);
            builder.EnableInstallers();

            builder.DisableFeature<TimeoutManager>();
            builder.UsePersistence<InMemoryPersistence>();
            var settings = new NinjectSettings
            {
                // avoid https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/mitigation-deserialization-of-objects-across-app-domains
                LoadExtensions = false
            };
            var kernel = new StandardKernel(settings);
            builder.UseContainer<NinjectBuilder>(c => c.ExistingKernel(kernel));
            builder.UseTransport<LearningTransport>();

            builder.Recoverability().Delayed(delayedRetries => delayedRetries.NumberOfRetries(0));
            builder.Recoverability().Immediate(immediateRetries => immediateRetries.NumberOfRetries(0));

            builder.RegisterComponents(r => { RegisterInheritanceHierarchyOfContextOnContainer(runDescriptor, r); });

            configurationBuilderCustomization(builder);


            return Task.FromResult(builder);
        }

        static void RegisterInheritanceHierarchyOfContextOnContainer(RunDescriptor runDescriptor, IConfigureComponents r)
        {
            var type = runDescriptor.ScenarioContext.GetType();
            while (type != typeof(object))
            {
                r.RegisterSingleton(type, runDescriptor.ScenarioContext);
                type = type.BaseType;
            }
        }

        static IEnumerable<Type> GetTypesScopedByTestClass(EndpointCustomizationConfiguration endpointConfiguration)
        {
            var assemblies = new AssemblyScanner().GetScannableAssemblies();

            var types = assemblies.Assemblies
                //exclude all test types by default
                .Where(a =>
                {
                    var references = a.GetReferencedAssemblies();

                    return references.All(an => an.Name != "nunit.framework");
                })
                .SelectMany(a => a.GetTypes());


            types = types.Union(GetNestedTypeRecursive(endpointConfiguration.BuilderType.DeclaringType, endpointConfiguration.BuilderType));

            types = types.Union(endpointConfiguration.TypesToInclude);

            return types.Where(t => !endpointConfiguration.TypesToExclude.Contains(t)).ToList();
        }

        static IEnumerable<Type> GetNestedTypeRecursive(Type rootType, Type builderType)
        {
            if (rootType == null)
            {
                throw new InvalidOperationException("Make sure you nest the endpoint infrastructure inside the TestFixture as nested classes");
            }

            yield return rootType;

            if (typeof(IEndpointConfigurationFactory).IsAssignableFrom(rootType) && rootType != builderType)
            {
                yield break;
            }

            foreach (var nestedType in rootType.GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SelectMany(t => GetNestedTypeRecursive(t, builderType)))
            {
                yield return nestedType;
            }
        }

        List<Type> typesToInclude;
    }
}