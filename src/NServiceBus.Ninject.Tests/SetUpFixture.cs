using NServiceBus.ContainerTests;
using NServiceBus.ObjectBuilder.Ninject;
using NUnit.Framework;

[SetUpFixture]
public class SetUpFixture
{
    [SetUp]
    public void Setup()
    {
        TestContainerBuilder.ConstructBuilder = () => new NinjectObjectBuilder();
    }

}