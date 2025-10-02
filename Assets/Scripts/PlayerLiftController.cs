using UnityEngine;
using FishNet.Object;
using UnityEngine.Animations.Rigging;
using System.Collections;
using FishNet.Demo.AdditiveScenes;

public class PlayerLiftController : NetworkBehaviour
{
    public GameObject handTransform;
    private LiftableObject heldObject;

    private CameraSwitch cameraSwitcher;
    private Collider playerCollider;
    private RigBuilder rigbuilder;
    [SerializeField] private TwoBoneIKConstraint rightHandIK;
    [SerializeField] private TwoBoneIKConstraint leftHandIK;
    [SerializeField] private Transform rightHandGripTarget;
    [SerializeField] private Transform leftHandGripTarget;

    [SerializeField] private float pickupRange = 3f;

    [SerializeField] private LayerMask objectLayer;

    public override void OnStartClient()
    {
        cameraSwitcher = gameObject.GetComponent<CameraSwitch>();
        playerCollider = gameObject.GetComponent<Collider>();
        rigbuilder = gameObject.GetComponent<RigBuilder>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldObject == null)
            {
                TryPickup();
            }
            else
            {
                Drop();
            }
        }
    }

    private void LateUpdate()
    {
        if (heldObject != null)
        {
            leftHandGripTarget.transform.position = heldObject.leftHandPosition.position;
            rightHandGripTarget.transform.position = heldObject.rightHandPosition.position;
        }
    }

    private void TryPickup()
    {

        Collider closestCollider = null;
        float minDistance = float.MaxValue;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupRange, objectLayer);

        foreach (Collider hit in hitColliders)
        {
            if (hit.gameObject == this.gameObject)
            {
                continue;
            }
            float distance = Vector3.Distance(transform.position, hit.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestCollider = hit;
            }
        }

        if (closestCollider != null) {
            LiftableObject obj = closestCollider.GetComponent<LiftableObject>();
            if (obj != null)
            {

                if (leftHandGripTarget != null && rightHandGripTarget != null)
                {
                    StartCoroutine(ReachForObject(obj));
                }
                
                heldObject = obj;
                if (heldObject.gameObject.name == "CameraObject")
                {
                    cameraSwitcher.canSwitchCamera = false;
                    cameraSwitcher.setToVisionCamera();
                }
            }
        }

    }


    private void Drop()
    {
        if (heldObject != null)
        {
            heldObject.DropServerRpc(heldObject.transform.position);
            EnableCollisionsWithHeldObject();
            heldObject = null;
            cameraSwitcher.canSwitchCamera = true;
            leftHandIK.weight = 0;
            rightHandIK.weight = 0;
        }
    }

    private IEnumerator ReachForObject(LiftableObject obj)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.3f;
            leftHandIK.weight = Mathf.Lerp(0f, 1f, t);
            rightHandIK.weight = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        obj.PickupServerRpc(Owner.ClientId, handTransform);
        DisableCollisionsWithHeldObject(obj);
    }

    private void DisableCollisionsWithHeldObject(LiftableObject obj)
    {
        Physics.IgnoreCollision(obj.GetComponent<Collider>(), playerCollider, true);
    }

    private void EnableCollisionsWithHeldObject()
    {
        if (heldObject != null)
            Physics.IgnoreCollision(heldObject.GetComponent<Collider>(), playerCollider, false);
    }
}
