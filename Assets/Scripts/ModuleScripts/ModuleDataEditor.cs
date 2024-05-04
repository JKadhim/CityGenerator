using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(ModuleData))]
public class ModuleDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ModuleData moduleData = (ModuleData)target;

        int count = moduleData.modules != null ? moduleData.modules.Length : 0;
        GUILayout.Label(count + " Modules");

        if (GUILayout.Button("Create module data"))
        {
            moduleData.CreateModules();
        }

        if (GUILayout.Button("Simplify module data"))
        {
            moduleData.SimplifyNeighbour();
        }
    }
}
