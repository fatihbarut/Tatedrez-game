using UnityEngine;

public class DragAndDropManager : MonoBehaviour
{
	private Piece selectedPiece;

	void Update()
	{
		// Select the piece when the mouse button is pressed
		if (Input.GetMouseButtonDown(0))
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out RaycastHit hit))
			{
				if (hit.collider.TryGetComponent(out Piece piece))
				{
					selectedPiece = piece;
				}
			}
		}

		// Drop the piece on a cell when the mouse button is released
		if (Input.GetMouseButtonUp(0) && selectedPiece != null)
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out RaycastHit hit))
			{
				if (hit.collider.TryGetComponent(out Cell cell))
				{
					selectedPiece.MoveTo(cell);
				}
			}

			selectedPiece = null;
		}
	}
}
