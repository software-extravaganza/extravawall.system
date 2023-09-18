using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using ExtravaCore.Models;
using Semver;

namespace ExtravaCore
{
    public class PackageManager
    {
        public IDictionary<string, Package> ParseDependencyConfiguration(string dependencyConfig)
        {
            return Package.Parse(dependencyConfig, strict: false, requireSingleLine: false);
        }
    }
}
