using CliWrap;
using ExtravaWallSetup.Commands.Framework;

namespace ExtravaWallSetup.Commands
{
    public class PackageCommands : CommandBase<PackageCommands>
    {
        //dpkg-query --no-pager --showformat='${Package}\t${Version}\n' -W PACKAGE
        const string COMMAND_APTGET = "apt";
        const string COMMAND_DPKG_QUERY = "dpkg-query";
        const string DPKG_ARG_NO_PAGER = "--no-pager";
        const string DPKG_ARG_SHOW_FORMAT = "--showformat='${Package}\t${Version}\n'";
        const string DPKG_ARG_SHOW_PACKAGE = "-W";  // Add package name as a following argument
        const string ARG_INSTALL = "install";
        const string ARG_LIST = "list";
        const string ARG_VERY_QUIET = "-qq";
        public async Task<(bool success, string result)> ListPackages(string package)
        {
            var args = new List<string> { DPKG_ARG_NO_PAGER, DPKG_ARG_SHOW_FORMAT, DPKG_ARG_SHOW_PACKAGE };
            if (!string.IsNullOrWhiteSpace(package)) {
                args.Add(package);
            }

            return await Instance.RunAsync(Cli.Wrap(COMMAND_DPKG_QUERY).WithArguments(args));
        }
    }
}
