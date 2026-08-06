using UnityEngine;

namespace TinyFishing.Fishing
{
    // Simple Archimedes-style float: pushes the object up whenever it's below
    // the water surface collider, proportional to how deep it is submerged,
    // and increases drag while in water so it settles instead of bobbing forever.
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Buoyancy : MonoBehaviour
    {
        [SerializeField] private Collider waterSurface;
        [SerializeField] private float floatStrength = 15f;
        [SerializeField] private float waterLinearDamping = 3f;
        [SerializeField] private float waterAngularDamping = 1f;
        [Tooltip("How far below the water surface this object should rest (its draft), e.g. half the bob's radius.")]
        [SerializeField] private float submergeOffset = 0.1f;

        private Rigidbody body;
        private float defaultLinearDamping;
        private float defaultAngularDamping;
        private bool inWater;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            defaultLinearDamping = body.linearDamping;
            defaultAngularDamping = body.angularDamping;
        }

        private void FixedUpdate()
        {
            if (waterSurface == null)
            {
                return;
            }

            var waterY = waterSurface.bounds.max.y;
            var depth = (waterY - submergeOffset) - transform.position.y;

            if (depth > 0f)
            {
                if (!inWater)
                {
                    inWater = true;
                    body.linearDamping = waterLinearDamping;
                    body.angularDamping = waterAngularDamping;
                }

                var force = Mathf.Clamp01(depth) * floatStrength;
                body.AddForce(Vector3.up * force, ForceMode.Acceleration);
            }
            else if (inWater)
            {
                inWater = false;
                body.linearDamping = defaultLinearDamping;
                body.angularDamping = defaultAngularDamping;
            }
        }
    }
}
