using FishNet.Object;
using UnityEngine;
using UnityEngine.UIElements;

public class CameraSwitch : NetworkBehaviour
{
    [SerializeField] GameObject echolocationCamera;
    public bool canSwitchCamera = true;

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Tab) && canSwitchCamera == true)
        {
            if (echolocationCamera.activeSelf == true)
            {
                echolocationCamera.SetActive(false);
            }
            else
            {
                echolocationCamera.SetActive(true);
            }
        }
    }

    public void setToVisionCamera() {
        echolocationCamera.SetActive(false);
    }
}
