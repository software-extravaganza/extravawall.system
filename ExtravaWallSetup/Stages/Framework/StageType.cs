namespace ExtravaWallSetup.Stages.Framework
{
    public enum StageType
    {
        None = 0,
        Initialize = 10,
        Menu = 20,
        InstallBegin = 30,
        //InstallAgreement = 40,
        InstallCheckSystem = 50,
        InstallInstallPrerequisites = 60,
        InstallInstallProduct = 70,
        InstallConfigureBasics = 80,
        InstallFinish = 90,
        RecoverBegin = 100,
        RecoverAgreement = 110,
        RecoverCheckSystem = 120,
        End = 1000
    }
}