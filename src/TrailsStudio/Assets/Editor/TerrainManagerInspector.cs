using Assets.Scripts.Managers;
using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using Unity.VisualScripting;

[CustomEditor(typeof(TerrainManager))]
public class TerrainManagerInspector : Editor
{
    public VisualTreeAsset m_InspectorXML;

    private ObjectField startField;
    private ObjectField endField;

    private FloatField widthField;    

    private Button getCoordsButton;

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement inspector = new();

        m_InspectorXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/TerrainManagerInspector.uxml");

        inspector = m_InspectorXML.Instantiate();

        VisualElement inspectorFoldout = inspector.Q<VisualElement>("DefaultInspector");
        InspectorElement.FillDefaultInspector(inspectorFoldout, serializedObject, this);

        getCoordsButton = inspector.Q<Button>("GetCoordsButton");
        getCoordsButton.RegisterCallback<ClickEvent>(ev => CallGetCoords());

        startField = inspector.Q<ObjectField>("StartField");
        endField = inspector.Q<ObjectField>("EndField");


        widthField = inspector.Q<FloatField>("WidthInput");       

        return inspector;
    }

    public void CallGetCoords()
    {
        int counter = 0;
        Terrain terrain = TerrainManager.GetTerrainForPosition((startField.value as GameObject).transform.position);
        foreach (var coord in TerrainManager.GetHeightmapCoordinatesForPath((startField.value as GameObject).transform.position, (endField.value as GameObject).transform.position, widthField.value))
        {
            counter++;
            if (counter == 100)
            {
                Debug.DrawRay(TerrainManager.HeightmapToWorldCoordinates(coord, terrain), Vector3.up * 5f, Color.cyan, 10f);
                counter = 0;
            }
            
        }
    }
}
