using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TelloAgent : MonoBehaviour {

    public GameObject TelloController;
    private TelloController _controller;

    void Start() {
        TelloController = GameObject.Find("TelloController");
        _controller = TelloController.GetComponent<TelloController>();
        _controller.Connect();

        _controller.onConnected += (TelloLib.Tello.ConnectionState newState) => {
            Debug.Log("Connected" + newState);
            _controller.TakeOff();
            //3秒後に着陸
            StartCoroutine(Land());
        };
    }

    IEnumerator Land() {
        yield return new WaitForSeconds(3);
        _controller.Land();
    }

}
