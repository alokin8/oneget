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
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;

    public class Snapshot {
        private Dictionary<string, FileInfo> _files;
        private ChocolateyState _state;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Still in development!")]
        internal Snapshot(ChocolateyState state, string folder) {
            _state = state;
            Folder = folder;
            _state.Verbose("Taking Snapshot", folder);
            _state.CreateFolder(folder);
            _files = Directory.EnumerateFiles(Folder, "*", SearchOption.AllDirectories).ToDictionary(each => each, each => new FileInfo(each), StringComparer.OrdinalIgnoreCase);
        }

        public string Folder {get; internal set;}

        public void WriteFileDiffLog(string logPath) {
            _state.Verbose("Diffing Snapshot", Folder);
            var now = Directory.EnumerateFiles(Folder, "*", SearchOption.AllDirectories).ToDictionary(each => each, each => new FileInfo(each), StringComparer.OrdinalIgnoreCase);

            // modified
            var modified = now.Keys.Where(each => _files.ContainsKey(each) && (_files[each].Length != now[each].Length || _files[each].LastWriteTime != now[each].LastWriteTimeUtc));

            //added
            var added = now.Keys.Where(each => !_files.ContainsKey(each));

            //deleted
            var deleted = _files.Keys.Where(each => !now.ContainsKey(each));

            File.WriteAllLines(logPath, modified.Concat(added).Concat(deleted));
        }
    }
}