using System;
using HeavyDowner.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeavyDowner.UI
{
    [Serializable]
    public class EquipmentSlotBinding
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Image rarityFrame;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject equippedMark;

        public Button Button => button;

        public void Set(EquipmentDefinition definition, bool owned, int level, bool equipped)
        {
            icon.sprite = definition.Icon;
            icon.color = owned ? Color.white : new Color(0.22f, 0.22f, 0.28f, 0.72f);
            rarityFrame.color = definition.GetRarityColor();
            levelText.text = owned ? $"+{level}" : "";
            lockOverlay.SetActive(!owned);
            equippedMark.SetActive(equipped);
        }
    }

    public class EquipmentUIView : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private EquipmentSlotBinding[] weaponSlots;
        [SerializeField] private EquipmentSlotBinding[] armorSlots;
        [SerializeField] private Image selectedIcon;
        [SerializeField] private TMP_Text selectedNameText;
        [SerializeField] private TMP_Text selectedRarityText;
        [SerializeField] private TMP_Text selectedLevelText;
        [SerializeField] private TMP_Text selectedStatsText;
        [SerializeField] private TMP_Text enhancementOrbText;
        [SerializeField] private TMP_Text upgradeCostText;
        [SerializeField] private Button equipButton;
        [SerializeField] private TMP_Text equipButtonText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Animator popupAnimator;

        private PopupAnimation popupAnimation;

        public Button CloseButton => closeButton;
        public Button EquipButton => equipButton;
        public Button UpgradeButton => upgradeButton;
        public int WeaponSlotCount => weaponSlots.Length;
        public int ArmorSlotCount => armorSlots.Length;

        private void Awake()
        {
            popupAnimation = new PopupAnimation(popupRoot, popupAnimator);
            popupAnimation.HideImmediate();
        }

        public EquipmentSlotBinding GetWeaponSlot(int index)
        {
            return weaponSlots[index];
        }

        public EquipmentSlotBinding GetArmorSlot(int index)
        {
            return armorSlots[index];
        }

        public void Show()
        {
            closeButton.interactable = true;
            popupAnimation.Show();
        }

        public async Awaitable<bool> HideAsync()
        {
            closeButton.interactable = false;
            return await popupAnimation.HideAsync(destroyCancellationToken);
        }

        public void SetEnhancementOrbs(int amount)
        {
            enhancementOrbText.text = amount.ToString();
        }

        public void SetSelected(EquipmentDefinition definition, bool owned, int level, bool equipped, string stats, int upgradeCost)
        {
            selectedIcon.sprite = definition.Icon;
            selectedIcon.color = owned ? Color.white : new Color(0.3f, 0.3f, 0.36f, 0.78f);
            selectedNameText.text = definition.DisplayName;
            selectedRarityText.text = GetRarityName(definition.Rarity);
            selectedRarityText.color = definition.GetRarityColor();
            selectedLevelText.text = owned ? $"강화 +{level}" : "미획득";
            selectedStatsText.text = stats;
            upgradeCostText.text = level < definition.MaxLevel ? upgradeCost.ToString() : "MAX";
            equipButton.gameObject.SetActive(owned);
            equipButtonText.text = equipped ? "해제" : "장착";
            upgradeButton.gameObject.SetActive(owned && level < definition.MaxLevel);
        }

        private static string GetRarityName(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => "일반",
                EquipmentRarity.Uncommon => "고급",
                EquipmentRarity.Rare => "희귀",
                EquipmentRarity.Epic => "영웅",
                EquipmentRarity.Legendary => "전설",
                _ => ""
            };
        }
    }
}
