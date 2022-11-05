using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SobelFilter : MonoBehaviour
{
    public enum DisplayMode {Image, Edginess, Energy}
    public DisplayMode mode;
    
    public MeshRenderer originalDisplay;
    public MeshRenderer processedDisplay;
    public Texture2D original;
    
    Texture2D processedTex;
    private Texture2D energyVis;
    private Texture2D edgyVis;
    float[,] edginess;
    float[,] pixelEnergy;
    private Vector2Int[] bestSeam;
    private float maxEnergy;
    
    void Start()
    {
        originalDisplay.material.mainTexture = original;
        
        processedTex = new Texture2D(original.width, original.height);
        processedTex.filterMode = FilterMode.Point;
        processedTex.SetPixels(original.GetPixels());
        processedTex.Apply();
        
        Recalculate();
        UpdateVis();
        UpdateImageSize();
    }

    void Recalculate()
    {
        CalculateEdginess();
        CalculateEnergy();
        bestSeam = GetSeamToCarve().ToArray();
    }

    void UpdateVis()
    {
        energyVis = VisualizeEnergy();
        edgyVis = VisualizeEdginess();
        ShowSeam(bestSeam);
    }

    void UpdateImageSize()
    {
        originalDisplay.transform.localScale = new Vector3(1, original.height / (float) original.width);
        processedDisplay.transform.localScale = new Vector3(processedTex.width/(float)original.width, original.height / (float) original.width);

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Carve");
            CarveSeam(bestSeam);
            Recalculate();
            UpdateVis();
        }

        switch (mode)
        {
            case DisplayMode.Image:  processedDisplay.sharedMaterial.mainTexture = processedTex;
                break;
            case DisplayMode.Edginess: processedDisplay.sharedMaterial.mainTexture = edgyVis;
                break;
            case DisplayMode.Energy:  processedDisplay.sharedMaterial.mainTexture = energyVis;
                break;
        }
    }

    void CalculateEdginess()
    {
        edginess = new float[processedTex.width, processedTex.height];
        //Looping through every pixel of the texture
        for (int x = 0; x < processedTex.width; x++)
        {
            for (int y = 0; y < processedTex.height; y++)
            {
                Color averageColorHorizontal = GetAverageSobelColorHorizontal(x, y);
                Color averageColorVertical = GetAverageSobelColorVertical(x, y);

                Vector3 lumV = new Vector3(averageColorHorizontal.r, averageColorHorizontal.g, averageColorHorizontal.b);
                Vector3 lumH = new Vector3(averageColorVertical.r, averageColorVertical.g, averageColorVertical.b);

                float luminance = Mathf.Sqrt(Vector3.Dot(lumH,lumH) + Vector3.Dot(lumV, lumV));

                edginess[x, y] = luminance;
            }
        }
    }

    void ShowSeam(Vector2Int[] seamPoints)
    {
        foreach (var point in seamPoints)
        {
            processedTex.SetPixel(point.x, point.y, Color.magenta);
        }
        processedTex.Apply();
    }
    

    private void CarveSeam(Vector2Int[] seamPoints)
    {
       
        Texture2D adjustedTexture = new Texture2D(processedTex.width - 1, processedTex.height);

        for (int y = 0; y < processedTex.height; y++)
        {
            var pixelToCut = seamPoints[seamPoints.Length-1-y];
            int newX = 0;
            
            for (int x = 0; x < processedTex.width; x++)
            {
                if (x != pixelToCut.x)
                {
                    adjustedTexture.SetPixel(newX,y,processedTex.GetPixel(x,y));
                    newX++;
                }
            }
        }
        
        adjustedTexture.Apply();
        processedTex = adjustedTexture;
        processedDisplay.material.mainTexture = processedTex;
        UpdateImageSize();
    }

    private Color GetAverageSobelColorHorizontal(int x, int y)
    {
        var gxXa = (x + 1 > processedTex.width || y + 1 > processedTex.height) ? Color.clear : processedTex.GetPixel(x + 1, y + 1); 
        var gxXb = (x - 1 < 0 || y + 1 > processedTex.height) ? Color.clear : processedTex.GetPixel(x - 1, y + 1);
        var gxX = gxXa - gxXb;


        var gxYa = (x + 1 > processedTex.width) ? Color.clear : processedTex.GetPixel(x + 1, y);
        var gxYb = (x - 1 < 0) ? Color.clear : processedTex.GetPixel(x - 1, y);
        var gxY = 2 * (gxYa - gxYb);

        var gxZa = (x + 1 > processedTex.width || y - 1 < 0) ? Color.clear : processedTex.GetPixel(x + 1, y - 1);
        var gxZb = (x - 1 < 0 || y - 1 < 0) ? Color.clear : processedTex.GetPixel(x - 1, y - 1);
        var gxZ = gxZa - gxZb;

        return (gxX + gxY + gxZ);
    }

    private Color GetAverageSobelColorVertical(int x, int y)
    {
        var gyXa = (x - 1 < 0 || y - 1 < 0) ? Color.clear : processedTex.GetPixel(x - 1, y - 1);
        var gyXb = (x - 1 < 0 || y + 1 > processedTex.height) ? Color.clear : processedTex.GetPixel(x - 1, y + 1);
        var gyX = gyXb - gyXa;

        var gyYa = (y - 1 < 0 ) ? Color.clear : processedTex.GetPixel(x, y - 1);
        var gyYb = (y + 1 > processedTex.height) ? Color.clear : processedTex.GetPixel(x, y + 1);
        var gyY = 2 * (gyYb - gyYa);

        var gyZa = (x + 1 > processedTex.width || y - 1 < 0) ? Color.clear : processedTex.GetPixel(x + 1, y - 1);
        var gyZb = (x + 1 > processedTex.width || y + 1 > processedTex.height) ? Color.clear : processedTex.GetPixel(x + 1, y + 1);
        var gyZ = gyZb - gyZa;

        return (gyX + gyY + gyZ);
    }

    private void CalculateEnergy()
    {
        pixelEnergy = new float[processedTex.width, processedTex.height];
        maxEnergy = 0;
        
        for (int y = 0; y < processedTex.height; y++)
        {
            for (int x = 0; x < processedTex.width; x++)
            {
                if (y == 0)
                {
                    pixelEnergy[x, y] = edginess[x, y];
                }
                else
                {
                    pixelEnergy[x, y] = GetLowestEnergyForPixel(x, y);
                }

                maxEnergy = Mathf.Max(maxEnergy, pixelEnergy[x, y]);
            }
        }
    }

    Texture2D VisualizeEnergy()
    {
        Texture2D energyVis = new Texture2D(processedTex.width, processedTex.height);
        energyVis.filterMode = FilterMode.Point;
        for (int y = 0; y < energyVis.height; y++)
        {
            for (int x = 0; x < energyVis.width; x++)
            {
                float energyT = pixelEnergy[x, y] / maxEnergy;
                energyVis.SetPixel(x,y, new Color(energyT, energyT, energyT));
            }
        }
        energyVis.Apply();
        return energyVis;
    }
    
    Texture2D VisualizeEdginess()
    {
        Texture2D edgyVis = new Texture2D(processedTex.width, processedTex.height);
        edgyVis.filterMode = FilterMode.Point;
        for (int y = 0; y < edgyVis.height; y++)
        {
            for (int x = 0; x < edgyVis.width; x++)
            {
                float edginess = this.edginess[x, y];
                edgyVis.SetPixel(x,y, new Color(edginess, edginess, edginess));
            }
        }
        edgyVis.Apply();
        return edgyVis;
    }

    private float GetLowestEnergyForPixel(int x, int y)
    {
        float[] paths = new float[3];
        paths[0] = x - 1 < 0 ? float.MaxValue : pixelEnergy[x - 1, y - 1];
        paths[1] = pixelEnergy[x, y - 1];
        paths[2] = x + 1 >= processedTex.width ? float.MaxValue : pixelEnergy[x + 1, y - 1];
        
        var result = edginess[x,y] + Mathf.Min(paths);
        //Debug.Log($"Position {x},{y} -> {result}");
        return result;
    }

    private List<Vector2Int> GetSeamToCarve()
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int startPos = new Vector2Int(0, processedTex.height - 1);
        for (int x = 0; x < processedTex.width; x++)
        {
            if (pixelEnergy[x, processedTex.height - 1] < pixelEnergy[startPos.x, startPos.y])
                startPos.x = x;
        }
        path.Add(startPos);
        
        for (int y = processedTex.height - 1; y > 0; y--)
        {
            Vector2Int nextStep = GetLowestEnergyPixelBelow(path[path.Count - 1].x, y);
            path.Add(nextStep);
        }
        
        return path;
    }

    private Vector2Int GetLowestEnergyPixelBelow(int x, int y)
    {
        var eLeft = x - 1 < 0 ? 1 : pixelEnergy[x - 1, y - 1];
        if (x < 0 || x >= pixelEnergy.GetLength(0))
        {
            Debug.Log("X out of bounds: " + x + "   " + pixelEnergy.GetLength(0));
        }
        if (y < 0 || y >= pixelEnergy.GetLength(1))
        {
            Debug.Log("Y out of bounds: " + y + "   " + pixelEnergy.GetLength(1));
        }
        var eCenter = pixelEnergy[x, y - 1];
        var eRight = x + 1 >= processedTex.width ? 1 : pixelEnergy[x + 1, y - 1];
        int pathX = 0;
        if (eLeft < eCenter)
        {
            if (eLeft < eRight)
            {
                pathX = x - 1;
                //return new Vector2Int(x - 1, y - 1);
            }
            else
            {
                pathX = x + 1;
                // return new Vector2Int(x + 1, y - 1);
            }
        }
        else
        {
            if (eCenter < eRight)
            {
                pathX = x;
               // return new Vector2Int(x, y - 1);
            }
            else
            {
                pathX = x + 1;
              //  return new Vector2Int(x + 1, y - 1);
            }
        }

        pathX = Mathf.Clamp(pathX, 0, pixelEnergy.GetLength(0)-1);
        return new Vector2Int(pathX, y - 1);
    }
    
}
