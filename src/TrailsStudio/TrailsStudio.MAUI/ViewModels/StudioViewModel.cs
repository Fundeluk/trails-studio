using Evergine.Mathematics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TrailsStudio.MAUI.Evergine;

using TrailsStudio.Services;

namespace TrailsStudio.MAUI.ViewModels
{
    internal class StudioViewModel(EvergineView evergineView) : IQueryAttributable, INotifyPropertyChanged
    {
        private EvergineView evergineView = evergineView;
        private ControllerService controllerService = evergineView.Application.Container.Resolve<ControllerService>();

        private Vector3 cameraPos = Vector3.Zero;

        public float cameraX
        {
            get => cameraPos.X;
            set
            {
                if (cameraPos.X != value)
                {
                    cameraPos.X = value;
                    OnPropertyChanged();
                }                    
            }
        }

        public float cameraY
        {
            get => cameraPos.Y;
            set
            {
                if (cameraPos.Y != value)
                {
                    cameraPos.Y = value;
                    OnPropertyChanged();
                }
            }
        }

        public float cameraZ
        {
            get => cameraPos.Z;
            set
            {
                if (cameraPos.Z != value)
                {
                    cameraPos.Z = value;
                    OnPropertyChanged();
                }
            }
        }


        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            int rollInHeight = (int)query["rollInHeight"];
            int rollInAngle = (int)query["rollInAngle"];
            controllerService.RegisterRollInParams(rollInHeight, rollInAngle);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
