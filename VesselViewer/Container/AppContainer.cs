using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.Windsor;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;

namespace VesselViewer.Container
{
    public class AppContainer
    {
        public WindsorContainer Container { get; }

        public AppContainer()
        {
            Container = new WindsorContainer();
            Container.Register(
                Classes.FromThisAssembly().InNamespace("VesselViewer.Services").WithServiceAllInterfaces(),
                Classes.FromThisAssembly().InNamespace("VesselViewer.UI").LifestyleSingleton()
            );
        }
    }
}
