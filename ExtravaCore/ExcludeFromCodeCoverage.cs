using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExtravaCore {
    [AttributeUsage(AttributeTargets.All)]
    public class ExcludeFromCodeCoverage : Attribute { }
}