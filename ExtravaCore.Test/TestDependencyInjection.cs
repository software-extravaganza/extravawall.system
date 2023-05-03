using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SolidToken.SpecFlow.DependencyInjection;

namespace ExtravaCore.Test {
    public class TestDependencyInjection {
        [ScenarioDependencies]
        public static IServiceCollection CreateServices() {
            var services = new ServiceCollection();
            services.AddScoped<IElevator, Elevator>();
            services.AddScoped<IProcessManager, ProcessManager>();
            return services;
        }
    }
}