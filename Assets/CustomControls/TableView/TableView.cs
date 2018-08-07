//  Created by felix on 2018/7/11
//  Copyright (c) 2018 thedream.cc.  All rights reserved.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

namespace iRender.UIControll
{
    [RequireComponent(typeof(ScrollRect))]
    public class TableView : MonoBehaviour
    {
        private RectTransform rectTransform;
        private ScrollRect m_sr;
        private GridLayoutGroup m_layoutGroup;

        private int m_cellCount;
        public int CellCount
        {
            set
            {
                m_cellCount = value;
            }
        }
        public delegate TableViewCell CellForTableView(int index);
        public CellForTableView m_cellForTableView;

        private int m_rowCount;
        private int m_tailRowCellCount;
        private float[] m_cumulativeHeights;
        private float m_scrollOffset;
        private float m_maxScrollOffset;
        private Dictionary<string, LinkedList<TableViewCell>> m_poolInCells;
        private Dictionary<int, List<TableViewCell>> m_visibleCells;

        private Vector2Int m_visibleRowRange = Vector2Int.zero;

        public class OnCellBeInvisible : UnityEvent<TableViewCell, bool> { }
        private OnCellBeInvisible m_onCellBeInvisible;
        public void RegisterCellBeInvisibleCallback(OnCellBeInvisible callback)
        {
            m_onCellBeInvisible = callback;
        }

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            m_sr = GetComponent<ScrollRect>();
            m_layoutGroup = m_sr.content.GetComponent<GridLayoutGroup>();
        }

        void Start()
        {
            Initialize();
            if (m_cellForTableView != null)
            {
                LoadView();
            }
        }

        void Initialize()
        {
            CaculateRowCount();
            m_cumulativeHeights = new float[m_rowCount];
            for (int i = 0; i < m_cumulativeHeights.Length; i++)
            {
                m_cumulativeHeights[i] = m_layoutGroup.cellSize.y;
                if (i > 0)
                {
                    m_cumulativeHeights[i] += m_cumulativeHeights[i - 1];
                }
            }

            if (m_poolInCells == null)
            {
                m_poolInCells = new Dictionary<string, LinkedList<TableViewCell>>();
            }
        }

        private void OnEnable()
        {
            m_sr.onValueChanged.AddListener(OnScrollValueChange);
        }

        private void OnDisable()
        {
            m_sr.onValueChanged.RemoveListener(OnScrollValueChange);
        }

        private float m_screenWidth = Screen.width;
        private float m_screenHeight = Screen.height;
        void Update()
        {
            if (Screen.width != m_screenWidth || Screen.height != m_screenHeight)
            {
                m_layoutGroup.constraintCount = Mathf.FloorToInt(rectTransform.rect.width / m_layoutGroup.cellSize.x);
                Initialize();
                ClearVisibleRows();
                SetCellsListCapacity();
                SetVisibleCells();
                m_screenWidth = Screen.width;
                m_screenHeight = Screen.height;
            }
        }

        public void LoadView()
        {
            if (m_visibleCells == null)
            {
                m_visibleCells = new Dictionary<int, List<TableViewCell>>();
                CaculateVisibleRowRange();
            }
            else
            {
                ClearVisibleRows();
                m_preventOnScrollValueChange = true;
                Vector2 contentAnchorPos = m_sr.content.anchoredPosition;
                contentAnchorPos.y = 0;
                m_sr.content.anchoredPosition = contentAnchorPos;
                m_preventOnScrollValueChange = false;
            }
            SetVisibleCells();
        }

        private void SetVisibleCells()
        {
            for (int i = m_visibleRowRange.x; i <= m_visibleRowRange.y; i++)
            {
                AddBottomRowCells(i);
            }
        }

        private TableViewCell AddTopCell(int cell_index)
        {
            TableViewCell cell = m_cellForTableView(cell_index);
            cell.transform.SetParent(m_sr.content, false);
            cell.transform.SetSiblingIndex(0);
            return cell;
        }

        private TableViewCell AddBottomCell(int cell_index)
        {
            TableViewCell cell = m_cellForTableView(cell_index);
            cell.transform.SetParent(m_sr.content, false);
            cell.transform.SetSiblingIndex(m_sr.content.childCount - 1);
            return cell;
        }

        private bool m_preventOnScrollValueChange;
        private void OnScrollValueChange(Vector2 value)
        {
            if (m_preventOnScrollValueChange) return;
            m_scrollOffset = (1 - value.y) * m_maxScrollOffset;
            Vector2Int oldVisibleRowRange = m_visibleRowRange;
            CaculateVisibleRowRange();
            UpdateVisibleCells(oldVisibleRowRange);
        }

        private void CaculateRowCount()
        {
            m_rowCount = m_cellCount / m_layoutGroup.constraintCount;
            m_tailRowCellCount = m_cellCount % m_layoutGroup.constraintCount;
            if (m_tailRowCellCount > 0)
            {
                m_rowCount++;
            }
            else
            {
                m_tailRowCellCount = m_layoutGroup.constraintCount;
            }

            float contentHeight = m_rowCount * m_layoutGroup.cellSize.y;
            Vector2 sizeDelta = m_sr.content.sizeDelta;
            sizeDelta.y = contentHeight;
            m_sr.content.sizeDelta = sizeDelta;
            m_maxScrollOffset = contentHeight - rectTransform.rect.height;
        }

        private int FindIndexFromOffset(float offset)
        {
            return FindIndexFromOffset(0, offset, m_cumulativeHeights.Length - 1);
        }

        private int FindIndexFromOffset(int start_index, float offset, int end_index)
        {
            if (start_index == end_index)
            {
                return start_index;
            }
            int middleIndex = (start_index + end_index) / 2;
            if (m_cumulativeHeights[middleIndex] > offset)
            {
                return FindIndexFromOffset(start_index, offset, middleIndex);
            }
            else if (m_cumulativeHeights[middleIndex] < offset)
            {
                return FindIndexFromOffset(middleIndex + 1, offset, end_index);
            }
            else
            {
                return middleIndex;
            }
        }

        private void CaculateVisibleRowRange()
        {
            m_visibleRowRange.x = FindIndexFromOffset(m_scrollOffset);
            m_visibleRowRange.y = FindIndexFromOffset(m_scrollOffset + rectTransform.rect.height);
        }

        public TableViewCell GetPoolInCell(string identifier_in_pool)
        {
            LinkedList<TableViewCell> cells = null;
            if (m_poolInCells.TryGetValue(identifier_in_pool, out cells))
            {
                if (cells.Count > 0)
                {
                    TableViewCell cell = cells.First();
                    cells.RemoveFirst();
                    return cell;
                }
            }
            return null;
        }

        private void UpdateVisibleCells(Vector2Int old_visible_row_range)
        {
            if (m_visibleRowRange.x > old_visible_row_range.y || m_visibleRowRange.y < old_visible_row_range.x)
            {
                ClearVisibleRows();
                ChangeHeadPaddingContentHeight((m_visibleRowRange.x - old_visible_row_range.x) * m_layoutGroup.cellSize.y);
                SetVisibleCells();
                return;
            }

            for (int i = old_visible_row_range.x; i < m_visibleRowRange.x; i++)
            {
                RowCellsInPool(i);
                ChangeHeadPaddingContentHeight(m_layoutGroup.cellSize.y);
            }
            for (int i = m_visibleRowRange.y + 1; i <= old_visible_row_range.y; i++)
            {
                RowCellsInPool(i);
            }
            for (int i = old_visible_row_range.x - 1; i >= m_visibleRowRange.x; i--)
            {
                AddTopRowCells(i);
                ChangeHeadPaddingContentHeight(-m_layoutGroup.cellSize.y);
            }
            for (int i = old_visible_row_range.y + 1; i <= m_visibleRowRange.y; i++)
            {
                AddBottomRowCells(i);
            }
        }

        private void AddTopRowCells(int row)
        {
            List<TableViewCell> cellsList;
            if (!m_visibleCells.TryGetValue(row, out cellsList))
            {
                cellsList = new List<TableViewCell>(m_layoutGroup.constraintCount);
                m_visibleCells.Add(row, cellsList);
            }
            for (int i = cellsList.Capacity - 1; i > -1; i--)
            {
                cellsList.Add(AddTopCell(GetCellIndex(row, i)));
            }
        }

        private void AddBottomRowCells(int row)
        {
            List<TableViewCell> cellsList;
            if (!m_visibleCells.TryGetValue(row, out cellsList))
            {
                if (IsEndRow(row))
                {
                    cellsList = new List<TableViewCell>(m_tailRowCellCount);
                }
                else
                {
                    cellsList = new List<TableViewCell>(m_layoutGroup.constraintCount);
                }
                m_visibleCells.Add(row, cellsList);
            }
            for (int i = 0; i < cellsList.Capacity; i++)
            {
                cellsList.Add(AddBottomCell(GetCellIndex(row, i)));
            }
        }

        private void ChangeHeadPaddingContentHeight(float delta)
        {
            m_layoutGroup.padding.top += (int)delta;
        }

        private bool IsEndRow(int row)
        {
            return row == m_rowCount - 1;
        }

        private void ClearVisibleRows()
        {
            foreach (int row in m_visibleCells.Keys)
            {
                RowCellsInPool(row);
            }
        }

        private void RowCellsInPool(int row)
        {
            List<TableViewCell> cells = m_visibleCells[row];
            foreach (TableViewCell cell in cells)
            {
                CellInPool(cell);
            }
            cells.Clear();
        }

        private void CellInPool(TableViewCell cell)
        {
            cell.transform.SetParent(null, false);
            LinkedList<TableViewCell> cells = null;
            if (!m_poolInCells.TryGetValue(cell.IdentifierInPool, out cells))
            {
                cells = new LinkedList<TableViewCell>() { };
                m_poolInCells.Add(cell.IdentifierInPool, cells);
            }
            cells.AddLast(cell);
            if (m_onCellBeInvisible != null)
            {
                m_onCellBeInvisible.Invoke(cell, false);
            }
        }

        private int GetCellIndex(int row, int column)
        {
            return row * m_layoutGroup.constraintCount + column;
        }

        private void SetCellsListCapacity()
        {
            foreach (int row in m_visibleCells.Keys)
            {
                if (IsEndRow(row))
                {
                    m_visibleCells[row].Capacity = m_tailRowCellCount;
                }
                else
                {
                    m_visibleCells[row].Capacity = m_layoutGroup.constraintCount;
                }
            }
        }
    }
}