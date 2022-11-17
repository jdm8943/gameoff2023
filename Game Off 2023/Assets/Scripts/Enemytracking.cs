using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemytracking : MonoBehaviour
{
    public Transform Player;
    int movespeed = 4;
    int MaxDis = 10;
    int MinDis = 2;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        /* transform.LookAt(Player);*/

        transform.position = Vector2.MoveTowards(transform.position, Player.transform.position, movespeed * Time.deltaTime);

       /*if (Vector3.Distance(transform.position,Player.position)>=MinDis)
        {
            transform.position += transform.forward * movespeed * Time.deltaTime;

            if(Vector3.Distance(transform.position,Player.position)<=MaxDis)
            {
                movespeed=0;
            }
        }*/
    }
}
