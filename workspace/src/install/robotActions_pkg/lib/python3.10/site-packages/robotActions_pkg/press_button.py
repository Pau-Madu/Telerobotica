#!/usr/bin/env python3

import rclpy
from rclpy.node import Node
from std_msgs.msg import Bool
from sensor_msgs.msg import CompressedImage, JointState
from geometry_msgs.msg import Twist
import cv2
import numpy as np

"""
Nodo para presionar el botón rojo
Topicos simulados:
    - /joint_states_simulation (JointState): para controlar la herramienta
    - /simulated_camera/image_raw/compressed (CompressedImage): para detectar el botón rojo
Topicos reales:
    - /joint_states (JointState): para controlar la herramienta
    - /image_raw/compressed (CompressedImage): para detectar el botón rojo
"""

class PressButton(Node):
    def __init__(self):
        super().__init__('press_button')

        # CONFIGURACIÓN
        self.is_simulation = False  # True = simulación | False = robot real

        if self.is_simulation:
            self.get_logger().info("Modo SIMULACIÓN activado")
            joint_topic = '/joint_states_simulation'
            image_topic = '/simulated_camera/image_raw/compressed'
        else:
            self.get_logger().info("Modo REAL activado")
            joint_topic = '/joint_states'
            image_topic = '/image_raw/compressed'

        # Estado
        self.auto_mode = False
        self.button_pressed = False
        self.state = "IDLE"
        self.press_start_time = None

        # Publicadores
        self.joint_pub = self.create_publisher(JointState, joint_topic, 10)
        self.cmd_pub = self.create_publisher(Twist, '/cmd_vel', 10)
        self.tool_joint_name = '2J1'

        # Suscripciones
        self.create_subscription(Bool, '/button_detector', self.button_callback, 10)
        self.create_subscription(CompressedImage, image_topic, self.image_callback, 10)

        # Timer
        self.create_timer(0.05, self.control_loop)

        # Imagen
        self.latest_frame = None

        # Buffer para media móvil
        self.cx_buffer = []
        self.buffer_size = 5

        self.get_logger().info("Nodo PressButton iniciado")

    def button_callback(self, msg: Bool):
        if msg.data and not self.auto_mode:
            self.auto_mode = True
            self.state = "SEARCHING"
            self.get_logger().info("Modo automático activado")

    def image_callback(self, msg: CompressedImage):
        if not self.auto_mode or self.button_pressed:
            return

        np_arr = np.frombuffer(msg.data, np.uint8)
        frame = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)

        if frame is None:
            return

        # ROTACIÓN SOLO EN REAL
        if not self.is_simulation:
            frame = cv2.rotate(frame, cv2.ROTATE_180)

        self.latest_frame = frame

    def control_loop(self):
        if self.latest_frame is None or not self.auto_mode:
            return

        frame = self.latest_frame.copy()
        hsv = cv2.cvtColor(frame, cv2.COLOR_BGR2HSV)

        # Máscara rojo
        lower_red1 = np.array([0, 150, 150])     # Saturación y valor mínimos más altos
        upper_red1 = np.array([10, 255, 255])
        lower_red2 = np.array([170, 150, 150])
        upper_red2 = np.array([180, 255, 255])

        mask = cv2.inRange(hsv, lower_red1, upper_red1) | cv2.inRange(hsv, lower_red2, upper_red2)

        contours, _ = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

        if len(contours) == 0:
            self.get_logger().info("No se detecta botón rojo.")
            return

        largest = max(contours, key=cv2.contourArea)
        area = cv2.contourArea(largest)

        if area < 50:
            self.get_logger().info("Contorno muy pequeño, buscando...")
            return

        # Bounding box
        x, y, w, h = cv2.boundingRect(largest)
        cx = x + w // 2
        cy = y + h // 2

        # Media móvil
        self.cx_buffer.append(cx)
        if len(self.cx_buffer) > self.buffer_size:
            self.cx_buffer.pop(0)

        cx_filtered = int(sum(self.cx_buffer) / len(self.cx_buffer))

        # Dibujar
        cv2.rectangle(frame, (x, y), (x+w, y+h), (0,255,0), 2)
        cv2.circle(frame, (cx, cy), 5, (255,0,0), -1)
        cv2.circle(frame, (cx_filtered, cy), 5, (0,255,255), -1)

        cv2.imshow("Camera View", frame)
        cv2.waitKey(1)

        twist = Twist()

        # Estados
        if self.state == "SEARCHING":
            self.get_logger().info("Botón detectado → bajando herramienta")
            self.state = "LOWERING_TOOL"

        elif self.state == "LOWERING_TOOL":
            msg = JointState()
            msg.name = [self.tool_joint_name]
            msg.position = [72_500.0] # 72_500 robot
            self.joint_pub.publish(msg)

            self.get_logger().info("Herramienta bajada")
            self.state = "CENTERING"
            self.press_start_time = self.get_clock().now()

        elif self.state == "CENTERING":
            image_center_x = frame.shape[1] // 2
            error_x = cx_filtered - image_center_x

            Kp = 0.002
            dead_zone = 1

            if abs(error_x) > dead_zone:
                vel = -Kp * error_x
                vel = max(min(vel, 0.05), -0.05)

                twist.linear.y = vel
                self.cmd_pub.publish(twist)

                self.get_logger().info(f"Centrando... Error X: {error_x} | Vel: {vel:.3f}")
            else:
                twist.linear.y = 0.0
                self.cmd_pub.publish(twist)

                self.get_logger().info("Centrado REAL completado")
                self.state = "PRESSING"
                self.press_start_time = self.get_clock().now()

        elif self.state == "PRESSING":
            elapsed = (self.get_clock().now() - self.press_start_time).nanoseconds * 1e-9

            if elapsed < 8.0:
                twist.linear.x = 0.1
                self.cmd_pub.publish(twist)
            else:
                twist.linear.x = 0.0
                self.cmd_pub.publish(twist)

                self.get_logger().info("Acción completada")
                self.button_pressed = True
                rclpy.shutdown()
                exit(0)


def main(args=None):
    rclpy.init(args=args)
    node = PressButton()

    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        node.destroy_node()
        cv2.destroyAllWindows()
        rclpy.shutdown()


if __name__ == "__main__":
    main()
    exit(0)
