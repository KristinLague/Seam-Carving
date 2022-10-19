using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SobelFilter : MonoBehaviour
{
    public MeshRenderer imageRenderer;
    public Texture2D image;
    public Texture2D appliedTex;
    private float[,] edginess;

    void Start()
    {
        ApplySobel();
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

                float lumV = GetLuminance(averageColorVertical);
                float lumH = GetLuminance(averageColorHorizontal);

                var luminance = Mathf.Sqrt(lumH * lumH + lumV * lumV);

                edginess[x, y] = luminance;
            }
        }

        appliedTex = new Texture2D(image.width, image.height);
        for (int x = 0; x < image.width; x++)
        {
            for (int y = 0; y < image.height; y++)
            {
                float luminance = edginess[x, y];
                Color newCol = new Color(luminance, luminance, luminance, 1);
                appliedTex.SetPixel(x,y,newCol);
            }
        }
        
        appliedTex.Apply();
        imageRenderer.material.mainTexture = appliedTex;
    }

    private Color GetAverageSobelColorHorizontal(int x, int y)
    {
        var gxXa = (x + 1 > image.width || y + 1 > image.height) ? Color.clear : image.GetPixel(x + 1, y + 1); 
        var gxXb = (x - 1 < 0 || y + 1 > image.height) ? Color.clear : image.GetPixel(x - 1, y + 1);
        var gxX = gxXa - gxXb;


        var gxYa = (x + 1 > image.width) ? Color.clear : image.GetPixel(x + 1, y);
        var gxYb = (x - 1 < 0) ? Color.clear : image.GetPixel(x - 1, y);
        var gxY = 2 * (gxYa - gxYb);

        var gxZa = (x + 1 > image.width || y - 1 < 0) ? Color.clear : image.GetPixel(x + 1, y - 1);
        var gxZb = (x - 1 < 0 || y - 1 < 0) ? Color.clear : image.GetPixel(x - 1, y - 1);
        var gxZ = gxZa - gxZb;

        return (gxX + gxY + gxZ) / 3f;
    }

    private Color GetAverageSobelColorVertical(int x, int y)
    {
        var gyXa = (x - 1 < 0 || y - 1 < 0) ? Color.clear : image.GetPixel(x - 1, y - 1);
        var gyXb = (x - 1 < 0 || y + 1 > image.height) ? Color.clear : image.GetPixel(x - 1, y + 1);
        var gyX = gyXb - gyXa;

        var gyYa = (y - 1 < 0 ) ? Color.clear : image.GetPixel(x, y - 1);
        var gyYb = (y + 1 > image.height) ? Color.clear : image.GetPixel(x, y + 1);
        var gyY = 2 * (gyYb - gyYa);

        var gyZa = (x + 1 > image.width || y - 1 < 0) ? Color.clear : image.GetPixel(x + 1, y - 1);
        var gyZb = (x + 1 > image.width || y + 1 > image.height) ? Color.clear : image.GetPixel(x + 1, y + 1);
        var gyZ = gyZb - gyZa;

        return (gyX + gyY + gyZ) / 3f;
    }

    private float GetLuminance(Color color)
    {
        return (float) (0.299 * color.r + 0.587 * color.g + 0.114 * color.b);
    }


}
