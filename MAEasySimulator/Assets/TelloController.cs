using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TelloLib;

public class TelloController : MonoBehaviour {
    public delegate void OnConnection(TelloLib.Tello.ConnectionState newState);
    public OnConnection onConnected;
    private string LogPrefix = "[TelloController]";

    void Start() {
        Tello.onConnection += OnConnect;
    }

    public void Connect() {
        try {
            Tello.startConnecting();
        } catch (Exception) {
            Debug.LogError(LogPrefix + "ConnectionFailed");
        }
    }

    public void TakeOff() {
        Tello.takeOff();
    }

    public void Land() {
        Tello.land();
    }

    private void OnConnect(TelloLib.Tello.ConnectionState newState) {
        if (newState == Tello.ConnectionState.Connected) {
            Debug.Log(this.LogPrefix + "Tello Connected");
            onConnected?.Invoke(newState);
        }
    }
}
