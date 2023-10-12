using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    public LayerMask itemLayer;
    public float itemPickupRange = 1f;

    [HideInInspector]
    public ItemObject curItem = null;

    public static bool IsHoldingItem => Instance.curItem != null;
    public static Vector2 ItemDirection => Instance.curItem.AimDirection;
    public static ItemObject HeldItem => Instance.curItem;

    void Awake()
    {
        Instance = this;
        curItem = null;
    }

    // Update is called once per frame
    void Update()
    {
        // Look for item
        if (curItem == null)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, itemPickupRange, itemLayer);
            foreach (Collider2D col in colliders)
            {
                ItemObject item = col.GetComponent<ItemObject>();
                if (item != null && item.canPickup)
                {
                    curItem = item;
                    curItem.Pickup(PlayerController.Instance);
                    break;
                }
            }
        }
        else
        {
            if (Input.GetButtonDown("ThrowItem"))
            {
                curItem.Throw((Vector2)(Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position).normalized);
                curItem = null;
            }
        }


        if (curItem != null && !GameManager.PlayerFinished && !GameManager.PlayerDead)
            curItem.ItemUpdate();
    }
}
