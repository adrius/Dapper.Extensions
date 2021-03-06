﻿using Autofac;
using Dapper.Extensions.MasterSlave;
using Dapper.Extensions.SQL;

namespace Dapper.Extensions
{
    public static class AutofacExtensions
    {
        public static ContainerBuilder AddDapper<TDbProvider>(this ContainerBuilder container, string connectionName = "DefaultConnection", string serviceKey = null, bool enableMasterSlave = false) where TDbProvider : IDapper
        {
            container.RegisterType<ResolveContext>().As<IResolveContext>().IfNotRegistered(typeof(IResolveContext)).InstancePerLifetimeScope();
            container.RegisterType<ResolveKeyed>().As<IResolveKeyed>().IfNotRegistered(typeof(IResolveKeyed)).InstancePerLifetimeScope();
            container.RegisterType<ConnectionConfigureManager>().IfNotRegistered(typeof(ConnectionConfigureManager)).SingleInstance();
            container.RegisterType<WeightedPolling>().As<ILoadBalancing>().IfNotRegistered(typeof(ILoadBalancing)).SingleInstance();

            if (string.IsNullOrWhiteSpace(serviceKey))
            {
                if (enableMasterSlave)
                {
                    container.RegisterType<TDbProvider>().As<IDapper>().WithParameters(new[] { new NamedParameter("connectionName", connectionName), new NamedParameter("enableMasterSlave", true) }).InstancePerLifetimeScope();
                    container.RegisterType<TDbProvider>().Keyed<IDapper>("_slave").WithParameters(new[] { new NamedParameter("connectionName", connectionName), new NamedParameter("enableMasterSlave", true) }).InstancePerLifetimeScope();
                }
                else
                    container.RegisterType<TDbProvider>().As<IDapper>().WithParameter("connectionName", connectionName)
                        .InstancePerLifetimeScope();
            }
            else
            {
                if (enableMasterSlave)
                {
                    container.RegisterType<TDbProvider>().Keyed<IDapper>(serviceKey).WithParameters(new[] { new NamedParameter("connectionName", connectionName), new NamedParameter("enableMasterSlave", true) }).InstancePerLifetimeScope();
                    container.RegisterType<TDbProvider>().Keyed<IDapper>($"{serviceKey}_slave").WithParameters(new[] { new NamedParameter("connectionName", connectionName), new NamedParameter("enableMasterSlave", true) }).InstancePerLifetimeScope();
                }
                else
                    container.RegisterType<TDbProvider>().Keyed<IDapper>(serviceKey).WithParameter("connectionName", connectionName).InstancePerLifetimeScope();
            }
            return container;
        }

        /// <summary>
        /// Enable SQL separation
        /// </summary>
        /// <param name="services"></param>
        /// <param name="xmlRootDir">The root directory of the xml file</param>
        /// <returns></returns>
        public static ContainerBuilder AddSQLSeparationForDapper(this ContainerBuilder services, string xmlRootDir)
        {
            services.RegisterInstance(new SQLSeparateConfigure
            {
                RootDir = xmlRootDir
            });
            services.RegisterType<SQLManager>().As<ISQLManager>().SingleInstance();
            return services;
        }
    }
}
