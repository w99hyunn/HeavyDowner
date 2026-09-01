using HeavyDowner.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace HeavyDowner.UI
{
    public sealed class SkillButtonView : MonoBehaviour
    {
        private static readonly int READY_STATE_HASH = Animator.StringToHash("Ready");

        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private Animator readyAnimator;
        [SerializeField] private SkillSlotId slotId;

        private bool wasCoolingDown;

        public Button Button => button;
        public SkillSlotId SlotId => slotId;

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
    }
}
