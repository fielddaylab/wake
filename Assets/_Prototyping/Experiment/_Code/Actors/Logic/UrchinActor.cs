using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using BeauUtil.Variants;
using BeauRoutine.Extensions;
using BeauUtil.Debugger;

namespace ProtoAqua.Experiment
{
    public class UrchinActor : ActorModule
    {
        static public class Behaviors
        {
            static public readonly StringHash32 EatsKelp = "Urchin.Eats.Kelp";
        }

        #region Inspector

        [SerializeField, Required] private ActorSense m_FoodSense = null;
        [SerializeField, Required] private ParticleSystem m_EatParticles = null;

        #endregion // Inspector

        [NonSerialized] private Routine m_Anim;

        private void OnCreate()
        {
            Actor.Body.WorldTransform.SetRotation(RNG.Instance.NextFloat(360f), Axis.Z, Space.Self);
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
                    yield return Actor.Nav.SwimTo(Actor.Nav.Helper.GetRandomSwimTarget(Actor.Body.BodyRadius, Actor.Body.BodyRadius, Actor.Body.BodyRadius));
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

        private int GetIdleSwimCount()
        {
            return RNG.Instance.Next(GetProperty<int>("MinSwimsBeforeEat", 3), GetProperty<int>("MaxSwimsBeforeEat", 4) + 1);
        }

        private int GetBiteCount()
        {
            return RNG.Instance.Next(GetProperty<int>("MinBites", 3), GetProperty<int>("MaxBites", 5) + 1);
        }

        private IFoodSource GetNearestFoodSource()
        {
            Vector2 myPos = Actor.Body.WorldTransform.position;
            WeightedSet<IFoodSource> food = new WeightedSet<IFoodSource>();
            
            foreach(var obj in m_FoodSense.SensedObjects)
            {
                IFoodSource source = obj.Collider.GetComponentInParent<IFoodSource>();
                if (source.EnergyRemaining <= 0)
                    continue;

                if (!source.HasTag("Kelp"))
                    continue;

                float dist = Vector2.Distance(source.Transform.position, myPos);
                float weight = (source.EnergyRemaining / 100f) * (100f - dist);
                food.Add(source, weight);
            }

            food.FilterHigh(food.TotalWeight * GetProperty<float>("FoodFilterThreshold", 0.5f));
            return food.GetItemNormalized(RNG.Instance.NextFloat());
        }

        private IEnumerator EatAnimation(IFoodSource inFoodSource)
        {
            Transform targetTransform;
            Vector3 targetOffset;
            inFoodSource.TryGetEatLocation(Actor, out targetTransform, out targetOffset);

            yield return Actor.Nav.SwimTo(targetTransform.position + targetOffset);
            yield return 0.5f;
            
            using(ExperimentServices.BehaviorCapture.GetCaptureInstance(Actor, Behaviors.EatsKelp))
            {
                int biteCount = GetBiteCount();
                while(biteCount-- > 0)
                {
                    yield return Actor.Body.WorldTransform.ScaleTo(1.1f, 0.2f).Ease(Curve.CubeOut);
                    inFoodSource.Bite(Actor, GetProperty<float>("BiteSize", 5));
                    Services.Audio.PostEvent("urchin_eat");
                    if (ExperimentServices.BehaviorCapture.WasObserved(Behaviors.EatsKelp))
                    {
                        m_EatParticles.Emit(1);
                    }
                    yield return Actor.Body.WorldTransform.ScaleTo(1, 0.2f).Ease(Curve.CubeOut);
                    yield return RNG.Instance.NextFloat(0.8f, 1.2f);
                }
            }
        }

        public override void OnConstruct()
        {
            base.OnConstruct();

            Actor.Callbacks.OnCreate = OnCreate;
            Actor.Callbacks.OnThink = OnThink;

            m_FoodSense.Listener.FilterByComponentInParent<IFoodSource>();
        }
    }
}