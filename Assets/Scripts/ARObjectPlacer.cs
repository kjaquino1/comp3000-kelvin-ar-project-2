using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class ARObjectPlacer : MonoBehaviour
{
    [Header("Object Placement")]
    public GameObject objectToPlace;
    public int maxObjects = 10;

    private ARRaycastManager raycastManager;
    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private int objectsPlaced = 0;

    private void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        if (objectToPlace == null)
        {
            return;
        }

        if (objectsPlaced >= maxObjects)
        {
            return;
        }

        if (Touch.activeTouches.Count == 0)
        {
            return;
        }

        Touch touch = Touch.activeTouches[0];

        if (touch.phase != TouchPhase.Began)
        {
            return;
        }

        Vector2 touchPosition = touch.screenPosition;

        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            Instantiate(objectToPlace, hitPose.position, hitPose.rotation);
            objectsPlaced++;
        }
    }
}