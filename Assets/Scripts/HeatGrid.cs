using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatGrid : MonoBehaviour
{
    public struct HeatData
    {
        public double temperature;
    }

    private LocalBufferedGrid<HeatData> _grid = new LocalBufferedGrid<HeatData>(2, 2, 0.25f, 1, Vector3.zero);

    // Start is called before the first frame update
    void Start()
    {
        
        _grid.Initialize((x, y) => new HeatData()
        {
            temperature = 21
        });
    }

    private double HeatValueForNeighbour(HeatData neighbourData, HeatData data)
    {
        var temperatureDifference = data.temperature - neighbourData.temperature;
        // Depending on your conventions here, an average might be appropriate,
        // rather than a sum.
        return temperatureDifference * ((0.25 + 0.25) / 2); //50% transfer rate
    }

    private void TransferHeat()
    {
        for (int x = 0; x < _grid.width; x++)
        {
            for (int y = 0; y < _grid.height; y++)
            {
                // Implicitly copy since we made our type a struct.
                var data = _grid.GetData(x, y);

                var neighbours = _grid.GetNeighbours(x, y);

                foreach (var neighbour in neighbours)
                {
                    // Also implicitly copy since we made our type a struct.
                    var neighbourData = _grid.GetData(neighbour.x, neighbour.y);
                   
                    var difference = HeatValueForNeighbour(neighbourData, data);

                    data.temperature = data.temperature - difference;
                    // We don't update the neighbour's data here - we'll visit them
                    // individually at some point in our loop.
                }

                // Write our new data to the write-only buffer.
                _grid.SetData(x, y, data);
            }
        }

        // Done writing. Make our write-only buffer the new read-only version.
        // We can do this by flipping one bit, without copying the whole array over.
        _grid.FlipBuffers();
    }

    private void ApplyHeat(int x, int y)
    {
        var data = _grid.GetData(x, y);
        data.temperature += 21; // Double the temperature
        
        _grid.SetReadBuffer(x, y, data);
    }

    private void UpdateGrid()
    {
        for (int x = 0; x < _grid.width; x++)
        {
            for (int y = 0; y < _grid.height; y++)
            {
                var data = _grid.GetData(x, y);
                _grid.UpdateText(x,y, $"{data.temperature:n0}");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            var position = _grid.ScreenToGrid(Input.mousePosition, Camera.main);
            ApplyHeat(position.x, position.y);
;       }
        
        TransferHeat();
        UpdateGrid();
    }
}
