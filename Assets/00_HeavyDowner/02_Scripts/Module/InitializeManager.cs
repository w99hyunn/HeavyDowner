using System;
using UnityEngine;

namespace HeavyDowner.Module
{
    public sealed class InitializeManager : MonoBehaviour
    {
        public event Action Initialized;

        private void Awake()
        {
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            Initialized?.Invoke();
        }
    }
}
