namespace NServiceBus.AcceptanceTests
{
    using Ninject;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_using_externally_owned_container : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_shutdown_properly()
        {
            Scenario.Define<Context>()
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsFalse(Endpoint.Kernel.IsDisposed);
            Assert.DoesNotThrow(() => Endpoint.Kernel.Dispose());
        }

        class Context : ScenarioContext
        {
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public static IKernel Kernel { get; set; }

            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    var kernel = new StandardKernel();

                    config.UseContainer<NinjectBuilder>(c => c.ExistingKernel(kernel));

                    Kernel = kernel;
                });
            }
        }
    }
}