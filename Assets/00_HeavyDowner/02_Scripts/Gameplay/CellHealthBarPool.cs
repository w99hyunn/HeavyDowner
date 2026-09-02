using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace HeavyDowner.Gameplay
{
    public class CellHealthBarPool : MonoBehaviour
    {
        [SerializeField] private CellHealthBar healthBarPrefab;

        private readonly Dictionary<Vector2Int, CellHealthBar> activeHealthBars = new();
        private readonly List<Vector2Int> healthBarsToHide = new();
        private ObjectPool<CellHealthBar> healthBarPool;

        private void Awake()
        {
            int initialCapacity = 4;
            healthBarPool = new ObjectPool<CellHealthBar>(
                () => Instantiate(healthBarPrefab, transform),
                null,
                healthBar => healthBar.Hide(),
                healthBar => Destroy(healthBar.gameObject),
                true,
                initialCapacity);

            CellHealthBar[] prewarmedHealthBars = new CellHealthBar[initialCapacity];
            for (int index = 0; index < prewarmedHealthBars.Length; index++)
            {
                prewarmedHealthBars[index] = healthBarPool.Get();
            }
            for (int index = 0; index < prewarmedHealthBars.Length; index++)
            {
                healthBarPool.Release(prewarmedHealthBars[index]);
            }
        }

        public void Show(Vector2Int cellPosition, Vector3 worldPosition, float width, float normalizedHealth)
        {
            if (!activeHealthBars.TryGetValue(cellPosition, out CellHealthBar healthBar))
            {
                healthBar = healthBarPool.Get();
                activeHealthBars.Add(cellPosition, healthBar);
            }

            healthBar.Show(worldPosition, width, normalizedHealth);
        }

        public void Hide(Vector2Int cellPosition)
        {
            if (!activeHealthBars.TryGetValue(cellPosition, out CellHealthBar healthBar))
            {
                return;
            }

            activeHealthBars.Remove(cellPosition);
            healthBarPool.Release(healthBar);
        }

        public void HideRows(int firstRow, int rowCount)
        {
            healthBarsToHide.Clear();

            foreach (Vector2Int cellPosition in activeHealthBars.Keys)
            {
                if (cellPosition.y >= firstRow && cellPosition.y < firstRow + rowCount)
                {
                    healthBarsToHide.Add(cellPosition);
                }
            }

            for (int index = 0; index < healthBarsToHide.Count; index++)
            {
                Hide(healthBarsToHide[index]);
            }
        }

        public void HideAll()
        {
            foreach (CellHealthBar healthBar in activeHealthBars.Values)
            {
                healthBarPool.Release(healthBar);
            }

            activeHealthBars.Clear();
        }

        private void OnDestroy()
        {
            HideAll();
            healthBarPool.Clear();
        }
    }
}
