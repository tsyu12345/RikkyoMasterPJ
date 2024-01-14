using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

namespace Drone {

    //TODO:Agent以外のオブジェクトに関連する処理を、各オブジェクトごとに分割する

    public class DroneAgent : Agent {


        [Header("Operation Targets")]
        public GameObject DronePlatform; //ドローンの離着陸プラットフォーム 
        public GameObject Warehouse; //　物資倉庫
        public GameObject Shelter; // 避難所
        public GameObject Supplie; // 物資
        public GameObject Field; // フィールド

        [Header("Movement Parameters")]
        public float yLimit = 50.0f; //高度制限

        public float altitude;
        public float altitudeChangeSpeed = 0.5f; // 高度変更の速度
        public float moveSpeed;
        public GameObject Destination; // 現在AIが考えている目的地


    
        [Header("State of Drone")]
        public bool isGetSupplie = false; // 物資を持っているかどうか

        public bool isOnWarehouse = false; // 倉庫の範囲内にいるかどうか

        public bool isOnShelter = false; // 避難所の範囲内にいるかどうか


        //private props
        private Rigidbody Rbody;
        private NavMeshAgent NavAI;
        private GameObject[] DestinationList; //目的地のリスト



        void Start() {
            Rbody = GetComponent<Rigidbody>();
            NavAI = GetComponent<NavMeshAgent>();

            //目的地のリストを作成
            DestinationList = new GameObject[3];
            DestinationList[0] = null;
            DestinationList[1] = Warehouse;
            DestinationList[2] = Shelter;


            if(yLimit == 0) {
                throw new System.ArgumentNullException("yLimit", "Arguments 'yLimit' is required");
            }

        }

        public override void OnEpisodeBegin() {
            // ドローンの位置をDronePlatformの位置に初期化
            Vector3 pos = new Vector3(DronePlatform.transform.localPosition.x, 10f, DronePlatform.transform.localPosition.z);
            transform.localPosition = pos;
            NavAI.baseOffset = 10f;
            //ドローンの状態を初期化
            isGetSupplie = false;
            isOnWarehouse = false;
            isOnShelter = false;

            //物資を倉庫に戻す->座標をリセット
            Supplie.transform.parent = Warehouse.transform;
            Supplie.transform.localPosition = new Vector3(0,0.5f,0);
            Supplie.transform.localRotation = Quaternion.Euler(0, 0, 0);
            Supplie.GetComponent<Rigidbody>().useGravity = true;
            //scaleは1.0に戻す
            Supplie.transform.localScale = new Vector3(1,1,1);
            
            InitializeRandomPositions();


            Debug.Log("[Agent] Episode Initialize Compleat");
        }



        void Update() {
        }

        /**
        オブジェクトとの衝突イベントハンドラー
        */
        void OnTriggerEnter(Collider other) {
            if(other.gameObject.tag == "obstacle") {
                Debug.Log("[Agent] Hit Obstacle");
                AddReward(-1.0f);
                EndEpisode();
                return;
            }
            if(other.gameObject.tag == "warehouserange") {
                Debug.Log("[Agent] in range warehouse");
                isOnWarehouse = true;
                //AddReward(5.0f);
            }
            if(other.gameObject.tag == "shelterrange") {
                Debug.Log("[Agent] in range shelter");
                isOnShelter = true;
                //AddReward(5.0f);
            }

        }

        /**
        * オブジェクトとの接触が解除されたときのイベントハンドラー
        */
        void OnTriggerExit(Collider other) {
            if(other.gameObject.tag == "warehouserange") {
                Debug.Log("[Agent] out of range warehouse");
                isOnWarehouse = false;
            }
            if(other.gameObject.tag == "shelterrange") {
                Debug.Log("[Agent] out of range shelter");
                isOnShelter = false;
                EndEpisode();
                return;
            }
        }
        

        public override void CollectObservations(VectorSensor sensor) {
            // ドローンの速度,高度, 位置を観察
            sensor.AddObservation(moveSpeed);
            sensor.AddObservation(altitude);
            sensor.AddObservation(transform.localPosition);
            //現在の物資状態を観察
            sensor.AddObservation(isGetSupplie);
            //現在のドローンの位置状態を観察
            sensor.AddObservation(isOnShelter);
            sensor.AddObservation(isOnWarehouse);
            //各種オブジェクトの位置を観察
            sensor.AddObservation(Warehouse.transform.localPosition);
            sensor.AddObservation(Shelter.transform.localPosition);
            //obstacleの位置を観察
            var obstacles = GetsGameObjectsIncludeDeactive("obstacle");
            foreach(var obstacle in obstacles) {
                sensor.AddObservation(obstacle.transform.localPosition);
            }
        }




        public override void OnActionReceived(ActionBuffers actions) {
            ContinuousControl(actions);
            DiscreateControl(actions);

            
            //Fieldから離れたらリセット
            if(transform.localPosition.y > yLimit || transform.localPosition.y < 0) {
                Debug.Log("[Agent] Out of range");
                SetReward(-1.0f);
                EndEpisode();
                return;
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut) {
            
        }

        /// <summary>
        /// ドローンの操作系
        /// </summary>
        /// <param name="actions"></param>
        private void ContinuousControl(ActionBuffers actions) {
            // 入力値を取得
            var altitudeInput = actions.ContinuousActions[0];
            var moveSpeedInput = actions.ContinuousActions[1];
            
            //高度調整
            var targetAltitude = altitudeInput * 100f;
            float newBaseOffset = Mathf.Lerp(NavAI.baseOffset, targetAltitude, altitudeChangeSpeed * Time.deltaTime); //動きを滑らかにする線形補間
            NavAI.baseOffset = newBaseOffset;
            altitude = transform.localPosition.y;
            //移動速度調整
            NavAI.speed = moveSpeedInput * 10f;
            moveSpeed = NavAI.speed;
        }


        /// <summary>
        /// ドローンの「物資を持つ」「物資を離す」などの離散系行動制御関数
        /// ＜行動一覧＞
        /// ①物資の取得/切り離し
        /// ②目的地の設定 
        /// <param name="actions">エージェントの行動選択</param>//  
        private void DiscreateControl(ActionBuffers actions) {
            // 入力値を取得
            int ModeAction = actions.DiscreteActions[0]; //0: 待機, 1: 物資を取る, 2: 物資を離す
            int DestinationAction = actions.DiscreteActions[1]; //0: 空中待機, 1: 倉庫, 2: 避難所

            var getMode = ModeAction == 1 ? true : false;
            var releaseMode = ModeAction == 2 ? true : false;
            var choiceDestination = DestinationList[DestinationAction];

            NavAI.isStopped = false;
                
            //目的地を設定
            Destination = choiceDestination;

            if(choiceDestination == Shelter) {
                NavAI.SetDestination(choiceDestination.transform.position);
                transform.LookAt(choiceDestination.transform.position);
                if(isGetSupplie) {
                    AddReward(0.5f);
                }
            } else if(choiceDestination == Warehouse) {
                NavAI.SetDestination(choiceDestination.transform.position);
                transform.LookAt(choiceDestination.transform.position);
                if(!isGetSupplie) {
                    AddReward(0.25f);
                }
            } else if(choiceDestination == null) {
                NavAI.isStopped = true;
                //現在の位置を維持
                NavAI.SetDestination(transform.position);
            }



            // 物資を取るを選択した場合
            if (getMode) { 
                if(isOnWarehouse && !isGetSupplie) {
                    GetSupplie();
                    AddReward(1.0f);
                } else if(isGetSupplie) {
                    Debug.Log("[Agent] already get Supplie");
                } else if(!isOnWarehouse) {
                    Debug.Log("[Agent] Get Supplie on Field. not on Warehouse");
                }
            }

            // 物資を離すを選択した場合
            if(releaseMode) {
                ReleaseSupplie();
                Debug.Log("[Agent] Action:Release Supplie");
                if (isOnShelter && isGetSupplie) {
                    AddReward(1.0f);
                    Debug.Log("[Agent] !!GOAL!! Release Supplie on Shelter");
                    EndEpisode();
                    return;
                } else if(!isGetSupplie) { //物資を持っていない状態で物資を離した場合
                    Debug.Log("[Agent] not get Supplie... but Agent did release");
                } else if(!isOnShelter && isGetSupplie) { //避難所の上空以外で物資を離した場合
                    Debug.Log("[Agent] Release Supplie on Field. But not on Shelter");
                    SetReward(-1.0f);
                    EndEpisode();
                    return;
                }
            }
        }


        private void GetSupplie() {
            Debug.Log("[Agent] Get Supplie");
            //物資の重力を無効化 
            //TODO:将来的には重力有効の状態で、ぶら下がり状態を実装する
            Supplie.GetComponent<Rigidbody>().useGravity = false;
            // 物資を取る : オブジェクトの親をドローンに設定
            Supplie.transform.parent = transform;
            // 物資の位置をドローンの下部に設定
            Supplie.transform.localPosition = new Vector3(0, -4f, 0);
            Supplie.transform.localRotation = Quaternion.Euler(0, 0, 0);
            //位置を固定
            Supplie.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            
            isGetSupplie = true;
        }


        private void ReleaseSupplie() {
            //物資を落とす
            Supplie.GetComponent<Rigidbody>().useGravity = true;
            Supplie.transform.parent = Field.transform;
            //位置を固定解除
            Supplie.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
            isGetSupplie = false;
        }



        private float MyGetAxis(string axisName) {
            float axis = 0;
            switch (axisName) {
                case "Horizontal":
                    if(Input.GetKey(KeyCode.D)) {
                        axis = 1f;
                    } else if(Input.GetKey(KeyCode.A)) {
                        axis = -1f;
                    }
                    break;
                
                case "Vertical":
                    if(Input.GetKey(KeyCode.W)) {
                        axis = 1f;
                    } else if(Input.GetKey(KeyCode.S)) {
                        axis = -1f;
                    }
                    break;
            }
            //線形補間の利用
            axis = Mathf.Lerp(0, axis, 0.5f);
            return axis;
        }


        private List<GameObject> GetsGameObjectsIncludeDeactive(string tag) {
            List<GameObject> objectsWithTag = new List<GameObject>();
            foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>()) {
                if (obj.tag == tag) {
                    objectsWithTag.Add(obj);
                }
            }
            return objectsWithTag;
        }


        private void InitializeRandomPositions(float someMinimumDistance = 10f) {
            Vector3 fieldSize = Field.GetComponent<Collider>().bounds.size;
            Vector3 fieldCenter = Field.transform.position;

            Vector3 newWarehousePos, newShelterPos;
            int maxAttempts = 100; // 最大試行回数を設定
            int attempts = 0;

            do {
                newWarehousePos = GenerateRandomPosition(fieldCenter, fieldSize);
                newShelterPos = GenerateRandomPosition(fieldCenter, fieldSize);
                attempts++;
            } while (Vector3.Distance(newWarehousePos, newShelterPos) < someMinimumDistance && attempts < maxAttempts);

            if (attempts >= maxAttempts) {
                Debug.LogWarning("Failed to place Warehouse and Shelter sufficiently apart");
                return; // 適切な位置を見つけられなかった場合は処理を中断
            }

            newWarehousePos.y = Warehouse.transform.localPosition.y;
            newShelterPos.y = Shelter.transform.localPosition.y;
            Warehouse.transform.localPosition = newWarehousePos;
            Shelter.transform.localPosition = newShelterPos;
        }

        private Vector3 GenerateRandomPosition(Vector3 center, Vector3 size) {
            float x = Random.Range(center.x - size.x / 2, center.x + size.x / 2);
            float z = Random.Range(center.z - size.z / 2, center.z + size.z / 2);
            return new Vector3(x, 0, z); // y座標は0としておき、後で変更する
        }
    }
}

