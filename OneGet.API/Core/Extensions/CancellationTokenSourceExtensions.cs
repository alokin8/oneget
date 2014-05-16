using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.OneGet.Core.Extensions {
    using System.Threading;
    using Api;

    public static class CancellationTokenSourceExtensions {
        public static bool OkToContinue(this CancellationTokenSource cts, IsCancelled isCancelled) {
            if (isCancelled()) {
                cts.Cancel();
            }
            return !cts.Token.IsCancellationRequested;
        }

    }
}
