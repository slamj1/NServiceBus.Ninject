namespace NServiceBus
{
    using Container;
    using Ninject;
    using ObjectBuilder.Common;
    using ObjectBuilder.Ninject;
    using Settings;

    /// <summary>
    /// Ninject Container
    /// </summary>
    public class NinjectBuilder : ContainerDefinition
    {
        /// <summary>
        /// Implementers need to new up a new container.
        /// </summary>
        /// <param name="settings">The settings to check if an existing container exists.</param>
        /// <returns>The new container wrapper.</returns>
        public override IContainer CreateContainer(ReadOnlySettings settings)
        {
            KernelHolder kernelHolder;

            if (settings.TryGet(out kernelHolder))
            {
                settings.AddStartupDiagnosticsSection("NServiceBus.Ninject", new
                {
                    UsingExistingKernel = true
                });

                return new NinjectObjectBuilder(kernelHolder.ExistingKernel);
            }

            settings.AddStartupDiagnosticsSection("NServiceBus.Ninject", new
            {
                UsingExistingKernel = false
            });

            return new NinjectObjectBuilder();
        }

        internal class KernelHolder
        {
            public KernelHolder(IKernel kernel)
            {
                ExistingKernel = kernel;
            }

            public IKernel ExistingKernel { get; }
        }
    }
}