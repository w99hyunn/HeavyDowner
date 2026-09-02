using System;
using System.Threading;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public class GameplayAbilityRuntime
    {
        private readonly IGameplayAbilityContext context;
        private readonly CancellationToken lifetimeToken;
        private CancellationTokenSource activationCancellation;
        private float cooldownEndTime;

        public GameplayAbilityRuntime(GameplayAbilityDefinition ability, IGameplayAbilityContext context, CancellationToken lifetimeToken)
        {
            Definition = ability;
            this.context = context;
            this.lifetimeToken = lifetimeToken;
        }

        public event Action StateChanged;

        public GameplayAbilityDefinition Definition { get; }
        public bool IsActive { get; private set; }
        public float CooldownRemaining => Mathf.Max(0f, cooldownEndTime - Time.time);
        public bool IsReady => !IsActive && CooldownRemaining <= 0f;

        public void Activate()
        {
            TryActivate();
        }

        public void Cancel()
        {
            if (!IsActive)
            {
                return;
            }

            activationCancellation.Cancel();
        }

        public bool TryActivate(int magnitude = 0)
        {
            if (!IsReady)
            {
                return false;
            }

            cooldownEndTime = Time.time + Definition.Cooldown;
            activationCancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken);
            IsActive = true;
            StateChanged?.Invoke();

            _ = ExecuteAsync(magnitude, activationCancellation);
            _ = WaitForCooldownAsync(Definition.Cooldown);
            return true;
        }

        private async Awaitable ExecuteAsync(int magnitude, CancellationTokenSource cancellationSource)
        {
            GameplayCueHandle loopCueHandle = default;

            try
            {
                foreach (GameplayCueDefinition cue in Definition.ActivationCues)
                {
                    context.Cues.PlayOneShot(cue, context.CuePosition);
                }

                if (Definition.LoopCue != null)
                {
                    loopCueHandle = context.Cues.PlayLoop(Definition.LoopCue, context.LoopCueAnchor);
                }

                cancellationSource.Token.ThrowIfCancellationRequested();
                await Definition.ExecuteAsync(context, magnitude, cancellationSource.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (Definition.LoopCue != null)
                {
                    context.Cues.Stop(loopCueHandle);
                }

                foreach (GameplayCueDefinition cue in Definition.EndCues)
                {
                    context.Cues.PlayOneShot(cue, context.CuePosition);
                }

                IsActive = false;
                activationCancellation = null;
                cancellationSource.Dispose();
                StateChanged?.Invoke();
            }
        }

        private async Awaitable WaitForCooldownAsync(float cooldown)
        {
            if (cooldown <= 0f)
            {
                return;
            }

            try
            {
                await Awaitable.WaitForSecondsAsync(cooldown, lifetimeToken);
                StateChanged?.Invoke();
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
