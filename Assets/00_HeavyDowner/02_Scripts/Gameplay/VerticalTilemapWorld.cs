using UnityEngine;

namespace HeavyDowner.Gameplay
{
    [RequireComponent(typeof(Grid))]
    public class VerticalTilemapWorld : MonoBehaviour
    {
        private const int HORIZONTAL_CELL_COUNT = 9;

        private Grid grid;

        public Vector2 CellSize => grid.cellSize;
        public int HorizontalCellCount => HORIZONTAL_CELL_COUNT;

        private void Awake()
        {
            TryGetComponent<Grid>(out grid);
        }

        public Vector2Int WorldToCell(Vector3 position)
        {
            Vector3 origin = transform.position;
            return new Vector2Int(Mathf.RoundToInt((position.x - origin.x) / grid.cellSize.x), Mathf.RoundToInt((position.y - origin.y) / grid.cellSize.y));
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            Vector3 origin = transform.position;
            return new Vector3(origin.x + cell.x * grid.cellSize.x, origin.y + cell.y * grid.cellSize.y, origin.z);
        }

        public bool IsPlayableColumn(int column)
        {
            return Mathf.Abs(column) <= HORIZONTAL_CELL_COUNT / 2;
        }

    }
}
