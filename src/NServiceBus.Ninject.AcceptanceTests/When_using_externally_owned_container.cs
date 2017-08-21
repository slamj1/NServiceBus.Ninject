namespace NServiceBus.Ninject.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using global::Ninject;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_using_externally_owned_container : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_shutdown_properly()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsFalse(context.Kernel.IsDisposed);
            Assert.DoesNotThrow(() => context.Kernel.Dispose());
        }

        class Context : ScenarioContext
        {
            public IKernel Kernel { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((config, desc) =>
                {
                    config.SendFailedMessagesTo("error");
                    var setttings = new NinjectSettings()
                    {
                        // avoid https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/mitigation-deserialization-of-objects-across-app-domains
                        LoadExtensions = false
                    };
                    var kernel = new StandardKernel(setttings);

                    config.UseContainer<NinjectBuilder>(c => c.ExistingKernel(kernel));

                    var context = (Context) desc.ScenarioContext;
                    context.Kernel = kernel;
                });
            }
        }
    }
}