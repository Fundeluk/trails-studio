using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(TakeoffMeshGenerator))]
public class TakeOffInspector : Editor
{
    public VisualTreeAsset m_InspectorXML;

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement inspector = new VisualElement();

        inspector.Add(new Label("Custom inspector"));

        m_InspectorXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/TakeOff_Inspector_UXML.uxml");

        inspector = m_InspectorXML.Instantiate();

        inspector.Q<Button>("RedrawButton").RegisterCallback<ClickEvent>(ev => ((TakeoffMeshGenerator)target).GenerateTakeoffMesh());

        return inspector;
    }

}
