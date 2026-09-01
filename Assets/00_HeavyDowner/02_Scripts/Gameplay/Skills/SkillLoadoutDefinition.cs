using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    [Serializable]
    public struct SkillSlotDefinition
    {
        [SerializeField] private SkillSlotId slotId;
        [SerializeField] private SkillDefinition skill;

        public SkillSlotId SlotId => slotId;
        public SkillDefinition Skill => skill;
    }

    [CreateAssetMenu(menuName = "Heavy Downer/Skills/Loadout", fileName = "SkillLoadout")]
    public sealed class SkillLoadoutDefinition : ScriptableObject
    {
        [SerializeField] private SkillSlotDefinition[] slots;

        public IReadOnlyList<SkillSlotDefinition> Slots => slots;
    }
}
