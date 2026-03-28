using TerrainEditing;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(TerrainManager))]
public class TerrainManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector (keeps all other fields as they are)
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug & Visualization", EditorStyles.boldLabel);

        // Disable the button if the game is not playing
        EditorGUI.BeginDisabledGroup(!Application.isPlaying);

        if (GUILayout.Button("Draw Coordinate States (Rays)"))
        {
            TerrainManager manager = (TerrainManager)target;
            manager.DebugDrawCoordinateStates();
        }

        EditorGUI.EndDisabledGroup();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use the visualization.", MessageType.Info);
        }
    }
    
}
