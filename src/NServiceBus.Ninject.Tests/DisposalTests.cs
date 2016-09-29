namespace NServiceBus.Ninject.Tests
{
    using global::Ninject;
    using ObjectBuilder.Ninject;
    using NUnit.Framework;

    [TestFixture]
    public class DisposalTests
    {
        [Test]
        public void Owned_container_should_be_disposed()
        {
            var kernel = new StandardKernel();

            var container = new NinjectObjectBuilder(kernel, true);
            container.Dispose();

            Assert.True(kernel.IsDisposed);
        }

        [Test]
        public void Externally_owned_container_should_not_be_disposed()
        {
            var kernel = new StandardKernel();

            var container = new NinjectObjectBuilder(kernel, false);
            container.Dispose();

            Assert.False(kernel.IsDisposed);
        }
    }
}