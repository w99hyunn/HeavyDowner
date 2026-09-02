using System.Collections.Generic;

namespace HeavyDowner.Gameplay
{
    public class GameplayAbilitySystem
    {
        private readonly GameplayCuePlayer cuePlayer;
        private readonly List<GameplayAbilityRuntime> abilities = new();
        private readonly List<GameplayCueDefinition> collectedCues = new();

        public GameplayAbilitySystem(GameplayCuePlayer cuePlayer)
        {
            this.cuePlayer = cuePlayer;
        }

        public GameplayAbilityRuntime GrantAbility(GameplayAbilityRuntime runtime)
        {
            collectedCues.Clear();
            runtime.Definition.CollectCues(collectedCues);
            for (int index = 0; index < collectedCues.Count; index++)
            {
                cuePlayer.Prewarm(collectedCues[index]);
            }

            abilities.Add(runtime);
            return runtime;
        }

        public void CancelAll()
        {
            for (int index = 0; index < abilities.Count; index++)
            {
                abilities[index].Cancel();
            }
        }
    }
}
