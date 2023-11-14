# HoloLens2-UR5 Pick and Place
This project aims to perform "Pick and Place" task with "Universal Robot 5" by using HoloLens 2 application to determine the pick and place position. <br />
We developed a HoloLens 2 app using Unity 3D and create TCP client that sends custom pose message to ROS2, and utilized the OpenCV library to achieve ArUco Marker detect functionality in order to perform registration between HoloLens 2 and UR5.

## Overview 
The application is a augmented reality tool that utilizes the Microsoft HoloLens 2 headset to communicate with ROS2 server via TCP connection. This application detects and tracks ArUco markers through the HoloLens 2's photo/video camera, and it allows users to control virtual cubes using drag and drop hand gestures. The virtual cubes represent pick and place positions, which users can adjust by moving them around. This makes the application user-friendly and enables users to easily control pick and place positions using augmented reality technology.

## Compatibility
- Unity 2019.4*
- Visual Studio 2019

## Structure

    ├── Assets                                      
    │   ├── Scripts                                     # Folder for all the C# scripts
    │   │   ├── MainController.cs                       # System initialization, camera frame capture, ArUco Marker detection
    │   │   ├── TcpClient.cs                            # Create TCP Client to send message to ROS2
    │   │   ├── ArUcoUtils.cs                           # Functions for perform calculations with OpenCV
    │   │   ├── UserDefinedCameraCalibrationParams.cs   # Define parameters for camera calibration
    │   │   └── ...
    │   ├── Plugins
    │   │   ├── OpenCvRuntimeComponent.winmd            # OpenCV Runtime Wrapper
    │   │   └── ...
    │   └── ...
    └── ...

## Build
1. Open this folder in Unity.
2. Go to Build Settings, switch target platform to `Universal Windows Platform`, select `HoloLens` for target device, and `ARM64` as the target platform.
3. In the `Scenes in Build`, select `MainScene`.
4. Build the Unity project in a new folder (e.g. App folder).
5. Save the changes. Open `App/Holo Ros Comm.sln`.
6. In Visual Studio 2019, change the configuration to `Release` and change the build platform to `ARM64`. 
    1. If you are deploying the app with cable plugged, select `Device` with a green start icon next the `ARM64`.
    2. If you want to deploy the app wirelessly, choose `Remote Machine` for the selection. Then, go to `Project > Properties > Configuration Properties > Debugging > Machine Name`, and enter the IP address of your Hololens 2.
7. Then go to `Debug > Start Without Debugging` to deploy the application.
8. Done!

## Demo
To watch the full demo video, please visit [HoloLens 2 Pick and Place with UR5](https://youtu.be/8-yKbps1ocE)
### Registration
<img src="/Demo/Registration.png" width="720">

### Setting Pick Position
<img src="/Demo/Pick.png" width="720">

### Setting Place Position
<img src="/Demo/Place.png" width="720">

### Pick and Place
<img src="/Demo/ShortPnP.gif" width="720">
