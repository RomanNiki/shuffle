using App.Scripts.Generator.Handlers;
using App.Scripts.Generator.Services;
using App.Scripts.Random.Providers;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Scripts.Generator.Bootstrap
{
    public class InstallerMatrix : LifetimeScope
    {
        [SerializeField] private ConfigSorterGroups configSorterGroups;
        [SerializeField] private ConfigServiceSorter configServiceSorter;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            builder.Register<HandlerSorterGroups>(Lifetime.Scoped).As<IHandlerSorter>().WithParameter(configSorterGroups);


            builder.Register<ServiceSorter>(Lifetime.Singleton).WithParameter(configServiceSorter)
                .AsImplementedInterfaces();
            
            builder.Register<ProviderRandom>(Lifetime.Singleton).WithParameter(111).AsImplementedInterfaces();
        }
    }
}