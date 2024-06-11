using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ClassB : MonoBehaviour
{
    public float damage;
    private ClassA classA;
    protected List<List<bool>> bools;

    public Dictionary<int, int> aaaaaaa;
    private void Awake()
    {
        damage = 10;
        classA = new ClassA();
        bools = new List<List<bool>>();
        bools.Add(new List<bool>());
        bools[0].Add(true);
        bools[0].Add(true);
        bools.Add(new List<bool>());
        bools[1].Add(false);
        bools[1].Add(false);
        bools.Add(new List<bool>());
        bools[2].Add(true);
        bools[2].Add(true);

        aaaaaaa = new Dictionary<int, int>();
        aaaaaaa.Add(1, 2);
        aaaaaaa.Add(3, 4);
        aaaaaaa.Add(5, 6);
        
    }
}
