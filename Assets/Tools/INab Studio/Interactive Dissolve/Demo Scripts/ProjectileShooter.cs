using UnityEngine;

namespace INab.InteractiveDissolveDemo
{
    /// <summary>
    /// Simple script to shoot projectile prefab at stuff in layer mask.
    /// </summary>
    public class ProjectileShooter : MonoBehaviour
    {
        public GameObject projectilePrefab; // The projectile prefab to shoot
        public Transform shootPoint; // The point from which the projectile will be shot
        public float shootForce = 1000f; // The force applied to the projectile to propel it forward

        public LayerMask layerMask;

        // Update is called once per frame
        void Update()
        {
            // Check for the fire button (left mouse button in this case)
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                ShootProjectile();
            }
        }

        void ShootProjectile()
        {
            var myCamera = Camera.main;

            Ray ray = myCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 300, layerMask.value))
            {
                // Instantiate the projectile at the shootPoint's position and rotation
                GameObject projectile = Instantiate(projectilePrefab, shootPoint.position, shootPoint.rotation);

                Rigidbody rb = projectile.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    var hitDir = (hit.point - shootPoint.position).normalized;
                    rb.AddForce(hitDir * shootForce);
                }
            }
        }

    }
}