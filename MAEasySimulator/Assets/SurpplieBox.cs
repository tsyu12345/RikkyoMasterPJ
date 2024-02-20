using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurpplieBox : MonoBehaviour {
    public delegate void OnLanding();
    public OnLanding onLandingShelter;
    private Vector3 StartPos;

    void Start() {
        StartPos = transform.localPosition;
    }

    private void OnTriggerEnter(Collider other) {
        Debug.LogWarning("SurpplieBox: OnTriggerEnter" + other.tag);
        if (other.CompareTag("Shelter")) {
            onLandingShelter?.Invoke();
            Reset();
        }
    }

    private void Reset() {
        transform.localPosition = StartPos;
        transform.localRotation = Quaternion.Euler(0, 0, 0);
    }
}
