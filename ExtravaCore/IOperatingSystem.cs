using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExtravaCore.Commands;
using Semver;

namespace ExtravaCore;

public interface IOperatingSystem {
    string Name { get; }
    SemVersion Version { get; }
    ICommandDriver Commands { get; }
}
