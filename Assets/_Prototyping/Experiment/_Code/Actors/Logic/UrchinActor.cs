using System.Dynamic;
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
    public class UrchinActor : ActorModule, IFoodSource
    {
        #region Inspector

        [SerializeField] private Transform m_PivotTransform = null;
        [SerializeField] private Transform m_RenderTransform = null;
        [SerializeField, Required] private ActorSense m_FoodSense = null;
        [SerializeField, Required] private ParticleSystem m_EatParticles = null;

        #endregion // Inspector

        [NonSerialized] private Routine m_Anim;
        [NonSerialized] private KelpStem m_CurrStem = null;
        [NonSerialized] private GlassWall m_CurrWall = null;
        [NonSerialized] private bool descend = false;
        [NonSerialized] private float minKelpHeight = float.PositiveInfinity;


        #region IFoodSource

        StringHash32 IFoodSource.Id { get { return Actor.Id; } }

        bool IFoodSource.HasTag(StringHash32 inTag)
        {
            return inTag == "Urchin";
        }

        Transform IFoodSource.Transform { get { return m_RenderTransform; } }

        Collider2D IFoodSource.Collider { get { return Actor.Body.Collider; } }

        ActorCtrl IFoodSource.Parent { get { return Actor; } }

        float IFoodSource.EnergyRemaining { get { return 5; } }

        void IFoodSource.Bite(ActorCtrl inActor, float inBite)
        {
            m_Anim.Replace(this, BittenAnim());
        }
        
        bool IFoodSource.TryGetEatLocation(ActorCtrl inActor, out Transform outTransform, out Vector3 outOffset)
        {
            outTransform = m_PivotTransform;
            outOffset = Vector3.zero;
            return true;
        }

        private IEnumerator BittenAnim()
        {
            Actor.Recycle();
            yield break;
        }

        #endregion // IFoodSource

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
            Actor.Body.WorldTransform.SetRotation(RNG.Instance.NextFloat(360f), Axis.Z, Space.Self);
        }

        private void OnThink()
        {
            if (!m_Anim)
            {
                m_Anim.Replace(this, Animation());
            }
        }

        #endregion // Events

        #region Properties

        private int GetIdleSwimCount()
        {
            return RNG.Instance.Next(GetProperty<int>("MinSwimsBeforeEat", 3), GetProperty<int>("MaxSwimsBeforeEat", 4) + 1);
        }

        private int GetBiteCount()
        {
            return RNG.Instance.Next(GetProperty<int>("MinBites", 3), GetProperty<int>("MaxBites", 5) + 1);
        }

        #endregion // Properties

        private IEnumerator Animation()
        {
            int maxRetries = GetProperty<int>("NoSourceRetries", 20);
            int currRetries = 0;
            int swims = GetIdleSwimCount();
            while(true)
            {
                while(swims-- > 0)
                {
                    if (m_CurrStem != null)
                    {
                        yield return ClimbAnimation(m_CurrStem, descend);
                    }
                    else if(m_CurrWall != null)
                    {
                        yield return ClimbAnimation(m_CurrWall, descend);
                    }

                    else {
                        yield return Actor.Nav.SwimTo(Actor.Nav.Helper.GetFloorSpawnTarget(Actor.Body.BodyRadius, Actor.Body.BodyRadius));
                    }
                    
                    yield return RNG.Instance.NextFloat(GetProperty<float>("MinSwimDelay", 0.5f), GetProperty<float>("MaxSwimDelay", 1));
                }

                bool StemSearch = RNG.Instance.NextBool();

                if(StemSearch) 
                {
                    m_CurrStem = m_CurrStem == null? GetNearestStem() : m_CurrStem;
                }

                if(m_CurrStem == null)
                {
                    if(m_CurrWall == null)
                    {
                        currRetries += 1;
                        if(currRetries > maxRetries)
                        {
                            m_CurrWall = GetNearestWall();
                            if(m_CurrWall != null)  currRetries = 0;
                        }
                    }
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

        private KelpStem GetNearestStem() {
            Vector2 myPos = Actor.Body.WorldTransform.position;
            foreach(var obj in m_FoodSense.SensedObjects) 
            {
                KelpStem stem = obj.Collider.GetComponentInParent<KelpStem>();
                if(stem == null) continue;
                if(!stem.hasSpine()) return null;
                stem.ResetPosition(obj.Collider.transform.position);

                if(stem.height < minKelpHeight) minKelpHeight = stem.height;
                return stem;
            }
            
            return null;
        }

        private GlassWall GetNearestWall()
        {
            Vector2 myPos = Actor.Body.WorldTransform.position;
            foreach(var obj in m_FoodSense.SensedObjects)
            {
                GlassWall wall = obj.Collider.GetComponentInParent<GlassWall>();
                if(wall == null) continue;
                wall.ResetPosition(obj.Collider.transform.position);
                
                return wall;
            }

            return null;
        }

        private IEnumerator ClimbAnimation(Climbable stem, bool descend=false) {
            Vector2 myPos = Actor.Body.WorldTransform.position;
            float insideOffset = stem.Settings == ClimbSettings.KelpStem? Actor.Body.BodyRadius : 0f;
            yield return Actor.Nav.SwimTo(
                Actor.Nav.Helper.GetClimb(stem.root, insideOffset, GetProperty<float>("ClimbSpeed", 0.3f), myPos.y, descend));
            yield return RNG.Instance.NextFloat(GetProperty<float>("MinSwimDelay", 0.5f), GetProperty<float>("MaxSwimDelay", 1));
            if (Actor.Nav.Helper.ReachedTheFloor(myPos, Actor.Body.BodyRadius))
            {
                descend = false;
                m_CurrStem = null;
            }
            else if(myPos.y >= stem.height) {
                descend = true;
            }

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

                if (source.EnergyRemaining <= 0)
                    continue;

                if (!source.HasTag("Kelp") || !source.HasTag("Sargassum"))
                    continue;

                float dist = Vector2.Distance(source.Transform.position, myPos);
                float weight = (source.EnergyRemaining / 100f) * (100f - dist);
                food.Add(source, weight);

            }

            food.FilterHigh(food.TotalWeight * GetProperty<float>("FoodFilterThreshold", 0.2f));
            return food.GetItemNormalized(RNG.Instance.NextFloat());
        }

        private IEnumerator EatAnimation(IFoodSource inFoodSource)
        {
            Transform targetTransform;
            Vector3 targetOffset;
            inFoodSource.TryGetEatLocation(Actor, out targetTransform, out targetOffset);

            yield return Actor.Nav.SwimTo(targetTransform.position + targetOffset);
            yield return 0.5f;

            BFEat eatingBehavior = BestiaryUtils.FindEatingRule(Actor.Bestiary, inFoodSource.Parent.Bestiary.Id());
            using(ExperimentServices.BehaviorCapture.GetCaptureInstance(Actor, eatingBehavior.Id()))
            {
                int biteCount = GetBiteCount();
                while(biteCount-- > 0)
                {
                    yield return Actor.Body.WorldTransform.ScaleTo(1.1f, 0.2f).Ease(Curve.CubeOut);
                    inFoodSource.Bite(Actor, GetProperty<float>("BiteSize", 5));
                    Services.Audio.PostEvent("urchin_eat");
                    if (ExperimentServices.BehaviorCapture.WasObserved(eatingBehavior.Id()))
                    {
                        m_EatParticles.Emit(1);
                    }
                    yield return Actor.Body.WorldTransform.ScaleTo(1, 0.2f).Ease(Curve.CubeOut);
                    yield return RNG.Instance.NextFloat(0.8f, 1.2f);
                }
            }
            float height = (targetTransform.position + targetOffset).y;
            if(m_CurrStem != null) {
                descend = true;
            }
        }
    }
}