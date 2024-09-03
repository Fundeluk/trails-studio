using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrailsStudio.MAUI.Evergine;

namespace TrailsStudio.MAUI.ViewModels
{
    internal class StudioViewModel
    {
        private EvergineView evergineView;

        public StudioViewModel(EvergineView evergineView)
        {
            this.evergineView = evergineView;
        }
    }
}
