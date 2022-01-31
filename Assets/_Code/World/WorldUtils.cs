using UnityEngine;
using BeauUtil;
using UnityEngine.Events;

namespace Aqua
{
    static public class WorldUtils
    {
        static public TriggerListener2D ListenForPlayer(Collider2D inCollider, UnityAction<Collider2D> inOnEnter, UnityAction<Collider2D> inOnExit)
        {
            return ListenForLayerMask(inCollider, GameLayers.Player_Mask, inOnEnter, inOnExit);
        }

        static public TriggerListener2D ListenForLayerMask(Collider2D inCollider, LayerMask inLayers, UnityAction<Collider2D> inOnEnter, UnityAction<Collider2D> inOnExit)
        {
            TriggerListener2D listener = inCollider.EnsureComponent<TriggerListener2D>();
            
            listener.SetOccupantTracking(false);
            listener.LayerFilter = inLayers;

            if (inOnEnter != null)
                listener.onTriggerEnter.AddListener(inOnEnter);
            if (inOnExit != null)
                listener.onTriggerExit.AddListener(inOnExit);

            return listener;
        }

        static public TriggerListener2D TrackPlayer(Collider2D inCollider, UnityAction<Collider2D> inOnEnter, UnityAction<Collider2D> inOnExit)
        {
            return TrackLayerMask(inCollider, GameLayers.Player_Mask, inOnEnter, inOnExit);
        }

        static public TriggerListener2D TrackLayerMask(Collider2D inCollider, LayerMask inLayers, UnityAction<Collider2D> inOnEnter, UnityAction<Collider2D> inOnExit)
        {
            TriggerListener2D listener = inCollider.EnsureComponent<TriggerListener2D>();
            
            listener.SetOccupantTracking(true);
            listener.LayerFilter = inLayers;

            if (inOnEnter != null)
                listener.onTriggerEnter.AddListener(inOnEnter);
            if (inOnExit != null)
                listener.onTriggerExit.AddListener(inOnExit);

            return listener;
        }
    }
}