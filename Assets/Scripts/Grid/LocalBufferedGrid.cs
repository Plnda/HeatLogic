using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public partial class LocalBufferedGrid<T> where T: struct
{
    public readonly int width;
    public readonly int height;
    public readonly float cellSize;
    public readonly int scale;
    public readonly Vector3 origin;
    
    private int _readIndex = 0;
    private int _writeIndex => 1 ^ _readIndex;

    private T[,,] _gridData;
    private TextMesh[,] _meshes;

    public delegate T OnGridCellCreation(int x, int y);

    public LocalBufferedGrid(int gridWidth, int gridHeight, float gridScale, float gridCellSize, Vector3 gridOrigin)
    {
        int scaleFactor = (int) (gridCellSize / gridScale);
        scale = scaleFactor;
        
        width = gridWidth * scaleFactor;
        height = gridHeight * scaleFactor;
        origin = gridOrigin;
        cellSize = gridCellSize / scaleFactor;
        
        _gridData = new T[2, width, height];
    }
    
    public List<Vector2Int> GetNeighbours(int x, int y, int radius = 1)
    { 
        var minRow = Mathf.Max(x - radius, _gridData.GetLowerBound(1));
        var maxRow = Mathf.Min((x + radius), _gridData.GetUpperBound(1));
        
        var minCol = Mathf.Max(y - radius, _gridData.GetLowerBound(2));
        var maxCol = Mathf.Min(y + radius, _gridData.GetUpperBound(2));
        
        var results = new List<Vector2Int>();
        
        for (var row = minRow; row <= maxRow; row++)
        { 
            for (var col = minCol; col <= maxCol; col++)
            { 
                if (row == x && col == y)
                { 
                    continue;
                }
                
                results.Add(new Vector2Int(row, col));
            }
        }

        return results;
    }
    
    public void FlipBuffers()
    {
        _readIndex = _writeIndex;
        
        Debug.Log($"{_readIndex}|{_writeIndex}");
    }
    
    public void Initialize(OnGridCellCreation creationBlock)
    {
        var parent = new GameObject("Debug Text Parent");
        _meshes = new TextMesh[width, height];

        for(int x = 0; x < _gridData.GetLength(1); x++)
        {
            for (int z = 0; z < _gridData.GetLength(2); z++)
            {
                var initialData = creationBlock.Invoke(x, z);

                _gridData[_readIndex, x, z] = initialData;
                _gridData[_writeIndex, x, z] = initialData;
                
                var currentPosition = GetWorldPosition(x, z);
                    
                _meshes[x,z] = CreateDebugText($"{x}-{z}", parent.transform, currentPosition + new Vector3(cellSize, 0, cellSize) * .5f, Color.red);

                Debug.DrawLine(currentPosition, GetWorldPosition(x, z + 1), Color.red, 100);
                Debug.DrawLine(currentPosition, GetWorldPosition(x + 1, z),  Color.red, 100);
            }
        }
        
        Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height),  Color.red, 100);
        Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height),  Color.red, 100);
    }
}

/// <summary>
/// Position Logic
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class LocalBufferedGrid<T> {
    
    /// <summary>
    /// Get world position based on grid position
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public Vector3 GetWorldPosition(int x, int z)
    {
        return new Vector3(x, 0, z) * cellSize + origin;
    }

    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition - origin).x / cellSize);
        int z = Mathf.FloorToInt((worldPosition - origin).z / cellSize);

        return new Vector2Int(x, z);
    }

    /// <summary>
    /// Get the grid position based on screen position
    /// </summary>
    /// <param name="screenPosition"></param>
    /// <param name="worldCamera"></param>
    /// <returns></returns>
    public Vector2Int ScreenToGrid(Vector3 screenPosition, Camera worldCamera)
    {
        Ray ray = worldCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit raycastHit))
        {
            return GetGridPosition(raycastHit.point);
        }

        return new Vector2Int(-999,-999);
    }
    
    /// <summary>
    /// Checks if we are currently in bound
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public bool InBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }
}

/// <summary>
/// Data Management
/// </summary>
public partial class LocalBufferedGrid<T>
{
    /// <summary>
    /// Writes directly to the read buffer
    /// </summary>
    /// <param name="gridPosition"></param>
    /// <param name="data"></param>
    public void SetReadBuffer(int x, int y, T data)
    {
        if (InBounds(x, y))
        {
            _gridData[_readIndex, x, y] = data;
        }
    }

    public void SetData(int x, int y, T data)
    {
        if (!InBounds(x, y))
        {
            return;
        }
        
        _gridData[_writeIndex, x, y] = data;
    }
    
    public T GetData(int x, int y)
    {
        if (!InBounds(x, y))
        {
            return default;
        }
        
        return _gridData[_readIndex, x, y];
    }
}

/// <summary>
/// Debug logic
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class LocalBufferedGrid<T>
{
    public void UpdateText(int x, int y, string text)
    {
        var textMesh = _meshes[x, y];
        textMesh.text = text;
    }
    
    private TextMesh CreateDebugText(string text, Transform parent, Vector3 position, Color color)
    {
        GameObject obj = new GameObject($"Grid_Debug_text_{(int) position.x}-{(int) position.z}", typeof(TextMesh));
        obj.transform.SetParent(parent, false);
        obj.transform.rotation = Quaternion.Euler(90,0,0);
        obj.transform.localPosition = position;
        
        var mesh = obj.GetComponent<TextMesh>();
        mesh.text = text;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.characterSize = 0.08f;
        mesh.fontSize = 14;
        mesh.color = color;

        return mesh;
    }
}
