using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExtravaCore.Commands;
using ExtravaCore.Commands.Framework;
using Semver;

namespace ExtravaCore;

public interface IOperatingSystem {
    string Name { get; }
    SemVersion Version { get; }
    Func<ICommandDriver> CommandDriverFactory { get; }
}
