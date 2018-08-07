using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestCell : MonoBehaviour {
    public Text text;
    public void LoadView(int index)
    {
        text.text = index.ToString();
    }
}
