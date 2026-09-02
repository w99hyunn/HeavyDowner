using UnityEngine;
using UnityEngine.Tilemaps;

namespace HeavyDowner.Gameplay
{
    public class BossAbilitySystem : MonoBehaviour
    {
        [SerializeField] private Tilemap enemyTilemap;
        [SerializeField] private IngamePlayerController player;
        [SerializeField] private Transform screenAnchor;
        [SerializeField] private SkillCuePlayer cuePlayer;

        private VerticalTilemapWorld world;

        private void Awake()
        {
            TryGetComponent<VerticalTilemapWorld>(out world);
        }

        public BossEntity CreateEntity(BossDefinition definition, Vector2Int anchor, int health)
        {
            GameplayAbilitySystem abilitySystem = new(cuePlayer);
            BossEntity entity = new(definition, anchor, health, abilitySystem);
            entity.InitializeAbility(abilitySystem.GrantAbility(new SkillRuntime(definition.Ability, new BossAbilityContext(entity, world, enemyTilemap, player, screenAnchor, cuePlayer), destroyCancellationToken)));
            return entity;
        }
    }
}
