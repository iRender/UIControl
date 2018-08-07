using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace iRender.UIControll
{
    public class TableViewCell : MonoBehaviour
    {
        public string IdentifierInPool
        {
            get
            {
                return GetType().Name;
            }
        }      
    }
}