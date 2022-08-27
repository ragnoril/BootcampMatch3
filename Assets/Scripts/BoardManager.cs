using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public GameObject[] TilePrefabs;
    
    public int BoardWidth;
    public int BoardHeight;

    public int[] GameBoard;

    public Tile SelectedTile;
    public Tile SwapTile;

    public GameObject Explosion;
    public GameObject Empty;

    public event Action OnBoardMove;
    public event Action<int> OnTilesPopped;

    public void InitBoard()
    {
        SelectedTile = null;
        SwapTile = null;

        GameBoard = new int[BoardWidth * BoardHeight];

        for(int i = 0; i < BoardWidth * BoardHeight; i++)
        {
            GameBoard[i] = -1;
        }

        FillBoardRandomly();
        GenerateBoard();
    }

    private void GenerateBoard()
    {
        for (int i = 0; i < BoardWidth; i++)
        {
            for(int j = 0; j < BoardHeight; j++)
            {
                GameObject emptyTile = Instantiate(Empty);
                emptyTile.transform.SetParent(transform);
                emptyTile.transform.localPosition = new Vector3(i, -j, 0f);

                if (GameBoard[i] < 0) continue;

                GameObject tile = Instantiate(TilePrefabs[GameBoard[(j * BoardWidth) + i]]);
                tile.transform.SetParent(transform);
                tile.transform.localPosition = new Vector3(i, -j, 1f);
            }
        }
    }

    private void FillBoardRandomly()
    {
        for (int i = 0; i < BoardWidth * BoardHeight; i++)
        {
            GameBoard[i] = UnityEngine.Random.Range(0,TilePrefabs.Length);
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hitInfo = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hitInfo.collider != null)
            {
                SelectedTile = hitInfo.collider.GetComponent<Tile>();
                Debug.Log("Candy selected: " + SelectedTile.TileType + " position: " + SelectedTile.transform.position);

            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            int selectedX = (int)SelectedTile.transform.localPosition.x;
            int selectedY = (int)(SelectedTile.transform.localPosition.y * -1);

            int swipeX = (int)(Mathf.Clamp(Mathf.Round(mousePos.x - SelectedTile.transform.position.x), -1f, 1f));
            int swipeY = (int)Mathf.Clamp(Mathf.Round(SelectedTile.transform.position.y - mousePos.y), -1f, 1f);

            int x = selectedX + swipeX;
            int y = selectedY + swipeY;

            SwapTile = GetTile(x, y);
            if (SwapTile != null)
            {
                Debug.Log("Candy swiped: " + SwapTile.TileType + " position: " + SwapTile.transform.position);

                int swapX = (int)SwapTile.transform.localPosition.x;
                int swapY = (int)(SwapTile.transform.localPosition.y * -1);

                int selectedPos = GetBoardPosition(selectedX, selectedY);
                int swapPos = GetBoardPosition(swapX, swapY);
                GameBoard[swapPos] = SelectedTile.TileType;
                GameBoard[selectedPos] = SwapTile.TileType;

                Vector3 tempPos = SwapTile.transform.position;
                SwapTile.transform.position = SelectedTile.transform.position;
                SelectedTile.transform.position = tempPos;

                OnBoardMove?.Invoke();

                CheckForCombos();
                CheckForEmptySpaces();
                FillEmptySpaces();
            }

        }
    }

    private void FillEmptySpaces()
    {
        for (int i = 0; i < BoardWidth; i++)
        {
            for (int j = 0; j < BoardHeight; j++)
            {
                int pos = GetBoardPosition(i, j);
                if (GameBoard[pos] < 0)
                {
                    GameBoard[pos] = UnityEngine.Random.Range(0, TilePrefabs.Length);
                    GameObject tile = Instantiate(TilePrefabs[GameBoard[pos]]);
                    tile.transform.SetParent(transform);
                    tile.transform.localPosition = new Vector3(i, -j, 1f);
                }
            }
        }
    }

    private void CheckForEmptySpaces()
    {
        for (int i = 0; i < BoardWidth; i++)
        {
            for (int j = (BoardHeight - 2); j > -1 ; j--)
            {
                if (GameBoard[GetBoardPosition(i, j)] < 0) continue;

                Tile tile = GetTile(i, j);
                Debug.Log("tile: " + tile);
                int y = j + 1;
                Debug.Log("check for x: " + i + " y: " + j.ToString());

                while (y < BoardHeight && GameBoard[GetBoardPosition(i, y)] < 0)
                {
                    Debug.Log("swap x: " + i + " y: " + j + " with x: " + i + " y: " + y.ToString());
                    Debug.Log("tile: " + tile);

                    if (tile == null)
                    {
                        Debug.Log("get tile x: " + i + " y: " + (y - 1));
                        tile = GetTile(i, y - 1);
                    }

                    tile.transform.localPosition = new Vector2(i, -y);
                    GameBoard[GetBoardPosition(i, y - 1)] = -1;
                    GameBoard[GetBoardPosition(i, y)] = tile.TileType;
                    y++;
                }
            }
        }
    }

    private void CheckForCombos()
    {
        int popCount = 0;
        int selectedX = (int)SelectedTile.transform.localPosition.x;
        int selectedY = (int)(SelectedTile.transform.localPosition.y * -1);

        int swapX = (int)SwapTile.transform.localPosition.x;
        int swapY = (int)(SwapTile.transform.localPosition.y * -1);

        // selected tile
        List<Tile> comboH = CheckCombosHorizontal(selectedX, selectedY, SelectedTile.TileType);
        List<Tile> comboV = CheckCombosVertical(selectedX, selectedY, SelectedTile.TileType);

        bool isSelectedPops = false;

        if (comboH.Count > 1)
        {
            popCount += comboH.Count;
            foreach (Tile tile in comboH)
                PopTile(tile);

            isSelectedPops = true;
        }

        if (comboV.Count > 1)
        {
            popCount += comboV.Count;
            foreach (Tile tile in comboV)
                PopTile(tile);

            isSelectedPops = true;
        }

        if (isSelectedPops)
        {
            PopTile(SelectedTile);
            popCount += 1;
        }

        
        // swap tile
        comboH = CheckCombosHorizontal(swapX, swapY, SwapTile.TileType);
        comboV = CheckCombosVertical(swapX, swapY, SwapTile.TileType);

        bool isSwappedPops = false;

        if (comboH.Count > 1)
        {
            popCount += comboH.Count;
            foreach (Tile tile in comboH)
                PopTile(tile);

            isSwappedPops = true;
        }

        if (comboV.Count > 1)
        {
            popCount += comboV.Count;
            foreach (Tile tile in comboV)
                PopTile(tile);

            isSwappedPops = true;
        }

        if (isSwappedPops)
        {
            PopTile(SwapTile);
            popCount += 1;
        }

        if (popCount > 0)
            OnTilesPopped?.Invoke(popCount);
    }

    public List<Tile> CheckCombosHorizontal(int x, int y, int tileType)
    {
        List<Tile> comboList = new List<Tile>();

        int preX = x - 1;
        int postX = x + 1;
        while (preX > -1 || postX < BoardWidth)
        {
            if (preX > -1)
            {
                int prePos = GetBoardPosition(preX, y);
                if (GameBoard[prePos] == tileType)
                {
                    Tile tile = GetTile(preX, y);
                    if (tile != null)
                    {
                        comboList.Add(tile);
                        preX -= 1;
                    }
                }
                else
                {
                    preX = -1;
                }
            }

            if (postX < BoardWidth)
            {
                int postPos = GetBoardPosition(postX, y);
                if (GameBoard[postPos] == tileType)
                {
                    Tile tile = GetTile(postX, y);
                    if (tile != null)
                    {
                        comboList.Add(tile);
                        postX += 1;
                    }
                }
                else
                {
                    postX = BoardWidth;
                }
            }
        }

        return comboList;
    }

    public List<Tile> CheckCombosVertical(int x, int y, int tileType)
    {
        List<Tile> comboList = new List<Tile>();

        int preY = y - 1;
        int postY = y + 1;
        while (preY > -1 || postY < BoardHeight)
        {
            if (preY > -1)
            {
                int prePos = GetBoardPosition(x, preY);
                if (GameBoard[prePos] == tileType)
                {
                    Tile tile = GetTile(x, preY);
                    if (tile != null)
                    {
                        comboList.Add(tile);
                        preY -= 1;
                    }
                }
                else
                {
                    preY = -1;
                }
            }

            if (postY < BoardHeight)
            {
                int postPos = GetBoardPosition(x, postY);
                if (GameBoard[postPos] == tileType)
                {
                    Tile tile = GetTile(x, postY);
                    if (tile != null)
                    {
                        comboList.Add(tile);
                        postY += 1;
                    }
                }
                else
                {
                    postY = BoardHeight;
                }
            }
        }

        return comboList;
    }

    public int GetBoardPosition(int x, int y)
    {
        return ((y * BoardWidth) + x);
    }

    public Tile GetTile(int x, int y)
    {
        Debug.Log("Checking for tile x: " + x + " y: " + y);
        //RaycastHit2D tileHit = Physics2D.Raycast(transform.position + new Vector3(x, -y, 0f), Vector2.zero);
        Collider2D tileHit = Physics2D.OverlapPoint(new Vector2(transform.position.x + x, transform.position.y - y));
        if (tileHit != null)
        {
            return tileHit.GetComponent<Tile>();
        }

        return null;
    }

    public void PopTile(Tile tile)
    {
        int tileX = (int)tile.transform.localPosition.x;
        int tileY = (int)(tile.transform.localPosition.y * -1);

        GameBoard[GetBoardPosition(tileX, tileY)] = -1;

        GameObject exp = Instantiate(Explosion, tile.transform.position, Quaternion.identity);

        Destroy(exp, 1f);
        Destroy(tile.gameObject);
    }

}
