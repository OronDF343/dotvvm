﻿using DotVVM.Framework.Runtime.Compilation.ResolvedControlTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotVVM.Framework.Styles
{
    public interface IStyleApplicator
    {
        void ApplyStyle(ResolvedControl control);
    }
}
