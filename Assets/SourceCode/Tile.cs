using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;  // Needed for Array.Resize and other general utilities
using System.Linq;  // Needed for ToList() and other LINQ methods

public class Tile : MonoBehaviour
{
    public Tile subTile;
    public float weight = 1;

    // public bool isWalkable = true;
    // the 4 direction neighbours
    public Tile[] upNeighbours;
    public Tile[] rightNeighbours;
    public Tile[] downNeighbours;
    public Tile[] leftNeighbours;

    [Header("Constraints")]

    // the 4 direction constraint tiles that this tile shouldnot connect to

    public Tile[] upConstraints;
    public Tile[] rightConstraints;
    public Tile[] downConstraints;
    public Tile[] leftConstraints;


    // [SerializeField] public int ConnectionsCounts = 0;
    public int ConnectionsCounts = 0;

    // Collection of tiles with constraints that this tile will combine and compare with its own neighbors
    public Tile[] constraintBaseTiles = new Tile[0];
    public Tile[] surroundingTiles;// auto fills in 4 direction neibours

    void Start()
    {
        OnTileSpawned();

        CountConnectionss();
    }

    public virtual void OnTileSpawned()
    {
        // Debug.Log("OnTileSpawned");
        // Set Layer to TileLayer
        gameObject.layer = LayerMask.NameToLayer("TileLayer");
        gameObject.tag = "Tile";  // Set the tag for tile detection
        // Remove "(Clone)" from the name
        gameObject.name = gameObject.name.Replace("(Clone)", "");
    }



    public virtual GameObject GetTileObject(Vector3 position, Quaternion rotation)
    {
        // Debug.Log("GetTileObject");
        return Instantiate(gameObject, position, rotation);
    }

    public void CountConnectionss()
    {
        ConnectionsCounts = 0;
        if (upNeighbours != null)
        {
            ConnectionsCounts += upNeighbours.Length;
        }
        if (rightNeighbours != null)
        {
            ConnectionsCounts += rightNeighbours.Length;
        }
        if (downNeighbours != null)
        {
            ConnectionsCounts += downNeighbours.Length;
        }
        if (leftNeighbours != null)
        {
            ConnectionsCounts += leftNeighbours.Length;
        }
    }

    public bool AllNeighborsContainSelf()
    {
        bool toReturn = true;
        int matchCount = 0;
        foreach (Tile t_up in upNeighbours)
        {
            if (t_up.downNeighbours != null && t_up.downNeighbours.Length > 0)
            {
                if (ArrayContainsTile(t_up.downNeighbours, this)) matchCount++;
            }
        }
        if (matchCount != upNeighbours.Length)
        {
            Debug.LogWarning("Missing Neighbours: upNeighbours: " + matchCount + " Length " + upNeighbours.Length + " for " + gameObject.name);
            FindAndReportMissingTileConnections(upNeighbours, 0);
            toReturn = false;
        }
        matchCount = 0;
        foreach (Tile t_dw in downNeighbours)
        {
            if (t_dw.upNeighbours != null && t_dw.upNeighbours.Length > 0)
            {
                if (ArrayContainsTile(t_dw.upNeighbours, this)) matchCount++;
            }
        }
        if (matchCount != downNeighbours.Length)
        {
            Debug.LogWarning("Missing Neighbours: downNeighbours: " + matchCount + " Length " + downNeighbours.Length + " for " + gameObject.name);
            toReturn = false;
            FindAndReportMissingTileConnections(downNeighbours, 2);
        }
        matchCount = 0;
        foreach (Tile t_rt in rightNeighbours)
        {
            if (t_rt.leftNeighbours != null && t_rt.leftNeighbours.Length > 0)
            {
                if (ArrayContainsTile(t_rt.leftNeighbours, this)) matchCount++;
            }
        }
        if (matchCount != rightNeighbours.Length)
        {
            Debug.LogWarning("Missing Neighbours: rightNeighbours: " + matchCount + " Length " + rightNeighbours.Length + " for " + gameObject.name);
            toReturn = false;
            FindAndReportMissingTileConnections(rightNeighbours, 1);
        }
        matchCount = 0;
        foreach (Tile t_lf in leftNeighbours)
        {
            if (t_lf.rightNeighbours != null && t_lf.rightNeighbours.Length > 0)
            {
                if (ArrayContainsTile(t_lf.rightNeighbours, this)) matchCount++;
            }
        }
        if (matchCount != leftNeighbours.Length)
        {
            Debug.LogWarning("Missing Neighbours: leftNeighbours: " + matchCount + " Length " + leftNeighbours.Length + " for " + gameObject.name);
            toReturn = false;
            FindAndReportMissingTileConnections(leftNeighbours, 3);
        }
        return toReturn;
    }

    // list direction 0 = up, 1 = right, 2 = down, 3 = left
    void FindAndReportMissingTileConnections(Tile[] myList, int direction)
    {
        string OppsiteDirection = "down";
        switch (direction)
        {
            case 1:
                OppsiteDirection = "left";
                break;
            case 2:
                OppsiteDirection = "up";
                break;
            case 3:
                OppsiteDirection = "right";
                break;
        }

        string toReport = $"{gameObject.name} is missing connections from {OppsiteDirection} of: ";
        foreach (Tile susTile in myList)
        {
            if (direction == 0)
            {
                if (!Array.Exists(susTile.downNeighbours, tile => tile == this))
                {
                    toReport += $"{susTile.gameObject.name} ";
                }
            }
            else if (direction == 1)
            {
                if (!Array.Exists(susTile.leftNeighbours, tile => tile == this))
                {
                    toReport += $"{susTile.gameObject.name} ";
                }
            }
            else if (direction == 2)
            {
                if (!Array.Exists(susTile.upNeighbours, tile => tile == this))
                {
                    toReport += $"{susTile.gameObject.name} ";
                }
            }
            else if (direction == 3)
            {
                if (!Array.Exists(susTile.rightNeighbours, tile => tile == this))
                {
                    toReport += $"{susTile.gameObject.name} ";
                }
            }

        }
        Debug.LogWarning(toReport);
    }


    // check a tile has me in its neighbors array
    // dir 4 meaning any direction works
    // dir 0-3 is asking for specific direction of the neighbor
    // Example: dir = 3 asks if neighbor has this tile in neighbor's leftNeighbours array
    public bool ThisNeighborHasMe(Tile neighbor, int dir = 4)
    {
        // Any direction
        if (dir == 4)
        {
            if (Array.Exists(neighbor.upNeighbours, tile => tile == this)) return true;
            if (Array.Exists(neighbor.rightNeighbours, tile => tile == this)) return true;
            if (Array.Exists(neighbor.downNeighbours, tile => tile == this)) return true;
            if (Array.Exists(neighbor.leftNeighbours, tile => tile == this)) return true;
            return false;
        }
        //specific direction this neighbor has me in this direction
        //3 means neighbor has me in its left
        switch (dir)
        {
            case 0:
                return (Array.Exists(neighbor.upNeighbours, tile => tile == this));
            case 1:
                return (Array.Exists(neighbor.rightNeighbours, tile => tile == this));
            case 2:
                return (Array.Exists(neighbor.downNeighbours, tile => tile == this));
            case 3:
                return (Array.Exists(neighbor.leftNeighbours, tile => tile == this));
            default:
                return false;
        }
    }

    public bool ArrayContainsTile(Tile[] tileArray, Tile target)
    {
        return Array.Exists(tileArray, t => t.gameObject.name == target.gameObject.name);
    }

    [ContextMenu("AddMeToMyNeighbors")]
    //Add To Every Neighbor if this is not existing on their oppsite neighbor
    //Can't Undo, Check Before use this.
    public void AddMeToMyNeighbors()
    {
        // Check and fix upNeighbours
        foreach (Tile t_up in upNeighbours)
        {
            if (t_up.downNeighbours != null)
            {
                AddMeToEndOfThisArray(ref t_up.downNeighbours, $"{t_up.gameObject.name}'s downNeighbours");
            }
        }

        // Check and fix downNeighbours
        foreach (Tile t_down in downNeighbours)
        {
            if (t_down.upNeighbours != null)
            {
                AddMeToEndOfThisArray(ref t_down.upNeighbours, $"{t_down.gameObject.name}'s upNeighbours");
            }
        }

        // Check and fix rightNeighbours
        foreach (Tile t_right in rightNeighbours)
        {
            if (t_right.leftNeighbours != null)
            {
                AddMeToEndOfThisArray(ref t_right.leftNeighbours, $"{t_right.gameObject.name}'s leftNeighbours");
            }
        }

        // Check and fix leftNeighbours
        foreach (Tile t_left in leftNeighbours)
        {
            if (t_left.rightNeighbours != null)
            {
                AddMeToEndOfThisArray(ref t_left.rightNeighbours, $"{t_left.gameObject.name}'s rightNeighbours");
            }
        }
    }
    private void AddMeToEndOfThisArray(ref Tile[] array, string neighborName)
    {
        // Check if this tile already exists in the array
        if (!Array.Exists(array, tile => tile == this))
        {
            Debug.LogWarning($"Fixing missing link: Adding {this.gameObject.name} to {neighborName}");

            List<Tile> tempList = array.ToList();
            tempList.Add(this);
            array = tempList.ToArray(); // Update the original array reference
        }
    }

    [ContextMenu("CleanUpArrays")]
    public void CleanUpDuplicates()
    {
        CleanUpArray(ref upNeighbours);
        CleanUpArray(ref rightNeighbours);
        CleanUpArray(ref downNeighbours);
        CleanUpArray(ref leftNeighbours);
    }


    private void CleanUpArray(ref Tile[] array)
    {
        array = new HashSet<Tile>(array.Where(tile => tile != null)).ToArray();
    }

    public void RemoveMeFromMyNeighbor(int dir4way)
    {
        Tile[] neighbors = null;
        // string oppositeSide = "";

        // Determine the direction and assign the appropriate neighbor array
        switch (dir4way)
        {
            case 0:
                neighbors = upNeighbours;
                // oppositeSide = "downNeighbours";
                break;
            case 1:
                neighbors = rightNeighbours;
                // oppositeSide = "leftNeighbours";
                break;
            case 2:
                neighbors = downNeighbours;
                // oppositeSide = "upNeighbours";
                break;
            case 3:
                neighbors = leftNeighbours;
                // oppositeSide = "rightNeighbours";
                break;
            default:
                Debug.LogError("Invalid direction provided to RemoveMeFromMyNeighbor. Use 0 (up), 1 (right), 2 (down), or 3 (left).");
                return;
        }
        // Opposite direction mapping: 0 = down, 1 = left, 2 = up, 3 = right
        int oppositeDir = (dir4way + 2) % 4;

        // Go through each neighbor and remove this tile from their opposite neighbor list
        foreach (Tile neighbor in neighbors)
        {
            if (neighbor != null)
            {
                neighbor.RemoveTileFromArray(this, oppositeDir);
            }
        }
    }

    // Adds a method to remove this tile from a specific neighbor array inside the neighbor tile
    public void RemoveTileFromArray(Tile tileToRemove, int dir4way)
    {
        Tile[] targetArray = GetNeighborArray(dir4way);

        if (targetArray != null && Array.Exists(targetArray, tile => tile == tileToRemove))
        {
            Debug.LogWarning($"Removing {tileToRemove.gameObject.name} from {gameObject.name}'s {GetDirectionName(dir4way)} neighbors");
            targetArray = targetArray.Where(tile => tile != tileToRemove && tile != null).ToArray();

            // Update the specific direction neighbor array
            SetNeighborArray(targetArray, dir4way);
        }
    }

    // Returns a specific neighbor array based on direction (0 = up, 1 = right, 2 = down, 3 = left)
    private Tile[] GetNeighborArray(int dir4way)
    {
        return dir4way switch
        {
            0 => upNeighbours,
            1 => rightNeighbours,
            2 => downNeighbours,
            3 => leftNeighbours,
            _ => leftNeighbours
        };
    }
    // Sets the neighbor array in the specified direction
    private void SetNeighborArray(Tile[] array, int dir4way)
    {
        switch (dir4way)
        {
            case 0:
                upNeighbours = array;
                break;
            case 1:
                rightNeighbours = array;
                break;
            case 2:
                downNeighbours = array;
                break;
            case 3:
                leftNeighbours = array;
                break;
        }
    }

    // Optional helper for debugging: returns direction name based on index
    private string GetDirectionName(int dir4way)
    {
        return dir4way switch
        {
            0 => "up",
            1 => "right",
            2 => "down",
            3 => "left",
            _ => "unknown"
        };
    }

    [ContextMenu("DelinkTop")]
    public void DelinkTop() { RemoveMeFromMyNeighbor(0); }


    [ContextMenu("DelinkRight")]
    public void DelinkRight() { RemoveMeFromMyNeighbor(1); }

    [ContextMenu("DelinkBottom")]

    public void DelinkBottom() { RemoveMeFromMyNeighbor(2); }

    [ContextMenu("DelinkLeft")]

    public void DelinkLeft() { RemoveMeFromMyNeighbor(3); }

    [ContextMenu("DelinkAll")]

    public void DelinkAll()
    {
        RemoveMeFromMyNeighbor(0);
        RemoveMeFromMyNeighbor(1);
        RemoveMeFromMyNeighbor(2);
        RemoveMeFromMyNeighbor(3);
    }

    [ContextMenu("InitializeNeighbors")]

    // Remove all neighbors
    public void InitializeNeighbors()
    {
        upNeighbours = new Tile[0];
        rightNeighbours = new Tile[0];
        downNeighbours = new Tile[0];
        leftNeighbours = new Tile[0];
    }


    [ContextMenu("CrossOverConstraints")]
    // Update my constraint arrays with the overlap of my constraint and constraintTile's same direction constraint
    // after four constraint directions are modified, check against my neighbors in the corresponding direction
    // Exapmle: my NeighborUp has ABCDE and neighborDown has FGHIJ
    // my constraintUp has D, constraintTile1Up has AB, constraintTile2Up has BC
    // my constraintDown has nothing, constraintTile1Down has FG, constraintTile2Down has GH
    // after this method, my constraintUp will be ABCD
    // my constraintDown will be FGH

    public void CrossOverConstraints()
    {
        // Check if the constraintBaseTiles array is empty
        if (constraintBaseTiles.Length == 0)
        {
            Debug.LogWarning("No constraintBaseTiles found for " + gameObject.name);
            return;
        }

        // Go through each constraintBaseTile and update the constraints
        foreach (Tile constraintTile in constraintBaseTiles)
        {
            // Check if the constraintTile is null
            if (constraintTile == null)
            {
                Debug.LogWarning("Null constraintTile found for " + gameObject.name);
                continue;
            }

            // Update the constraints for each direction
            UpdateConstraints(constraintTile.upConstraints, ref upConstraints);
            UpdateConstraints(constraintTile.rightConstraints, ref rightConstraints);
            UpdateConstraints(constraintTile.downConstraints, ref downConstraints);
            UpdateConstraints(constraintTile.leftConstraints, ref leftConstraints);
        }
        RemoveConstraintsFromNeighbors();

    }

    // Update the constraints based on the constraintTile's constraints and the current constraints
    private void UpdateConstraints(Tile[] constraintTileConstraints, ref Tile[] myConstraints)
    {
        // Check if the constraintTileConstraints is null
        if (constraintTileConstraints == null)
        {
            Debug.LogWarning("Null constraintTileConstraints found for " + gameObject.name);
            return;
        }

        // Check if the myConstraints is null
        if (myConstraints == null)
        {
            myConstraints = new Tile[0];
        }

        // Create a list to store the updated constraints
        List<Tile> updatedConstraints = myConstraints.ToList();

        // Go through each constraint in the constraintTileConstraints
        foreach (Tile constraint in constraintTileConstraints)
        {
            // Check if the constraint is null
            if (constraint == null)
            {
                Debug.LogWarning("Null constraint found for " + gameObject.name);
                continue;
            }

            // Check if the constraint is not already in the updatedConstraints
            if (!updatedConstraints.Contains(constraint))
            {
                updatedConstraints.Add(constraint);
            }
        }

        // Update the myConstraints array with the updatedConstraints
        myConstraints = updatedConstraints.ToArray();
    }



    // Remove all constraint tiles in upConstraints/rightConstraints from neighbor tile arrays
    public void RemoveConstraintsFromNeighbors()
    {
        RemoveConstraintsFromNeighbors(upConstraints, 0);
        RemoveConstraintsFromNeighbors(rightConstraints, 1);
        RemoveConstraintsFromNeighbors(downConstraints, 2);
        RemoveConstraintsFromNeighbors(leftConstraints, 3);
    }

    // Remove all constraint tiles in constraints from neighbor tiles
    private void RemoveConstraintsFromNeighbors(Tile[] constraints, int dir4way)
    {
        if (constraints == null) return;
        foreach (Tile constraintTile in constraints)
        {
            if (constraintTile == null) continue;

            // Remove the constraint from the neighbor's neighbor array
            RemoveTileFromArray(constraintTile, dir4way);
        }
    }

    [ContextMenu("ClearConstraints")]
    public void ClearConstraints()
    {
        upConstraints = new Tile[0];
        rightConstraints = new Tile[0];
        downConstraints = new Tile[0];
        leftConstraints = new Tile[0];
    }

    [ContextMenu("ClearAll")]
    public void ClearAll()
    {
        upNeighbours = new Tile[0];
        rightNeighbours = new Tile[0];
        downNeighbours = new Tile[0];
        leftNeighbours = new Tile[0];
        upConstraints = new Tile[0];
        rightConstraints = new Tile[0];
        downConstraints = new Tile[0];
        leftConstraints = new Tile[0];
    }






}
