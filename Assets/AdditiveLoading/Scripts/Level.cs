using System.Collections.Generic;
using UnityEngine;

public class Level
{
    Dictionary<Vector3Int, bool> levelGrid = new Dictionary<Vector3Int, bool>();
    List<GameObject> levelObjects = new List<GameObject>();

    BoundsInt levelBounds = new();

    /// <summary>
    /// Returns the total size of the level.
    /// </summary>
    /// <remarks>
    /// Setting the <see cref="LevelBounds"/> property will expand the current bounds  
    /// to include the new bounds.
    /// <para/>
    /// To reset the bounds, use the <see cref="ResetLevelBounds"/> method.
    /// </remarks>
    BoundsInt LevelBounds
    {
        get => levelBounds; 
        set
        {
            // Expand the bounds to include the new bounds
            levelBounds.min = Vector3Int.Min(levelBounds.min, value.min);
            levelBounds.max = Vector3Int.Max(levelBounds.max, value.max);
            levelBounds.size = levelBounds.max - levelBounds.min;
        }
    }
    public void ResetLevelBounds() => levelBounds = new BoundsInt(Vector3Int.zero, Vector3Int.zero);

    /// <summary>
    /// Gets the boundaries of the level as a <see cref="BoundsInt"/> structure.
    /// </summary>
    public BoundsInt Bounds
    {
        get { return levelBounds; }
    }

    /// <summary>
    /// Gets a list of grid positions representing the current level layout.
    /// </summary>
    /// <remarks> 
    /// Grid size is 1x1x1 units with the value being the minimum corner of each grid cell occupied by level geometry.
    /// </remarks>
    public List<Vector3Int> LevelGrid { get { return new List<Vector3Int>(levelGrid.Keys); } }


    /// <summary>
    /// Find all game objects in this scene that belong to this level and add them to the levelObjects dictionary.
    /// </summary>
    public void CheckLevelBounds(GameObject gameObject)
    {
        levelObjects.Clear();
        levelGrid.Clear();

        GameObject[] allObjects = gameObject.scene.GetRootGameObjects();

        // Iterate through all game objects in the scene
        // If they have geometry fill all 1x1x1 cells they occupy in the levelGrid dictionary
        foreach (GameObject obj in allObjects)
        {
            if (!MapObjectToLevelGrid(obj)) continue;
        }

    }

    /// <summary>
    /// Maps the specified game object to the level grid based on its bounds.
    /// </summary>
    /// <remarks>This method adds the game object to the internal collection of level objects and marks the
    /// corresponding grid cells as occupied based on the object's bounds. If the object's bounds are <see
    /// langword="null"/>, the object is not added to the grid.</remarks>
    /// <param name="obj">The game object to map to the level grid. Cannot be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the object was successfully mapped to the level grid;  otherwise, <see
    /// langword="false"/> if the object is invalid or its bounds could not be determined.</returns>
    private bool MapObjectToLevelGrid(GameObject obj)
    {
        if (!obj) return false;
        BoundsInt? bounds = GetBounds(obj);

        if (bounds == null) return false;

        levelObjects.Add(obj);

        for (int x = bounds.Value.min.x; x < bounds.Value.max.x; x++)
        {
            for (int y = bounds.Value.min.y; y < bounds.Value.max.y; y++)
            {
                for (int z = bounds.Value.min.z; z < bounds.Value.max.z; z++)
                {
                    // TODO If an object ovberlapping a cell is diminimus in size, we may want to skip adding it to the grid
                    Vector3Int cell = new(x, y, z);
                    levelGrid.TryAdd(cell, true);
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Calculates the axis-aligned bounding box of the specified <see cref="GameObject"/> in integer coordinates.
    /// </summary>
    /// <remarks>The bounds are calculated based on the <see cref="Renderer.bounds"/> of the <paramref
    /// name="gameObject"/>, with the minimum and maximum values rounded to the nearest integers using <see
    /// cref="Vector3Int.FloorToInt"/>  and <see cref="Vector3Int.CeilToInt"/>, respectively.</remarks>
    /// <param name="gameObject">The <see cref="GameObject"/> for which to calculate the bounds. Must not be <c>null</c>.</param>
    /// <returns>A <see cref="BoundsInt"/> representing the calculated bounds of the <paramref name="gameObject"/> in integer
    /// coordinates, or <c>null</c> if the <paramref name="gameObject"/> does not have a <see cref="Renderer"/>
    /// component.</returns>
    BoundsInt? GetBounds(GameObject gameObject)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();

        if(!renderer)
        {
            return null;
        }

        BoundsInt bounds = new()
        {
            min = Vector3Int.FloorToInt(renderer.bounds.min),
            max = Vector3Int.CeilToInt(renderer.bounds.max),
            size = Vector3Int.CeilToInt(renderer.bounds.size)
        };

        return bounds;
    }
}
