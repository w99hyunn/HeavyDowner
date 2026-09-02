using UnityEngine;
using UnityEngine.Tilemaps;

namespace HeavyDowner.Gameplay
{
    public class BossAbilitySystem : MonoBehaviour
    {
        [SerializeField] private VerticalTilemapWorld world;
        [SerializeField] private Tilemap enemyTilemap;
        [SerializeField] private IngamePlayerController player;
        [SerializeField] private Transform screenAnchor;
        [SerializeField] private SkillCuePlayer cuePlayer;

        public BossEntity CreateEntity(BossDefinition definition, Vector2Int anchor, int health)
        {
            GameplayAbilitySystem abilitySystem = new(cuePlayer);
            BossEntity entity = new(definition, anchor, health, abilitySystem);
            BossAbilityContext context = new(entity, world, enemyTilemap, player, screenAnchor, cuePlayer);
            SkillRuntime ability = abilitySystem.GrantAbility(new SkillRuntime(definition.Ability, context, destroyCancellationToken));
            entity.InitializeAbility(ability);
            return entity;
        }
    }
}
