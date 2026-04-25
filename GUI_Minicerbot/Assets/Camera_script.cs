using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using TMPro;

public class Camera_script : MonoBehaviour
{
    [Header("Configuración de Interfaz")]
    public RawImage mainDisplay;      
    public RawImage secondaryDisplay; 
    public Button changeButton;       // Botón azul "CAMERA"

    [Header("HUD - Telepropiocepción")]
    public TextMeshProUGUI fpsText;
    public TextMeshProUGUI latencyText;
    public TextMeshProUGUI bitrateText;

    [Header("Control de Zoom")]
    public Slider zoomSlider;

    private Texture2D textureReal, textureReal2, textureSim;
    private byte[] dataReal, dataReal2, dataSim;
    private bool isRealUpdated, isReal2Updated, isSimUpdated;

    [Header("Estado de la Lógica")]
    public bool modoSimulacionActivo = false; 
    public bool verMovil1EnGrande = true;     

    private float lastRealTime, lastReal2Time, lastSimTime;
    private float timeoutThreshold = 2.0f;
    private int frameCount = 0;
    private long totalBytesInSecond = 0;
    private float nextUpdate = 0.0f;

    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<CompressedImageMsg>("/image_raw/compressed", ProcessRealImage);
        ROSConnection.GetOrCreateInstance().Subscribe<CompressedImageMsg>("/image_raw/compressed_2", ProcessReal2Image);
        ROSConnection.GetOrCreateInstance().Subscribe<CompressedImageMsg>("/simulated_camera/image_raw/compressed", ProcessSimImage);
        
        textureReal = new Texture2D(1, 1);
        textureReal2 = new Texture2D(1, 1);
        textureSim = new Texture2D(1, 1);

        if (zoomSlider != null) SetZoom(zoomSlider.value);
    }

    private void ProcessRealImage(CompressedImageMsg msg) { dataReal = msg.data; isRealUpdated = true; lastRealTime = Time.time; totalBytesInSecond += msg.data.Length; }
    private void ProcessReal2Image(CompressedImageMsg msg) { dataReal2 = msg.data; isReal2Updated = true; lastReal2Time = Time.time; totalBytesInSecond += msg.data.Length; }
    private void ProcessSimImage(CompressedImageMsg msg) { dataSim = msg.data; isSimUpdated = true; lastSimTime = Time.time; totalBytesInSecond += msg.data.Length; }

    void Update()
    {
        if (Time.time >= nextUpdate) { UpdateTelemetryUI(); nextUpdate = Time.time + 1.0f; frameCount = 0; totalBytesInSecond = 0; }

        if (mainDisplay == null || secondaryDisplay == null) return;

        if (isRealUpdated || isReal2Updated || isSimUpdated)
        {
            if (isRealUpdated) { textureReal.LoadImage(dataReal); textureReal.Apply(); isRealUpdated = false; }
            if (isReal2Updated) { textureReal2.LoadImage(dataReal2); textureReal2.Apply(); isReal2Updated = false; }
            if (isSimUpdated) { textureSim.LoadImage(dataSim); textureSim.Apply(); isSimUpdated = false; }

            // LÓGICA DE VISUALIZACIÓN CON ROTACIONES EN CADA CÁMARA
            // Cámara 1 (Real): 180º | Cámara 2 (Real2): 270º | Simulación: 0º

            if (modoSimulacionActivo)
            {
                // Grande: Simulación (0º) | Pequeña: Móvil 2 (270º)
                AsignarTexturaYRotacion(mainDisplay, textureSim, 0);
                AsignarTexturaYRotacion(secondaryDisplay, textureReal2, 270);
            }
            else
            {
                if (verMovil1EnGrande) {
                    // Grande: Móvil 1 (180º) | Pequeña: Móvil 2 (270º)
                    AsignarTexturaYRotacion(mainDisplay, textureReal, 180);
                    AsignarTexturaYRotacion(secondaryDisplay, textureReal2, 270);
                } else {
                    // Grande: Móvil 2 (270º) | Pequeña: Móvil 1 (180º)
                    AsignarTexturaYRotacion(mainDisplay, textureReal2, 270);
                    AsignarTexturaYRotacion(secondaryDisplay, textureReal, 180);
                }
            }
            frameCount++;
        }

        bool movil1Vivo = (Time.time - lastRealTime) < timeoutThreshold;
        if (changeButton != null) {
            changeButton.interactable = movil1Vivo; 
        }
    }

    // Función auxiliar para no repetir código y mantener los paneles en su sitio
    private void AsignarTexturaYRotacion(RawImage display, Texture2D tex, float zRotation)
    {
        display.texture = tex;
        // Rotamos el RectTransform sobre su propio eje Z. 
        // Asegúrate en Unity de que el Pivot de la RawImage esté en (0.5, 0.5)
        display.rectTransform.localRotation = Quaternion.Euler(0, 0, zRotation);
    }

    public void ToggleModoSimulacion() {
        modoSimulacionActivo = !modoSimulacionActivo;
        if (modoSimulacionActivo) verMovil1EnGrande = true;
    }

    public void ToggleEntreMovilesReales() {
        modoSimulacionActivo = false; 
        verMovil1EnGrande = !verMovil1EnGrande;
    }

    void UpdateTelemetryUI() {
        if (fpsText != null) fpsText.text = $"FPS: {frameCount}";
        if (bitrateText != null) bitrateText.text = $"Bitrate: {(totalBytesInSecond / 1024f):F1} KB/s";
        if (latencyText != null) {
            float lastT = modoSimulacionActivo ? lastSimTime : (verMovil1EnGrande ? lastRealTime : lastReal2Time);
            float lat = (lastT > 0) ? (Time.time - lastT) * 1000 : 0;
            latencyText.text = $"Latency: {lat:F0} ms";
        }
    }

    public void SetZoom(float zoomValue) {
        if (zoomValue < 1f) zoomValue = 1f;
        if (mainDisplay != null) {
            float size = 1.0f / zoomValue;
            float offset = (1.0f - size) / 2.0f;
            mainDisplay.uvRect = new Rect(offset, offset, size, size);
        }
    }
}
