using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrailsStudio.MAUI.Evergine;

using TrailsStudio.Services;

namespace TrailsStudio.MAUI.ViewModels
{
    internal class StudioViewModel(EvergineView evergineView) : IQueryAttributable
    {
        public int rollInHeight;
        public int rollInAngle;

        private EvergineView evergineView = evergineView;
        private ControllerService controllerService = evergineView.Application.Container.Resolve<ControllerService>();
        

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            rollInHeight = (int)query["rollInHeight"];
            rollInAngle = (int)query["rollInAngle"];
            controllerService.RegisterRollInParams(rollInHeight, rollInAngle);
        }
    }
}
