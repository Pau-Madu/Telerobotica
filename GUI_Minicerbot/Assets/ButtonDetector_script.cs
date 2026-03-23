using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class ButtonPublisher : MonoBehaviour
{
    public ROSConnection ros;
    public string topicName = "/button_detector";
    private Button button;             // Referencia al botón
    private Text buttonText;           // Referencia al texto del botón

    void Start()
    {
        // Inicializa ROS
        if (ros == null)
            ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<BoolMsg>(topicName);

        // Consigue referencia al Button
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("No se encontró componente Button en el GameObject.");
            return;
        }

        // Cambiar el texto del botón
        buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = "Button Detector";
        }

        // Posicionar el botón en la esquina inferior izquierda
        RectTransform rt = button.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0, 0);   // esquina inferior izquierda
            rt.anchorMax = new Vector2(0, 0);   // esquina inferior izquierda
            rt.pivot = new Vector2(0, 0);       // punto de pivote inferior izquierda
            rt.anchoredPosition = new Vector2(32, 170); // margen desde la esquina
        }

        // Añade listener al botón
        button.onClick.AddListener(OnButtonPressed);
    }

    public void OnButtonPressed()
    {
        BoolMsg msg = new BoolMsg(true);
        ros.Publish(topicName, msg);
        Debug.Log("Button pressed, message sent to /button_detector");
    }
}
