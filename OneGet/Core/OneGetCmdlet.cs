using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerShell.OneGet.Core {
    using System.Collections;
    using Microsoft.OneGet;
    using Microsoft.OneGet.Core.Api;
    using Microsoft.OneGet.Core.Extensions;
    using Microsoft.OneGet.Core.Tasks;

    public class OneGetCmdlet : AsyncCmdlet {

        private static readonly object _lockObject = new object();

        protected override void Init() {
            if (IsCancelled()) {
                return;
            }
            if (!IsInitialized) {
                lock (_lockObject) {
                    if (!IsInitialized) {
                        try {
                            var privateData = MyInvocation.MyCommand.Module.PrivateData as Hashtable;

                            var assemblyProviders = privateData.GetStringCollection("Providers/Assembly");

                            if (assemblyProviders.IsNullOrEmpty()) {
                                Event<Error>.Raise("Configuration Error", "PrivateData is null");
                                return;
                            }
                            IsInitialized = HostApi.Init(Invoke, assemblyProviders, !IsInvocation);
                        }
                        catch (Exception e) {
                            e.Dump();
                        }
                    }
                }
            }
        }
    }
}
