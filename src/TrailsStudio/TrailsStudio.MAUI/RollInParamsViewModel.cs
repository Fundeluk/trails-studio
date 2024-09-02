using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TrailsStudio.MAUI
{
    public class RollInParamsViewModel : INotifyPropertyChanged
    {
        private string rollInHeight;
        private string rollInAngle;
        private bool areParamsValid;

        public string RollInHeight
        {
            get => rollInHeight;
            set
            {
                if (rollInHeight == value)
                    return;

                rollInHeight = value;
                OnPropertyChanged();
                ValidateEntries();
            }
        }

        public string RollInAngle
        {
            get => rollInAngle;
            set
            {
                if (rollInAngle == value)
                    return;

                rollInAngle = value;
                OnPropertyChanged();
                ValidateEntries();
            }
        }

        public bool AreParamsValid
        {
            get => areParamsValid;
            set
            {
                if (areParamsValid == value)
                    return;

                areParamsValid = value;
                OnPropertyChanged();
            }
        }

        private void ValidateEntries()
        {
            bool heightValid = int.TryParse(RollInHeight, out int height) && height > 0 && height < 10000;
            bool angleValid = int.TryParse(RollInAngle, out int angle) && angle > 0 && angle < 1000;

            AreParamsValid = heightValid && angleValid;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
