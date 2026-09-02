using System.Collections.Generic;
using HeavyDowner.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeavyDowner.UI
{
    public class GameResultUIView : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TMP_Text depthText;
        [SerializeField] private TMP_Text enhancementOrbText;
        [SerializeField] private Image[] equipmentIcons;
        [SerializeField] private GameObject emptyRewardText;
        [SerializeField] private Button mainButton;
        [SerializeField] private TMP_Text mainButtonText;
        [SerializeField] private Animator popupAnimator;

        private PopupAnimation popupAnimation;

        public Button MainButton => mainButton;

        private void Awake()
        {
            popupAnimation = new PopupAnimation(popupRoot, popupAnimator);
            popupAnimation.HideImmediate();
        }

        public void Show(int depth, int enhancementOrbs, IReadOnlyList<EquipmentDefinition> equipment)
        {
            depthText.text = $"{depth:N0}m";
            enhancementOrbText.text = enhancementOrbs.ToString();
            emptyRewardText.SetActive(equipment.Count == 0);
            for (int index = 0; index < equipmentIcons.Length; index++)
            {
                bool visible = index < equipment.Count;
                equipmentIcons[index].transform.parent.gameObject.SetActive(visible);
                if (visible)
                {
                    equipmentIcons[index].sprite = equipment[index].Icon;
                }
            }

            popupAnimation.Show();
        }

        public async Awaitable<bool> HideAsync()
        {
            mainButton.interactable = false;
            return await popupAnimation.HideAsync(destroyCancellationToken);
        }

        public void SetSaving()
        {
            mainButton.interactable = false;
            mainButtonText.text = "메인으로";
        }

        public void SetSaved()
        {
            mainButton.interactable = true;
            mainButtonText.text = "메인으로";
        }

        public void SetSaveFailed()
        {
            mainButton.interactable = true;
            mainButtonText.text = "저장 재시도";
        }
    }
}
