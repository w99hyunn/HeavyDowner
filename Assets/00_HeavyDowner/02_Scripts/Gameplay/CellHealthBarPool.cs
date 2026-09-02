using System.Collections.Generic;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public class CellHealthBarPool : MonoBehaviour
    {
        [SerializeField] private CellHealthBar healthBarPrefab;

        private readonly Dictionary<Vector2Int, CellHealthBar> activeHealthBars = new();
        private readonly Stack<CellHealthBar> inactiveHealthBars = new();
        private readonly List<Vector2Int> healthBarsToHide = new();

        private void Awake()
        {
            for (int index = 0; index < 4; index++)
            {
                CellHealthBar healthBar = Instantiate(healthBarPrefab, transform);
                healthBar.Hide();
                inactiveHealthBars.Push(healthBar);
            }
        }

        public void Show(Vector2Int cellPosition, Vector3 worldPosition, float width, float normalizedHealth)
        {
            if (!activeHealthBars.TryGetValue(cellPosition, out CellHealthBar healthBar))
            {
                healthBar = inactiveHealthBars.Count > 0
                    ? inactiveHealthBars.Pop()
                    : Instantiate(healthBarPrefab, transform);
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
            healthBar.Hide();
            inactiveHealthBars.Push(healthBar);
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
                healthBar.Hide();
                inactiveHealthBars.Push(healthBar);
            }

            activeHealthBars.Clear();
        }
    }
}
