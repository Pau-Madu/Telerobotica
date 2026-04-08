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
    public Transform robotReferencia; 

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<TwistMsg>(topicName);
    }

void Update()
    {
        // 1. AJUSTE DE VELOCIDAD CON GATILLOS (R1 aumenta, L1 disminuye)
        // Usamos GetKeyDown o una pulsación mantenida suave
        if (Input.GetKey(KeyCode.JoystickButton5) || Input.GetKey(KeyCode.E)) // R1 Augmenta la velocidad
        {
            currentLinearSpeed = Mathf.Clamp(currentLinearSpeed + 0.01f, 0, maxLinearLimit);
        }
        if (Input.GetKey(KeyCode.JoystickButton4) || Input.GetKey(KeyCode.Q)) // L1 Disminuye la velocidad
        {
            currentLinearSpeed = Mathf.Clamp(currentLinearSpeed - 0.01f, 0, maxLinearLimit);
        }


        // 2. LECTURA DE ENTRADAS (Joystick y Teclado)
        float move = 0f;
        float turn = 0f;

        // Lectura de Joysticks analógicos
        float joyVertical = Input.GetAxis("Vertical");
        float joyHorizontal = Input.GetAxis("Horizontal");

        // --- MOVIMIENTO TECLADO (WASD normal / Flechas PRECISIÓN) ---
        if (Input.GetKey(KeyCode.W)) move = 1f;
        else if (Input.GetKey(KeyCode.S)) move = -1f;
        
        if (Input.GetKey(KeyCode.A)) turn = 1f;
        else if (Input.GetKey(KeyCode.D)) turn = -1f;

        // --- MODO PRECISIÓN PC (Flechas van al 20% de la velocidad actual) ---
        if (Input.GetKey(KeyCode.UpArrow)) move = 0.2f;
        else if (Input.GetKey(KeyCode.DownArrow)) move = -0.2f;
        
        if (Input.GetKey(KeyCode.LeftArrow)) turn = 0.2f;
        else if (Input.GetKey(KeyCode.RightArrow)) turn = -0.2f;

        // Si no hay teclado, usamos el Joystick
        if (move == 0) move = joyVertical;
        if (turn == 0) turn = -joyHorizontal;

        // 3. BOTONES DE ACCIÓN DEL MANDO (Cámaras)
        // Botón X (Cruz) -> Intercambio de móviles reales
        if (Input.GetKeyDown(KeyCode.JoystickButton0)) 
        {
            if (cameraScript != null) cameraScript.ToggleEntreMovilesReales();
        }

        // Botón Cuadrado -> Cambio a Simulación
        if (Input.GetKeyDown(KeyCode.JoystickButton2))
        {
            if (cameraScript != null) cameraScript.ToggleModoSimulacion();
        }

        // 4. CÁLCULO FINAL
        float finalForward = move * currentLinearSpeed;
        float finalTurn = turn * currentAngularSpeed;

        // 5. MOVIMIENTO DE REFERENCIA (Unity)
        if (robotReferencia != null)
        {
            float dt = Time.deltaTime;
            robotReferencia.Rotate(0, -finalTurn * Mathf.Rad2Deg * dt, 0);
            robotReferencia.Translate(Vector3.forward * finalForward * dt);
        }

        // 6. PUBLICAR A ROS
        TwistMsg cmdVel = new TwistMsg();
        cmdVel.linear.x = finalForward;
        cmdVel.angular.z = finalTurn;
        ros.Publish(topicName, cmdVel);
    }
}
