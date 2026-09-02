using System.Collections.Generic;
using HeavyDowner.Module;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public class RunRewardSession : MonoBehaviour
    {
        [SerializeField] private EquipmentCatalog catalog;
        [SerializeField, Range(0f, 1f)] private float equipmentDropChance = 0.18f;

        private readonly List<EquipmentDefinition> acquiredEquipment = new();
        private readonly List<EquipmentDefinition> dropCandidates = new();
        private readonly List<EquipmentId> acquiredIds = new();
        private EquipmentDropVisualPool dropVisualPool;
        private int enhancementOrbs;

        public IReadOnlyList<EquipmentDefinition> AcquiredEquipment => acquiredEquipment;
        public int EnhancementOrbs => enhancementOrbs;
        public bool IsCommitted { get; private set; }

        private void Awake()
        {
            TryGetComponent<EquipmentDropVisualPool>(out dropVisualPool);
        }

        public void TryDropEquipment(Vector3 worldPosition)
        {
            if (Random.value >= equipmentDropChance)
            {
                return;
            }

            DropEquipment(worldPosition);
        }

        public void DropEquipment(Vector3 worldPosition)
        {
            dropCandidates.Clear();
            int totalWeight = 0;
            for (int index = 0; index < catalog.Items.Count; index++)
            {
                EquipmentDefinition definition = catalog.Items[index];
                if (PlayerDataService.IsEquipmentOwned(definition.Id) || ContainsAcquired(definition.Id))
                {
                    continue;
                }

                dropCandidates.Add(definition);
                totalWeight += definition.DropWeight;
            }

            if (dropCandidates.Count == 0)
            {
                return;
            }

            int roll = Random.Range(0, totalWeight);
            EquipmentDefinition dropped = dropCandidates[0];
            for (int index = 0; index < dropCandidates.Count; index++)
            {
                dropped = dropCandidates[index];
                roll -= dropped.DropWeight;
                if (roll < 0)
                {
                    break;
                }
            }

            acquiredEquipment.Add(dropped);
            dropVisualPool.Play(dropped.Icon, worldPosition);
        }

        public void CollectEnhancementOrb(int amount, Vector3 worldPosition)
        {
            enhancementOrbs += amount;
            dropVisualPool.Play(catalog.EnhancementOrbIcon, worldPosition);
        }

        public async Awaitable CommitAsync(int depth)
        {
            if (IsCommitted)
            {
                return;
            }

            acquiredIds.Clear();
            for (int index = 0; index < acquiredEquipment.Count; index++)
            {
                acquiredIds.Add(acquiredEquipment[index].Id);
            }

            await PlayerDataService.CommitRunAsync(acquiredIds, enhancementOrbs, depth);
            IsCommitted = true;
        }

        private bool ContainsAcquired(EquipmentId id)
        {
            for (int index = 0; index < acquiredEquipment.Count; index++)
            {
                if (acquiredEquipment[index].Id == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
