using UnityEngine;

public class Piece : MonoBehaviour
{
	public Cell CurrentCell;
	public bool isOnTable = false;
	public enum PieceColor { White, Black }
	public enum PieceType { Horse, Bishop, Rock }
	public PieceColor pieceColor = PieceColor.White;
	public PieceType pieceType = PieceType.Horse;

	public void SetCurrentCell(Cell newCell)
	{
		CurrentCell = newCell;
	}

	public virtual void MoveTo(Cell targetCell)
	{
		if (targetCell.IsOccupied)
		{
			Debug.Log("Cell is already occupied!");
			return;
		}

		if (CurrentCell != null)
		{
			CurrentCell.IsOccupied = false;
		}

		CurrentCell = targetCell;
		CurrentCell.IsOccupied = true;

		// Move piece to target cell
		transform.position = targetCell.transform.position;
		isOnTable = true; // The piece is now on the table
	}
}
