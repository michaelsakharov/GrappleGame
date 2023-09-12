using System.Collections.Generic;
using UnityEngine;

public enum WeaponType
{
    Projectile,
    Raycast
}
public enum Auto
{
    Full,
    Semi
}

/// <summary>
/// This is the core script that is used to create weapons.  There are 3 basic
/// types of weapons that can be made with this script:
/// 
/// Raycast - Uses raycasting to make instant hits where the weapon is pointed starting at
/// the position of raycastStartSpot
/// 
/// Projectile - Instantiates projectiles and lets them handle things like damage and accuracy.
/// </summary>


public class GunItem : ItemObject
{
    // Weapon Type
    public WeaponType type = WeaponType.Projectile;     // Which weapon system should be used

    // Auto
    public Auto auto = Auto.Full;                       // How does this weapon fire - semi-auto or full-auto

    // General
    [Space]
    public Transform raycastStartSpot;                  // The spot that the raycasting weapon system should use as an origin for rays
    public LayerMask hittableLayers;                    // Layers that will register hits from raycasts
    public float delayBeforeFire = 0.0f;                // An optional delay that causes the weapon to fire a specified amount of time after it normally would (0 for no delay)

    // Projectile
    [Space]
    public GameObject projectile;                       // The projectile to be launched (if the type is projectile)
    public Transform projectileSpawnSpot;               // The spot where the projectile should be instantiated

    // Power
    [Space]
    public float power = 80.0f;                         // The amount of power this weapon has (how much damage it can cause) (if the type is raycast)
    public float forceMultiplier = 10.0f;               // Multiplier used to change the amount of force applied to rigid bodies that are shot

    // Range
    [Space]
    public float range = 9999.0f;                       // How far this weapon can shoot (for raycast)

    // Rate of Fire
    [Space]
    public float rateOfFire = 10;                       // The number of rounds this weapon fires per second
    private float actualROF;                            // The frequency between shots based on the rateOfFire
    private float fireTimer;                            // Timer used to fire at a set frequency

    // Ammo
    [Space]
    public bool infiniteAmmo = false;                   // Whether or not this weapon should have unlimited ammo
    public int ammoCapacity = 12;                       // The number of rounds this weapon can fire before it has to reload
    public int shotPerRound = 1;                        // The number of "bullets" that will be fired on each round.  Usually this will be 1, but set to a higher number for things like shotguns with spread
    private int currentAmmo;                            // How much ammo the weapon currently has
    public float reloadTime = 2.0f;                     // How much time it takes to reload the weapon
    public bool showCurrentAmmo = true;                 // Whether or not the current ammo should be displayed in the GUI
    public TMPro.TMP_Text ammoText;                     // Text object that will display the current ammo

    // Accuracy
    [Space]
    [Header("Accuracy goes from 0-100, 100 means no spread, 0 means maximum spread!")]
    public float accuracy = 80.0f;                      // How accurate this weapon is on a scale of 0 to 100
    private float currentAccuracy;                      // Holds the current accuracy.  Used for varying accuracy based on speed, etc.
    public float accuracyDropPerShot = 1.0f;            // How much the accuracy will decrease on each shot
    public float accuracyRecoverRate = 0.1f;            // How quickly the accuracy recovers after each shot (value between 0 and 1)
    public float spread = 0.0f;                         // The amount of spread (in degrees) that the weapon has.  0 for no spread.  This is only used for raycast weapons

    // Burst
    [Space]
    public int burstRate = 3;                           // The number of shots fired per each burst
    public float burstPause = 0.0f;                     // The pause time between bursts
    private int burstCounter = 0;                       // Counter to keep track of how many shots have been fired per burst
    private float burstTimer = 0.0f;                    // Timer to keep track of how long the weapon has paused between bursts

    // Recoil
    [Space]
    public float recoilForce = 0.0f;                    // The force to apply to the player

    // Effects
    [Space]
    public ParticleSystem shells;                       // The Particle System for Shells
    public bool makeMuzzleEffects = true;				// Whether or not the weapon should make muzzle effects
    public GameObject[] muzzleEffects =
        new GameObject[] { null };                      // Effects to appear at the muzzle of the gun (muzzle flash, smoke, etc.)
    public Transform muzzleEffectsPosition;             // The spot where the muzzle effects should appear from
    public bool makeHitEffects = true;                  // Whether or not the weapon should make hit effects
    public GameObject[] hitEffects =
        new GameObject[] { null };                      // Effects to be displayed where the "bullet" hit
    public bool makeShotTrails = true;                  // Whether or not the weapon should make shot trails
    public Material trailMaterial;
    public Color trailColor = Color.white;
    public float trailWidth = 0.5f;
    public float trailDuration = 2f;

    // Crosshairs
    [Space]
    public bool showCrosshair = true;                   // Whether or not the crosshair should be displayed
    public Texture2D crosshairTexture;                  // The texture used to draw the crosshair
    public int crosshairLength = 10;                    // The length of each crosshair line
    public int crosshairWidth = 4;                      // The width of each crosshair line
    public float startingCrosshairSize = 10.0f;         // The gap of space (in pixels) between the crosshair lines (for weapon inaccuracy)
    private float currentCrosshairSize;                 // The gap of space between crosshair lines that is updated based on weapon accuracy in realtime

    // Audio
    [Space]
    public AudioClip fireSound;                         // Sound to play when the weapon is fired
    public AudioClip reloadSound;                       // Sound to play when the weapon is reloading
    public AudioClip dryFireSound;                      // Sound to play when the user tries to fire but is out of ammo
    AudioSource audioSource;                            // Reference to the audio source component

    // Other
    private bool canFire = true;                        // Whether or not the weapon can currently fire (used for semi-auto weapons)
    private List<LineRenderer> shotTrails = new();


    // Use this for initialization
    void Start()
    {
        // Calculate the actual ROF to be used in the weapon systems.  The rateOfFire variable is
        // designed to make it easier on the user - it represents the number of rounds to be fired
        // per second.  Here, an actual ROF decimal value is calculated that can be used with timers.
        if (rateOfFire != 0)
            actualROF = 1.0f / rateOfFire;
        else
            actualROF = 0.01f;

        currentAccuracy = accuracy;

        // Initialize the current crosshair size variable to the starting value specified by the user
        currentCrosshairSize = startingCrosshairSize;

        // Make sure the fire timer starts at 0
        fireTimer = 0.0f;

        // Start the weapon off with a full magazine
        currentAmmo = ammoCapacity;

        // Give this weapon an audio source component if it doesn't already have one
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Make sure raycastStartSpot isn't null
        if (raycastStartSpot == null)
            raycastStartSpot = gameObject.transform;

        // Make sure muzzleEffectsPosition isn't null
        if (muzzleEffectsPosition == null)
            muzzleEffectsPosition = gameObject.transform;

        // Make sure projectileSpawnSpot isn't null
        if (projectileSpawnSpot == null)
            projectileSpawnSpot = gameObject.transform;

        // Make sure crosshairTexture isn't null
        if (crosshairTexture == null)
            crosshairTexture = new Texture2D(0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        // Calculate the current accuracy for this weapon
        currentAccuracy = Mathf.Lerp(currentAccuracy, accuracy, accuracyRecoverRate * Time.deltaTime);
        if (type == WeaponType.Projectile)
            currentAccuracy = accuracy;

        // Calculate the current crosshair size.  This is what causes the crosshairs to grow and shrink dynamically while shooting
        currentCrosshairSize = startingCrosshairSize + (accuracy - currentAccuracy) * 0.8f;

        // Update the fireTimer
        fireTimer += Time.deltaTime;

        // Reset the Burst
        if (burstCounter >= burstRate)
        {
            burstTimer += Time.deltaTime;
            if (burstTimer >= burstPause)
            {
                burstCounter = 0;
                burstTimer = 0.0f;
            }
        }

        // Reload if the weapon is out of ammo automatically
        if (currentAmmo <= 0)
        {
            currentAmmo = ammoCapacity;
            fireTimer = -reloadTime;
            if (reloadSound != null) audioSource.PlayOneShot(reloadSound);
        }

        // Ammo Display
        if (user != null)
        {
            if (showCurrentAmmo && ammoText != null)
            {
                ammoText.enabled = true;
                ammoText.text = currentAmmo + "/" + ammoCapacity;
            }
        }
        else
        {
            if (ammoText != null)
                ammoText.enabled = false;
        }

        // Reduce Trail widths, based on duration as seconds
        if (shotTrails.Count > 0)
        {
            Stack<LineRenderer> trailsToRemove = new();
            for (int i = 0; i < shotTrails.Count; i++)
            {
                shotTrails[i].widthMultiplier -= Time.deltaTime / trailDuration;
                if (shotTrails[i].widthMultiplier <= 0.05f)
                {
                    Destroy(shotTrails[i].gameObject);
                    trailsToRemove.Push(shotTrails[i]);
                }
            }

            while(trailsToRemove.TryPop(out var trail))
                shotTrails.Remove(trail);
        }
    }

    public override bool DoAim() => true;
    public override bool DoAimMirror() => true;

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        canFire = true;
    }

    public override void ItemUpdate()
    {
        if (Input.GetButton("UseItem"))
        {
            if (!canFire) return;

            if (fireTimer < actualROF || burstCounter >= burstRate) return;

            fireTimer = 0.0f; // Reset the fire timer to 0 (for ROF)
            burstCounter++; // Increment the burst counter

            // If this is a semi-automatic weapon, set canFire to false (this means the weapon can't fire again until the player lets up on the fire button)
            if (auto == Auto.Semi) canFire = false;

            if (type == WeaponType.Raycast)
            {
                if (delayBeforeFire > 0) Invoke(nameof(Fire), delayBeforeFire);
                else Fire();
            }
            else
            {
                if (delayBeforeFire > 0) Invoke(nameof(Launch), delayBeforeFire);
                else Launch();
            }
        }
        else
        {
            // If the weapon is semi-auto and the user lets up on the button, set canFire to true
            canFire = true;
        }
    }

    void OnGUI()
    {
        if (user == null) return;

        // Crosshairs
        if (showCrosshair)
        {
            // Hold the location of the mouse in a variable
            Vector2 center = Input.mousePosition;
            // Invert y
            center.y = Screen.height - center.y;

            // Draw the crosshairs based on the weapon's inaccuracy
            // Left
            Rect leftRect = new Rect(center.x - crosshairLength - currentCrosshairSize, center.y - (crosshairWidth / 2), crosshairLength, crosshairWidth);
            GUI.DrawTexture(leftRect, crosshairTexture, ScaleMode.StretchToFill);
            // Right
            Rect rightRect = new Rect(center.x + currentCrosshairSize, center.y - (crosshairWidth / 2), crosshairLength, crosshairWidth);
            GUI.DrawTexture(rightRect, crosshairTexture, ScaleMode.StretchToFill);
            // Top
            Rect topRect = new Rect(center.x - (crosshairWidth / 2), center.y - crosshairLength - currentCrosshairSize, crosshairWidth, crosshairLength);
            GUI.DrawTexture(topRect, crosshairTexture, ScaleMode.StretchToFill);
            // Bottom
            Rect bottomRect = new Rect(center.x - (crosshairWidth / 2), center.y + currentCrosshairSize, crosshairWidth, crosshairLength);
            GUI.DrawTexture(bottomRect, crosshairTexture, ScaleMode.StretchToFill);
        }
    }

    bool PrepareFire()
    {
        // First make sure there is ammo
        if (currentAmmo <= 0)
        {
            if (dryFireSound != null) audioSource.PlayOneShot(dryFireSound);
            return false;
        }

        // Subtract 1 from the current ammo
        if (!infiniteAmmo)
            currentAmmo--;

        return true;
    }

    // Raycasting system
    void Fire()
    {
        // If prepare fire is false, return
        if (!PrepareFire()) return;

        // Fire once for each shotPerRound value
        for (int i = 0; i < shotPerRound; i++)
        {
            // Calculate accuracy for this shot
            float acc = (100 - currentAccuracy);
            float spreadAmount = Mathf.Lerp(0.0f, spread, acc / 100) * ((Random.value * 2f) - 1f); // Random makes it flip between positive and negative
            Vector2 direction = AimDirection; // We could use the raycastStartSpot.right value, but this is more accurate to where the player is actually aiming

            // Add spreadAmount as Degrees to the current direction
            direction = Quaternion.Euler(0, 0, spreadAmount) * direction;

            direction = direction.normalized;


            RaycastHit2D hit = Physics2D.Raycast(raycastStartSpot.position, direction, range, hittableLayers);
            if (hit != default)
            {
                ShotTrail(raycastStartSpot.position, hit.point);

                // Warmup heat
                float damage = power;

                // Damage
                hit.collider.gameObject.SendMessageUpwards("ChangeHealth", -damage, SendMessageOptions.DontRequireReceiver);

                // Hit Effects
                if (makeHitEffects)
                    foreach (GameObject hitEffect in hitEffects)
                        if (hitEffect != null)
                            Instantiate(hitEffect, hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));

                // Add force to the object that was hit
                if (hit.rigidbody)
                    hit.rigidbody.AddForce(direction * power * forceMultiplier);
            }
            else
            {
                ShotTrail(raycastStartSpot.position, direction * range);
            }
        }

        // Drop accurate per Fire, not per individual shot
        currentAccuracy -= accuracyDropPerShot;
        if (currentAccuracy <= 0.0f)
            currentAccuracy = 0.0f;

        ShotFX();
    }

    void ShotTrail(Vector2 start, Vector2 end)
    {
        // Trail effect, create a Line Renderer and set it's values
        GameObject trail = new GameObject("Trail");
        LineRenderer lr = trail.AddComponent<LineRenderer>();
        lr.material = trailMaterial;
        lr.startColor = trailColor;
        lr.endColor = trailColor;
        lr.startWidth = trailWidth;
        lr.endWidth = trailWidth/2f;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        shotTrails.Add(lr);
    }

    // Projectile system
    public void Launch()
    {
        // If prepare fire is false, return
        if (!PrepareFire()) return;

        // Fire once for each shotPerRound value
        for (int i = 0; i < shotPerRound; i++)
        {
            // Instantiate the projectile
            if (projectile != null)
                Instantiate(projectile, projectileSpawnSpot.position, projectileSpawnSpot.rotation);
            else
                Debug.Log("Projectile to be instantiated is null.  Make sure to set the Projectile field in the inspector.");
        }

        ShotFX();
    }

    void ShotFX()
    {
        user.Rigidbody.AddForce(-AimDirection * recoilForce, ForceMode2D.Impulse);

        // Muzzle flash effects
        if (makeMuzzleEffects)
        {
            GameObject muzfx = muzzleEffects[Random.Range(0, muzzleEffects.Length)];
            if (muzfx != null)
                Instantiate(muzfx, muzzleEffectsPosition.position, muzzleEffectsPosition.rotation);
        }

        if (shells != null) shells.Emit(1); // Instantiate shell props
        if (fireSound != null) audioSource.PlayOneShot(fireSound); // Play the gunshot sound
    }
}