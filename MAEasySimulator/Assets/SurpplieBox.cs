using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurpplieBox : MonoBehaviour {
    public delegate void OnLanding();
    public OnLanding onLandingShelter;

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Shelter")) {
            onLandingShelter?.Invoke();
        }
    }
}
