using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;


public class EnvManager : MonoBehaviour {

    [Header("GameObjects")]
    public GameObject FieldPlane;
    public GameObject Field;
    public GameObject SubFieldPlane;

    public List<GameObject> buildings;
    public GameObject DroneStation;
    [Header("Agent Groups")]
    public List<GameObject> Agents;
    public List<string> Teams; //TODO:indexで参照するのをやめる
    private SimpleMultiAgentGroup SurpplierGroup;
    private SimpleMultiAgentGroup SpyAgentGroup;
    private string LogPrefix = "EnvManager: ";
    private List<SurpplieAgent> Surppliers;
    private List<SpyAgent> Spies;

    void Start() {
        SurpplierGroup = new SimpleMultiAgentGroup();
        SpyAgentGroup = new SimpleMultiAgentGroup();
        Surppliers = new List<SurpplieAgent>();
        Spies = new List<SpyAgent>();
        foreach(GameObject drone in Agents) {
            if (drone.tag == Teams[0]) {
                SurpplierGroup.RegisterAgent(drone.GetComponent<Agent>());
                Surppliers.Add(drone.GetComponent<SurpplieAgent>());
            } else if (drone.tag == Teams[1]) {
                SpyAgentGroup.RegisterAgent(drone.GetComponent<Agent>());
                Spies.Add(drone.GetComponent<SpyAgent>());
            }
        }
        Debug.LogWarning(LogPrefix + "SpyGroupCount:" + Spies.Count);
        Debug.LogWarning(LogPrefix + "SurpplierGroupCount:" + Surppliers.Count);
        AddEventListeners();
    }
    
    public void InitializeRandomPositions(float someMinimumDistance = 10f) {
        Vector3 fieldPlaneSize = FieldPlane.GetComponent<Collider>().bounds.size;
        Vector3 fieldPlaneCenter = FieldPlane.transform.position;
        int maxAttempts = 100; // 最大試行回数を設定
        int attempts;

        // buildingsの位置をランダムに設定, SubFieldPlaneの範囲内でランダムに配置
        foreach (GameObject building in buildings) {
            Vector3 newBuildingPos;
            attempts = 0; // 試行回数のリセット
            do {
                newBuildingPos = GenerateRandomPosition(SubFieldPlane.transform.position, SubFieldPlane.GetComponent<Collider>().bounds.size);
                attempts++;
            } while (Vector3.Distance(newBuildingPos, DroneStation.transform.position) < someMinimumDistance && attempts < maxAttempts);

            if (attempts >= maxAttempts) {
                Debug.LogWarning("Failed to place building sufficiently apart from DroneStation");
                return; // 適切な位置を見つけられなかった場合は処理を中断 無限ループを防ぐため
            }
            building.transform.position = newBuildingPos; // ローカル座標を使用して位置を設定
        }
    }

    private Vector3 GenerateRandomPosition(Vector3 center, Vector3 size) {
        float x = Random.Range(center.x - size.x / 2, center.x + size.x / 2);
        float z = Random.Range(center.z - size.z / 2, center.z + size.z / 2);

        // 生成されたXとZ座標を使用して新しい位置Vector3を返す
        // y座標はSubFieldPlaneのy座標に合わせるか、必要に応じて調整
        return new Vector3(x, center.y, z);
    }


    private void AddEventListeners() {
        foreach(SurpplieAgent surpplier in Surppliers) {
            surpplier.onLandingSurpplieShelter += OnLandingSurpplie;
        }
        foreach(SpyAgent spy in Spies) {
            spy.onFindShelter += OnDetectShelter;
        }
    }

    private void OnLandingSurpplie() {
        Debug.LogWarning(LogPrefix + "Surpplier Landing Surpplie");
        SurpplierGroup.SetGroupReward(1f);
        SurpplierGroup.EndGroupEpisode();
        SpyAgentGroup.SetGroupReward(0.5f);
        SpyAgentGroup.EndGroupEpisode();
    }

    private void OnDetectShelter(Vector3 pos) {
        Debug.LogWarning(LogPrefix + "Spy Agents Find Shelter");
        SpyAgentGroup.SetGroupReward(1f);
    }

}
