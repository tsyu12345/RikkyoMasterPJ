using UnityEngine;
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
        public float moveSpeed = 5f; // 移動速度
        public float rotSpeed = 100f; // 回転速度
        public float verticalForce = 20f; // 上昇・下降速度
        public float forwardTiltAmount = 0; // 前傾角
        public float sidewaysTiltAmount = 0; // 横傾角
        public float rotAmount = 0; // 回転角
        public float tiltVel = 2f; // 傾きの変化速度
        public float tiltAng = 45f; // 傾きの最大角度
        public float yLimit = 50.0f; //高度制限

        [Header("State of Drone")]
        public bool isGetSupplie = false; // 物資を持っているかどうか

        public bool isOnWarehouse = false; // 倉庫の範囲内にいるかどうか

        public bool isOnShelter = false; // 避難所の範囲内にいるかどうか

        //private props
        private Rigidbody Rbody;
        private float rot;

        //環境の範囲値(x, y, z)を格納した変数     
        private float[] fieldXRange = new float[2];
        private float[] fieldYRange = new float[2];
        private float[] fieldZRange = new float[2];



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
            }
            if(other.gameObject.tag == "warehouserange") {
                Debug.Log("[Agent] in range warehouse");
                isOnWarehouse = true;
                if(!isGetSupplie) {
                    AddReward(0.5f);
                }
            }
            if(other.gameObject.tag == "shelterrange") {
                Debug.Log("[Agent] in range shelter");
                isOnShelter = true;
                if(isGetSupplie) {
                    AddReward(0.5f);
                }
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
            }
        }
        public override void Initialize() {
            Rbody = GetComponent<Rigidbody>();
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
            Vector3 pos = new Vector3(DronePlatform.transform.localPosition.x, DronePlatform.transform.localPosition.y + 1f, DronePlatform.transform.localPosition.z);
            transform.localPosition = pos;
            //ドローンの状態を初期化
            isGetSupplie = false;
            isOnWarehouse = false;
            isOnShelter = false;
        
            //倉庫と避難所が重なっている限りwhile
            while(Vector3.Distance(Warehouse.transform.localPosition, Shelter.transform.localPosition) < 10.0f) {
                //倉庫と避難所の位置をlocal内でランダムに設定
                Warehouse.transform.localPosition = new Vector3(Random.Range(fieldXRange[0], fieldXRange[1]), 0, Random.Range(fieldZRange[0], fieldZRange[1]));
                Shelter.transform.localPosition = new Vector3(Random.Range(fieldXRange[0], fieldXRange[1]), 0, Random.Range(fieldZRange[0], fieldZRange[1]));
            }

            //物資を倉庫に戻す->座標をリセット
            Supplie.transform.parent = Warehouse.transform;
            Supplie.transform.localPosition = new Vector3(0,0.5f,0);
            Supplie.transform.localRotation = Quaternion.Euler(0, 0, 0);
            Supplie.GetComponent<Rigidbody>().useGravity = true;

            Debug.Log("[Agent] Episode Initialize Compleat");
        }

        public override void CollectObservations(VectorSensor sensor) {
            // ドローンの速度を観察
            sensor.AddObservation(Rbody.velocity);
            // ドローンの回転を観察
            sensor.AddObservation(transform.rotation.eulerAngles);
            //自身の現在位置（x,y,z）を観察
            sensor.AddObservation(transform.localPosition);
        }




        public override void OnActionReceived(ActionBuffers actions) {
            ContinuousControl(actions);
            DiscreateControl(actions);

            //Fieldから離れたらリセット
            if(transform.localPosition.y > yLimit || transform.localPosition.y < 0) {
                Debug.Log("[Agent] Out of range");
                EndEpisode();
                AddReward(-1.0f);
            }
            
            /*
            if(isOutRange(yLimit)) {
                Debug.Log("Out of range");
                EndEpisode();
            }*/
        }

        public override void Heuristic(in ActionBuffers actionsOut) {
            // ドローンの操縦系
            float horInput = MyGetAxis("Horizontal");
            float verInput = MyGetAxis("Vertical");
            float upInput = Input.GetKey(KeyCode.Q) ? 1f : 0f;
            float downInput = Input.GetKey(KeyCode.E) ? 1f : 0f;
            int rotInput = 0; //回転のみ離散値
            int rightRotInput = Input.GetKey(KeyCode.RightArrow) ? 1 : 0;
            int leftRotInput = Input.GetKey(KeyCode.LeftArrow) ? 1 : 0;
            //Debug.Log($"[Agent] Rotation Input:{rotInput}");
            //Rotを割り当てる
            rotInput = rightRotInput - leftRotInput;

            //　ドローンの操作系:Discrete な行動
            //物資をとる
            bool getMode = Input.GetKey(KeyCode.G) ? true : false;
            // 物資を離す
            bool releaseMode = Input.GetKey(KeyCode.R) ? true : false;

            // 入力をエージェントのアクションに割り当てます
            var continuousAct = actionsOut.ContinuousActions;
            continuousAct[0] = horInput;
            continuousAct[1] = verInput;
            continuousAct[2] = upInput;
            continuousAct[3] = downInput;

            var discreteAct = actionsOut.DiscreteActions;
            discreteAct[0] = 0;
            if (getMode) {
                discreteAct[0] = 1;
            }
            if (releaseMode) {
                discreteAct[0] = 2;
            }
            //回転の方向
            discreteAct[1] = rotInput;
        }

        /// <summary>
        /// ドローンの操作系
        /// </summary>
        /// <param name="actions"></param>
        private void ContinuousControl(ActionBuffers actions) {
            // 入力値を取得
            float horInput = actions.ContinuousActions[0];
            float verInput = actions.ContinuousActions[1];
            float upInput = actions.ContinuousActions[2];
            float downInput = actions.ContinuousActions[3];
            //回転だけ離散値
            int rotInput = actions.DiscreteActions[1];
            // 移動方向を計算
            Vector3 moveDirection = new Vector3(horInput, 0, verInput) * moveSpeed;
            // Rigidbodyに力を加えてドローンを移動させる
            Rbody.AddForce(transform.TransformDirection(moveDirection));

            // 上昇キーが押された場合
            if (upInput > 0) {
                // 上方向に力を加える
                Rbody.AddForce(Vector3.up * verticalForce * upInput);
            }

            // 下降キーが押された場合
            if (downInput > 0) {
                // 下方向に力を加える
                Rbody.AddForce(Vector3.down * verticalForce * downInput);
            }


            // 入力に基づいて傾き・回転を計算
            sidewaysTiltAmount = Mathf.Lerp(sidewaysTiltAmount, -horInput * tiltAng, tiltVel * Time.fixedDeltaTime);
            forwardTiltAmount = Mathf.Lerp(forwardTiltAmount, verInput * tiltAng, tiltVel * Time.fixedDeltaTime);
            rotAmount += rotInput * rotSpeed * Time.fixedDeltaTime;

            // 傾き・回転をドローンに適用
            Quaternion targetRot = Quaternion.Euler(forwardTiltAmount, rotAmount, sidewaysTiltAmount);
            transform.rotation = targetRot;
        }

        /// <summary>
        /// ドローンの「物資を持つ」「物資を離す」などの離散系行動制御関数
        /// </summary>
        /// <param name="actions">エージェントの行動選択</param>//  
        private void DiscreateControl(ActionBuffers actions) {
            // 入力値を取得
            int ModeAction = actions.DiscreteActions[0];
            var getMode = ModeAction == 1 ? true : false;
            var releaseMode = ModeAction == 2 ? true : false;
                
            //ドローンの位置とWarehouseの位置を取得し、ドローンがWarehouseの上にいるかどうかを判定

            // 物資を取るを選択した場合
            if (getMode) { 
                if(isOnWarehouse && !isGetSupplie) {
                    Debug.Log("[Agent] Get Supplie");
                    //物資の重力を無効化 TODO:将来的には重力有効の状態で、ぶら下がり状態を実装する
                    Supplie.GetComponent<Rigidbody>().useGravity = false;
                    // 物資を取る : オブジェクトの親をドローンに設定
                    Supplie.transform.parent = transform;
                    // 物資の位置をドローンの下部に設定
                    Supplie.transform.localPosition = new Vector3(0, -4f, 0);
                    Supplie.transform.localRotation = Quaternion.Euler(0, 0, 0);
                    //位置を固定
                    Supplie.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                    
                    isGetSupplie = true;
                    AddReward(1.0f);
                }
            }

            // 避難所の上空で物資を離す
            if(releaseMode) {
                //物資を落とす
                Supplie.GetComponent<Rigidbody>().useGravity = true;
                Supplie.transform.parent = Field.transform;
                //位置を固定解除
                Supplie.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                
                Debug.Log("[Agent] Action:Release Supplie");
                if (isOnShelter && isGetSupplie) {
                    AddReward(1.0f);
                    Debug.Log("[Agent] Release Supplie on Shelter");
                    isGetSupplie = false;
                    EndEpisode();
                    return;
                } else if(!isGetSupplie) { //物資を持っていない状態で物資を離した場合
                    AddReward(-0.5f);
                    Debug.Log("[Agent] not get Supplie... but Agent did release");
                    isGetSupplie = false;
                } else if(!isOnShelter && isGetSupplie) { //避難所の上空以外で物資を離した場合
                    Debug.Log("[Agent] Release Supplie on Field. But not on Shelter");
                    AddReward(-1.0f);
                    isGetSupplie = false;
                    EndEpisode();
                    return;
                }
            }
        }

        /// <summary>
        /// ドローンがフィールド（Field）外に出たかどうかを判定するメソッド
        /// </summary>
        /// <param name="yMax">フィールドの高さ(限界高度)</param>
        /// <returns>フィールド外に出た場合はtrue、そうでない場合はfalseを返す</returns>
        private bool isOutRange(float yMax) {
            
            // FieldオブジェクトのTransformコンポーネントを取得
            var FieldTransform = Field.transform;

            // Fieldオブジェクトのローカルスケールを取得
            Vector3 FieldLocalScale = FieldTransform.localScale;

            // Fieldオブジェクトの中心のローカル座標を取得
            Vector3 FieldCenterLocalPosition = FieldTransform.localPosition;

            // Fieldオブジェクトの4辺のローカル座標を計算
            Vector3 leftEdgeLocalPosition = FieldCenterLocalPosition + new Vector3(-FieldLocalScale.x / 2, 0, 0);
            Vector3 rightEdgeLocalPosition = FieldCenterLocalPosition + new Vector3(FieldLocalScale.x / 2, 0, 0);
            Vector3 topEdgeLocalPosition = FieldCenterLocalPosition + new Vector3(0, 0, FieldLocalScale.z / 2);
            Vector3 bottomEdgeLocalPosition = FieldCenterLocalPosition + new Vector3(0, 0, -FieldLocalScale.z / 2);

            //ドローンの位置がフィールドの範囲外かどうかを判定
            var isInXRange = transform.localPosition.x < rightEdgeLocalPosition.x && transform.localPosition.x > leftEdgeLocalPosition.x;
            var isInYRange = transform.localPosition.y < yMax && transform.localPosition.y > 0;
            var isInZRange = transform.localPosition.z < topEdgeLocalPosition.z && transform.localPosition.z > bottomEdgeLocalPosition.z;

            if(!isInYRange || !isInXRange || !isInZRange) {
                return true;
            } else {
                return false;
            }
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

    }

}

