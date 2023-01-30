using ExtravaWallSetup.GUI;
using ExtravaWallSetup.Stages.Framework;
using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace ExtravaWallSetup.Stages
{
    public class EndStage : StepBase
    {
        private ExitView _view;

        public override string Name => "End";

        public override StageType Stage => StageType.End;

        public override short StepOrder => 0;
        public string ExitError { get; set; }

        protected override async Task Execute()
        {
            var isSuccess = string.IsNullOrWhiteSpace(ExitError);
            var title = isSuccess ? (ustring)"Install Successfull" : "Install failed";  //💪 😖
            var content = isSuccess ? (ustring)"Go play in traffic (network traffic)" : $"{ExitError}"; //🙃 😩
            //var msg = isSuccess ? MessageBox.Query(title, content) : MessageBox.ErrorQuery(title, content);
            _view = new ExitView(isSuccess, title, content);
            Console.Add(_view);
            await Task.CompletedTask;
        }
    }
}
