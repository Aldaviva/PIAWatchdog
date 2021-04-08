using System;
using System.Reflection;
using Autofac;

namespace PIAWatchdog.Injection
{
    public static class ContainerFactory
    {
        public static IContainer CreateContainer()
        {
            var containerBuilder = new ContainerBuilder();
            Assembly assembly = Assembly.GetExecutingAssembly();

            containerBuilder.RegisterAssemblyTypes(assembly)
                .Where(t => t.GetCustomAttribute<ComponentAttribute>() != null)
                .AsImplementedInterfaces()
                .SingleInstance()
                .OnActivated(eventArgs =>
                {
                    eventArgs.Instance.GetType()
                        .GetMethod("PostConstruct", new Type[0])?
                        .Invoke(eventArgs.Instance, new object[0]);
                });

            containerBuilder.RegisterAssemblyModules(assembly);

            return containerBuilder.Build();
        }
    }
}