using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.SceneManagement; // DOTween Library

public class BoardManager : MonoBehaviour
{
	public enum GameStance { Static, Dynamic }
	public enum PlayerStance { Think, Hold }

	public GameStance CurrentGameStance = GameStance.Static;
	public PlayerStance CurrentPlayerStance = PlayerStance.Think;

	private GameObject Holded; // The object being held

	private void Start()
	{
		StartCoroutine(CheckStance());
	}

	IEnumerator CheckStance()
	{
		while (true)
		{
			yield return new WaitForSeconds(0.01f);
			GameObject[] pieces = GameObject.FindGameObjectsWithTag("Piece");
			bool allOnTable = true;

			foreach (GameObject piece in pieces)
			{
				Piece pieceComponent = piece.GetComponent<Piece>();
				if (pieceComponent != null && !pieceComponent.isOnTable)
				{
					allOnTable = false; // If even one piece is not on the table, the check fails
					break; // No need to check further
				}
			}

			if (allOnTable)
			{
				// If all pieces are on the table, change the stance of BoardManager to Dynamic
				CurrentGameStance = GameStance.Dynamic;
				Debug.Log("All pieces are on the table. Stance changed to Dynamic.");
				yield break; // Completely stop the loop
			}
		}
	}

	void Update()
	{
		if (CurrentGameStance == GameStance.Static)
		{
			HandleStaticStateInput();
		}
		if (CurrentGameStance == GameStance.Dynamic)
		{
			HandleDynamicStateInput();
		}
	}

	private float lastClickTime = -1f; // Tracks the last mouse click time
	private float clickCooldown = 0.05f; // Minimum time between clicks (in seconds)

	public void HandleDynamicStateInput()
	{
		// Raycast for any object
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
		{
			if (hit.collider.gameObject.name == "CancelMove")
			{
				CancelMove();
				return;
			}
		}

		// Ignore clicks if within the cooldown period
		if (Time.time - lastClickTime < clickCooldown) return;

		// Update the last click time
		lastClickTime = Time.time;

		// Proceed only if the left mouse button is pressed
		if (!Input.GetMouseButton(0)) return;

		// Exit if the mouse is outside the screen bounds
		if (Input.mousePosition.x < 0 || Input.mousePosition.y < 0 ||
			Input.mousePosition.x > Screen.width || Input.mousePosition.y > Screen.height)
		{
			return; // End the process
		}

		// Raycast for Piece
		Ray pieceRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(pieceRay, out RaycastHit pieceHit, Mathf.Infinity, LayerMask.GetMask("Piece")))
		{
			Piece piece = pieceHit.collider.GetComponent<Piece>();

			// If a Piece is selected and PlayerStance is Think
			if (CurrentPlayerStance == PlayerStance.Think)
			{
				HandlePieceSelection(pieceHit.collider.gameObject);
			}
		}

		// Raycast for Cell
		Ray cellRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(cellRay, out RaycastHit cellHit, Mathf.Infinity, LayerMask.GetMask("Cell")))
		{
			// If a Cell is clicked and an object is being held
			if (CurrentPlayerStance == PlayerStance.Hold)
			{
				HandleCellClickWhileOnTable(cellHit.collider.gameObject);
				return; // Exit after handling the Cell click
			}
		}
	}

	private void HandleStaticStateInput()
	{
		// Proceed only if the left mouse button is pressed
		if (!Input.GetMouseButton(0)) return;
		// Raycast for any object
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
		{
			if (hit.collider.gameObject.name == "CancelMove")
			{
				CancelMove();
				return;
			}
		}

		// Exit if the mouse is outside the screen bounds
		if (Input.mousePosition.x < 0 || Input.mousePosition.y < 0 ||
			Input.mousePosition.x > Screen.width || Input.mousePosition.y > Screen.height)
		{
			return; // End the process
		}

		// Raycast for Cell
		Ray cellRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(cellRay, out RaycastHit cellHit, Mathf.Infinity, LayerMask.GetMask("Cell")))
		{
			// If a Cell is clicked and an object is being held
			if (CurrentPlayerStance == PlayerStance.Hold)
			{
				HandleCellClick(cellHit.collider.gameObject);
				return; // Exit after handling the Cell click
			}
		}

		// Raycast for Piece
		Ray pieceRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(pieceRay, out RaycastHit pieceHit, Mathf.Infinity, LayerMask.GetMask("Piece")))
		{
			Piece piece = pieceHit.collider.GetComponent<Piece>();
			if (piece == null) return;

			// If a Piece is selected and PlayerStance is Think
			if (CurrentPlayerStance == PlayerStance.Think)
			{
				if (!piece.isOnTable) // If the piece is not on the table
				{
					HandlePieceSelection(pieceHit.collider.gameObject);
				}
				else if (piece.isOnTable && CurrentGameStance == GameStance.Dynamic) // If the piece is on the table
				{
					HandlePieceSelection(pieceHit.collider.gameObject);
				}
			}
		}
	}

	private void HandlePieceSelection(GameObject selectedPiece)
	{
		Holded = selectedPiece;
		CurrentPlayerStance = PlayerStance.Hold;

		// Animation: Raise along the Z-axis and sway left and right
		Vector3 originalPosition = Holded.transform.position;
		Holded.transform.DOMove(originalPosition + new Vector3(0, 0, -1), 0.5f).SetEase(Ease.OutQuad); // Raise along the Z-axis

		Holded.transform.DORotate(new Vector3(0, 0, 10), 0.3f) // Sway left and right
			.SetLoops(-1, LoopType.Yoyo)
			.SetEase(Ease.InOutQuad);
	}
	private void HandleCellClick(GameObject clickedCell)
	{
		// Get the Cell component
		Cell cell = clickedCell.GetComponent<Cell>();
		if (cell == null) return;

		// Check if the cell is occupied
		if (cell.IsOccupied)
		{
			Debug.Log("Cell is occupied!");
			return; // Do not allow placing the piece on an occupied cell
		}

		// Stop DOTween animations
		Holded.transform.DOKill();
		Holded.transform.rotation = Quaternion.identity;

		// Update piece properties
		Piece heldPiece = Holded.GetComponent<Piece>();
		if (heldPiece != null)
		{
			heldPiece.isOnTable = true;
			heldPiece.SetCurrentCell(cell);
		}

		// Set the clicked cell as the parent of the piece
		// Holded.transform.SetParent(cell.transform);

		// Move the held object to the clicked Cell's local position
		Holded.transform.position = new Vector3(cell.transform.position.x, cell.transform.position.y, -1);

		// Mark the cell as occupied
		cell.IsOccupied = true;
		cell.lastPiece = Holded.GetComponent<Piece>();

		// Reset the Holded reference and change PlayerStance to Think
		Holded = null;
		CurrentPlayerStance = PlayerStance.Think;
		CheckVictoryCondition();
		Debug.Log($"Piece placed on {cell.name}");
	}

	private void HandleCellClickWhileOnTable(GameObject clickedCell)
	{
		// Get the Cell component of the clicked cell
		Cell cell = clickedCell.GetComponent<Cell>();
		if (cell == null) return;

		// Get the held piece and its current cell
		Piece heldPiece = Holded.GetComponent<Piece>();
		if (heldPiece == null || heldPiece.CurrentCell == null) return;

		Cell currentCell = heldPiece.CurrentCell;

		ResetAllCellsPossibilities();
		// Update possibilities for Bishop, Rock, and Horse
		UpdateBishopPossibilities(currentCell);
		UpdateRockPossibilities(currentCell);
		UpdateHorsePossibilities(currentCell);

		// Check if the clicked cell is already occupied
		if (cell.IsOccupied)
		{
			Debug.Log("Cell is occupied!");
			return; // Do not allow placing the piece on an occupied cell
		}

		// Check movement possibility based on piece type
		if (heldPiece.pieceType == Piece.PieceType.Horse && !cell.isHorsePossible)
		{
			Debug.Log("This cell is not valid for a Horse!");
			return; // Do not allow placing a Horse on an invalid cell
		}
		if (heldPiece.pieceType == Piece.PieceType.Bishop && !cell.isBishopPossible)
		{
			Debug.Log("This cell is not valid for a Bishop!");
			return; // Do not allow placing a Bishop on an invalid cell
		}
		if (heldPiece.pieceType == Piece.PieceType.Rock && !cell.isRockPossible)
		{
			Debug.Log("This cell is not valid for a Rock!");
			return; // Do not allow placing a Rock on an invalid cell
		}

		// Mark the current cell as unoccupied
		if (heldPiece.CurrentCell != null)
		{
			heldPiece.CurrentCell.IsOccupied = false;
		}
		currentCell.lastPiece = null;

		// Stop DOTween animations for the held piece
		Holded.transform.DOKill();
		Holded.transform.rotation = Quaternion.identity;

		// Mark the piece as being on the table
		heldPiece.isOnTable = true;

		// Move the held piece to the clicked cell's position
		Holded.transform.DOMove(new Vector3(clickedCell.transform.position.x, clickedCell.transform.position.y, -1), 0.5f)
			.SetEase(Ease.OutQuad);

		// Update the current cell of the held piece
		heldPiece.SetCurrentCell(cell);

		// Mark the clicked cell as occupied
		cell.IsOccupied = true;
		cell.lastPiece = Holded.GetComponent<Piece>();

		// Reset the held piece and update player stance
		Holded = null;
		CurrentPlayerStance = PlayerStance.Think;
		CheckVictoryCondition();
		Debug.Log($"Piece placed on {cell.name}");
	}

	private void UpdateRockPossibilities(Cell currentCell)
	{
		// Parse the cell name to extract its position (e.g., "Cell.9" -> 9)
		string cellName = currentCell.name;
		if (!int.TryParse(cellName.Split('.')[1], out int cellNumber)) return;

		// Define matrix dimensions
		int matrixSize = 3; // Assuming a 3x3 grid
		int row = (cellNumber - 1) / matrixSize; // Row index (0-based)
		int col = (cellNumber - 1) % matrixSize; // Column index (0-based)

		// Iterate over the matrix
		for (int r = 0; r < matrixSize; r++)
		{
			for (int c = 0; c < matrixSize; c++)
			{
				// Check for same row or same column (horizontal or vertical)
				if ((r == row || c == col) && (r != row || c != col)) // Exclude the current cell
				{
					string targetCellName = $"Cell.{r * matrixSize + c + 1}";
					GameObject targetCell = GameObject.Find(targetCellName);
					if (targetCell != null)
					{
						Cell targetCellComponent = targetCell.GetComponent<Cell>();
						if (targetCellComponent != null)
						{
							targetCellComponent.isRockPossible = true;
						}
					}
				}
			}
		}
	}

	// Update isBishopPossible for diagonal cells
	private void UpdateBishopPossibilities(Cell currentCell)
	{
		// Parse the cell name to extract its position (e.g., "Cell.9" -> 9)
		string cellName = currentCell.name;
		if (!int.TryParse(cellName.Split('.')[1], out int cellNumber)) return;

		// Define matrix dimensions
		int matrixSize = 3; // Assuming a 3x3 grid
		int row = (cellNumber - 1) / matrixSize; // Row index (0-based)
		int col = (cellNumber - 1) % matrixSize; // Column index (0-based)

		// Iterate over diagonal cells
		for (int r = 0; r < matrixSize; r++)
		{
			for (int c = 0; c < matrixSize; c++)
			{
				if (Mathf.Abs(r - row) == Mathf.Abs(c - col) && (r != row || c != col)) // Diagonal check
				{
					string targetCellName = $"Cell.{r * matrixSize + c + 1}";
					GameObject targetCell = GameObject.Find(targetCellName);
					if (targetCell != null)
					{
						Cell targetCellComponent = targetCell.GetComponent<Cell>();
						if (targetCellComponent != null)
						{
							targetCellComponent.isBishopPossible = true;
						}
					}
				}
			}
		}
	}

	private void UpdateHorsePossibilities(Cell currentCell)
	{
		// Parse the cell name to extract its position (e.g., "Cell.9" -> 9)
		string cellName = currentCell.name;
		if (!int.TryParse(cellName.Split('.')[1], out int cellNumber)) return;

		// Define matrix dimensions
		int matrixSize = 3; // Assuming a 3x3 grid
		int row = (cellNumber - 1) / matrixSize; // Row index (0-based)
		int col = (cellNumber - 1) % matrixSize; // Column index (0-based)

		// Iterate over the matrix
		for (int r = 0; r < matrixSize; r++)
		{
			for (int c = 0; c < matrixSize; c++)
			{
				// Skip the current cell and directly adjacent cells
				if (Mathf.Abs(r - row) <= 1 && Mathf.Abs(c - col) <= 1)
				{
					continue; // Horse cannot move to cells adjacent to its own
				}

				// Check the cell number to determine the parity (odd/even) rule
				int targetCellNumber = r * matrixSize + c + 1; // Calculate the target cell number
				bool isTargetEven = targetCellNumber % 2 == 0;
				bool isCurrentEven = cellNumber % 2 == 0;

				// Skip if the target cell doesn't satisfy the odd/even rule
				if (isCurrentEven == isTargetEven)
				{
					continue; // Cannot move to cells with the same parity
				}

				// Find and update the target cell
				string targetCellName = $"Cell.{targetCellNumber}";
				GameObject targetCell = GameObject.Find(targetCellName);
				if (targetCell != null)
				{
					Cell targetCellComponent = targetCell.GetComponent<Cell>();
					if (targetCellComponent != null)
					{
						targetCellComponent.isHorsePossible = true;
					}
				}
			}
		}
	}

	public Cell[] cells; // The 9 cells will be assigned here (assignable in the Inspector)

	public void CheckVictoryCondition()
	{
		// Rows
		CheckLine(cells[0], cells[1], cells[2]);
		CheckLine(cells[3], cells[4], cells[5]);
		CheckLine(cells[6], cells[7], cells[8]);

		// Columns
		CheckLine(cells[0], cells[3], cells[6]);
		CheckLine(cells[1], cells[4], cells[7]);
		CheckLine(cells[2], cells[5], cells[8]);

		// Diagonals
		CheckLine(cells[0], cells[4], cells[8]);
		CheckLine(cells[2], cells[4], cells[6]);
	}

	private void CheckLine(Cell cell1, Cell cell2, Cell cell3)
	{
		// Ensure all cells have a lastPiece
		if (cell1.lastPiece == null || cell2.lastPiece == null || cell3.lastPiece == null) return;

		// Check if all pieces have the same color
		if (cell1.lastPiece.pieceColor == cell2.lastPiece.pieceColor &&
			cell2.lastPiece.pieceColor == cell3.lastPiece.pieceColor)
		{
			Debug.Log("Victory: " + cell1.lastPiece.pieceColor);
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		}
	}

	private void CancelMove()
	{
		if (Holded == null) return;

		// Stop any ongoing DOTween animations for the held piece
		Holded.transform.DOKill();

		// Reset the position of the held piece to its original position
		if (Holded.GetComponent<Piece>().CurrentCell != null)
		{
			Vector3 originalPosition = Holded.GetComponent<Piece>().CurrentCell.transform.position;
			Holded.transform.position = new Vector3(originalPosition.x, originalPosition.y, -1);
		}

		// Reset rotation
		Holded.transform.rotation = Quaternion.identity;

		// Reset PlayerStance to Think
		CurrentPlayerStance = PlayerStance.Think;

		// Clear the reference to the held object
		Holded = null;

		Debug.Log("Move canceled. Piece returned to its original position.");
	}

	public void ResetAllCellsPossibilities()
	{
		foreach (Cell cell in cells)
		{
			if (cell != null)
			{
				cell.isRockPossible = false;
				cell.isBishopPossible = false;
				cell.isHorsePossible = false;
			}
		}
		Debug.Log("All cell possibilities have been reset.");
	}
}


