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
#if STATIC_LINK
using NuGet;
using NuGet.Commands;
#else
#endif
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using Core.Extensions;
    using Core.Platform;
    using Core.Process;
    using Win32;

    internal class ChocolateyState : Dispatcher, IDisposable {
        private static readonly Regex _rxPkgParse = new Regex(@"'(?<pkgId>\S*)\s(?<ver>.*?)'");
        internal static string _nuGetExePath;
        private readonly Regex _rxFastPath = new Regex(@"\$(?<source>[\w,\+,\/,=]*)\\(?<id>[\w,\+,\/,=]*)\\(?<version>[\w,\+,\/,=]*)");
        protected Lazy<PackageRepositoryFactory> _packageRepositoryFactory = new Lazy<PackageRepositoryFactory>(() => new PackageRepositoryFactory());
        private List<IPackageRepository> _repositories;

        internal ChocolateyState(Func<string, IEnumerable<object>, object> c) : base(c) {
            if (NuGet.NuGetCorePath == null) {
                NuGet.NuGetCorePath = GetNuGetDllPath();
            }
        }

        internal string ChocolateyModuleFolder {
            get {
                return Path.GetDirectoryName(Path.GetFullPath(Assembly.GetExecutingAssembly().Location));
            }
        }

        public string ChocolateyModuleFile {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
            get {
                return Path.Combine(ChocolateyModuleFolder, "Chocolatey.psd1");
            }
        }

        internal string EtcPath {
            get {
                return Path.Combine(ChocolateyModuleFolder, "etc");
            }
        }

        internal string ChocolateyConfigPath {
            get {
                return Path.Combine(RootInstallationPath, "chocolateyinstall", "Chocolatey.config");
            }
        }

        internal XDocument Config {
            get {
                try {
                    var doc = XDocument.Load(ChocolateyConfigPath);
                    if (doc.Root != null && doc.Root.Name == "chocolatey") {
                        return doc;
                    }

                    // doc root isn't right. make a new one!
                } catch {
                    // bad doc 
                }
                return XDocument.Load(new MemoryStream(@"<?xml version=""1.0""?>
<chocolatey>
    <useNuGetForSources>false</useNuGetForSources>
    <sources>
        <source id=""chocolatey"" value=""http://chocolatey.org/api/v2/"" />
    </sources>
</chocolatey>
".ToByteArray()));
            }
            set {
                Verbose("Saving Chocolatey Config", ChocolateyConfigPath);

                if (value == null) {
                    return;
                }
                
                CreateFolder(Path.GetDirectoryName(ChocolateyConfigPath));
                value.Save(ChocolateyConfigPath);
            }
        }

        internal string HelperModulePath {
            get {
                return Path.Combine(EtcPath, "Helpers.psd1");
            }
        }

        private string SystemDrive {
            get {
                var drive = Environment.GetEnvironmentVariable("SystemDrive");

                if (string.IsNullOrEmpty(drive)) {
                    return "c:\\";
                }
                return drive + "\\";
            }
        }

        internal string RootInstallationPath {
            get {
                var rip = Environment.GetEnvironmentVariable("ChocolateyPath");
                if (string.IsNullOrEmpty(rip)) {
                    // current default
                    rip = Path.Combine(SystemDrive, @"\", "Chocolatey");

                    // store it.
                    Environment.SetEnvironmentVariable("ChocolateyPath", rip, EnvironmentVariableTarget.User);
                    Environment.SetEnvironmentVariable("ChocolateyPath", rip, EnvironmentVariableTarget.Process);
                }

                if (!rip.DirectoryHasDriveLetter()) {
                    rip = rip.TrimStart('\\');
                    rip = Path.Combine(SystemDrive, rip);
                    Environment.SetEnvironmentVariable("ChocolateyPath", rip, EnvironmentVariableTarget.User);
                    Environment.SetEnvironmentVariable("ChocolateyPath", rip, EnvironmentVariableTarget.Process);
                }
                if (!Directory.Exists(rip)) {
                    Directory.CreateDirectory(rip);
                }
                return rip;
            }
        }

        internal string PackageInstallationPath {
            get {
                var path = Path.Combine(RootInstallationPath, "lib");
                if (!Directory.Exists(path)) {
                    CreateFolder(path);
                }
                return path;
            }
        }

        internal string PackageExePath {
            get {
                var path = Path.Combine(RootInstallationPath, "bin");
                if (!Directory.Exists(path)) {
                    CreateFolder(path);
                }
                return Path.Combine(RootInstallationPath, "bin");
            }
        }

        internal string NuGetExePath {
            get {
#if STATIC_LINK 
                return typeof (InstallCommand).Assembly.Location;
#else
                return _nuGetExePath ?? (_nuGetExePath = GetNuGetExePath());
#endif
            }
        }

        internal bool AllowPrereleaseVersions {
            get {
                return IsSwitchSet("AllowPrereleaseVersions");
            }
        }

        internal bool LeavePartialPackageInstalled {
            get {
                return IsSwitchSet("LeavePartialPackageInstalled");
            }
        }


        internal bool AllVersions {
            get {
                return IsSwitchSet("AllVersions");
            }
        }

        internal bool LocalOnly {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
            get {
                return IsSwitchSet("LocalOnly");
            }
        }

        internal string InstallArguments {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
            get {
                return InstallOption("InstallArguments");
            }
        }

        internal string PackageParameters {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
            get {
                return InstallOption("PackageParameters");
            }
        }

        internal bool OverrideArguments {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
            get {
                return InstallOption("OverrideArguments").IsTrue();
            }
        }

        internal string Hint {
            get {
                return MetadataValue("Hint");
            }
        }

        internal bool IgnoreDependencies {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
            get {
                return InstallOption("IgnoreDependencies").IsTrue();
            }
        }

        internal bool ForceX86 {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
            get {
                return InstallOption("ForceX86").IsTrue();
            }
        }

        internal Dictionary<string, PackageSource> AllPackageRepositories {
            get {
                try {
                    return Config.XPathSelectElements("/chocolatey/sources/source")
                        .Where(each => each.Attribute("id") != null && each.Attribute("value") != null)
                        .ToDictionaryNicely(each => each.Attribute("id").Value, each => new PackageSource {
                            Name = each.Attribute("id").Value,
                            Location = each.Attribute("value").Value,
                            Trusted = each.Attributes("trusted").Any() && each.Attribute("trusted").Value.IsTrue()
                        }, StringComparer.OrdinalIgnoreCase);
                } catch {
                }
                return new Dictionary<string, PackageSource> {
                    {
                        "chocolatey", new PackageSource {
                            Name = "chocolatey",
                            Location = "http://chocolatey.org/api/v2/",
                            Trusted = false,
                        }
                    }
                };
            }
        }

        internal IEnumerable<IPackageRepository> Repositories {
            get {
                if (_repositories == null) {
                    _repositories = new List<IPackageRepository>();

                    // grab all the repositories that are specified by the user.

                    var allSources = (PackageSources() ?? AllPackageRepositories.Values.Select(each => each.Name)).ToArray();

                    // did we get any?
                    if (allSources.Length == 0) {
                        // use em all.
                        allSources = AllPackageRepositories.Values.Select(each => each.Name).ToArray();
                    }
                    _repositories.AddRange(allSources.Select(each => ResolveRepository(each)).WhereNotNull().Select(each => {
                        try {
                            return _packageRepositoryFactory.Value.CreateRepository(each);
                        } catch {

                        }
                        return null;
                    }).WhereNotNull());
                }

                // return a copy instead of the collection so that it can get modified without botching calls in progress.
                return _repositories.ToArray();
            }
        }

        private string PowerShellExe {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
            get {
                return Path.Combine(KnownFolders.GetFolderPath(KnownFolder.Windows), @"System32\WindowsPowerShell\v1.0\", "PowerShell.exe");
            }
        }

        internal void AddPackageSource(string id, string location, bool trusted) {
            // quick and dirty. 
            try {
                id = id.ToLower(CultureInfo.CurrentCulture);
                if (AllPackageRepositories.ContainsKey(id)) {
                    RemovePackageSource(id);
                }
                var config = Config;
                var sources = config.XPathSelectElements("/chocolatey/sources").FirstOrDefault();
                if (sources == null) {
                    config.Root.Add(sources = new XElement("sources"));
                }
                var source = new XElement("source");
                source.SetAttributeValue("id", id);
                source.SetAttributeValue("value", location);
                if (trusted) {
                    source.SetAttributeValue("trusted", true);
                }
                sources.Add(source);
                Config = config;
            } catch (Exception e) {
                e.Dump();
            }
        }

        internal void RemovePackageSource(string id) {
            id = id.ToLower(CultureInfo.CurrentCulture);
            var config = Config;
            var source = config.XPathSelectElements("/chocolatey/sources/source[@id='{0}']".format(id)).FirstOrDefault();
            if (source != null) {
                source.Remove();
                Config = config;
            }
        }

        public string MetadataValue(string switchName) {
            var key = GetMetadataKeys().FirstOrDefault(each => each.Equals(switchName, StringComparison.CurrentCultureIgnoreCase));
            return GetMetadataValues(key ?? switchName).FirstOrDefault();
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public string[] MetadataValues(string switchName) {
            var key = GetMetadataKeys().FirstOrDefault(each => each.Equals(switchName, StringComparison.CurrentCultureIgnoreCase));
            return GetMetadataValues(key ?? switchName).ToArray();
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public string InstallOption(string switchName) {
            var key = GetInstallationOptionKeys().FirstOrDefault(each => each.Equals(switchName, StringComparison.CurrentCultureIgnoreCase));
            return GetInstallationOptionValues(key ?? switchName).FirstOrDefault();
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public string[] InstallOptions(string switchName) {
            var key = GetInstallationOptionKeys().FirstOrDefault(each => each.Equals(switchName, StringComparison.CurrentCultureIgnoreCase));
            return GetInstallationOptionValues(key ?? switchName).ToArray();
        }

        public bool IsSwitchSet(string switchName) {
            return MetadataValue(switchName).IsTrue();
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public void GenerateLaunchFile() {
        }

        public string MakeFastPath(string source, string id, string version) {
            return @"${0}\{1}\{2}".format(source.ToBase64(), id.ToBase64(), version.ToBase64());
        }

        public bool TryParseFastPath(string fastPath, out string source, out string id, out string version) {
            var match = _rxFastPath.Match(fastPath);
            source = match.Success ? match.Groups["source"].Value.FromBase64() : null;
            id = match.Success ? match.Groups["id"].Value.FromBase64() : null;
            version = match.Success ? match.Groups["version"].Value.FromBase64() : null;
            return match.Success;
        }

        public bool NuGetInstall(string source, string packageId, string version, out List<Tuple<string, string>> successful, out List<Tuple<string, string>> already, out List<Tuple<string, string>> failed) {
            var s = new List<Tuple<string, string>>();
            var a = new List<Tuple<string, string>>();
            var f = new List<Tuple<string, string>>();

            using (var p = AsyncProcess.Start(NuGetExePath, @"install ""{0}"" -Version ""{1}"" -Source ""{2}"" -PackageSaveMode ""nuspec;nupkg""  -OutputDirectory ""{3}"" -Verbosity detailed".format(packageId, version, source, PackageInstallationPath))) {
                foreach (var l in p.StandardOutput) {
                    if (string.IsNullOrEmpty(l)) {
                        continue;
                    }
                    Verbose("NuGet", l, null);
                    // Successfully installed 'ComicRack 0.9.162'.
                    if (l.Contains("Successfully installed")) {
                        var pkg = _rxPkgParse.Match(l);
                        s.Add(new Tuple<string, string>(pkg.Groups["pkgId"].Value, pkg.Groups["ver"].Value));
                        continue;
                    }

                    if (l.Contains("already installed")) {
                        var pkg = _rxPkgParse.Match(l);
                        a.Add(new Tuple<string, string>(pkg.Groups["pkgId"].Value, pkg.Groups["ver"].Value));
                        continue;
                    }

                    if (l.Contains("not installed")) {
                        var pkg = _rxPkgParse.Match(l);
                        f.Add(new Tuple<string, string>(pkg.Groups["pkgId"].Value, pkg.Groups["ver"].Value));
                        continue;
                    }
                }

                foreach (var l in p.StandardError) {
                    if (string.IsNullOrEmpty(l)) {
                        continue;
                    }
                    Warning("NuGet", l, null);
                }

                successful = s;
                already = a;
                failed = f;

                return p.ExitCode == 0;
            }
        }

        /// <summary>
        ///     Calls PowerShell with the chocolatey helper module imported.
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="path"></param>
        /// <param name="packageFolder"></param>
        /// <returns></returns>
        internal void InvokeChocolateyScript(string packageFolder, string packageName, string path) {
            Environment.SetEnvironmentVariable("chocolateyPackageFolder", packageFolder);
            Environment.SetEnvironmentVariable("chocolateyInstallArguments", "");
            Environment.SetEnvironmentVariable("chocolateyInstallOverride", "");

            var script = new ChocolateyScript(this);
            script.InvokeChocolateyScript(path);

            Environment.SetEnvironmentVariable("chocolateyPackageFolder", null);
            Environment.SetEnvironmentVariable("chocolateyInstallArguments", null);
            Environment.SetEnvironmentVariable("chocolateyInstallOverride", null);
        }

        public void GenerateBins(string pkgPath) {
            var exes = Directory.EnumerateFiles(pkgPath, "*.exe", SearchOption.AllDirectories);
            foreach (var exe in exes) {
                if (FilesystemExtensions.FileExists((exe + ".ignore"))) {
                    continue;
                }
                if (FilesystemExtensions.FileExists(exe + ".gui")) {
                    GenerateGuiBin(exe);
                    continue;
                }
                GenerateConsoleBin(exe);
            }
        }

        public string GetBatFileLocation(string exe, string name) {
            if (string.IsNullOrEmpty(name)) {
                return Path.Combine(PackageExePath, Path.GetFileNameWithoutExtension(exe) + ".bat");
            } else {
                return Path.Combine(PackageExePath, Path.GetFileNameWithoutExtension(name) + ".bat");
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Can be called from powershell")]
        public void GeneratePS1ScriptBin(string ps1, string name = null) {
            File.WriteAllText(GetBatFileLocation(ps1, name), @"@echo off
powershell -NoProfile -ExecutionPolicy unrestricted -Command ""& '{0}'  %*""".format(ps1));
        }

        public void GenerateConsoleBin(string exe, string name = null) {
            File.WriteAllText(GetBatFileLocation(exe, name), @"@echo off
SET DIR=%~dp0%
cmd /c ""%DIR%{0} %*""
exit /b %ERRORLEVEL%".format(PackageExePath.RelativePathTo(exe)));
        }

        public void GenerateGuiBin(string exe, string name = null) {
            File.WriteAllText(GetBatFileLocation(exe, name), @"@echo off
SET DIR=%~dp0%
start """" ""%DIR%{0}"" %*".format(PackageExePath.RelativePathTo(exe)));
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public void RemoveBins(string pkgPath) {
            var exes = Directory.EnumerateFiles(pkgPath, "*.exe", SearchOption.AllDirectories);
            foreach (var exe in exes) {
                if (FilesystemExtensions.FileExists(exe + ".ignore")) {
                    continue;
                }
                if (FilesystemExtensions.FileExists(exe + ".gui")) {
                    RemoveGuiBin(exe);
                    continue;
                }
                RemoveConsoleBin(exe);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public void RemoveConsoleBin(string exe, string name = null) {
            GetBatFileLocation(exe, name).TryHardToDelete();
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public void RemoveGuiBin(string exe, string name = null) {
            GetBatFileLocation(exe, name).TryHardToDelete();
        }

        public bool IsPackageInstalled(string name, string version) {
            return (from pkgFile in Directory.EnumerateFileSystemEntries(PackageInstallationPath, "*.nupkg", SearchOption.AllDirectories) 
                    where PackageHelper.IsPackageFile(pkgFile) select new ZipPackage(pkgFile))
                    .Any(pkg => pkg.Id.Equals(name, StringComparison.OrdinalIgnoreCase) && pkg.Version.ToString().Equals(version, StringComparison.OrdinalIgnoreCase));
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        internal string InstalledPackageFile(string name, string version) {
            return (from pkgFile in Directory.EnumerateFileSystemEntries(PackageInstallationPath, "*.nupkg", SearchOption.AllDirectories) 
                    where PackageHelper.IsPackageFile(pkgFile) 
                    let pkg = new ZipPackage(pkgFile) 
                        where pkg.Id.Equals(name, StringComparison.OrdinalIgnoreCase) && pkg.Version.ToString().Equals(version, StringComparison.OrdinalIgnoreCase) 
                        select pkgFile).FirstOrDefault();
        }

        protected bool PostProcessPackageInstall(string pkgName, string pkgVersion) {
            var pkgPath = Path.Combine(PackageInstallationPath, pkgName + "." + pkgVersion);

            var scripts = Directory.EnumerateFiles(pkgPath, "chocolateyInstall.ps1", SearchOption.AllDirectories);
            var script = scripts.FirstOrDefault();
            if (script != null) {
                // Error("Missing chocolateyinstall.ps1", pkgPath, null);
                // break;
                // Now for the script.
                InvokeChocolateyScript(pkgPath, pkgName, script);
            }

            // Now handle 'bins'
            GenerateBins(pkgPath);

            return true;
        }

        public bool InstallSingleChocolateyPackage(PackageReference packageReference) {
            /*
             Progress(1, "Installing", (n * 100) / success.Count, "{0} '{1}' ({2} of {3})", new object[] {
                                        (each.Item1 == id ? "SoftwareIdentity" : "Dependent SoftwareIdentity"), each.Item1, n++, success.Count
                                    });
            */

            List<Tuple<string, string>> success;
            List<Tuple<string, string>> already;
            List<Tuple<string, string>> failed;

            if (ShouldProcessPackageInstall(packageReference.Id, packageReference.Version, packageReference.Source)) {
                // Get NuGet to install the SoftwareIdentity
                if (NuGetInstall(packageReference.Source, packageReference.Id, packageReference.Version, out success, out already, out failed)) {
                    // NuGet Installations went ok. 
                    switch (success.Count) {
                        case 0:
                            // didn't actually install anything. that's odd.
                            if (already.Count > 0 && failed.Count == 0) {
                                // looks like it was already there?
                                Verbose("Skipped", "Package '{0} v{1}' already installed", packageReference.Id, packageReference.Version);
                                return true;
                            } else {
                                Verbose("NotInstalled", "Package '{0} v{1}' failed to install", packageReference.Id, packageReference.Version);
                            }
                            return false;

                        case 1:
                            try {
                                // awesome. Just like we thought should happen
                                if (PostProcessPackageInstall(success[0].Item1, success[0].Item2)) {
                                    YieldPackage(packageReference.FastPath, packageReference.Id, packageReference.Version, "semver", packageReference.Package.Summary, GetNameForSource(packageReference.Source));
                                    return true;
                                } else {
                                    Verbose("PostProcessPackageInstall returned false", "This is unexpected");
                                }
                            } catch (Exception e) {
                                // Sad. Package had a problem.
                                // roll it back.
                                Verbose("PostProcessPackageInstall or YieldPackage threw exception","{0}/{1} \r\n{2}", e.GetType().Name, e.Message, e.StackTrace);
                                e.Dump();
                            }
                            if (!LeavePartialPackageInstalled) {
                                UninstallPackage(packageReference.FastPath, true);
                            }
                            return false;

                        default:
                            // what? more than one installed. Not good. Roll em back and complain.
                            // uninstall package.
                            Error("SomethingBad", "Package '{0} v{1}' installed more than one package, and this was unexpected",packageReference.Id, packageReference.Version );
                            break;
                    }
                }
            } else {
                return WhatIf();
            }

            return false;
        }


        public bool UninstallPackage(string fastPath, bool isRollback) {
            string source;
            string id;
            string version;

            if (TryParseFastPath(fastPath, out source, out id, out version)) {
                if (!id.EndsWith(".install", StringComparison.OrdinalIgnoreCase)) {
                    // try doing an uninstall of that first.
                    // todo: hack to uninstall .install package first.
                    var nupkgs = Directory.EnumerateFileSystemEntries(PackageInstallationPath, "*.nupkg", SearchOption.AllDirectories);
                    foreach (var pkgFile in nupkgs) {
                        if (PackageHelper.IsPackageFile(pkgFile)) {
                            var pkg = new ZipPackage(pkgFile);
                            // todo: wildcard matching?
                            if (pkg.Id.Equals(id + ".install", StringComparison.CurrentCultureIgnoreCase)
                                || pkg.Id.Equals(id + ".portable", StringComparison.CurrentCultureIgnoreCase)
                                || pkg.Id.Equals(id + ".app", StringComparison.CurrentCultureIgnoreCase)
                                || pkg.Id.Equals(id + ".commandline", StringComparison.CurrentCultureIgnoreCase)
                                ) {
                                UninstallPackage(MakeFastPath(pkgFile, pkg.Id, pkg.Version.ToString()), isRollback);
                            }
                        }
                    }
                }

                // installed packages should have the local nupkg as their source, but we might have gotten here in an odd way.
                if (!source.FileExists()) {
                    source = InstalledPackageFile(id, version);
                    if (source == null) {
                        // now, this is bad
                        return false;
                    }
                }

                if (isRollback || ShouldProcessPackageUninstall(id, version)) {
                    var pkgPath = Path.GetDirectoryName(source);

                    Verbose("Removing Package", pkgPath, null);

                    // remove the bins.
                    RemoveBins(pkgPath);

                    // and run the install script.
                    try {
                        var scripts = Directory.EnumerateFiles(pkgPath, "chocolateyunInstall.ps1", SearchOption.AllDirectories);
                        var script = scripts.FirstOrDefault();
                        if (script != null) {
                            // found the script, let's run that.
                            InvokeChocolateyScript(pkgPath, id, script);
                        }
                    } catch (Exception e) {
                        e.Dump();
                    }
                    // and remove the package directory.
                    pkgPath.TryHardToDelete();

                    if (!isRollback) {
                        YieldPackage(fastPath, id, version, "semver", "", "");
                    }

                    if (IsElevated()) {
                        EnvironmentUtility.SystemPath = EnvironmentUtility.SystemPath.RemoveMissingFolders();
                    }

                    EnvironmentUtility.UserPath = EnvironmentUtility.UserPath.RemoveMissingFolders();
                    EnvironmentUtility.Path = EnvironmentUtility.Path.RemoveMissingFolders();
                    return true;
                }

                return false;
            }
            return false;

        }


        internal string ResolveRepository(string repository) {
            if (Uri.IsWellFormedUriString(repository, UriKind.Absolute)) {
                var uri = new Uri(repository, UriKind.Absolute);
                switch (uri.Scheme.ToLower(CultureInfo.CurrentCulture)) {
                    case "http":
                    case "https":
                    case "file":
                        // looks like a valid url
                        return repository;
                }
            }

            try {
                if (Directory.Exists(repository)) {
                    return repository;
                }
            } catch {
                // ignore.
            }
            var apr = AllPackageRepositories;
            if (apr.ContainsKey(repository)) {
                return apr[repository].Location;
            }
            Error("Invalid Package Source", repository);
            return null;
        }

        internal IEnumerable<IPackage> FilterOnVersion(IEnumerable<IPackage> pkgs, string requiredVersion, string minimumVersion, string maximumVersion) {
            if (requiredVersion.Is()) {
                pkgs = pkgs.Where(each => each.Version == new SemanticVersion(requiredVersion));
            }

            if (minimumVersion.Is()) {
                pkgs = pkgs.Where(each => each.Version >= new SemanticVersion(minimumVersion));
            }

            if (maximumVersion.Is()) {
                pkgs = pkgs.Where(each => each.Version <= new SemanticVersion(maximumVersion));
            }

            return pkgs;
        }

        internal IEnumerable<IPackage> FilterOnName(IEnumerable<IPackage> pkgs, string name) {
            return pkgs.Where(each => each.Id.IndexOf(name, StringComparison.OrdinalIgnoreCase) > -1);
        }

        internal IEnumerable<PackageReference> GetUninstalledPackageDependencies(PackageReference packageReference) {
            foreach (var depSet in packageReference.Package.DependencySets) {
                foreach (var dep in depSet.Dependencies) {
                    // get all the packages that match this dependency
                    var depRefs = dep.VersionSpec == null ? GetPackageById(dep.Id).ToArray() : GetPackageByIdAndVersionSpec(dep.Id, dep.VersionSpec).ToArray();

                    if (depRefs.Length == 0) {
                        Error("DependencyResolutionFailure", "Unable to resolve dependent package '{0} v{1}'", dep.Id, ((object)dep.VersionSpec ?? "").ToString());
                        throw new Exception("DependencyResolutionFailure");
                    }

                    if (depRefs.Any(each => IsPackageInstalled(each.Id, each.Version))) {
                        // we have a compatible version installed.
                        continue;
                    }
                   
                    yield return depRefs[0];

                    // it's not installed. return this as a needed package, but first, get it's dependencies.
                    foreach (var nestedDep in GetUninstalledPackageDependencies(depRefs[0])) {
                        yield return nestedDep;
                    }
                }
            }
        }

        internal IEnumerable<PackageReference> GetPackageByIdAndVersionSpec(string name, IVersionSpec versionSpec) {
            if (string.IsNullOrEmpty(name)) {
                return Enumerable.Empty<PackageReference>();
            }

            return Repositories.AsParallel().SelectMany(repository => {
                var pkgs = repository.FindPackages(name, versionSpec, AllowPrereleaseVersions, false);

                /*
                // necessary?
                pkgs = from p in pkgs where p.IsLatestVersion select p;
                */

                var pkgs2 = (IEnumerable<IPackage>)pkgs;

                return pkgs2.Select(pkg => new PackageReference {
                    Package = pkg,
                    Source = repository.Source,
                    FastPath = MakeFastPath(repository.Source, pkg.Id, pkg.Version.ToString())
                });
            }).OrderByDescending(each => each.Package.Version);
        }

        internal IEnumerable<PackageReference> GetPackageById(string name, string requiredVersion = null, string minimumVersion = null, string maximumVersion = null) {
            if (string.IsNullOrEmpty(name)) {
                return Enumerable.Empty<PackageReference>();
            }

            return Repositories.AsParallel().SelectMany(repository => {
                try {
                    var pkgs = repository.FindPackagesById(name);

                    if (!AllVersions && (string.IsNullOrEmpty(requiredVersion) && string.IsNullOrEmpty(minimumVersion) && string.IsNullOrEmpty(maximumVersion))) {
                        //  pkgs = from p in pkgs where p.IsLatestVersion select p;
                        // TODO: FIX THIS ASAP :::: temporary workaround:
                        // VERY SAD: this handles the version filter at the client instead of the server
                        // very slow! (need to work out how to jimmy a new query into  NuGet?)
                        pkgs = pkgs.GroupBy(p => p.Id).Select(set => set.MyMax(p => p.Version));
                    }

                    return FilterOnVersion(pkgs, requiredVersion, minimumVersion, maximumVersion)
                        .Select(pkg => new PackageReference {
                            Package = pkg,
                            Source = repository.Source,
                            FastPath = MakeFastPath(repository.Source, pkg.Id, pkg.Version.ToString())
                        });
                } catch (Exception e) {
                    e.Dump();
                    return Enumerable.Empty<PackageReference>();
                }
            });
        }

        internal PackageReference GetPackageByPath(string filePath) {
            if (PackageHelper.IsPackageFile(filePath)) {
                var package = new ZipPackage(filePath);

                return new PackageReference {
                    FastPath = MakeFastPath(filePath, package.Id, package.Version.ToString()),
                    Source = Path.GetDirectoryName(filePath),
                    Package = package,
                    IsPackageFile = true,
                };
            }
            return null;
        }

        internal PackageReference GetPackageByFastpath(string fastPath) {
            string source;
            string id;
            string version;
            if (TryParseFastPath(fastPath, out source, out id, out version)) {
                if (source.DirectoryHasDriveLetter() && FilesystemExtensions.FileExists(source)) {
                    return GetPackageByPath(source);
                }

                var repo = _packageRepositoryFactory.Value.CreateRepository(ResolveRepository(source));
                var pkg = repo.FindPackage(id, new SemanticVersion(version));
                if (pkg != null) {
                    return new PackageReference {
                        FastPath = fastPath,
                        Source = source,
                        Package = pkg,
                    };
                }
            }
            return null;
        }

        public IEnumerable<PackageReference> SearchForPackages(string name, string requiredVersion, string minimumVersion, string maximumVersion) {
            return Repositories.AsParallel().SelectMany(repository => {
                
                var packages = repository.GetPackages().Find(Hint.Is() ? Hint : name);

                // why does this method return less results? It looks the same to me!?
                // var packages = repository.Search(Hint.Is() ? Hint : name);

                IEnumerable<IPackage> pkgs = null;

                // query filtering:
                if (!AllVersions && (string.IsNullOrEmpty(requiredVersion) && string.IsNullOrEmpty(minimumVersion) && string.IsNullOrEmpty(maximumVersion))) {

                    //slow, client side way: pkgs = packages.ToEnumerable.GroupBy(p => p.Id).Select(set => set.MyMax(p => p.Version));
                    // new way: uses method in NuGet.exe in 2.8.1.1+
                    pkgs = packages.FindLatestVersion().ToEnumerable;
                } else {
                    // post-query filtering:
                    pkgs = packages.ToEnumerable;
                }

                // if they passed a name, restrict the search things that actually contain the name in the FullName.
                if (name.Is()) {
                    pkgs = FilterOnName(pkgs, name);
                }

                return FilterOnVersion(pkgs, requiredVersion, minimumVersion, maximumVersion)
                    .Select(pkg => new PackageReference {
                        Package = pkg,
                        Source = repository.Source,
                        FastPath = MakeFastPath(repository.Source, pkg.Id, pkg.Version.ToString())
                    });
            });
        }

        internal string GetNameForSource(string source) {
            var apr = AllPackageRepositories;

            try {
                if (FilesystemExtensions.FileExists(source)) {
                    return "Local File";
                }
            } catch {
            }

           return apr.Keys.FirstOrDefault(key => {
                var location = apr[key].Location;
                if (location.Equals(source, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }

                // make trailing slashes consistent
                if (source.TrimEnd('/').Equals(location.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }

                // and trailing backslashes
                if (source.TrimEnd('\\').Equals(location.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }

                return false;

            }) ?? source;
        }

        internal bool YieldPackages(IEnumerable<PackageReference> packageReferences) {
            var foundPackage = false;

            foreach (var pkg in packageReferences) {
                foundPackage = true;
                if (!YieldPackage(pkg.FastPath, pkg.Package.Id, pkg.Package.Version.ToString(), "semver", pkg.Package.Summary, GetNameForSource(pkg.Source))) {
                    break;
                }
            }
            return foundPackage;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public bool StartChocolateyProcessAsAdmin(string statements, string exeToRun, bool minimized, bool noSleep, int[] validExitCodes, string workingDirectory) {
            Verbose("StartChocolateyProcessAsAdmin", "Exe '{0}', Args '{1}'", exeToRun, statements);

            if (exeToRun.EqualsIgnoreCase("powershell")) {
                // run as a powershell script
                if (IsElevated()) {
                    Verbose("Already Elevated", "Running PowerShell script in process");
                    // in proc, we're already good.
                    var script = new ChocolateyScript(this);
                    script.InvokeChocolateyScript(statements);
                    return true;
                }

                Verbose("Not Elevated", "Running PowerShell script in new process");
                // otherwise setup a new proc
                exeToRun = PowerShellExe;
                statements = "import-Module {0}; invoke-chocolateyscript -Base64Script {1}".format(ChocolateyModuleFile, statements.ToBase64());
            }

            // just a straight exec from here.
            try {
                Verbose("Launching Process", "EXE :'{0}'", exeToRun);
                var process = AsyncProcess.Start(new ProcessStartInfo {
                    FileName = exeToRun,
                    Arguments = statements,
                    WorkingDirectory = workingDirectory,
                    WindowStyle = minimized ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal,
                    Verb = IsElevated() ? "" : "runas",
                });

                while (!process.WaitForExit(1)) {
                    if (IsCancelled()) {
                        process.Kill();
                        Verbose("Process Killed", "Host requested cancellation");
                        throw new Exception("Killed Process {0}".format(exeToRun));
                    }
                }
                if (validExitCodes.Contains(process.ExitCode)) {
                    Verbose("Process Exited Successfully.", "{0}", exeToRun);
                    return true;
                }
                Error("Process Failed", "{0}", exeToRun);
                throw new Exception("Process Exited with non-successful exit code {0} : {1} ".format( exeToRun, process.ExitCode));
            } catch (Exception e) {
                e.Dump();

                Error("Process Execution Failed", "'{0}' -- {1}", exeToRun, e.Message);
                throw e;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public string GetChocolateyBinRoot() {
            var storeValue = false;

            var cbr = Environment.GetEnvironmentVariable("ChocolateyBinRoot");
            if (string.IsNullOrEmpty(cbr)) {
                cbr = Environment.GetEnvironmentVariable("chocolatey_bin_root");
                if (string.IsNullOrEmpty(cbr)) {
                    // nothing at all? use the default
                    cbr = Path.Combine(SystemDrive+ "\\", "tools");
                    storeValue = true;
                }
            }

            // fix it if it's not rooted
            if (!cbr.DirectoryHasDriveLetter()) {
                cbr = Path.Combine(SystemDrive + "\\", cbr);
            }

            if (storeValue) {
                Environment.SetEnvironmentVariable("ChocolateyBinRoot", cbr, EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("ChocolateyBinRoot", cbr, EnvironmentVariableTarget.Process);
            }
            return cbr;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public bool GetChocolateyWebFile(string packageName, string fileFullPath, string url, string url64bit) {
            if (!string.IsNullOrEmpty(url64bit) && Environment.Is64BitOperatingSystem && !ForceX86) {
                url = url64bit;
            }

            Verbose("GetChocolateyWebFile", "{0} => {1}", packageName, url);

            var file = DownloadFile(url, fileFullPath);
            if (string.IsNullOrEmpty(file)) {
                throw new Exception("Failed to download file {0}".format(url));
            }

            return true;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public bool InstallChocolateyInstallPackage(string packageName, string fileType, string silentArgs, string file, int[] validExitCodes, string workingDirectory) {
            Verbose("InstallChocolateyInstallPackage", "{0}", packageName);

            switch (fileType.ToLowerInvariant()) {
                case "msi":
                    return StartChocolateyProcessAsAdmin("/i {0} {1}".format(file, silentArgs), "msiexec.exe", true, true, validExitCodes, workingDirectory);

                case "exe":
                    return StartChocolateyProcessAsAdmin("{0}".format(silentArgs), file, true, true, validExitCodes, workingDirectory);

                case "msu":
                    return StartChocolateyProcessAsAdmin("{0} {1}".format(file, silentArgs), "wusa.exe", true, true, validExitCodes, workingDirectory);
            }
            return false;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public bool InstallChocolateyPackage(string packageName, string fileType, string silentArgs, string url, string url64bit, int[] validExitCodes, string workingDirectory) {
            try {
                var tempFolder = FilesystemExtensions.TempPath;
                var chocTempDir = Path.Combine(tempFolder, "chocolatey");
                var pkgTempDir = Path.Combine(chocTempDir, packageName);
                Delete(pkgTempDir);
                CreateFolder(pkgTempDir);

                var file = Path.Combine(pkgTempDir, "{0}install.{1}".format(packageName, fileType));
                if (GetChocolateyWebFile(packageName, file, url, url64bit)) {
                    if (InstallChocolateyInstallPackage(packageName, fileType, silentArgs, file, validExitCodes, workingDirectory)) {
                        Verbose("Package Successfully Installed", packageName);
                        return true;
                    }
                    throw new Exception("Failed Install.");
                }
                throw new Exception("Failed Donwload.");
            } catch (Exception e) {
                e.Dump();
                Error("PackageInstallation Failed", packageName);
                throw new Exception("Failed Installation");
            }
        }

       

       

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public Snapshot SnapshotFolder(string locationToMonitor) {
            return new Snapshot(this, locationToMonitor);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public bool InstallChocolateyPath(string pathToInstall, string context) {
            if (context.EqualsIgnoreCase("machine")) {
                if (IsElevated()) {
                    EnvironmentUtility.SystemPath = EnvironmentUtility.SystemPath.Append(pathToInstall).RemoveMissingFolders();
                    EnvironmentUtility.Path = EnvironmentUtility.Path.Append(pathToInstall).RemoveMissingFolders();
                    return true;
                }
                Error("Elevation Required", "May not modify system path without elevation");
                return false;
            }
            EnvironmentUtility.UserPath = EnvironmentUtility.UserPath.Append(pathToInstall).RemoveMissingFolders();
            EnvironmentUtility.Path = EnvironmentUtility.Path.Append(pathToInstall).RemoveMissingFolders();
            return true;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public void UpdateSessionEnvironment() {
            Verbose("Reloading Environment", "");
            EnvironmentUtility.Rehash();
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public bool GetFtpFile(string url, string fileName, string username, string password, bool quiet) {
            /*# Create a FTPWebRequest object to handle the connection to the ftp server
                $ftprequest = [System.Net.FtpWebRequest]::create($url)

                # set the request's network credentials for an authenticated connection
                $ftprequest.Credentials =
                    New-Object System.Net.NetworkCredential($username,$password)

                $ftprequest.Method = [System.Net.WebRequestMethods+Ftp]::DownloadFile
                $ftprequest.UseBinary = $true
                $ftprequest.KeepAlive = $false

                # send the ftp request to the server
                $ftpresponse = $ftprequest.GetResponse()
                [int]$goal = $ftpresponse.ContentLength
	
                # get a download stream from the server response
                $reader = $ftpresponse.GetResponseStream()

                # create the target file on the local system and the download buffer
                $writer = New-Object IO.FileStream ($fileName,[IO.FileMode]::Create)
                [byte[]]$buffer = New-Object byte[] 1024
                [int]$total = [int]$count = 0

                # loop through the download stream and send the data to the target file
                do{
                    $count = $reader.Read($buffer, 0, $buffer.Length);
                    $writer.Write($buffer, 0, $count);
                    if(!$quiet) {
                        $total += $count
                        if($goal -gt 0) {
                            # Write-Progress "Downloading $url to $fileName" "Saving $total of $goal" -id 0 -percentComplete (($total/$goal)*100) 
                            $state.Progress( 2,  "Downloading $url to $fileName", (($total*100)/$goal), "Saving $total of $goal");
                        } else {
                            # Write-Progress "Downloading $url to $fileName" "Saving $total bytes..." -id 0 -Completed
                            $state.Progress( 2, "Downloading $url to $fileName", (($total*100)/$goal), "Saving $total bytes");
                        }
                        if ($total -eq $goal) {
                            # Write-Progress "Completed download of $url." "Completed a total of $total bytes of $fileName" -id 0 -Completed 
                            $state.ProgressComplete( 2, "Completed download of $url.", "Completed a total of $total bytes of $fileName" );
                        }
                    }
                } while ($count -ne 0)
	
                $writer.Flush()
                $writer.close()*/
            return false;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public bool GetWebFile(string url, string fileName, string userAgent, bool passthru, bool quiet) {
            /*
             Write-Debug "[LEGACY] Running 'Get-WebFile' for $fileName with url:`'$url`', userAgent: `'$userAgent`' ";
              #if ($url -eq '' return)
              $req = [System.Net.HttpWebRequest]::Create($url);
              #to check if a proxy is required
              $webclient = new-object System.Net.WebClient
              if (!$webclient.Proxy.IsBypassed($url))
              {
                $creds = [net.CredentialCache]::DefaultCredentials
                if ($creds -eq $null) {
                  Write-Debug "Default credentials were null. Attempting backup method"
                  $cred = get-credential
                  $creds = $cred.GetNetworkCredential();
                }
                $proxyaddress = $webclient.Proxy.GetProxy($url).Authority
                Write-host "Using this proxyserver: $proxyaddress"
                $proxy = New-Object System.Net.WebProxy($proxyaddress)
                $proxy.credentials = $creds
                $req.proxy = $proxy
              }
 
              #http://stackoverflow.com/questions/518181/too-many-automatic-redirections-were-attempted-error-message-when-using-a-httpw
              $req.CookieContainer = New-Object System.Net.CookieContainer
              if ($userAgent -ne $null) {
                Write-Debug "Setting the UserAgent to `'$userAgent`'"
                $req.UserAgent = $userAgent
              }
              $res = $req.GetResponse();

              if($fileName -and !(Split-Path $fileName)) {
                $fileName = Join-Path (Get-Location -PSProvider "FileSystem") $fileName
              }
              elseif((!$Passthru -and ($fileName -eq $null)) -or (($fileName -ne $null) -and (Test-Path -PathType "Container" $fileName)))
              {
                [string]$fileName = ([regex]'(?i)filename=(.*)$').Match( $res.Headers["Content-Disposition"] ).Groups[1].Value
                $fileName = $fileName.trim("\/""'")
                if(!$fileName) {
                   $fileName = $res.ResponseUri.Segments[-1]
                   $fileName = $fileName.trim("\/")
                   if(!$fileName) {
                      $fileName = Read-Host "Please provide a file name"
                   }
                   $fileName = $fileName.trim("\/")
                   if(!([IO.FileInfo]$fileName).Extension) {
                      $fileName = $fileName + "." + $res.ContentType.Split(";")[0].Split("/")[1]
                   }
                }
                $fileName = Join-Path (Get-Location -PSProvider "FileSystem") $fileName
              }
              if($Passthru) {
                $encoding = [System.Text.Encoding]::GetEncoding( $res.CharacterSet )
                [string]$output = ""
              }

              if($res.StatusCode -eq 200) {
                [long]$goal = $res.ContentLength
                $reader = $res.GetResponseStream()
                if($fileName) {
                   $writer = new-object System.IO.FileStream $fileName, "Create"
                }
                [byte[]]$buffer = new-object byte[] 1048576
                [long]$total = [long]$count = [long]$iterLoop =0
                do
                {
                   $count = $reader.Read($buffer, 0, $buffer.Length);
                   if($fileName) {
                      $writer.Write($buffer, 0, $count);
                   }
                   if($Passthru){
                      $output += $encoding.GetString($buffer,0,$count)
                   } elseif(!$quiet) {
                      $total += $count
                      if($goal -gt 0 -and ++$iterLoop%10 -eq 0) {
                         # Write-Progress "Downloading $url to $fileName" "Saving $total of $goal" -id 0 -percentComplete (($total/$goal)*100) 
                         $state.Progress( 2 , "Downloading $url to $fileName", (($total*100)/$goal), "Saving $total of $goal" )
                      }
                      if ($total -eq $goal) {
                        # Write-Progress "Completed download of $url." "Completed a total of $total bytes of $fileName" -id 0 -Completed 
                        $state.ProgressComplete( 2, "Completed download of $url.", "Completed a total of $total bytes of $fileName" )
                      }
                   }
                } while ($count -gt 0)
   
                $reader.Close()
                if($fileName) {
                   $writer.Flush()
                   $writer.Close()
                }
                if($Passthru){
                   $output
                }
              }
              $res.Close();
             */
            return false;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public bool InstallChocolateyPowershellCommand(string packageName, string psFileFullPath, string url, string url64bit, string workingDirectory) {
            if (GetChocolateyWebFile(packageName, psFileFullPath, url, url64bit)) {
                if (FilesystemExtensions.FileExists(psFileFullPath)) {
                    GeneratePS1ScriptBin(psFileFullPath);
                    return true;
                }
            } 

            Error("Unable to download script", url);
            return false;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public bool InstallChocolateyVsixPackage(string packageName, string vsixUrl, string vsVersion) {
            Verbose("InstallChocolateyVsixPackage", packageName);
            var vs = Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Microsoft\VisualStudio");
            var versions = vs.GetSubKeyNames().Select(each => {
                float f;
                if (!float.TryParse(each, out f)) {
                    return null;
                }
                if (f < 10.0) {
                    return null;
                }
                
                var vsv = vs.OpenSubKey(each);
                if (vsv.GetValueNames().Contains("InstallDir", StringComparer.OrdinalIgnoreCase)) {
                    return new {
                        Version = f,
                        InstallDir = vsv.GetValue("InstallDir").ToString()
                    };
                }
                return null;
            }).WhereNotNull().OrderByDescending( each => each.Version);

            var reqVsVersion = versions.FirstOrDefault();

            if (!string.IsNullOrEmpty(vsVersion)) {
                float f;
                if (!float.TryParse(vsVersion, out f)) {
                    throw new Exception("Unable to parse version number");
                }

                reqVsVersion = versions.FirstOrDefault(each => each.Version == f);
            }

            if (reqVsVersion == null) {
                throw new Exception("Required Visual Studio Version is not installed");
            }

            var vsixInstller = Path.Combine(reqVsVersion.InstallDir, "VsixInstaller.exe");
            if (!FilesystemExtensions.FileExists(vsixInstller)) {
                throw new Exception("Can't find Visual Studio VSixInstaller.exe {0}".format(vsixInstller));
            }
            var file = DownloadFile(vsixUrl, Path.Combine(FilesystemExtensions.TempPath, packageName.MakeSafeFileName()));
            if (string.IsNullOrEmpty(file) || !FilesystemExtensions.FileExists(file)) {
                throw new Exception("Unable to download file {0}".format(vsixUrl));
            }
            var process = AsyncProcess.Start(new ProcessStartInfo {
                FileName = vsixInstller,
                Arguments = @"/q ""{0}""".format( file),
            });
            process.WaitForExit();
            if (process.ExitCode > 0 && process.ExitCode != 1001) {
                Error("VsixInstall Failure", file);
                throw new Exception("Install failure");
            }
            return false;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public bool InstallChocolateyExplorerMenuItem(string menuKey, string menuLabel, string command, string type) {
            Verbose("InstallChocolateyExplorerMenuItem", "{0}/{1}/{2}/{3}",menuKey,  menuLabel,  command,  type);

            var key = type == "file" ? "*" : (type == "directory" ? "directory" : null);
            if (key == null) {
                return false;
            }
            if (!IsElevated()) {
                return StartChocolateyProcessAsAdmin("Install-ChocolateyExplorerMenuItem '{0}' '{1}' '{2}' '{3}'".format(menuKey, menuLabel, command, type), "powershell", false, false, new[] {
                    0
                }, Environment.CurrentDirectory);
            }

            var k = Registry.ClassesRoot.CreateSubKey(@"{0}\shell\{1}".format(key, menuKey));
            k.SetValue(null, menuLabel);
            var c = k.CreateSubKey("command");
            c.SetValue(null, @"{0} ""%1""");
            return true;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public bool UninstallChocolateyPackage(string packageName, string fileType, string silentArgs, string file, int[] validExitCodes, string workingDirectory) {
            Verbose("UninstallChocolateyPackage", packageName);

            switch (fileType.ToLowerInvariant()) {
                case "msi":
                    return StartChocolateyProcessAsAdmin("/x {0} {1}".format(file, silentArgs), "msiexec.exe", true, true, validExitCodes, workingDirectory);

                case "exe":
                    return StartChocolateyProcessAsAdmin("{0}".format(silentArgs), file, true, true, validExitCodes, workingDirectory);

                default :
                    Error("Unsupported Uninstall Type", fileType);
                    break;
            }
            return false;
        }


        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public string GetChocolateyUnzip(string fileFullPath, string destination, string specificFolder, string packageName) {
            Verbose("GetChocolateyUnzip", fileFullPath);
            try {
                var zipfileFullPath = fileFullPath;

                if (!string.IsNullOrEmpty(specificFolder)) {
                    fileFullPath = Path.Combine(fileFullPath, specificFolder);
                }

                if (!string.IsNullOrEmpty(packageName)) {
                    var packageLibPath = Environment.GetEnvironmentVariable("ChocolateyPackageFolder");
                    CreateFolder(packageLibPath);
                    var zipFileName = Path.GetFileName(zipfileFullPath);
                    var zipExtractLogFullPath = Path.Combine(packageLibPath, "{0}.txt".format(zipFileName));
                    var snapshot = new Snapshot(this, destination);
                    foreach (var f in UnzipFileIncremental(fileFullPath, destination)) {
                        // Verbose("Unzipped file", f);
                    }
                    snapshot.WriteFileDiffLog(zipExtractLogFullPath);
                }
                else {
                    foreach (var f in UnzipFileIncremental(fileFullPath, destination)) {
                        // Verbose("Unzipped file", f);
                    }
                }
                return destination;
            }
            catch (Exception e) {
                e.Dump();
                Error("PackageInstallation Failed", packageName);
                throw new Exception("Failed Installation");
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public bool InstallChocolateyZipPackage(string packageName, string url, string unzipLocation, string url64bit, string specificFolder, string workingDirectory) {
            Verbose("InstallChocolateyZipPackage", packageName);
            try {
                var tempFolder = FilesystemExtensions.TempPath;
                var chocTempDir = Path.Combine(tempFolder, "chocolatey");
                var pkgTempDir = Path.Combine(chocTempDir, packageName);
                Delete(pkgTempDir);
                CreateFolder(pkgTempDir);

                var file = Path.Combine(pkgTempDir, "{0}install.{1}".format(packageName, "zip"));
                if (GetChocolateyWebFile(packageName, file, url, url64bit)) {
                    if (GetChocolateyUnzip(file, unzipLocation, specificFolder, packageName).Is()) {
                        Verbose("Package Successfully Installed", packageName);
                        return true;
                    }
                    throw new Exception("Failed Install.");
                }
                throw new Exception("Failed Download.");
            }
            catch (Exception e) {
                e.Dump();
                Error("PackageInstallation Failed", packageName);
                throw new Exception("Failed Installation");
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public bool UnInstallChocolateyZipPackage(string packageName, string zipFileName) {
            Verbose("UnInstallChocolateyZipPackage", "Package '{0}', ZipFile '{1}'", packageName, zipFileName);
            try {
                var packageLibPath = Environment.GetEnvironmentVariable("ChocolateyPackageFolder");
                var zipContentFile = Path.Combine(packageLibPath, "{0}.txt".format(Path.GetFileName(zipFileName)));
                if (FilesystemExtensions.FileExists(zipContentFile)) {
                    foreach (var file in File.ReadAllLines(zipContentFile).Where( each => !string.IsNullOrEmpty(each) && FilesystemExtensions.FileExists(each ))) {
                        file.TryHardToDelete();
                    }
                }
            } catch (Exception e) {
                e.Dump();
                Error("uninstall failure", packageName);
            }
            return true;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public bool InstallChocolateyFileAssociation(string extension, string executable) {
            Verbose("InstallChocolateyFileAssociation", "{0} with executable {1}", extension, executable);
            if (string.IsNullOrEmpty(executable)) {
                throw new ArgumentNullException("executable");
            }

            if (string.IsNullOrEmpty(extension)) {
                throw new ArgumentNullException("extension");
            }
            executable = Path.GetFullPath(executable);
            if (!FilesystemExtensions.FileExists(executable)) {
                throw new FileNotFoundException("Executable not found",executable);
            }

            extension = "." + extension.Trim().Trim('.');
            var fileType = Path.GetFileName(executable).Replace(' ', '_');

            return StartChocolateyProcessAsAdmin(@"/c assoc {0}={1} & ftype {1}={2} ""%1"" %*".format(extension, fileType,executable ), "cmd.exe", false, false, new [] {0}, Environment.CurrentDirectory);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        public bool InstallChocolateyPinnedTaskBarItem(string targetFilePath) {
            Verbose("InstallChocolateyPinnedTaskBarItem", targetFilePath);
            if (string.IsNullOrEmpty(targetFilePath)) {
                Error("Failed InstallChocolateyPinnedTaskBarItem", "Empty path");
                throw new Exception("Failed.");
            }

            ShellApplication.Pin(Path.GetFullPath(targetFilePath));
            return true;
        }
    }
}