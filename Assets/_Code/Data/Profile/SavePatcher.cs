namespace Aqua.Profile
{
    static public class SavePatcher
    {
        public const uint CurrentVersion = 1;

        static public bool TryPatch(SaveData ioData)
        {
            if (ioData.Version == CurrentVersion)
                return false;

            Patch(ioData);
            ioData.Version = CurrentVersion;
            return true;
        }

        static private void Patch(SaveData ioData)
        {
            if (ioData.Version == 0)
            {
                UpgradeFromVersion0(ioData);
            }
        }

        static private void UpgradeFromVersion0(SaveData ioData)
        {
            ioData.Map.TimeMode = TimeMode.FreezeAt12;
            ioData.Map.CurrentTime = new GTDate(12, 0, 0);
        }
    }
}