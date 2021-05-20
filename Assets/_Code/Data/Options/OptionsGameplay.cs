using AquaAudio;
using BeauData;
using BeauUtil;

namespace Aqua.Option
{
    /// <summary>
    /// Gameplay options settings.
    /// </summary>
    public struct OptionGameplay : ISerializedObject, ISerializedVersion
    {
        public MovementMode Movement;

        // observation
        public ButtonMode ObservationMode;
        public ButtonMode ObservationAction;

        public void SetDefaults()
        {
            Movement = MovementMode.Hold;
            ObservationMode = ButtonMode.Hold;
            ObservationMode = ButtonMode.Hold;
        }

        public ushort Version { get { return 1; } }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.Enum("movementMode", ref Movement);
            ioSerializer.Enum("observationMode", ref ObservationMode);
            ioSerializer.Enum("observationAction", ref ObservationAction);
        }
    }

    public enum MovementMode : byte
    {
        Hold,
        Path
    }

    public enum ButtonMode : byte
    {
        Hold,
        Toggle
    }
}
