using Assets.Scripts.Misc;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.Builders.Slope;

namespace Assets.Scripts.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [SerializeField] VisualTreeAsset settingsFieldTemplate;

        VisualElement settings;

        //TabView settingTabView;

        ListView takeoffSettings;
        ListView landingSettings;
        ListView slopeSettings;
        ListView lineSettings;

        Button restoreDefaultsButton;
        Button closeButton;
        
        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            settings = root.Q<VisualElement>("SettingsContainer");

            closeButton = root.Q<Button>("CloseButton");
            closeButton.RegisterCallback<ClickEvent>(CloseClicked);

            restoreDefaultsButton = root.Q<Button>("RestoreDefaultsButton");
            restoreDefaultsButton.RegisterCallback<ClickEvent>(RestoreDefaults);


            //settingTabView = root.Q<TabView>("SettingTabView");

            takeoffSettings = root.Q<ListView>("TakeoffListView");
            landingSettings = root.Q<ListView>("LandingListView");
            slopeSettings = root.Q<ListView>("SlopeListView");
            lineSettings = root.Q<ListView>("LineListView");

            FillAllLists();
        }

        private void OnDisable()
        {
            PlayerPrefs.Save();
        }

        private void CloseClicked(ClickEvent evt)
        {
            enabled = false;
            settings.style.display = DisplayStyle.None;
        }
        private void RestoreDefaults(ClickEvent evt)
        {
            TakeoffSettings.ResetToDefaults();
            LandingSettings.ResetToDefaults();
            SlopeSettings.ResetToDefaults();
            LineSettings.ResetToDefaults();
            FillAllLists();
        }

        private void FillAllLists()
        {
            FillTakeoffList();
            FillLandingList();
            FillSlopeList();
            FillLineList();
        }


        private void FillTakeoffList()
        {
            List<SettingsField<float>> takeoffSettingsList = TakeoffSettings.GetAllSettings();

            takeoffSettings.Clear();
            takeoffSettings.makeItem = () =>
            {
                var newListEntry = settingsFieldTemplate.Instantiate();

                var newListEntryController = new SettingsFloatEntryController();

                newListEntry.userData = newListEntryController;

                newListEntryController.Initialize(newListEntry);

                return newListEntry;
            };

            takeoffSettings.bindItem = (element, index) =>
            {
                (element.userData as SettingsFloatEntryController)?.SetField(takeoffSettingsList[index]);
            };

            takeoffSettings.fixedItemHeight = 100;
            
            takeoffSettings.itemsSource = takeoffSettingsList;
        }

        private void FillLandingList()
        {
            List<SettingsField<float>> landingSettingsList = LandingSettings.GetAllSettings();

            landingSettings.Clear();
            landingSettings.makeItem = () =>
            {
                var newListEntry = settingsFieldTemplate.Instantiate();

                var newListEntryController = new SettingsFloatEntryController();

                newListEntry.userData = newListEntryController;

                newListEntryController.Initialize(newListEntry);

                return newListEntry;
            };

            landingSettings.bindItem = (element, index) =>
            {
                (element.userData as SettingsFloatEntryController)?.SetField(landingSettingsList[index]);
            };

            landingSettings.fixedItemHeight = 100;

            landingSettings.itemsSource = landingSettingsList;
        }

        private void FillSlopeList()
        {
            List<SettingsField<float>> slopeSettingsList = SlopeSettings.GetAllSettings();

            slopeSettings.Clear();
            slopeSettings.makeItem = () =>
            {
                var newListEntry = settingsFieldTemplate.Instantiate();

                var newListEntryController = new SettingsFloatEntryController();

                newListEntry.userData = newListEntryController;

                newListEntryController.Initialize(newListEntry);

                return newListEntry;
            };

            slopeSettings.bindItem = (element, index) =>
            {
                (element.userData as SettingsFloatEntryController)?.SetField(slopeSettingsList[index]);
            };

            slopeSettings.fixedItemHeight = 100;

            slopeSettings.itemsSource = slopeSettingsList;
        }

        private void FillLineList()
        {
            List<SettingsField<float>> lineSettingsList = LineSettings.GetAllSettings();

            lineSettings.Clear();
            lineSettings.makeItem = () =>
            {
                var newListEntry = settingsFieldTemplate.Instantiate();

                var newListEntryController = new SettingsFloatEntryController();

                newListEntry.userData = newListEntryController;

                newListEntryController.Initialize(newListEntry);

                return newListEntry;
            };

            lineSettings.bindItem = (element, index) =>
            {
                (element.userData as SettingsFloatEntryController)?.SetField(lineSettingsList[index]);
            };

            lineSettings.fixedItemHeight = 100;

            lineSettings.itemsSource = lineSettingsList;
        }

    }

    public class SettingsFloatEntryController
    {
        private FloatField field;
        private Label fieldLabel;
        private Label description;

        public void Initialize(VisualElement root)
        {
            field = root.Q<FloatField>("ValueInput");
            fieldLabel = field.Q<Label>();
            description = root.Q<Label>("EntryDescription");
        }

        public void SetField(SettingsField<float> settingsField)
        {
            field.value = settingsField;
            fieldLabel.text = settingsField.displayName + $" ({settingsField.unit})";
            description.text = settingsField.description;

            field.isDelayed = true; // Update value only after user stops typing
            field.RegisterValueChangedCallback(evt =>
            {
                settingsField.SetValue(evt.newValue);
            });
        }
    }
}