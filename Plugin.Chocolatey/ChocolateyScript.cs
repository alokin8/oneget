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
    using PowerShell.Core;

    public class ChocolateyScript {
        private readonly ChocolateyState _state;

        internal ChocolateyScript(ChocolateyState state) {
            _state = state;
        }

        internal void InvokeChocolateyScript(string command, params string[] arguments) {

            // hacks to fix broken packages (some of which don't even complain on original chocolatey)
            command = command.Replace(@"SetEnvironmentVariable('JAVA_HOME', C:\Program Files\Java\jre7,", @"SetEnvironmentVariable('JAVA_HOME', 'C:\Program Files\Java\jre7',");
            

            using (dynamic ps = new DynamicPowershell()) {
                // grant access to the current call state.
                ps["state"] = _state;

                // import our new helpers
                DynamicPowershellResult result = ps.ImportModule(Name: _state.HelperModulePath, PassThru: true);
                if (!result.Success) {
                    throw new Exception("Unable to load helper module for install script.");
                }

                result = ps.InvokeExpression(command);

                if (!result.Success) {
                    foreach (var i in result.Errors) {
                        _state.Error(i.CategoryInfo.Reason, i.Exception.Message, null);
                    }
                    throw new Exception("Failed executing chocolatey script.");
                }

                ps["state"] = null;
            }
        }
    }
}