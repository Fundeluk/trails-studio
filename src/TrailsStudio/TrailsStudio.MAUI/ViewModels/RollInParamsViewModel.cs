using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TrailsStudio.MAUI.ViewModels
{
    public class RollInParamsViewModel : INotifyPropertyChanged
    {
        private readonly INavigation _navigation;

        private string rollInHeightEntry;
        private string rollInAngleEntry;

        private int rollInHeight;
        private int rollInAngle;

        private bool heightValid;
        private bool angleValid;
        private bool areParamsValid;
        public ICommand SetParamsCommand { get; }

        public ICommand PopBackCommand { get; }

        public RollInParamsViewModel(INavigation navigation)
        {
            SetParamsCommand = new Command(
                execute: async () =>
                {
                    var navigationParams = new ShellNavigationQueryParameters
                    {
                        {"rollInHeight", rollInHeight },
                        { "rollInAngle", rollInAngle }
                    };
                    await Shell.Current.GoToAsync($"studiopage", navigationParams);

                },
                canExecute: () => AreParamsValid);

            PopBackCommand = new Command(
                execute: async () =>
                {
                    await _navigation.PopAsync();
                });

            _navigation = navigation;
        }

        public string RollInHeightEntry
        {
            get => rollInHeightEntry;
            set
            {
                if (rollInHeightEntry == value)
                    return;

                rollInHeightEntry = value;
                OnPropertyChanged();
                ValidateEntries();
            }
        }

        public string RollInAngleEntry
        {
            get => rollInAngleEntry;
            set
            {
                if (rollInAngleEntry == value)
                    return;

                rollInAngleEntry = value;
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
            bool heightValid = int.TryParse(RollInHeightEntry, out int height) && height > 0 && height < 10000;
            bool angleValid = int.TryParse(RollInAngleEntry, out int angle) && angle > 0 && angle < 1000;

            HeightValid = heightValid;
            if (heightValid) rollInHeight = height;

            AngleValid = angleValid;
            if (angleValid) rollInAngle = angle;

            AreParamsValid = heightValid && angleValid;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
