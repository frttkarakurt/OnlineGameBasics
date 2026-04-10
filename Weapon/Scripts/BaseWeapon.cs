using UnityEngine;
using Mirror;
using TMPro;
using System.Collections;

public class BaseWeapon : NetworkBehaviour
{
    private InputActions controls;

    [Header("Weapon Data")]
    public WeaponData weaponData;
    public Transform firePoint;

    [Header("Ammo Settings")]
    [SyncVar(hook = nameof(OnAmmoChanged))] private int currentAmmo;
    [SyncVar(hook = nameof(OnAmmoChanged))] private int spareAmmo;

    private AudioSource audioSource;
    private float lastFireTime;
    private bool isReloading = false;

    public TMP_Text ammoText;

    void Awake()
    {
        controls = new InputActions();
        audioSource = GetComponent<AudioSource>();

        currentAmmo = weaponData.maganizeSize;
        spareAmmo = weaponData.spareAmmo;
        UpdateAmmoUI();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // Ateş etme
        float attackInput = controls.Player.Attack.ReadValue<float>();
        if (attackInput > 0.1f && Time.time - lastFireTime > weaponData.fireRate && !isReloading)
        {
            if (currentAmmo > 0)
            {
                lastFireTime = Time.time;
                Fire();
            }
            else
            {
                StartCoroutine(ReloadCoroutine());
            }
        }

        // Manuel reload tuşu (R)
        if (controls.Player.Reload.triggered && !isReloading)
        {
            StartCoroutine(ReloadCoroutine());
        }
    }

    void Fire()
    {
        currentAmmo--;
        UpdateAmmoUI();

        // Local efektler
        if (weaponData.muzzleFlashPrefab != null)
        {
            ParticleSystem flash = Instantiate(weaponData.muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            flash.Play();
            Destroy(flash.gameObject, 1f);
        }

        if (audioSource != null && weaponData.shootSound != null)
        {
            audioSource.PlayOneShot(weaponData.shootSound);
        }

        // Server'a ateşi bildir
        CmdFire(firePoint.position, firePoint.forward);
    }

    [Command]
    void CmdFire(Vector3 origin, Vector3 direction)
    {
        Ray ray = new Ray(origin, direction);
        if (Physics.Raycast(ray, out RaycastHit hit, weaponData.range))
        {
            var health = hit.collider.GetComponent<GamePlayer>();
            if (health != null)
            {
                health.TakeDamage(weaponData.damage,gameObject.GetComponentInParent<GamePlayer>());
            }
        }

        // Diğer client’larda efekt
        RpcPlayEffects(origin, direction);
    }

    [ClientRpc]
    void RpcPlayEffects(Vector3 origin, Vector3 direction)
    {
        if (isLocalPlayer) return;

        if (weaponData.muzzleFlashPrefab != null)
        {
            ParticleSystem flash = Instantiate(weaponData.muzzleFlashPrefab, origin, Quaternion.LookRotation(direction));
            flash.Play();
            Destroy(flash.gameObject, 1f);
        }

        if (audioSource != null && weaponData.shootSound != null)
        {
            audioSource.PlayOneShot(weaponData.shootSound);
        }
    }

    IEnumerator ReloadCoroutine()
    {
        if (isReloading || spareAmmo <= 0 || currentAmmo == weaponData.maganizeSize)
            yield break;

        isReloading = true;

        // Opsiyonel: reload animasyon veya ses beklemesi
        if (weaponData.reloadSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(weaponData.reloadSound);
        }

        yield return new WaitForSeconds(weaponData.reloadTime);

        Reload();
        isReloading = false;
    }

    private void Reload()
    {
        int neededAmmo = weaponData.maganizeSize - currentAmmo;
        int ammoToLoad = Mathf.Min(neededAmmo, spareAmmo);

        currentAmmo += ammoToLoad;
        spareAmmo -= ammoToLoad;

        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo} / {spareAmmo}";
        }
    }

    private void OnAmmoChanged(int oldVal, int newVal)
    {
        UpdateAmmoUI();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();
}
