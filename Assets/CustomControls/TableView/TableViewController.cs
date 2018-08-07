using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace iRender.UIControll
{
    public class TableViewController : MonoBehaviour
    {
        public TableView tableView;
        public TableViewCell protocolCell;
        private int[] m_cellModels;

        void Awake()
        {
            m_cellModels = new int[100];
            for (int i = 0; i < m_cellModels.Length; i++)
            {
                m_cellModels[i] = i;
            }
            tableView.CellCount = m_cellModels.Length;
            tableView.m_cellForTableView = GetCell;

            List<int> testList = new List<int>();
            testList.Add(1);
            testList.Add(2);
            testList.Add(3);
            testList.Clear();
            testList.Capacity = 1;
        }

        public TableViewCell GetCell(int index)
        {
            TableViewCell cell = tableView.GetPoolInCell(protocolCell.IdentifierInPool);
            if (cell == null)
            {
                cell = GameObject.Instantiate(protocolCell.gameObject).GetComponent<TableViewCell>();
            }
            cell.GetComponent<TestCell>().LoadView(m_cellModels[index]);
            return cell;
        }

        private void OnGUI()
        {
            if (GUI.Button(new Rect(0, 0, 100, 100), "Reload"))
            {
                for (int i = 0; i < m_cellModels.Length; i++)
                {
                    m_cellModels[i] = -m_cellModels[i];
                }
                tableView.LoadView();
            }
        }
    }
}