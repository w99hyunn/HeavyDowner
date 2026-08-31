using TMPro;
using UnityEngine;

namespace HeavyDowner.UI
{
    public class ShowVersion : MonoBehaviour
    {
        private TMP_Text versionText;

        private void Awake()
        {
            TryGetComponent<TMP_Text>(out versionText);
            versionText.text = $"ver {Application.version}";
        }
    }
}