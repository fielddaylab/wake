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
    public class SeaOtterActor : ActorModule
    {
        static public class Behaviors
        {
            static public readonly StringHash32 EatsUrchin = "SeaOtter.Eats.Urchin";
        }

        #region Inspector

        [SerializeField, Required] private ActorSense m_FoodSense = null;
        [SerializeField, Required] private ParticleSystem m_EatParticles = null;

        [SerializeField, Required] private ActorPools m_Pools = null;

        [SerializeField, Required] private Transform Front = null;

        #endregion // Inspector

        [NonSerialized] private Routine m_Anim;

        private void OnCreate()
        {
            Actor.Body.WorldTransform.SetRotation(RNG.Instance.NextFloat(30f), Axis.Z, Space.Self);
        }

        private void OnThink()
        {
            if (!m_Anim)
            {
                m_Anim.Replace(this, Animation());
            }
        }

        private IEnumerator Animation()
        {
            int swims = GetIdleSwimCount();
            while(true)
            {
                while(swims-- > 0)
                {

                    Vector3 NextPosition = Actor.Nav.Helper.GetRandomSwimTarget(Actor.Body.BodyRadius, Actor.Body.BodyRadius, Actor.Body.BodyRadius);
                    RotateActor(NextPosition);

                    yield return Actor.Nav.SwimTo(NextPosition);
                    yield return RNG.Instance.NextFloat(GetProperty<float>("MinSwimDelay", 0.5f), GetProperty<float>("MaxSwimDelay", 1));
                }

                ICreature nearestFood = GetNearestFoodSource();
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

        private void RotateActor(Vector3 nextPosition)
        {
            Vector3 CurrPosition = Actor.Body.RenderGroup.transform.position;
            Vector3 TargetDirection = nextPosition - CurrPosition;
            Vector3 CurrDirection = GetCurrDirection();
            float y = 0;
            float angle = Vector3.Angle(GetCurrDirection(), TargetDirection);
            if (angle > 90f)
            {
                angle = 180f - angle;
                y = 180f;
            }
            Actor.Body.WorldTransform.Rotate(0f, y, angle);

            return;

        }

        private Vector3 GetCurrDirection()
        {
            return Front.position - Actor.Body.WorldTransform.position;
        }

        private int GetIdleSwimCount()
        {
            return RNG.Instance.Next(GetProperty<int>("MinSwimsBeforeEat", 3), GetProperty<int>("MaxSwimsBeforeEat", 4) + 1);
        }

        private ICreature GetNearestFoodSource()
        {
            Vector2 myPos = Actor.Body.WorldTransform.position;
            WeightedSet<ICreature> food = new WeightedSet<ICreature>();
            
            foreach(var obj in m_FoodSense.SensedObjects)
            {
                ICreature source = obj.Collider.GetComponentInParent<ICreature>();
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

        private IEnumerator EatAnimation(ICreature inFoodSource)
        {
            Transform targetTransform;
            Vector3 targetOffset;
            inFoodSource.TryGetEatLocation(Actor, out targetTransform, out targetOffset);
            Vector3 NextPosition = targetTransform.position + targetOffset;
            RotateActor(NextPosition);
            yield return Actor.Nav.SwimTo(NextPosition);
            yield return 0.5f;

            using (ExperimentServices.BehaviorCapture.GetCaptureInstance(Actor, Behaviors.EatsUrchin))
            {
                yield return Actor.Body.WorldTransform.ScaleTo(1.1f, 0.2f).Ease(Curve.CubeOut);
                inFoodSource.Bite(Actor, GetProperty<float>("BiteSize", 5));
                Services.Audio.PostEvent("seaotter_eat");
                if (ExperimentServices.BehaviorCapture.WasObserved(Behaviors.EatsUrchin))
                {
                        m_EatParticles.Emit(1);
                }
                yield return Actor.Body.WorldTransform.ScaleTo(1, 0.2f).Ease(Curve.CubeOut);
                yield return RNG.Instance.NextFloat(0.8f, 1.2f);
            }
        }

        public override void OnConstruct()
        {
            base.OnConstruct();

            Actor.Callbacks.OnCreate = OnCreate;
            Actor.Callbacks.OnThink = OnThink;

            m_FoodSense.Listener.FilterByComponentInParent<ICreature>();
        }
    }
}