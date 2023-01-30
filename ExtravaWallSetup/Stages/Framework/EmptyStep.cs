using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtravaWallSetup.Stages.Framework {
    public class EmptyStep : StepBase {
        public override string Name => "";

        public override StageType Stage => StageType.None;

        public override short StepOrder => 0;

        protected override async Task Execute() {
            await Task.CompletedTask;
        }
    }
}
