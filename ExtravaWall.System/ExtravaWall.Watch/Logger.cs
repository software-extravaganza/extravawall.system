using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExtravaWall.Watch {
    public class Logger {
        private bool _enabled = true;
        public Logger(bool enabled) {
            _enabled = enabled;
        }
        public void Log(string message) {
            if (!_enabled) {
                return;
            }

            Console.WriteLine(message);
        }
    }
}