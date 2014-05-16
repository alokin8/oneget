using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.OneGet.Plugin.Chocolatey.CmdLets {
    using System.Management.Automation;
    using Core.Extensions;
    using Microsoft.PowerShell.OneGet.Core;


    /// <summary>
    /// I'm not entirely convinced this is the best way to handle this, but 
    /// it'll have to do for now.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "ChocolateyScript")]
    public class InvokeChocolateyScript : AsyncCmdlet {

        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "UseUnencodedScript")]
        public string Script {get; set;}

        [Parameter(Mandatory = true, ParameterSetName = "UseBase64Script")]
        public string Base64Script { get; set; }
        

        [Parameter(ValueFromRemainingArguments = true)]
        public string[] Arguments;

        public override bool ProcessRecordAsync() {
            var scr = string.IsNullOrEmpty(Base64Script) ? Script : Base64Script.FromBase64();
            try {
                using (var state = new ChocolateyState(Invoke)) {
                    var script = new ChocolateyScript(state);
                    script.InvokeChocolateyScript(scr, Arguments);
                }
            } catch (Exception e) {
                e.Dump();
            }
            return true;
        }
    }

}
