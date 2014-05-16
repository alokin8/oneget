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

namespace Microsoft.PowerShell.OneGet.CmdLets {
    using System;
    using System.Linq;
    using System.Management.Automation;
    using Core;
    using Microsoft.OneGet;
    using Microsoft.OneGet.Core.Api;
    using Microsoft.OneGet.Core.Extensions;
    using Microsoft.OneGet.Core.Tasks;

    [Cmdlet(VerbsCommon.Remove, PackageSourceNoun, SupportsShouldProcess = true)]
    public class RemovePackageSource : OneGetCmdlet {
        [Parameter(Mandatory = true, Position = 0)]
        public string Name {get; set;}

        [Parameter(Position = 1)]
        public string Provider {get; set;}

#if AFTER_CTP
        [Parameter]
        public SwitchParameter Machine {get; set;}

        [Parameter]
        public SwitchParameter User {get; set;}

        [Parameter]
        public SwitchParameter Force {get; set;}
#endif

        public override bool ProcessRecordAsync() {
            Provider provider = null;

            if (string.IsNullOrEmpty(Provider)) {
                var providers = HostApi.SelectProviders(Provider, new[] {
                    Name
                }).ToArray();

                if (providers.Length == 1) {
                    provider = providers[0];
                    Provider = provider.CachedProviderName;
                } else {
                    Event<Error>.Raise("Conflict", "Multiple providers have source '{0}'; must specify -Provider ".format(Name));
                    return false;
                }
            } else {
                provider = HostApi.SelectProviders(Provider).FirstOrDefault();
                if (provider == null) {
                    Event<Error>.Raise("Unknown Provider", Provider);
                    return false;
                }
            }

            using (var sources = CancelWhenStopped(provider.GetPackageSources(Invoke))) {
                var src = sources.FirstOrDefault(each => each.Name.Equals(Name, StringComparison.OrdinalIgnoreCase));
                if (src != null) {
                    if (ShouldProcess("Name = '{0}' Location = '{1}' Provider = '{2}'".format(src.Name, src.Location, src.Provider)).Result) {
                        provider.RemovePackageSource(Name, Invoke);
                        return true;
                    }
                    return false;
                } else {
                    Event<Error>.Raise("Unknown Source", Name);
                }
            }

            return true;
        }
    }
}