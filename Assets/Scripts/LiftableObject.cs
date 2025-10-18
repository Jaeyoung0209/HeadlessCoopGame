using FishNet.Managing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class LiftableObject : NetworkBehaviour
{
    public Rigidbody rb;
    private NetworkObject no;
    private BoxCollider object_collider;

    public readonly SyncVar<bool> isBeingHeld = new SyncVar<bool>();
    public readonly SyncVar<int> holdingPlayerId = new SyncVar<int>();

    public Transform leftHandPosition;
    public Transform rightHandPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        no = GetComponent<NetworkObject>();
        object_collider = GetComponent<BoxCollider>();
    }

    private void ApplyPickup(int playerId, NetworkObject handNo)
    {
        isBeingHeld.Value = true;
        holdingPlayerId.Value = playerId;

        rb.isKinematic = true;
        // object_collider.enabled = false;

        if (handNo != null)
        {
            no.SetParent(handNo);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }

    private void ApplyDrop(Vector3 dropPosition)
    {
        isBeingHeld.Value = false;
        holdingPlayerId.Value = -1;

        no.UnsetParent();
        // object_collider.enabled = true;
        rb.isKinematic = false;
        rb.position = dropPosition;
    }

    [ServerRpc(RequireOwnership = false)]
    public void PickupServerRpc(int playerId, GameObject handTransform)
    {
        var handNo = handTransform.GetComponent<NetworkObject>();
        if (handNo != null)
        {
            // Update state on server
            isBeingHeld.Value = true;
            holdingPlayerId.Value = playerId;

            // Broadcast to everyone (including host)
            PickupObserversRpc(playerId, handNo.ObjectId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DropServerRpc(Vector3 dropPosition)
    {
        isBeingHeld.Value = false;
        holdingPlayerId.Value = -1;

        DropObserversRpc(dropPosition);
    }

    [ObserversRpc]
    private void PickupObserversRpc(int playerId, int handTransformId)
    {
        NetworkObject handNo = null;

        // First try client-side lookup
        if (
            NetworkManager.ClientManager.Objects.Spawned.TryGetValue(
                handTransformId,
                out var clientObj
            )
        )
        {
            handNo = clientObj;
        }
        // Fallback to server-side lookup (in case host/server context)
        else if (
            NetworkManager.ServerManager.Objects.Spawned.TryGetValue(
                handTransformId,
                out var serverObj
            )
        )
        {
            handNo = serverObj;
        }

        if (handNo != null)
        {
            ApplyPickup(playerId, handNo);
        }
        else
        {
            Debug.LogWarning(
                $"[LiftableObject] Could not find NetworkObject with ID {handTransformId}"
            );
        }
    }

    [ObserversRpc]
    private void DropObserversRpc(Vector3 dropPosition)
    {
        ApplyDrop(dropPosition);
    }
}
