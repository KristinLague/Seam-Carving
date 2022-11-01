using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SobelFilter : MonoBehaviour
{
    public MeshRenderer imageRenderer;
    public Texture2D image;
    public Texture2D appliedTex;
    public float[,] edginess;
    public float[,] pixelEnergy;
    public bool Carve;

    void Start()
    {
        ApplySobel();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            CarveSeam();
        }
    }

    void ApplySobel()
    {
        edginess = new float[image.width, image.height];
        //Looping through every pixel of the texture
        for (int x = 0; x < image.width; x++)
        {
            for (int y = 0; y < image.height; y++)
            {
                Color averageColorHorizontal = GetAverageSobelColorHorizontal(x, y);
                Color averageColorVertical = GetAverageSobelColorVertical(x, y);

                Vector3 lumV = new Vector3(averageColorHorizontal.r, averageColorHorizontal.g, averageColorHorizontal.b);
                Vector3 lumH = new Vector3(averageColorVertical.r, averageColorVertical.g, averageColorVertical.b);

                float luminance = Mathf.Sqrt(Vector3.Dot(lumH,lumH) + Vector3.Dot(lumV, lumV));

                edginess[x, y] = luminance;
            }
        }

        appliedTex = new Texture2D(image.width, image.height);
        GetPixelEnergy();
        var bestSeamToCarve = GetSeamToCarve();
        
        for (int x = 0; x < image.width; x++)
        {
            for (int y = 0; y < image.height; y++)
            {
                float luminance = edginess[x, y];
                //Color newCol = new Color(luminance, luminance, luminance, 1);
                appliedTex.SetPixel(x,y,image.GetPixel(x,y));
            }
        }

        foreach (var pixel in bestSeamToCarve)
        {
            appliedTex.SetPixel(pixel.x,pixel.y,Color.magenta);
        }
        
        appliedTex.Apply();
        imageRenderer.material.mainTexture = appliedTex;
    }

    private void CarveSeam()
    {
        Debug.Log("CALLED");
        Texture2D adjustedTexture = new Texture2D(appliedTex.width - 1, appliedTex.height);
        var bestSeamToCarve = GetSeamToCarve();
        bestSeamToCarve.Reverse();
        
        for (int y = 0; y < appliedTex.height; y++)
        {
            var pixelToCut = bestSeamToCarve[y];
            int newX = 0;
            
            for (int x = 0; x < appliedTex.width; x++)
            {
                if (x != pixelToCut.x)
                {
                    adjustedTexture.SetPixel(newX,y,appliedTex.GetPixel(x,y));
                    newX++;
                }
            }
        }
        
        adjustedTexture.Apply();
        appliedTex = adjustedTexture;
        imageRenderer.gameObject.transform.localScale = new Vector3(appliedTex.width / 100f, appliedTex.height/100f, 1f);
        imageRenderer.material.mainTexture = appliedTex;
        image = adjustedTexture;
        ApplySobel();
        Carve = false;
    }

    private Color GetAverageSobelColorHorizontal(int x, int y)
    {
        var gxXa = (x + 1 >= image.width || y + 1 >= image.height) ? Color.clear : image.GetPixel(x + 1, y + 1); 
        var gxXb = (x - 1 < 0 || y + 1 >= image.height) ? Color.clear : image.GetPixel(x - 1, y + 1);
        var gxX = gxXa - gxXb;


        var gxYa = (x + 1 >= image.width) ? Color.clear : image.GetPixel(x + 1, y);
        var gxYb = (x - 1 < 0) ? Color.clear : image.GetPixel(x - 1, y);
        var gxY = 2 * (gxYa - gxYb);

        var gxZa = (x + 1 >= image.width || y - 1 < 0) ? Color.clear : image.GetPixel(x + 1, y - 1);
        var gxZb = (x - 1 < 0 || y - 1 < 0) ? Color.clear : image.GetPixel(x - 1, y - 1);
        var gxZ = gxZa - gxZb;

        return (gxX + gxY + gxZ);
    }

    private Color GetAverageSobelColorVertical(int x, int y)
    {
        var gyXa = (x - 1 < 0 || y - 1 < 0) ? Color.clear : image.GetPixel(x - 1, y - 1);
        var gyXb = (x - 1 < 0 || y + 1 >= image.height) ? Color.clear : image.GetPixel(x - 1, y + 1);
        var gyX = gyXb - gyXa;

        var gyYa = (y - 1 < 0 ) ? Color.clear : image.GetPixel(x, y - 1);
        var gyYb = (y + 1 >= image.height) ? Color.clear : image.GetPixel(x, y + 1);
        var gyY = 2 * (gyYb - gyYa);

        var gyZa = (x + 1 >= image.width || y - 1 < 0) ? Color.clear : image.GetPixel(x + 1, y - 1);
        var gyZb = (x + 1 >= image.width || y + 1 >= image.height) ? Color.clear : image.GetPixel(x + 1, y + 1);
        var gyZ = gyZb - gyZa;

        return (gyX + gyY + gyZ);
    }

    private void GetPixelEnergy()
    {
        pixelEnergy = new float[appliedTex.width, appliedTex.height];
        for (int y = 0; y < appliedTex.height; y++)
        {
            for (int x = 0; x < appliedTex.width; x++)
            {
                if (y == 0)
                {
                    pixelEnergy[x, y] = edginess[x, y];
                }
                else
                {
                    pixelEnergy[x, y] = GetLowestEnergyForPixel(x, y);
                }
            }
        }
    }

    private float GetLowestEnergyForPixel(int x, int y)
    {
        float[] paths = new float[3];
        paths[0] = x - 1 < 0 ? 1 : pixelEnergy[x - 1, y - 1];
        paths[1] = pixelEnergy[x, y - 1];
        paths[2] = x + 1 >= appliedTex.width ? 1 : pixelEnergy[x + 1, y - 1];
        
        var result = edginess[x,y] + Mathf.Min(paths);
        //Debug.Log($"Position {x},{y} -> {result}");
        return result;
    }

    private List<Vector2Int> GetSeamToCarve()
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int startPos = new Vector2Int(0, appliedTex.height - 1);
        for (int x = 0; x < appliedTex.width; x++)
        {
            if (pixelEnergy[x, appliedTex.height - 1] < pixelEnergy[startPos.x, startPos.y])
                startPos.x = x;
        }
        path.Add(startPos);
        
        for (int y = appliedTex.height - 1; y > 0; y--)
        {
            Vector2Int nextStep = GetLowestEnergyPixelBelow(path[path.Count - 1].x, y);
            path.Add(nextStep);
        }

        Debug.Log(path.Count);
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
        var eRight = x + 1 >= appliedTex.width ? 1 : pixelEnergy[x + 1, y - 1];
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
