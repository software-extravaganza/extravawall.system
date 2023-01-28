using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtravaWallSetup {
    public class SystemInfoModel {
        public string PrettyName { get; set; }
        public string Name { get; set; }
        public string VersionId { get; set; }
        public string VersonCodeName { get; set; }
        public string ID { get; set; }
        public Uri HomeUrl { get; set; }
        public Uri SupportUrl { get; set; }
        public Uri BugReportUrl { get; set; }
    }
}
