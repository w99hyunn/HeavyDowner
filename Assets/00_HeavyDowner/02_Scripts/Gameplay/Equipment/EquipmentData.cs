using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public enum EquipmentType
    {
        Weapon,
        Armor
    }

    public enum EquipmentRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum EquipmentId
    {
        None = 0,
        IronPickaxe = 1,
        CobaltHammer = 2,
        MagmaDrill = 3,
        CrystalBreaker = 4,
        CoreBore = 5,
        MinerVest = 6,
        ReinforcedJacket = 7,
        CobaltPlate = 8,
        CrystalGuard = 9,
        CorePlate = 10
    }

    [Serializable]
    public sealed class EquipmentDefinition
    {
        [SerializeField] private EquipmentId id;
        [SerializeField] private string displayName;
        [SerializeField] private EquipmentType type;
        [SerializeField] private EquipmentRarity rarity;
        [SerializeField] private Color rarityColor = Color.white;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0)] private int attackBonus;
        [SerializeField, Min(0)] private int attackBonusPerLevel;
        [SerializeField, Min(0)] private int attackRadius;
        [SerializeField, Min(0)] private int attackRadiusLevelInterval;
        [SerializeField, Min(0)] private int healthBonus;
        [SerializeField, Min(0)] private int healthBonusPerLevel;
        [SerializeField, Min(1)] private int upgradeBaseCost = 5;
        [SerializeField, Min(1)] private int maxLevel = 10;
        [SerializeField, Min(0)] private int dropWeight = 1;

        public EquipmentId Id => id;
        public string DisplayName => displayName;
        public EquipmentType Type => type;
        public EquipmentRarity Rarity => rarity;
        public Sprite Icon => icon;
        public int MaxLevel => maxLevel;
        public int DropWeight => dropWeight;

        public int GetAttackBonus(int level)
        {
            return attackBonus + attackBonusPerLevel * level;
        }

        public int GetAttackRadius(int level)
        {
            return attackRadiusLevelInterval > 0
                ? attackRadius + level / attackRadiusLevelInterval
                : attackRadius;
        }

        public int GetHealthBonus(int level)
        {
            return healthBonus + healthBonusPerLevel * level;
        }

        public int GetUpgradeCost(int level)
        {
            return upgradeBaseCost * (level + 1);
        }

        public Color GetRarityColor()
        {
            return rarityColor;
        }
    }

    [Serializable]
    public sealed class EquipmentProgress
    {
        public EquipmentId Id;
        public int Level;

        public EquipmentProgress(EquipmentId id, int level)
        {
            Id = id;
            Level = level;
        }
    }

    [Serializable]
    public sealed class EquipmentSaveData
    {
        public int EnhancementOrbs;
        public EquipmentId EquippedWeapon;
        public EquipmentId EquippedArmor;
        public List<EquipmentProgress> Items = new();

        public EquipmentSaveData()
        {
        }

        public EquipmentSaveData(EquipmentSaveData source)
        {
            EnhancementOrbs = source.EnhancementOrbs;
            EquippedWeapon = source.EquippedWeapon;
            EquippedArmor = source.EquippedArmor;
            for (int index = 0; index < source.Items.Count; index++)
            {
                EquipmentProgress item = source.Items[index];
                Items.Add(new EquipmentProgress(item.Id, item.Level));
            }
        }

        public bool IsOwned(EquipmentId id)
        {
            return GetItemIndex(id) >= 0;
        }

        public int GetLevel(EquipmentId id)
        {
            int index = GetItemIndex(id);
            return index >= 0 ? Items[index].Level : 0;
        }

        public EquipmentProgress GetProgress(EquipmentId id)
        {
            return Items[GetItemIndex(id)];
        }

        public void Add(EquipmentId id)
        {
            if (!IsOwned(id))
            {
                Items.Add(new EquipmentProgress(id, 0));
            }
        }

        private int GetItemIndex(EquipmentId id)
        {
            for (int index = 0; index < Items.Count; index++)
            {
                if (Items[index].Id == id)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
