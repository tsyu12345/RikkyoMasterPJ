using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurpplieBox : MonoBehaviour {
    public delegate void OnLanding();
    public OnLanding onLandingShelter;
    public delegate void InRangeCanGet();
    public InRangeCanGet inRangeCanGet;
    public Vector3 StartPos;
    public delegate void OutRangeCanGet();
    public OutRangeCanGet outRangeCanGet;
    public string GetableTag;

    void Start() {
        StartPos = transform.localPosition;
    }

    void Update() {
        if(transform.position.y < 0) {
            Reset();
        }
    }

    private void OnTriggerEnter(Collider other) {
        ///Debug.LogWarning("SurpplieBox: OnTriggerEnter" + other.tag);
        if (other.CompareTag("Shelter")) {
            onLandingShelter?.Invoke();
            Debug.LogWarning("SurpplieBox: OnLandingShelter");
            Reset();
        } else if (other.CompareTag("Obstacle")) {
            Debug.LogWarning("SurpplieBox: OnCrash");
            Reset();
        }
    }

    private void OnTriggerExit(Collider other) {
        if(other.CompareTag(GetableTag)) {
            outRangeCanGet?.Invoke();
        }
    }

    private void OnTriggerStay(Collider other) {
        if(other.CompareTag(GetableTag)) {
            inRangeCanGet?.Invoke();
        }
    }

    public void Reset() {
        transform.localPosition = StartPos;
        transform.localRotation = Quaternion.Euler(0, 0, 0);
        Debug.Log("Reset Supplie");
    }
}
