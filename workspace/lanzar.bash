source /opt/ros/humble/setup.bash
colcon build
source install/setup.bash
ros2 launch ros_tcp_endpoint endpoint.py
