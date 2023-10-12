using System;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class ItemObject : MonoBehaviour
{
    [NonSerialized] public PlayerController user;
    Rigidbody2D rb;
    [NonSerialized] public bool canPickup = true;

    public GameObject visualObject;
    public Vector2 Offset;

    public float ThrowForce = 4;
    public float ThrowAngularForce = 5;

    public Vector2 AimDirection
    {
        get
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            return mousePos - (Vector2)user.transform.position;
        }
    }

    void LateUpdate()
    {
        UpdateAim();
        if (!canPickup)
            if(Vector3.Distance(this.transform.position, PlayerController.Instance.transform.position) > Inventory.Instance.itemPickupRange*2f)
                canPickup = true;
    }

    public virtual void Pickup(PlayerController player)
    {
        user = player;

        rb = rb != null ? rb : GetComponent<Rigidbody2D>();

        rb.isKinematic = true;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;
        transform.SetParent(player.transform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    void UpdateAim()
    {
        if (DoAim() && user != null)
        {
            visualObject.transform.localScale = new Vector3(1, 1, 1);
            transform.right = AimDirection;
            if (DoAimMirror() && transform.right.x < 0)
            {
                visualObject.transform.localScale = new Vector3(1, -1, 1);
            }
        }
    }

    public abstract bool DoAim();
    public abstract bool DoAimMirror();

    public abstract void ItemUpdate();

    public virtual void Throw(Vector2 direction)
    {
        rb.isKinematic = false;
        transform.localScale = Vector3.one; // reset scale
        transform.SetParent(null);
        rb.velocity = user.Velocity + (direction * ThrowForce);
        rb.AddTorque(ThrowAngularForce, ForceMode2D.Impulse);
        canPickup = false;
        user = null;
    }

}
