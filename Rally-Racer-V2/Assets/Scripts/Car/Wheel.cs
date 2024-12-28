using System;
using UnityEngine;

namespace Car
{
    [Serializable]
    public class Wheel : MonoBehaviour
    {
        #region Getters/Setters

        public GameObject WheelObject => wheelObject;
        public Axle Axle => axle;

        #endregion

        [SerializeField] private Axle axle;

        private GameObject wheelObject;
        private MeshCollider wheelCollider;

        private void Awake()
        {
            wheelObject = gameObject;
            wheelCollider = GetComponent<MeshCollider>();
        }
    }
}