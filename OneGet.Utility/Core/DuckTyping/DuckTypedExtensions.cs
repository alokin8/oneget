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

namespace Microsoft.OneGet.Core.DuckTyping {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Extensions;

    public static class DuckTypedExtensions {
        private static readonly IDictionary<Tuple<Type, Type>, bool> _compatibilityMatrix = new Dictionary<Tuple<Type, Type>, bool>();

        internal static IEnumerable<FieldInfo> GetRequiredMembers(this Type duckType) {
            if (duckType != null && typeof (DuckTypedClass).IsAssignableFrom(duckType)) {
                return duckType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(each => each.GetCustomAttributes(typeof (DuckTypedClass.RequiredAttribute), true).Any());
            }
            return Enumerable.Empty<FieldInfo>();
        }

        internal static IEnumerable<FieldInfo> GetOptionalMembers(this Type duckType) {
            if (duckType != null && typeof (DuckTypedClass).IsAssignableFrom(duckType)) {
                return duckType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(each => each.GetCustomAttributes(typeof (DuckTypedClass.OptionalAttribute), true).Any());
            }
            return Enumerable.Empty<FieldInfo>();
        }

        internal static IEnumerable<MethodInfo> GetPublicMethods(this Type candidateType) {
            if (candidateType != null) {
                return candidateType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            }
            return Enumerable.Empty<MethodInfo>();
        }

        internal static IEnumerable<FieldInfo> GetPublicFields(this Type candidateType) {
            if (candidateType != null) {
                return candidateType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            }
            return Enumerable.Empty<FieldInfo>();
        }

        public static bool IsTypeCompatible(this Type duckType, Type candidateType) {
            lock (_compatibilityMatrix) {
                return _compatibilityMatrix.GetOrAdd(new Tuple<Type, Type>(duckType, candidateType), () => {
                    if (duckType != null && candidateType != null) {
                        var publicMethods = candidateType.GetPublicMethods().ToArray();

                        foreach (var member in duckType.GetRequiredMembers()) {
                            var expectedDelegateType = member.FieldType;

                            // check the 'type' of each member to see if the candidate type has a
                            // member with that same name.
                            // (the 'type' of the member will be a delegate)
                            if (! publicMethods.Any(each => each.Name.Equals(expectedDelegateType.Name, StringComparison.CurrentCultureIgnoreCase) && expectedDelegateType.IsDelegateAssignableFromMethod(each))) {
                                //Console.WriteLine( "Type '{0}' is not a type match for '{1}' because of missing member '{2}'",  candidateType.Name, duckType.Name, member );
#if DETAILED_DEBUG
                                Event<Verbose>.Raise("Not DUCKY", "Type '{0}' is not a type match for '{1}' because of missing member '{2}'", new object[] {candidateType.Name, duckType.Name, member});
#endif
                                return false;
                            }
                            // so far, so good...!
                        }
                        return true;
                    }
                    return false;
                });
            }
        }

        internal static bool IsObjectCompatible(this Type duckType, object candidateObject) {
            if (candidateObject != null) {
                var candidateType = candidateObject.GetType();

                if (duckType != null && candidateType != null) {
                    var publicMethods = candidateType.GetPublicMethods().ToArray();
                    var publicFields = candidateType.GetPublicFields().Where(each => each.FieldType.BaseType == typeof (MulticastDelegate)).ToArray();

                    foreach (var member in duckType.GetRequiredMembers()) {
                        var expectedDelegateType = member.FieldType;

                        // check the 'type' of each member to see if the candidate type has a
                        // member with that same name.
                        // (the 'type' of the member will be a delegate)
                        if (!publicMethods.Any(each => each.Name.Equals(expectedDelegateType.Name, StringComparison.CurrentCultureIgnoreCase) && expectedDelegateType.IsDelegateAssignableFromMethod(each))) {
                            // or if a compatible delegate is present.
                            if (!publicFields.Any(each => each.Name.Equals(expectedDelegateType.Name, StringComparison.CurrentCultureIgnoreCase) && expectedDelegateType.IsDelegateAssignableFromDelegate(each.FieldType))) {
                                //Console.WriteLine("Type '{0}' is not a type match for '{1}' because of missing member '{2}'", candidateType.Name, duckType.Name, member);
#if DETAILED_DEBUG
                                
                                Event<Verbose>.Raise("Not DUCKY", "Type '{0}' is not a type match for '{1}' because of missing member '{2}'", new object[] {candidateType.Name, duckType.Name, member});
#endif

                                return false;
                            }
                        }
                        // so far, so good...!
                    }
                    return true;
                }
                return false;
            }

            return false;
        }

        internal static bool IsSupported(this Delegate d) {
            if (d == null) {
                return false;
            }

            DuckTypedClass.InstanceSupportsMethod(d.Target, d.GetType().Name);
            return true;
        }
    }
}