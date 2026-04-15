using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;

public class ArmControlROS : MonoBehaviour
{
    [Header("Configuración ROS")]
    public string topicName = "/joint_states"; 
    
    [Header("Articulación 1: Elevación (Slider)")]
    public Slider armSlider;
    public string jointElevacion = "2J1"; 

    [Header("Articulación 2: Giro (Botón)")]
    public string jointGiro = "2J2"; 
    public float posicionActualGiro = 0f;
    public float velocidadGiroConstante = 5000f; // El valor de tu foto
    public float minGiro = -20000f;
    public float maxGiro = 20000f;
    
    private int direccionGiro = 0; // -1, 0, 1

    void Start()
    {
        // Registramos el publicador (se comparte el tópico pero mandaremos mensajes distintos)
        ROSConnection.GetOrCreateInstance().RegisterPublisher<JointStateMsg>(topicName);

        if (armSlider != null)
        {
            armSlider.onValueChanged.AddListener(delegate { PublicarSoloElevacion(); });
        }
    }

    void Update()
    {
        // Si el giro está activo, publicamos constantemente la rotación
        if (direccionGiro != 0)
        {
            posicionActualGiro += direccionGiro * velocidadGiroConstante * Time.deltaTime;
            posicionActualGiro = Mathf.Clamp(posicionActualGiro, minGiro, maxGiro);
            PublicarSoloGiro();
        }
    }

    // --- MÉTODOS DE PUBLICACIÓN SEPARADOS ---

    // 1. PUBLICADOR SOLO PARA EL SLIDER (2J1)
    public void PublicarSoloElevacion()
    {
        JointStateMsg msg = new JointStateMsg();
        msg.header = CreateHeader();
        
        msg.name = new string[] { jointElevacion };
        msg.position = new double[] { (double)armSlider.value };
        msg.velocity = new double[] { 0.0 };

        ROSConnection.GetOrCreateInstance().Publish(topicName, msg);
    }

    // 2. PUBLICADOR SOLO PARA EL GIRO (2J2)
    public void PublicarSoloGiro()
    {
        JointStateMsg msg = new JointStateMsg();
        msg.header = CreateHeader();

        msg.name = new string[] { jointGiro };
        msg.position = new double[] { (double)posicionActualGiro };
        
        // Lógica de velocidad de tu foto: 0 si parado, 200 si gira
        double velEnvio = (direccionGiro == 0) ? 0.0 : 200.0;
        msg.velocity = new double[] { velEnvio };

        ROSConnection.GetOrCreateInstance().Publish(topicName, msg);
    }

    // --- CONTROL DEL BOTÓN ---
    public void ToggleGiro()
    {
        if (direccionGiro == 0) direccionGiro = 1;
        else {
            direccionGiro = 0;
            PublicarSoloGiro(); // Mandamos un último mensaje para asegurar la parada
        }
        Debug.Log(direccionGiro != 0 ? "Giro 2J2 activado" : "Giro 2J2 parado");
    }

    // Función auxiliar para el Header de ROS2
    private HeaderMsg CreateHeader()
    {
        HeaderMsg header = new HeaderMsg();
        header.frame_id = "base_link";
        // Si tu versión soporta GetTime(), añádela aquí. Si no, déjalo así.
        return header;
    }
}