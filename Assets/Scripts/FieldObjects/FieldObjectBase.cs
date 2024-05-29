using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Framework.Variables;
using Framework.SystemInfo;

namespace ClashTheCube
{
    [RequireComponent(typeof(Rigidbody))]
    public abstract class FieldObjectBase : MonoBehaviour
    {
        [SerializeField] protected Vector2Variable swipeDelta;
        [SerializeField] protected FloatReference xSwipeDeltaMultiplier;
        [SerializeField] protected FloatReference xSwipeDeltaMultiplierDesktop;
        [SerializeField] protected FloatReference ySwipeDeltaMultiplier;
        [SerializeField] protected FloatReference ySwipeDeltaMultiplierDesktop;

        [SerializeField] protected FloatReference force;
        [SerializeField] protected FloatReference forceConstraint;
        [SerializeField] protected FloatReference velocity;
        [SerializeField] protected FloatReference yConstraint;

        [SerializeField] protected GameObject directionLine;

        [SerializeField] protected int linePoints;
        [SerializeField] protected float timeIntervalInPoints;
        [SerializeField] private float lineStartWidth, lineEndWidth;
        [SerializeField] private string startColorHex, endColorHex;

        [SerializeField] Transform hitMarker;
        [SerializeField] LayerMask redLineLayer;
        [SerializeField, Range(10, 100)] int maxPoints = 50;
        [SerializeField, Range(0.01f, 0.5f)] float increment = 0.025f;
        [SerializeField, Range(1.05f, 2f)] float rayOverlap = 1.1f;

        public FieldObjectState State { get; protected set; }
        public Rigidbody Body { get; private set; }
        public LineRenderer DirectionLine { get; private set; }

        protected Vector3 destPosition;
        protected Vector3 forceDirection;
        protected bool sleeping;
        protected IFieldObjectHolder objectHolder;
        protected float currentForce;

        public void SetObjectHolder(IFieldObjectHolder holder)
        {
            objectHolder = holder;
            objectHolder.AddObject(this);
        }
        
        protected void Awake()
        {
            Body = GetComponent<Rigidbody>();
            sleeping = true;
            currentForce = force;
            forceDirection = Vector3.forward + Vector3.up;

            if (DirectionLine == null)
            {
                DirectionLine = directionLine.GetComponent<LineRenderer>();
            }
            SetLineParameters();
        }

        protected void Update()
        {
            PredictTrajectory();

            if (State != FieldObjectState.Initial)
            {
                return;
            }
        }

        protected void FixedUpdate()
        {
            sleeping = Body.velocity.magnitude < 0.1f;
        }

        public void MoveLeft()
        {
            forceDirection += Vector3.right * swipeDelta.Value.x * GetXDeltaMultiplier();
        }

        public void MoveRight()
        {
            forceDirection += Vector3.right * swipeDelta.Value.x * GetXDeltaMultiplier();
        }

        public void MoveDown()
        {
            currentForce += swipeDelta.Value.y * GetYDeltaMultiplier();

            if (currentForce < force - forceConstraint)
            {
                currentForce = force - forceConstraint;
            }
        }

        public void MoveUp()
        {
            currentForce += swipeDelta.Value.y * GetYDeltaMultiplier();

            if (currentForce > force + forceConstraint)
            {
                currentForce = force + forceConstraint;
            }
        }

        public void Accelerate()
        {
            if (State != FieldObjectState.Initial)
            {
                return;
            }

            Body.isKinematic = false;
            Body.AddForce(forceDirection * currentForce, ForceMode.Impulse);

            State = FieldObjectState.Transition;
        }

        private float GetXDeltaMultiplier()
        {
            return Platform.IsMobilePlatform() ? xSwipeDeltaMultiplier : xSwipeDeltaMultiplierDesktop;
        }

        private float GetYDeltaMultiplier()
        {
            return Platform.IsMobilePlatform() ? ySwipeDeltaMultiplier : ySwipeDeltaMultiplierDesktop;
        }

        private void SetLineParameters()
        {
            Color newCol;
            
            DirectionLine.startWidth = lineStartWidth;
            DirectionLine.endWidth = lineEndWidth;

            DirectionLine.material = new Material(Shader.Find("Sprites/Default"));

            if (ColorUtility.TryParseHtmlString(startColorHex, out newCol))
                DirectionLine.startColor = newCol;
            if (ColorUtility.TryParseHtmlString(endColorHex, out newCol))
                DirectionLine.endColor = newCol;
        }

        public void UpdateDirectionLine()
        {
            var active = State == FieldObjectState.Initial;

            if (directionLine.activeInHierarchy != active)
            {
                directionLine.SetActive(active);
                hitMarker.gameObject.SetActive(active);
            }
        }

        private void PredictTrajectory()
        {
            Vector3 velocity = forceDirection * currentForce / Body.mass;
            Vector3 position = transform.position;
            Vector3 nextPosition;
            float overlap;

            UpdateLineRender(maxPoints, (0, position));

            for (int i = 1; i < maxPoints; i++)
            {
                velocity = CalculateNewVelocity(velocity, Body.drag, increment);
                nextPosition = position + velocity * increment;

                overlap = Vector3.Distance(position, nextPosition) * rayOverlap;

                if (Physics.Raycast(position, velocity.normalized, out RaycastHit hit, overlap, ~redLineLayer))
                {
                    UpdateLineRender(i, (i - 1, hit.point));
                    MoveHitMarker(hit);
                    break;
                }

                position = nextPosition;
                UpdateLineRender(maxPoints, (i, position));
            }
        }

        private void UpdateLineRender(int count, (int point, Vector3 pos) pointPos)
        {
            DirectionLine.positionCount = count;
            DirectionLine.SetPosition(pointPos.point, pointPos.pos);
        }

        private Vector3 CalculateNewVelocity(Vector3 velocity, float drag, float increment)
        {
            velocity += Physics.gravity * increment;
            velocity *= Mathf.Clamp01(1f - drag * increment);
            return velocity;
        }

        private void MoveHitMarker(RaycastHit hit)
        {
            float offset = 0.025f;
            hitMarker.position = hit.point + hit.normal * offset;
            hitMarker.rotation = Quaternion.LookRotation(hit.normal, Vector3.up);
        }
    }
}
