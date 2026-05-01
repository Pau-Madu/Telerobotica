using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry; 

public class RosTeleopDualControl : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "/cmd_vel";

    [Header("Límites de Velocidad")]
    public float maxLinearLimit = 0.2f;  
    public float maxAngularLimit = 0.5f;
    
    [Header("Estado Actual (Crucero)")]
    public float currentLinearSpeed = 0.15f;  
    public float currentAngularSpeed = 0.3f;

    [Header("Referencias")]
    public Camera_script cameraScript; 
    // public Transform robotReferencia; 

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<TwistMsg>(topicName);
    }

    void Update()
    {
        // 1. AJUSTE DE VELOCIDAD CON GATILLOS DEL MANDO
        if (Input.GetKey(KeyCode.JoystickButton5) || Input.GetKey(KeyCode.E)) 
        {
            currentLinearSpeed = Mathf.Clamp(currentLinearSpeed + 0.01f, 0, maxLinearLimit);
        }
        if (Input.GetKey(KeyCode.JoystickButton4) || Input.GetKey(KeyCode.Q)) 
        {
            currentLinearSpeed = Mathf.Clamp(currentLinearSpeed - 0.01f, 0, maxLinearLimit);
        }

        // 2. LECTURA DE ENTRADAS
        float moveX = 0f;
        float moveY = 0f; // Para el desplazamiento lateral de las flechas
        float turn = 0f;

        // Lectura de Joysticks
        float joyVertical = Input.GetAxis("Vertical");
        float joyHorizontal = Input.GetAxis("Horizontal");

        // --- MOVIMIENTO TECLADO WASD (del taclado)---
        if (Input.GetKey(KeyCode.W)) moveX = 1f;
        else if (Input.GetKey(KeyCode.S)) moveX = -1f;
        
        if (Input.GetKey(KeyCode.A)) turn = 1f;
        else if (Input.GetKey(KeyCode.D)) turn = -1f;

        // AJUSTE FINO CON LAS FLECHAS (del taclado)
        if (Input.GetKey(KeyCode.UpArrow)) moveX = 0.2f;
        else if (Input.GetKey(KeyCode.DownArrow)) moveX = -0.2f;
        // AJUSTE FINO lateral (no gira, solo desplazamiento lateral)
        if (Input.GetKey(KeyCode.LeftArrow)) moveY = 0.2f;
        else if (Input.GetKey(KeyCode.RightArrow)) moveY = -0.2f;

        // Prioridad: Si no se usan las flechas para el lateral, el joystick controla el giro
        if (moveX == 0 && moveY == 0) moveX = joyVertical;
        if (turn == 0 && moveY == 0) turn = -joyHorizontal; 

        // 3. BOTONES DE ACCIÓN DEL MANDO
        if (Input.GetKeyDown(KeyCode.JoystickButton0)) 
        {
            if (cameraScript != null) cameraScript.ToggleEntreMovilesReales(); //Cambiamos entre camaras Reales
        }
        if (Input.GetKeyDown(KeyCode.JoystickButton2))
        {
            if (cameraScript != null) cameraScript.ToggleModoSimulacion();
        }

        // 4. CÁLCULO Movimiento
        float finalForward = moveX * currentLinearSpeed;
        float finalStrafe = moveY * currentLinearSpeed;
        float finalTurn = turn * currentAngularSpeed;

        // 5. MOVIMIENTO DE REFERENCIA
        /*if (robotReferencia != null)  // Intento fallido de mover el robot en Unity para visualizar el movimiento y mejorar la LATENCIA, pero no es necesario para el control real
        {
            float dt = Time.deltaTime;
            robotReferencia.Rotate(0, -finalTurn * Mathf.Rad2Deg * dt, 0);
            Vector3 movimientoLocal = new Vector3(finalStrafe, 0, finalForward);
            robotReferencia.Translate(movimientoLocal * dt);
        }
        */

        // 6. PUBLICAR A ROS
        TwistMsg cmdVel = new TwistMsg();
        cmdVel.linear.x = finalForward;
        cmdVel.linear.y = finalStrafe; // Envío lateral para ajuste fino
        cmdVel.angular.z = finalTurn;  // Giro controlado por WASD o Joystick
        
        ros.Publish(topicName, cmdVel);
    }
}