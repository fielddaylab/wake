using UnityEngine;
using Aqua;
using Aqua.Scripting;
using BeauUtil;
using System.Collections;
using BeauRoutine;

namespace ProtoAqua.Observation
{
    [RequireComponent(typeof(ScriptDestructible))]
    public class BreakableRock : MonoBehaviour {
        [SerializeField] private Collider2D m_SolidCollider = null;
        [SerializeField] private GameObject m_Renderer = null;
        [SerializeField] private ParticleSystem m_BreakParticles = null;
        [SerializeField] private SerializedHash32 m_BreakSFX = null;

        private void Awake() {
            GetComponent<ScriptDestructible>().OnDestruct = DestructSequence;
        }

        static private IEnumerator DestructSequence(ScriptDestructible destructible) {
            BreakableRock rock = destructible.GetComponent<BreakableRock>();
            rock.m_SolidCollider.enabled = false;
            rock.m_BreakParticles.Play(true);
            Services.Audio.PostEvent(rock.m_BreakSFX);
            Services.Camera.AddShake(0.3f, 0.1f, 0.8f);
            yield return null;
            rock.m_Renderer.SetActive(false);
            yield return rock.m_BreakParticles.WaitToComplete();
        }
    }
}