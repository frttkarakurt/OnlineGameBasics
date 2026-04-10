using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/WeaponData")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public float fireRate = 0.2f;
    public float range = 100f;
    public int damage = 25;
    public int maganizeSize = 30;
    public int spareAmmo = 90;
    public int reloadTime=5;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public ParticleSystem muzzleFlashPrefab;
}
