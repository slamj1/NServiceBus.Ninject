using Ninject;
using Ninject.Extensions.ContextPreservation;
using Ninject.Extensions.NamedScope;
using NServiceBus.ContainerTests;
using NServiceBus.ObjectBuilder.Ninject;
using NUnit.Framework;

[SetUpFixture]
public class SetUpFixture
{
    [SetUp]
    public void Setup()
    {
        TestContainerBuilder.ConstructBuilder = () => new NinjectObjectBuilder(new StandardKernel(new NinjectSettings {LoadExtensions = false},
                                                                new ContextPreservationModule(), new NamedScopeModule()));
    }

}