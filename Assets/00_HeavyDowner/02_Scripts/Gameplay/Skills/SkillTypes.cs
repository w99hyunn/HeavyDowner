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
        int AttackCorridor(Vector2Int origin, int distance, int halfWidth, int damage);
        void AttackArea(Vector2Int center, int radius, int damage);
    }

    public readonly struct SkillExecutionContext : IGameplayAbilityContext
    {
        public SkillExecutionContext(ISkillActor actor, ISkillBoard board, GameplayCuePlayer cues)
        {
            Actor = actor;
            Board = board;
            Cues = cues;
        }

        public ISkillActor Actor { get; }
        public ISkillBoard Board { get; }
        public GameplayCuePlayer Cues { get; }
        public Vector3 CuePosition => Actor.SkillTransform.position;
        public Transform LoopCueAnchor => Actor.SkillTransform;
    }

}
