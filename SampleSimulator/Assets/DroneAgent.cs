using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

namespace Drone {


    public class DroneAgent : Agent {

        [Header("Operation Targets")]
        public GameObject DronePlatform; //ドローンの離着陸プラットフォーム 
        public GameObject Warehouse; //　物資倉庫
        public GameObject Supplie; // 物資
        public GameObject Field; // フィールド

        [Header("Movement Parameters")]
        public float moveSpeed = 5f; // 移動速度
        public float rotSpeed = 100f; // 回転速度
        public float verticalForce = 20f; // 上昇・下降速度
        public float forwardTiltAmount = 0; // 前傾角
        public float sidewaysTiltAmount = 0; // 横傾角
        public float tiltVel = 2f; // 傾きの変化速度
        public float tiltAng = 45f; // 傾きの最大角度
        public float yLimit = 50.0f; //高度制限

        [Header("State of Drone")]
        public bool isGetSupplie = false; // 物資を持っているかどうか

        private Rigidbody Rbody;
        public override void Initialize() {
            Rbody = GetComponent<Rigidbody>();
            if(yLimit == 0) {
                throw new System.ArgumentNullException("yLimit", "Arguments 'yLimit' is required");
            }
        }

        public override void OnEpisodeBegin() {
            // ドローンの位置をDronePlatformの位置に初期化
            Vector3 pos = new Vector3(DronePlatform.transform.localPosition.x, DronePlatform.transform.localPosition.y + 1f, DronePlatform.transform.localPosition.z);
        }

        public override void CollectObservations(VectorSensor sensor) {}

        public override void OnActionReceived(ActionBuffers actions) {
            Control(actions);
            DiscreateControl(actions);

            //Fieldから離れたらリセット
            if(isOutRange(yLimit)) {
                Debug.Log("Out of range");
                EndEpisode();
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut) {
            // ドローンの操縦系:Continuous な行動
            float horInput = Input.GetAxis("Horizontal");
            float verInput = Input.GetAxis("Vertical");
            float upInput = Input.GetKey(KeyCode.Q) ? 1 : 0;
            float downInput = Input.GetKey(KeyCode.E) ? 1 : 0;
            float rotInput = Input.GetAxis("Mouse X");
            //　ドローンの操作系:Discrete な行動
            //物資をとる
            int getMode = Input.GetKey(KeyCode.G) ? 1 : 0;
            // 物資を離す
            int releaseMode = Input.GetKey(KeyCode.R) ? 1 : 0;

            // 入力をエージェントのアクションに割り当てます
            var continuousAct = actionsOut.ContinuousActions;
            continuousAct[0] = horInput;
            continuousAct[1] = verInput;
            continuousAct[2] = upInput;
            continuousAct[3] = downInput;
            continuousAct[4] = rotInput;

            var discreteAct = actionsOut.DiscreteActions;
            discreteAct[0] = 0;
            if (getMode == 1) discreteAct[0] = 1;
            if (releaseMode == 1) discreteAct[0] = 2;
        }

        /// <summary>
        /// ドローンの操作系
        /// </summary>
        /// <param name="actions"></param>
        private void Control(ActionBuffers actions) {
            // 入力値を取得
            float horInput = actions.ContinuousActions[0];
            float verInput = actions.ContinuousActions[1];
            float upInput = actions.ContinuousActions[2];
            float downInput = actions.ContinuousActions[3];
            float rotInput = actions.ContinuousActions[4];

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

            // ドローンの回転処理（Y軸周り）
            transform.Rotate(0, rotInput * rotSpeed * Time.fixedDeltaTime, 0);

            // 入力に基づいて傾きを計算
            sidewaysTiltAmount = Mathf.Lerp(sidewaysTiltAmount, -horInput * tiltAng, tiltVel * Time.fixedDeltaTime);
            forwardTiltAmount = Mathf.Lerp(forwardTiltAmount, verInput * tiltAng, tiltVel * Time.fixedDeltaTime);

            // 傾きをドローンに適用
            Quaternion targetRot = Quaternion.Euler(forwardTiltAmount, 0, sidewaysTiltAmount);
            transform.localRotation = targetRot;
        }

        /// <summary>
        /// ドローンの「物資を持つ」「物資を離す」などの離散系行動制御関数
        /// </summary>
        /// <param name="actions">エージェントの行動選択</param>//  
        private void DiscreateControl(ActionBuffers actions) {
            // 入力値を取得
            int getMode = actions.DiscreteActions[0];
            int releaseMode = actions.DiscreteActions[0];

            //ドローンの位置とWarehouseの位置を取得し、ドローンがWarehouseの上にいるかどうかを判定
            var allowance = 3.0f;
            var isOnWarehouse = transform.localPosition.x < Warehouse.transform.localPosition.x + allowance && transform.localPosition.x > Warehouse.transform.localPosition.x - allowance
                && transform.localPosition.z < Warehouse.transform.localPosition.z + allowance && transform.localPosition.z > Warehouse.transform.localPosition.z - allowance;

            // 物資を取る
            if (getMode == 1 && isOnWarehouse && !isGetSupplie) {
                Debug.Log("Get Supplie");
                // 物資を取る : オブジェクトの親をドローンに設定
                Supplie.transform.parent = transform;
                // 物資の位置をドローンの下部に設定
                Supplie.transform.localPosition = new Vector3(0, -0.7f, 0);
                isGetSupplie = true;
            }

            // 物資を離す
            if (releaseMode == 1) {
                // 物資を離す
                Supplie.transform.parent = Field.transform;
                isGetSupplie = false;
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

    }

}

