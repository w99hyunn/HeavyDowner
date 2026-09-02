using System.Collections.Generic;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    [CreateAssetMenu(menuName = "Heavy Downer/Equipment/Catalog", fileName = "EquipmentCatalog")]
    public sealed class EquipmentCatalog : ScriptableObject
    {
        [SerializeField] private Sprite enhancementOrbIcon;
        [SerializeField] private EquipmentDefinition[] items;

        public Sprite EnhancementOrbIcon => enhancementOrbIcon;
        public IReadOnlyList<EquipmentDefinition> Items => items;

        public EquipmentDefinition Get(EquipmentId id)
        {
            for (int index = 0; index < items.Length; index++)
            {
                if (items[index].Id == id)
                {
                    return items[index];
                }
            }

            throw new KeyNotFoundException($"Equipment definition is missing: {id}");
        }
    }
}
