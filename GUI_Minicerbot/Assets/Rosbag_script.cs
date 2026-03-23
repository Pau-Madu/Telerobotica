using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Diagnostics;
using System.IO;
using System;

public class RosbagControl : MonoBehaviour
{
    public Button rosbagButton;
    public TextMeshProUGUI buttonText;
    public string workspacePath = "/home/pau/Documents/GitHub/Telerobotica/workspace";
    
    private Process rosbagProcess;
    private bool isRecording = false;

    public void ToggleRosbag()
    {
        if (!isRecording) {
            StartRecording();
        } else {
            StopRecording();
        }
    }

    void StartRecording()
    {
        try {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string savePath = $"{workspacePath}/rosbags/bag_{timestamp}";

            // Comando con carga de entorno
	    string command = $"source /opt/ros/humble/setup.bash && ros2 bag record -o /home/pau/Documents/GitHub/Telerobotica/workspace/rosbags/bag_{timestamp} /cmd_vel /light_sensor";

            ProcessStartInfo psi = new ProcessStartInfo {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                UseShellExecute = false,
                // IMPORTANTE: Ponemos estas en FALSE para evitar que Unity se quede esperando respuesta
                RedirectStandardError = false, 
                RedirectStandardOutput = false,
                CreateNoWindow = true
            };

            rosbagProcess = Process.Start(psi);
            
            isRecording = true;
            UpdateUI();
            UnityEngine.Debug.Log("Grabación iniciada en segundo plano: " + savePath);

        } catch (Exception e) {
            UnityEngine.Debug.LogError("Error al lanzar proceso: " + e.Message);
        }
    }

    void StopRecording()
    {
        if (isRecording)
        {
            // Enviamos la señal 2 (SIGINT/Ctrl+C) al proceso de ros2 bag
            string killCommand = "pkill -2 -f 'ros2 bag record'";
        
            ProcessStartInfo killPsi = new ProcessStartInfo {
                FileName = "/bin/bash",
                Arguments = $"-c \"{killCommand}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
        
            Process.Start(killPsi);
        
            isRecording = false;
            UpdateUI();
            UnityEngine.Debug.Log("Grabación detenida con señal SIGINT (limpia).");
        }
    }

    void UpdateUI()
    {
        if (buttonText != null) buttonText.text = isRecording ? "Stop Rosbag" : "Record Rosbag";
        if (rosbagButton != null) rosbagButton.image.color = isRecording ? Color.red : Color.white;
    }

    // Seguridad: Si cierras Unity, que se detenga el rosbag para no dejar procesos basura
    void OnApplicationQuit() {
        if (isRecording) StopRecording();
    }
}
