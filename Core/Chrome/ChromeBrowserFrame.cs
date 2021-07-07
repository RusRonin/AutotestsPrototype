﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Chrome
{
    public class ChromeBrowserFrame
    {
        public string Description { get; set; }
        public string DevtoolsFrontendUrl { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }
        public string WebSocketDebuggerUrl { get; set; }
    }
}
