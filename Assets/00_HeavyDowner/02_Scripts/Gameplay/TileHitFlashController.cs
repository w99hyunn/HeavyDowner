using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HeavyDowner.Gameplay
{
    public class TileHitFlashController : MonoBehaviour
    {
        [SerializeField] private float duration = 0.08f;
        [SerializeField] private Color color = new(1f, 0.72f, 0.62f, 1f);

        private readonly List<HitFlash> flashes = new();

        private struct HitFlash
        {
            public HitFlash(Tilemap tilemap, Vector3Int position, float restoreTime)
            {
                Tilemap = tilemap;
                Position = position;
                RestoreTime = restoreTime;
            }

            public Tilemap Tilemap { get; }
            public Vector3Int Position { get; }
            public float RestoreTime { get; set; }
        }

        private void Awake()
        {
            enabled = false;
        }

        private void Update()
        {
            for (int index = flashes.Count - 1; index >= 0; index--)
            {
                HitFlash flash = flashes[index];
                if (Time.time < flash.RestoreTime)
                {
                    continue;
                }

                flash.Tilemap.SetColor(flash.Position, Color.white);
                flashes.RemoveAt(index);
            }

            if (flashes.Count == 0)
            {
                enabled = false;
            }
        }

        public void Flash(Tilemap tilemap, Vector3Int position)
        {
            Flash(tilemap, position, color, duration);
        }

        public void Flash(Tilemap tilemap, Vector3Int position, Color flashColor, float flashDuration)
        {
            tilemap.SetTileFlags(position, TileFlags.None);
            tilemap.SetColor(position, flashColor);

            for (int index = 0; index < flashes.Count; index++)
            {
                HitFlash flash = flashes[index];
                if (flash.Tilemap == tilemap && flash.Position == position)
                {
                    flash.RestoreTime = Time.time + flashDuration;
                    flashes[index] = flash;
                    enabled = true;
                    return;
                }
            }

            flashes.Add(new HitFlash(tilemap, position, Time.time + flashDuration));
            enabled = true;
        }

        public void Remove(Tilemap tilemap, Vector3Int position)
        {
            for (int index = flashes.Count - 1; index >= 0; index--)
            {
                HitFlash flash = flashes[index];
                if (flash.Tilemap == tilemap && flash.Position == position)
                {
                    flashes.RemoveAt(index);
                }
            }

            if (flashes.Count == 0)
            {
                enabled = false;
            }
        }

        public void RemoveRows(int firstRow, int rowCount)
        {
            for (int index = flashes.Count - 1; index >= 0; index--)
            {
                int row = flashes[index].Position.y;
                if (row >= firstRow && row < firstRow + rowCount)
                {
                    flashes.RemoveAt(index);
                }
            }

            if (flashes.Count == 0)
            {
                enabled = false;
            }
        }

        public void Clear()
        {
            flashes.Clear();
            enabled = false;
        }
    }
}
