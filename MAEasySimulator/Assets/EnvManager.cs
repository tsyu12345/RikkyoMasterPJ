using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;


public class EnvManager : MonoBehaviour {

    [Header("GameObjects")]
    public GameObject FieldPlane;
    public GameObject Field;

    public List<GameObject> buildings;
    public GameObject DroneStation;
    [Header("Agent Groups")]
    public List<GameObject> Agents;
    public List<string> Teams; //TODO:indexで参照するのをやめる
    private SimpleMultiAgentGroup SurpplierGroup;
    private SimpleMultiAgentGroup SpyAgentGroup;

    void Start() {
        SurpplierGroup = new SimpleMultiAgentGroup();
        SpyAgentGroup = new SimpleMultiAgentGroup();
        foreach(GameObject drone in Agents) {
            if (drone.tag == Teams[0]) {
                SurpplierGroup.RegisterAgent(drone.GetComponent<Agent>());
            } else if (drone.tag == Teams[1]) {
                SpyAgentGroup.RegisterAgent(drone.GetComponent<Agent>());
            }
        }
    }
    
    public void InitializeRandomPositions(float someMinimumDistance = 10f) {
        Vector3 fieldPlaneSize = FieldPlane.GetComponent<Collider>().bounds.size;
        Vector3 fieldPlaneCenter = FieldPlane.transform.position;
        int maxAttempts = 100; // 最大試行回数を設定
        int attempts = 0;

        //buildingsの位置をランダムに設定（DroneStationとTODO:他のbuildingと重ならないように）
        foreach (GameObject building in buildings) {
            Vector3 newBuildingPos;
            do {
                newBuildingPos = GenerateRandomPosition(fieldPlaneCenter, fieldPlaneSize);
                attempts++;
            } while (Vector3.Distance(newBuildingPos, DroneStation.transform.position) < someMinimumDistance && attempts < maxAttempts);
            
            if (attempts >= maxAttempts) {
                Debug.LogWarning("Failed to place building sufficiently apart from DroneStation");
                return; // 適切な位置を見つけられなかった場合は処理を中断 無限ループを防ぐため
            }
            building.transform.position = newBuildingPos;
        }        
    }

    private Vector3 GenerateRandomPosition(Vector3 center, Vector3 size) {
        float x = Random.Range(center.x - size.x / 2, center.x + size.x / 2);
        float z = Random.Range(center.z - size.z / 2, center.z + size.z / 2);
        return new Vector3(x, 0, z); // y座標は0としておき、後で変更する
    }

}
