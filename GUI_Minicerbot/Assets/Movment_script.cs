using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry; 

public class RosTeleopDualControl : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "/cmd_vel";

    [Header("Límites de Velocidad")]
    public float maxLinearLimit = 0.2f;  // Máximo que puede alcanzar
    public float maxAngularLimit = 0.5f;
    
    [Header("Estado Actual (Crucero)")]
    public float currentLinearSpeed = 0.15f;  // Se ajusta con R2/L2
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
        // 1. AJUSTE DE VELOCIDAD CON GATILLOS (R2 aumenta, L2 disminuye)
        // Usamos GetKeyDown o una pulsación mantenida suave
        if (Input.GetKey(KeyCode.JoystickButton5) || Input.GetKey(KeyCode.E)) // R2 Augmenta la velocidad
        {
            currentLinearSpeed = Mathf.Clamp(currentLinearSpeed + 0.01f, 0, maxLinearLimit);
        }
        if (Input.GetKey(KeyCode.JoystickButton4) || Input.GetKey(KeyCode.Q)) // L2 Disminuye la velocidad
        {
            currentLinearSpeed = Mathf.Clamp(currentLinearSpeed - 0.01f, 0, maxLinearLimit);
        }

        // 2. LECTURA ANALÓGICA DEL JOYSTICK (La "puntuación")
        // GetAxis devuelve un valor de -1.0 a 1.0 dependiendo de cuánto inclines el palo
        float joyVertical = Input.GetAxis("Vertical");
        float joyHorizontal = Input.GetAxis("Horizontal");

        float move = 0f;
        float turn = 0f;

        // Mezcla de teclado (para emergencias)
        if (Input.GetKey(KeyCode.W)) move = 1f;
        else if (Input.GetKey(KeyCode.S)) move = -1f;
        if (Input.GetKey(KeyCode.A)) turn = 1f;
        else if (Input.GetKey(KeyCode.D)) turn = -1f;

        // Si no hay teclado, usamos la "puntuación" del Joystick
        if (move == 0) move = joyVertical;
        if (turn == 0) turn = -joyHorizontal;

        // 3. CÁLCULO FINAL (Velocidad Proporcional)
        // Aquí es donde la IA/Mando brilla: si mueves el joystick solo un poco, 
        // el robot se mueve a un % de la velocidad de crucero.
        float finalForward = move * currentLinearSpeed;
        float finalTurn = turn * currentAngularSpeed;

        // 4. MOVIMIENTO DE REFERENCIA (Unity)
        if (robotReferencia != null)
        {
            float dt = Time.deltaTime;
            robotReferencia.Rotate(0, -finalTurn * Mathf.Rad2Deg * dt, 0);
            robotReferencia.Translate(Vector3.forward * finalForward * dt);
        }

        // 5. PUBLICAR A ROS
        TwistMsg cmdVel = new TwistMsg();
        cmdVel.linear.x = finalForward;
        cmdVel.angular.z = finalTurn;
        ros.Publish(topicName, cmdVel);
    }
}
