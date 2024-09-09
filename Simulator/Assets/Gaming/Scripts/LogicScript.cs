using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using UnityEngine.UI;
using System.IO;

// Logic script to align the "currentPlane" with the "targetPlane" based on position and rotation, while tracking scores and timing

public class LogicScript : MonoBehaviour
{
    // Variables to control the game's logic
    public int playerScore;
    public bool targetComplete = false;
    public float prevTimeCount, timeCount;
    public bool writeCSV = false;

    // UI elements to display relevant information such as player position, score and a timer
    public Text positionText;
    public Text scoreText;
    public Text scoreDisplay;
    public Text timerText;

    // Game objects related to gameplay, lile the planes, the success screen and the reset button
    public GameObject currentPlane;
    public GameObject targetPlane;
    public GameObject gameSpace;
    public GameObject successScreen;
    public GameObject resetButton;

    // Utility variables to for calculation and file writing
    private double score;
    private string distX, distY, distZ;
    StreamWriter writer;
    private float timeCSV;
    public float hitPoint;

    // Reference to SocketClient component, which is handling external communication to the haptic device
    private SocketClient socket;

    // Start is called before the first frame update
    void Start()
    {
        // Initialization of the communication with the haptic device
        socket = GameObject.FindGameObjectWithTag("Client").GetComponent<SocketClient>();

        //writer = new StreamWriter(Application.dataPath + "/Gaming/Experiments/stiffness_50.csv");
        //writer.WriteLine("Final Score" + "," + "x" + "," + "y" + "," + "z" + "," + "belly" + "," + "Time");
        //writer.WriteLine("Final Score" + "," + "Translation Score" + "," + "Rotation Score" + "," + "Translation" + "," + "Rotation");
    }

    // Update is called once per frame
    void Update()
    {
        // show distance to target plane (center point) - relative position between the currentPlane and the targetPlane
        ShowPosition();

        // show time used to reach target plane (time passed since the last target completion)
        timeCount = ShowTimer(prevTimeCount);

        if (targetComplete != true)
        {
            prevTimeCount = timeCount;
            successScreen.SetActive(false);
            // Player's score based on the distance and rotation difference between the currentPlane and the targetPlane
            score = ComputeScore(currentPlane, targetPlane);
        }
        
        // Update scores in the UI
        scoreText.text = "Score: " + (100 * score).ToString("N2");
        scoreDisplay.text = (100 * score).ToString("N2");

        // If the score is higher than 0.80 and the target has not already been marked complete, the success screen and reset button are shown, and the target is marked complete.
        if ((score > 0.80) && (targetComplete != true))
        {
            successScreen.SetActive(true);
            resetButton.SetActive(true);
            targetComplete = true;
        }
    }

    // The following function calculates the player's score by considering both the translation (position difference) and rotation (orientation difference) between the current and target planes.
    private double ComputeScore(GameObject P, GameObject T)
    {
        //Vector3 boundaryCentroid = new Vector3((float)0.9, (float)-0.6, (float)-2.6);

        // calculate translation and rotation from target plane
        //double deltaTranslation = Vector3.Distance(Vector3.Normalize(P.transform.position), Vector3.Normalize(T.transform.position));

        // positional distance between the two planes
        double deltaTranslation = Vector3.Distance(P.transform.position, T.transform.position);
        // rotational difference
        double deltaRotation = ComputeRotation(P, T);

        if (deltaTranslation > 0.6)
        {
            deltaTranslation = 0.6;
        }

        // calculate current score and score normalization
        // The position and rotation differences are normalized to a scale, with larger values being penalized (values exceeding a limit are clamped)
        double scoreTranslation = NormalizeData(deltaTranslation, 0, 0.6);
        double scoreRotation = NormalizeData(deltaRotation, 0, 90);
        double scoreToDisplay = 0.5 * scoreRotation + 0.5 * scoreTranslation;
        //Debug.Log("Translation: " + deltaTranslation);
        //Debug.Log("Rotation: " + deltaRotation);
        //Debug.Log("Translation Score: " + scoreTranslation);
        //Debug.Log("Rotation Score: " + scoreRotation);

        double posTranslation = NormalizeData(deltaTranslation, 0.6, 0);
        if (P.transform.position.z > -2.6)
        {
            posTranslation = -posTranslation;
        }

        // The final score is a weigthed combination of the translation and rotation scores
        Vector3 targetDirection = T.transform.position - P.transform.position;
        double angleRotation = Vector3.SignedAngle(targetDirection, P.transform.up, T.transform.up);
        double rotRotation = -deltaRotation;
        
        if (angleRotation < 0)
        {
            rotRotation = -rotRotation;
        }

        //Debug.DrawRay(P.transform.position, P.transform.up * 3);

        // If writeCSV is true, the score and plane positions are written to a CSV file at every frame
        if (writeCSV)
        {
            timeCSV += Time.deltaTime;
            writer.WriteLine(scoreToDisplay.ToString() + "," + P.transform.position.x.ToString() + "," + P.transform.position.y.ToString() + "," + P.transform.position.z.ToString() + "," + hitPoint.ToString() + ","+ timeCSV.ToString()); ;
            //writer.WriteLine(scoreToDisplay.ToString() + "," + scoreTranslation.ToString() + "," + scoreRotation.ToString() + "," + posTranslation.ToString() + "," + rotRotation.ToString());
            writer.Flush();
            //writer.Close();
        }

        return scoreToDisplay;
    }

    // This function calculates the angular difference between the normal vectors (upward direction) of the two planes using the dot product formula
    private double ComputeRotation(GameObject P, GameObject T)
    {
        // extract normal vector of the plane
        Vector3 normalP = P.transform.up;
        Vector3 normalT = T.transform.up;

        // calculate angle between two normal vectors (dot product)
        double dotProduct = 0;
        for (int idx = 0; idx < 3; idx++)
        {
            dotProduct += normalP[idx] * normalT[idx];
        }
        double angleR = Math.Acos(Math.Round(dotProduct, 5));
        double angleD = angleR * Mathf.Rad2Deg;
        
        // If the angle exceeds 90 degrees, it is adjusted to reflect a mirrored rotation (keeping the angle between 0 and 90 degrees)
        if (angleD > 90)
        {
            angleD = Math.Abs(180 - angleD);
        }

        return angleD;
    }

    // This function takes raw data (e.g., distance or angle) and normalizes it within a specific range (e.g., between 0 and 1)
    private double NormalizeData(double rawData, double rangeMax, double rangeMin)
    {
        double normData = (rawData - rangeMin) / (rangeMax - rangeMin);

        return normData;
    }

    // This function increments and formats the timeToDisplay variable and updates the timer in the UI
    private float ShowTimer(float timeToDisplay)
    {
        timeToDisplay += Time.deltaTime;
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        return timeToDisplay;
    }

    // The following list retrieves the corner vertices of the plane by scaling and transforming the local vertex positions to global coordinates. It can be used for collision or alignment detection (not used atm)
    private List<Vector3> cornerVertices(GameObject plane)
    {
        List<Vector3> localPoints;
        List<Vector3> globalPoints = new List<Vector3>();
        List<Vector3> cornerPoints = new List<Vector3>();
        List<int> planeCorners = new List<int>() { 0, 10, 110, 120, 55 };

        localPoints = new List<Vector3>(plane.transform.GetComponent<MeshFilter>().mesh.vertices);
        Vector3 pointMagnitude = plane.transform.TransformDirection(localPoints[55].x * (float)0.1, localPoints[55].y * (float)0.1, localPoints[55].z * (float)0.1);

        foreach (Vector3 point in localPoints)
        {
            // multiply points by plane scale factor 0.1
            Vector3 pointTranslation = plane.transform.TransformPoint(point.x * (float)0.1, point.y * (float)0.1, point.z * (float)0.1);
            globalPoints.Add(pointTranslation - pointMagnitude);
        }

        foreach (int id in planeCorners)
        {
            cornerPoints.Add(globalPoints[id]);
        }

        return cornerPoints;
    }

    // This method compares the positions of the corners of two planes to check their alignment, potentially contributing to score adjustments or target validation (not used at the moment).
    private double VerifyPlane(GameObject P, GameObject T)
    {
        List<bool> alignPlane = new List<bool>();
        List<Vector3> P_vertices = new List<Vector3>();
        List<Vector3> T_vertices = new List<Vector3>();
        double verticeScore = 0.0;

        P_vertices = cornerVertices(P);
        T_vertices = cornerVertices(T);

        for (int i = 0; i < T_vertices.Count; i++)
        {
            alignPlane.Add(false);
            foreach (Vector3 pointP in P_vertices)
            {
                alignPlane.Add(false);
                double deltaDistance = Vector3.Distance(T_vertices[i], pointP);
                if (deltaDistance < 0.02f)
                {
                    alignPlane[i] = true;
                }
            }
        }

        if (!alignPlane.Contains(false))
        {
            verticeScore = 0.5;
        }
        
        return verticeScore;
    }

    // This method displays the positional difference between the currentPlane and the targetPlane in the positionText UI element
    private void ShowPosition()
    {
        double distanceX = targetPlane.transform.position.x - currentPlane.transform.position.x;
        double distanceY = targetPlane.transform.position.y - currentPlane.transform.position.y;
        double distanceZ = targetPlane.transform.position.z - currentPlane.transform.position.z;
        if (distanceX < 0)
        {
            distX = distanceX.ToString("N3");
        }
        else
        {
            distX = " " + distanceX.ToString("N3");
        }
        if (distanceY < 0)
        {
            distY = distanceY.ToString("N3");
        }
        else
        {
            distY = " " + distanceY.ToString("N3");
        }
        if (distanceZ < 0)
        {
            distZ = distanceZ.ToString("N3");
        }
        else
        {
            distZ = " " + distanceZ.ToString("N3");
        }
        positionText.text = "x: " + distX +
                            "\ny: " + distY +
                            "\nz: " + distZ;
    }
}
