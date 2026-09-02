using System.Collections.Generic;

namespace HeavyDowner.Gameplay
{
    public class GameplayAbilitySystem
    {
        private readonly SkillCuePlayer cuePlayer;
        private readonly List<SkillRuntime> abilities = new();
        private readonly List<SkillCueDefinition> collectedCues = new();

        public GameplayAbilitySystem(SkillCuePlayer cuePlayer)
        {
            this.cuePlayer = cuePlayer;
        }

        public SkillRuntime GrantAbility(SkillRuntime runtime)
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
