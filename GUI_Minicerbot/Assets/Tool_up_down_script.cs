using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

public class ArmControlROS : MonoBehaviour
{
    [Header("Configuración ROS")]
    // Tópico corregido según tu última observación
    public string topicName = "/joint_states_simulation";
    
    [Header("Referencias UI")]
    public Slider armSlider;
    public string jointName = "joint_brazo"; 

    void Start()
    {
        // Registro del publicador
        ROSConnection.GetOrCreateInstance().RegisterPublisher<JointStateMsg>(topicName);

        // Seguridad: Si el slider no está asignado en el inspector, intentamos buscarlo
        if (armSlider == null) armSlider = GetComponent<Slider>();
        
        if (armSlider != null)
        {
            armSlider.onValueChanged.AddListener(OnSliderChanged);
        }
        else
        {
            Debug.LogError("¡OJO! No has asignado el Slider en el script ArmControlROS");
        }
    }

    public void OnSliderChanged(float value)
    {
        JointStateMsg msg = new JointStateMsg();
        msg.name = new string[] { jointName };
        msg.position = new double[] { (double)value };
        
        // Enviamos arrays de velocidad y esfuerzo vacíos para que ROS2 no de problemas
        msg.velocity = new double[] { 0.0 };
        msg.effort = new double[] { 0.0 };

        ROSConnection.GetOrCreateInstance().Publish(topicName, msg);
    }
}
