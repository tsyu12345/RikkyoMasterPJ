using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Types : MonoBehaviour {
    
    [System.Serializable]
    public class MessageData {
        public string type;
        public string content;

        public string ToJson() {
            return JsonUtility.ToJson(this);
        }

        public static MessageData FromJson(string json) {
            return JsonUtility.FromJson<MessageData>(json);
        }
    }
}
