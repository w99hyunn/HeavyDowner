using System;
using HeavyDowner.Gameplay;
using HeavyDowner.Module;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Events;

namespace HeavyDowner.UI
{
    public class EquipmentUIController : MonoBehaviour
    {
        [SerializeField] private EquipmentCatalog catalog;

        public event Action EnhancementOrbsChanged;

        private EquipmentUIView view;
        private EquipmentDefinition[] weapons;
        private EquipmentDefinition[] armors;
        private UnityAction[] weaponActions;
        private UnityAction[] armorActions;
        private EquipmentDefinition selected;
        private bool isSaving;

        private void Awake()
        {
            TryGetComponent<EquipmentUIView>(out view);
            BuildCategoryLists();
            BuildSlotActions();
        }

        private void OnEnable()
        {
            view.CloseButton.onClick.AddListener(OnCloseClicked);
            view.EquipButton.onClick.AddListener(OnEquipClicked);
            view.UpgradeButton.onClick.AddListener(OnUpgradeClicked);
            for (int index = 0; index < weapons.Length; index++)
            {
                view.GetWeaponSlot(index).Button.onClick.AddListener(weaponActions[index]);
            }

            for (int index = 0; index < armors.Length; index++)
            {
                view.GetArmorSlot(index).Button.onClick.AddListener(armorActions[index]);
            }
        }

        private void OnDisable()
        {
            view.CloseButton.onClick.RemoveListener(OnCloseClicked);
            view.EquipButton.onClick.RemoveListener(OnEquipClicked);
            view.UpgradeButton.onClick.RemoveListener(OnUpgradeClicked);
            for (int index = 0; index < weapons.Length; index++)
            {
                view.GetWeaponSlot(index).Button.onClick.RemoveListener(weaponActions[index]);
            }

            for (int index = 0; index < armors.Length; index++)
            {
                view.GetArmorSlot(index).Button.onClick.RemoveListener(armorActions[index]);
            }
        }

        public void Show()
        {
            selected = ResolveInitialSelection();
            Refresh();
            view.Show();
        }

        private void OnCloseClicked()
        {
            _ = view.HideAsync();
        }

        private void BuildCategoryLists()
        {
            weapons = new EquipmentDefinition[view.WeaponSlotCount];
            armors = new EquipmentDefinition[view.ArmorSlotCount];
            int weaponIndex = 0;
            int armorIndex = 0;
            for (int index = 0; index < catalog.Items.Count; index++)
            {
                EquipmentDefinition definition = catalog.Items[index];
                if (definition.Type == EquipmentType.Weapon)
                {
                    weapons[weaponIndex++] = definition;
                }
                else
                {
                    armors[armorIndex++] = definition;
                }
            }
        }

        private void BuildSlotActions()
        {
            weaponActions = new UnityAction[weapons.Length];
            for (int index = 0; index < weapons.Length; index++)
            {
                EquipmentDefinition definition = weapons[index];
                weaponActions[index] = () => Select(definition);
            }

            armorActions = new UnityAction[armors.Length];
            for (int index = 0; index < armors.Length; index++)
            {
                EquipmentDefinition definition = armors[index];
                armorActions[index] = () => Select(definition);
            }
        }

        private EquipmentDefinition ResolveInitialSelection()
        {
            EquipmentId equipped = PlayerDataService.EquippedWeapon;
            if (equipped != EquipmentId.None)
            {
                return catalog.Get(equipped);
            }

            for (int index = 0; index < catalog.Items.Count; index++)
            {
                EquipmentDefinition definition = catalog.Items[index];
                if (PlayerDataService.IsEquipmentOwned(definition.Id))
                {
                    return definition;
                }
            }

            return catalog.Items[0];
        }

        private void Select(EquipmentDefinition definition)
        {
            selected = definition;
            RefreshSelected();
        }

        private void Refresh()
        {
            view.SetEnhancementOrbs(PlayerDataService.EnhancementOrbs);
            for (int index = 0; index < weapons.Length; index++)
            {
                EquipmentDefinition definition = weapons[index];
                view.GetWeaponSlot(index).Set(definition, PlayerDataService.IsEquipmentOwned(definition.Id), PlayerDataService.GetEquipmentLevel(definition.Id), PlayerDataService.EquippedWeapon == definition.Id);
            }

            for (int index = 0; index < armors.Length; index++)
            {
                EquipmentDefinition definition = armors[index];
                view.GetArmorSlot(index).Set(definition, PlayerDataService.IsEquipmentOwned(definition.Id), PlayerDataService.GetEquipmentLevel(definition.Id), PlayerDataService.EquippedArmor == definition.Id);
            }

            RefreshSelected();
        }

        private void RefreshSelected()
        {
            int level = PlayerDataService.GetEquipmentLevel(selected.Id);
            view.SetSelected(
                selected,
                PlayerDataService.IsEquipmentOwned(selected.Id),
                level,
                selected.Type == EquipmentType.Weapon ? PlayerDataService.EquippedWeapon == selected.Id : PlayerDataService.EquippedArmor == selected.Id,
                selected.Type == EquipmentType.Weapon ? GetWeaponStats(selected, level) : $"HP +{selected.GetHealthBonus(level):N0}",
                selected.GetUpgradeCost(level));
        }

        private async void OnEquipClicked()
        {
            if (isSaving)
            {
                return;
            }

            isSaving = true;
            try
            {
                await PlayerDataService.EquipAsync(selected);
            }
            catch (RequestFailedException exception)
            {
                isSaving = false;
                Debug.LogException(exception);
                PopupSingleton.Instance.ShowMessage("장비 저장에 실패했습니다.\r\n다시 시도해주세요.");
                return;
            }

            isSaving = false;
            Refresh();
        }

        private async void OnUpgradeClicked()
        {
            if (isSaving)
            {
                return;
            }

            if (PlayerDataService.EnhancementOrbs < selected.GetUpgradeCost(PlayerDataService.GetEquipmentLevel(selected.Id)))
            {
                PopupSingleton.Instance.ShowMessage("강화 재료가 부족합니다.");
                return;
            }

            isSaving = true;
            try
            {
                await PlayerDataService.UpgradeAsync(selected);
            }
            catch (RequestFailedException exception)
            {
                isSaving = false;
                Debug.LogException(exception);
                PopupSingleton.Instance.ShowMessage("장비 강화에 실패했습니다.\r\n다시 시도해주세요.");
                return;
            }

            isSaving = false;
            Refresh();
            EnhancementOrbsChanged?.Invoke();
        }

        private static string GetWeaponStats(EquipmentDefinition definition, int level)
        {
            int radius = definition.GetAttackRadius(level);
            string power = $"공격력 +{definition.GetAttackBonus(level):N0}";
            return radius > 0 ? $"{power}\n공격 범위 +{radius}" : power;
        }
    }
}
