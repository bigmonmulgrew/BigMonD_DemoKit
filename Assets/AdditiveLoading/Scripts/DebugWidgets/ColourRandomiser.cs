using UnityEngine;

/// <summary>
/// Simple debugging script to get all child gameobjects and set their colour at editor time to a random colour.
/// One random colour is chosed and applied to all children.
/// Script has a button in the inspector to trigger the randomisation.
/// </summary>
public class ColourRandomiser : MonoBehaviour
{
    Color objectColour;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [ContextMenu("Randomise Colour")]
    public void RandomiseColour()
    {
        objectColour = new Color(Random.value, Random.value, Random.value);
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.material.color = objectColour;
        }
    }


}
