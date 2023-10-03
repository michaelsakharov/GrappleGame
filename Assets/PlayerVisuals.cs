using System;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.VisualScripting.Member;
using static UnityEditor.PlayerSettings;

[ExecuteAlways]
public class PlayerVisuals : MonoBehaviour
{
    public Vector2Int body = new Vector2Int(0, 0);
    public Texture2D Body;
    [Space]
    public Vector2Int head = new Vector2Int(1, 6);
    public Texture2D HeadTex;
    [Space]
    public Vector2Int leftFoot = new Vector2Int(0, 0);
    public Vector2Int rightFoot = new Vector2Int(0, 0);
    public Vector2Int leftHand = new Vector2Int(0, 0);
    public Vector2Int rightHand = new Vector2Int(0, 0);
    public Texture2D LimbTex;
    [Space]
    public Vector2Int leftEarRoot = new Vector2Int(0, 0);
    public Vector2Int rightEarRoot = new Vector2Int(0, 0);
    public Vector2Int leftEarMiddle = new Vector2Int(0, 0);
    public Vector2Int rightEarMiddle = new Vector2Int(0, 0);
    public Vector2Int leftEarEnd = new Vector2Int(0, 0);
    public Vector2Int rightEarEnd = new Vector2Int(0, 0);
    public Texture2D EarTex;
    public Texture2D BottomEarTex;
    [Space]
    [Space]
    public float headSpringStrength = 10f;
    public float earSpringStrength = 10f;
    public float earMiddleSpringStrength = 10f;
    public float limbSpringStrength = 10f;
    public float bodySpringStrength = 5f;
    Vector2 b = new Vector2(0, 0);
    Vector2 h = new Vector2(0, 0);
    Vector2 lF = new Vector2(0, 0);
    Vector2 rF = new Vector2(0, 0);
    Vector2 lH = new Vector2(0, 0);
    Vector2 rH = new Vector2(0, 0);
    Vector2 lEMiddle = new Vector2(0, 0);
    Vector2 rEMiddle = new Vector2Int(0, 0);
    Vector2 lEEnd = new Vector2(0, 0);
    Vector2 rEEnd = new Vector2Int(0, 0);

    public Vector2Int res;
    public float scale = 1;
    public float angle;
    public Texture2D player;
    public Material mat;
    public string matPropName = "_MainTex";
    Color32[] clearColorCache;

    Vector2 prevPos = Vector2.zero;
    Vector2 vel = Vector2.zero;
    bool isFlipped = false;
    bool hasHooked = false;

    private void OnValidate()
    {
        OnEnable();
    }

    void OnEnable()
    {
        clearColorCache = new Color32[res.x * res.y];
        for (int x = 0; x < res.x; x++)
            for (int y = 0; y < res.y; y++)
                clearColorCache[x + y * res.x] = new Color32(0, 0, 0, 0);
        prevPos = transform.position;
        b = body;
        h = head;
        lF = leftFoot;
        rF = rightFoot;
        lH = leftHand;
        rH = rightHand;
        lEMiddle = leftEarMiddle;
        rEMiddle = rightEarMiddle;
        lEEnd = leftEarEnd;
        rEEnd = rightEarEnd;
        FixedUpdate();
    }

    private void LateUpdate()
    {
        if (PlayerController.Instance == null) return;
        if (!hasHooked)
            PlayerController.Instance.OnImpact += OnImpact;

        // do Mirror set out scale to -1
        //if (PlayerController.Instance.IsGrappling)
        //{
        //    if (PlayerController.Instance.GrapplePosition.x < transform.position.x)
        //        isFlipped = true;
        //    else if (PlayerController.Instance.GrapplePosition.x > transform.position.x)
        //        isFlipped = false;
        //}
        //else
        {
            if (PlayerController.Instance.Velocity.x < 0)
                isFlipped = true;
            else if (PlayerController.Instance.Velocity.x > 0)
                isFlipped = false;
        }

        if (isFlipped)
            this.transform.localScale = new Vector3(-scale, scale, scale);
        else
            this.transform.localScale = new Vector3(scale, scale, scale);

        vel.y = transform.position.y - prevPos.y;
        if (!isFlipped)
            vel.x = transform.position.x - prevPos.x;
        else
            vel.x = prevPos.x - transform.position.x;
        // adjust by time
        vel /= Time.deltaTime;
        prevPos = transform.position;

        if (PlayerController.Instance.IsGrappling)
        {
            // Set hand to be at some point along the Grappling direction
            lH = (PlayerController.Instance.GrappleDirection.normalized * 8f);

            Vector2 pixelSize = new Vector2(scale / res.x, scale / res.y);
            Vector2 localOffset = new Vector2(lH.x * pixelSize.x, lH.y * pixelSize.y) / 2f;
            Vector2 worldSpacePosition = localOffset + (Vector2)transform.position;
            PlayerController.Instance.line.SetPosition(0, worldSpacePosition);

            if (isFlipped)
                lH.x *= -1;

            doBounce();
        }
        else if (!PlayerController.Instance.IsGrounded)
        {
            ApplySpringPhysics(ref lH, leftHand, limbSpringStrength);
            doBounce();
        }
        else if (PlayerController.Instance.IsGrounded)
        {
            b = body;
            h = head;
            lF = leftFoot;
            rF = rightFoot;
            lH = leftHand;
            rH = rightHand;
            if (PlayerController.Instance.IsHoldingItem)
                HoldItem();
            lEMiddle = leftEarMiddle;
            rEMiddle = rightEarMiddle;
            lEEnd = leftEarEnd;
            rEEnd = rightEarEnd;
        }

    }

    private void OnImpact(Vector2 vel)
    {
        if (isFlipped)
            vel.x *= -1;

        AddForceToLimb(ref b, body, vel, 3, 3);
        AddForceToLimb(ref h, head, vel, 6, 3);
        AddForceToLimb(ref lF, leftFoot, vel);
        AddForceToLimb(ref rF, rightFoot, vel);
        AddForceToLimb(ref lH, leftHand, vel);
        AddForceToLimb(ref rH, rightHand, vel);
        AddForceToLimb(ref lEMiddle, leftEarMiddle, vel, 8, 3);
        AddForceToLimb(ref rEMiddle, rightEarMiddle, vel, 8, 3);
        AddForceToLimb(ref lEEnd, leftEarEnd, vel, 10, 4);
        AddForceToLimb(ref rEEnd, rightEarEnd, vel, 10, 4);
    }

    void doBounce()
    {
        if (!PlayerController.Instance.IsHoldingItem)
            ApplySpringPhysics(ref rH, rightHand, limbSpringStrength);
        else
            HoldItem();

        ApplySpringPhysics(ref b, body, bodySpringStrength, 3, 3);
        ApplySpringPhysics(ref h, head, headSpringStrength, 6, 3);
        ApplySpringPhysics(ref lF, leftFoot, limbSpringStrength);
        ApplySpringPhysics(ref rF, rightFoot, limbSpringStrength);
        ApplySpringPhysics(ref lEMiddle, leftEarMiddle, earMiddleSpringStrength, 8, 3);
        ApplySpringPhysics(ref rEMiddle, rightEarMiddle, earMiddleSpringStrength, 8, 3);
        ApplySpringPhysics(ref lEEnd, leftEarEnd, earSpringStrength, 10, 4);
        ApplySpringPhysics(ref rEEnd, rightEarEnd, earSpringStrength, 10, 4);
    }

    void HoldItem()
    {
        rH = (PlayerController.Instance.ItemDirection.normalized * 8f);

        Vector2 pixelSize = new Vector2(scale / res.x, scale / res.y);
        Vector2 localOffset = new Vector2(rH.x * pixelSize.x, rH.y * pixelSize.y) / 2f;
        // PlayerController.Instance.HeldItem.Offset
        // thats the pivot point of the item
        // so we need to offset the item to move the items Pivot/Offset to be the same as the hand
        // its in local space to the item
        var offset = PlayerController.Instance.HeldItem.Offset;
        if (PlayerController.Instance.ItemDirection.x < 0)
            offset.y *= -1;
        Vector2 itemPivot = PlayerController.Instance.HeldItem.transform.TransformPoint(offset);
        Vector2 itemOffset = itemPivot - (Vector2)PlayerController.Instance.HeldItem.transform.position;
        PlayerController.Instance.HeldItem.transform.localPosition = localOffset - itemOffset;

        // Flip this after for use in Rendering
        if (isFlipped)
            rH.x *= -1;
    }

    void FixedUpdate()
    {
        UpdateTexture();
        mat.SetTexture(matPropName, player);
    }

    void ApplySpringPhysics(ref Vector2 limbPosition, Vector2 targetPosition, float springStrength, float maxHorDist = 6, float maxVertDist = 6)
    {
        Vector2 displacement = targetPosition - limbPosition;
        if (displacement.magnitude < 1.5)
            displacement *= 0.0f;

        // Apply spring force
        Vector2 springForce = springStrength * displacement;

        // Apply damping based on player's velocity
        Vector2 dampingForce = -vel * springStrength;

        // Calculate total force
        Vector2 totalForce = springForce + dampingForce;

        // Update limb position based on the forces
        limbPosition += totalForce * Time.deltaTime;
        //limbPosition = Vector2.MoveTowards(limbPosition, targetPosition, totalForce.magnitude * Time.deltaTime);

        // Distance was split into horizontal and vertical
        limbPosition = HandleMaxDist(limbPosition, targetPosition, maxHorDist, maxVertDist);
    }

    void AddForceToLimb(ref Vector2 limbPosition, Vector2 targetPosition, Vector2 force, float maxHorDist = 6, float maxVertDist = 6)
    {
        // Update limb position based on the forces
        limbPosition += force;

        // Distance was split into horizontal and vertical
        limbPosition = HandleMaxDist(limbPosition, targetPosition, maxHorDist, maxVertDist);
    }

    Vector2 HandleMaxDist(Vector2 limbPosition, Vector2 targetPosition, float maxHorDist, float maxVertDist)
    {
        if (Mathf.Abs(limbPosition.x - targetPosition.x) > maxHorDist)
        {
            float xDistToTargetSign = Mathf.Sign(limbPosition.x - targetPosition.x);
            limbPosition.x = targetPosition.x + (maxHorDist * xDistToTargetSign);

        }
        if (Mathf.Abs(limbPosition.y - targetPosition.y) > maxVertDist)
        {
            float yDistToTargetSign = Mathf.Sign(limbPosition.y - targetPosition.y);
            limbPosition.y = targetPosition.y + (maxVertDist * yDistToTargetSign);
        }

        return limbPosition;
    }

    void UpdateTexture()
    {
        if (player == null || player.IsDestroyed())
            player = new Texture2D(res.x, res.y, TextureFormat.RGBA32, false);
        else if (player.width != res.x || player.height != res.y)
        {
            DestroyImmediate(player);
            player = new Texture2D(res.x, res.y, TextureFormat.RGBA32, false);
        }
        player.filterMode = FilterMode.Point;
        player.wrapMode = TextureWrapMode.Clamp;
        player.SetPixels32(clearColorCache);

        // Stamp the body into the center
        var c = new Vector2Int((res.x / 2), (res.y / 2));
        Stamp(Body, c + new Vector2Int((int)b.x, (int)b.y));
        Stamp(HeadTex, c + new Vector2Int((int)h.x, (int)h.y));
        Stamp(LimbTex, c + new Vector2Int((int)lF.x, (int)lF.y));
        Stamp(LimbTex, c + new Vector2Int((int)rF.x, (int)rF.y));
        Stamp(LimbTex, c + new Vector2Int((int)lH.x, (int)lH.y));
        Stamp(LimbTex, c + new Vector2Int((int)rH.x, (int)rH.y));

        //float angle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
        //Stamp(EarTex, c + new Vector2Int((int)lE.x, (int)lE.y));
        //Stamp(BottomEarTex, c + new Vector2Int((int)rE.x, (int)rE.y));
        // Procedurally Draw Ears
        var headOf = new Vector2Int((int)h.x, (int)h.y);
        var inside = new Color32(255, 106, 0, 255);
        var neighbors = new Color32(247, 247, 247, 255);
        DrawLine(c + leftEarRoot + headOf, c + new Vector2Int((int)lEMiddle.x, (int)lEMiddle.y), inside, neighbors);
        DrawLine(c + rightEarRoot + headOf, c + new Vector2Int((int)rEMiddle.x, (int)rEMiddle.y), inside, neighbors);
        DrawLine(c + new Vector2Int((int)lEMiddle.x, (int)lEMiddle.y), c + new Vector2Int((int)lEEnd.x, (int)lEEnd.y), inside, neighbors);
        DrawLine(c + new Vector2Int((int)rEMiddle.x, (int)rEMiddle.y), c + new Vector2Int((int)rEEnd.x, (int)rEEnd.y), inside, neighbors);

        // Add an Outline
        for (int x = 0; x < res.x; x++)
            for (int y = 0; y < res.y; y++)
            {
                var col = player.GetPixel(x, y);
                bool isEdge = false;
                if (col.a > 0.5)
                {
                    isEdge |= player.GetPixel(x + 1, y).a < 0.5;
                    isEdge |= player.GetPixel(x - 1, y).a < 0.5;
                    isEdge |= player.GetPixel(x, y + 1).a < 0.5;
                    isEdge |= player.GetPixel(x, y - 1).a < 0.5;
                    if (isEdge)
                        player.SetPixel(x, y, Color.black);
                }
            }

        player.Apply();
    }

    void Stamp(Texture2D tex, Vector2Int offset, bool doMirror = false)
    {
        //offset += new Vector2Int((int)vel.x * 2, (int)vel.y * 2);
        int halfWidth = tex.width / 2;
        int halfHeight = tex.height / 2;
        for (int x = 0; x < tex.width; x++)
            for (int y = 0; y < tex.height; y++)
            {
                var col = tex.GetPixel(x, y);
                if (col.a > 0.5)
                {
                    // Calculate mirrored x coordinate
                    int mirroredX = doMirror ? tex.width - x - 1 : x;

                    player.SetPixel(
                        x - halfWidth + offset.x,
                        y - halfHeight + offset.y,
                        tex.GetPixel(mirroredX, y) // Stamp with mirrored pixel
                    );
                }
            }
    }

    void DrawLine(Vector2Int start, Vector2Int end, Color32 color, Color32 neighborColor)
    {
        int x = start.x;
        int y = start.y;
        int dx = Mathf.Abs(end.x - start.x);
        int dy = Mathf.Abs(end.y - start.y);
        int sx = (start.x < end.x) ? 1 : -1;
        int sy = (start.y < end.y) ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            bool isLast = x == end.x && y == end.y;
            player.SetPixel(x, y, color);

            // Color in each Neighbor pixel
            if (!isLast)
                player.SetPixel(x + 1, y, neighborColor);
            player.SetPixel(x - 1, y, neighborColor);
            player.SetPixel(x - 2, y, new Color32(128, 128, 128, 255));
            player.SetPixel(x - 1, y + 1, neighborColor);
            //player.SetPixel(x, y - 1, neighborColor);

            if (isLast) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x += sx; }
            if (e2 < dx) { err += dx; y += sy; }
        }
    }

}
