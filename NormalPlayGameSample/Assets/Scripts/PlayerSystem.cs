using UnityEngine;

public class PlayerSystem : MonoBehaviour
{
    private int point;
    public int Point {
        get { return this.point; }
    }

    void Start()
    {
        this.point = 0;
    }

    public void AddPoint(int point)
    {
        this.point += point;
    }
}
