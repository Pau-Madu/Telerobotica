using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry; 

public class RosTeleopDualControl : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "/cmd_vel";

    [Header("Configuración de Velocidad")]
    public float linearSpeed = 0.2f;    // Velocidad lineal (m/s)
    public float angularSpeed = 0.5f;   // Velocidad angular (rad/s)

    [Header("Referencia a la Cámara")]
    public Camera_script cameraScript; 

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<TwistMsg>(topicName);
    }

    void Update()
    {
        // 1. CONTROL DE CÁMARA (Opcional, se mantiene por si usas el botón)
        if (Input.GetKeyDown(KeyCode.JoystickButton0))
        {
            if (cameraScript != null) cameraScript.ToggleCamera();
        }

        // 2. OBTENCIÓN DE ENTRADAS DIGITALES (Teclado)
        float finalForward = 0;
        float finalTurn = 0;

        // Movimiento Lineal
        if (Input.GetKey(KeyCode.W)) finalForward = linearSpeed;
        else if (Input.GetKey(KeyCode.S)) finalForward = -linearSpeed;

        // Movimiento Angular
        if (Input.GetKey(KeyCode.A)) finalTurn = angularSpeed;
        else if (Input.GetKey(KeyCode.D)) finalTurn = -angularSpeed;

        // 3. PUBLICACIÓN DEL MENSAJE TWIST
        // Directamente enviamos los valores obtenidos del teclado
        TwistMsg cmdVel = new TwistMsg();
        cmdVel.linear.x = finalForward;
        cmdVel.angular.z = finalTurn;

        ros.Publish(topicName, cmdVel);
    }
}
