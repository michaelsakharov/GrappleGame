using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FollowPlayer : MonoBehaviour
{
    public bool followX = true;
    public bool followY = true;
    public float startZoom = 5f;
    public float maxZoom = 10f;
    public float zoomMagnitude = 1f;
    public float zoomIncreaseSpeed = 1f;
    public float zoomDecreaseSpeed = 1f;
    Camera cam;

    PlayerController player => PlayerController.Instance;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographicSize = startZoom;
        if (followX && followY)
            transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10);
        else if (followX)
            transform.position = new Vector3(player.transform.position.x, transform.position.y, -10);
        else if (followY)
            transform.position = new Vector3(transform.position.x, player.transform.position.y, -10);
    }

    // Update is called once per frame
    void Update()
    {
        // follow player
        if (followX && followY)
            transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10);
        else if (followX)
            transform.position = new Vector3(player.transform.position.x, transform.position.y, -10);
        else if (followY)
            transform.position = new Vector3(transform.position.x, player.transform.position.y, -10);

        // increase camera zoom based on speed
        float targetZoom = startZoom + (player.Velocity.magnitude * zoomMagnitude);
        targetZoom = Mathf.Clamp(targetZoom, startZoom, maxZoom);
        if (cam.orthographicSize < targetZoom)
            cam.orthographicSize += zoomIncreaseSpeed * Time.deltaTime;
        else if (cam.orthographicSize > targetZoom)
            cam.orthographicSize -= zoomDecreaseSpeed * Time.deltaTime;
        // clamp
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, startZoom, maxZoom);
    }
}
