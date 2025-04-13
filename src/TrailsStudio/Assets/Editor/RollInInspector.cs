using Assets.Scripts.Builders;
using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using Unity.VisualScripting;
using Assets.Scripts.Managers;


[CustomEditor(typeof(RollInBuilder))]
public class RollInInspector : Editor
{
    public VisualTreeAsset m_InspectorXML;    

    private Button buildButton;

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement inspector = new();

        m_InspectorXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/RollInInspector.uxml");

        inspector = m_InspectorXML.Instantiate();

        VisualElement defaultInspector = inspector.Q<VisualElement>("DefaultInspector");
        InspectorElement.FillDefaultInspector(defaultInspector, serializedObject, this);

        buildButton = inspector.Q<Button>("BuildButton");
        buildButton.RegisterCallback<ClickEvent>(ev => ((RollInBuilder)target).CreateRollIn());        

        return inspector;
    }    
}
