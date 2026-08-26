using TMPro;
using UnityEngine;

public class TextUnderliner : MonoBehaviour
{
    public void Underline()
    {
        text.fontStyle = FontStyles.Underline;
    }
    public void Ununderline()
    {
        text.fontStyle = FontStyles.Normal;
    }
    public TMP_Text text;
}
