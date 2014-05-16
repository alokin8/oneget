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

// ReSharper disable InconsistentNaming
#if NOT_STATIC_LINK
#else
namespace Microsoft.OneGet.Plugin.Chocolatey {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;

    internal class DynamicType {
        protected internal readonly dynamic actual;

        internal DynamicType(dynamic actualObject) {
            actual = actualObject;
        }

        public override string ToString() {
            return actual.ToString();
        }
    }

    
    
    internal class IPackage : DynamicType {
        
        internal IPackage(object actualObject) : base (actualObject) {
            
        }

        public string Id {
            get {
                return actual.Id;
            }
        }

        public SemanticVersion Version {
            get {
                return new SemanticVersion(actual.Version);
            }
        }

        public string GetFullName() {
            return actual.GetFullName();
        }

        public IEnumerable<PackageDependencySet> DependencySets {
            get {
                foreach (var each in actual.DependencySets) {
                    yield return new PackageDependencySet(each);
                }
            }
        }

        public bool IsLatestVersion {
            get {
                return actual.IsLatestVersion;
            }
        }

        public string Summary {
            get {
                return actual.Summary;
            }
        }
    }

    internal class PackageDependencySet : DynamicType {
        internal PackageDependencySet(object actualObject)
            : base(actualObject) {
        }

        internal IEnumerable<PackageDependency> Dependencies {
            get {
                foreach (var each in actual.Dependencies) {
                    yield return new PackageDependency(each);
                }
            }
        }
    }

    internal class IVersionSpec : DynamicType {
        internal IVersionSpec(object actualObject)
            : base(actualObject) {
        }

        public override string ToString() {
            if (actual == null) {
                return string.Empty;
            }
            return actual.ToString();
        }
    }

    internal class PackageDependency  : DynamicType {
        internal PackageDependency(object actualObject)
            : base(actualObject) {
        }

        public string Id {
            get {
                return actual.Id;
            }
        }

        public IVersionSpec VersionSpec {
            get {
                var vs = actual.VersionSpec;
                return vs == null ? null : new IVersionSpec(vs);
            }
        }
    }

    public static class PackageHelper {
        private static readonly Lazy<Type> NuGetType = new Lazy<Type>(() => NuGet.Assembly.GetType("NuGet.PackageHelper"));
        private static readonly dynamic t = new StaticMembersDynamicWrapper(NuGetType.Value);

        public static bool IsPackageFile(string pkgFile) {
            return t.IsPackageFile(pkgFile);
        }


        public static bool IsManifest(string path) {
            return t.IsManifest(path);
        }


        public static bool IsAssembly(string path) {
            return t.IsAssembly(path);
        }

        /*
        public static IPackage ResolvePackage(IPackageRepository repository, string packageId, SemanticVersion version) {

        }
         */
    }

    internal class ZipPackage : IPackage {
        internal static object NewZipPackage(string filename) {
            return NuGet.Assembly.CreateInstance("NuGet.ZipPackage", true, BindingFlags.Default, null, new object[] { filename }, null, new object[] { });
        }
        internal ZipPackage(string pkgFile)
            : base(NewZipPackage(pkgFile)) {
        }

    }

    internal class IQueryablePackages : DynamicType , IQueryable, IQueryable<IPackage> {
        internal IQueryablePackages(object actualObject)
            : base(actualObject) {
        }

        internal IEnumerable<IPackage> ToEnumerable {
            get {
                foreach (var each in (IEnumerable)actual) {
                    yield return new IPackage(each);
                }
            }
        }

        
        IEnumerator<IPackage> IEnumerable<IPackage>.GetEnumerator() {
            return actual.GetEnumerator();
        }

        public IEnumerator GetEnumerator() {
            return actual.GetEnumerator();
        }

        public Expression Expression {
            get {
                return actual.Expression;
            }
        }

        public Type ElementType {
            get {
                return actual.ElementType;
            }
        }

        public IQueryProvider Provider {
            get {
                return actual.Provider;
            }
        }
    }

    internal class IPackageRepository : DynamicType {
        internal IPackageRepository(object actualObject)
            : base(actualObject) {
        }

        public string Source {
            get {
                return actual.Source;
            }
        }

        public IEnumerable<IPackage> FindPackagesById(string name) {
            foreach( var each  in actual.FindPackagesById( name ) ) {
                yield return new IPackage(each);
            }
        }

        public IPackage FindPackage(string id, SemanticVersion semanticVersion) {
            return new IPackage(actual.FindPackage(id,semanticVersion.actual));
        }

        public IQueryablePackages  GetPackages() {
            return new IQueryablePackages(actual.GetPackages());
        }
    }


    internal static class PackageRepositoryExtensions {
        private static readonly Lazy<Type> NuGetType = new Lazy<Type>(() => NuGet.Assembly.GetType("NuGet.PackageRepositoryExtensions"));
        private static readonly dynamic t = new StaticMembersDynamicWrapper(NuGetType.Value);

        internal static IEnumerable<IPackage> FindPackages(this IPackageRepository repository,string name, IVersionSpec versionSpec, bool allowPrereleaseVersions, bool b) {
            foreach( var p in (t.FindPackages(repository.actual, name, versionSpec.actual, allowPrereleaseVersions, b))) {
                yield return new IPackage( p);
            }
        }

        internal static IQueryablePackages Search(this IPackageRepository repository, string searchTerm, IEnumerable<string> targetFrameworks, bool allowPrereleaseVersions) {
            return new IQueryablePackages(t.Search(repository.actual, searchTerm, targetFrameworks, allowPrereleaseVersions));
        }

        internal static IQueryablePackages Search(this IPackageRepository repository, string searchTerm, bool allowPrereleaseVersions) {
            return new IQueryablePackages(t.Search(repository.actual, searchTerm, Enumerable.Empty<string>(), allowPrereleaseVersions));
        }
        internal static IQueryablePackages Search(this IPackageRepository repository, string searchTerm) {
            return new IQueryablePackages(t.Search(repository.actual, searchTerm, Enumerable.Empty<string>(), false));
        }
    }


    public static class PackageExtensions {
        private static readonly Lazy<Type> NuGetType = new Lazy<Type>(() => NuGet.Assembly.GetType("NuGet.PackageExtensions"));
        private static readonly dynamic t = new StaticMembersDynamicWrapper(NuGetType.Value);

        internal static IQueryablePackages Find(this IQueryablePackages packages, string query) {
            return new IQueryablePackages(t.Find(packages.actual, query));
        }



        internal static IQueryablePackages Find(this IQueryable<IPackage> packages, string query) {
            return new IQueryablePackages(t.Find((packages as IQueryablePackages).actual, query));
        }

        internal static IQueryablePackages FindLatestVersion(this IQueryablePackages packages) {
            return new IQueryablePackages(t.FindLatestVersion(packages.actual));
        }
    }

    internal class SemanticVersion : DynamicType, IComparable, IComparable<SemanticVersion>, IEquatable<SemanticVersion> {
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != this.GetType()) {
                return false;
            }
            return Equals((SemanticVersion)obj);
        }

        public override int GetHashCode() {
            return actual.GetHashCode();
        }

        internal static object NewSemanticVersion(string version) {
            if (string.IsNullOrEmpty(version)) {
                version = "0.0";
            }
            // force version numbers to be at least n.n
            if (version.IndexOf('.') == -1) {
                version = version + ".0";
            }
            return NuGet.Assembly.CreateInstance("NuGet.SemanticVersion", true, BindingFlags.Default, null, new object[] { version }, null, new object[] { });
        }

        internal SemanticVersion(string version) : base(NewSemanticVersion(version)) {
        }

        internal SemanticVersion(object semanticVersion) : base(semanticVersion) {
            
        }

        public int CompareTo(SemanticVersion other) {
            return actual.CompareTo(other.actual);
        }

        public int CompareTo(object obj) {
            return actual.CompareTo(obj);
        }

        public static bool operator ==(SemanticVersion version1, SemanticVersion version2) {
            if (object.ReferenceEquals((object)version1, (object)null))
                return object.ReferenceEquals((object)version2, (object)null);
            else
                return version1.Equals(version2);
        }

        public static bool operator !=(SemanticVersion version1, SemanticVersion version2) {
            return !(version1 == version2);
        }

        public static bool operator <(SemanticVersion version1, SemanticVersion version2) {
            if (version1 == (SemanticVersion)null)
                throw new ArgumentNullException("version1");
            else
                return version1.CompareTo(version2) < 0;
        }

        public static bool operator <=(SemanticVersion version1, SemanticVersion version2) {
            if (!(version1 == version2))
                return version1 < version2;
            else
                return true;
        }

        public static bool operator >(SemanticVersion version1, SemanticVersion version2) {
            if (version1 == (SemanticVersion)null)
                throw new ArgumentNullException("version1");
            else
                return version2 < version1;
        }

        public static bool operator >=(SemanticVersion version1, SemanticVersion version2) {
            if (!(version1 == version2))
                return version1 > version2;
            else
                return true;
        }

        public bool Equals(SemanticVersion other) {
            if (actual == null && other == null) {
                return true;
            }
            if (actual == null) {
                return false;
            }

            if (other == null || other.actual == null) {
                return false;
            }
            return actual.Equals(other.actual);
        }
    }

    internal class PackageRepositoryFactory : DynamicType {
        private static object NewPackageRepositoryFactory() {
            return NuGet.Assembly.CreateInstance("NuGet.PackageRepositoryFactory", true, BindingFlags.Default, null, new object[] { }, null, new object[] { });
        }
         internal PackageRepositoryFactory() : base( NewPackageRepositoryFactory() ) {
        }
        internal IPackageRepository CreateRepository(string repository)  {
            return new IPackageRepository(actual.CreateRepository(repository));
        }
    }


    public class StaticMembersDynamicWrapper : DynamicObject {
        private Type _type;

        public StaticMembersDynamicWrapper(Type type) {
            _type = type;
        }

        // Handle static properties
        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            if (binder == null) {
                throw new ArgumentNullException("binder");
            }
            PropertyInfo prop = _type.GetProperty(binder.Name, BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public);
            if (prop == null) {
                result = null;
                return false;
            }

            result = prop.GetValue(null, null);
            return true;
        }

        // Handle static methods
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result) {
            IEnumerable<MethodInfo> methods = _type.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public).Where(each => each.Name == binder.Name && each.GetParameters().Count() == args.Length).ToArray();
            // MethodInfo method = _type.GetMethod(binder.Name, BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public);

            var method = methods.FirstOrDefault();

            if (args!= null && args.Length >0 &&  method.IsGenericMethod) {
                var t1 = args[0].GetType();
                    var ga = t1.GetGenericArguments();
                    method = method.MakeGenericMethod(new[] { ga.FirstOrDefault() });
            }
            
            if (method == null) {
                result = null;
                return false;
            }

            result = method.Invoke(null, args);
            return true;
        }
    }

    internal static class NuGet {
        private static Assembly _assembly;

        internal static Assembly Assembly {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Justification = "Gotta.")]
            get {
                return _assembly ?? (_assembly = Assembly.LoadFile(NuGetCorePath));
            }
        }

        public static string NuGetCorePath {get; set;}
    }
}
#endif