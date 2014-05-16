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
    using System;
    using System.Collections.Generic;
    using Core.DuckTyping;
    using Core.Extensions;
    using Callback = System.Func<string, System.Collections.Generic.IEnumerable<object>, object>;

    /// <summary>
    ///     STATUS: PROTOTYPE - NOT FINAL INTERFACE
    ///     A package plugin assembly must expose one or more classes that implement the methods
    ///     that when queried, will return a collection of PackageProviders
    ///     This allows PackageProviderAssemblies to provide multiple package providers without having to expose exactly how
    ///     those package provders are implemented.
    /// </summary>
    internal class Plugin : DuckTypedClass {

        #region plugin-interface-definition
        internal static class Interface {

            [Method, Optional]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development.")]
			internal delegate string get_PluginName();
			
            [Method, Optional]
            internal delegate void InitPlugin(Callback c);

            [Method, Optional]
            internal delegate void DisposePlugin();

            [Property, Required]
            internal delegate IEnumerable<string> get_PackageProviderNames();

            [Property, Required]
            internal delegate object CreatePackageProvider(string name);
        }
        #endregion

        [Method, Optional]
        public Interface.DisposePlugin DisposePlugin = () => {};

        [Method, Optional]
        public Interface.InitPlugin InitPlugin = (c) => {};

        [Property, Required]
        public Interface.get_PackageProviderNames _getPackageProviderNames;

        [Method, Required]
        public Interface.CreatePackageProvider _createPackageProvider;

        [Property, Required]
        public Interface.get_PluginName _getPluginName;

        internal Plugin(Type duckTypeClass) : base(duckTypeClass) {
        }

        public string PluginName {
            get {
                try {
                    return _getPluginName();
                } catch (Exception e) {
                    e.Dump();
                    return "PLUGIN-NAME-UNKNOWN";
                }
            }
        }

        /*
        internal string OptionalField {
            get {
                return _getPluginName.IsSupported() ? _getPluginName() : "";
            }
        }
        */

        public Provider CreatePackageProvider(string name) {
            var provider = _createPackageProvider(name);
            if (provider is Provider) {
                return provider as Provider;
            }
            if (typeof(Provider).IsObjectCompatible(provider)) {
                return new Provider(provider);
            }
            return null;
        }

        internal IEnumerable<string> PackageProviderNames {
            get {
                return _getPackageProviderNames().ByRef();
            }
        }
    }
}
