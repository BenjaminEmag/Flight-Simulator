using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Klareh
{
    public class ZeroController : MonoBehaviour
    {
        public bool Engine;
        public bool Wheels;
        private Animator _animator;

        public float thrustPercentage = 0f;

        void Start()
        {
            Engine = true;
            Wheels = true;
            _animator = GetComponent<Animator>();
        }

        void FixedUpdate()
        {
            
            if (thrustPercentage != 0f)
                _animator.SetBool("EngineState", true);
            else
                _animator.SetBool("EngineState", false);
            
        }
    }
}
