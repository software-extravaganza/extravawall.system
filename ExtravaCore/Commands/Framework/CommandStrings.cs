namespace ExtravaCore.Commands.Framework;

public partial class CommandBase {
    protected class CommandStrings {

        // UNAME COMMAND
        public const string COMMAND_UNAME = "uname";
        public const string UNAME_ARG_NAME = "-n";
        public const string UNAME_ARG_OS = "-s";
        public const string UNAME_ARG_ARCHITECTURE = "-m";
        public const string UNAME_ARG_ALL = "-a";

        // APT COMMANDS
        public const string COMMAND_APTGET = "apt-get";
        public const string COMMAND_APTCACHE = "apt-cache";
        public const string COMMAND_DPKG_QUERY = "dpkg-query";
        public const string DPKG_ARG_NO_PAGER = "--no-pager";
        public const string DPKG_ARG_SHOW_FORMAT = "--showformat=${Package}\t${Version}";
        public const string DPKG_ARG_SHOW_PACKAGE = "-W";  // Add package name as a following argument
        public const string APTGET_ARG_INSTALL = "install";//
        public const string APTGET_ARG_ASSUME_YES = "--assume-yes";
        public const string APTGET_ARG_SHOW_PROGRESS = "--show-progress";
        public const string APTGET_ARG_LIST = "list";
        public const string APTGET_ARG_VERY_QUIET = "-qq";
        public const string APTCACHE_ARG_SEARCH = "search";

        // RPM COMMANDS
        public const string COMMAND_RPM = "rpm";
        public const string RPM_ARG_QUERY = "-q";
        public const string RPM_ARG_QUERY_FORMAT = "--queryformat";
        public const string RPM_ARG_QUERY_FORMAT_STRING = "${NAME}\t${VERSION}\n";
        public const string RPM_ARG_QUERY_ALL = "-a";
        public const string RPM_ARG_LIST = "-l";

    }
}
