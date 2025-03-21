using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single cell in the procedural grid system, managing tile selection and neighbor relationships.
/// </summary>
public class Cell : MonoBehaviour
{
    public bool isCollapsed = false, isBeingValidated = false;
    public Tile selectedTile;
    public Tile[] tileOptions;
    public Cell cellAbove = null, cellToTheRight = null, cellBelow = null, cellToTheLeft = null;

    /// <summary>
    /// Initializes a new cell with given collapse state and tile options.
    /// </summary>
    public void CreateCell(bool collapseState, Tile[] tiles)
    {
        isCollapsed = collapseState;
        tileOptions = tiles;
    }

    /// <summary>
    /// Assigns a tile to the cell and creates its visual representation.
    /// </summary>
    public void CreateTile(Tile tile)
    {
        selectedTile = tile;
        CreateTile();
    }
    /// <summary>
    /// Instantiates the selected tile if the cell is collapsed.
    /// </summary>
    public void CreateTile()
    {
        if (isCollapsed)
        {
            GameObject tileObject = selectedTile.GetTileObject(transform.position, Quaternion.identity);
            tileObject.transform.SetParent(transform);
        }
    }

    /// <summary>
    /// Retrieves the neighboring cell in a specified direction.
    /// </summary>
    public Cell GetCellNeighbor(int dir4way)
    {
        return dir4way switch
        {
            0 => cellAbove,
            1 => cellToTheRight,
            2 => cellBelow,
            3 => cellToTheLeft,
            _ => null
        };
    }

    /// <summary>
    /// Sets a neighboring cell in a specified direction.
    /// </summary>
    public void SetCellNeighbor(int dir4way, Cell newCell)
    {
        switch (dir4way)
        {
            case 0: cellAbove = newCell; break;
            case 1: cellToTheRight = newCell; break;
            case 2: cellBelow = newCell; break;
            case 3: cellToTheLeft = newCell; break;
        }
    }

    /// <summary>
    /// Sets a tile for the cell and removes existing children.
    /// </summary>
    public void SetTile(Tile tile)
    {
        DestroyCellChildren();
        selectedTile = tile;
        isCollapsed = true;
    }

    /// <summary>
    /// Sets a fallback tile and restricts available options to the fallback tile.
    /// </summary>
    public void SetFallBackTile(Tile tile)
    {
        DestroyCellChildren();
        selectedTile = tile;
        isCollapsed = true;
        tileOptions = new Tile[] { tile };
    }
    /// <summary>
    /// Refills the cell with new tile options, marking it as uncollapsed.
    /// </summary>
    public void ReFillCell(Tile[] tiles)
    {
        tileOptions = tiles;
        isCollapsed = false;
    }

    /// <summary>
    /// Destroys all child objects of the cell.
    /// </summary>
    public void DestroyCellChildren()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        isCollapsed = false;
    }

    void OnDestroy()
    {
        DestroyCellChildren();
    }
}