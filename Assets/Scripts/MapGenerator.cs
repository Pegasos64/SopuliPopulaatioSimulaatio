using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class MapGenerator : MonoBehaviour
{

    public int xDimension, yDimension;
    public int Generations;

    public string fileName = "Matrices.txt";

    public Color Alive, Dead;

    [Range(0.000000001f, 2)]
    public float tPerGeneration = 0.1f;
    public bool fullSpeed = false;

    private MeshRenderer mr;
    public Material mat;

    private int currentGen = 0;
    private int populationNumber = 0;
    private float lastTick = 0;
    private int[,] matrix, lastMatrix;

    public bool destroyOnEdge = false;
    public bool detectStale = false;

    private bool staleMatrix = false;
    public Text OutputText;
    private int deaths, births, changes;
    private int t_deaths, t_births, t_changes;
    private int[,] matrix1, matrix2, matrix3, matrix4, matrix5, matrix6, matrix7, matrix8, matrix9;

    private int[,] LongestMatrix;
    private int[,] ShortestMatrix;
    private int lowestGeneration;
    private int highestGeneration;
    private int totalGenerations;
    private float averageGeneration;

    public Button SaveButton;
    private Text sButtonText;
    private bool overWritePrompt = false;


    // Use this for initialization
    void Start()
    {
        mr = this.GetComponent<MeshRenderer>();

        matrix = LuoAlkuperainenMatriisi(xDimension, yDimension);

        Application.runInBackground = true;

        LongestMatrix = matrix;
        highestGeneration = int.MinValue;
        lowestGeneration = int.MaxValue;
        totalGenerations = 0;

        mat.mainTexture = new Texture2D(xDimension, yDimension);
        mat.mainTexture = TextureFromMatrix(matrix, (Texture2D)mat.mainTexture);
        lastTick = Time.time;
        lastMatrix = matrix;

        changes = 0;
        deaths = 0;
        births = 0;

        t_births = 0;
        t_deaths = 0;
        t_changes = 0;

        sButtonText = SaveButton.GetComponentInChildren<Text>();

    }

    // Update is called once per frame
    void Update()
    {
        
        if ((Time.time - lastTick >= tPerGeneration || fullSpeed) && currentGen <= Generations && !staleMatrix)
        {
            int[,] newMatrix = SelviytymisTaistelu(lastMatrix, out changes, out deaths, out births);
            mat.mainTexture = TextureFromMatrix(newMatrix, (Texture2D)mat.mainTexture);
            lastTick = Time.time;

            if (detectStale)
            {
                staleMatrix = CheckStaleness(newMatrix);
            }

            lastMatrix = newMatrix;
            currentGen++;
            OutputText.text = "Population "+(populationNumber+1) +" Generation : " + currentGen + "\n\nChanges : "+ changes + "\nBirths : "+births + "\nDeaths : "+ deaths;
            t_births += births;
            t_deaths += deaths;
            t_changes += changes;
            OutputText.text = OutputText.text + "\n\nTotal:\nChanges : " + t_changes + "\nBirths : " + t_births + "\nDeaths : " + t_deaths;
            OutputText.text += "\n\nHighest generation reached: " + highestGeneration;
            OutputText.text += "\nLowest generation reached: " + lowestGeneration;
            OutputText.text += "\nAverage generation : " + averageGeneration;

        }else if (staleMatrix || currentGen >= Generations)
        {
            if(currentGen > highestGeneration)
            {
                LongestMatrix = matrix;
                highestGeneration = currentGen;
            }
            if(currentGen < lowestGeneration)
            {
                ShortestMatrix = matrix;
                lowestGeneration = currentGen;
            }

            matrix = LuoAlkuperainenMatriisi(xDimension, yDimension);
            lastMatrix = matrix;
            staleMatrix = false;

            totalGenerations += currentGen;
            averageGeneration = totalGenerations / (populationNumber+1);

            currentGen = 0;

            t_births = 0;
            t_deaths = 0;
            t_changes = 0;

            populationNumber++;
        }
    }

    public bool CheckStaleness(int[,] currentMatrix)
    {
        //Siirretään muistissa olevia matriiseja yhdellä eteenpäin, ja poistetaan vanhin
        matrix9 = matrix8;
        matrix8 = matrix7;
        matrix7 = matrix6;
        matrix6 = matrix5;
        matrix5 = matrix4;
        matrix4 = matrix3;
        matrix3 = matrix2;
        matrix2 = matrix1;
        matrix1 = lastMatrix;

        //Lisätään matriisit listaan, jotta niitä voidaan käydä läpi tehokkaasti
        List<int[,]> Matrices = new List<int[,]>();
        Matrices.Add(matrix1);
        Matrices.Add(matrix2);
        Matrices.Add(matrix3);
        Matrices.Add(matrix4);
        Matrices.Add(matrix5);
        Matrices.Add(matrix6);
        Matrices.Add(matrix7);
        Matrices.Add(matrix8);
        Matrices.Add(matrix9);

        //Tarkistetaank
        foreach(int[,] m in Matrices)
        {
            if (m == null) return false;
        }

        bool[] sameAs = { true, true, true, true, true, true, true, true, true };
        for (int i = 0; i < currentMatrix.GetLength(0); i++)
        {
            for (int j = 0; j < currentMatrix.GetLength(1); j++)
            {
                int value = currentMatrix[i, j];
                for(int x = 0; x<Matrices.Count; x++)
                {
                    if (value != Matrices[x][i, j])
                    {
                        sameAs[x] = false;
                    }
                }
            }
        }

        foreach(bool val in sameAs)
        {
            if (val) return true;
        }
        return false;
    } 

    public Texture2D TextureFromMatrix(int[,] matrix, Texture2D oldTexture)
    {
        int width = matrix.GetLength(0);
        int height = matrix.GetLength(1);

        oldTexture.filterMode = FilterMode.Point;
        oldTexture.wrapMode = TextureWrapMode.Clamp;

        Color[] colormap = new Color[width * height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                colormap[y * width + x] = Color.Lerp(Alive, Dead, matrix[x, y]);
            }
        }

        oldTexture.SetPixels(colormap);
        oldTexture.Apply();
        return oldTexture;

    }
    #region Selviytymistaistelu
    public void MatriisinKasittely(int[,] matriisi, int sukupolvet)
    {
        // TULOSTUS VAIHE
        int muutoksia = 0;
        int syntymia = 0;
        int kuolemia = 0;

        int[,] edellinenGeneraatio = matriisi;
        for (int sukupolvi = 0; sukupolvi < sukupolvet; sukupolvi++)
        {
            int[,] lopputulos = SelviytymisTaistelu(edellinenGeneraatio, out muutoksia, out kuolemia, out syntymia);
            edellinenGeneraatio = lopputulos;
        }
    }

    private int[,] SelviytymisTaistelu(int[,] alkuperainen, out int muutoksia, out int kuolemia, out int syntymia)
    {
        int firstDimensionLength = alkuperainen.GetLength(0);
        int secondDimensionLength = alkuperainen.GetLength(1);

        muutoksia = 0;
        kuolemia = 0;
        syntymia = 0;

        int[,] tulos = new int[firstDimensionLength, secondDimensionLength];

        for (int i = 0; i < firstDimensionLength; i++)
        {
            for (int j = 0; j < secondDimensionLength; j++)
            {
                tulos[i, j] = LaskeTulos(i, j, alkuperainen);
                if (tulos[i, j] != alkuperainen[i, j])
                {
                    muutoksia += 1;
                    if (tulos[i, j] == 1)
                    {
                        syntymia += 1;
                    }
                    else
                    {
                        kuolemia += 1;
                    }
                    //Console.WriteLine("Muutos: [" + i + "," + j + "]" + "  nro : " + muutoksia);
                }
            }
        }
        return tulos;
    }

    public int LaskeTulos(int i, int j, int[,] matrix)
    {
        bool N, NE, E, SE, S, SW, W, NW;
        N = NE = E = SE = S = SW = W = NW = true;
        if (i == 0) //ylin rivi
        {
            N = NE = NW = false;
            if (destroyOnEdge) return 0;
        }
        else if (i == matrix.GetLength(0) - 1) // alin rivi
        {
            S = SE = SW = false;
            if (destroyOnEdge) return 0;

        }
        if (j == 0) // vasen laita
        {
            W = SW = NW = false;
            if (destroyOnEdge) return 0;
        }
        else if (j == matrix.GetLength(1) - 1) // oikea laita
        {
            E = SE = NE = false;
            if (destroyOnEdge) return 0;
        }

        int naapurit = 0;

        if (N) naapurit += matrix[i - 1, j];
        if (NE) naapurit += matrix[i - 1, j + 1];
        if (E) naapurit += matrix[i, j + 1];
        if (SE) naapurit += matrix[i + 1, j + 1];
        if (S) naapurit += matrix[i + 1, j];
        if (SW) naapurit += matrix[i + 1, j - 1];
        if (W) naapurit += matrix[i, j - 1];
        if (NW) naapurit += matrix[i - 1, j - 1];

        switch (matrix[i, j])
        {
            case 1:
                if (naapurit == 2 || naapurit == 3)
                {
                    return 1;
                }
                return 0;
            case 0:
                if (naapurit == 3)
                {
                    return 1;
                }
                return 0;
        }
        return 0;
    }
    #endregion

    #region Apumetodit
    public static string MatriisiStringiksi(int[,] matrix)
    {
        string merkkijono = "\n";
        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                merkkijono += (matrix[i, j] + " ");
            }
            merkkijono += ("\n");
        }
        return merkkijono;
    }
    public static int[,] LuoAlkuperainenMatriisi(int _xDimension, int _yDimension)
    {

        int[,] matriisi = new int[_xDimension, _yDimension];
        for (int i = 0; i < _xDimension; i++)
        {
            for (int j = 0; j < _yDimension; j++)
            {
                matriisi[i, j] = Random.Range(0, 2);
            }
        }
        return matriisi;
    }

    public void Save()
    {
        if (File.Exists(fileName) && !overWritePrompt)
        {
            sButtonText.text = "Overwrite?";
            overWritePrompt = true;
        }else
        {
            sButtonText.text = "Saved!";
            StreamWriter sr = File.CreateText(fileName);

            sr.WriteLine("Random seeded populations, ran on " + System.DateTime.Now);
            sr.WriteLine(populationNumber + " populations in " + Time.time + " seconds");
            sr.WriteLine(Time.time / 60 / 60 + "hours " + Time.time / 60 + "minutes " + Time.time + " seconds");
            sr.WriteLine("Average generations reached : " + averageGeneration);
            sr.WriteLine("Highest generation reached : " + highestGeneration);
            sr.WriteLine("Lowest generation reached : " + lowestGeneration);

            sr.Write("Longest matrix (" + highestGeneration + " generations) : \n");
            sr.Write(MatriisiStringiksi(LongestMatrix));
            sr.Write("\nShortest matrix (" + lowestGeneration + " generations) : \n");
            sr.Write(MatriisiStringiksi(LongestMatrix));

            sr.Close();
        }


    }
    #endregion
}
