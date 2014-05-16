
###
# ==++==
#
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#
###
@{
    GUID = "77C5605C-59C4-45A5-9926-EE4E9CBD79A1"
    Author = "Microsoft Corporation"
    CompanyName = "Microsoft Corporation"
    Copyright = "(C) Microsoft Corporation. All rights reserved."
    HelpInfoUri = "http://unknown"
    ModuleVersion = "1.0.0.0"
    PowerShellVersion = "2.0"
    ClrVersion = "4.0"
    RootModule = "Microsoft.OneGet.Plugin.Chocolatey.dll"
    # NestedModules = @('')
    # TypesToProcess = ""
    # FormatsToProcess = ""
    CmdletsToExport = @(
        'Write-ChocolateyPackage',
        'Publish-ChocolateyPackage'
		'Invoke-ChocolateyScript'
	)
}
