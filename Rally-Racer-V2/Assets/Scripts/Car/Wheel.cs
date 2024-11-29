using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Car
{
    [Serializable]
    public class Wheel : MonoBehaviour
    {
        public WheelCollider WheelCollider
        {
            get => _wheelCollider;
            set => _wheelCollider = value;
        }

        public GameObject wheelObject;
        public Axle axle;

        private WheelCollider _wheelCollider;

        private void Awake()
        {
            _wheelCollider = GetComponent<WheelCollider>();
        }
    }
}