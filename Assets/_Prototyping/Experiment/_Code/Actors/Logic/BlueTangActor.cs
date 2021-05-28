using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using BeauUtil.Variants;
using BeauRoutine.Extensions;
using BeauUtil.Debugger;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class BlueTangActor : ActorModule
    {
        #region Inspector

        [SerializeField, Required] private ActorSense m_FoodSense = null;
        [SerializeField, Required] private ParticleSystem m_EatParticles = null;
        [SerializeField, Required] private Transform Front = null;
        [SerializeField, Required] private SpriteRenderer spriteRenderer;

        #endregion // Inspector

        [NonSerialized] private Routine m_Anim;
        [NonSerialized] private ActorStateId m_stressSate; //tracks if this critter is currently stressed or not

        #region Events

        public override void OnConstruct()
        {
            base.OnConstruct();

            Actor.Callbacks.OnCreate = OnCreate;
            Actor.Callbacks.OnThink = OnThink;

            m_FoodSense.Listener.FilterByComponentInParent<IFoodSource>();
        }

        private void OnCreate()
        {
            Actor.Body.WorldTransform.SetRotation(RNG.Instance.NextFloat(30f), Axis.Z, Space.Self);
        }

        private void OnThink()
        {
            if (!m_Anim)
            {
                m_stressSate = Actor.getActorStressState();
                m_Anim.Replace(this, Animation());
            }
        }

        #endregion // Events

        private IEnumerator Animation()
        {
            if (m_stressSate == ActorStateId.Alive)
            {
                int swims = GetIdleSwimCount();
                while (true)
                {
                    while (swims-- > 0)
                    {
                        Vector3 NextPosition = Actor.Nav.Helper.GetRandomSwimTarget(
                            Actor.Body.BodyRadius, Actor.Body.BodyRadius, Actor.Body.BodyRadius);
                        RotateActor(NextPosition);

                        yield return Actor.Nav.SwimTo(NextPosition);
                        yield return RNG.Instance.NextFloat(GetProperty<float>("MinSwimDelay", 0.5f), GetProperty<float>("MaxSwimDelay", 1));
                    }

                    IFoodSource nearestFood = GetNearestFoodSource();
                    if (nearestFood == null)
                    {
                        swims = 1;
                    }
                    else
                    {
                        yield return EatAnimation(nearestFood);
                        swims = GetIdleSwimCount();
                    }
                }
            }   
            else if (m_stressSate == ActorStateId.Stressed)
            {

            }
        }

        private void RotateActor(Vector3 nextPosition)
        {
            float turnSpeed = GetProperty<float>("swimSpeed", 0.5f);
            Vector3 LookAt = nextPosition - Actor.Body.RenderGroup.transform.position;
            Actor.Body.WorldTransform.rotation = Quaternion.Slerp(Actor.Body.WorldTransform.rotation, Quaternion.LookRotation(LookAt), turnSpeed * Time.deltaTime);

            return;
            // Vector3 CurrPosition = Actor.Body.RenderGroup.transform.position;
            // Vector3 TargetDirection = nextPosition - CurrPosition;
            // Vector3 CurrDirection = GetCurrDirection();
            // float y = 0;
            // float angle = Vector3.Angle(GetCurrDirection(), TargetDirection);
            // if (angle > 90f)
            // {
            //     angle = 180f - angle;
            //     y = 180f;
            // }
            // Actor.Body.WorldTransform.Rotate(0f, y, angle);

            // return;

        }

        // private Vector3 GetCurrDirection()
        // {
        //     return Front.position - Actor.Body.WorldTransform.position;
        // }

        private int GetIdleSwimCount()
        {
            return RNG.Instance.Next(GetProperty<int>("MinSwimsBeforeEat", 3), GetProperty<int>("MaxSwimsBeforeEat", 4) + 1);
        }

        private IFoodSource GetNearestFoodSource()
        {
            Vector2 myPos = Actor.Body.WorldTransform.position;
            WeightedSet<IFoodSource> food = new WeightedSet<IFoodSource>();
            
            foreach(var obj in m_FoodSense.SensedObjects)
            {
                IFoodSource source = obj.Collider.GetComponentInParent<IFoodSource>();
                if (source == null)
                    continue;

                if (!source.HasTag("Urchin"))
                    continue;

                float dist = Vector2.Distance(source.Transform.position, myPos);
                float weight = (100f - dist);
                food.Add(source, weight);
            }

            // food.FilterHigh(food.TotalWeight * GetProperty<float>("FoodFilterThreshold", 0.5f));
            return food.GetItemNormalized(RNG.Instance.NextFloat());
        }

        private IEnumerator EatAnimation(IFoodSource inFoodSource)
        {
            StringHash32 originalId = inFoodSource.Id;

            Transform targetTransform;
            Vector3 targetOffset;
            
            int tries = 3;
            while(tries > 0)
            {
                inFoodSource.TryGetEatLocation(Actor, out targetTransform, out targetOffset);
                Vector3 NextPosition = targetTransform.position + targetOffset;
                RotateActor(NextPosition);
                yield return Actor.Nav.SwimTo(NextPosition);
                yield return 0.5f;

                if (inFoodSource.Id != originalId)
                    yield break;

                if (Actor.Body.Rigidbody.IsOverlapping(inFoodSource.Collider))
                    break;

                if (--tries <= 0)
                    yield break;
            }

            BFEat eatingBehavior = BestiaryUtils.FindEatingRule(Actor.Bestiary, inFoodSource.Parent.Bestiary.Id());
            
            using (ExperimentServices.BehaviorCapture.GetCaptureInstance(Actor, eatingBehavior.Id()))
            {
                yield return Actor.Body.WorldTransform.ScaleTo(1.1f, 0.2f).Ease(Curve.CubeOut);
                inFoodSource.Bite(Actor, GetProperty<float>("BiteSize", 5));
                // Services.Audio.PostEvent("seaotter_eat");
                if (ExperimentServices.BehaviorCapture.WasObserved(eatingBehavior.Id()))
                {
                    m_EatParticles.Emit(1);
                }
                yield return Actor.Body.WorldTransform.ScaleTo(1, 0.2f).Ease(Curve.CubeOut);
                yield return RNG.Instance.NextFloat(0.8f, 1.2f);
            }
        }
    }
}