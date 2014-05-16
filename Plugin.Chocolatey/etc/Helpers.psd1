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
    GUID = "6a7f457a-947f-4639-b3ea-8b40bd09632a"
    Author = "Microsoft Corporation"
    CompanyName = "Microsoft Corporation"
    Copyright = "(C) Microsoft Corporation. All rights reserved."
    HelpInfoUri = "http://unknown"
    ModuleVersion = "1.0.0.0"
    PowerShellVersion = "2.0"
    ClrVersion = "4.0"
    RootModule = "Helpers.psm1"
	# Requires access to the OneGet ProviderAPI
	# RequiredAssemblies = @('OneGet.API')

    # TypesToProcess = ""
    # FormatsToProcess = ""
    FunctionsToExport = @(
        'Generate-BinFile',
		'Get-BinRoot',
		'Get-ChocolateyUnzip',
		'Get-ChocolateyWebFile',
		'Get-EnvironmentVar',
		'Get-FtpFile',
		'Get-ProcessorBits',
		'Get-WebFile',
		'Install-ChocolateyDesktopLink',
		'Install-ChocolateyEnvironmentVariable',
		'Install-ChocolateyExplorerMenuItem',
		'Install-ChocolateyFileAssociation',
		'Install-ChocolateyInstallPackage',
		'Install-ChocolateyPackage',
		'Install-ChocolateyPath',
		'Install-ChocolateyPinnedTaskBarItem',
		'Install-ChocolateyPowershellCommand',
		'Install-ChocolateyVsixPackage',
		'Install-ChocolateyZipPackage',
		'Remove-BinFile',
		'Start-ChocolateyProcessAsAdmin',
		'Uninstall-ChocolateyPackage',
		'UnInstall-ChocolateyZipPackage',
		'Update-SessionEnvironment',
		'Write-ChocolateyFailure',
		'Write-ChocolateySuccess',
		'Write-Debug',
		'Write-Error',
		'Write-FileUpdateLog',
		'Write-Host',
		'Write-Verbose'
   )
}
