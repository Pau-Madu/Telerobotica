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

    [Header("Referencias UI - Giro (Herramienta)")]
    public string jointGiro = "2J2"; 
    public float velocidadGiro = 5.0f; // Aumentamos la velocidad para que sea visible
    
    private bool estaGirando = false;
    private float anguloActual = 0f;

    void Start()
    {
        ROSConnection.GetOrCreateInstance().RegisterPublisher<JointStateMsg>(topicName);

        if (armSlider != null)
        {
            armSlider.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    void Update()
    {
        if (estaGirando)
        {
            anguloActual += velocidadGiro * Time.deltaTime;
            
            // Si el slider no está conectado, que al menos no envíe 0
            float alturaSegura = 0f;
            if (armSlider != null) {
                alturaSegura = armSlider.value;
            } else {
                // Si no hay slider, podrías poner aquí el valor por defecto 
                // para que el brazo no se caiga, por ejemplo: alturaSegura = 0.5f;
                Debug.LogWarning("¡Pau, conecta el Slider en el Inspector!");
            }

            PublicarEstadoBrazo(alturaSegura, anguloActual);
        }
    }

    public void ToggleGiro()
    {
        estaGirando = !estaGirando;
        Debug.Log(estaGirando ? "Girando herramienta..." : "Detenido.");
    }

    public void OnSliderChanged(float value)
    {
        // Solo publicamos el cambio del slider si NO está girando para no mezclar mensajes
        if (!estaGirando)
        {
            PublicarEstadoBrazo(value, anguloActual);
        }
    }

    private void PublicarEstadoBrazo(float posElevacion, float posGiro)
    {
        JointStateMsg msg = new JointStateMsg();
        msg.name = new string[] { jointElevacion, jointGiro };
        
        // IMPORTANTE: Aseguramos que pasamos los valores como double
        msg.position = new double[] { (double)posElevacion, (double)posGiro };
        
        // Dejamos el resto en cero
        msg.velocity = new double[] { 0.0, 0.0 };
        msg.effort = new double[] { 0.0, 0.0 };

        ROSConnection.GetOrCreateInstance().Publish(topicName, msg);
    }
}
