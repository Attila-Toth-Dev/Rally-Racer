using System;
using Car;
using UnityEngine;

namespace Collisions
{
    [RequireComponent(typeof(MeshCollider))]
    public class SparxCollisionSystem : MonoBehaviour
    {
        [NonSerialized] public GameObject wheel;

        private MeshRenderer wheelRenderer;
        private Collider wheelCollider;
    
        private void Awake()
        {
            wheel = GetComponent<GameObject>();
            wheelRenderer = wheel.GetComponent<MeshRenderer>();
            
            wheelCollider = GetComponent<Collider>();
        }
    }
}
