namespace NServiceBus.Features
{
    /// <summary>
    /// Adds Diagnostics information
    /// </summary>
    public class NinjectDiagnostics : Feature
    {
        /// <summary>
        /// Constructor for diagnostics feature
        /// </summary>
        public NinjectDiagnostics()
        {
            EnableByDefault();
        }

        /// <summary>
        /// Sets up diagnostics
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Settings.AddStartupDiagnosticsSection("NServiceBus.Ninject", new
            {
                UsingExistingKernel = context.Settings.HasSetting<NinjectBuilder.KernelHolder>()
            });
        }
    }
}
