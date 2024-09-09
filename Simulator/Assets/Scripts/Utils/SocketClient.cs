using System;
using System.IO;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

// Script designed to handle communication between Unity and a C++ server over a TCP connection. It receives positional (posX, posY, posZ) and rotational (rotX, rotY, rotZ) data from the server and uses these to update the position and rotation of a GameObject in Unity. There is a separate thread (clientThread) for managing the server connection and data reading to avoid blocking Unity's main thread. The received pose data is then dequeued and used to update the position and rotation of the GameObject this script is attached to, in the Update method.

public class SocketClient : MonoBehaviour
{
    private Thread clientThread;
    private TcpClient tcpClient;
    private bool isConnected = false;
    private float scale = 15f; // originally 20.0f
    private Vector3 pos_vect, rot_vect;

    private float getZ;
    public float posBelly;
    public bool touchBelly = false;
    public bool pos1 = true;
    public bool pos2, pos3, pos4 = false;
    public bool message = false;

    private LogicScript logic;

    // Define a concurrent queue to store received pose values
    // Incoming pose data (position and rotation) is processed and stored in a concurrent queue (poseQueue), which ensures thread-safe access to the data between the receiving thread and the Unity main thread.
    private ConcurrentQueue<(Vector3 position, Vector3 rotation)> poseQueue = new ConcurrentQueue<(Vector3, Vector3)>();


    // below is all additional things
    private Vector3 lastPosition;
    private Vector3 lastRotation;
    private float smoothingFactor = 1f;

    private bool inStartupPhase = true;
    private float startupDuration = 2.0f;
    private float startupTimer = 0.0f;
    private Vector3 safePosition = new Vector3(1, 1, 1);


    // Use this for initialization
    void Start()
    {
        // Create and start a new thread for the socket connection to avoid blocking the main thread
        clientThread = new Thread(ConnectToServer);
        clientThread.IsBackground = true;
        clientThread.Start();

        logic = GameObject.FindGameObjectWithTag("Logic").GetComponent<LogicScript>();
    }

    // The following method connects to the server using a TCP client (tcpClient). Once connected, it enters a loop to continuously read pose data from the server using a BinaryReader that reads the X, Y, and Z coordinates for both position and rotation as float values.
    void ConnectToServer()
    {
        try
        {
            // Connect to the C++ server
            tcpClient = new TcpClient("127.0.0.1", 12345);
            isConnected = true;

            // Receive data from the server
            using (NetworkStream stream = tcpClient.GetStream())
            using (BinaryReader reader = new BinaryReader(stream))
            {
                while (isConnected && tcpClient.Connected)
                {
                    // Read the pose values from the server
                    float posX = reader.ReadSingle();
                    float posY = reader.ReadSingle();
                    float posZ = reader.ReadSingle();
                    float rotX = reader.ReadSingle();
                    float rotY = reader.ReadSingle();
                    float rotZ = reader.ReadSingle();

                    //float forceX = reader.ReadSingle();
                    //float forceY = reader.ReadSingle();
                    //float forceZ = reader.ReadSingle();

                    // Process the received pose values (e.g., update a GameObject's position and rotation)
                    // The pose data received from the server is processed differently based on which positional state (pos1, pos2, pos3, pos4) is active. This allows for transformations based on different frames of reference, scaling and rotating the position and orientation values accordingly. For example, if pos1 is active, the position and rotation values are adjusted differently from when pos2 or other states are active.
                    if (pos1)
                    {
                        pos_vect = new Vector3(-posX * scale, -posY * scale, -posZ * scale);
                        rot_vect = new Vector3(rotX, rotY, rotZ + 45f);
                    }
                    else if (pos2)
                    {
                        pos_vect = new Vector3(-posX * scale, posY * scale, posZ * scale);
                        rot_vect = new Vector3(rotX + 180f, -rotY, rotZ + 45f);
                    }
                    else if (pos3)
                    {
                        pos_vect = new Vector3(posX * scale, -posY * scale, posZ * scale);
                        rot_vect = new Vector3(-rotX + 180f, rotY, rotZ + 45f);
                    }
                    else if (pos4)
                    {
                        pos_vect = new Vector3(posX * scale, posY * scale, -posZ * scale);
                        rot_vect = new Vector3(-rotX, -rotY, rotZ + 45f);
                    }

                    //if (touchBelly && (pos_vect.z < posBelly))
                    //{
                    //    pos_vect.z = posBelly;
                    //    Debug.Log($"Collided at posZ: {pos_vect.z} or posBelly: {posBelly}");
                    //}

                    // After processing the position and rotation values, they are enqueued into the poseQueue. This is a thread-safe queue that stores the data until it can be dequeued and processed in Unity's main thread.
                    poseQueue.Enqueue((pos_vect, rot_vect));

                    //Debug.Log($"p ({pos_vect.x}, {pos_vect.y}, {pos_vect.z}) | r ({rotX}, {rotY}, {rotZ})");
                    //Debug.Log($"p ({posX}, {posY}, {posZ}) | r ({rotX}, {rotY}, {rotZ})");
                    //Debug.Log($"p ({posX}, {posY}, {posZ}) | r ({rotX}, {rotY}, {rotZ}) | f ({forceX}, {forceY}, {forceZ})");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error connecting to server: {e.Message}");
        }
    }

    // Update is called once per frame (it dequeues the pose data from poseQueue and updates the GameObject's position (transform.localPosition) and rotation (transform.localRotation).)
    void Update()
    {
        // // additional stuff here: smoothing of movements could be applied by interpolating between the current and last position/rotation using Vector3.Lerp for position and Quaternion.Slerp for rotation.
        // if (inStartupPhase)
        // {
        //    startupTimer += Time.deltaTime;
        //     if (startupTimer < startupDuration)
        //    {
        //        transform.localPosition = Vector3.Lerp(transform.localPosition, safePosition, Time.deltaTime / startupDuration);
        //     }
        //    else
        //    {inStartupPhase = false;} return;
        //}

        // Check if there are any pose values in the queue
        // The received pose data is dequeued and used to update the position and rotation of the GameObject this script is attached to
        if (poseQueue.TryDequeue(out (Vector3 position, Vector3 rotation) pose))
        {
            // Update the GameObject's position and rotation with the received pose values
            transform.localPosition = pose.position;
            transform.localRotation = Quaternion.Euler(pose.rotation);

            // // above code was removed, below code added: smoothing movements by interpolating between the last and current positions and rotations. 
            //transform.localPosition = Vector3.Lerp(lastPosition, pose.position, smoothingFactor);
            //transform.localRotation = Quaternion.Slerp(Quaternion.Euler(lastRotation), Quaternion.Euler(pose.rotation), smoothingFactor);

            //lastPosition = transform.localPosition;
            //lastRotation = transform.localRotation.eulerAngles;
        }

        // If the message flag is set, the script sends a signal to the server with SendData(1000) and resets the flag. This is used for sending feedback or commands back to the server.
        if (message)
        {
            SendData(1000);
            message = false;
        }

        // // additional stuff here: positional correction or force application when a specific condition is met (touchBelly). 
        // if (touchBelly && transform.localPosition.z < posBelly)
        // {
        //    transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, posBelly);
        //}
        //if (touchBelly)
        //{
            //transform.GetComponent<Rigidbody>().velocity = new Vector3(0f, 0f, 0f);
            //posBelly = transform.localPosition.z;
            //SendData((float)0.08);
        //}
    }

    void FixedUpdate()
    {
        if (touchBelly)
        {
            transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
    }


    // Cleanup when the script is disabled or the application is closed. It safely closes the TCP connection and joins the client thread, ensuring that no resources are left hanging.
    void OnDisable()
    {
        if (tcpClient != null)
        {
            isConnected = false;
            tcpClient.Close();
            tcpClient = null;
        }

        if (clientThread != null)
        {
            clientThread.Join();
            clientThread = null;
        }
    }    

    // These methods handle collision events. In OnCollisionEnter, when an object with the tag "Belly" is hit, it could enable touchBelly and store the collision position (posBelly). It is intended to adjust the GameObject's velocity or position based on the collision, perhaps providing a rebounding effect using Vector3.Reflect.
    void OnCollisionEnter(Collision collision)
    {
        // below was originally removed
        //touchBelly = true;
        //posBelly = transform.localPosition.z;

        //ALL of below is completely new, so delete if necessary
        //if (collision.gameObject.CompareTag("Belly") && !inStartupPhase)
       // {touchBelly = true; posBelly = transform.localPosition.z; Rigidbody rb = GetComponent<Rigidbody>();
           // if (rb != null)
           // {
            //    Vector3 collisionNormal = collision.contacts[0].normal;
            //    Vector3 incomingVelocity = rb.velocity;
            //    Vector3 reboundVelocity = Vector3.Reflect(incomingVelocity, collisionNormal);
            //    rb.velocity = reboundVelocity * 0.5f;
            //}

        //}
    }

    private void OnCollisionExit(Collision collision)
    {
        // originally below was removed
        //touchBelly = false;


        // below is new and should be deleted if doesn't work
        //if (collision.gameObject.CompareTag("Belly") && !inStartupPhase)
        //{
          //  touchBelly = false;
       // }
    }

    // Send haptic feedback to the server: it converts a float value (sendCode) into a string and sends it via the network stream to the server. Error handling is in place to catch exceptions if the connection fails.
    public void SendData(float sendCode)
    {
        string clientMessage = " ";

        if (tcpClient == null)
        {
            return;
        }
        try
        {
            NetworkStream stream = tcpClient.GetStream();
            if (stream.CanWrite)
            {
                clientMessage = sendCode.ToString();
                byte[] arrByte = System.Text.Encoding.ASCII.GetBytes(clientMessage);
                stream.Write(arrByte, 0, arrByte.Length);
                //Debug.Log("Sent message to haptic device ...");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error connecting to server: {e.Message}");
        }
    }
}

// // Sornsiri's version
// using System;
// using System.IO;
// using System.Collections.Concurrent;
// using System.Net.Sockets;
// using System.Threading;
// using UnityEngine;

// public class SocketClient : MonoBehaviour
// {
//     private Thread clientThread;
//     private TcpClient tcpClient;
//     private bool isConnected = false;
//     private float scale = 20.0f;
//     private Vector3 pos_vect, rot_vect;

//     private float getZ;
//     public float posBelly;
//     public bool touchBelly = false;
//     public bool pos1 = true;
//     public bool pos2, pos3, pos4 = false;
//     public bool message = false;

//     private LogicScript logic;

//     // Define a concurrent queue to store received pose values
//     private ConcurrentQueue<(Vector3 position, Vector3 rotation)> poseQueue = new ConcurrentQueue<(Vector3, Vector3)>();


//     // Use this for initialization
//     void Start()
//     {
//         // Create and start a new thread for the socket connection to avoid blocking the main thread
//         clientThread = new Thread(ConnectToServer);
//         clientThread.IsBackground = true;
//         clientThread.Start();

//         logic = GameObject.FindGameObjectWithTag("Logic").GetComponent<LogicScript>();
//     }

//     void ConnectToServer()
//     {
//         try
//         {
//             // Connect to the C++ server
//             tcpClient = new TcpClient("127.0.0.1", 12345);
//             isConnected = true;

//             // Receive data from the server
//             using (NetworkStream stream = tcpClient.GetStream())
//             using (BinaryReader reader = new BinaryReader(stream))
//             {
//                 while (isConnected && tcpClient.Connected)
//                 {
//                     // Read the pose values from the server
//                     float posX = reader.ReadSingle();
//                     float posY = reader.ReadSingle();
//                     float posZ = reader.ReadSingle();
//                     float rotX = reader.ReadSingle();
//                     float rotY = reader.ReadSingle();
//                     float rotZ = reader.ReadSingle();

//                     //float forceX = reader.ReadSingle();
//                     //float forceY = reader.ReadSingle();
//                     //float forceZ = reader.ReadSingle();

//                     // Process the received pose values (e.g., update a GameObject's position and rotation)
//                     if (pos1)
//                     {
//                         pos_vect = new Vector3(-posX * scale, -posY * scale, -posZ * scale);
//                         rot_vect = new Vector3(rotX, rotY, rotZ + 45f);
//                     }
//                     else if (pos2)
//                     {
//                         pos_vect = new Vector3(-posX * scale, posY * scale, posZ * scale);
//                         rot_vect = new Vector3(rotX + 180f, -rotY, rotZ + 45f);
//                     }
//                     else if (pos3)
//                     {
//                         pos_vect = new Vector3(posX * scale, -posY * scale, posZ * scale);
//                         rot_vect = new Vector3(-rotX + 180f, rotY, rotZ + 45f);
//                     }
//                     else if (pos4)
//                     {
//                         pos_vect = new Vector3(posX * scale, posY * scale, -posZ * scale);
//                         rot_vect = new Vector3(-rotX, -rotY, rotZ + 45f);
//                     }

//                     //if (touchBelly && (pos_vect.z < posBelly))
//                     //{
//                     //    pos_vect.z = posBelly;
//                     //    Debug.Log($"Collided at posZ: {pos_vect.z} or posBelly: {posBelly}");
//                     //}

//                     poseQueue.Enqueue((pos_vect, rot_vect));

//                     //Debug.Log($"p ({pos_vect.x}, {pos_vect.y}, {pos_vect.z}) | r ({rotX}, {rotY}, {rotZ})");
//                     //Debug.Log($"p ({posX}, {posY}, {posZ}) | r ({rotX}, {rotY}, {rotZ})");
//                     //Debug.Log($"p ({posX}, {posY}, {posZ}) | r ({rotX}, {rotY}, {rotZ}) | f ({forceX}, {forceY}, {forceZ})");
//                 }
//             }
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Error connecting to server: {e.Message}");
//         }
//     }

//     // Update is called once per frame
//     void Update()
//     {
//         // Check if there are any pose values in the queue
//         if (poseQueue.TryDequeue(out (Vector3 position, Vector3 rotation) pose))
//         {
//             // Update the GameObject's position and rotation with the received pose values
//             transform.localPosition = pose.position;
//             transform.localRotation = Quaternion.Euler(pose.rotation);
//         }

//         if (message)
//         {
//             SendData(1000);
//             message = false;
//         }

//         if (touchBelly)
//         {
//             //transform.GetComponent<Rigidbody>().velocity = new Vector3(0f, 0f, 0f);
//             //posBelly = transform.localPosition.z;
//             //SendData((float)0.08);
//         }
//     }

//     void FixedUpdate()
//     {
//         if (touchBelly)
//         {
//             transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
//         }
//     }


//     // Cleanup when the script is disabled or the application is closed
//     void OnDisable()
//     {
//         if (tcpClient != null)
//         {
//             isConnected = false;
//             tcpClient.Close();
//             tcpClient = null;
//         }

//         if (clientThread != null)
//         {
//             clientThread.Join();
//             clientThread = null;
//         }
//     }    

//     void OnCollisionEnter(Collision collision)
//     {
//         //touchBelly = true;
//         //posBelly = transform.localPosition.z;
//     }

//     private void OnCollisionExit(Collision collision)
//     {
//         //touchBelly = false;
//     }

//     // Send haptic feedback to the server
//     public void SendData(float sendCode)
//     {
//         string clientMessage = " ";

//         if (tcpClient == null)
//         {
//             return;
//         }
//         try
//         {
//             NetworkStream stream = tcpClient.GetStream();
//             if (stream.CanWrite)
//             {
//                 clientMessage = sendCode.ToString();
//                 byte[] arrByte = System.Text.Encoding.ASCII.GetBytes(clientMessage);
//                 stream.Write(arrByte, 0, arrByte.Length);
//                 //Debug.Log("Sent message to haptic device ...");
//             }
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Error connecting to server: {e.Message}");
//         }
//     }
// }
