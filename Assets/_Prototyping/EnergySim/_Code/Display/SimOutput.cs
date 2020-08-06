using System.Runtime.InteropServices;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using System;

namespace ProtoAqua.Energy
{
    public class SimOutput : MonoBehaviour
    {
        private const int ActorFlag = 0x01;
        private const int EnvFlag = 0x02;

        #region Types

        [Serializable] private class VarDisplayPool : SerializablePool<SimVariableDisplay> { }

        #endregion // Types

        #region Inspector

        [Header("Variables")]

        [SerializeField] private VarDisplayPool m_VarPool = null;
        [SerializeField] private Transform m_VarSeparator = null;

        [Header("Populations")]

        [SerializeField] private ImagePool m_ActorPool = null;
        [SerializeField] private Sprite m_DefaultActorSprite = null;

        #endregion // Inspector

        [NonSerialized] private int m_SeparatorCheckedFlags = 0;

        #region Unity Events

        private void Awake()
        {
            m_ActorPool.Initialize();
            m_VarSeparator.gameObject.SetActive(false);
        }

        #endregion // Unity Events

        public void Display(ScenarioPackageHeader inHeader, in EnergySimContext inContext, EnergySimState inComparison = null)
        {
            System.Random random = new System.Random((int) (inContext.CachedCurrent.NextSeedA ^ uint.MaxValue));

            m_VarPool.Reset();
            m_VarSeparator.gameObject.SetActive(false);
            m_SeparatorCheckedFlags = 0;

            DisplayActorCounts(inHeader, inContext, inComparison, random);
            DisplayEnvVars(inHeader, inContext, inComparison, random);
            DisplayActorDots(inHeader, inContext, random);

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) m_VarPool.DefaultSpawnTransform);
        }

        private void CheckVarDivider(int inFlag)
        {
            if ((m_SeparatorCheckedFlags & inFlag) == inFlag)
                return;

            m_SeparatorCheckedFlags |= inFlag;

            if (m_VarPool.ActiveObjects.Count > 0 && !m_VarSeparator.gameObject.activeSelf)
            {
                m_VarSeparator.gameObject.SetActive(true);
                m_VarSeparator.SetAsLastSibling();
            }
        }

        private void DisplayEnvVars(ScenarioPackageHeader inHeader, in EnergySimContext inContext, EnergySimState inComparison, System.Random inRandom)
        {
            int resCount = inContext.Database.Resources.Count();
            for(int i = 0; i < resCount; ++i)
            {
                VarType varType = inContext.Database.Resources[i];
                ushort count = inContext.CachedCurrent.Environment.OwnedResources[i];

                if (varType.HasFlags(VarTypeFlags.HideAlways))
                    continue;

                if (varType.HasFlags(VarTypeFlags.HideIfZero) && count <= 0)
                    continue;

                if (inHeader != null)
                {
                    if (!varType.HasAnyContentArea(inHeader.ContentAreas))
                        continue;
                }

                string label = varType.ScriptName();
                float diff = 0;
                if (inComparison != null)
                {
                    diff = (float) count - inComparison.Environment.OwnedResources[i];
                }

                CheckVarDivider(EnvFlag);
                
                m_VarPool.Alloc().Display(label, count.ToString(), diff);
            }

            int propCount = inContext.Database.Properties.Count();
            for(int i = 0; i < propCount; ++i)
            {
                VarType varType = inContext.Database.Properties[i];
                float value = inContext.CachedCurrent.Environment.Properties[i];

                if (varType.HasFlags(VarTypeFlags.HideAlways))
                    continue;

                if (varType.HasFlags(VarTypeFlags.HideIfZero) && value == 0)
                    continue;

                if (inHeader != null)
                {
                    if (!varType.HasAnyContentArea(inHeader.ContentAreas))
                        continue;
                }

                string label = varType.ScriptName();
                float diff = 0;
                if (inComparison != null)
                {
                    diff = value - inComparison.Environment.Properties[i];
                }

                CheckVarDivider(EnvFlag);
                
                m_VarPool.Alloc().Display(label, value.ToString(), diff);
            }
        }

        private void DisplayActorCounts(ScenarioPackageHeader inHeader, in EnergySimContext inContext, EnergySimState inComparison, System.Random inRandom)
        {
            int actorCount = inContext.Database.Actors.Count();
            for(int i = 0; i < actorCount; ++i)
            {
                ActorType actorType = inContext.Database.Actors[i];
                bool bIncludedInStartingState = false;

                if (inContext.Scenario != null)
                {
                    foreach(var id in inContext.Scenario.Data.StartingActorIds())
                    {
                        if (id == actorType.Id())
                        {
                            bIncludedInStartingState = true;
                            break;
                        }
                    }
                }

                if (!bIncludedInStartingState)
                    continue;

                var displayConfig = actorType.DisplaySettings();

                bool bDisplayPopulation = displayConfig.DisplayPopulation;
                bool bDisplayMass = displayConfig.DisplayMass || (actorType.Flags() & ActorTypeFlags.AllowPartialConsumption) != 0;

                if (bDisplayPopulation && bDisplayMass)
                {
                    string label = actorType.ScriptName() + " Population";
                    int count = inContext.CachedCurrent.Populations[i];
                    float diffCount = 0;
                    if (inComparison != null)
                    {
                        diffCount = (float) count - inComparison.Populations[i];
                    }

                    uint mass = inContext.CachedCurrent.Masses[i];
                    float diffMass = 0;
                    if (inComparison != null)
                    {
                        diffMass = (float) mass - inComparison.Masses[i];
                    }

                    CheckVarDivider(ActorFlag);
                    
                    m_VarPool.Alloc().Display(label, count.ToStringLookup(), diffCount, mass.ToString(), diffMass);
                }
                else if (bDisplayPopulation)
                {
                    string label = actorType.ScriptName() + " Population";
                    int count = inContext.CachedCurrent.Populations[i];
                    float diff = 0;
                    if (inComparison != null)
                    {
                        diff = (float) count - inComparison.Populations[i];
                    }

                    CheckVarDivider(ActorFlag);
                    
                    m_VarPool.Alloc().Display(label, count.ToStringLookup(), diff);
                }
                else if (bDisplayMass)
                {
                    string label = actorType.ScriptName() + " Mass";
                    uint mass = inContext.CachedCurrent.Masses[i];
                    float diff = 0;
                    if (inComparison != null)
                    {
                        diff = (float) mass - inComparison.Masses[i];
                    }

                    CheckVarDivider(ActorFlag);
                    
                    m_VarPool.Alloc().Display(label, mass.ToString(), diff);
                }
            }
        }

        private void DisplayActorDots(ScenarioPackageHeader inHeader, in EnergySimContext inContext, System.Random inRandom)
        {
            m_ActorPool.Reset();

            int actorCount = inContext.Database.Actors.Count();
            for(int i = 0; i < actorCount; ++i)
            {
                ActorType type = inContext.Database.Actors[i];
                ushort count = inContext.CachedCurrent.Populations[i];
                ActorType.DisplayConfig display = type.DisplaySettings();

                if (count == 0)
                    continue;

                bool bHerd = (type.Flags() & ActorTypeFlags.TreatAsHerd) == ActorTypeFlags.TreatAsHerd;
                for(int actorIdx = 0; actorIdx < inContext.CachedCurrent.ActorCount; ++actorIdx)
                {
                    ref ActorState actor = ref inContext.CachedCurrent.Actors[actorIdx];
                    if (actor.Type != type.Id())
                        continue;

                    ushort mass = actor.Mass;
                    if (mass == 0)
                        continue;

                    if (bHerd)
                    {
                        ushort scaledScale = (ushort) (display.MassScale * 8);

                        int fullHerds = mass / scaledScale;
                        int partialHerd = mass % scaledScale;
                        
                        while(--fullHerds >= 0)
                        {
                            SpawnActor(display, scaledScale, inRandom);
                        }

                        if (partialHerd > 0)
                        {
                            SpawnActor(display, (ushort) partialHerd, inRandom);
                        }
                    }
                    else
                    {
                        SpawnActor(display, mass, inRandom);
                    }
                }
            }
        }

        private Image SpawnActor(ActorType.DisplayConfig inConfig, ushort inMass, System.Random inRandom)
        {
            Image img = m_ActorPool.Alloc();
            
            Sprite spr = inConfig.Image;
            if (spr == null)
                spr = m_DefaultActorSprite;
            
            img.sprite = spr;
            img.color = inConfig.Color;

            RectTransform rect = img.rectTransform;

            float scale = (float) inMass / inConfig.MassScale;
            rect.SetScale(scale, Axis.XY);

            Vector2 pos = inRandom.NextVector2(Vector2.zero, Vector2.one);
            rect.anchorMin = rect.anchorMax = pos;
            rect.anchoredPosition = Vector2.zero;

            rect.gameObject.SetActive(true);

            return img;
        }
    }
}