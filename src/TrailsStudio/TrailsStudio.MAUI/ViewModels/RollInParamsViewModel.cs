using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TrailsStudio.MAUI.ViewModels
{
    public class RollInParamsViewModel : INotifyPropertyChanged
    {
        private readonly INavigation _navigation;

        private string rollInHeight;
        private string rollInAngle;
        private bool heightValid;
        private bool angleValid;
        private bool areParamsValid;
        public ICommand SetParamsCommand { get; }

        public ICommand PopBackCommand { get; }

        public RollInParamsViewModel(INavigation navigation)
        {
            SetParamsCommand = new Command(
                execute: () =>
                {
                    // Save the parameters
                },
                canExecute: () => AreParamsValid);

            PopBackCommand = new Command(
                execute: async () =>
                {
                    await _navigation.PopAsync();
                });

            _navigation = navigation;
        }

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

        public bool HeightValid
        {
            get => heightValid;
            set
            {
                if (heightValid == value)
                    return;

                heightValid = value;
                OnPropertyChanged();
            }
        }

        public bool AngleValid
        {
            get => angleValid;
            set
            {
                if (angleValid == value)
                    return;

                angleValid = value;
                OnPropertyChanged();
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
                ((Command)SetParamsCommand).ChangeCanExecute();
            }
        }

        private void ValidateEntries()
        {
            bool heightValid = int.TryParse(RollInHeight, out int height) && height > 0 && height < 10000;
            bool angleValid = int.TryParse(RollInAngle, out int angle) && angle > 0 && angle < 1000;

            HeightValid = heightValid;
            AngleValid = angleValid;

            AreParamsValid = heightValid && angleValid;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
