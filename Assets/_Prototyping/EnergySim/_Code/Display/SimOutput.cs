using System.Runtime.InteropServices;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BeauPools;
using BeauRoutine;
using BeauUtil;

namespace ProtoAqua.Energy
{
    public class SimOutput : MonoBehaviour
    {
        #region Inspector

        [Header("Env Vars")]

        [SerializeField] private TMP_Text[] m_EnvVarCounts = null;

        [Header("Actor Counts")]

        [SerializeField] private TMP_Text[] m_ActorCounts = null;

        [Header("Populations")]

        [SerializeField] private ImagePool m_ActorPool = null;
        [SerializeField] private Sprite m_DefaultActorSprite = null;

        #endregion // Inspector

        #region Unity Events

        private void Awake()
        {
            m_ActorPool.Initialize();
        }

        #endregion // Unity Events

        public void Display(ScenarioPackageHeader inHeader, in EnergySimContext inContext)
        {
            System.Random random = new System.Random((int) (inContext.CachedCurrent.NextSeedA ^ uint.MaxValue));

            DisplayEnvVars(inHeader, inContext, random);
            DisplayActorCounts(inHeader, inContext, random);
            DisplayActorDots(inHeader, inContext, random);
        }

        private void DisplayEnvVars(ScenarioPackageHeader inHeader, in EnergySimContext inContext, System.Random inRandom)
        {
            int textIdx = 0;
            int resCount = inContext.Database.Resources.Count();
            for(int i = 0; i < resCount; ++i)
            {
                VarType type = inContext.Database.Resources[i];
                ushort count = inContext.CachedCurrent.Environment.OwnedResources[i];

                if (type.HasFlags(VarTypeFlags.HideAlways))
                    continue;

                if (type.HasFlags(VarTypeFlags.HideIfZero) && count <= 0)
                    continue;

                if (inHeader != null)
                {
                    if (!type.HasAnyContentArea(inHeader.ContentAreas))
                        continue;
                }
                
                TMP_Text element = m_EnvVarCounts[textIdx++];
                element.SetText(string.Format("{0}: {1}", inContext.Database.Resources.IndexToId(i).ToString(), count));
                element.gameObject.SetActive(true);
            }

            int propCount = inContext.Database.Properties.Count();
            for(int i = 0; i < propCount; ++i)
            {
                VarType type = inContext.Database.Properties[i];
                float value = inContext.CachedCurrent.Environment.Properties[i];

                if (type.HasFlags(VarTypeFlags.HideAlways))
                    continue;

                if (type.HasFlags(VarTypeFlags.HideIfZero) && value == 0)
                    continue;

                if (inHeader != null)
                {
                    if (!type.HasAnyContentArea(inHeader.ContentAreas))
                        continue;
                }
                
                TMP_Text element = m_EnvVarCounts[textIdx++];
                element.SetText(string.Format("{0}: {1}", inContext.Database.Properties.IndexToId(i).ToString(), value));
                element.gameObject.SetActive(true);
            }

            for(; textIdx < m_EnvVarCounts.Length; ++textIdx)
            {
                TMP_Text element = m_EnvVarCounts[textIdx];
                element.SetText(string.Empty);
                element.gameObject.SetActive(false);
            }
        }

        private void DisplayActorCounts(ScenarioPackageHeader inHeader, in EnergySimContext inContext, System.Random inRandom)
        {
            int textIdx = 0;
            int actorCount = inContext.Database.Actors.Count();
            for(int i = 0; i < actorCount; ++i)
            {
                TMP_Text element = m_ActorCounts[textIdx++];

                ushort count = inContext.CachedCurrent.Populations[i];
                uint mass = inContext.CachedCurrent.Masses[i];
                element.SetText(string.Format("{0}: {1} ({2})", inContext.Database.Actors.IndexToId(i), count, mass));
                element.gameObject.SetActive(true);
            }

            for(; textIdx < m_ActorCounts.Length; ++textIdx)
            {
                TMP_Text element = m_ActorCounts[textIdx];
                element.SetText(string.Empty);
                element.gameObject.SetActive(false);
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