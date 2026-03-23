using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using TMPro;

public class Camera_script : MonoBehaviour
{
    [Header("Configuración de Interfaz")]
    public RawImage viewportDisplay;      // El bloque de Visualizacion de Imagenes se almacena en esta Variable
    public Button changeButton;           // El bloque de Boton para intercambiar las imagenes se almacena en esta Variable
    private RectTransform rectTransform;

    [Header("HUD - Telepropiocepción")]    // Los bloques de Texto se almacenan en estas Variables
    public TextMeshProUGUI fpsText;
    public TextMeshProUGUI latencyText;
    public TextMeshProUGUI bitrateText;
    public TextMeshProUGUI posText;

    // Variables para cálculo de FPS y Bitrate
    private int frameCount = 0;
    private long totalBytesInSecond = 0;
    private float nextUpdate = 0.0f;

    // Variables de datos (Telepresencia)
    private Texture2D textureReal;
    private Texture2D textureSim;
    private byte[] dataReal;
    private byte[] dataSim;
    private bool isRealUpdated;
    private bool isSimUpdated;

    public bool showingRealCamera = true; //Por defecto esta activa la Camara real desde el inicio.
    
    // Watchdog para Observabilidad
    private float lastRealTime = 0f;        //Tiempos de espera para las Activaciones del Botón
    private float lastSimTime = 0f;
    private float timeoutThreshold = 2.0f; 

    public Slider zoomSlider; //Para tener el slider del Zoom

    void Start()
    {
        rectTransform = viewportDisplay.GetComponent<RectTransform>();  //Ponemos el Display de las imagenes

        // Suscripción a los flujos de video comprimido (Ahorro de ancho de banda)
        ROSConnection.GetOrCreateInstance().Subscribe<CompressedImageMsg>("/image_raw/compressed", ProcessRealImage);   //Nos subscribimos a los topicos de las imagenes Reales y Simulacion
        ROSConnection.GetOrCreateInstance().Subscribe<CompressedImageMsg>("/simulated_camera/image_raw/compressed", ProcessSimImage);
        
        textureReal = new Texture2D(1, 1);
        textureSim = new Texture2D(1, 1);
        UpdateRotation();
    }

    private void ProcessRealImage(CompressedImageMsg msg) 
    { 
        dataReal = msg.data;                          // Guarda los bytes de la imagen comprimida
        isRealUpdated = true;                         // Avisa al resto del script que hay una imagen nueva lista para ser procesada y dibujada
        lastRealTime = Time.time;                     // Guarda el segundo exacto en el que llegó el mensaje.
        totalBytesInSecond += msg.data.Length;        // Sumamos los bytes para el cálculo de Bitrate
    }

    private void ProcessSimImage(CompressedImageMsg msg) 
    { 
        dataSim = msg.data; 
        isSimUpdated = true; 
        lastSimTime = Time.time; 
        totalBytesInSecond += msg.data.Length;         // Sumamos los bytes para el cálculo de Bitrate
    }
    


    void Update()
    {
        // --- GESTIÓN DE MÉTRICAS (Actualización cada 1 segundo) ---
        if (Time.time >= nextUpdate)
        {
            UpdateTelemetryUI();
            nextUpdate = Time.time + 1.0f;
            frameCount = 0;
            totalBytesInSecond = 0;
        }

        // --- RENDERIZADO Y WATCHDOG ---
        bool isRealNow = (Time.time - lastRealTime) < timeoutThreshold;
        bool isSimNow = (Time.time - lastSimTime) < timeoutThreshold;

        if (showingRealCamera && isRealUpdated)
        {
            textureReal.LoadImage(dataReal);
            textureReal.Apply();
            viewportDisplay.texture = textureReal;
            isRealUpdated = false;
            frameCount++; // Contamos frames para el cálculo de FPS
        }
        else if (!showingRealCamera && isSimUpdated)
        {
            textureSim.LoadImage(dataSim);
            textureSim.Apply();
            viewportDisplay.texture = textureSim;
            isSimUpdated = false;
            frameCount++;
        }
        
        UpdateButtonState(isRealNow, isSimNow);
    }

    void UpdateTelemetryUI()
    {
        // FPS: Crítico para la seguridad. Según Tema 2.2, baja fluidez = pérdida de control.
        if (fpsText != null) {
            fpsText.text = $"FPS: {frameCount}";
            fpsText.color = frameCount > 15 ? Color.green : Color.red;
        }

        // BITRATE: Muestra el consumo de red en KB/s
        if (bitrateText != null) {
            float kbps = (totalBytesInSecond / 1024f);
            bitrateText.text = $"Bitrate: {kbps:F1} KB/s";
        }

        // LATENCIA: Estimada por el tiempo desde el último paquete recibido
        if (latencyText != null) {
            float lat = (Time.time - (showingRealCamera ? lastRealTime : lastSimTime)) * 1000;
            latencyText.text = $"Latency: {lat:F0} ms";
        }
    }

    void UpdateButtonState(bool realAvail, bool simAvail)
    {
        if (changeButton == null) return;
        // Interbloqueo: Solo permite cambiar si hay señal en el otro canal
        changeButton.interactable = showingRealCamera ? simAvail : realAvail;
    }

    public void ToggleCamera()
    {
        showingRealCamera = !showingRealCamera;
        UpdateRotation();
    }

    void UpdateRotation()
    {
        if (rectTransform == null) return;
        // Reindexado: Asegura que la orientación sea natural para el operador (Tema 2.3)
        rectTransform.localRotation = showingRealCamera ? Quaternion.Euler(0, 0, 180) : Quaternion.Euler(0, 0, 0);
    }
    
    public void SetZoom(float zoomValue)
    {
        // 1. Seguridad: Evitamos valores menores a 1 que rompen la vista
        if (zoomValue < 1f) zoomValue = 1f;

        // 2. Verificamos que la imagen exista antes de tocarla
        if (viewportDisplay != null)
        {
            float size = 1.0f / zoomValue;
            float offset = (1.0f - size) / 2.0f;

            // Aplicamos el recorte
            viewportDisplay.uvRect = new Rect(offset, offset, size, size);
            Debug.Log($"Zoom aplicado: {zoomValue}x | Size: {size}");
        }
        else 
        {
            Debug.LogError("¡Error! No has arrastrado la RawImage al script de la cámara.");
        }
    }
}
