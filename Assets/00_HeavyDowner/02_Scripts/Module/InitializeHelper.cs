using System;
using UnityEngine;

namespace HeavyDowner.Module
{
    public sealed class InitializeHelper : MonoBehaviour
    {
        public event Action Initialized;

        private void Start()
        {
            Initialized?.Invoke();
        }
    }
}
