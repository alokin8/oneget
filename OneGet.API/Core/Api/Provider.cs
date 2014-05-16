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

namespace Microsoft.OneGet.Core.Api {
    // result API callback.
    //
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;
    using Collections;
    using DuckTyping;
    using Extensions;
    using Packaging;
    using Tasks;
    using Callback = System.Func<string, System.Collections.Generic.IEnumerable<object>, object>;

    // Methods used by Providers.

    // the name of the delegate is the name that the client must call their method
    // we bind using the names of the delegates, and not the delegate field names
    // ie, we bind to GetProviderName not _getProviderName.

    /// <summary>
    ///     STATUS: PROTOTYPE - NOT FINAL INTERFACE
    ///     Interface that every SoftwareIdentity management provider must implement
    /// </summary>
    public class Provider : MarshalByRefObject {
        private readonly Interface _interface;
        public IEnumerable<string> FileMasksThatMeanSomethingToYou; //ie, a nuget handler might recognize 'SoftwareIdentity.config' files
        public IEnumerable<string> FolderMasksThatMeanSomethingToYou; //ie, a git handler would recognize '.git' folders
        public string ProviderVersion;
        public IEnumerable<string> SupportedFileExtensions;
        public IEnumerable<string> SupportedMagicByteSequences;
        public IEnumerable<string> SupportedMediaType;
        public IEnumerable<string> SupportedUriSchemes;
        public bool SupportsFolderContext;
        public bool SupportsSystemContext;
        public bool SupportsUserContext;
        private string _providerName;

        internal Provider(object duckTypedInstance) {
            _interface = new Interface(duckTypedInstance);
        }

        public string CachedProviderName {
            get {
                return _providerName;
            }
        }

        public string GetProviderName(Callback c) {
            try {
                return _providerName ?? (_providerName = _interface.getProviderName(c));
            } catch (Exception e) {
                e.Dump();
                return "PROVIDER-NAME-UNKNOWN";
            }
        }

        public void AddPackageSource(string name, string location, bool trusted, Callback c) {
            _interface.addPackageSource(name, location, trusted,   new InvokableDispatcher(c, Instance.Service.Invoke));
        }

        public void RemovePackageSource(string name, Callback c) {
            _interface.removePackageSource(name, new InvokableDispatcher(c, Instance.Service.Invoke));
        }

        public override object InitializeLifetimeService() {
            return null;
        }

        public CancellableEnumerable<SoftwareIdentity> FindPackageByUri(string u, Callback c) {
            var providerName = GetProviderName(c);

            return CallAndCollectResults<SoftwareIdentity, YieldPackage>(
                c, // inherited callback
                nc => _interface.findPackageByUri(u, nc), // actual call
                (collection, okToContinue) => ((fastpath, name, version, scheme, summary, source) => {
                    collection.Add(new SoftwareIdentity {
                        FastPath = fastpath,
                        Name = name,
                        Version = version,
                        VersionScheme = scheme,
                        Summary = summary,
                        ProviderName = providerName,
                        Source = source,
                        Status = "Available"
                    });
                    return okToContinue();
                }));
        }

        public CancellableEnumerable<SoftwareIdentity> FindPackageByFile(string filename, Callback c) {
            var providerName = GetProviderName(c);

            return CallAndCollectResults<SoftwareIdentity, YieldPackage>(
                c, // inherited callback
                nc => _interface.findPackageByFile(filename, nc), // actual call
                (collection, okToContinue) => ((fastpath, name, version, scheme, summary, source) => {
                    collection.Add(new SoftwareIdentity {
                        FastPath = fastpath,
                        Name = name,
                        Version = version,
                        VersionScheme = scheme,
                        Summary = summary,
                        ProviderName = providerName,
                        Source = source,
                        Status = "Available"
                    });
                    return okToContinue();
                }));
        }

        public CancellableEnumerable<SoftwareIdentity> FindPackage(string name, string requiredVersion, string minimumVersion, string maximumVersion, Callback c) {
            var providerName = GetProviderName(c);

            return CallAndCollectResults<SoftwareIdentity, YieldPackage>(
                c, // inherited callback
                nc => _interface.findPackage(name, requiredVersion, minimumVersion, maximumVersion, nc), // actual call
                (collection, okToContinue) => ((fastpath, n, version, scheme, summary, source) => {
                    collection.Add(new SoftwareIdentity {
                        FastPath = fastpath,
                        Name = n,
                        Version = version,
                        VersionScheme = scheme,
                        Summary = summary,
                        ProviderName = providerName,
                        Source = source,
                        Status = "Available"
                    });
                    return okToContinue();
                }), false);
        }

        public CancellableEnumerable<SoftwareIdentity> GetInstalledPackages(string name, Callback c) {
            var providerName = GetProviderName(c);

            return CallAndCollectResults<SoftwareIdentity, YieldPackage>(
                c, // inherited callback
                nc => _interface.getInstalledPackages(name, nc), // actual call
                (collection, okToContinue) => ((fastpath, n, version, scheme, summary, source) => {
                    collection.Add(new SoftwareIdentity {
                        FastPath = fastpath,
                        Name = n,
                        Version = version,
                        VersionScheme = scheme,
                        Summary = summary,
                        ProviderName = providerName,
                        Source = source,
                        Status = "Installed"
                    });
                    return okToContinue();
                }));
        }

        /* CTP */

        public CancellableEnumerable<SoftwareIdentity> InstallPackage(SoftwareIdentity softwareIdentity, Callback c) {
            if (softwareIdentity == null) {
                throw new ArgumentNullException("softwareIdentity");
            }

            if (c == null) {
                throw new ArgumentNullException("c");
            }
            var providerName = GetProviderName(c);

            if (!_interface.isTrustedPackageSource(softwareIdentity.Source, c)) {
                try {
                    if (!(bool)c.DynamicInvoke<ShouldContinueWithUntrustedPackageSource>(softwareIdentity.Name, softwareIdentity.Source)) {
                        c.DynamicInvoke<Error>("Cancelled", "User declined to trust package source ", null);
                        throw new Exception("cancelled");
                    }
                } catch {
                    c.DynamicInvoke<Error>("Cancelled", "User declined to trust package source ", null);
                    throw new Exception("cancelled"); 
                }
            }

            return CallAndCollectResults<SoftwareIdentity, YieldPackage>(
                c, // inherited callback
                nc => _interface.installPackageByFastpath(softwareIdentity.FastPath, nc), // actual call
                (collection, okToContinue) => ((fastpath, n, version, scheme, summary, source) => {
                    collection.Add(new SoftwareIdentity {
                        FastPath = fastpath,
                        Name = n,
                        Version = version,
                        VersionScheme = scheme,
                        Summary = summary,
                        ProviderName = providerName,
                        Source = source,
                        Status = "Installed"
                    });
                    return okToContinue();
                }));
        }

        public CancellableEnumerable<SoftwareIdentity> UninstallPackage(SoftwareIdentity softwareIdentity, Callback c) {
            var providerName = GetProviderName(c);

            return CallAndCollectResults<SoftwareIdentity, YieldPackage>(
                c, // inherited callback
                nc => _interface.uninstallPackage(softwareIdentity.FastPath, nc), // actual call
                (collection, okToContinue) => ((fastpath, n, version, scheme, summary, source) => {
                    collection.Add(new SoftwareIdentity {
                        FastPath = fastpath,
                        Name = n,
                        Version = version,
                        VersionScheme = scheme,
                        Summary = summary,
                        ProviderName = providerName,
                        Source = source,
                        Status = "Not Installed"
                    });
                    return okToContinue();
                }));
        }

        public void GetPackageDependencies() {
        }

        public CancellableEnumerable<PackageSource> GetPackageSources(Callback c) {
            return CallAndCollectResults<PackageSource, YieldSource>(
                c,
                nc => _interface.getPackageSources(nc),
                (collection, okToContinue) => ((name, location, isTrusted) => {
                    collection.Add(new PackageSource() {
                        Name = name,
                        Location = location,
                        Provider = CachedProviderName,
                        IsTrusted = isTrusted
                    });
                    return okToContinue();
                }));
        }

        /// <summary>
        ///     I noticed that most of my functions ended up as a pattern that was extremely common.
        ///     I've therefore decided to distill this down to eliminate fat-fingered mistakes when cloning the pattern.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="OnResultFn"></typeparam>
        /// <param name="c"></param>
        /// <param name="action"></param>
        /// <param name="onResultFn"></param>
        /// <returns></returns>
        private CancellableEnumerable<TResult> CallAndCollectResults<TResult, OnResultFn>(Callback c, Action<Callback> action, Func<CancellableBlockingCollection<TResult>, OkToContinue, OnResultFn> onResultFn,bool cancelOnException = true) {
            var result = new CancellableBlockingCollection<TResult>();

            Task.Factory.StartNew(() => {
                try {
                    // callback.DynamicInvoke<Verbose>("Hello", "World", null);
                    var isOkToContinueFn = new OkToContinue(() => !(result.IsCancelled || (bool)c.DynamicInvoke<IsCancelled>()));

                    using (var cb = new InvokableDispatcher(c, Instance.Service.Invoke) { isOkToContinueFn, onResultFn(result, isOkToContinueFn) }) {
                        try {
                            action(cb);
                        } catch (Exception e) {
                            if (cancelOnException) {
                                result.Cancel();
                                Event<ExceptionThrown>.Raise(e.GetType().Name, e.Message, e.StackTrace);
                            }
                        }
                    }
                } catch (Exception e) {
                    e.Dump();
                } finally {
                    result.CompleteAdding();
                }
            });

            return result;
        }

        public CancellableEnumerable<MetadataDefinition> GetMetadataDefinitions(Callback c) {
            return CallAndCollectResults<MetadataDefinition, YieldMetadataDefinition>(
                c,
                nc => _interface.getMetadataDefinitions(nc),
                (collection, okToContinue) => ((name, type, values) => {
                    collection.Add(new MetadataDefinition {
                        Name = name,
                        Type = type,
                        PossibleValues = values
                    });
                    return okToContinue();
                }));
        }

        public CancellableEnumerable<InstallOptionDefinition> GetInstallationOptionDefinitions(Callback c) {
            return CallAndCollectResults<InstallOptionDefinition, YieldInstallationOptionsDefinition>(
                c,
                nc => _interface.getInstallationOptionDefinitions(nc),
                (collection, okToContinue) => ((name, type, required, values) => {
                    collection.Add(new InstallOptionDefinition {
                        Name = name,
                        Type = type,
                        PossibleValues = values,
                        IsRequired = required
                    });
                    return okToContinue();
                }));
        }

        public bool IsValidPackageSource(string packageSource, Callback c) {
            return _interface.isValidPackageSource(packageSource, new InvokableDispatcher(c, Instance.Service.Invoke));
        }

        /* after CTP */

        public void FindPackageUpdates() {
        }

        // public string UniqueID; //Guid?

        public void IdentifyArbitraryFile() {
        }

        public void IdentifyArbitraryDirectory() {
        }

        public void IdentifyArbitraryUri() {
        }

        internal class Interface : DuckTypedClass {
            [Method, Optional]
            public AddPackageSource addPackageSource;

            [Method, Optional]
            public FindPackage findPackage;

            [Method, Optional]
            public FindPackageByFile findPackageByFile;

            [Method, Optional]
            public FindPackageByUri findPackageByUri;

            [Method, Optional]
            public GetInstallationOptionDefinitions getInstallationOptionDefinitions;

            [Method, Optional]
            public GetInstalledPackages getInstalledPackages;

            [Property, Optional]
            public GetMetadataDefinitions getMetadataDefinitions;

            [Method, Optional]
            public GetPackageSources getPackageSources;

            [Property, Required]
            public GetProviderName getProviderName;

            [Method, Optional]
            public InstallPackageByFastpath installPackageByFastpath;

            [Method, Optional]
            public InstallPackageByFile installPackageByFile;

            [Method, Optional]
            public InstallPackageByUri installPackageByUri;

            [Method, Optional]
            public IsTrustedPackageSource isTrustedPackageSource;

            [Method, Optional]
            public IsValidPackageSource isValidPackageSource;

            [Method, Optional]
            public RemovePackageSource removePackageSource;

            [Method, Optional]
            public UninstallPackage uninstallPackage;

            internal Interface(object instance) : base(instance) {
            }

            internal delegate void AddPackageSource(string name, string location, bool trusted, Callback c);

            internal delegate bool FindPackage(string name, string requiredVersion, string minimumVersion, string maximumVersion, Callback c);

            internal delegate bool FindPackageByFile(string file, Callback c);

            internal delegate bool FindPackageByUri(string u, Callback c);

            internal delegate void GetInstallationOptionDefinitions(Callback c);

            internal delegate bool GetInstalledPackages(string name, Callback c);

            internal delegate void GetMetadataDefinitions(Callback c);

            internal delegate bool GetPackageSources(Callback c);

            internal delegate string GetProviderName(Callback c);

            internal delegate bool InstallPackageByFastpath(string fastPath, Callback c);

            internal delegate bool InstallPackageByFile(string filePath, Callback c);

            internal delegate bool InstallPackageByUri(string u, Callback c);

            internal delegate bool IsTrustedPackageSource(string packageSource, Callback c);

            internal delegate bool IsValidPackageSource(string packageSource, Callback c);

            internal delegate void RemovePackageSource(string name, Callback c);

            internal delegate bool UninstallPackage(string fastPath, Callback c);
        }
    }

    internal static class CallbackExt {
        public static T Lookup<T>(this Callback c) where T : class {
            return c(typeof (T).Name, null) as T ?? typeof (T).CreateEmptyDelegate() as T;
        }

        public static object DynamicInvoke<T>(this Callback c, params object[] args) where T : class {
            return c(typeof (T).Name, args);
        }
    }
}