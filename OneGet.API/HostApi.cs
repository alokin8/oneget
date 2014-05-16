// 
//  Copyright (c) Microsoft Corporation. All rights reserved. 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  

namespace Microsoft.OneGet {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Core.Api;
    using Core.AppDomains;
    using Core.DuckTyping;
    using Core.Extensions;
    using Core.Packaging;
    using Core.Tasks;
    using Callback = System.Func<string, System.Collections.Generic.IEnumerable<object>, object>;

    /// <summary>
    ///     PROTOTYPE -- This API is nowhere near what the actual public API will resemble
    ///     The (far off ) Actual API must be built to support native clients, and the managed wrapper
    ///     Will talk to it, so we want the interim managed API to resemble that hopefully.
    ///     In the mean time, this is suited just to what I need for the cmdlets (the only
    ///     'public' api in the short term)
    ///     The Client API is designed for use by installation hosts:
    ///     - OneGet Powershell Cmdlets
    ///     - WMI/OMI Management interfaces
    ///     - DSC Interfaces
    ///     - WiX's Burn
    ///     The Client API provides high-level consumer functions to support SDII functionality.
    /// </summary>
    public static class HostApi {
        internal static IDictionary<string, Provider> Providers = new Dictionary<string, Provider>();
        // internal static IDictionary<string, Func<Provider>> _providerFactory = new Dictionary<string, Func<Provider>>();

        private static bool _isInitialized;
        private static readonly Dictionary<Assembly, PluginDomain> _domains = new Dictionary<Assembly, PluginDomain>();
        private static object _lockObject = new object();

        public static IEnumerable<string> ProviderNames {
            get {
                return Providers.Keys;
            }
        }

        public static IEnumerable<Provider> AllProviders {
            get {
                return Providers.Values;
            }
        }

        /// <summary>
        ///     STATUS: PROTOTYPE METHOD.
        ///     This initializes the provider registry with the list of provider plugins, pulled from the powershell module.
        ///     In the long run, this should not rely on being called from the
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="pluginAssemblies"></param>
        public static bool Init(Callback callback, IEnumerable<string> pluginAssemblies, bool okToBootstrapNuGet ) {
            if (!_isInitialized) {
                lock (_lockObject) {
                    try {
                        if (Instance.Service.GetNuGetDllPath().IsEmptyOrNull()) {
                            // we are unable to bootstrap NuGet correctly.
                            // We can't really declare that the plugins are ready, and we should just 
                            // return as if we never really succeded (as it may have been that this got called as 
                            // the result of a tab-completion and we can't fully bootstrap if that was the case.
                            Event<Error>.Raise("Bad", "NuGet is required");
                            return false;
                        }
                    } catch {
                        return false;
                    }

                    if (!_isInitialized) {

                        // var wrappedFunc = new WrappedFunc<string, IEnumerable<string>>((str) => configurationData.GetStringCollection(str).ByRef());

                        // there is no trouble with loading plugins concurrently.
                        Parallel.ForEach(pluginAssemblies, plugin => {
                            try {
                                if (TryToLoadPluginAssembly(callback, plugin)) {
                                    Event<Verbose>.Raise("Loading Plugin", plugin);
                                } else {
                                    Event<Warning>.Raise("Failed to load any providers from plugin", plugin);
                                }
                            } catch (Exception e) {
                                Event<ExceptionThrown>.Raise(e.GetType().Name, e.Message, e.StackTrace);
                            }
                        });
                        _isInitialized = true;
                    }
                }
            }
            return _isInitialized;
        }

        public static IEnumerable<PackageSource> GetAllSourceNames(Callback callback) {
            return Providers.Values.SelectMany(each => each.GetPackageSources(callback));
        }

        public static IEnumerable<Provider> SelectProviders(string providerName) {
            return SelectProviders(providerName, null);
        }

        public static IEnumerable<Provider> SelectProviders(string providerName, IEnumerable<string> sourceNames) {
            var providers = AllProviders;

            if (providerName.Is()) {
                // strict name match for now.
                providers = providers.Where(each => each.CachedProviderName.Equals(providerName, StringComparison.CurrentCultureIgnoreCase));
            }
            /*
            if (!sourceNames.IsNullOrEmpty()) {
                // let the providers select which ones are good.
                providers = providers.Where(each => each.IsValidPackageSource.IsSupported() && sourceNames.Any(sourceName => each.IsValidPackageSource(sourceName)));
            }
            */
            return providers;
        }

        /// <summary>
        ///     Searches for the assembly, interrogates it for it's providers and then proceeds to load
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="pluginAssemblyName"></param>
        /// <returns></returns>
        private static bool TryToLoadPluginAssembly(Callback callback, string pluginAssemblyName) {
            // find all the matches for the assembly specified, order by version (descending)
            var assemblyPath = FindAssembly(pluginAssemblyName);

            if (assemblyPath == null) {
                return false;
            }

            var loadedAssembly = LoadAssembly(assemblyPath);
            var result = false;

            if (loadedAssembly != null) {
                // check to see if the assembly has something that looks like a Plugin class
                var pluginTypes = loadedAssembly.GetTypes().Where(each => each.IsPublic && each.BaseType != typeof (MulticastDelegate) && typeof (Plugin).IsTypeCompatible(each)).ToArray();
                if (pluginTypes.Length > 0) {
                    foreach (var pluginType in pluginTypes) {
                        var pluginDomain = GetPluginDomain(loadedAssembly);
                        // if there is one, instantiate it.

                        // load this assembly into the target domain.
                        var asm = pluginDomain.LoadFileWithReferences(Assembly.GetExecutingAssembly().Location);

                        // get a plugin object from the other domain
                        var plugin = pluginDomain.InvokeFunc((type) => new Plugin(type), pluginType);

                        // todo: we need to pass some delegates to the provider to report progress, errors, etc.
                        // todo: perhaps that should be in the initProvider delegate? (since we're going to call that before we do any real work.)

                        // start up the plugin.
                        try {
                            plugin.InitPlugin(callback);
                        } catch (Exception e) {
                            e.Dump();
                            Event<Warning>.Raise("PLUGIN", "Plugin '{0}' failed during initialization.", new object[] {
                                plugin.PluginName
                            });
                            // plugin failed to init correctly.
                            // skip it.
                            continue;
                        }

                        // ask for the collection of PackageProviders
                        foreach (var name in plugin.PackageProviderNames) {
                            // gonna need a local copy
                            var providerName = name;

                            if (providerName.Is()) {
                                var provider = plugin.CreatePackageProvider(providerName);
                                var x = provider.GetProviderName(callback);
                                Event<Verbose>.Raise("LOADING PROVIDER", providerName);
                                Providers.AddOrSet(providerName, provider);
                                result = true;
                            } else {
                                Event<Warning>.Raise("Issue", "Plugin '{0}' returned a Provider '{1}' that doesn't meet the requirements.", new object[] {
                                    plugin.PluginName, providerName.GetType().Name
                                });
                            }
                        }
                    }
                } else {
                    // There are no plugins in this assembly
                    // perhaps there are package providers in here we can just instantiate.
                    // TODO : load providers directly when no plugin class is around
                }
            }

            return result;
        }

        private static PluginDomain GetPluginDomain(Assembly assembly) {
            return _domains[assembly];
        }

#if AFTER_CTP
        private static void UnloadAssembly(Assembly assembly) {
            PluginDomain pd = null;
            try {
                lock (_domains) {
                    pd = _domains[assembly];
                    _domains.Remove(assembly);
                }
            } catch (Exception e) {
                e.Dump();
            }
            if (pd != null) {
                ((IDisposable)pd).Dispose();
            }
            pd = null;
        }
#endif

        /// <summary>
        ///     PROTOTYPE - Quick and dirty assembly/plugin loader.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static Assembly LoadAssembly(string path) {
            try {
                // this needs to load the assembly in it's own domain
                // so that we can drop them when necessary.
                var pd = new PluginDomain();

                // add event listeners to the new appdomain.
                pd.Invoke(c => {CurrentTask.Events += new Verbose(c.Invoke);}, typeof (Verbose).CreateWrappedProxy(new Verbose((s, f, o) => Event<Verbose>.Raise(s, f, o.ByRef()))) as WrappedFunc<string, string, IEnumerable<object>, bool>);
                pd.Invoke(c => {CurrentTask.Events += new Warning(c.Invoke);}, typeof (Warning).CreateWrappedProxy(new Warning((s, f, o) => Event<Warning>.Raise(s, f, o.ByRef()))) as WrappedFunc<string, string, IEnumerable<object>, bool>);
                pd.Invoke(c => {CurrentTask.Events += new Message(c.Invoke);}, typeof (Message).CreateWrappedProxy(new Message((s, f, o) => Event<Message>.Raise(s, f, o.ByRef()))) as WrappedFunc<string, string, IEnumerable<object>, bool>);
                pd.Invoke(c => {CurrentTask.Events += new Error(c.Invoke);}, typeof (Error).CreateWrappedProxy(new Error((s, f, o) => Event<Error>.Raise(s, f, o.ByRef()))) as WrappedFunc<string, string, IEnumerable<object>, bool>);
                pd.Invoke(c => {CurrentTask.Events += new Debug(c.Invoke);}, typeof (Debug).CreateWrappedProxy(new Debug((s, f, o) => Event<Debug>.Raise(s, f, o.ByRef()))) as WrappedFunc<string, string, IEnumerable<object>, bool>);
                pd.Invoke(c => {CurrentTask.Events += new Progress(c.Invoke);},
                    typeof (Progress).CreateWrappedProxy(new Progress((id, s, i, f, o) => Event<Progress>.Raise(id, s, i, f, o.ByRef()))) as WrappedFunc<int, string, int, string, IEnumerable<object>, bool>);
                pd.Invoke(c => {CurrentTask.Events += new ProgressComplete(c.Invoke);},
                    typeof (ProgressComplete).CreateWrappedProxy(new ProgressComplete((id, s, f, o) => Event<ProgressComplete>.Raise(id, s, f, o))) as WrappedFunc<int, string, string, IEnumerable<object>, bool>);
                pd.Invoke(c => {CurrentTask.Events += new ExceptionThrown(c.Invoke);},
                    typeof (ExceptionThrown).CreateWrappedProxy(new ExceptionThrown((e, m, s) => Event<ExceptionThrown>.Raise(e, m, s))) as WrappedFunc<string, string, string, bool>);

                pd.Invoke(c => {CurrentTask.Events += new GetHostDelegate(c.Invoke);}, typeof (GetHostDelegate).CreateWrappedProxy(new GetHostDelegate(() => Event<GetHostDelegate>.Raise())) as WrappedFunc<Callback>);

                var asm = pd.LoadFile(path);

                lock (_domains) {
                    _domains.Add(asm, pd);
                }

                return asm;
            } catch (Exception e) {
                e.Dump();
            }
            return null;
        }

        /// <summary>
        ///     PROTOTYPE -- extremely simplified assembly locator.
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        private static string FindAssembly(string assemblyName) {
            try {
                string fullPath;
                // is the name given a strong name?
                if (assemblyName.Contains(',')) {
                    // looks like a strong name
                    // todo: not there yet...
                    return null;
                }

                // is it a path?
                if (assemblyName.Contains('\\') || assemblyName.Contains('/') || assemblyName.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase)) {
                    fullPath = Path.GetFullPath(assemblyName);
                    if (File.Exists(fullPath)) {
                        return fullPath;
                    }
                    if (File.Exists(fullPath + ".dll")) {
                        return fullPath;
                    }
                }
                // must be just just a plain name.

                // todo: search the GAC too?

                // search the local folder.
                fullPath = Path.GetFullPath(assemblyName + ".dll");
                if (File.Exists(fullPath)) {
                    return fullPath;
                }

                // try next to where we are.
                fullPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), assemblyName + ".dll");
                if (File.Exists(fullPath)) {
                    return fullPath;
                }
            } catch (Exception e) {
                e.Dump();
            }
            return null;
        }

#if AFTER_CTP
        private static bool TryToLoadNativeProvider(string dllName) {
            // the idea here is to allow someone to code a provider as a native C DLL
            // with a specific set of exports that would allow us to dynamically bind to it.
            //
            // quite a few options for implementation.
            // ideally, a very small exported interface that can be queried to ask for
            // a set of providers.
            //
            // I don't see a really good reason to push this one out into a seperate assembly
            // since it's just an unnecessary further abstraction.

            return false;
        }
#endif
    }
}