using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class EnvManager : MonoBehaviour {
    
    public List<PickupAgent> PickupAgents;
    public List<TransmitAgent> TransmitAgents;
    public List<GameObject> Supplies;
    private SimpleMultiAgentGroup PickupAgentGroup;
    private SimpleMultiAgentGroup TransmitAgentGroup;

}
