using System;
using System.Threading;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public sealed class SkillRuntime : IDisposable
    {
        private readonly SkillExecutionContext context;
        private readonly CancellationToken lifetimeToken;
        private readonly Action stateChanged;

        private CancellationTokenSource activationCancellation;
        private float cooldownEndTime;

        public SkillRuntime(
            SkillDefinition definition,
            SkillExecutionContext context,
            CancellationToken lifetimeToken,
            Action stateChanged)
        {
            Definition = definition;
            this.context = context;
            this.lifetimeToken = lifetimeToken;
            this.stateChanged = stateChanged;
        }

        public SkillDefinition Definition { get; }
        public bool IsActive { get; private set; }
        public float CooldownRemaining => Mathf.Max(0f, cooldownEndTime - Time.time);
        public bool IsReady => !IsActive && CooldownRemaining <= 0f;

        public void Activate()
        {
            cooldownEndTime = Time.time + Definition.Cooldown;
            activationCancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken);
            IsActive = true;
            stateChanged();

            _ = ExecuteAsync(activationCancellation);
            _ = WaitForCooldownAsync(Definition.Cooldown);
        }

        public void Cancel()
        {
            if (!IsActive)
            {
                return;
            }

            activationCancellation.Cancel();
        }

        public void Dispose()
        {
            Cancel();
        }

        private async Awaitable ExecuteAsync(CancellationTokenSource cancellationSource)
        {
            CancellationToken cancellationToken = cancellationSource.Token;
            SkillCueHandle loopCueHandle = default;

            try
            {
                foreach (SkillCueDefinition cue in Definition.ActivationCues)
                {
                    context.Cues.PlayOneShot(cue, context.Actor.SkillTransform.position);
                }

                if (Definition.LoopCue != null)
                {
                    loopCueHandle = context.Cues.PlayLoop(Definition.LoopCue, context.Actor.SkillTransform);
                }

                cancellationToken.ThrowIfCancellationRequested();
                await Definition.ExecuteAsync(context, cancellationToken);
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

                foreach (SkillCueDefinition cue in Definition.EndCues)
                {
                    context.Cues.PlayOneShot(cue, context.Actor.SkillTransform.position);
                }

                IsActive = false;
                activationCancellation = null;
                cancellationSource.Dispose();
                stateChanged();
            }
        }

        private async Awaitable WaitForCooldownAsync(float cooldown)
        {
            if (cooldown <= 0f)
            {
                stateChanged();
                return;
            }

            try
            {
                await Awaitable.WaitForSecondsAsync(cooldown, lifetimeToken);
                stateChanged();
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
