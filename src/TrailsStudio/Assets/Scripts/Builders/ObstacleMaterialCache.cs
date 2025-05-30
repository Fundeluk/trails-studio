using UnityEngine;

[CreateAssetMenu(fileName = "ObstacleMaterialCache", menuName = "Scriptable Objects/ObstacleMaterialCache")]
public class ObstacleMaterialCache : ScriptableObject
{
    public Material canBuildMaterial;
    public Material cannotBuildMaterial;
    public Material defaultDirtMaterial;
}
