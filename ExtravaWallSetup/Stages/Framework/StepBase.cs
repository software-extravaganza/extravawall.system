﻿using ExtravaWallSetup.GUI.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtravaWallSetup.Stages.Framework {
    public abstract class StepBase : IStep {
        protected InstallManager Install { get; private set; }
        private TaskCompletionSource _taskCompletionSource = new TaskCompletionSource();
        protected VirtualConsoleManager Console => Install.Console!;
        public abstract string Name { get; }
        public abstract StageType Stage { get; }
        public abstract short StepOrder { get; }

        public virtual bool AutoComplete { get; }
        public bool IsComplete { get; private set; }
        public bool IsExecuting { get; private set; }

        public Task StepTask => _taskCompletionSource.Task;
        public async Task BeginExecute() {
            try {
                IsExecuting = true;
                Install.AddOrUpdateSystemInfo("Install Stage", Name);
                await Execute();
            } catch {
                //todo: handle exception
            } finally {
                if (AutoComplete) {
                    NotifyThatStepIsComplete();
                }
            }
        }

        public async Task BeginComplete() {
            try {
                if (!IsExecuting || IsComplete) {
                    return;
                }

                await Complete();
                IsExecuting = false;
                IsComplete = true;
            } catch {
                //todo: handle exception
            }

        }
        protected abstract Task Execute();

        protected void NotifyThatStepIsComplete() {
            _taskCompletionSource.SetResult();
        }
        protected virtual async Task Complete() {
            await Task.CompletedTask;
        }

        protected StepBase(InstallManager installManager) {
            Install = installManager;
        }

        public virtual void Initialize() { }
    }
}
