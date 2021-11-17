using System;
using UnityEngine;

namespace Fragsurf.Movement
{
    /// <summary>
    /// Easily add a surfable character to the scene
    /// </summary>
    [AddComponentMenu("Fragsurf/Surf Character")]
    public class SurfCharacter : MonoBehaviour, ISurfControllable
    {

        public enum ColliderType
        {
            Capsule,
            Box
        }

        ///// Fields /////

        [Header("Physics Settings")]
        public int TickRate = 128;
        public float ColliderSizeX = 1f;
        public float ColliderSizeY = 2f;
        public float ColliderSizeZ = 1f;
        //public Vector3 ColliderSize = new Vector3(1, 2, 1);
        public ColliderType CollisionType;

        [Header("View Settings")]
        public Transform viewTransform;
        public Vector3 ViewOffset = new Vector3(0, 0.61f, 0);

        [Header("Input Settings")]
        public float XSens = 50;
        public float YSens = 50;
        public KeyCode JumpButton = KeyCode.LeftShift;
        public KeyCode CrouchButton = KeyCode.LeftControl;
        public KeyCode MoveLeft = KeyCode.A;
        public KeyCode MoveRight = KeyCode.D;
        public KeyCode MoveForward = KeyCode.W;
        public KeyCode MoveBack = KeyCode.S;
        public KeyCode InteractButton = KeyCode.E;

        [Header("Movement Config")]
        [SerializeField]
        public MovementConfig moveConfig;

        private GameObject _groundObject;
        private Vector3 _baseVelocity;
        private Collider _collider;
        private Vector3 _angles;
        private Vector3 _startPosition;

        private MoveData _moveData = new MoveData();
        private SurfController _controller = new SurfController();
        //shit variables for shit step up ability
        private float capcHeight;
        private float capcRadius;

        ///// Properties /////

        public bool InteractPressed { get; private set; }

        public MoveType MoveType
        {
            get { return MoveType.Walk; }
        }

        public MovementConfig MoveConfig
        {
            get { return moveConfig; }
        }

        public MoveData MoveData
        {
            get { return _moveData; }
        }

        public Collider Collider
        {
            get { return _collider; }
        }

        public GameObject GroundObject
        {
            get { return _groundObject; }
            set { _groundObject = value; }
        }

        public Vector3 BaseVelocity
        {
            get { return _baseVelocity; }
        }

        public Vector3 Forward
        {
            get { return viewTransform.forward; }
        }

        public Vector3 Right
        {
            get { return viewTransform.right; }
        }

        public Vector3 Up
        {
            get { return viewTransform.up; }
        }

        ///// Methods /////

        private void Awake()
        {
            Application.targetFrameRate = 144;
            QualitySettings.vSyncCount = 1;

            Time.fixedDeltaTime = 1f / TickRate;
        }

        private void Start()
        {
            if (viewTransform == null)
                viewTransform = Camera.main.transform;
            viewTransform.SetParent(transform, false);
            viewTransform.localPosition = ViewOffset;
            viewTransform.localRotation = transform.rotation;

            _collider = gameObject.GetComponent<Collider>();

            if (_collider != null)
                GameObject.Destroy(_collider);

            // rigidbody is required to collide with triggers
            var rbody = gameObject.GetComponent<Rigidbody>();
            if (rbody == null)
                rbody = gameObject.AddComponent<Rigidbody>();
            rbody.isKinematic = true;

            /*switch(CollisionType)
            {
                case ColliderType.Box:
                    _collider = gameObject.AddComponent<BoxCollider>();
                    var boxc = (BoxCollider)_collider;
                    boxc.size = ColliderSize;
                    break;

                case ColliderType.Capsule:
                    _collider = gameObject.AddComponent<CapsuleCollider>();
                    var capc = (CapsuleCollider)_collider;
                    capc.height = ColliderSize.y;
                    capc.radius = ColliderSize.x / 2f;
                    break;
            }*/
            _collider = gameObject.AddComponent<CapsuleCollider>();
            var capc = (CapsuleCollider)_collider;
            capc.height = ColliderSizeY;
            //shit variables for shit step up ability
            capcHeight = capc.height;
            capc.radius = ColliderSizeX / 2f;
            capcRadius = capc.radius;

            //stepRayUpper = new Vector3(transform.position.x, capcHeight/5, transform.position.z);
            //stepRayLower = new Vector3(transform.position.x, 0, transform.position.z);

            _collider.isTrigger = true;
            _moveData.Origin = transform.position;
            _startPosition = transform.position;
        }

        private void Update()
        {
            if (Time.timeScale != 0f)
            {
                UpdateTestBinds();
                UpdateRotation();
                UpdateMoveData();
                _controller.ProcessMovement(this, moveConfig, Time.fixedDeltaTime);
            }
        }
        public float GetTheVelocity()
        {
            return Mathf.Round(_moveData.Velocity.magnitude);
        }
        public bool accessController()
        {
            return _controller.bruh;
        }


        private void UpdateTestBinds()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                MoveData.Velocity = Vector3.zero;
                MoveData.Origin = _startPosition;
            }
        }



        private void FixedUpdate()
        {
            transform.position = MoveData.Origin;
        }

        private void UpdateMoveData()
        {
            var moveLeft = Input.GetKey(MoveLeft);
            var moveRight = Input.GetKey(MoveRight);
            var moveFwd = Input.GetKey(MoveForward);
            var moveBack = Input.GetKey(MoveBack);
            var jump = Input.GetKey(JumpButton);
            var crouch = Input.GetKey(CrouchButton);
            InteractPressed = Input.GetKeyDown(InteractButton);

            //Shit Variables for the Shit Crouch mechanic
            var capc = (CapsuleCollider)_collider;
            Vector3 up = transform.TransformDirection(Vector3.up);

            Vector3 stepRayLower = new Vector3(transform.position.x, (transform.position.y) - (capcHeight / 2), transform.position.z); ;
            Vector3 stepRayUpper = new Vector3(transform.position.x, (transform.position.y) - capcHeight / 3, transform.position.z);


            if (!moveLeft && !moveRight)
                _moveData.SideMove = 0;
            else if (moveLeft)
                _moveData.SideMove = -MoveConfig.Accel;
            else if (moveRight)
                _moveData.SideMove = MoveConfig.Accel;

            if (!moveFwd && !moveBack)
                _moveData.ForwardMove = 0;
            else if (moveFwd)
                _moveData.ForwardMove = MoveConfig.Accel;
            else if (moveBack)
                _moveData.ForwardMove = -MoveConfig.Accel;

            if (Input.GetKeyDown(JumpButton))
            {
                _moveData.Buttons = _moveData.Buttons.AddFlag((int)InputButtons.Jump);
            }
            else
                _moveData.Buttons = _moveData.Buttons.RemoveFlag((int)InputButtons.Jump);

            if (crouch)
            {
                capc.height = ColliderSizeY / 2;

            }
            else
            {
                if (!Physics.Raycast(transform.position, up, capc.height / 2))
                {
                    if (capc.height < ColliderSizeY - (ColliderSizeY / 100.0f))
                        capc.height = Mathf.Lerp(capc.height, ColliderSizeY, Time.deltaTime * 8f);
                    else
                        capc.height = ColliderSizeY;
                }
            }

            if (Physics.Raycast(stepRayLower, transform.TransformDirection(Vector3.forward), capcRadius * 1.1f))
            {

                if (!Physics.Raycast(stepRayUpper, transform.TransformDirection(Vector3.forward), capcRadius * 1.1f))
                {

                    MoveData.Origin += new Vector3(0, 0.1f, 0);
                }
            }

            //Debug.DrawRay(transform.position, up, Color.green);
            _moveData.OldButtons = _moveData.Buttons;
            _moveData.ViewAngles = _angles;

        }
        private void LateUpdate()
        {
            viewTransform.rotation = Quaternion.Euler(_angles);

        }
        private void UpdateRotation()
        {
            float mx = (Input.GetAxis("Mouse X") * XSens * .02f);
            float my = Input.GetAxis("Mouse Y") * YSens * .02f;
            var rot = _angles + new Vector3(-my, mx, 0f);
            rot.x = Mathf.Clamp(rot.x, -85f, 85f);
            _angles = rot;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static float ClampAngle(float angle, float from, float to)
        {
            if (angle < 0f) angle = 360 + angle;
            if (angle > 180f) return Mathf.Max(angle, 360 + from);
            return Mathf.Min(angle, to);
        }

        /*        private void stepUp(){
                    Debug.Log("test1");
                    if (Physics.Raycast(stepRayLower, transform.TransformDirection(Vector3.forward), capcRadius*2)) {
                        Debug.Log("test2");
                        if (!Physics.Raycast(stepRayUpper, transform.TransformDirection(Vector3.forward), capcRadius)){
                            Debug.Log("test3");
                            MoveData.Origin += new Vector3(0, 0.1f, 0);
                        }
                    }
                }*/

    }
}

