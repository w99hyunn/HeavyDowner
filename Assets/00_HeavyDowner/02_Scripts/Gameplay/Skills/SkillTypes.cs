using System;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    [Flags]
    public enum SkillCapability
    {
        None = 0,
        Movement = 1 << 0,
        Attack = 1 << 1,
        Defense = 1 << 2
    }

    public enum SkillSlotId
    {
        Left,
        Center,
        Right
    }

    [Serializable]
    public struct SkillSlotDefinition
    {
        [SerializeField] private SkillSlotId slotId;
        [SerializeField] private SkillDefinition skill;

        public SkillSlotId SlotId => slotId;
        public SkillDefinition Skill => skill;
    }

    public interface ISkillActor
    {
        event Action ShieldDepleted;

        Transform SkillTransform { get; }
        Vector2Int CurrentCell { get; }
        bool IsDead { get; }

        void SetMovementLocked(bool locked);
        void MoveToCell(Vector2Int targetCell);
        void ActivateShield(float damageReduction, float healthNormalized);
        void DeactivateShield();
    }

    public interface ISkillBoard
    {
        void AttackCorridor(Vector2Int origin, int distance, int halfWidth, int damage);
        void AttackArea(Vector2Int center, int radius, int damage);
    }

    public readonly struct SkillExecutionContext
    {
        public SkillExecutionContext(ISkillActor actor, ISkillBoard board, SkillCuePlayer cues)
        {
            Actor = actor;
            Board = board;
            Cues = cues;
        }

        public ISkillActor Actor { get; }
        public ISkillBoard Board { get; }
        public SkillCuePlayer Cues { get; }
    }

    public readonly struct SkillCueHandle
    {
        internal SkillCueHandle(int id)
        {
            Id = id;
        }

        internal int Id { get; }
    }
}
