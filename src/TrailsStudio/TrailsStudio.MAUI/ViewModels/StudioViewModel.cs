using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrailsStudio.MAUI.Evergine;

namespace TrailsStudio.MAUI.ViewModels
{
    internal class StudioViewModel(EvergineView evergineView) : IQueryAttributable
    {
        public int rollInHeight;
        public int rollInAngle;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            rollInHeight = (int)query["rollInHeight"];
            rollInAngle = (int)query["rollInAngle"];
        }
        private EvergineView evergineView = evergineView;
    }
}
