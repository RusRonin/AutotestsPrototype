﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Awaiters
{
    public interface IPageLoadedAwaitingTester
    {
        public void NotifyReady();
    }
}