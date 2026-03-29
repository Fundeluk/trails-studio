using System;
using Managers;
using Obstacles;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UI
{
    public class RollInSetupUI : MonoBehaviour
    {
        private TextField nameInput;
        private FloatField heightInput;
        private IntegerField angleInput;
        private Label exitSpeedLabel;

        private Button buildButton;
        private Button cancelButton;

        private VisualElement root;

        private void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("rollInSetUpContainer");

            nameInput = root.Q<TextField>("NameInput");
            nameInput.isDelayed = true; // only trigger change event when user finishes editing
            
            heightInput = root.Q<FloatField>("heightInput");
            heightInput.isDelayed = true;
            
            angleInput = root.Q<IntegerField>("angleInput");
            angleInput.isDelayed = true;
            
            exitSpeedLabel = root.Q<Label>("RollInExitSpeedLabel");
            buildButton = root.Q<Button>("buildButton");
            cancelButton = root.Q<Button>("cancelButton");
            
            nameInput.RegisterValueChangedCallback(OnLineNameChanged);
            heightInput.RegisterValueChangedCallback(OnRollInHeightChanged);
            angleInput.RegisterValueChangedCallback(OnRollInAngleChanged);
            buildButton.RegisterCallback<ClickEvent>(BuildClicked);
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
            
            heightInput.value = MainMenuController.Height;
            angleInput.value = MainMenuController.Angle;
            RecalculateAndShowExitSpeed(MainMenuController.Angle, MainMenuController.Height);
        }


        private void OnLineNameChanged(ChangeEvent<string> evt)
        {
            if (evt.newValue != "") return;
            MainMenuUIManager.Instance.ShowMessage("Name cannot be empty", 3f);
            nameInput.Focus();
            buildButton.Toggle(false);
        }
        
        
        private void OnRollInHeightChanged(ChangeEvent<float> evt)
        {
            float clampedValue = Mathf.Clamp(evt.newValue, RollInSettings.MinHeight, RollInSettings.MaxHeight);

            if (!Mathf.Approximately(clampedValue, evt.newValue))
            {
                heightInput.SetValueWithoutNotify(clampedValue);
            }
            
            RecalculateAndShowExitSpeed(angleInput.value, evt.newValue);
        }
        
        private void OnRollInAngleChanged(ChangeEvent<int> evt) 
        {
            int clampedValue = Mathf.Clamp(evt.newValue, (int)RollInSettings.MinAngleDeg, (int)RollInSettings.MaxAngleDeg);

            if (clampedValue != evt.newValue)
            {
                angleInput.SetValueWithoutNotify(clampedValue);
            }
            
            RecalculateAndShowExitSpeed(evt.newValue, heightInput.value);
        }

        private void RecalculateAndShowExitSpeed(float angleDeg, float height)
        {
            if (RollIn.TryGetExitSpeedMs(angleDeg, height, out float exitSpeed))
            {
                exitSpeedLabel.text = $"{PhysicsManager.PhysicsManager.MsToKmh(exitSpeed), 10:0}km/h";
                buildButton.Toggle(true);
                return;
            }
            
            MainMenuUIManager.Instance.ShowMessage("Cannot calculate exit speed with current inputs", 3f);
            buildButton.Toggle(false);
        }

        private void BuildClicked(ClickEvent evt)
        {
            MainMenuController.LineName = nameInput.value;
            MainMenuController.Height = heightInput.value;
            MainMenuController.Angle = angleInput.value;

            ToStudio();
        }
        
        private void ToStudio()
        {
            SceneManager.sceneLoaded += OnStudioSceneLoaded;
            SceneManager.LoadScene("StudioScene", LoadSceneMode.Single);

            void OnStudioSceneLoaded(Scene scene, LoadSceneMode mode)
            {            
                // Unsubscribe from the event
                SceneManager.sceneLoaded -= OnStudioSceneLoaded;           
            }        
        }
        
        private void ToMainMenu()
        {
            enabled = false;
            root.style.display = DisplayStyle.None;
        }
        
        private void CancelClicked(ClickEvent evt)
        {
            ToMainMenu();
        }    
    }
}