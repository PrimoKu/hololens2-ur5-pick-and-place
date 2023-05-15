using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.Input;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Experimental.UI;

#if ENABLE_WINMD_SUPPORT
using HL2UnityPlugin;
using Windows.Graphics.Imaging;
using Windows.Perception.Spatial;
using Windows.Storage.Streams;
#endif

public class MainController : MonoBehaviour
{
    public MediaCaptureUtility.MediaCaptureProfiles MediaCaptureProfiles;
    public ArUcoUtils.ArUcoDictionaryName ArUcoDictionaryName = ArUcoUtils.ArUcoDictionaryName.DICT_6X6_50;
    public ArUcoUtils.ArUcoTrackingType ArUcoTrackingType = ArUcoUtils.ArUcoTrackingType.Markers;
    public ArUcoUtils.CameraCalibrationParameterType CalibrationParameterType = ArUcoUtils.CameraCalibrationParameterType.UserDefined;
    public UserDefinedCameraCalibrationParams UserDefinedCalibParams;
    public ArUcoBoardPositions ArUcoBoardPositions;
    public Camera MainCamera;
    private Vector3 mCameraPos;
    public Text StatusBlock, TCPStatus;
    public GameObject cube, pickCube, placeCube;
    public TouchScreenKeyboard keyboard;
    private MediaCaptureUtility _MediaCaptureUtility;
    private bool _isRunning = false;
    public Matrix4x4 TransformUnityCamera { get; set; }
    public Matrix4x4 CameraToWorldUnity { get; set; }
    public Matrix4x4 TransformUnityWorld {get; set;}

    private Vector3 markerPosition, TmarkerPosition, pickPosition, placePosition;
    private Quaternion markerRotation, TmarkerRotation, pickRotation, placeRotation;
    private bool _detected = false;


#if ENABLE_WINMD_SUPPORT
    HL2ResearchMode researchMode;
    OpenCVRuntimeComponent.CvUtils CvUtils;
    private SpatialCoordinateSystem _unityCoordinateSystem = null;
    private SpatialCoordinateSystem _frameCoordinateSystem = null;
    private SpatialCoordinateSystem _LFframeCoordinateSystem = null;
#endif

    async void Start()
    {
        pickCube.transform.GetComponent<Renderer>().enabled = false;
        placeCube.transform.GetComponent<Renderer>().enabled = false;
        try
        {
#if ENABLE_WINMD_SUPPORT
            // Asynchronously start media capture
            await StartMediaCapture();
            researchMode = new HL2ResearchMode();
            
            // Configure the dll with input parameters
            CvUtils = new OpenCVRuntimeComponent.CvUtils(
                ArUcoBoardPositions.ComputeMarkerSizeForTrackingType(
                    ArUcoTrackingType, 
                    ArUcoBoardPositions.markerSizeForSingle,
                    ArUcoBoardPositions.markerSizeForBoard),
                ArUcoBoardPositions.numMarkers,
                (int)ArUcoDictionaryName,
                ArUcoBoardPositions.FillCustomObjectPointsFromUnity());
            Debug.Log("Created new instance of the cvutils class.");

            Debug.Log("Begin tracking in frame grab loop.");
            _isRunning = true;
#endif 
        }
        catch (Exception ex)
        {
            // StatusBlock.text = $"Error init: {ex.Message}";
            Debug.LogError($"Failed to start marker tracking: {ex}");
        }
    }


    void LateUpdate()
    {
        mCameraPos = MainCamera.transform.position;

        pickPosition = pickCube.transform.position;
        var pickAngles = pickCube.transform.rotation.eulerAngles;
        pickRotation = Quaternion.Euler(pickAngles);

        placePosition = placeCube.transform.position;
        var placeAngles = placeCube.transform.rotation.eulerAngles;
        placeRotation = Quaternion.Euler(placeAngles);

        cube.transform.GetComponent<Renderer>().enabled = false;

        #if ENABLE_WINMD_SUPPORT
        if (_MediaCaptureUtility.IsCapturing) {
            var mediaFrameReference = _MediaCaptureUtility.GetLatestFrame();
            HandleArUcoTracking(mediaFrameReference);
        } else {
            return;
        }
        #endif
    }

    private async Task StartMediaCapture()
    {
        StatusBlock.text = $"Starting camera...";

#if ENABLE_WINMD_SUPPORT
        try
        {
            Debug.Log("Creating MediaCaptureUtility and initializing frame reader.");
            _MediaCaptureUtility = new MediaCaptureUtility();
            await _MediaCaptureUtility.InitializeMediaFrameReaderAsync(MediaCaptureProfiles);
            StatusBlock.text = $"Camera started. Running!";
            Debug.Log("Successfully initialized frame reader.");
        }
        catch (Exception ex)
        {
            StatusBlock.text = $"Failed to start camera: {ex.Message}. Using loaded/picked image.";
        }

        try
        {
            _unityCoordinateSystem = Marshal.GetObjectForIUnknown(WorldManager.GetNativeISpatialCoordinateSystemPtr()) as SpatialCoordinateSystem;
            StatusBlock.text = $"Acquired unity coordinate system!";
            Debug.Log("Successfully cached pointer to Unity spatial coordinate system.");
        }
        catch (Exception ex)
        {
            StatusBlock.text = $"Failed to get Unity spatial coordinate system: {ex.Message}.";
        }
#endif
    }

#if ENABLE_WINMD_SUPPORT
    private void HandleArUcoTracking(Windows.Media.Capture.Frames.MediaFrameReference mediaFrameReference)
    {
        var softwareBitmap = mediaFrameReference?.VideoMediaFrame?.SoftwareBitmap;
        Debug.Log("Successfully requested software bitmap.");

        if (softwareBitmap != null)
        {
            var cameraProjectionTransform = mediaFrameReference.VideoMediaFrame.CameraIntrinsics.UndistortedProjectionTransform;
            Debug.Log($"_cameraProjectionTransform: {cameraProjectionTransform}");

            OpenCVRuntimeComponent.CameraCalibrationParams calibParams = 
                new OpenCVRuntimeComponent.CameraCalibrationParams(System.Numerics.Vector2.Zero, System.Numerics.Vector2.Zero, System.Numerics.Vector3.Zero, System.Numerics.Vector2.Zero, 0, 0);

            switch (CalibrationParameterType)
            {
                case ArUcoUtils.CameraCalibrationParameterType.UserDefined:
                    calibParams = new OpenCVRuntimeComponent.CameraCalibrationParams(
                        new System.Numerics.Vector2(UserDefinedCalibParams.focalLength.x, UserDefinedCalibParams.focalLength.y),
                        new System.Numerics.Vector2(UserDefinedCalibParams.principalPoint.x, UserDefinedCalibParams.principalPoint.y),
                        new System.Numerics.Vector3(UserDefinedCalibParams.radialDistortion.x, UserDefinedCalibParams.radialDistortion.y, UserDefinedCalibParams.radialDistortion.z),
                        new System.Numerics.Vector2(UserDefinedCalibParams.tangentialDistortion.x, UserDefinedCalibParams.tangentialDistortion.y),
                        (int)mediaFrameReference.VideoMediaFrame.CameraIntrinsics.ImageWidth,
                        (int)mediaFrameReference.VideoMediaFrame.CameraIntrinsics.ImageHeight);
                        Debug.Log($"User-defined calibParams: [{calibParams}]");
                    break;
                default:
                    break;
            }

            _frameCoordinateSystem = mediaFrameReference.CoordinateSystem;
            _LFframeCoordinateSystem = researchMode.GetRigNodeSpatialCoordinateSystem();
            Debug.Log($"_frameCoordinateSystem set from media frame reference");

            switch (ArUcoTrackingType)
            {
                case ArUcoUtils.ArUcoTrackingType.Markers:
                    DetectMarkers(softwareBitmap, calibParams);
                    break;

                case ArUcoUtils.ArUcoTrackingType.None:
                    StatusBlock.text = $"Not running tracking...";
                    break;

                default:
                    StatusBlock.text = $"No option selected for tracking...";
                    break;
            }
        }
        softwareBitmap?.Dispose();
    }
#endif

#if ENABLE_WINMD_SUPPORT
    private void DetectMarkers(SoftwareBitmap softwareBitmap, OpenCVRuntimeComponent.CameraCalibrationParams calibParams)
    {
        // StatusBlock.text = $"Start detecting...";
        var markers = CvUtils.DetectMarkers(softwareBitmap, calibParams);

        if(markers.Count != 0) {
            _detected = true; 
        } else {
            _detected = false;
        }

        foreach (var marker in markers)
        {
            TransformUnityCamera = ArUcoUtils.GetTransformInUnityCamera(ArUcoUtils.Vec3FromFloat3(marker.Position), ArUcoUtils.RotationQuatFromRodrigues(ArUcoUtils.Vec3FromFloat3(marker.Rotation)));
            // var mp = ArUcoUtils.GetVectorFromMatrix(TransformUnityCamera);
            // var mr = ArUcoUtils.GetQuatFromMatrix(TransformUnityCamera); 

            markerPosition = ArUcoUtils.Vec3FromFloat3(marker.Position);
            markerRotation = ArUcoUtils.RotationQuatFromRodrigues(ArUcoUtils.Vec3FromFloat3(marker.Rotation));

            CameraToWorldUnity = GetViewToUnityTransform(_frameCoordinateSystem);
            TransformUnityWorld = CameraToWorldUnity * TransformUnityCamera;

            // StatusBlock.text = $"Detected marker ID: {marker.Id} \n Position: {ArUcoUtils.GetVectorFromMatrix(TransformUnityWorld)} \n Rotation: {ArUcoUtils.GetQuatFromMatrix(TransformUnityWorld)} ";
            TmarkerPosition = ArUcoUtils.GetVectorFromMatrix(TransformUnityWorld);
            TmarkerRotation = ArUcoUtils.GetQuatFromMatrix(TransformUnityWorld);

            cube.transform.SetPositionAndRotation(TmarkerPosition, TmarkerRotation);
            // StatusBlock.text = $"Position: {TmarkerPosition.x}, {TmarkerPosition.y}, {TmarkerPosition.z}";
            // StatusBlock.text = $"APosition: {markerPosition.x}, {markerPosition.y}, {markerPosition.z}\n TPosition: {mp.x}, {mp.y}, {mp.z}\n CPosition: {TmarkerPosition.x}, {TmarkerPosition.y}, {TmarkerPosition.z}";
            cube.transform.GetComponent<Renderer>().enabled = true;
        }

    }
#endif

#if ENABLE_WINMD_SUPPORT
    private Matrix4x4 GetViewToUnityTransform(SpatialCoordinateSystem frameCoordinateSystem){
        if (frameCoordinateSystem == null || _unityCoordinateSystem == null) {
            return Matrix4x4.identity;
        }

        System.Numerics.Matrix4x4? cameraToUnityRef = frameCoordinateSystem.TryGetTransformTo(_unityCoordinateSystem);
        System.Numerics.Matrix4x4? LfcameraToUnityRef = _LFframeCoordinateSystem.TryGetTransformTo(_unityCoordinateSystem);

        if (!cameraToUnityRef.HasValue)
            return Matrix4x4.identity;

        var viewToCamera = Matrix4x4.identity;
        var cameraToUnity = ArUcoUtils.Mat4x4FromFloat4x4(cameraToUnityRef.Value);
        var LfcameraToUnity = ArUcoUtils.Mat4x4FromFloat4x4(LfcameraToUnityRef.Value);

        // StatusBlock.text = $"{LfcameraToUnity[0,0]}, {LfcameraToUnity[0,1]}, {LfcameraToUnity[0,2]}, {LfcameraToUnity[0,3]}\n{LfcameraToUnity[1,0]}, {LfcameraToUnity[1,1]}, {LfcameraToUnity[1,2]}, {LfcameraToUnity[1,3]}\n{LfcameraToUnity[2,0]}, {LfcameraToUnity[2,1]}, {LfcameraToUnity[2,2]}, {LfcameraToUnity[2,3]}\n{LfcameraToUnity[3,0]}, {LfcameraToUnity[3,1]}, {LfcameraToUnity[3,2]}, {LfcameraToUnity[3,3]}\n";
        // TCPStatus.text = $"{cameraToUnity[0,0]}, {cameraToUnity[0,1]}, {cameraToUnity[0,2]}, {cameraToUnity[0,3]}\n{cameraToUnity[1,0]}, {cameraToUnity[1,1]}, {cameraToUnity[1,2]}, {cameraToUnity[1,3]}\n{cameraToUnity[2,0]}, {cameraToUnity[2,1]}, {cameraToUnity[2,2]}, {cameraToUnity[2,3]}\n{cameraToUnity[3,0]}, {cameraToUnity[3,1]}, {cameraToUnity[3,2]}, {cameraToUnity[3,3]}\n";

        var viewToUnityWinRT = viewToCamera * cameraToUnity;
        var viewToUnity = Matrix4x4.Transpose(viewToUnityWinRT);
        viewToUnity.m20 *= -1.0f;
        viewToUnity.m21 *= -1.0f;
        viewToUnity.m22 *= -1.0f;
        viewToUnity.m23 *= -1.0f;

        return viewToUnity;
    }
#endif

    public PoseMsg MarkerPose() {
        PoseMsg markerPose = new PoseMsg(
            "marker_pose", 
            new Position(-(double)TmarkerPosition.x, (double)TmarkerPosition.y, (double)TmarkerPosition.z), 
            new Rotation((double)TmarkerRotation.x, (double)TmarkerRotation.y, (double)TmarkerRotation.z, (double)TmarkerRotation.w));
        return markerPose;
    } 

    public PoseMsg PickPose() {
        PoseMsg markerPose = new PoseMsg(
            "pick_pose",
            new Position(-(double)pickPosition.x, (double)pickPosition.y, (double)pickPosition.z), 
            new Rotation((double)pickRotation.x, (double)pickRotation.y, (double)pickRotation.z, (double)pickRotation.w));
        return markerPose;
    } 

    public PoseMsg PlacePose() {
        PoseMsg markerPose = new PoseMsg(
            "place_pose",
            new Position(-(double)placePosition.x, (double)placePosition.y, (double)placePosition.z), 
            new Rotation((double)placeRotation.x, (double)placeRotation.y, (double)placeRotation.z, (double)placeRotation.w));
        return markerPose;
    } 

    #region Button Callback
    public void OpenSystemKeyboard()
    {
        keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.NumberPad, false, false, false, false);
    }

    public void SpawnPick() {
        pickCube.transform.position = new Vector3(mCameraPos.x, mCameraPos.y - 0.2f, mCameraPos.z + 0.5f);
        pickCube.transform.GetComponent<Renderer>().enabled = true;
    }

    public void SpawnPlace() {
        placeCube.transform.position = new Vector3(mCameraPos.x, mCameraPos.y - 0.2f, mCameraPos.z + 0.5f);
        placeCube.transform.GetComponent<Renderer>().enabled = true;
    }
    
    #endregion

}
