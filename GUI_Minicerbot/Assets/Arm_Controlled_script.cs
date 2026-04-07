using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

public class ArmControlROS : MonoBehaviour
{
    [Header("Configuración ROS")]
    public string topicName = "/joint_states_simulation";
    
    [Header("Referencias UI - Elevación")]
    public Slider armSlider;
    public string jointElevacion = "2J1"; 

    [Header("Referencias UI - Giro (Desatornillador)")]
    public string jointGiro = "2J2";
    public float velocidadGiro = 1.0f; // Velocidad a la que desatornilla
    private bool estaGirando = false;
    private float anguloActual = 0f;

    void Start()
    {
        // Registro del publicador (solo hace falta una vez para el tópico)
        ROSConnection.GetOrCreateInstance().RegisterPublisher<JointStateMsg>(topicName);

        if (armSlider != null)
        {
            armSlider.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    void Update()
    {
        // Si el modo desatornillar está activo, incrementamos el ángulo y publicamos
        if (estaGirando)
        {
            // Calculamos el nuevo ángulo según el tiempo para que el giro sea fluido
            anguloActual += velocidadGiro * Time.deltaTime;
            PublicarEstadoBrazo(armSlider != null ? armSlider.value : 0f, anguloActual);
        }
    }

    // Llama al botón circular "Rotate Tool"
    public void ToggleGiro()
    {
        estaGirando = !estaGirando;
        Debug.Log(estaGirando ? "Desatornillando..." : "Giro parado.");
    }

    public void OnSliderChanged(float value)
    {
        // Cuando movemos el slider, enviamos la posición del slider y el ángulo de giro actual
        PublicarEstadoBrazo(value, anguloActual);
    }

    // Función auxiliar para enviar ambos joints a la vez (Formato estándar ROS)
    private void PublicarEstadoBrazo(float posElevacion, float posGiro)
    {
        JointStateMsg msg = new JointStateMsg();
        
        // Enviamos los nombres de ambos joints
        msg.name = new string[] { jointElevacion, jointGiro };
        
        // Enviamos las posiciones de ambos
        msg.position = new double[] { (double)posElevacion, (double)posGiro };
        
        msg.velocity = new double[] { 0.0, 0.0 };
        msg.effort = new double[] { 0.0, 0.0 };

        ROSConnection.GetOrCreateInstance().Publish(topicName, msg);
    }
}
