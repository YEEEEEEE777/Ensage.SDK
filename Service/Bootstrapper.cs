// <copyright file="Bootstrapper.cs" company="Ensage">
//    Copyright (c) 2018 Ensage.
// </copyright>

namespace Ensage.SDK.Service
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using Ensage.SDK.Extensions;
    using Ensage.SDK.Helpers;
    using Ensage.SDK.Logger;
    using Ensage.SDK.Service.Metadata;

    using NLog;

    using PlaySharp.Toolkit.AppDomain.Loader;
    using PlaySharp.Toolkit.Helper;

    public class Bootstrapper : AssemblyEntryPoint
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public Bootstrapper()
        {
            Logging.Init();
        }

        public ContextContainer<IServiceContext> Default { get; private set; }

        public List<PluginContainer> PluginContainer { get; } = new List<PluginContainer>();

        public IEnumerable<Lazy<IPluginLoader, IPluginLoaderMetadata>> Plugins
        {
            get
            {
                return this.Default.GetExports<IPluginLoader, IPluginLoaderMetadata>();
            }
        }

        private EnsageServiceContext Context { get; set; }

        public void BuildUp(object instance)
        {
            this.Default.BuildUp(instance);
        }

        public IEnumerable<object> GetAllInstances(Type serviceType)
        {
            return this.Default.GetAllInstances(serviceType);
        }

        public object GetInstance(Type serviceType, string key)
        {
            return this.Default.GetInstance(serviceType, key);
        }

        protected override void OnActivated()
        {
            UpdateManager.Subscribe(this.OnLoad);
        }

        protected override void OnDeactivated()
        {
            try
            {
                if (this.Context.menuManager.IsValueCreated)
                {
                    this.Context.MenuManager.Dispose();
                }

                if (this.Context.rendererManager.IsValueCreated)
                {
                    this.Context.RenderManager.Dispose();
                }
            }
            catch (Exception e)
            {
                Log.Warn(e);
            }
        }

        private async Task ActivatePluginsTask()
        {
            var sw = Stopwatch.StartNew();
            var activationTime = new Stopwatch();

            foreach (var plugin in this.PluginContainer.Where(e => e.Menu && !e.IsActive).OrderBy(e => e.Metadata.Priority))
            {
                activationTime.Restart();
                plugin.Activate();
                activationTime.Stop();

                Log.Info($"Activated {plugin.Metadata.Name} in {activationTime.Elapsed}");

                if (sw.Elapsed.Seconds >= 2)
                {
                    await Task.Delay(500);
                    sw.Restart();
                }
            }
        }

        private void AddPlugin(Lazy<IPluginLoader, IPluginLoaderMetadata> assembly)
        {
            try
            {
                Log.Debug($"Found {assembly.Metadata.Name}");

                if (assembly.Metadata.IsSupported())
                {
                    this.PluginContainer.Add(new PluginContainer(this.Context.Config.Plugins.Factory, assembly));
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void DeactivatePlugins()
        {
            foreach (var plugin in this.PluginContainer.OrderByDescending(e => e.Metadata.Priority))
            {
                try
                {
                    plugin.Deactivate();
                }
                catch (Exception e)
                {
                    Log.Warn(e);
                }
            }
        }

        private async Task DiscoverPlugins()
        {
            var sw = Stopwatch.StartNew();

            foreach (var assembly in this.Plugins.OrderBy(e => e.Metadata.Priority))
            {
                this.AddPlugin(assembly);

                if (sw.Elapsed.Seconds >= 2)
                {
                    await Task.Delay(500);
                    sw.Restart();
                }
            }
        }

        private void OnAssemblyLoad(object sender, Assembly assembly)
        {
            foreach (var plugin in this.Plugins.Where(e => e.Value.GetType().Assembly == assembly))
            {
                this.AddPlugin(plugin);
            }
        }

        private async void OnBootstrap()
        {
            try
            {
                if (ObjectManager.LocalHero == null)
                {
                    throw new Exception("Local Hero is null");
                }

                var sw = Stopwatch.StartNew();

                Log.Debug("====================================================");
                Log.Debug($">> Ensage.SDK Bootstrap started");
                Log.Debug("====================================================");

                Game.UnhandledException += this.OnUnhandledException;

                Log.Debug($">> Building Context for LocalHero");
                this.Context = new EnsageServiceContext(ObjectManager.LocalHero);

                this.Default = this.Context.Container;
                this.Default.RegisterValue(this);

                Log.Debug($">> Initializing Services");
                IoC.Initialize(this.BuildUp, this.GetInstance, this.GetAllInstances);

                Log.Debug($">> Searching Plugins");
                await this.DiscoverPlugins();
                await Task.Delay(1000);

                Log.Debug($">> Activating Plugins");
                await this.ActivatePluginsTask();

                sw.Stop();

                Log.Debug("====================================================");
                Log.Debug($">> Bootstrap completed in {sw.Elapsed}");
                Log.Debug("====================================================");

                ContainerFactory.Loader.AssemblyLoad += this.OnAssemblyLoad;
            }
            catch (ReflectionTypeLoadException e)
            {
                foreach (var exception in e.LoaderExceptions)
                {
                    Log.Fatal(exception);
                }
            }
            catch (Exception e)
            {
                Log.Fatal(e);
            }
        }

        private void OnLoad()
        {
            if (ObjectManager.LocalHero == null)
            {
                return;
            }

            UpdateManager.Unsubscribe(this.OnLoad);
            UpdateManager.BeginInvoke(this.OnBootstrap);
        }

        private void OnUnhandledException(object sender, Exception e)
        {
            Log.Error(e);
        }
    }
}
