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
    using System.Globalization;
    using System.Linq;
    using System.Management.Automation;
    using Core;
    using OneGet.Core.Api;
    using OneGet.Core.Extensions;

    internal class PowerShellProvider : IDisposable {
        public Provider.Interface.FindPackage FindPackage = null;

        private dynamic _powershell;
        private DynamicPowershellResult _result;

        // ReSharper disable InconsistentNaming
        public Provider.Interface.GetProviderName GetProviderName = null;

        public PowerShellProvider(dynamic ps, PSModuleInfo module, List<PowerShellProvider> _instances) {
            _powershell = ps;

            var exportedItems = module.ExportedFunctions.Select(each => each.Key)
                .Union(module.ExportedCmdlets.Select(each => each.Key))
                .Union(module.ExportedAliases.Select(each => each.Key));

            // TEMPORARY: for now we're going to hand-code the generation of the delegates
            // that are expected to be supplied.
            // once we've got a decent test matrix in place, Interface think we can
            // generate the delegates programatically
            // this will likely involve the creation of a replacement for DynamicPowershell
            // that can dynamically create delegates based on introspection of the powershell
            // cmdlets. This *would* remove the dependency on the DLR.
            foreach (var function in exportedItems) {
                switch (function.ToLower(CultureInfo.CurrentCulture)) {
                    case "get-providername":
                        GetProviderName = (callback) => {
                            // call the cmdlet
                            _result = _powershell.GetProviderName();

                            // process the results
                            if (_result.Success) {
                                return _result.Value.ToString();
                            }
                            return null;
                        };
                        break;

                    case "find-package":
                        FindPackage = (n, rv, mv, mxv,  y) => {
                            // Console.WriteLine("Calling PS fn");
                            _result = _powershell.FindPackage(n, rv, mv, mxv, y);
                            return _result.Success;
                        };
                        break;
                   
                }
            }
        }

        // ReSharper restore InconsistentNaming

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                if (_powershell != null) {
                    _powershell.Dispose();
                    _powershell = null;
                }
            }
        }
    }
}
