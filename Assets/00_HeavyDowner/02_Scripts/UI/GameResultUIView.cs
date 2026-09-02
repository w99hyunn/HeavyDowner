using System;
using System.Collections.Generic;
using HeavyDowner.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeavyDowner.UI
{
    public sealed class GameResultUIView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text depthText;
        [SerializeField] private TMP_Text enhancementOrbText;
        [SerializeField] private Image[] equipmentIcons;
        [SerializeField] private GameObject emptyRewardText;
        [SerializeField] private Button mainButton;
        [SerializeField] private TMP_Text mainButtonText;
        [SerializeField] private Animator popupAnimator;
        [SerializeField] private float hideDuration = 0.14f;

        private int animationVersion;

        public Button MainButton => mainButton;

        private void Awake()
        {
            HideImmediate();
        }

        public void Show(int depth, int enhancementOrbs, IReadOnlyList<EquipmentDefinition> equipment)
        {
            animationVersion++;
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

            panel.SetActive(true);
            popupAnimator.Play("Show", 0, 0f);
        }

        public async Awaitable<bool> HideAsync()
        {
            int hideVersion = ++animationVersion;
            mainButton.interactable = false;
            popupAnimator.Play("Hide", 0, 0f);

            try
            {
                await Awaitable.WaitForSecondsAsync(hideDuration, destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            if (hideVersion != animationVersion)
                return false;

            panel.SetActive(false);
            return true;
        }

        public void HideImmediate()
        {
            animationVersion++;
            panel.SetActive(false);
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
