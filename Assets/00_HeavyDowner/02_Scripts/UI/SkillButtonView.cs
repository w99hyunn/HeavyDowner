using System;
using HeavyDowner.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace HeavyDowner.UI
{
    public class SkillButtonView : MonoBehaviour
    {
        private static readonly int READY_STATE_HASH = Animator.StringToHash("Ready");

        [SerializeField] private Image iconImage;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private SkillSlotId slotId;

        private Button button;
        private Animator readyAnimator;
        private bool wasCoolingDown;

        public event Action<SkillSlotId> Clicked;

        public SkillSlotId SlotId => slotId;

        private void Awake()
        {
            TryGetComponent<Button>(out button);
            TryGetComponent<Animator>(out readyAnimator);
        }

        private void OnEnable()
        {
            button.onClick.AddListener(OnClicked);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(OnClicked);
        }

        public void SetIcon(Sprite icon)
        {
            iconImage.sprite = icon;
        }

        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
        }

        public void SetCooldown(float remainingNormalized)
        {
            float remaining = Mathf.Clamp01(remainingNormalized);
            bool isCoolingDown = remaining > 0f;
            cooldownOverlay.gameObject.SetActive(isCoolingDown);
            cooldownOverlay.fillAmount = remaining;

            if (wasCoolingDown && !isCoolingDown)
            {
                readyAnimator.Play(READY_STATE_HASH, 0, 0f);
            }

            wasCoolingDown = isCoolingDown;
        }

        private void OnClicked()
        {
            Clicked?.Invoke(slotId);
        }
    }
}
