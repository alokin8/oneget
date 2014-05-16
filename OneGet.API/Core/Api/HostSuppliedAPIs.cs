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
    using System.Collections.Generic;
    using Callback = System.Func<string, System.Collections.Generic.IEnumerable<object>, object>;

    #region declare host-supplied-callbacks
    /// <summary>
    /// Used by a provider to request what metadata keys were passed from the user
    /// </summary>
    /// <returns></returns>
    public delegate IEnumerable<string> GetMetadataKeys();

    public delegate IEnumerable<string> GetMetadataValues(string key);

    public delegate IEnumerable<string> GetInstallationOptionKeys();

    public delegate IEnumerable<string> GetInstallationOptionValues(string key);


    public delegate IEnumerable<string> PackageSources();

    /// <summary>
    /// Returns a string collection of values from a specified path in a hierarchal
    /// configuration hashtable.
    /// </summary>
    /// <param name="path">Path to the configuration key. Nodes are traversed by specifying a '/' character:
    ///  Example: "Providers/Module" ""</param>
    /// <returns>A collection of string values from the configuration.
    /// Returns an empty collection if no data is found for that path</returns>
    public delegate IEnumerable<string> GetConfiguration(string path);

    /// <summary>
    ///     The plugin/provider can query to see if the operation has been cancelled.
    ///     This provides for a gentle way for the caller to notify the callee that
    ///     they don't want any more results.
    /// </summary>
    /// <returns>returns TRUE if the operation has been cancelled.</returns>
    public delegate bool IsCancelled();

    // Standard Callbacks that we'll both use internally and pass on down to plugins.
    public delegate bool Warning(string messageCode, string message, IEnumerable<object> args = null);

    public delegate bool Message(string messageCode, string message, IEnumerable<object> args = null);

    public delegate bool Error(string messageCode, string message, IEnumerable<object> args = null);

    public delegate bool Debug(string messageCode, string message, IEnumerable<object> args = null);

    public delegate bool Verbose(string messageCode, string message, IEnumerable<object> args = null);

    public delegate bool ExceptionThrown(string exceptionType, string message, string stacktrace);

    public delegate bool Progress(int activityId, string activity, int progress, string message, IEnumerable<object> args = null);

    public delegate bool ProgressComplete(int activityId, string activity, string message, IEnumerable<object> args = null);

    public delegate Callback GetHostDelegate();

    public delegate bool ShouldContinueWithUntrustedPackageSource(string package, string packageSource);

    public delegate bool ShouldProcessPackageInstall(string packageName, string version, string source);

    public delegate bool ShouldProcessPackageUninstall(string packageName, string version);

    public delegate bool ShouldContinueAfterPackageInstallFailure(string packageName, string version, string source);

    public delegate bool ShouldContinueAfterPackageUninstallFailure(string packageName, string version, string source);

    public delegate bool ShouldContinueRunningInstallScript(string packageName, string version, string source, string scriptLocation );

    public delegate bool ShouldContinueRunningUninstallScript(string packageName, string version, string source, string scriptLocation);

    public delegate bool AskPermission(string permission);

    public delegate bool WhatIf();

    #endregion
}
