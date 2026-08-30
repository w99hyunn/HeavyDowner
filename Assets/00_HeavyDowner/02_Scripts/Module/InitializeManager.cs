using System;
using UnityEngine;

namespace HeavyDowner.Module
{
    public sealed class InitializeManager : MonoBehaviour
    {
        public event Action Initialized;

        private void Start()
        {
            Initialized?.Invoke();
        }
    }
}
