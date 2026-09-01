using TMPro;
using UnityEngine;

namespace HeavyDowner.UI
{
    public class ShowVersion : MonoBehaviour
    {
        private void Awake()
        {
            TryGetComponent<TMP_Text>(out TMP_Text versionText);
            versionText.text = $"ver {Application.version}";
        }
    }
}
