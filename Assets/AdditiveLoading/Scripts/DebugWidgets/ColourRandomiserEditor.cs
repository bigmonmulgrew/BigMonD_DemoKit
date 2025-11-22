using UnityEditor;
using UnityEngine;
/// <summary>
/// Provides a button on the ColourRandomiser component to randomise the colour of all child objects in the editor.
/// </summary>
[CustomEditor(typeof(ColourRandomiser))] 
public class ColourRandomiserEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ColourRandomiser colourRandomiser = (ColourRandomiser)target;

        if (GUILayout.Button("Randomise Colour"))
        {
            colourRandomiser.RandomiseColour();
        }
    }
}
