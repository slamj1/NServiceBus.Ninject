using Ninject;
using Ninject.Extensions.ContextPreservation;
using Ninject.Extensions.NamedScope;
using NServiceBus.ContainerTests;
using NServiceBus.ObjectBuilder.Common;
using NServiceBus.ObjectBuilder.Ninject;
using NUnit.Framework;

[SetUpFixture]
public class SetUpFixture
{
    public SetUpFixture()
    {
        TestContainerBuilder.ConstructBuilder = ConstructNinjectObjectBuilder;
    }

    static IContainer ConstructNinjectObjectBuilder()
    {
        var ninjectSettings = new NinjectSettings { LoadExtensions = false };
        var contextPreservationModule = new ContextPreservationModule();
        var namedScopeModule = new NamedScopeModule();
        var standardKernel = new StandardKernel(ninjectSettings,contextPreservationModule, namedScopeModule);
        return new NinjectObjectBuilder(standardKernel, true);
    }
}