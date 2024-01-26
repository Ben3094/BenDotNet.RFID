using System;

namespace BenDotNet.RFID.NFC.NFC_V
{
    public enum Manufacturer
    {
        Motorola = 0x01,
        STMicroelectronics = 0x02,
        Hitachi = 0x03,
        NXPSemiconductors = 0x04,
        InfineonTechnologies = 0x05,
        Cylink = 0x06,
        TexasInstrument = 0x07,
        FujitsuLimited = 0x08,
        MatsushitaElectronicsSemiconductorCompany = 0x09,
        NEC = 0x0A,
        OkiElectricIndustry = 0x0B,
        Toshiba = 0x0C,
        MitsubishiElectric = 0x0D,
        SamsungElectronics = 0x0E,
        Hynix = 0x0F,
        LGSemiconductors = 0x10,
        Emosyn_EMMicroelectronics = 0x11,
        INSIDETechnology = 0x12,
        ORGAKartensysteme = 0x13,
        SHARP = 0x14,
        ATMEL = 0x15,
        EMMicroelectronicMarin = 0x16,
        SMARTRACTECHNOLOGY = 0x17,
        ZMD = 0x18,
        XICOR = 0x19,
        Sony = 0x1A,
        MalaysiaMicroelectronicSolutions = 0x1B,
        Emosyn = 0x1C,
        ShanghaiFudanMicroelectronics = 0x1D,
        MagellanTechnology = 0x1E,
        Melexis = 0x1F,
        RenesasTechnology = 0x20,
        TAGSYS = 0x21,
        Transcore = 0x22,
        ShanghaiBelling = 0x23,
        MasktechGermany = 0x24,
        InnovisionResearchAndTechnology = 0x25,
        HitachiULSISystems = 0x26,
        Yubico = 0x27,
        Ricoh = 0x28,
        ASK = 0x29,
        UnicoreMicrosystems = 0x2A,
        DallasSemiconductor_Maxim = 0x2B,
        Impinj = 0x2C,
        RightPlugAlliance = 0x2D,
        Broadcom = 0x2E,
        MStarSemiconductor = 0x2F,
        BeeDarTechnology = 0x30,
        RFIDsec = 0x31,
        SchweizerElectronic = 0x32,
        AMICTechnologyp = 0x33,
        Mikron = 0x34,
        FraunhoferInstituteForPhotonicMicrosystems = 0x35,
        IDSMicrochip = 0x36,
        Kovio = 0x37,
        HMTMicroelectronic = 0x38,
        SiliconCraftTechnology = 0x39,
        AdvancedFilmDevice = 0x3A,
        Nitecrest = 0x3B,
        Verayo = 0x3C,
        HIDGlobal = 0x3D,
        ProductivityEngineering = 0x3E,
        Austriamicrosystems = 0x3F,
        Gemalto = 0x40,
        RenesasElectronics = 0x41,
        _3Alogics = 0x42,
        TopTroniQAsiaLimitedHong = 0x43,
        Gentag = 0x44,
        InvengoInformationTechnology = 0x45,
        GuangzhouSysurMicroelectronics = 0x46,
        CEITEC = 0x47,
        ShanghaiQuanrayElectronics = 0x48,
        MediaTek = 0x49,
        Angstrem = 0x4A,
        CelisicSemiconductor = 0x4B,
        LEGICIdentsystems = 0x4C,
        Balluff = 0x4D,
        OberthurTechnologies = 0x4E,
        Silterra = 0x4F,
        DELTADanishElectronics = 0x50,
        GieseckeAndDevrient = 0x51,
        ShenzhenChinaVisionMicroelectronics = 0x52,
        ShanghaiFeijuMicroelectronics = 0x53,
        Intel = 0x54,
        Microsensys = 0x55,
        SonixTechnology = 0x56,
        QualcommTechnologies = 0x57,
        RealtekSemiconductorp = 0x58,
        FreevisionTechnologies = 0x59,
        GiantecSemiconductor = 0x5A,
        AngstremT = 0x5B,
        STARCHIP = 0x5C,
        SPIRTECH = 0x5D,
        GANTNERElectronic = 0x5E,
        NordicSemiconductor = 0x5F,
        Verisiti = 0x60,
        WearlinksTechnology = 0x61,
        UserstarInformationSystems = 0x62,
        PragmaticPrinting = 0x63,
        LSI_TEC = 0x64,
        Tendyron = 0x65,
        MUTOSmart = 0x66,
        ONSemiconductor = 0x67,
        TUBITAK_BILGEM = 0x68,
        HuadaSemiconductor = 0x69,
        SEVENEY = 0x6A,
        ISSM = 0x6B,
        Wisesec = 0x6C,
        Holtek = 0x7E
    }

    public class Tag : NFC.Tag
    {
        public Tag(byte[] uid, DetectionSource detectionSource) : base(uid, detectionSource)
        {
            if (uid[TYPE5_TAG_PLATFORM_IDENTIFIER_BYTE_INDEX] != TYPE5_TAG_PLATFORM_IDENTIFIER)
                throw new ArgumentException("Not a NFC-V tag");

            this.ManufacturerIdentifier = uid[MANUFACTURER_IDENTIFIER_BYTE_INDEX];

            this.serialNumber = uid[SERIAL_NUMBER_BYTE_INDEX..(SERIAL_NUMBER_BYTE_INDEX + SERIAL_NUMBER_BYTE_LENGTH - 1)];
        }

        public const byte TYPE5_TAG_PLATFORM_IDENTIFIER_BYTE_INDEX = 7;
        public const byte TYPE5_TAG_PLATFORM_IDENTIFIER = 0xE0;

        public const byte MANUFACTURER_IDENTIFIER_BYTE_INDEX = 6;
        public readonly byte ManufacturerIdentifier;
        public Manufacturer Manufacturer => (Manufacturer)this.ManufacturerIdentifier;

        public const byte SERIAL_NUMBER_BYTE_INDEX = 0;
        public const byte SERIAL_NUMBER_BYTE_LENGTH = 6;
        private byte[] serialNumber;
        public byte[] SerialNumber
        {
            get { return this.serialNumber; }
            protected set { throw new ArgumentException("Serial number impossible to change"); }
        }
    }
}
