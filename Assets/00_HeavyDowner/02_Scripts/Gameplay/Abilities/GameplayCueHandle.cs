namespace HeavyDowner.Gameplay
{
    public readonly struct GameplayCueHandle
    {
        internal GameplayCueHandle(int id)
        {
            Id = id;
        }

        internal int Id { get; }
    }
}
