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

namespace Microsoft.OneGet.Plugin.Chocolatey {
    using System;
    using System.Collections.Generic;
    using Callback = System.Func<string, System.Collections.Generic.IEnumerable<object>, object>; 


    #region copy collection-callbacks
    // standard callbacks for accessing collections
    public delegate string LookupString (string name);

    public delegate IEnumerable<string> LookupEnumerable (string name);
    #endregion

    #region copy service-api-callbacks
    public delegate string GetNuGetExePath ();

    public delegate string GetNuGetDllPath ();

    public delegate string DownloadFile (string remoteLocation, string localLocation);

    public delegate void AddPinnedItemToTaskbar (string item);

    public delegate void RemovePinnedItemFromTaskbar (string item);

    public delegate bool CreateShortcutLink (string linkPath, string targetPath, string description, string workingDirectory, string arguments);

    public delegate IEnumerable<string> UnzipFileIncremental (string zipFile, string folder);

    public delegate IEnumerable<string> UnzipFile (string zipFile, string folder);

    public delegate void AddFileAssociation ();

    public delegate void RemoveFileAssociation ();

    public delegate void AddExplorerMenuItem ();

    public delegate void RemoveExplorerMenuItem ();

    public delegate bool SetEnvironmentVariable (string variable, string value, string context);

    public delegate bool RemoveEnvironmentVariable (string variable, string context);

    public delegate void AddFolderToPath ();

    public delegate void RemoveFolderFromPath ();

    public delegate void InstallMSI ();

    public delegate void RemoveMSI ();

    public delegate void StartProcess ();

    public delegate void InstallVSIX ();

    public delegate void UninstallVSIX ();

    public delegate void InstallPowershellScript ();

    public delegate void UninstallPowershellScript ();

    public delegate void SearchForExecutable ();

    public delegate void GetUserBinFolder ();

    public delegate void GetSystemBinFolder ();

    public delegate bool CopyFile (string sourcePath, string destinationPath);

    public delegate void CopyFolder ();

    public delegate void Delete (string path);

    public delegate void DeleteFolder (string folder);

    public delegate void CreateFolder (string folder);

    public delegate void DeleteFile (string filename);

    public delegate void BeginTransaction ();

    public delegate void AbortTransaction ();

    public delegate void EndTransaction ();

    public delegate void GenerateUninstallScript ();

    public delegate string GetKnownFolder (string knownFolder);

    public delegate bool IsElevated ();
    #endregion

    #region copy core-supplied-callbacks
    /// <summary>
    ///     The plugin/provider can query to see if the operation has been cancelled.
    ///     This provides for a gentle way for the caller to notify the callee that
    ///     they don't want any more results. It's essentially just !IsCancelled()
    /// </summary>
    /// <returns>returns FALSE if the operation has been cancelled.</returns>
    public delegate bool OkToContinue ();

    /// <summary>
    /// Used by a provider to return fields for a SoftwareIdentity.
    /// </summary>
    /// <param name="fastPath"></param>
    /// <param name="name"></param>
    /// <param name="version"></param>
    /// <param name="versionScheme"></param>
    /// <param name="summary"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    public delegate bool YieldPackage (string fastPath, string name, string version, string versionScheme, string summary, string source );

    /// <summary>
    /// Used by a provider to return fields for a package source (repository)
    /// </summary>
    /// <param name="name"></param>
    /// <param name="location"></param>
    /// <returns></returns>
    public delegate bool YieldSource (string name, string location, bool isTrusted );

    /// <summary>
    /// Used by a provider to return the fields for a Metadata Definition
    /// 
    /// The cmdlets can use this to supply tab-completion for metadata to the user.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="expectedType"></param>
    /// <param name="permittedValues"></param>
    /// <returns></returns>
    public delegate bool YieldMetadataDefinition (string name, string expectedType, IEnumerable<string> permittedValues);

    /// <summary>
    /// Used by a provider to return the fields for an Installation Option Definition
    /// 
    /// The cmdlets can use this to supply tab-completion for installation options to the user.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="expectedType"></param>
    /// <param name="required"></param>
    /// <param name="permittedValues"></param>
    /// <returns></returns>
    public delegate bool YieldInstallationOptionsDefinition (string name, string expectedType, bool required, IEnumerable<string> permittedValues);
    #endregion

    #region copy host-supplied-callbacks
    /// <summary>
    /// Used by a provider to request what metadata keys were passed from the user
    /// </summary>
    /// <returns></returns>
    public delegate IEnumerable<string> GetMetadataKeys ();

    public delegate IEnumerable<string> GetMetadataValues (string key);

    public delegate IEnumerable<string> GetInstallationOptionKeys ();

    public delegate IEnumerable<string> GetInstallationOptionValues (string key);

    public delegate IEnumerable<string> PackageSources ();

    /// <summary>
    /// Returns a string collection of values from a specified path in a hierarchal
    /// configuration hashtable.
    /// </summary>
    /// <param name="path">Path to the configuration key. Nodes are traversed by specifying a '/' character:
    ///  Example: "Providers/Module" ""</param>
    /// <returns>A collection of string values from the configuration.
    /// Returns an empty collection if no data is found for that path</returns>
    public delegate IEnumerable<string> GetConfiguration (string path);

    /// <summary>
    ///     The plugin/provider can query to see if the operation has been cancelled.
    ///     This provides for a gentle way for the caller to notify the callee that
    ///     they don't want any more results.
    /// </summary>
    /// <returns>returns TRUE if the operation has been cancelled.</returns>
    public delegate bool IsCancelled ();

    // Standard Callbacks that we'll both use internally and pass on down to plugins.
    public delegate bool Warning (string messageCode, string message, IEnumerable<object> args = null);

    public delegate bool Message (string messageCode, string message, IEnumerable<object> args = null);

    public delegate bool Error (string messageCode, string message, IEnumerable<object> args = null);

    public delegate bool Debug (string messageCode, string message, IEnumerable<object> args = null);

    public delegate bool Verbose (string messageCode, string message, IEnumerable<object> args = null);

    public delegate bool ExceptionThrown (string exceptionType, string message, string stacktrace);

    public delegate bool Progress (int activityId, string activity, int progress, string message, IEnumerable<object> args = null);

    public delegate bool ProgressComplete (int activityId, string activity, string message, IEnumerable<object> args = null);

    public delegate Callback GetHostDelegate ();

    public delegate bool ShouldContinueWithUntrustedPackageSource (string package, string packageSource);

    public delegate bool ShouldProcessPackageInstall (string packageName, string version, string source);

    public delegate bool ShouldProcessPackageUninstall (string packageName, string version);

    public delegate bool ShouldContinueAfterPackageInstallFailure (string packageName, string version, string source);

    public delegate bool ShouldContinueAfterPackageUninstallFailure (string packageName, string version, string source);

    public delegate bool ShouldContinueRunningInstallScript (string packageName, string version, string source, string scriptLocation );

    public delegate bool ShouldContinueRunningUninstallScript (string packageName, string version, string source, string scriptLocation);

    public delegate bool AskPermission (string permission);

    public delegate bool WhatIf ();
    #endregion

    /// <summary>
    ///     This generated class can be copied to any project that implements a OneGet plugin
    ///     This gives type-safe access to the callbacks and APIs without having to take a direct
    ///     dependency on the OneGet core Assemblies.
    /// </summary>
    internal static class CallbackExtensions {
        /// <summary>
        ///     This transforms a generic delegate into a type-specific delegate so that you can
        ///     call the target delegate with the appropriate signature.
        /// </summary>
        /// <typeparam name="TDelegate"></typeparam>
        /// <param name="src"></param>
        /// <returns></returns>
        public static TDelegate CastDelegate<TDelegate>(this Delegate src) where TDelegate : class {
            return (TDelegate)(object)Delegate.CreateDelegate(typeof (TDelegate), src.Target, src.Method, true); // throw on fail
        }

        /// <summary>
        ///     This calls the supplied delegate with the name of the callback that we're actaully looking for
        ///     and then casts the resulting delegate back to the type that we're expecting.
        /// </summary>
        /// <typeparam name="TDelegate"></typeparam>
        /// <param name="c"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static TDelegate Resolve<TDelegate>(this Callback c, params object[] args) where TDelegate : class {
            var delegateType = typeof (TDelegate);
            if (delegateType.BaseType != typeof (MulticastDelegate)) {
                throw new Exception("Generic Type Incorrect");
            }
            // calling with null args set returns the delegate instead of calling the delegate.
            // return CastDelegate<TDelegate>(CastDelegate<Func<string, IEnumerable<object>, Delegate>>(call)(delegateType.Name, null));
            // var m = call(delegateType.Name, null);
            var m = (Delegate)c(delegateType.Name, null);
            return m == null ? null : CastDelegate<TDelegate>(m);
        }


        #region generate-resolved collection-callbacks
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static string LookupString(this Callback c , string name ) {
            return (c.Resolve<LookupString>() ?? (( pname)=> default(string) ) )( name);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static IEnumerable<string> LookupEnumerable(this Callback c , string name ) {
            return (c.Resolve<LookupEnumerable>() ?? (( pname)=> default(IEnumerable<string>) ) )( name);
        }
        #endregion

        #region generate-resolved service-api-callbacks
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static string GetNuGetExePath(this Callback c  ) {
            return (c.Resolve<GetNuGetExePath>() ?? (()=> default(string) ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static string GetNuGetDllPath(this Callback c  ) {
            return (c.Resolve<GetNuGetDllPath>() ?? (()=> default(string) ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static string DownloadFile(this Callback c , string remoteLocation, string localLocation ) {
            return (c.Resolve<DownloadFile>() ?? (( premoteLocation, plocalLocation)=> default(string) ) )( remoteLocation, localLocation);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void AddPinnedItemToTaskbar(this Callback c , string item ) {
             (c.Resolve<AddPinnedItemToTaskbar>() ?? (( pitem)=> { } ) )( item);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void RemovePinnedItemFromTaskbar(this Callback c , string item ) {
             (c.Resolve<RemovePinnedItemFromTaskbar>() ?? (( pitem)=> { } ) )( item);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool CreateShortcutLink(this Callback c , string linkPath, string targetPath, string description, string workingDirectory, string arguments ) {
            return (c.Resolve<CreateShortcutLink>() ?? (( plinkPath, ptargetPath, pdescription, pworkingDirectory, parguments)=> default(bool) ) )( linkPath, targetPath, description, workingDirectory, arguments);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static IEnumerable<string> UnzipFileIncremental(this Callback c , string zipFile, string folder ) {
            return (c.Resolve<UnzipFileIncremental>() ?? (( pzipFile, pfolder)=> default(IEnumerable<string>) ) )( zipFile, folder);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static IEnumerable<string> UnzipFile(this Callback c , string zipFile, string folder ) {
            return (c.Resolve<UnzipFile>() ?? (( pzipFile, pfolder)=> default(IEnumerable<string>) ) )( zipFile, folder);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void AddFileAssociation(this Callback c  ) {
             (c.Resolve<AddFileAssociation>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void RemoveFileAssociation(this Callback c  ) {
             (c.Resolve<RemoveFileAssociation>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void AddExplorerMenuItem(this Callback c  ) {
             (c.Resolve<AddExplorerMenuItem>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void RemoveExplorerMenuItem(this Callback c  ) {
             (c.Resolve<RemoveExplorerMenuItem>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool SetEnvironmentVariable(this Callback c , string variable, string value, string context ) {
            return (c.Resolve<SetEnvironmentVariable>() ?? (( pvariable, pvalue, pcontext)=> default(bool) ) )( variable, value, context);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool RemoveEnvironmentVariable(this Callback c , string variable, string context ) {
            return (c.Resolve<RemoveEnvironmentVariable>() ?? (( pvariable, pcontext)=> default(bool) ) )( variable, context);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void AddFolderToPath(this Callback c  ) {
             (c.Resolve<AddFolderToPath>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void RemoveFolderFromPath(this Callback c  ) {
             (c.Resolve<RemoveFolderFromPath>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void InstallMSI(this Callback c  ) {
             (c.Resolve<InstallMSI>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void RemoveMSI(this Callback c  ) {
             (c.Resolve<RemoveMSI>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void StartProcess(this Callback c  ) {
             (c.Resolve<StartProcess>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void InstallVSIX(this Callback c  ) {
             (c.Resolve<InstallVSIX>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void UninstallVSIX(this Callback c  ) {
             (c.Resolve<UninstallVSIX>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void InstallPowershellScript(this Callback c  ) {
             (c.Resolve<InstallPowershellScript>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void UninstallPowershellScript(this Callback c  ) {
             (c.Resolve<UninstallPowershellScript>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void SearchForExecutable(this Callback c  ) {
             (c.Resolve<SearchForExecutable>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void GetUserBinFolder(this Callback c  ) {
             (c.Resolve<GetUserBinFolder>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void GetSystemBinFolder(this Callback c  ) {
             (c.Resolve<GetSystemBinFolder>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool CopyFile(this Callback c , string sourcePath, string destinationPath ) {
            return (c.Resolve<CopyFile>() ?? (( psourcePath, pdestinationPath)=> default(bool) ) )( sourcePath, destinationPath);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void CopyFolder(this Callback c  ) {
             (c.Resolve<CopyFolder>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void Delete(this Callback c , string path ) {
             (c.Resolve<Delete>() ?? (( ppath)=> { } ) )( path);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void DeleteFolder(this Callback c , string folder ) {
             (c.Resolve<DeleteFolder>() ?? (( pfolder)=> { } ) )( folder);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void CreateFolder(this Callback c , string folder ) {
             (c.Resolve<CreateFolder>() ?? (( pfolder)=> { } ) )( folder);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void DeleteFile(this Callback c , string filename ) {
             (c.Resolve<DeleteFile>() ?? (( pfilename)=> { } ) )( filename);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void BeginTransaction(this Callback c  ) {
             (c.Resolve<BeginTransaction>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void AbortTransaction(this Callback c  ) {
             (c.Resolve<AbortTransaction>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void EndTransaction(this Callback c  ) {
             (c.Resolve<EndTransaction>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static void GenerateUninstallScript(this Callback c  ) {
             (c.Resolve<GenerateUninstallScript>() ?? (()=> { } ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static string GetKnownFolder(this Callback c , string knownFolder ) {
            return (c.Resolve<GetKnownFolder>() ?? (( pknownFolder)=> default(string) ) )( knownFolder);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool IsElevated(this Callback c  ) {
            return (c.Resolve<IsElevated>() ?? (()=> default(bool) ) )();
        }
        #endregion

        #region generate-resolved core-supplied-callbacks
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool OkToContinue(this Callback c  ) {
            return (c.Resolve<OkToContinue>() ?? (()=> default(bool) ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool YieldPackage(this Callback c , string fastPath, string name, string version, string versionScheme, string summary, string source ) {
            return (c.Resolve<YieldPackage>() ?? (( pfastPath, pname, pversion, pversionScheme, psummary, psource)=> default(bool) ) )( fastPath, name, version, versionScheme, summary, source);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool YieldSource(this Callback c , string name, string location, bool isTrusted ) {
            return (c.Resolve<YieldSource>() ?? (( pname, plocation, pisTrusted)=> default(bool) ) )( name, location, isTrusted);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool YieldMetadataDefinition(this Callback c , string name, string expectedType, IEnumerable<string> permittedValues ) {
            return (c.Resolve<YieldMetadataDefinition>() ?? (( pname, pexpectedType, ppermittedValues)=> default(bool) ) )( name, expectedType, permittedValues);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool YieldInstallationOptionsDefinition(this Callback c , string name, string expectedType, bool required, IEnumerable<string> permittedValues ) {
            return (c.Resolve<YieldInstallationOptionsDefinition>() ?? (( pname, pexpectedType, prequired, ppermittedValues)=> default(bool) ) )( name, expectedType, required, permittedValues);
        }
        #endregion

        #region generate-resolved host-supplied-callbacks
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static IEnumerable<string> GetMetadataKeys(this Callback c  ) {
            return (c.Resolve<GetMetadataKeys>() ?? (()=> default(IEnumerable<string>) ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static IEnumerable<string> GetMetadataValues(this Callback c , string key ) {
            return (c.Resolve<GetMetadataValues>() ?? (( pkey)=> default(IEnumerable<string>) ) )( key);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static IEnumerable<string> GetInstallationOptionKeys(this Callback c  ) {
            return (c.Resolve<GetInstallationOptionKeys>() ?? (()=> default(IEnumerable<string>) ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static IEnumerable<string> GetInstallationOptionValues(this Callback c , string key ) {
            return (c.Resolve<GetInstallationOptionValues>() ?? (( pkey)=> default(IEnumerable<string>) ) )( key);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static IEnumerable<string> PackageSources(this Callback c  ) {
            return (c.Resolve<PackageSources>() ?? (()=> default(IEnumerable<string>) ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static IEnumerable<string> GetConfiguration(this Callback c , string path ) {
            return (c.Resolve<GetConfiguration>() ?? (( ppath)=> default(IEnumerable<string>) ) )( path);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool IsCancelled(this Callback c  ) {
            return (c.Resolve<IsCancelled>() ?? (()=> default(bool) ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool Warning(this Callback c , string messageCode, string message, IEnumerable<object> args ) {
            return (c.Resolve<Warning>() ?? (( pmessageCode, pmessage, pargs)=> default(bool) ) )( messageCode, message, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool Message(this Callback c , string messageCode, string message, IEnumerable<object> args ) {
            return (c.Resolve<Message>() ?? (( pmessageCode, pmessage, pargs)=> default(bool) ) )( messageCode, message, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool Error(this Callback c , string messageCode, string message, IEnumerable<object> args ) {
            return (c.Resolve<Error>() ?? (( pmessageCode, pmessage, pargs)=> default(bool) ) )( messageCode, message, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool Debug(this Callback c , string messageCode, string message, IEnumerable<object> args ) {
            return (c.Resolve<Debug>() ?? (( pmessageCode, pmessage, pargs)=> default(bool) ) )( messageCode, message, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool Verbose(this Callback c , string messageCode, string message, IEnumerable<object> args ) {
            return (c.Resolve<Verbose>() ?? (( pmessageCode, pmessage, pargs)=> default(bool) ) )( messageCode, message, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool ExceptionThrown(this Callback c , string exceptionType, string message, string stacktrace ) {
            return (c.Resolve<ExceptionThrown>() ?? (( pexceptionType, pmessage, pstacktrace)=> default(bool) ) )( exceptionType, message, stacktrace);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool Progress(this Callback c , int activityId, string activity, int progress, string message, IEnumerable<object> args ) {
            return (c.Resolve<Progress>() ?? (( pactivityId, pactivity, pprogress, pmessage, pargs)=> default(bool) ) )( activityId, activity, progress, message, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool ProgressComplete(this Callback c , int activityId, string activity, string message, IEnumerable<object> args ) {
            return (c.Resolve<ProgressComplete>() ?? (( pactivityId, pactivity, pmessage, pargs)=> default(bool) ) )( activityId, activity, message, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static Callback GetHostDelegate(this Callback c  ) {
            return (c.Resolve<GetHostDelegate>() ?? (()=> default(Callback) ) )();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool ShouldContinueWithUntrustedPackageSource(this Callback c , string package, string packageSource ) {
            return (c.Resolve<ShouldContinueWithUntrustedPackageSource>() ?? (( ppackage, ppackageSource)=> default(bool) ) )( package, packageSource);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool ShouldProcessPackageInstall(this Callback c , string packageName, string version, string source ) {
            return (c.Resolve<ShouldProcessPackageInstall>() ?? (( ppackageName, pversion, psource)=> default(bool) ) )( packageName, version, source);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool ShouldProcessPackageUninstall(this Callback c , string packageName, string version ) {
            return (c.Resolve<ShouldProcessPackageUninstall>() ?? (( ppackageName, pversion)=> default(bool) ) )( packageName, version);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool ShouldContinueAfterPackageInstallFailure(this Callback c , string packageName, string version, string source ) {
            return (c.Resolve<ShouldContinueAfterPackageInstallFailure>() ?? (( ppackageName, pversion, psource)=> default(bool) ) )( packageName, version, source);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool ShouldContinueAfterPackageUninstallFailure(this Callback c , string packageName, string version, string source ) {
            return (c.Resolve<ShouldContinueAfterPackageUninstallFailure>() ?? (( ppackageName, pversion, psource)=> default(bool) ) )( packageName, version, source);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool ShouldContinueRunningInstallScript(this Callback c , string packageName, string version, string source, string scriptLocation ) {
            return (c.Resolve<ShouldContinueRunningInstallScript>() ?? (( ppackageName, pversion, psource, pscriptLocation)=> default(bool) ) )( packageName, version, source, scriptLocation);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool ShouldContinueRunningUninstallScript(this Callback c , string packageName, string version, string source, string scriptLocation ) {
            return (c.Resolve<ShouldContinueRunningUninstallScript>() ?? (( ppackageName, pversion, psource, pscriptLocation)=> default(bool) ) )( packageName, version, source, scriptLocation);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool AskPermission(this Callback c , string permission ) {
            return (c.Resolve<AskPermission>() ?? (( ppermission)=> default(bool) ) )( permission);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
		public static bool WhatIf(this Callback c  ) {
            return (c.Resolve<WhatIf>() ?? (()=> default(bool) ) )();
        }
        #endregion

        #region generate-powershell collection-callbacks
public static string PowerShellScriptcollection = @"
		function fn-LookupString {
			param(
				[string] $p_name)
			return $_callback.Invoke( ""LookupString"", @($p_name) )
		}
		function fn-LookupEnumerable {
			param(
				[string] $p_name)
			return $_callback.Invoke( ""LookupEnumerable"", @($p_name) )
		}
";
#endregion


        #region generate-powershell service-api-callbacks
public static string PowerShellScriptserviceapi = @"
		function fn-GetNuGetExePath {
			return $_callback.Invoke( ""GetNuGetExePath"",@() )
		}
		function fn-GetNuGetDllPath {
			return $_callback.Invoke( ""GetNuGetDllPath"",@() )
		}
		function fn-DownloadFile {
			param(
				[string] $p_remoteLocation, 
				[string] $p_localLocation)
			return $_callback.Invoke( ""DownloadFile"", @($p_remoteLocation,$p_localLocation) )
		}
		function fn-AddPinnedItemToTaskbar {
			param(
				[string] $p_item)
			return $_callback.Invoke( ""AddPinnedItemToTaskbar"", @($p_item) )
		}
		function fn-RemovePinnedItemFromTaskbar {
			param(
				[string] $p_item)
			return $_callback.Invoke( ""RemovePinnedItemFromTaskbar"", @($p_item) )
		}
		function fn-CreateShortcutLink {
			param(
				[string] $p_linkPath, 
				[string] $p_targetPath, 
				[string] $p_description, 
				[string] $p_workingDirectory, 
				[string] $p_arguments)
			return $_callback.Invoke( ""CreateShortcutLink"", @($p_linkPath,$p_targetPath,$p_description,$p_workingDirectory,$p_arguments) )
		}
		function fn-UnzipFileIncremental {
			param(
				[string] $p_zipFile, 
				[string] $p_folder)
			return $_callback.Invoke( ""UnzipFileIncremental"", @($p_zipFile,$p_folder) )
		}
		function fn-UnzipFile {
			param(
				[string] $p_zipFile, 
				[string] $p_folder)
			return $_callback.Invoke( ""UnzipFile"", @($p_zipFile,$p_folder) )
		}
		function fn-AddFileAssociation {
			return $_callback.Invoke( ""AddFileAssociation"",@() )
		}
		function fn-RemoveFileAssociation {
			return $_callback.Invoke( ""RemoveFileAssociation"",@() )
		}
		function fn-AddExplorerMenuItem {
			return $_callback.Invoke( ""AddExplorerMenuItem"",@() )
		}
		function fn-RemoveExplorerMenuItem {
			return $_callback.Invoke( ""RemoveExplorerMenuItem"",@() )
		}
		function fn-SetEnvironmentVariable {
			param(
				[string] $p_variable, 
				[string] $p_value, 
				[string] $p_context)
			return $_callback.Invoke( ""SetEnvironmentVariable"", @($p_variable,$p_value,$p_context) )
		}
		function fn-RemoveEnvironmentVariable {
			param(
				[string] $p_variable, 
				[string] $p_context)
			return $_callback.Invoke( ""RemoveEnvironmentVariable"", @($p_variable,$p_context) )
		}
		function fn-AddFolderToPath {
			return $_callback.Invoke( ""AddFolderToPath"",@() )
		}
		function fn-RemoveFolderFromPath {
			return $_callback.Invoke( ""RemoveFolderFromPath"",@() )
		}
		function fn-InstallMSI {
			return $_callback.Invoke( ""InstallMSI"",@() )
		}
		function fn-RemoveMSI {
			return $_callback.Invoke( ""RemoveMSI"",@() )
		}
		function fn-StartProcess {
			return $_callback.Invoke( ""StartProcess"",@() )
		}
		function fn-InstallVSIX {
			return $_callback.Invoke( ""InstallVSIX"",@() )
		}
		function fn-UninstallVSIX {
			return $_callback.Invoke( ""UninstallVSIX"",@() )
		}
		function fn-InstallPowershellScript {
			return $_callback.Invoke( ""InstallPowershellScript"",@() )
		}
		function fn-UninstallPowershellScript {
			return $_callback.Invoke( ""UninstallPowershellScript"",@() )
		}
		function fn-SearchForExecutable {
			return $_callback.Invoke( ""SearchForExecutable"",@() )
		}
		function fn-GetUserBinFolder {
			return $_callback.Invoke( ""GetUserBinFolder"",@() )
		}
		function fn-GetSystemBinFolder {
			return $_callback.Invoke( ""GetSystemBinFolder"",@() )
		}
		function fn-CopyFile {
			param(
				[string] $p_sourcePath, 
				[string] $p_destinationPath)
			return $_callback.Invoke( ""CopyFile"", @($p_sourcePath,$p_destinationPath) )
		}
		function fn-CopyFolder {
			return $_callback.Invoke( ""CopyFolder"",@() )
		}
		function fn-Delete {
			param(
				[string] $p_path)
			return $_callback.Invoke( ""Delete"", @($p_path) )
		}
		function fn-DeleteFolder {
			param(
				[string] $p_folder)
			return $_callback.Invoke( ""DeleteFolder"", @($p_folder) )
		}
		function fn-CreateFolder {
			param(
				[string] $p_folder)
			return $_callback.Invoke( ""CreateFolder"", @($p_folder) )
		}
		function fn-DeleteFile {
			param(
				[string] $p_filename)
			return $_callback.Invoke( ""DeleteFile"", @($p_filename) )
		}
		function fn-BeginTransaction {
			return $_callback.Invoke( ""BeginTransaction"",@() )
		}
		function fn-AbortTransaction {
			return $_callback.Invoke( ""AbortTransaction"",@() )
		}
		function fn-EndTransaction {
			return $_callback.Invoke( ""EndTransaction"",@() )
		}
		function fn-GenerateUninstallScript {
			return $_callback.Invoke( ""GenerateUninstallScript"",@() )
		}
		function fn-GetKnownFolder {
			param(
				[string] $p_knownFolder)
			return $_callback.Invoke( ""GetKnownFolder"", @($p_knownFolder) )
		}
		function fn-IsElevated {
			return $_callback.Invoke( ""IsElevated"",@() )
		}
";
#endregion

        #region generate-powershell core-supplied-callbacks
public static string PowerShellScriptcoresupplied = @"
		function fn-OkToContinue {
			return $_callback.Invoke( ""OkToContinue"",@() )
		}
		function fn-YieldPackage {
			param(
				[string] $p_fastPath, 
				[string] $p_name, 
				[string] $p_version, 
				[string] $p_versionScheme, 
				[string] $p_summary, 
				[string] $p_source)
			return $_callback.Invoke( ""YieldPackage"", @($p_fastPath,$p_name,$p_version,$p_versionScheme,$p_summary,$p_source) )
		}
		function fn-YieldSource {
			param(
				[string] $p_name, 
				[string] $p_location, 
				[bool] $p_isTrusted)
			return $_callback.Invoke( ""YieldSource"", @($p_name,$p_location,$p_isTrusted) )
		}
		function fn-YieldMetadataDefinition {
			param(
				[string] $p_name, 
				[string] $p_expectedType, 
				[string[]] $p_permittedValues)
			return $_callback.Invoke( ""YieldMetadataDefinition"", @($p_name,$p_expectedType,$p_permittedValues) )
		}
		function fn-YieldInstallationOptionsDefinition {
			param(
				[string] $p_name, 
				[string] $p_expectedType, 
				[bool] $p_required, 
				[string[]] $p_permittedValues)
			return $_callback.Invoke( ""YieldInstallationOptionsDefinition"", @($p_name,$p_expectedType,$p_required,$p_permittedValues) )
		}
";
#endregion

        #region generate-powershell host-supplied-callbacks
public static string PowerShellScripthostsupplied = @"
		function fn-GetMetadataKeys {
			return $_callback.Invoke( ""GetMetadataKeys"",@() )
		}
		function fn-GetMetadataValues {
			param(
				[string] $p_key)
			return $_callback.Invoke( ""GetMetadataValues"", @($p_key) )
		}
		function fn-GetInstallationOptionKeys {
			return $_callback.Invoke( ""GetInstallationOptionKeys"",@() )
		}
		function fn-GetInstallationOptionValues {
			param(
				[string] $p_key)
			return $_callback.Invoke( ""GetInstallationOptionValues"", @($p_key) )
		}
		function fn-PackageSources {
			return $_callback.Invoke( ""PackageSources"",@() )
		}
		function fn-GetConfiguration {
			param(
				[string] $p_path)
			return $_callback.Invoke( ""GetConfiguration"", @($p_path) )
		}
		function fn-IsCancelled {
			return $_callback.Invoke( ""IsCancelled"",@() )
		}
		function fn-Warning {
			param(
				[string] $p_messageCode, 
				[string] $p_message, 
				[object[]] $p_args)
			return $_callback.Invoke( ""Warning"", @($p_messageCode,$p_message,$p_args) )
		}
		function fn-Message {
			param(
				[string] $p_messageCode, 
				[string] $p_message, 
				[object[]] $p_args)
			return $_callback.Invoke( ""Message"", @($p_messageCode,$p_message,$p_args) )
		}
		function fn-Error {
			param(
				[string] $p_messageCode, 
				[string] $p_message, 
				[object[]] $p_args)
			return $_callback.Invoke( ""Error"", @($p_messageCode,$p_message,$p_args) )
		}
		function fn-Debug {
			param(
				[string] $p_messageCode, 
				[string] $p_message, 
				[object[]] $p_args)
			return $_callback.Invoke( ""Debug"", @($p_messageCode,$p_message,$p_args) )
		}
		function fn-Verbose {
			param(
				[string] $p_messageCode, 
				[string] $p_message, 
				[object[]] $p_args)
			return $_callback.Invoke( ""Verbose"", @($p_messageCode,$p_message,$p_args) )
		}
		function fn-ExceptionThrown {
			param(
				[string] $p_exceptionType, 
				[string] $p_message, 
				[string] $p_stacktrace)
			return $_callback.Invoke( ""ExceptionThrown"", @($p_exceptionType,$p_message,$p_stacktrace) )
		}
		function fn-Progress {
			param(
				[int] $p_activityId, 
				[string] $p_activity, 
				[int] $p_progress, 
				[string] $p_message, 
				[object[]] $p_args)
			return $_callback.Invoke( ""Progress"", @($p_activityId,$p_activity,$p_progress,$p_message,$p_args) )
		}
		function fn-ProgressComplete {
			param(
				[int] $p_activityId, 
				[string] $p_activity, 
				[string] $p_message, 
				[object[]] $p_args)
			return $_callback.Invoke( ""ProgressComplete"", @($p_activityId,$p_activity,$p_message,$p_args) )
		}
		function fn-GetHostDelegate {
			return $_callback.Invoke( ""GetHostDelegate"",@() )
		}
		function fn-ShouldContinueWithUntrustedPackageSource {
			param(
				[string] $p_package, 
				[string] $p_packageSource)
			return $_callback.Invoke( ""ShouldContinueWithUntrustedPackageSource"", @($p_package,$p_packageSource) )
		}
		function fn-ShouldProcessPackageInstall {
			param(
				[string] $p_packageName, 
				[string] $p_version, 
				[string] $p_source)
			return $_callback.Invoke( ""ShouldProcessPackageInstall"", @($p_packageName,$p_version,$p_source) )
		}
		function fn-ShouldProcessPackageUninstall {
			param(
				[string] $p_packageName, 
				[string] $p_version)
			return $_callback.Invoke( ""ShouldProcessPackageUninstall"", @($p_packageName,$p_version) )
		}
		function fn-ShouldContinueAfterPackageInstallFailure {
			param(
				[string] $p_packageName, 
				[string] $p_version, 
				[string] $p_source)
			return $_callback.Invoke( ""ShouldContinueAfterPackageInstallFailure"", @($p_packageName,$p_version,$p_source) )
		}
		function fn-ShouldContinueAfterPackageUninstallFailure {
			param(
				[string] $p_packageName, 
				[string] $p_version, 
				[string] $p_source)
			return $_callback.Invoke( ""ShouldContinueAfterPackageUninstallFailure"", @($p_packageName,$p_version,$p_source) )
		}
		function fn-ShouldContinueRunningInstallScript {
			param(
				[string] $p_packageName, 
				[string] $p_version, 
				[string] $p_source, 
				[string] $p_scriptLocation)
			return $_callback.Invoke( ""ShouldContinueRunningInstallScript"", @($p_packageName,$p_version,$p_source,$p_scriptLocation) )
		}
		function fn-ShouldContinueRunningUninstallScript {
			param(
				[string] $p_packageName, 
				[string] $p_version, 
				[string] $p_source, 
				[string] $p_scriptLocation)
			return $_callback.Invoke( ""ShouldContinueRunningUninstallScript"", @($p_packageName,$p_version,$p_source,$p_scriptLocation) )
		}
		function fn-AskPermission {
			param(
				[string] $p_permission)
			return $_callback.Invoke( ""AskPermission"", @($p_permission) )
		}
		function fn-WhatIf {
			return $_callback.Invoke( ""WhatIf"",@() )
		}
";
#endregion

    }
}
