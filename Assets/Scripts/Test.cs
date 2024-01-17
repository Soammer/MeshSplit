using UnityEngine;

public class Test : MonoBehaviour
{
    private float Timer;
    public void Update()
    {
        Timer += Time.deltaTime;
        if(Timer > 1)
        {
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(Input.mousePosition);
            Vector3 StartPos = Camera.main.ScreenToWorldPoint(screenPosition);
            Debug.Log(Input.mousePosition + "\r\n" + screenPosition + "\r\n" + StartPos);
            Timer = 0;
        }
    }
}