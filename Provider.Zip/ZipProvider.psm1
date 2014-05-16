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

function Get-ProviderName {
	"Zip"
}

function Install-Package { 

}

function Uninstall-Package { 

}

function Init-Provider {
param( $getConfigStrings )
	# holey-moley ... this works!
	 [System.Console]::WriteLine("In Init!")
	 $getConfigStrings.Invoke("Providers/Module") | foreach-Object{  [System.Console]::WriteLine("Item $_" ) }
}

function Find-Package {
	param( $Name, $RequiredVersion, $MinimumVersion, $MaximumVersion, $Callback )
	 # [System.Console]::WriteLine("In script FP!")
	 # we should wrap these somehow.
	 # $Callback.Invoke("YieldPackage" , @("packageName", "1.0") )
	 
	#if( -not $YieldPackage.Invoke("packagename", "1.0") ) {
	#	return false;
	#}

	#if( -not $YieldPackage.Invoke("otherpackagename", "2.0") ) {
		#return false;
	#}

	return true;
}
