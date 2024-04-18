using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurpplieBox : MonoBehaviour {
    public delegate void OnLanding();
    public OnLanding onLandingShelter;
    public delegate void InRangeCanGet();
    public InRangeCanGet inRangeCanGet;
    public GameObject StartPosArea;
    public delegate void OutRangeCanGet();
    public OutRangeCanGet outRangeCanGet;
    public string GetableTag;

    void Start() {
        
    }

    void Update() {
        
    }

    private void OnTriggerEnter(Collider other) {
        ///Debug.LogWarning("SurpplieBox: OnTriggerEnter" + other.tag);
        if (transform.parent.CompareTag(GetableTag)) {
            return;
        }
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
        Vector3 renewPos = new Vector3(StartPosArea.transform.localPosition.x, StartPosArea.transform.localPosition.y, StartPosArea.transform.localPosition.z);
        transform.localPosition = renewPos;
        transform.localRotation = Quaternion.Euler(0, 0, 0);
        Debug.Log("SurpplieBox: Reset Supplie");
    }
}
