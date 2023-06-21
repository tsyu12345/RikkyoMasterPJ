using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour {

    //obstacleタグが付与されているオブジェクトのリスト
    public List<GameObject> obstacles = new List<GameObject>();

    // Start is called before the first frame update
    void Start() {
        //obstacleタグが付与されているオブジェクトを全て取得
        GameObject[] obs = GameObject.FindGameObjectsWithTag("obstacle");
        foreach(GameObject ob in obs) {
            obstacles.Add(ob);
        }
    }

    // Update is called once per frame
    void Update(){
        
    }


    /**
    * 自身の位置がobstacleタグが付与されているオブジェクトと重なった場合、
    * 自身の位置をフィールド内のランダムな位置に移動させる
    */
    void OnCollisionEnter(Collision collision) {
        if(collision.gameObject.tag == "obstacle") {
            transform.localPosition = new Vector3(Random.Range(-4f, 4f), Random.Range(1.0f, 25), Random.Range(-4.8f, 4.8f));
        }
    }


}
