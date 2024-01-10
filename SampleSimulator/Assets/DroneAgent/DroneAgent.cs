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
        public GameObject[] DestinationList; // 向かうべき目的地のリスト。このリストのIndexから選択する

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

        //環境の範囲値(x, y, z)を格納した変数     
        private float[] fieldXRange = new float[2];
        private float[] fieldYRange = new float[2];
        private float[] fieldZRange = new float[2];



        public override void Initialize() {
            Rbody = GetComponent<Rigidbody>();
            NavAI = GetComponent<NavMeshAgent>();


            if(yLimit == 0) {
                throw new System.ArgumentNullException("yLimit", "Arguments 'yLimit' is required");
            }

            //Fieldの範囲値を取得
            var FieldTransform = Field.transform;
            var FieldLocalScale = FieldTransform.localScale;
            var FieldCenterLocalPosition = FieldTransform.localPosition;
            fieldXRange[0] = FieldCenterLocalPosition.x - FieldLocalScale.x / 2;
            fieldXRange[1] = FieldCenterLocalPosition.x + FieldLocalScale.x / 2;
            fieldYRange[0] = FieldCenterLocalPosition.y - FieldLocalScale.y / 2;
            fieldYRange[1] = FieldCenterLocalPosition.y + FieldLocalScale.y / 2;
            fieldZRange[0] = FieldCenterLocalPosition.z - FieldLocalScale.z / 2;
            fieldZRange[1] = FieldCenterLocalPosition.z + FieldLocalScale.z / 2;

        }

        public override void OnEpisodeBegin() {
            // ドローンの位置をDronePlatformの位置に初期化
            Vector3 pos = new Vector3(DronePlatform.transform.localPosition.x, 10f, DronePlatform.transform.localPosition.z);
            transform.localPosition = pos;
            //ドローンの状態を初期化
            isGetSupplie = false;
            isOnWarehouse = false;
            isOnShelter = false;

            //物資を倉庫に戻す->座標をリセット
            Supplie.transform.parent = Warehouse.transform;
            Supplie.transform.localPosition = new Vector3(0,0.5f,0);
            Supplie.transform.localRotation = Quaternion.Euler(0, 0, 0);
            Supplie.GetComponent<Rigidbody>().useGravity = true;
            
            Rbody.AddForce(transform.TransformDirection(new Vector3(0, 10.0f, 10.0f)));
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
                AddReward(-5.0f);
                EndEpisode();
            }
            if(other.gameObject.tag == "warehouserange") {
                Debug.Log("[Agent] in range warehouse");
                isOnWarehouse = true;
                AddReward(5.0f);
            }
            if(other.gameObject.tag == "shelterrange") {
                Debug.Log("[Agent] in range shelter");
                isOnShelter = true;
                AddReward(5.0f);
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
            }
        }
        

        public override void CollectObservations(VectorSensor sensor) {
            // ドローンの速度,高度を観察
            sensor.AddObservation(moveSpeed);
            sensor.AddObservation(altitude);
            //現在の物資状態を観察
            sensor.AddObservation(isGetSupplie);
            //現在のドローンの位置状態を観察
            sensor.AddObservation(isOnShelter);
            sensor.AddObservation(isOnWarehouse);

        }




        public override void OnActionReceived(ActionBuffers actions) {
            ContinuousControl(actions);
            DiscreateControl(actions);
            //Fieldから離れたらリセット
            if(transform.localPosition.y > yLimit || transform.localPosition.y < 0) {
                Debug.Log("[Agent] Out of range");
                AddReward(-10.0f);
                EndEpisode();
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
            int ModeAction = actions.DiscreteActions[0];
            int DestinationAction = actions.DiscreteActions[1];

            var getMode = ModeAction == 1 ? true : false;
            var releaseMode = ModeAction == 2 ? true : false;
            var choiceDestination = DestinationList[DestinationAction];
                
            //目的地を設定
            NavAI.SetDestination(choiceDestination.transform.position);
            transform.LookAt(choiceDestination.transform.position);
            Destination = choiceDestination;

            if(!isGetSupplie && DestinationAction == 1) { //物資を持っていない状態で避難所を選択した場合
                AddReward(-10.0f);
                EndEpisode();
            }
            // 物資を取るを選択した場合
            if (getMode) { 
                if(isOnWarehouse && !isGetSupplie) {
                    GetSupplie();
                    AddReward(10.0f);
                }
            }

            // 物資を離すを選択した場合
            if(releaseMode) {
                ReleaseSupplie();
                Debug.Log("[Agent] Action:Release Supplie");
                if (isOnShelter && isGetSupplie) {
                    AddReward(10.0f);
                    Debug.Log("[Agent] Release Supplie on Shelter");
                    isGetSupplie = false;
                    EndEpisode();
                } else if(!isGetSupplie) { //物資を持っていない状態で物資を離した場合
                    AddReward(-10.0f);
                    Debug.Log("[Agent] not get Supplie... but Agent did release");
                    isGetSupplie = false;
                    EndEpisode();
                } else if(!isOnShelter && isGetSupplie) { //避難所の上空以外で物資を離した場合
                    Debug.Log("[Agent] Release Supplie on Field. But not on Shelter");
                    AddReward(-10.0f);
                    isGetSupplie = false;
                    EndEpisode();
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
    }

}

