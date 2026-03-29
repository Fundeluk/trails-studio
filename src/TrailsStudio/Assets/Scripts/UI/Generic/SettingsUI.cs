using System.Collections.Generic;
using LineSystem;
using Misc;
using Obstacles;
using Obstacles.Landing;
using Obstacles.TakeOff;
using PhysicsManager;
using TerrainEditing.Slope;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UI
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
        ListView rollInSettings;
        ListView physicsSettings;

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
            rollInSettings = root.Q<ListView>("RollInListView");
            physicsSettings = root.Q<ListView>("PhysicsListView");
            
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
            RollInSettings.ResetToDefaults();
            PhysicsSettings.ResetToDefaults();
            FillAllLists();
        }

        private void FillAllLists()
        {
            FillList<FloatField, float>(takeoffSettings, TakeoffSettings.GetAllSettings());
            FillList<FloatField, float>(landingSettings, LandingSettings.GetAllSettings());
            FillList<FloatField, float>(slopeSettings, SlopeSettings.GetAllSettings());
            FillList<FloatField, float>(lineSettings, LineSettings.GetAllSettings());
            FillList<FloatField, float>(takeoffSettings, TakeoffSettings.GetAllSettings());
            FillList<FloatField, float>(rollInSettings, RollInSettings.GetAllSettings());
            FillList<FloatField, float>(physicsSettings, PhysicsSettings.GetAllSettings());
        }

        private void FillList<TField, TValue>(ListView listView, List<SettingsField<TValue>> settingFields) where TField : TextValueField<TValue>
        {
            listView.Clear();
            listView.makeItem = () =>
            {
                var newListEntry = settingsFieldTemplate.Instantiate();
                
                var newListEntryController = new SettingsEntryController<TField,TValue>();

                newListEntry.userData = newListEntryController;

                newListEntryController.Initialize(newListEntry);

                return newListEntry;
            };
            
            listView.bindItem = (element, index) =>
            {
                if (element.userData is SettingsEntryController<TField,TValue> controller)
                {
                    controller.SetField(settingFields[index]);
                }
            };
            
            listView.fixedItemHeight = 100;

            listView.itemsSource = settingFields;
        }
    }

    public class SettingsEntryController<TField, TValue> where TField : TextValueField<TValue>
    {
        private TField field;
        private Label fieldLabel;
        private Label description;
        
        public void Initialize(VisualElement root)
        {
            field = root.Q<TField>("ValueInput");
            fieldLabel = field.Q<Label>();
            description = root.Q<Label>("EntryDescription");
        }
        
        public void SetField(SettingsField<TValue> settingsField)
        {
            field.value = settingsField;
            fieldLabel.text = settingsField.Unit.Length != 0 
                ? settingsField.DisplayName + $" ({settingsField.Unit})" : settingsField.DisplayName;
            
            description.text = settingsField.Description;

            field.isDelayed = true; // Update value only after user stops typing
            field.RegisterValueChangedCallback(evt =>
            {
                settingsField.SetValue(evt.newValue);
            });
        }
    }

    
}