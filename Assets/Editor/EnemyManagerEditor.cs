using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


[CustomEditor(typeof(EnemyManager))]
public class EnemyManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EnemyManager manager = (EnemyManager)target;

        string[] sceneNames = new string[SceneManager.sceneCountInBuildSettings];

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            sceneNames[i] = System.IO.Path.GetFileNameWithoutExtension(path);
        }

        manager.nextSceneIndex = EditorGUILayout.Popup("Next Scene", manager.nextSceneIndex, sceneNames);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(manager);
        }
    }
}

