using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;

public class ArmControlROS : MonoBehaviour
{
    [Header("Configuración ROS")]
    public string topicName = "/joint_states"; 
    
    [Header("Articulación 1: Elevación (2J1)")]
    public Slider armSlider;
    public string jointElevacion = "2J1"; 

    [Header("Articulación 2: Giro (2J2)")]
    public string jointGiro = "2J2"; 
    
    [Tooltip("Escribe aquí la posición exacta para 2J2")]
    public float posicionManual2J2 = 0f; 
    
    public float velocidadGiroConstante = 200f; // Velocidad constante para el giro automático
    
    private bool estaGirandoActivamente = false;
    private float ultimaPosManualEnviada = -1f;

    void Start()
    {
        ROSConnection.GetOrCreateInstance().RegisterPublisher<JointStateMsg>(topicName);

        if (armSlider != null)
        {
            armSlider.onValueChanged.AddListener(delegate { PublicarSoloElevacion(); });
        }
    }

    void Update()
    {
        // 1. CONTROL MANUAL POR VARIABLE PÚBLICA (2J2)
        // Si el usuario cambia la variable en el Inspector, actualizamos la posición
        if (!Mathf.Approximately(posicionManual2J2, ultimaPosManualEnviada))
        {
            estaGirandoActivamente = false; // Paramos el giro automático si metemos posición a mano
            PublicarSoloGiro(posicionManual2J2, 0.0); // Enviamos posición manual con velocidad 0
            ultimaPosManualEnviada = posicionManual2J2;
        }

        // 2. LÓGICA DEL GIRO AUTOMÁTICO (Botón)
        if (estaGirandoActivamente)
        {
            posicionManual2J2 += 1 * velocidadGiroConstante * Time.deltaTime; // Cambiar signo = cambiar sentido de giro
            // Actualizamos la variable de control para que no detecte "cambio manual"
            ultimaPosManualEnviada = posicionManual2J2; 
            
            PublicarSoloGiro(posicionManual2J2, 200.0);
        }
        else if (!estaGirandoActivamente)
        {
            // Enviamos constantemente la posición manual para mantener el motor bloqueado ahí
            PublicarSoloGiro(posicionManual2J2, 0.0);
        }
    }

    public void PublicarSoloElevacion()
    {
        JointStateMsg msg = new JointStateMsg();
        msg.header = CreateHeader();
        msg.name = new string[] { jointElevacion };
        msg.position = new double[] { (double)armSlider.value };
        msg.velocity = new double[] { 0.0 };
        ROSConnection.GetOrCreateInstance().Publish(topicName, msg);
    }

    public void PublicarSoloGiro(float pos, double vel)
    {
        JointStateMsg msg = new JointStateMsg();
        msg.header = CreateHeader();
        msg.name = new string[] { jointGiro };
        msg.position = new double[] { (double)pos };
        msg.velocity = new double[] { vel };

        ROSConnection.GetOrCreateInstance().Publish(topicName, msg);
    }

    public void ToggleGiro()
    {
        estaGirandoActivamente = !estaGirandoActivamente;
    }

    private HeaderMsg CreateHeader()
    {
        HeaderMsg header = new HeaderMsg();
        header.frame_id = "base_link";
        return header;
    }
}



// Para MOVER el INTERRUPTOR se ha puesto la herramienta totalmente en Horizontal y la posicion de Giro = 0.
// A continuación se ha cambiado el valor de Giro a 400 para que encaje con el botón.
// Finalmente se ha ajustado la velocidad de giro a -400 para que la herramienta gire hacia la Izquierda y active el interruptor.
