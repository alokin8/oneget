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

namespace Microsoft.OneGet.Plugin.PowerShell {
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using Core;
    using OneGet.Core.Extensions;
    using Callback = System.Func<string, System.Collections.Generic.IEnumerable<object>, object>; 

    public class PowershellPlugin : IDisposable {
        private readonly Dictionary<string, Func<PowerShellProvider>> _providerFactory = new Dictionary<string, Func<PowerShellProvider>>();
        private List<PowerShellProvider> _providerInstances = new List<PowerShellProvider>();

        public string PluginName {
            get {
                return "PowerShell";
            }
        }

        public IEnumerable<string> PackageProviderNames {
            get {
                return _providerFactory.Keys;
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                var pi = _providerInstances;
                _providerInstances = null;

                foreach(var i in pi) {
                    i.Dispose();
                }
            }
        }

        public object CreatePackageProvider(string name) {
            var factory = _providerFactory.GetByMatchedKey(name);
            if (factory == null) {
                return null;
            }

            // create the instance
            return factory();
        }

        private PowerShellProvider Create(string psModule) {
            dynamic ps = new DynamicPowershell();
            try {
                DynamicPowershellResult result = ps.ImportModule(Name: psModule, PassThru: true);
                var providerModule = result.Value as PSModuleInfo;
                if (result.Success && result.Value != null) {
                    result = ps.GetProviderName();
                    if (result.Success) {
                        var newInstance = new PowerShellProvider(ps, providerModule, _providerInstances);
                        _providerInstances.Add(newInstance);
                        return newInstance;
                    }
                }
            } catch (Exception e) {
                // something didn't go well.
                // skip it.
                e.Dump();
            }

            // didn't import correctly.
            ps.Dispose();
            return null;
        }

        public void InitPlugin(Callback c) {
            if (c == null) {
                throw new ArgumentNullException("c");
            }
            var modules = c.GetConfiguration("Providers/Module");

            // try to create each module at least once.
            foreach (var modulePath in modules) {
                var provider = Create(modulePath);
                if (provider != null) {
                    // need a local copy to capture
                    var path = modulePath;

                    // looks good to me, let's add this to the provider factory
                    _providerFactory.AddOrSet(provider.GetProviderName(c), () => Create(path));
                }
            }

            // check to see if dynamic powershell is working:
            /*
            Core.DynamicPowershellResult results = _powerShell.dir("c:\\");

            foreach (var result in results) {
                Console.WriteLine(result);
            }
            */

            // search the configuration data for any PowerShell plugins we're supposed to load.
            /* var modules = getConfigStrings.GetStringCollection("Providers/Module");
            _powerShell = new Core.DynamicPowershell();

            var results = _powerShell.GetChildItem("c:\\");
            foreach ( var result in results ) {
                Console.WriteLine(result);
            }
             */
        }

        public void DisposePlugin() {
        }
    }
}
