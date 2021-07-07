using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class TestingResult
    {
        public string Info => "HTML and screens saved in file system and ready to be processed";
        public List<string> Links { get; set; } = new List<string>();
        public List<string> Logs { get; set; } = new List<string>();
    }
}
